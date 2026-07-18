using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Solhigson.Framework.AuditCapture;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Persistence.EntityModels;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// F3-prime explicit-logging service behaviour, over a real SQLite in-memory DB (per test-pattern rule; NEVER
/// the EF InMemory provider) — sibling of the F2 <c>TestAuditDbContext</c> pattern. A shared open connection
/// lets the write context and the assertion read see one DB. Under the never-block invariant <c>LogAsync</c>
/// hands the built row to an out-of-band <see cref="IAuditSink"/> when one is wired; with NO sink it uses the
/// transitional persisting-safe fallback (inline <c>Add</c>+<c>SaveChangesAsync</c> on the bound context, the
/// shipped behaviour MINUS the rethrow). The success-path tests below exercise the fallback (no sink); the
/// never-block routing and swallow contracts are exercised with an injected sink. Runs in a serialized
/// collection so the process-global <c>audit_capture_failed</c> meter is observed without cross-class leakage.
/// </summary>
[Collection(AuditNeverBlockMetricsCollection.Name)]
public sealed class AuditTrailServiceTests : IDisposable
{
    private const string SubjectUserId = "subject-7";
    private const string ActorUserId = "op-1";
    private const string ActorDisplayName = "Margaret Hamilton";
    private const string ActorIp = "198.51.100.4";
    private const string ActorSourceType = "web";
    private const string ActorSourceId = "corr-99";

    private static readonly AuditActor Operator = new()
    {
        ActorUserId = ActorUserId,
        UserDisplayName = ActorDisplayName,
        UserIp = ActorIp,
        SourceType = ActorSourceType,
        SourceId = ActorSourceId,
    };

    private readonly SqliteConnection _connection;

    public AuditTrailServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        using var ctx = CreateMappedContext();
        ctx.Database.EnsureCreated();
    }

    // ── (1) SecurityEvent path ─────────────────────────────────────────────────

    [Fact]
    public async Task SecurityEvent_WritesExactlyOneRow_WithPassedCategoryActorFieldsAndSubjectKeys()
    {
        using (var ctx = CreateMappedContext())
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx);
            await service.LogAsync(
                AuditEventCategory.SecurityEvent,
                entityType: "User",
                entityId: SubjectUserId,
                actor: Operator,
                payloadOrDescriptor: new { eventType = "login.failed", attempts = 3 },
                cancellationToken: CancellationToken.None);
        }

        var row = SingleAuditRow();
        row.Category.ShouldBe(AuditEventCategory.SecurityEvent);
        row.EntityType.ShouldBe("User");
        row.EntityId.ShouldBe(SubjectUserId);
        row.ActorUserId.ShouldBe(ActorUserId);
        row.UserDisplayName.ShouldBe(ActorDisplayName);
        row.UserIp.ShouldBe(ActorIp);
        row.SourceType.ShouldBe(ActorSourceType);
        row.SourceId.ShouldBe(ActorSourceId);
        row.Changes.ShouldBeNull();
        row.Snapshot.ShouldNotBeNull();
        GetDiscriminator(row).ShouldBe("login.failed");
    }

    // ── (2) BusinessEvent path ─────────────────────────────────────────────────

    [Fact]
    public async Task BusinessEvent_WritesExactlyOneRow_WithBusinessCategoryAndDescriptorSnapshot()
    {
        using (var ctx = CreateMappedContext())
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx);
            await service.LogAsync(
                AuditEventCategory.BusinessEvent,
                entityType: "Report",
                entityId: "rep-1",
                actor: Operator,
                payloadOrDescriptor: new { eventType = "report.exported", format = "csv" },
                cancellationToken: CancellationToken.None);
        }

        var row = SingleAuditRow();
        row.Category.ShouldBe(AuditEventCategory.BusinessEvent);
        row.EntityType.ShouldBe("Report");
        row.EntityId.ShouldBe("rep-1");
        row.Changes.ShouldBeNull();
        GetDiscriminator(row).ShouldBe("report.exported");
    }

    // ── (3) activation-gate miss ───────────────────────────────────────────────

    [Fact]
    public async Task GateMiss_OnAContextWithoutTheAuditTrailMapping_WritesNothingAndDoesNotThrow()
    {
        using var unmappedConnection = new SqliteConnection("Filename=:memory:");
        unmappedConnection.Open();
        var opt = new DbContextOptionsBuilder<UnmappedDbContext>().UseSqlite(unmappedConnection).Options;
        using var ctx = new UnmappedDbContext(opt);
        ctx.Database.EnsureCreated();
        var service = new AuditTrailService<UnmappedDbContext>(ctx);

        await Should.NotThrowAsync(() => service.LogAsync(
            AuditEventCategory.SecurityEvent,
            entityType: "User",
            entityId: SubjectUserId,
            actor: Operator,
            payloadOrDescriptor: new { eventType = "login.failed" },
            cancellationToken: CancellationToken.None));

        ctx.ChangeTracker.Entries().ShouldBeEmpty(); // nothing staged on the unmapped context
        ReadAuditRows().ShouldBeEmpty();             // and the mapped DB got nothing
    }

    // ── (4) DataChange rejection ───────────────────────────────────────────────

    [Fact]
    public async Task DataChangeCategory_IsRejectedWithArgumentOutOfRange_AndWritesNothing()
    {
        using var ctx = CreateMappedContext();
        var service = new AuditTrailService<ServiceAuditDbContext>(ctx);

        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(() => service.LogAsync(
            AuditEventCategory.DataChange,
            entityType: "User",
            entityId: SubjectUserId,
            actor: Operator,
            payloadOrDescriptor: new { eventType = "login.failed" },
            cancellationToken: CancellationToken.None));

        ex.ParamName.ShouldBe("category");
        ReadAuditRows().ShouldBeEmpty();
    }

    // ── (5) descriptor discriminators stay distinguishable ────────────────────

    [Fact]
    public async Task TwoEventsWithDifferentDescriptors_ProduceDistinguishableSnapshotPayloads()
    {
        using (var ctx = CreateMappedContext())
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx);
            await service.LogAsync(
                AuditEventCategory.SecurityEvent, "User", SubjectUserId, Operator,
                new { eventType = "login.failed", attempts = 3 },
                cancellationToken: CancellationToken.None);
            await service.LogAsync(
                AuditEventCategory.SecurityEvent, "User", SubjectUserId, Operator,
                new { eventType = "login.lockout", lockedMinutes = 15 },
                cancellationToken: CancellationToken.None);
        }

        var rows = ReadAuditRows();
        rows.Count.ShouldBe(2);

        var discriminators = rows.Select(GetDiscriminator).ToList();
        discriminators.ShouldContain("login.failed");
        discriminators.ShouldContain("login.lockout");
    }

    // ── (6) cooperative cancellation ───────────────────────────────────────────

    [Fact]
    public async Task PreCancelledToken_AbortsBeforeAnyWrite()
    {
        var preCancelled = new CancellationToken(canceled: true);

        using (var ctx = CreateMappedContext())
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx);
            await Should.ThrowAsync<OperationCanceledException>(() => service.LogAsync(
                AuditEventCategory.SecurityEvent,
                entityType: "User",
                entityId: SubjectUserId,
                actor: Operator,
                payloadOrDescriptor: new { eventType = "login.failed" },
                cancellationToken: preCancelled));

            ctx.ChangeTracker.Entries().ShouldBeEmpty(); // aborted before the row was even staged
        }

        ReadAuditRows().ShouldBeEmpty();
    }

    // ── (7) coexistence with the F2 interceptors ───────────────────────────────

    [Fact]
    public async Task LogAsyncRow_WithBothF2InterceptorsWired_IsNotItselfAuditedAndPassesAppendOnly()
    {
        using (var ctx = CreateMappedContext(withF2Interceptors: true))
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx);
            await service.LogAsync(
                AuditEventCategory.BusinessEvent,
                entityType: "Report",
                entityId: "rep-1",
                actor: Operator,
                payloadOrDescriptor: new { eventType = "report.exported" },
                cancellationToken: CancellationToken.None);
        }

        // Exactly the explicit row: the capture interceptor did not audit it (recursion exclusion),
        // and the append-only guard permitted the Added state.
        var row = SingleAuditRow();
        row.Category.ShouldBe(AuditEventCategory.BusinessEvent);
        GetDiscriminator(row).ShouldBe("report.exported");
    }

    // ── (8) container registration (deliverable 2) ─────────────────────────────

    [Fact]
    public void ModuleLoad_RegistersTheOpenGeneric_ResolvableForAConsumerContext()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new SolhigsonAutofacModule(new ConfigurationBuilder().Build(), (string?)null));
        var opt = new DbContextOptionsBuilder<ServiceAuditDbContext>().UseSqlite(_connection).Options;
        builder.Register(_ => new ServiceAuditDbContext(opt)).AsSelf().InstancePerLifetimeScope();

        using var container = builder.Build();
        using var scope = container.BeginLifetimeScope();

        var service = scope.Resolve<IAuditTrailService<ServiceAuditDbContext>>();
        service.ShouldBeOfType<AuditTrailService<ServiceAuditDbContext>>();
    }

    // ── (9) consumer-override precedence (review F2) ───────────────────────────

    [Fact]
    public void ConsumerClosedRegistration_AfterModuleLoad_BeatsTheOpenGenericDefault()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new SolhigsonAutofacModule(new ConfigurationBuilder().Build(), (string?)null));
        builder.RegisterType<StubConsumerAuditTrailService>()
            .As<IAuditTrailService<ServiceAuditDbContext>>()
            .InstancePerLifetimeScope();

        using var container = builder.Build();
        using var scope = container.BeginLifetimeScope();

        // Fitness function for the precedence contract the module registration comments promise:
        // an explicitly registered closed IAuditTrailService<TContext> always beats the module's
        // open-generic default.
        scope.Resolve<IAuditTrailService<ServiceAuditDbContext>>()
            .ShouldBeOfType<StubConsumerAuditTrailService>();
    }

    // ── (10) argument guards (review F3) ───────────────────────────────────────

    [Fact]
    public async Task NullEntityType_IsRejectedWithArgumentNullException_AndWritesNothing()
    {
        using var ctx = CreateMappedContext();
        var service = new AuditTrailService<ServiceAuditDbContext>(ctx);

        var ex = await Should.ThrowAsync<ArgumentNullException>(() => service.LogAsync(
            AuditEventCategory.SecurityEvent,
            entityType: null!,
            entityId: SubjectUserId,
            actor: Operator,
            payloadOrDescriptor: new { eventType = "login.failed" },
            cancellationToken: CancellationToken.None));

        ex.ParamName.ShouldBe("entityType");
        ReadAuditRows().ShouldBeEmpty();
    }

    [Fact]
    public async Task WhitespaceEntityType_IsRejectedWithArgumentException_AndWritesNothing()
    {
        using var ctx = CreateMappedContext();
        var service = new AuditTrailService<ServiceAuditDbContext>(ctx);

        var ex = await Should.ThrowAsync<ArgumentException>(() => service.LogAsync(
            AuditEventCategory.SecurityEvent,
            entityType: "   ",
            entityId: SubjectUserId,
            actor: Operator,
            payloadOrDescriptor: new { eventType = "login.failed" },
            cancellationToken: CancellationToken.None));

        ex.ParamName.ShouldBe("entityType");
        ReadAuditRows().ShouldBeEmpty();
    }

    [Fact]
    public async Task NullActor_IsRejectedWithArgumentNullException_AndWritesNothing()
    {
        using var ctx = CreateMappedContext();
        var service = new AuditTrailService<ServiceAuditDbContext>(ctx);

        var ex = await Should.ThrowAsync<ArgumentNullException>(() => service.LogAsync(
            AuditEventCategory.SecurityEvent,
            entityType: "User",
            entityId: SubjectUserId,
            actor: null!,
            payloadOrDescriptor: new { eventType = "login.failed" },
            cancellationToken: CancellationToken.None));

        ex.ParamName.ShouldBe("actor");
        ReadAuditRows().ShouldBeEmpty();
    }

    [Fact]
    public async Task NullPayloadOrDescriptor_IsRejectedWithArgumentNullException_AndWritesNothing()
    {
        using var ctx = CreateMappedContext();
        var service = new AuditTrailService<ServiceAuditDbContext>(ctx);

        var ex = await Should.ThrowAsync<ArgumentNullException>(() => service.LogAsync(
            AuditEventCategory.SecurityEvent,
            entityType: "User",
            entityId: SubjectUserId,
            actor: Operator,
            payloadOrDescriptor: null!,
            cancellationToken: CancellationToken.None));

        ex.ParamName.ShouldBe("payloadOrDescriptor");
        ReadAuditRows().ShouldBeEmpty();
    }

    // ── (11) never-block: routes through the sink, adds nothing to the bound context ──

    [Fact]
    public async Task LogAsync_WithSinkWired_RoutesToTheSink_AndAddsNothingToTheBoundContext()
    {
        var sink = new RecordingAuditSink();
        using (var ctx = CreateMappedContext())
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx, sink);
            await service.LogAsync(
                AuditEventCategory.SecurityEvent,
                entityType: "User",
                entityId: SubjectUserId,
                actor: Operator,
                payloadOrDescriptor: new { eventType = "login.failed" },
                cancellationToken: CancellationToken.None);

            // Never-block: the row went to the out-of-band sink, not the bound (business) context.
            ctx.ChangeTracker.Entries().ShouldBeEmpty();
        }

        var row = sink.Received.ShouldHaveSingleItem();
        row.Category.ShouldBe(AuditEventCategory.SecurityEvent);
        row.EntityId.ShouldBe(SubjectUserId);
        ReadAuditRows().ShouldBeEmpty(); // nothing persisted to the bound context's DB
    }

    // ── (12) never-block: an injected sink failure is swallowed (LogAsync does NOT throw) ──

    [Fact]
    public async Task LogAsync_WhenTheSinkThrows_SwallowsAndEmitsMetric_WithoutThrowing()
    {
        var sink = new ThrowingAuditSink();
        long failures = 0;
        await Should.NotThrowAsync(() => CountCaptureFailedAsync(async () =>
        {
            using var ctx = CreateMappedContext();
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx, sink);
            await service.LogAsync(
                AuditEventCategory.SecurityEvent, "User", SubjectUserId, Operator,
                new { eventType = "login.failed" },
                cancellationToken: CancellationToken.None);
        }, n => failures = n));

        failures.ShouldBe(1);
        ReadAuditRows().ShouldBeEmpty();
    }

    // ── (13) never-block: an injected payload-build failure is swallowed ────────

    [Fact]
    public async Task LogAsync_WhenTheDescriptorIsNonSerializable_SwallowsWithoutThrowing()
    {
        var cyclic = new CyclicDescriptor();
        cyclic.Self = cyclic; // System.Text.Json throws on the reference cycle — a payload-build failure

        using var ctx = CreateMappedContext();
        var service = new AuditTrailService<ServiceAuditDbContext>(ctx); // fallback path (no sink)

        await Should.NotThrowAsync(() => service.LogAsync(
            AuditEventCategory.SecurityEvent, "User", SubjectUserId, Operator,
            cyclic,
            cancellationToken: CancellationToken.None));

        ctx.ChangeTracker.Entries().ShouldBeEmpty(); // build failed before any Add
        ReadAuditRows().ShouldBeEmpty();
    }

    // ── (14) transitional fallback: with no sink, persists inline on the bound context ──

    [Fact]
    public async Task LogAsync_WithNoSink_UsesThePersistingFallback_OnTheBoundContext()
    {
        using (var ctx = CreateMappedContext())
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx); // no sink
            await service.LogAsync(
                AuditEventCategory.BusinessEvent, "Report", "rep-1", Operator,
                new { eventType = "report.exported" },
                cancellationToken: CancellationToken.None);
        }

        var row = SingleAuditRow(); // the transitional fallback persisted it on the bound context
        row.Category.ShouldBe(AuditEventCategory.BusinessEvent);
        GetDiscriminator(row).ShouldBe("report.exported");
    }

    // ── (15) explicit-path action + subject-name pass-through ──────────────────
    // The optional trailing params (AFTER the required cancellationToken — every caller passes the token
    // by name, so extending rather than overloading breaks no call site) stamp AuditTrail.Action and
    // AuditTrail.SubjectDisplayName. Action is VERBATIM — an eventType-as-action is NEVER forced
    // lowercase, unlike the interceptor's pinned AuditActions data-change values; omitted params leave
    // both columns null; overlong values truncate app-side to the declared 128/256 column widths instead
    // of faulting the audit INSERT.

    [Fact]
    public async Task LogAsync_WithActionAndSubjectDisplayName_StampsBothVerbatim()
    {
        using (var ctx = CreateMappedContext())
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx);
            await service.LogAsync(
                AuditEventCategory.SecurityEvent,
                entityType: "User",
                entityId: SubjectUserId,
                actor: Operator,
                payloadOrDescriptor: new { eventType = "login.MfaEnrolled" },
                cancellationToken: CancellationToken.None,
                action: "login.MfaEnrolled",
                subjectDisplayName: "Ada Lovelace");
        }

        var row = SingleAuditRow();
        row.Action.ShouldBe("login.MfaEnrolled");        // VERBATIM: mixed case survives, NOT forced lowercase
        row.SubjectDisplayName.ShouldBe("Ada Lovelace"); // the SUBJECT's name…
        row.UserDisplayName.ShouldBe(ActorDisplayName);  // …never conflated with the ACTOR's
    }

    [Fact]
    public async Task LogAsync_WithoutActionOrSubjectDisplayName_LeavesBothColumnsNull()
    {
        using (var ctx = CreateMappedContext())
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx);
            await service.LogAsync(
                AuditEventCategory.SecurityEvent,
                entityType: "User",
                entityId: SubjectUserId,
                actor: Operator,
                payloadOrDescriptor: new { eventType = "login.failed" },
                cancellationToken: CancellationToken.None);
        }

        var row = SingleAuditRow();
        row.Action.ShouldBeNull();
        row.SubjectDisplayName.ShouldBeNull();
    }

    [Fact]
    public async Task LogAsync_WithOverlongActionAndSubjectDisplayName_PersistsBothTruncatedToTheColumnWidths()
    {
        using (var ctx = CreateMappedContext())
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx);
            await service.LogAsync(
                AuditEventCategory.SecurityEvent,
                entityType: "User",
                entityId: SubjectUserId,
                actor: Operator,
                payloadOrDescriptor: new { eventType = "attempt" },
                cancellationToken: CancellationToken.None,
                action: new string('a', 200),
                subjectDisplayName: new string('s', 300));
        }

        // Truncated app-side (attacker-controlled length analogue) instead of faulting the audit INSERT.
        var row = SingleAuditRow();
        row.Action.ShouldBe(new string('a', 128));
        row.SubjectDisplayName.ShouldBe(new string('s', 256));
    }

    // ── (16) the interceptor's attribution gate NEVER gates the explicit path ──
    // RequireAttributedActor gates ONLY AuditCaptureSaveChangesInterceptor's DataChange capture; LogAsync
    // has no dependency on AuditCaptureOptions at all (ctor deps: TContext + IAuditSink?), so an explicit
    // event from AuditActor.UnattributedActor still writes even when the gate-ON interceptor is attached
    // to the very context the transitional fallback persists through — the DSAR shape. The fallback's
    // inline SaveChangesAsync fires that gated interceptor with an unattributed provider, proving
    // structurally that the gate skips DataChange MATERIALIZATION only and never blocks a save.

    [Fact]
    public async Task LogAsync_WithUnattributedActor_StillWritesTheRow_WhileTheInterceptorGateIsOn()
    {
        using (var ctx = CreateMappedContext(
            withF2Interceptors: true,
            captureOptions: new AuditCaptureOptions { RequireAttributedActor = true }))
        {
            var service = new AuditTrailService<ServiceAuditDbContext>(ctx);
            await service.LogAsync(
                AuditEventCategory.SecurityEvent,
                entityType: "User",
                entityId: SubjectUserId,
                actor: AuditActor.UnattributedActor,
                payloadOrDescriptor: new { eventType = "dsar.data.exported" },
                cancellationToken: CancellationToken.None);
        }

        var row = SingleAuditRow(); // written via the fallback's SaveChangesAsync THROUGH the gated interceptor
        row.Category.ShouldBe(AuditEventCategory.SecurityEvent);
        row.ActorUserId.ShouldBeNull();                        // unattributed — and STILL written
        row.UserDisplayName.ShouldBe(AuditActor.Unattributed);
        GetDiscriminator(row).ShouldBe("dsar.data.exported");
    }

    // ── infrastructure ─────────────────────────────────────────────────────────

    /// <summary>
    /// Observes the process-global <c>audit_capture_failed</c> counter across a single asynchronous action.
    /// The enclosing test class runs in a non-parallel collection (determinism seam).
    /// </summary>
    private static async Task CountCaptureFailedAsync(Func<Task> action, Action<long> onCount)
    {
        long total = 0;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == AuditCaptureDiagnostics.MeterName
                    && instrument.Name == "audit_capture_failed")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => Interlocked.Add(ref total, measurement));
        listener.Start();
        await action();
        listener.Dispose();
        onCount(Interlocked.Read(ref total));
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        public List<AuditTrail> Received { get; } = [];

        public Task PersistAsync(IReadOnlyList<AuditTrail> rows, CancellationToken cancellationToken)
        {
            Received.AddRange(rows);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingAuditSink : IAuditSink
    {
        public Task PersistAsync(IReadOnlyList<AuditTrail> rows, CancellationToken cancellationToken)
            => throw new InvalidOperationException("sink is down");
    }

    private sealed class CyclicDescriptor
    {
        public CyclicDescriptor? Self { get; set; }
    }

    private ServiceAuditDbContext CreateMappedContext(
        bool withF2Interceptors = false,
        AuditCaptureOptions? captureOptions = null)
    {
        var optBuilder = new DbContextOptionsBuilder<ServiceAuditDbContext>().UseSqlite(_connection);
        if (withF2Interceptors)
        {
            optBuilder.AddInterceptors(
                new AuditCaptureSaveChangesInterceptor(
                    new UnattributedAuditActorProvider(),
                    new AuditCaptureRegistry(),
                    captureOptions ?? new AuditCaptureOptions()),
                new AuditTrailAppendOnlyInterceptor());
        }

        return new ServiceAuditDbContext(optBuilder.Options);
    }

    private List<AuditTrail> ReadAuditRows()
    {
        using var read = CreateMappedContext();
        return read.Set<AuditTrail>().AsNoTracking().OrderBy(x => x.Created).ThenBy(x => x.Id).ToList();
    }

    private AuditTrail SingleAuditRow() => ReadAuditRows().Single();

    private static string? GetDiscriminator(AuditTrail row)
    {
        using var doc = JsonDocument.Parse(row.Snapshot!);
        return doc.RootElement.GetProperty("eventType").GetString();
    }

    public void Dispose() => _connection.Dispose();

    // ── test model ─────────────────────────────────────────────────────────────

    /// <summary>Consumer-shaped context: maps <see cref="AuditTrail"/> exactly like the F2 test context.</summary>
    private sealed class ServiceAuditDbContext(DbContextOptions<ServiceAuditDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditTrail>(b =>
            {
                b.HasKey(x => new { x.Id, x.Created });
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Property(x => x.Created).ValueGeneratedNever();
            });
        }
    }

    /// <summary>Gate-miss context: a real model that simply omits the <see cref="AuditTrail"/> mapping block.</summary>
    private sealed class UnmappedDbContext(DbContextOptions<UnmappedDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UnrelatedEntity>().HasKey(x => x.Id);
        }
    }

    private sealed class UnrelatedEntity
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    /// <summary>
    /// Trivial consumer replacement for test (9): closed registration made AFTER the module load must
    /// beat the module's open-generic <see cref="AuditTrailService{TContext}"/> default.
    /// </summary>
    private sealed class StubConsumerAuditTrailService : IAuditTrailService<ServiceAuditDbContext>
    {
        public Task LogAsync(
            AuditEventCategory category,
            string entityType,
            string entityId,
            AuditActor actor,
            object payloadOrDescriptor,
            CancellationToken cancellationToken,
            string? action = null,
            string? subjectDisplayName = null) => Task.CompletedTask;
    }
}
