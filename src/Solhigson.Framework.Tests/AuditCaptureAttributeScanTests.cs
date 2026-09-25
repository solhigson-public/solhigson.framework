using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Shouldly;
using Solhigson.Framework.AuditCapture;
using Solhigson.Framework.Data.Attributes;
using Solhigson.Framework.Persistence.EntityModels;
using Xunit;
using SolhigsonLogManager = Solhigson.Framework.Logging.LogManager;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Inert audit-attribute scan: <c>[SolhigsonAuditInclude]</c>/<c>[SolhigsonAuditIgnore]</c> on an UNMAPPED base
/// class is silently ignored by concrete-type-only eligibility, so the interceptor warns once per model. These tests
/// assert on the pure scan's findings, on the once-per-model memo wiring, and on the warning actually reaching the
/// logger factory installed at emit time. Every model is built from its OWN DbContext type because EF caches one
/// <see cref="IModel"/> per context type process-wide and the memo is a static per-model table; a shared context type
/// would let one test's scan pre-claim a sibling test's model.
///
/// <para>The scan defers while <see cref="SolhigsonLogManager"/> has no factory, so the class installs a capturing
/// factory for every test and restores the process's previous factory afterwards. It shares the serialized audit
/// collection because <see cref="SolhigsonLogManager"/>'s factory and the <c>audit_capture_failed</c> meter are
/// process-global.</para>
/// </summary>
[Collection(AuditNeverBlockMetricsCollection.Name)]
public sealed class AuditCaptureAttributeScanTests : IDisposable
{
    private const string InertTemplatePrefix = "Inert audit attribute {AuditAttribute}";

    private readonly ILoggerFactory? _previousFactory;
    private readonly string? _previousServiceName;
    private readonly LockedCapturingLoggerFactory _capture = new();

    public AuditCaptureAttributeScanTests()
    {
        _previousFactory = AuditCaptureLog.CurrentLoggerFactory;
        _previousServiceName = SolhigsonLogManager.ServiceName;
        SolhigsonLogManager.SetLoggerFactory(_capture);
    }

    public void Dispose()
    {
        if (_previousFactory is not null)
        {
            SolhigsonLogManager.SetLoggerFactory(_previousFactory, _previousServiceName);
        }
        else
        {
            ClearLogManagerFactory();
            SolhigsonLogManager.ServiceName = _previousServiceName;
        }
    }

    // ── pure scan: findings ─────────────────────────────────────────────────

    [Fact]
    public void Scan_UnmappedIncludeBase_IsReportedOnce_NamingEveryConcreteType()
    {
        var findings = AuditCaptureAttributeScan.Scan(ModelOf<InertContext>());

        var finding = findings.Single(f => f.InertType == typeof(UnmappedIncludeBase));
        finding.AttributeType.ShouldBe(typeof(SolhigsonAuditIncludeAttribute));
        finding.AttributeName.ShouldBe("[SolhigsonAuditInclude]");
        finding.CarriesBothAttributes.ShouldBeFalse();
        finding.ConcreteTypes.ShouldBe([typeof(InertConcreteA), typeof(InertConcreteB)], ignoreOrder: true);
    }

    [Fact]
    public void Scan_UnmappedGrandparentIgnoreBase_IsFound_ThroughAnUndecoratedUnmappedParent()
    {
        var findings = AuditCaptureAttributeScan.Scan(ModelOf<InertContext>());

        var finding = findings.Single(f => f.InertType == typeof(GrandIgnoreBase));
        finding.AttributeName.ShouldBe("[SolhigsonAuditIgnore]");
        finding.ConcreteTypes.ShouldBe([typeof(InertConcreteC)]);

        // Exactly the two inert sites: the undecorated MidBase and the concrete types are never reported.
        findings.Count.ShouldBe(2);
    }

    [Fact]
    public void Scan_BothAttributesOnOneUnmappedBase_ReportsEachAttribute_AndFlagsThePrecedence()
    {
        var findings = AuditCaptureAttributeScan.Scan(ModelOf<DualContext>());

        findings.Count.ShouldBe(2);
        findings.ShouldAllBe(f => f.InertType == typeof(DualBase) && f.CarriesBothAttributes);
        findings.Select(f => f.AttributeName)
            .ShouldBe(["[SolhigsonAuditIgnore]", "[SolhigsonAuditInclude]"], ignoreOrder: true);
    }

    [Fact]
    public void Scan_ConcretePlacementsAndUndecoratedBases_ProduceZeroFindings()
    {
        AuditCaptureAttributeScan.Scan(ModelOf<CleanContext>()).ShouldBeEmpty();
    }

    [Fact]
    public void Scan_DecoratedBaseThatIsItselfMapped_Tph_IsNotReported()
    {
        var model = ModelOf<TphContext>();
        model.FindEntityType(typeof(TphRoot)).ShouldNotBeNull(); // guard: the base really is mapped

        AuditCaptureAttributeScan.Scan(model).ShouldBeEmpty();
    }

    [Fact]
    public void Scan_OwnedTypeWithDecoratedUnmappedBase_IsReported()
    {
        // Owned entities are tracked and captured like any other entry, so an inert base above one is a real miss.
        var model = ModelOf<OwnedContext>();
        model.GetEntityTypes().Single(e => e.ClrType == typeof(OwnedAddress)).IsOwned().ShouldBeTrue(); // guard

        var finding = AuditCaptureAttributeScan.Scan(model).ShouldHaveSingleItem();
        finding.InertType.ShouldBe(typeof(AddressBase));
        finding.ConcreteTypes.ShouldBe([typeof(OwnedAddress)]);
    }

    [Fact]
    public void Scan_KeylessTypeWithDecoratedUnmappedBase_IsSkipped()
    {
        // Keyless types are never tracked, so they can never be captured whatever the placement.
        var model = ModelOf<KeylessContext>();
        model.FindEntityType(typeof(KeylessView))!.FindPrimaryKey().ShouldBeNull(); // guard: really keyless

        AuditCaptureAttributeScan.Scan(model).ShouldBeEmpty();
    }

    [Fact]
    public void DisplayName_ClosedGenericBase_RendersReadableArguments_NotAssemblyQualifiedNames()
    {
        var finding = AuditCaptureAttributeScan.Scan(ModelOf<GenericBaseContext>()).ShouldHaveSingleItem();
        finding.InertType.ShouldBe(typeof(GenericIncludeBase<PlainEntity>));

        AuditCaptureAttributeScan.DisplayName(finding.InertType).ShouldBe(
            typeof(AuditCaptureAttributeScanTests).FullName + "+GenericIncludeBase<"
            + typeof(PlainEntity).FullName + ">");
    }

    // ── interceptor wiring: once per model, gated, never throws ────────────────

    [Fact]
    public void InertAttributeScan_RunsOncePerModel_SecondCallIsMemoized()
    {
        var interceptor = NewInterceptor();
        var model = ModelOf<MemoContext>();

        interceptor.SimulateInertAttributeScan(model).ShouldBe(InertAttributeScanOutcome.Scanned);
        interceptor.SimulateInertAttributeScan(model).ShouldBe(InertAttributeScanOutcome.AlreadyClaimed);

        // The memo is static per model, not per interceptor instance: a second instance must not rescan either.
        NewInterceptor().SimulateInertAttributeScan(model).ShouldBe(InertAttributeScanOutcome.AlreadyClaimed);
    }

    [Fact]
    public void SavingChanges_OnAnAuditTrailMappedContext_ClaimsTheModelScan()
    {
        var interceptor = NewInterceptor();
        using var ctx = NewContext<WiredContext>();

        interceptor.SimulateSavingChanges(ctx);
        interceptor.SimulateSavingChanges(ctx);

        // Capture already scanned this model, so an explicit scan is a memoized no-op.
        interceptor.SimulateInertAttributeScan(ctx.Model).ShouldBe(InertAttributeScanOutcome.AlreadyClaimed);
    }

    [Fact]
    public void SavingChanges_OnAContextWithoutAuditTrail_NeverScans()
    {
        var interceptor = NewInterceptor();
        using var ctx = NewContext<UngatedContext>();

        interceptor.SimulateSavingChanges(ctx);

        // The activation gate returned before the scan, so the model is still unclaimed.
        AuditCaptureAttributeScan.IsClaimed(ctx.Model).ShouldBeFalse();
        interceptor.SimulateInertAttributeScan(ctx.Model).ShouldBe(InertAttributeScanOutcome.Scanned);
    }

    [Fact]
    public void InertAttributeScan_ThrowingScanner_IsSwallowed_AndNotRetried()
    {
        var interceptor = NewInterceptor();
        var model = ModelOf<ThrowingScanContext>();
        var calls = 0;

        IReadOnlyList<InertAuditAttributeFinding> Throwing(IModel _)
        {
            calls++;
            throw new InvalidOperationException("scan blew up");
        }

        interceptor.SimulateInertAttributeScan(model, Throwing).ShouldBe(InertAttributeScanOutcome.Swallowed);
        interceptor.SimulateInertAttributeScan(model, Throwing).ShouldBe(InertAttributeScanOutcome.AlreadyClaimed);
        calls.ShouldBe(1);
        _capture.Snapshot().ShouldContain(e =>
            e.LoggerName == AuditCaptureLog.LoggerName && e.Level == LogLevel.Error && e.Exception != null);
    }

    [Fact]
    public void InertAttributeScan_WithFindings_ReportsScanned_AndLogsWithTheBothAttributesHint()
    {
        var interceptor = NewInterceptor();

        interceptor.SimulateInertAttributeScan(ModelOf<LoggedDualContext>()).ShouldBe(InertAttributeScanOutcome.Scanned);

        var warnings = InertWarnings();
        warnings.Count.ShouldBe(2);
        warnings.ShouldAllBe(w => ((string)w.Value("PrecedenceNote")!).Contains("ignore wins"));
    }

    [Fact]
    public void InertWarning_AfterASaveBeforeLoggingIsConfigured_ReachesTheLaterFactory_ExactlyOnce()
    {
        // The consumer shape the review found: statics touched and a gated save run (startup migrations) BEFORE
        // LogManager.SetLoggerFactory, with the interceptor's logger name already cached as a dead wrapper.
        ClearLogManagerFactory();
        _ = SolhigsonLogManager.GetLogger(AuditCaptureLog.LoggerName); // null-backed wrapper now cached by name

        var registry = new AuditCaptureRegistry().Include<InertConcreteB>();
        var interceptor = new AuditCaptureSaveChangesInterceptor(
            new UnattributedAuditActorProvider(), registry, new AuditCaptureOptions());
        using var ctx = NewContext<LateLoggingContext>();

        interceptor.SimulateSavingChanges(ctx);
        interceptor.SimulateInertAttributeScan(ctx.Model).ShouldBe(InertAttributeScanOutcome.DeferredNoUsableLogger);
        AuditCaptureAttributeScan.IsClaimed(ctx.Model).ShouldBeFalse(); // the scan was not spent

        SolhigsonLogManager.SetLoggerFactory(_capture);
        interceptor.SimulateSavingChanges(ctx);
        interceptor.SimulateSavingChanges(ctx);

        var warnings = InertWarnings();
        warnings.Count.ShouldBe(2); // one per (inert base, attribute): UnmappedIncludeBase and GrandIgnoreBase

        var include = warnings.Single(w => (string?)w.Value("InertType") == typeof(UnmappedIncludeBase).FullName);
        include.ArgumentNames.ShouldBe(
            ["AuditAttribute", "InertType", "ConcreteTypes", "EffectiveCapture", "PrecedenceNote"]);
        include.Value("AuditAttribute").ShouldBe("[SolhigsonAuditInclude]");
        include.Value("ConcreteTypes").ShouldBe(typeof(InertConcreteA).FullName + ", " + typeof(InertConcreteB).FullName);
        include.Value("EffectiveCapture").ShouldBe(
            typeof(InertConcreteA).FullName + ": not captured; " + typeof(InertConcreteB).FullName + ": captured");
        include.Value("PrecedenceNote").ShouldBe(string.Empty);
    }

    [Fact]
    public void CaptureFailureError_AfterLoggingIsConfiguredLate_ReachesTheFactory()
    {
        // Same dead-cached-wrapper shape as above, for the existing capture-build error log.
        ClearLogManagerFactory();
        _ = SolhigsonLogManager.GetLogger(AuditCaptureLog.LoggerName);
        SolhigsonLogManager.SetLoggerFactory(_capture);

        var interceptor = new AuditCaptureSaveChangesInterceptor(
            new ThrowingActorProvider(), new AuditCaptureRegistry(), new AuditCaptureOptions());
        using var ctx = NewContext<ErrorLogContext>();

        interceptor.SimulateSavingChanges(ctx);

        _capture.Snapshot().ShouldContain(e =>
            e.LoggerName == AuditCaptureLog.LoggerName
            && e.Level == LogLevel.Error
            && e.Exception is InvalidOperationException);
    }

    [Fact]
    public void InertAttributeScan_WithADisposedInstalledFactory_DoesNotThrow_AndDoesNotSpendTheScan()
    {
        var broken = LoggerFactory.Create(_ => { });
        SolhigsonLogManager.SetLoggerFactory(broken); // SetLoggerFactory itself calls CreateLogger, so install first
        broken.Dispose(); // CreateLogger now throws ObjectDisposedException

        var model = ModelOf<DisposedFactoryScanContext>();

        NewInterceptor().SimulateInertAttributeScan(model).ShouldBe(InertAttributeScanOutcome.DeferredNoUsableLogger);
        AuditCaptureAttributeScan.IsClaimed(model).ShouldBeFalse();

        // Once a working factory is installed, the deferred scan runs.
        SolhigsonLogManager.SetLoggerFactory(_capture);
        NewInterceptor().SimulateInertAttributeScan(model).ShouldBe(InertAttributeScanOutcome.Scanned);
    }

    [Fact]
    public void CaptureFailure_WithADisposedInstalledFactory_DoesNotThrowOutOfSavingChanges()
    {
        var broken = LoggerFactory.Create(_ => { });
        SolhigsonLogManager.SetLoggerFactory(broken);
        broken.Dispose();

        var interceptor = new AuditCaptureSaveChangesInterceptor(
            new ThrowingActorProvider(), new AuditCaptureRegistry(), new AuditCaptureOptions());
        using var ctx = NewContext<DisposedFactoryCaptureContext>();

        // Capture-build throws at actor resolution; its failure log must not rethrow through the broken factory.
        Should.NotThrow(() => interceptor.SimulateSavingChanges(ctx));
    }

    [Fact]
    public void CleanModel_EmitsZeroInertWarnings()
    {
        NewInterceptor().SimulateInertAttributeScan(ModelOf<LoggedCleanContext>()).ShouldBe(InertAttributeScanOutcome.Scanned);

        InertWarnings().ShouldBeEmpty();
    }

    [Fact]
    public void WarnOnInertAuditAttributesOnce_ChecksLockFreeIsClaimed_BeforeLockingTryClaim()
    {
        // Source pin (F3): TryClaim alone still yields AlreadyClaimed, so no behavioural test can see a dropped
        // fast path; only the order of the calls in the method body shows it. IsClaimed must also precede
        // TryCreateBound, whose CreateLogger takes the factory's lock on every save it runs on.
        var source = File.ReadAllText(Path.Combine(
            FindSrcDirectory(), "Solhigson.Framework", "AuditCapture", "AuditCaptureSaveChangesInterceptor.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        var start = source.IndexOf("InertAttributeScanOutcome WarnOnInertAuditAttributesOnce(", StringComparison.Ordinal);
        start.ShouldBeGreaterThan(-1);
        var end = source.IndexOf("\n    }\n", start, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(start);
        var body = source[start..end];

        var isClaimed = body.IndexOf("AuditCaptureAttributeScan.IsClaimed(model)", StringComparison.Ordinal);
        var bind = body.IndexOf("AuditCaptureLog.TryCreateBound(", StringComparison.Ordinal);
        var tryClaim = body.IndexOf("AuditCaptureAttributeScan.TryClaim(model)", StringComparison.Ordinal);
        isClaimed.ShouldBeGreaterThan(-1);
        bind.ShouldBeGreaterThan(-1);
        tryClaim.ShouldBeGreaterThan(-1);
        isClaimed.ShouldBeLessThan(bind);
        isClaimed.ShouldBeLessThan(tryClaim);
    }

    // ── helpers ─────────────────────────────────────────────────────────────
    /// <summary>
    /// The src folder, from this file's compile-time path (src/Solhigson.Framework.Tests/&lt;file&gt;), so the pin
    /// does not depend on where the test output directory sits.
    /// </summary>
    private static string FindSrcDirectory([CallerFilePath] string thisFile = "")
        => Path.GetDirectoryName(Path.GetDirectoryName(thisFile))
           ?? throw new InvalidOperationException("No src folder above " + thisFile);


    private List<CapturedLogEntry> InertWarnings()
        => _capture.Snapshot()
            .Where(e => e.LoggerName == AuditCaptureLog.LoggerName
                        && e.Level == LogLevel.Warning
                        && (e.Template?.StartsWith(InertTemplatePrefix, StringComparison.Ordinal) ?? false))
            .ToList();

    /// <summary>LogManager exposes no way to uninstall its factory; mirror a process that has not configured it yet.</summary>
    private static void ClearLogManagerFactory()
    {
        foreach (var name in new[] { "_loggerFactory", "_logger" })
        {
            typeof(SolhigsonLogManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, null);
        }
    }

    private static AuditCaptureSaveChangesInterceptor NewInterceptor()
        => new(new UnattributedAuditActorProvider(), new AuditCaptureRegistry(), new AuditCaptureOptions());

    private static TContext NewContext<TContext>() where TContext : DbContext
    {
        // Model building needs no open connection; these tests never open one.
        var options = new DbContextOptionsBuilder<TContext>().UseSqlite("Filename=:memory:").Options;
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }

    private static IModel ModelOf<TContext>() where TContext : DbContext
    {
        using var ctx = NewContext<TContext>();
        return ctx.Model;
    }

    private static void MapAuditTrail(ModelBuilder modelBuilder)
        => modelBuilder.Entity<AuditTrail>(b =>
        {
            b.HasKey(x => new { x.Id, x.Created });
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Created).ValueGeneratedNever();
        });

    private static void MapInert(ModelBuilder modelBuilder)
    {
        MapAuditTrail(modelBuilder);
        modelBuilder.Entity<InertConcreteA>().HasKey(x => x.Id);
        modelBuilder.Entity<InertConcreteB>().HasKey(x => x.Id);
        modelBuilder.Entity<InertConcreteC>().HasKey(x => x.Id);
    }

    private sealed class ThrowingActorProvider : IAuditActorProvider
    {
        public AuditActor GetCurrentActor() => throw new InvalidOperationException("actor resolution blew up");
    }

    // ── test model: inert placements ─────────────────────────────────────────

    [SolhigsonAuditInclude]
    private abstract class UnmappedIncludeBase
    {
        public string Id { get; set; } = null!;
    }

    private sealed class InertConcreteA : UnmappedIncludeBase;

    private sealed class InertConcreteB : UnmappedIncludeBase;

    [SolhigsonAuditIgnore]
    private abstract class GrandIgnoreBase
    {
        public string Id { get; set; } = null!;
    }

    private abstract class MidBase : GrandIgnoreBase;

    private sealed class InertConcreteC : MidBase;

    [SolhigsonAuditInclude]
    [SolhigsonAuditIgnore]
    private abstract class DualBase
    {
        public string Id { get; set; } = null!;
    }

    private sealed class DualConcrete : DualBase;

    [SolhigsonAuditInclude]
    private abstract class GenericIncludeBase<T>
    {
        public string Id { get; set; } = null!;
    }

    private sealed class GenericConcrete : GenericIncludeBase<PlainEntity>;

    [SolhigsonAuditInclude]
    private abstract class KeylessBase
    {
        public string? Name { get; set; }
    }

    private sealed class KeylessView : KeylessBase;

    // ── test model: clean placements ─────────────────────────────────────────

    private abstract class UndecoratedBase
    {
        public string Id { get; set; } = null!;
    }

    [SolhigsonAuditInclude]
    private sealed class ConcreteIncluded : UndecoratedBase;

    [SolhigsonAuditIgnore]
    private sealed class ConcreteIgnored : UndecoratedBase;

    [SolhigsonAuditInclude]
    private abstract class TphRoot
    {
        public string Id { get; set; } = null!;
    }

    private sealed class TphDerivedA : TphRoot;

    private sealed class TphDerivedB : TphRoot;

    [SolhigsonAuditInclude]
    private abstract class AddressBase
    {
        public string? Street { get; set; }
    }

    private sealed class OwnedAddress : AddressBase;

    private sealed class Owner
    {
        public string Id { get; set; } = null!;
        public OwnedAddress Address { get; set; } = null!;
    }

    private sealed class PlainEntity
    {
        public string Id { get; set; } = null!;
    }

    // ── one DbContext type per model (EF caches the model per context type) ────

    private sealed class InertContext(DbContextOptions<InertContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => MapInert(modelBuilder);
    }

    private sealed class LateLoggingContext(DbContextOptions<LateLoggingContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => MapInert(modelBuilder);
    }

    private sealed class DualContext(DbContextOptions<DualContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<DualConcrete>().HasKey(x => x.Id);
    }

    private sealed class LoggedDualContext(DbContextOptions<LoggedDualContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<DualConcrete>().HasKey(x => x.Id);
    }

    private sealed class GenericBaseContext(DbContextOptions<GenericBaseContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<GenericConcrete>().HasKey(x => x.Id);
    }

    private sealed class KeylessContext(DbContextOptions<KeylessContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<KeylessView>().HasNoKey();
    }

    private sealed class CleanContext(DbContextOptions<CleanContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            MapAuditTrail(modelBuilder);
            modelBuilder.Entity<ConcreteIncluded>().HasKey(x => x.Id);
            modelBuilder.Entity<ConcreteIgnored>().HasKey(x => x.Id);
        }
    }

    private sealed class TphContext(DbContextOptions<TphContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TphRoot>().HasKey(x => x.Id);
            modelBuilder.Entity<TphDerivedA>();
            modelBuilder.Entity<TphDerivedB>();
        }
    }

    private sealed class OwnedContext(DbContextOptions<OwnedContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Owner>(b =>
            {
                b.HasKey(x => x.Id);
                b.OwnsOne(x => x.Address);
            });
    }

    private sealed class MemoContext(DbContextOptions<MemoContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            MapAuditTrail(modelBuilder);
            modelBuilder.Entity<PlainEntity>().HasKey(x => x.Id);
        }
    }

    private sealed class WiredContext(DbContextOptions<WiredContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            MapAuditTrail(modelBuilder);
            modelBuilder.Entity<PlainEntity>().HasKey(x => x.Id);
        }
    }

    private sealed class ErrorLogContext(DbContextOptions<ErrorLogContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            MapAuditTrail(modelBuilder);
            modelBuilder.Entity<PlainEntity>().HasKey(x => x.Id);
        }
    }

    private sealed class DisposedFactoryScanContext(DbContextOptions<DisposedFactoryScanContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => MapInert(modelBuilder);
    }

    private sealed class DisposedFactoryCaptureContext(DbContextOptions<DisposedFactoryCaptureContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            MapAuditTrail(modelBuilder);
            modelBuilder.Entity<PlainEntity>().HasKey(x => x.Id);
        }
    }

    private sealed class LoggedCleanContext(DbContextOptions<LoggedCleanContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            MapAuditTrail(modelBuilder);
            modelBuilder.Entity<ConcreteIncluded>().HasKey(x => x.Id);
            modelBuilder.Entity<ConcreteIgnored>().HasKey(x => x.Id);
        }
    }

    private sealed class UngatedContext(DbContextOptions<UngatedContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<PlainEntity>().HasKey(x => x.Id);
    }

    private sealed class ThrowingScanContext(DbContextOptions<ThrowingScanContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<PlainEntity>().HasKey(x => x.Id);
    }

    /// <summary>
    /// Twin of <see cref="CapturingLoggerFactory"/> with locked appends and a snapshot read. The collection disables
    /// parallelization, so no other test runs while it is installed; the lock only covers a fire-and-forget handoff
    /// continuation that could log after the test body has moved on.
    /// </summary>
    private sealed class LockedCapturingLoggerFactory : ILoggerFactory
    {
        private readonly List<CapturedLogEntry> _entries = [];

        public List<CapturedLogEntry> Snapshot()
        {
            lock (_entries)
            {
                return [.. _entries];
            }
        }

        public ILogger CreateLogger(string categoryName) => new Logger(categoryName, this);

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }

        private void Add(CapturedLogEntry entry)
        {
            lock (_entries)
            {
                _entries.Add(entry);
            }
        }

        private sealed class Logger(string name, LockedCapturingLoggerFactory owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var values = new List<KeyValuePair<string, object?>>();
                if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
                {
                    values.AddRange(pairs);
                }

                owner.Add(new CapturedLogEntry(name, logLevel, values, exception));
            }
        }
    }
}

internal static class CapturedLogEntryExtensions
{
    /// <summary>The value of the named template argument, or null when absent.</summary>
    public static object? Value(this CapturedLogEntry entry, string name)
        => entry.Values.FirstOrDefault(v => v.Key == name).Value;
}
