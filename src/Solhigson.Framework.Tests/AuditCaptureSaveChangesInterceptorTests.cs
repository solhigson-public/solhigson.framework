using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Solhigson.Framework.AuditCapture;
using Solhigson.Framework.Data.Attributes;
using Solhigson.Framework.Persistence.EntityModels;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// F2 capture-interceptor behaviour, over a real SQLite in-memory DB (per test-pattern rule; NEVER the EF
/// InMemory provider). A shared open connection lets the write context and the assertion read see one DB.
/// The test DbContext maps <see cref="AuditTrail"/> in its own <c>OnModelCreating</c> (composite key
/// (Id, Created), payloads as text) alongside attribute-decorated test entities; both interceptors are wired
/// via <c>AddInterceptors</c>; the actor seam is a stub injected directly.
/// </summary>
public sealed class AuditCaptureSaveChangesInterceptorTests : IDisposable
{
    private const string ActorUserId = "actor-1";
    private const string ActorDisplayName = "Grace Hopper";
    private const string ActorIp = "203.0.113.7";
    private const string ActorSourceType = "web";
    private const string ActorSourceId = "corr-42";

    private readonly SqliteConnection _connection;
    private readonly AuditCaptureRegistry _registry = new();
    private readonly IAuditActorProvider _fullActor = new StubActorProvider(new AuditActor
    {
        ActorUserId = ActorUserId,
        UserDisplayName = ActorDisplayName,
        UserIp = ActorIp,
        SourceType = ActorSourceType,
        SourceId = ActorSourceId,
    });

    public AuditCaptureSaveChangesInterceptorTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }

    // ── (a) capture shape ─────────────────────────────────────────────────────

    [Fact]
    public void Insert_OfAnIncludedEntity_WritesSnapshotAndNullChanges()
    {
        using (var write = CreateContext())
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        var row = SingleAuditRow();
        row.Category.ShouldBe(AuditEventCategory.DataChange);
        row.EntityType.ShouldBe(nameof(IncludedEntity));
        row.EntityId.ShouldBe("inc-1");
        row.Changes.ShouldBeNull();
        row.Snapshot.ShouldNotBeNull();

        using var doc = JsonDocument.Parse(row.Snapshot!);
        doc.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");
    }

    [Fact]
    public async Task AsyncInsert_OfAnIncludedEntity_WritesSnapshotAndNullChanges()
    {
        using (var write = CreateContext())
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            await write.SaveChangesAsync();
        }

        var row = SingleAuditRow();
        row.Category.ShouldBe(AuditEventCategory.DataChange);
        row.EntityType.ShouldBe(nameof(IncludedEntity));
        row.EntityId.ShouldBe("inc-1");
        row.Changes.ShouldBeNull();
        row.Snapshot.ShouldNotBeNull();

        using var doc = JsonDocument.Parse(row.Snapshot!);
        doc.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");
    }

    [Fact]
    public void Update_OfAnIncludedEntity_WritesChangesArrayOfModifiedFieldsOnly()
    {
        using (var seed = CreateContext())
        {
            seed.Add(NewIncluded("inc-1", name: "Ada"));
            seed.SaveChanges();
        }

        using (var write = CreateContext())
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            entity.Name = "Grace"; // Email/ApiKey/Nickname/InternalNote unchanged
            write.SaveChanges();
        }

        var row = UpdateAuditRow();
        row.Snapshot.ShouldBeNull();
        row.Changes.ShouldNotBeNull();

        using var doc = JsonDocument.Parse(row.Changes!);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().ShouldBe(1); // only the modified field

        var change = doc.RootElement[0];
        change.GetProperty("field").GetString().ShouldBe("Name");
        change.GetProperty("old").GetString().ShouldBe("Ada");
        change.GetProperty("new").GetString().ShouldBe("Grace");
    }

    [Fact]
    public void Delete_OfAnIncludedEntity_WritesSnapshotAndNullChanges()
    {
        using (var seed = CreateContext())
        {
            seed.Add(NewIncluded("inc-1", name: "Ada"));
            seed.SaveChanges();
        }

        using (var write = CreateContext())
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            write.Remove(entity);
            write.SaveChanges();
        }

        // Two rows exist for inc-1 (INSERT then DELETE); the DELETE row is the later one.
        var deleteRow = ReadAuditRows().Where(x => x.EntityId == "inc-1").OrderBy(x => x.Created).ThenBy(x => x.Id).Last();
        deleteRow.Changes.ShouldBeNull();
        deleteRow.Snapshot.ShouldNotBeNull();

        using var doc = JsonDocument.Parse(deleteRow.Snapshot!);
        doc.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");
    }

    // ── (b) masking fail-closed, both sides ────────────────────────────────────

    [Fact]
    public void Insert_MasksPersonalDataAndNameMatchedFields_AndDropsIgnoredProperty()
    {
        using (var write = CreateContext())
        {
            write.Add(NewIncluded("inc-1", name: "Ada", email: "ada@x.io", apiKey: "sk-live-9", internalNote: "secret memo", nickname: "Countess"));
            write.SaveChanges();
        }

        var row = SingleAuditRow();
        using var doc = JsonDocument.Parse(row.Snapshot!);

        doc.RootElement.GetProperty("Email").GetString().ShouldBe(AuditFieldMasker.MaskMarker);   // [PersonalData]
        doc.RootElement.GetProperty("ApiKey").GetString().ShouldBe(AuditFieldMasker.MaskMarker);  // name-matched
        doc.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");                          // plain
        doc.RootElement.GetProperty("Nickname").GetString().ShouldBe("Countess");                 // no overlay here
        doc.RootElement.TryGetProperty("InternalNote", out _).ShouldBeFalse();                    // [SolhigsonAuditIgnore]
    }

    [Fact]
    public void Update_OfAPersonalDataField_MasksBothOldAndNewSides()
    {
        using (var seed = CreateContext())
        {
            seed.Add(NewIncluded("inc-1", name: "Ada", email: "ada@old.io"));
            seed.SaveChanges();
        }

        using (var write = CreateContext())
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            entity.Email = "ada@new.io";
            write.SaveChanges();
        }

        var row = UpdateAuditRow();
        using var doc = JsonDocument.Parse(row.Changes!);
        var change = doc.RootElement[0];

        change.GetProperty("field").GetString().ShouldBe("Email");
        change.GetProperty("old").GetString().ShouldBe(AuditFieldMasker.MaskMarker); // NOT "ada@old.io"
        change.GetProperty("new").GetString().ShouldBe(AuditFieldMasker.MaskMarker);
    }

    [Fact]
    public void Insert_AdditiveOverlay_MasksAnExtraFieldWithoutUnmaskingProtectedFields()
    {
        var options = new AuditCaptureOptions();
        options.AdditionalSensitiveNamePatterns.Add("nickname");

        using (var write = CreateContext(options: options))
        {
            write.Add(NewIncluded("inc-1", name: "Ada", email: "ada@x.io", nickname: "Countess"));
            write.SaveChanges();
        }

        var row = SingleAuditRow();
        using var doc = JsonDocument.Parse(row.Snapshot!);

        doc.RootElement.GetProperty("Nickname").GetString().ShouldBe(AuditFieldMasker.MaskMarker); // overlay masks
        doc.RootElement.GetProperty("Email").GetString().ShouldBe(AuditFieldMasker.MaskMarker);    // overlay never un-masks
        doc.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");                           // still plain
    }

    // ── (c) append-only rejection ──────────────────────────────────────────────

    [Fact]
    public void Update_OfATrackedAuditRow_ThrowsAppendOnly()
    {
        SeedRawAuditRow(out var id, out var created);

        using var write = CreateContext();
        var row = write.Set<AuditTrail>().Single(x => x.Id == id && x.Created == created);
        row.Snapshot = "tampered";

        var ex = Should.Throw<InvalidOperationException>(() => write.SaveChanges());
        ex.Message.ShouldContain("append-only");
    }

    [Fact]
    public async Task AsyncUpdate_OfATrackedAuditRow_ThrowsAppendOnly()
    {
        SeedRawAuditRow(out var id, out var created);

        using var write = CreateContext();
        var row = write.Set<AuditTrail>().Single(x => x.Id == id && x.Created == created);
        row.Snapshot = "tampered";

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => write.SaveChangesAsync());
        ex.Message.ShouldContain("append-only");
    }

    [Fact]
    public void Delete_OfATrackedAuditRow_ThrowsAppendOnly()
    {
        SeedRawAuditRow(out var id, out var created);

        using var write = CreateContext();
        var row = write.Set<AuditTrail>().Single(x => x.Id == id && x.Created == created);
        write.Remove(row);

        var ex = Should.Throw<InvalidOperationException>(() => write.SaveChanges());
        ex.Message.ShouldContain("append-only");
    }

    // ── (d) recursion exclusion ────────────────────────────────────────────────

    [Fact]
    public void CapturedInsert_ProducesExactlyOneRow_NoAuditOfAudit()
    {
        using (var write = CreateContext())
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        ReadAuditRows().Count.ShouldBe(1);
    }

    // ── (e) eligibility predicate ──────────────────────────────────────────────

    [Fact]
    public void RegistryInclude_CapturesAnUnattributedClass()
    {
        _registry.Include<RegistryIncludedEntity>();

        using (var write = CreateContext())
        {
            write.Add(new RegistryIncludedEntity { Id = "reg-1", Name = "Katherine" });
            write.SaveChanges();
        }

        var row = SingleAuditRow();
        row.EntityType.ShouldBe(nameof(RegistryIncludedEntity));
        row.EntityId.ShouldBe("reg-1");
    }

    [Fact]
    public void UnregisteredUnattributedClass_IsNotCaptured()
    {
        using (var write = CreateContext())
        {
            write.Add(new UnauditedEntity { Id = "un-1", Name = "Dorothy" });
            write.SaveChanges();
        }

        ReadAuditRows().ShouldBeEmpty();
    }

    [Fact]
    public void ClassLevelIgnoreAttribute_BeatsClassLevelIncludeAttribute()
    {
        using (var write = CreateContext())
        {
            write.Add(new IgnoredAndIncludedEntity { Id = "both-1", Name = "Radia" });
            write.SaveChanges();
        }

        ReadAuditRows().ShouldBeEmpty();
    }

    [Fact]
    public void RegistryIgnore_BeatsClassLevelIncludeAttribute()
    {
        _registry.Ignore<IncludedEntity>(); // attribute says Include, registry says Ignore → ignore wins

        using (var write = CreateContext())
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        ReadAuditRows().ShouldBeEmpty();
    }

    // ── (f) actor stamping ─────────────────────────────────────────────────────

    [Fact]
    public void CapturedRow_StampsTheResolvedActorsFiveFields()
    {
        using (var write = CreateContext())
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        var row = SingleAuditRow();
        row.ActorUserId.ShouldBe(ActorUserId);
        row.UserDisplayName.ShouldBe(ActorDisplayName);
        row.UserIp.ShouldBe(ActorIp);
        row.SourceType.ShouldBe(ActorSourceType);
        row.SourceId.ShouldBe(ActorSourceId);
    }

    [Fact]
    public void CapturedRow_UnattributedProvider_StampsTheUnattributedDefault()
    {
        using (var write = CreateContext(actor: new UnattributedAuditActorProvider()))
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        var row = SingleAuditRow();
        row.ActorUserId.ShouldBeNull();
        row.UserDisplayName.ShouldBe(AuditActor.Unattributed);
        row.SourceType.ShouldBe(AuditActor.Unattributed);
        row.UserIp.ShouldBeNull();
        row.SourceId.ShouldBeNull();
    }

    // ── infrastructure ─────────────────────────────────────────────────────────

    private TestAuditDbContext CreateContext(
        IAuditActorProvider? actor = null,
        AuditCaptureOptions? options = null)
    {
        var capture = new AuditCaptureSaveChangesInterceptor(
            actor ?? _fullActor,
            _registry,
            options ?? new AuditCaptureOptions());
        var appendOnly = new AuditTrailAppendOnlyInterceptor();

        var opt = new DbContextOptionsBuilder<TestAuditDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(capture, appendOnly)
            .Options;

        return new TestAuditDbContext(opt);
    }

    private System.Collections.Generic.List<AuditTrail> ReadAuditRows()
    {
        using var read = CreateContext();
        return read.Set<AuditTrail>().AsNoTracking().OrderBy(x => x.Created).ThenBy(x => x.Id).ToList();
    }

    private AuditTrail SingleAuditRow() => ReadAuditRows().Single();

    private AuditTrail UpdateAuditRow() => ReadAuditRows().Single(x => x.Changes != null);

    private void SeedRawAuditRow(out Guid id, out DateTime created)
    {
        var seed = new AuditTrail
        {
            Category = AuditEventCategory.DataChange,
            EntityType = "Seed",
            EntityId = "seed-1",
            Snapshot = """{"seed":true}""",
        };
        id = seed.Id;
        created = seed.Created;

        using var ctx = CreateContext();
        ctx.Add(seed); // Added is permitted by the append-only guard
        ctx.SaveChanges();
    }

    private static IncludedEntity NewIncluded(
        string id,
        string name,
        string? email = null,
        string? apiKey = null,
        string? internalNote = null,
        string? nickname = null) => new()
    {
        Id = id,
        Name = name,
        Email = email,
        ApiKey = apiKey,
        InternalNote = internalNote,
        Nickname = nickname,
    };

    public void Dispose() => _connection.Dispose();

    // ── test model ─────────────────────────────────────────────────────────────

    private sealed class StubActorProvider(AuditActor actor) : IAuditActorProvider
    {
        public AuditActor GetCurrentActor() => actor;
    }

    [SolhigsonAuditInclude]
    private sealed class IncludedEntity
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;

        [PersonalData]
        public string? Email { get; set; }

        public string? ApiKey { get; set; }

        [SolhigsonAuditIgnore]
        public string? InternalNote { get; set; }

        public string? Nickname { get; set; }
    }

    private sealed class RegistryIncludedEntity
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    [SolhigsonAuditInclude]
    [SolhigsonAuditIgnore]
    private sealed class IgnoredAndIncludedEntity
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private sealed class UnauditedEntity
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private sealed class TestAuditDbContext(DbContextOptions<TestAuditDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditTrail>(b =>
            {
                b.HasKey(x => new { x.Id, x.Created });
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Property(x => x.Created).ValueGeneratedNever();
            });

            modelBuilder.Entity<IncludedEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<RegistryIncludedEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<IgnoredAndIncludedEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<UnauditedEntity>().HasKey(x => x.Id);
        }
    }
}
