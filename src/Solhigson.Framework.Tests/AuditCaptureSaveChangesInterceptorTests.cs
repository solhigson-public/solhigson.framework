using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.Json;
using System.Threading;
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
/// F2-prime never-block capture-interceptor behaviour, over a real SQLite in-memory DB (per test-pattern rule;
/// NEVER the EF InMemory provider). Under the never-block invariant the interceptor no longer <c>Add()</c>s
/// audit rows into the business ChangeTracker: it STASHES the built rows and hands them to an out-of-band
/// <see cref="IAuditSink"/> on the outermost-transaction-committed signal (M1). These tests therefore assert on
/// what a recording sink RECEIVES (the built payload) rather than on rows persisted to the business context,
/// and directly exercise the never-block mechanics (handoff timing M1, swallow-all + accepted loss M2/M3,
/// take-and-remove-once M4). Runs in a serialized collection so the process-global <c>audit_capture_failed</c>
/// meter is observed without cross-class leakage.
/// </summary>
[Collection(AuditNeverBlockMetricsCollection.Name)]
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
        using var ctx = CreateContext(new RecordingAuditSink());
        ctx.Database.EnsureCreated();
    }

    // ── (a) capture shape — asserted on the sink's received payload ─────────────

    [Fact]
    public void Insert_OfAnIncludedEntity_HandsOffSnapshotAndNullChanges()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        var row = sink.Received.ShouldHaveSingleItem();
        row.Category.ShouldBe(AuditEventCategory.DataChange);
        row.EntityType.ShouldBe(nameof(IncludedEntity));
        row.EntityId.ShouldBe("inc-1");
        row.Changes.ShouldBeNull();
        row.Snapshot.ShouldNotBeNull();

        using var doc = JsonDocument.Parse(row.Snapshot!);
        doc.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");
    }

    [Fact]
    public async Task AsyncInsert_OfAnIncludedEntity_HandsOffSnapshotAndNullChanges()
    {
        var sink = new RecordingAuditSink();
        await using (var write = CreateContext(sink))
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            await write.SaveChangesAsync();
        }

        var row = sink.Received.ShouldHaveSingleItem();
        row.EntityId.ShouldBe("inc-1");
        row.Changes.ShouldBeNull();
        using var doc = JsonDocument.Parse(row.Snapshot!);
        doc.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");
    }

    [Fact]
    public void Update_OfAnIncludedEntity_HandsOffChangesArrayOfModifiedFieldsOnly()
    {
        Seed("inc-1", "Ada");

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            entity.Name = "Grace"; // Email/ApiKey/Nickname/InternalNote unchanged
            write.SaveChanges();
        }

        var row = sink.Received.ShouldHaveSingleItem();
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
    public void Delete_OfAnIncludedEntity_HandsOffSnapshotAndNullChanges()
    {
        Seed("inc-1", "Ada");

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            write.Remove(entity);
            write.SaveChanges();
        }

        var row = sink.Received.ShouldHaveSingleItem();
        row.Changes.ShouldBeNull();
        row.Snapshot.ShouldNotBeNull();
        using var doc = JsonDocument.Parse(row.Snapshot!);
        doc.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");
    }

    // ── (b) masking fail-closed, both sides (unchanged behaviour) ───────────────

    [Fact]
    public void Insert_MasksPersonalDataAndNameMatchedFields_AndDropsIgnoredProperty()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(NewIncluded("inc-1", name: "Ada", email: "ada@x.io", apiKey: "sk-live-9", internalNote: "secret memo", nickname: "Countess"));
            write.SaveChanges();
        }

        using var doc = JsonDocument.Parse(sink.Received.ShouldHaveSingleItem().Snapshot!);
        doc.RootElement.GetProperty("Email").GetString().ShouldBe(AuditFieldMasker.MaskMarker);   // [PersonalData]
        doc.RootElement.GetProperty("ApiKey").GetString().ShouldBe(AuditFieldMasker.MaskMarker);  // name-matched
        doc.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");                          // plain
        doc.RootElement.GetProperty("Nickname").GetString().ShouldBe("Countess");                 // no overlay here
        doc.RootElement.TryGetProperty("InternalNote", out _).ShouldBeFalse();                    // [SolhigsonAuditIgnore]
    }

    [Fact]
    public void Update_OfAPersonalDataField_MasksBothOldAndNewSides()
    {
        Seed("inc-1", "Ada", email: "ada@old.io");

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            entity.Email = "ada@new.io";
            write.SaveChanges();
        }

        using var doc = JsonDocument.Parse(sink.Received.Single(r => r.Changes != null).Changes!);
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

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink, options: options))
        {
            write.Add(NewIncluded("inc-1", name: "Ada", email: "ada@x.io", nickname: "Countess"));
            write.SaveChanges();
        }

        using var doc = JsonDocument.Parse(sink.Received.ShouldHaveSingleItem().Snapshot!);
        doc.RootElement.GetProperty("Nickname").GetString().ShouldBe(AuditFieldMasker.MaskMarker); // overlay masks
        doc.RootElement.GetProperty("Email").GetString().ShouldBe(AuditFieldMasker.MaskMarker);    // overlay never un-masks
        doc.RootElement.GetProperty("Name").GetString().ShouldBe("Ada");                           // still plain
    }

    // ── (c) append-only rejection (guard unchanged; asserts on the DB) ──────────

    [Fact]
    public void Update_OfATrackedAuditRow_ThrowsAppendOnly()
    {
        SeedRawAuditRow(out var id, out var created);

        using var write = CreateContext(new RecordingAuditSink());
        var row = write.Set<AuditTrail>().Single(x => x.Id == id && x.Created == created);
        row.Snapshot = "tampered";

        var ex = Should.Throw<InvalidOperationException>(() => write.SaveChanges());
        ex.Message.ShouldContain("append-only");
    }

    [Fact]
    public async Task AsyncUpdate_OfATrackedAuditRow_ThrowsAppendOnly()
    {
        SeedRawAuditRow(out var id, out var created);

        await using var write = CreateContext(new RecordingAuditSink());
        var row = write.Set<AuditTrail>().Single(x => x.Id == id && x.Created == created);
        row.Snapshot = "tampered";

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => write.SaveChangesAsync());
        ex.Message.ShouldContain("append-only");
    }

    [Fact]
    public void Delete_OfATrackedAuditRow_ThrowsAppendOnly()
    {
        SeedRawAuditRow(out var id, out var created);

        using var write = CreateContext(new RecordingAuditSink());
        var row = write.Set<AuditTrail>().Single(x => x.Id == id && x.Created == created);
        write.Remove(row);

        var ex = Should.Throw<InvalidOperationException>(() => write.SaveChanges());
        ex.Message.ShouldContain("append-only");
    }

    // ── (d) recursion exclusion ─────────────────────────────────────────────────

    [Fact]
    public void CapturedInsert_HandsOffExactlyOneRow_NoAuditOfAudit()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        sink.Received.Count.ShouldBe(1);
        sink.Received.ShouldNotContain(r => r.EntityType == nameof(AuditTrail));
    }

    // ── (e) eligibility predicate ──────────────────────────────────────────────

    [Fact]
    public void RegistryInclude_CapturesAnUnattributedClass()
    {
        _registry.Include<RegistryIncludedEntity>();

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(new RegistryIncludedEntity { Id = "reg-1", Name = "Katherine" });
            write.SaveChanges();
        }

        var row = sink.Received.ShouldHaveSingleItem();
        row.EntityType.ShouldBe(nameof(RegistryIncludedEntity));
        row.EntityId.ShouldBe("reg-1");
    }

    [Fact]
    public void UnregisteredUnattributedClass_IsNotCaptured()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(new UnauditedEntity { Id = "un-1", Name = "Dorothy" });
            write.SaveChanges();
        }

        sink.Received.ShouldBeEmpty();
    }

    [Fact]
    public void ClassLevelIgnoreAttribute_BeatsClassLevelIncludeAttribute()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(new IgnoredAndIncludedEntity { Id = "both-1", Name = "Radia" });
            write.SaveChanges();
        }

        sink.Received.ShouldBeEmpty();
    }

    [Fact]
    public void RegistryIgnore_BeatsClassLevelIncludeAttribute()
    {
        _registry.Ignore<IncludedEntity>(); // attribute says Include, registry says Ignore → ignore wins

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        sink.Received.ShouldBeEmpty();
    }

    // ── (f) actor stamping ─────────────────────────────────────────────────────

    [Fact]
    public void CapturedRow_StampsTheResolvedActorsFiveFields()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        var row = sink.Received.ShouldHaveSingleItem();
        row.ActorUserId.ShouldBe(ActorUserId);
        row.UserDisplayName.ShouldBe(ActorDisplayName);
        row.UserIp.ShouldBe(ActorIp);
        row.SourceType.ShouldBe(ActorSourceType);
        row.SourceId.ShouldBe(ActorSourceId);
    }

    [Fact]
    public void CapturedRow_UnattributedProvider_StampsTheUnattributedDefault()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink, actor: new UnattributedAuditActorProvider()))
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        var row = sink.Received.ShouldHaveSingleItem();
        row.ActorUserId.ShouldBeNull();
        row.UserDisplayName.ShouldBe(AuditActor.Unattributed);
        row.SourceType.ShouldBe(AuditActor.Unattributed);
        row.UserIp.ShouldBeNull();
        row.SourceId.ShouldBeNull();
    }

    // ── (g) never-block: no business-context mutation ──────────────────────────

    [Fact]
    public void Capture_StashesAndHandsOff_ButAddsNothingToTheBusinessChangeTracker()
    {
        var sink = new RecordingAuditSink();
        using var write = CreateContext(sink);
        write.Add(NewIncluded("inc-1", name: "Ada"));
        write.SaveChanges();

        // The interceptor never Add()s an audit row into the business context (audit persistence has left
        // the business transaction); the payload was handed off out-of-band instead.
        write.ChangeTracker.Entries<AuditTrail>().ShouldBeEmpty();
        sink.Received.Count.ShouldBe(1);
        AuditCaptureSaveChangesInterceptor.HasPendingCapture(write).ShouldBeFalse(); // taken-and-removed on success (M4)
    }

    // ── (h) never-block: handoff fires ONLY after the outermost commit (M1) ────

    [Fact]
    public async Task ExplicitTransaction_DoesNotHandOffAtSavedChanges_ButFiresAfterCommit()
    {
        var sink = new RecordingAuditSink();
        await using var write = CreateContext(sink);
        await using var tx = await write.Database.BeginTransactionAsync();

        write.Add(NewIncluded("inc-1", name: "Ada"));
        await write.SaveChangesAsync();

        // SavedChanges fired with the explicit transaction STILL OPEN — no handoff yet, rows accumulating.
        sink.CallCount.ShouldBe(0);
        AuditCaptureSaveChangesInterceptor.HasPendingCapture(write).ShouldBeTrue();

        await tx.CommitAsync();

        // The handoff fires on the outermost-transaction-committed signal.
        sink.CallCount.ShouldBe(1);
        sink.Received.Count.ShouldBe(1);
        AuditCaptureSaveChangesInterceptor.HasPendingCapture(write).ShouldBeFalse(); // taken-and-removed (M4)
    }

    [Fact]
    public async Task MultipleSavesInOneTransaction_AccumulateAndHandOffOnceAtCommit()
    {
        var sink = new RecordingAuditSink();
        await using var write = CreateContext(sink);
        await using var tx = await write.Database.BeginTransactionAsync();

        write.Add(NewIncluded("inc-1", name: "Ada"));
        await write.SaveChangesAsync();
        write.Add(NewIncluded("inc-2", name: "Grace"));
        await write.SaveChangesAsync();

        sink.CallCount.ShouldBe(0);
        await tx.CommitAsync();

        sink.CallCount.ShouldBe(1);
        // Ordered accumulation: rows seal into the accumulator in save order and hand off once at commit.
        sink.Received.Select(r => r.EntityId).ShouldBe(["inc-1", "inc-2"]);
    }

    // ── (i) never-block: rolled-back / abandoned transaction discards (M1/M4) ───

    [Fact]
    public async Task RolledBackTransaction_DiscardsThePayload_NoHandoff()
    {
        var sink = new RecordingAuditSink();
        await using (var write = CreateContext(sink))
        {
            await using var tx = await write.Database.BeginTransactionAsync();
            write.Add(NewIncluded("inc-1", name: "Ada"));
            await write.SaveChangesAsync();
            await tx.RollbackAsync();

            sink.CallCount.ShouldBe(0); // never handed off
            AuditCaptureSaveChangesInterceptor.HasPendingCapture(write).ShouldBeFalse(); // taken-and-removed (M4)
        }
    }

    // ── (j) never-block: capture-phase throw swallowed, source save commits ────

    [Fact]
    public void InjectedCapturePhaseThrow_IsSwallowed_EmitsMetric_AndSourceSaveStillCommits()
    {
        var sink = new RecordingAuditSink();
        var failures = CountCaptureFailed(() =>
        {
            using var write = CreateContext(sink, actor: new ThrowingActorProvider());
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges(); // MUST NOT throw — never-block
        });

        failures.ShouldBe(1);                 // audit_capture_failed emitted once
        sink.Received.ShouldBeEmpty();         // nothing was handed off (guaranteed single-event loss, M3)
        IncludedExists("inc-1").ShouldBeTrue(); // the business write still committed
    }

    // ── (k) retry-execution-strategy safety of the CWT stash (F1) ──────────────
    // A RETRYING IExecutionStrategy re-runs SaveChanges on a transient failure. The once-per-top-level-save
    // interceptor events (SavingChanges / SavedChanges / SaveChangesFailed) fire above the retry loop, while
    // EF's implicit per-attempt transaction may begin and end once PER ATTEMPT. The interceptor must be correct
    // under EITHER pairing (SavingChanges re-fires or not; a failed attempt raises TransactionRolledBack or a
    // silent dispose): no silent audit loss on retry-rollback-then-success, no duplicate rows on re-fire. These
    // drive the interceptor's own callbacks (the F1 simulation seams) to reproduce the sequence deterministically
    // WITHOUT a real transient failure, which SQLite cannot raise.

    [Fact]
    public void Retry_AttemptRollbackThenReCaptureThenSuccess_HandsOffExactlyOneRow_NoLossNoDuplicate()
    {
        var sink = new RecordingAuditSink();
        var (ctx, interceptor) = CreateContextAndInterceptor(sink);
        using (ctx)
        {
            ctx.Add(NewIncluded("inc-1", name: "Ada"));

            interceptor.SimulateSavingChanges(ctx);                         // attempt 1 captures the in-flight save
            interceptor.SimulateTransactionRolledBack(ctx, Guid.NewGuid()); // attempt 1's implicit tx rolls back — NOT the deferred tx

            // The stash SURVIVES a per-attempt implicit-transaction rollback: no silent loss.
            AuditCaptureSaveChangesInterceptor.HasPendingCapture(ctx).ShouldBeTrue();

            interceptor.SimulateSavingChanges(ctx);                         // attempt 2 re-captures (SavingChanges re-fire)
            interceptor.SimulateSavedChanges(ctx);                          // attempt 2 succeeds (no open tx) → dispatch
        }

        sink.Received.Count.ShouldBe(1);         // EXACTLY one — the rollback lost nothing, the re-fire duplicated nothing
        sink.Received[0].EntityId.ShouldBe("inc-1");
    }

    [Fact]
    public void Retry_SavingChangesReEnteredBeforeDispatch_DoesNotDuplicate()
    {
        var sink = new RecordingAuditSink();
        var (ctx, interceptor) = CreateContextAndInterceptor(sink);
        using (ctx)
        {
            ctx.Add(NewIncluded("inc-1", name: "Ada"));

            interceptor.SimulateSavingChanges(ctx); // capture
            interceptor.SimulateSavingChanges(ctx); // re-fire before any dispatch — REPLACE, not append
            interceptor.SimulateSavingChanges(ctx); // and again
            interceptor.SimulateSavedChanges(ctx);  // dispatch
        }

        sink.Received.Count.ShouldBe(1); // one logical save → one row despite three SavingChanges of the same save
        sink.Received[0].EntityId.ShouldBe("inc-1");
    }

    [Fact]
    public void Retry_NonDeferredRollbackDuringExplicitTransaction_StillAccumulatesAndDispatchesOnceAtCommit()
    {
        var sink = new RecordingAuditSink();
        var (ctx, interceptor) = CreateContextAndInterceptor(sink);
        using (ctx)
        using (var tx = ctx.Database.BeginTransaction()) // a real explicit tx with a real TransactionId
        {
            ctx.Add(NewIncluded("inc-1", name: "Ada"));

            interceptor.SimulateSavingChanges(ctx);                         // save 1 captures
            interceptor.SimulateTransactionRolledBack(ctx, Guid.NewGuid()); // a per-attempt (non-deferred) rollback — ignored
            interceptor.SimulateSavedChanges(ctx);                          // save 1 succeeds, explicit tx open → seal + defer

            sink.CallCount.ShouldBe(0);                                     // deferred, not dispatched while the tx is open
            AuditCaptureSaveChangesInterceptor.HasPendingCapture(ctx).ShouldBeTrue();

            interceptor.SimulateTransactionCommitted(ctx, tx.TransactionId); // the DEFERRED tx commits → dispatch
        }

        sink.CallCount.ShouldBe(1);                     // handed off once, at the deferred commit
        sink.Received.Select(r => r.EntityId).ShouldBe(["inc-1"]);
    }

    // ── (l) FF1 real-EF: abandoned explicit transaction on a reused (pooled) context ──
    // The ubiquitous `using var tx = ctx.Database.BeginTransaction(); …; tx.Commit();` idiom, where the body
    // THROWS before Commit, disposes the transaction with NO explicit Commit and NO Rollback. Against a real
    // EF Core transaction, EF raises NEITHER TransactionRolledBack NOR a handled dispose callback for that
    // path, so a naive interceptor LEAKS the sealed rows + deferral in the weak table; because a pooled
    // DbContext reuses the SAME instance across leases, the next save on that instance flushes them → a phantom
    // audit row misattributed into the next save's handoff. Unlike the (k) simulation-seam tests, this drives
    // the REAL transaction lifecycle and so reproduces FF1: it FAILS pre-fix (handoff = [inc-1, inc-2]) and
    // PASSES after the stale-deferred reconciliation (handoff = [inc-2] only).

    [Fact]
    public async Task AbandonedExplicitTransaction_ThenReusedContext_HandsOffOnlyTheSecondSave_NoPhantom()
    {
        var sink = new RecordingAuditSink();
        await using var write = CreateContext(sink);

        // Save inside an explicit transaction, then DISPOSE it without Commit/Rollback (throw-before-Commit).
        var tx = await write.Database.BeginTransactionAsync();
        write.Add(NewIncluded("inc-1", name: "Ada"));
        await write.SaveChangesAsync();
        await tx.DisposeAsync(); // abandoned: no Commit, no Rollback — EF delivers no handled end callback

        sink.CallCount.ShouldBe(0);                                                 // the abandoned action emitted nothing (correct loss)
        AuditCaptureSaveChangesInterceptor.HasPendingCapture(write).ShouldBeTrue(); // leaked deferral still stashed

        // Reuse the SAME context instance (pooled-context reuse) for a second, no-transaction save.
        write.Add(NewIncluded("inc-2", name: "Grace"));
        await write.SaveChangesAsync();

        // ONLY the second row — the abandoned inc-1 is reconciled away, never phantom-flushed into this handoff.
        sink.Received.Select(r => r.EntityId).ShouldBe(["inc-2"]);
    }

    // ── (m) FF3 savepoint over-capture — DOCUMENTED current limitation ──────────
    // RollbackToSavepoint raises RolledBackToSavepoint, which this interceptor does NOT implement, so a row
    // already sealed for an audited write that an inner savepoint rollback later undoes still flushes at the
    // outer commit — an OVER-capture (an audit row for work that never persisted). This is currently
    // UNREACHABLE for audited writes in the consumer: the only savepoint users (Elfrique's
    // ListingProjectionInterceptor and ConfigService) wrap a savepoint around ONLY non-audited projection /
    // config statements written AFTER the audited source rows, so a rollback-to-savepoint there never undoes an
    // audited entity's own write. This test LOCKS the documented over-capture so a future RolledBackToSavepoint
    // handler visibly flips it and forces the class XML-doc note to be revisited.

    [Fact]
    public void SavepointRollbackOfAnAuditedWrite_CurrentlyOverCaptures_DocumentedLimitation()
    {
        var sink = new RecordingAuditSink();
        using var write = CreateContext(sink);
        using (var tx = write.Database.BeginTransaction())
        {
            write.Add(NewIncluded("kept-1", name: "Ada"));
            write.SaveChanges();          // audited row sealed, deferred to the outer commit

            tx.CreateSavepoint("sp");
            write.Add(NewIncluded("undone-1", name: "Grace"));
            write.SaveChanges();          // audited row for undone-1 sealed
            tx.RollbackToSavepoint("sp"); // undone-1's DB write erased — its already-sealed audit row is NOT

            tx.Commit();
        }

        // OVER-capture (documented limitation): BOTH rows flush, though only kept-1 actually persisted.
        sink.Received.Select(r => r.EntityId).OrderBy(x => x).ShouldBe(["kept-1", "undone-1"]);
        IncludedExists("kept-1").ShouldBeTrue();
        IncludedExists("undone-1").ShouldBeFalse(); // proves undone-1 was rolled back yet still over-captured
    }

    // ── (n) changed-only UPDATE delta (raw-value equality, PRE-mask) ────────────
    // A full-entity Modified save — every non-key property IsModified, the shape UserManager.UpdateAsync
    // produces, reproduced here via Entry(e).State = EntityState.Modified — must hand off ONLY the
    // genuinely-changed fields, compared on the RAW pre-mask values (a post-mask comparison would read
    // "***" == "***" and drop a genuinely-changed sensitive field). Zero genuine changes ⇒ NO audit row
    // (a no-op save is not an auditable change), extending the existing Modified no-delta no-row path.

    [Fact]
    public void FullEntityUpdate_WithTwoGenuineChangesOfN_HandsOffExactlyThoseTwoFieldDeltas()
    {
        Seed("inc-1", "Ada"); // Email/ApiKey/Nickname null, LoginCount 0

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            entity.Name = "Grace";
            entity.Nickname = "Countess"; // null → value transition MUST emit
            write.Entry(entity).State = EntityState.Modified; // marks EVERY non-key property IsModified
            write.SaveChanges();
        }

        var row = sink.Received.ShouldHaveSingleItem();
        using var doc = JsonDocument.Parse(row.Changes!);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().ShouldBe(2); // unchanged Email/ApiKey (null==null) and LoginCount (0==0) dropped

        var byField = doc.RootElement.EnumerateArray().ToDictionary(c => c.GetProperty("field").GetString()!);
        byField["Name"].GetProperty("old").GetString().ShouldBe("Ada");
        byField["Name"].GetProperty("new").GetString().ShouldBe("Grace");
        byField["Nickname"].GetProperty("old").ValueKind.ShouldBe(JsonValueKind.Null);
        byField["Nickname"].GetProperty("new").GetString().ShouldBe("Countess");
    }

    [Fact]
    public void FullEntityUpdate_WithNoGenuineChanges_HandsOffNoAuditRow()
    {
        Seed("inc-1", "Ada", email: "ada@x.io", loginCount: 7); // nonzero int exercises boxed 7 == 7

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            write.Entry(entity).State = EntityState.Modified; // all IsModified, zero value changes
            write.SaveChanges(); // the business save itself still succeeds
        }

        sink.CallCount.ShouldBe(0); // NO row — not an empty [] payload
        sink.Received.ShouldBeEmpty();
    }

    [Fact]
    public void FullEntityUpdate_OfAMaskedField_EmitsItMaskedAndDropsTheUnchangedMaskedSibling()
    {
        Seed("inc-1", "Ada", email: "ada@old.io", apiKey: "sk-live-1");

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            entity.Email = "ada@new.io"; // genuinely-changed [PersonalData] field
            write.Entry(entity).State = EntityState.Modified; // ApiKey also IsModified, value unchanged
            write.SaveChanges();
        }

        using var doc = JsonDocument.Parse(sink.Received.ShouldHaveSingleItem().Changes!);
        var change = doc.RootElement.EnumerateArray().ShouldHaveSingleItem(); // ApiKey dropped ⇒ equality ran on RAW values
        change.GetProperty("field").GetString().ShouldBe("Email");
        change.GetProperty("old").GetString().ShouldBe(AuditFieldMasker.MaskMarker); // still masked, NOT "ada@old.io"
        change.GetProperty("new").GetString().ShouldBe(AuditFieldMasker.MaskMarker);
    }

    [Fact]
    public void FullEntityUpdate_OfAValueTypeProperty_EmitsItsDeltaUnderBoxedValueEquality()
    {
        Seed("inc-1", "Ada", loginCount: 3);

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            entity.LoginCount = 4; // boxed int comparison must use VALUE semantics, not reference equality
            write.Entry(entity).State = EntityState.Modified;
            write.SaveChanges();
        }

        using var doc = JsonDocument.Parse(sink.Received.ShouldHaveSingleItem().Changes!);
        var change = doc.RootElement.EnumerateArray().ShouldHaveSingleItem(); // unchanged string siblings dropped too
        change.GetProperty("field").GetString().ShouldBe("LoginCount");
        change.GetProperty("old").GetInt32().ShouldBe(3);
        change.GetProperty("new").GetInt32().ShouldBe(4);
    }

    // ── (o) action + subject display-name stamping ─────────────────────────────
    // Action disambiguates INSERT from DELETE (both ride Snapshot): Added → "created",
    // Modified-with-genuine-delta → "updated", Deleted → "deleted" — pinned as LITERAL lowercase strings
    // (the persisted wire contract that list surfaces filter on), deliberately not via the AuditActions
    // constants, so a constant-value drift breaks these tests. SubjectDisplayName binds via
    // IAuditSubjectNamed TYPE MEMBERSHIP (an `is` check on the tracked instance), so a base-class
    // implementation binds where an inherit:false attribute probe would not; a non-implementing entity
    // stamps null; an overlong name truncates to the declared 256 column width instead of faulting the
    // consumer's audit INSERT; a THROWING consumer getter rides the capture phase's never-block swallow
    // boundary like any consumer-seam failure (the (j) ThrowingActorProvider analogue).

    [Fact]
    public void Insert_OfAnIncludedEntity_StampsActionCreated()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        sink.Received.ShouldHaveSingleItem().Action.ShouldBe("created");
    }

    [Fact]
    public void Update_WithAGenuineChange_StampsActionUpdated()
    {
        Seed("inc-1", "Ada");

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            entity.Name = "Grace";
            write.SaveChanges();
        }

        sink.Received.ShouldHaveSingleItem().Action.ShouldBe("updated");
    }

    [Fact]
    public void Delete_OfAnIncludedEntity_StampsActionDeleted()
    {
        Seed("inc-1", "Ada");

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            write.Remove(entity);
            write.SaveChanges();
        }

        sink.Received.ShouldHaveSingleItem().Action.ShouldBe("deleted");
    }

    [Fact]
    public void Insert_OfAnEntityImplementingIAuditSubjectNamed_StampsSubjectDisplayName()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(new NamedEntity { Id = "named-1", DisplayName = "Ada Lovelace" });
            write.SaveChanges();
        }

        sink.Received.ShouldHaveSingleItem().SubjectDisplayName.ShouldBe("Ada Lovelace");
    }

    [Fact]
    public void Insert_OfAnEntityWhoseBaseClassImplementsIAuditSubjectNamed_StampsFromTheBaseImplementation()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(new DerivedNamedEntity { Id = "derived-1", DisplayName = "Grace Hopper" });
            write.SaveChanges();
        }

        // Type membership binds through the base class — an inherit:false attribute probe would miss this.
        sink.Received.ShouldHaveSingleItem().SubjectDisplayName.ShouldBe("Grace Hopper");
    }

    [Fact]
    public void Insert_OfANonImplementingEntity_StampsNullSubjectDisplayName()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        sink.Received.ShouldHaveSingleItem().SubjectDisplayName.ShouldBeNull();
    }

    [Fact]
    public void Insert_WithAnOverlongSubjectDisplayName_TruncatesTo256Characters()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink))
        {
            write.Add(new NamedEntity { Id = "named-1", DisplayName = new string('x', 300) });
            write.SaveChanges();
        }

        // Truncated app-side instead of faulting the consumer's varchar(256) audit INSERT (row loss).
        sink.Received.ShouldHaveSingleItem().SubjectDisplayName.ShouldBe(new string('x', 256));
    }

    [Fact]
    public void Insert_WhenTheSubjectNameGetterThrows_SwallowsUnderNeverBlock_AndTheBusinessSaveStillCommits()
    {
        var sink = new RecordingAuditSink();
        var failures = CountCaptureFailed(() =>
        {
            using var write = CreateContext(sink);
            write.Add(new ThrowingNamedEntity { Id = "throw-1" });
            write.SaveChanges(); // MUST NOT throw — the consumer getter bug rides the M2 boundary
        });

        failures.ShouldBe(1);          // audit_capture_failed emitted once (visible, not silently nulled)
        sink.Received.ShouldBeEmpty(); // no row for the failed capture (guaranteed single-event loss, M3)
        ThrowingNamedExists("throw-1").ShouldBeTrue(); // the business write still committed
    }

    // ── (p) attribution gate (RequireAttributedActor) ──────────────────────────
    // Opt-in capture gate: with RequireAttributedActor on, a save whose resolved actor carries no user
    // identity (null/whitespace ActorUserId — background jobs, startup, anonymous requests) materializes
    // NO DataChange rows, decided ONCE before the per-entry loop so a single skip covers BOTH payload
    // shapes (Snapshot AND Changes) and no downstream consumer seam runs. The predicate keys on
    // ActorUserId — user identity IS the attribution fact; SourceType is transport (UnattributedActor
    // carries a NON-null SourceType yet MUST gate, so these tests fail under any SourceType-keyed
    // variant). A legitimate skip is not a failure: no audit_capture_failed metric. Default false:
    // capture is unchanged unless a consumer opts in (the (f) unattributed test above pins the
    // option-off behaviour end-to-end). The explicit LogAsync path is NEVER gated — pinned in
    // AuditTrailServiceTests.

    [Fact]
    public void RequireAttributedActor_DefaultsToFalse_SoConsumersKeepCurrentCaptureBehaviour()
    {
        new AuditCaptureOptions().RequireAttributedActor.ShouldBeFalse();
    }

    [Fact]
    public void GateOn_UnattributedActorInsert_HandsOffNoRow_NoFailureMetric_AndTheBusinessSaveStillCommits()
    {
        var sink = new RecordingAuditSink();
        var failures = CountCaptureFailed(() =>
        {
            using var write = CreateContext(sink,
                actor: new UnattributedAuditActorProvider(),
                options: new AuditCaptureOptions { RequireAttributedActor = true });
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        });

        sink.CallCount.ShouldBe(0);             // nothing handed off — the Snapshot shape is gated
        sink.Received.ShouldBeEmpty();
        failures.ShouldBe(0);                   // a legitimate skip is NOT a failure (no audit_capture_failed)
        IncludedExists("inc-1").ShouldBeTrue(); // the gate skips the AUDIT, never the business save
    }

    [Fact]
    public void GateOn_UnattributedActorUpdate_HandsOffNoRow_TheSingleGateCoversTheChangesShapeToo()
    {
        Seed("inc-1", "Ada");

        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink,
            actor: new UnattributedAuditActorProvider(),
            options: new AuditCaptureOptions { RequireAttributedActor = true }))
        {
            var entity = write.Set<IncludedEntity>().Single(x => x.Id == "inc-1");
            entity.Name = "Grace"; // a genuine delta that WOULD emit a Changes row un-gated
            write.SaveChanges();
        }

        sink.Received.ShouldBeEmpty(); // gated before materialization — Changes shape covered by the same check
    }

    [Fact]
    public void GateOn_AttributedActor_StillHandsOffTheRow()
    {
        var sink = new RecordingAuditSink();
        using (var write = CreateContext(sink,
            options: new AuditCaptureOptions { RequireAttributedActor = true }))
        {
            write.Add(NewIncluded("inc-1", name: "Ada"));
            write.SaveChanges();
        }

        // _fullActor resolves a non-null ActorUserId, so the gate passes the save through untouched.
        var row = sink.Received.ShouldHaveSingleItem();
        row.ActorUserId.ShouldBe(ActorUserId);
        row.EntityId.ShouldBe("inc-1");
    }

    // ── infrastructure ─────────────────────────────────────────────────────────

    private TestAuditDbContext CreateContext(
        IAuditSink sink,
        IAuditActorProvider? actor = null,
        AuditCaptureOptions? options = null)
        => CreateContextAndInterceptor(sink, actor, options).Context;

    /// <summary>
    /// Builds a context whose capture interceptor is ALSO returned, so a retry-safety test can drive the
    /// interceptor's callbacks directly (the F1 simulation seams) rather than inject a real transient failure.
    /// </summary>
    private (TestAuditDbContext Context, AuditCaptureSaveChangesInterceptor Interceptor) CreateContextAndInterceptor(
        IAuditSink sink,
        IAuditActorProvider? actor = null,
        AuditCaptureOptions? options = null)
    {
        var capture = new AuditCaptureSaveChangesInterceptor(
            actor ?? _fullActor,
            _registry,
            options ?? new AuditCaptureOptions(),
            sink);
        var appendOnly = new AuditTrailAppendOnlyInterceptor();

        var opt = new DbContextOptionsBuilder<TestAuditDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(capture, appendOnly)
            .Options;

        return (new TestAuditDbContext(opt), capture);
    }

    private void Seed(string id, string name, string? email = null, string? apiKey = null, int loginCount = 0)
    {
        using var seed = CreateContext(new RecordingAuditSink());
        seed.Add(NewIncluded(id, name, email: email, apiKey: apiKey, loginCount: loginCount));
        seed.SaveChanges();
    }

    private bool IncludedExists(string id)
    {
        using var read = CreateContext(new RecordingAuditSink());
        return read.Set<IncludedEntity>().AsNoTracking().Any(x => x.Id == id);
    }

    private bool ThrowingNamedExists(string id)
    {
        using var read = CreateContext(new RecordingAuditSink());
        return read.Set<ThrowingNamedEntity>().AsNoTracking().Any(x => x.Id == id);
    }

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

        using var ctx = CreateContext(new RecordingAuditSink());
        ctx.Add(seed); // Added is permitted by the append-only guard, and AuditTrail is recursion-excluded from capture
        ctx.SaveChanges();
    }

    /// <summary>
    /// Observes the process-global <c>audit_capture_failed</c> counter across a single synchronous action.
    /// The enclosing test class runs in a non-parallel collection, so no other class emits the counter
    /// concurrently (determinism seam).
    /// </summary>
    private static long CountCaptureFailed(Action action)
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
        action();
        listener.Dispose();
        return Interlocked.Read(ref total);
    }

    private static IncludedEntity NewIncluded(
        string id,
        string name,
        string? email = null,
        string? apiKey = null,
        string? internalNote = null,
        string? nickname = null,
        int loginCount = 0) => new()
    {
        Id = id,
        Name = name,
        Email = email,
        ApiKey = apiKey,
        InternalNote = internalNote,
        Nickname = nickname,
        LoginCount = loginCount,
    };

    public void Dispose() => _connection.Dispose();

    // ── test doubles ─────────────────────────────────────────────────────────

    private sealed class StubActorProvider(AuditActor actor) : IAuditActorProvider
    {
        public AuditActor GetCurrentActor() => actor;
    }

    /// <summary>Injects a capture-phase throw at actor resolution (masking / delta enumeration analogue).</summary>
    private sealed class ThrowingActorProvider : IAuditActorProvider
    {
        public AuditActor GetCurrentActor() => throw new InvalidOperationException("actor resolution blew up");
    }

    /// <summary>Records the rows handed to the out-of-band sink (the never-block persistence seam).</summary>
    private sealed class RecordingAuditSink : IAuditSink
    {
        public List<AuditTrail> Received { get; } = [];

        public int CallCount { get; private set; }

        public Task PersistAsync(IReadOnlyList<AuditTrail> rows, CancellationToken cancellationToken)
        {
            CallCount++;
            Received.AddRange(rows); // synchronous — deterministic for the sync fire-and-forget hook
            return Task.CompletedTask;
        }
    }

    // ── test model ─────────────────────────────────────────────────────────────

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

        /// <summary>Non-string value type: pins boxed VALUE-equality (not reference) in the changed-only delta.</summary>
        public int LoginCount { get; set; }
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

    /// <summary>Direct <see cref="IAuditSubjectNamed"/> implementor (subject-name stamping).</summary>
    [SolhigsonAuditInclude]
    private sealed class NamedEntity : IAuditSubjectNamed
    {
        public string Id { get; set; } = null!;
        public string? DisplayName { get; set; }

        /// <summary>Get-only, no backing field ⇒ EF never maps it; reads the mapped column.</summary>
        public string? AuditSubjectDisplayName => DisplayName;
    }

    /// <summary>Base-class implementation: type membership must bind through inheritance.</summary>
    private abstract class NamedEntityBase : IAuditSubjectNamed
    {
        public string? DisplayName { get; set; }

        public string? AuditSubjectDisplayName => DisplayName;
    }

    [SolhigsonAuditInclude]
    private sealed class DerivedNamedEntity : NamedEntityBase
    {
        public string Id { get; set; } = null!;
    }

    /// <summary>A consumer bug: the subject-name getter throws (the ThrowingActorProvider analogue).</summary>
    [SolhigsonAuditInclude]
    private sealed class ThrowingNamedEntity : IAuditSubjectNamed
    {
        public string Id { get; set; } = null!;

        public string? AuditSubjectDisplayName => throw new InvalidOperationException("subject name getter blew up");
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
            modelBuilder.Entity<NamedEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<DerivedNamedEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<ThrowingNamedEntity>().HasKey(x => x.Id);
        }
    }
}
