using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Solhigson.Framework.Data.Attributes;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Diagnostics surface for the never-block audit-capture path (§2 M2/M3). Exposes the
/// <c>audit_capture_failed</c> counter so a consumer's OpenTelemetry metrics pipeline can subscribe to
/// <see cref="MeterName"/> and alert on swallowed audit losses. The framework only EMITS the metric; the
/// consumer's observability stack subscribes and evaluates alert thresholds.
/// </summary>
public static class AuditCaptureDiagnostics
{
    /// <summary>The meter name a consumer adds to its OpenTelemetry metrics pipeline (e.g. <c>AddMeter</c>).</summary>
    public const string MeterName = "Solhigson.Framework.AuditCapture";

    private static readonly Meter Meter = new(MeterName);

    /// <summary>
    /// Incremented once per swallowed audit-capture or handoff failure — the concrete signal of the
    /// guaranteed-single-event accepted-loss mode (§2 M3). Tagged by <c>reason</c>
    /// (<c>capture-build</c> | <c>handoff</c> | <c>explicit-log</c>) for on-call diagnosis.
    /// </summary>
    public static readonly Counter<long> CaptureFailed = Meter.CreateCounter<long>(
        "audit_capture_failed",
        unit: "events",
        description: "Audit capture-phase / handoff / explicit-log failures swallowed under the never-block "
                     + "invariant (guaranteed single-event accepted loss — §2 M3).");
}

/// <summary>
/// Never-block audit-capture interceptor (F2-prime, §2 NEVER-BLOCK INVARIANT). At <c>SavingChanges[Async]</c>
/// it reads the business change delta (the pre-save hook is required to see original values), builds an
/// <see cref="AuditTrail"/> row per capture-eligible entry, and STASHES the built rows in per-save state —
/// it MUST NOT <c>Add()</c> anything to the business <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker"/>
/// (audit PERSISTENCE has left the business transaction). Once the TRUE outermost business transaction COMMITS
/// it hands the stashed rows to the out-of-band <see cref="IAuditSink"/>; a rolled-back, abandoned, or failed
/// save DISCARDS them. This is the exact inverse of the prior fail-CLOSED same-transaction capture, which
/// <c>Add()</c>-ed audit rows into the business command batch.
///
/// <para><b>Trigger timing (M1).</b> The persist handoff fires on the outermost-business-transaction-committed
/// signal, NOT unconditionally at <c>SavedChanges</c>. <c>SavedChanges</c> is the once-per-top-level-save
/// authority: it fires exactly once after a <c>SaveChanges</c> call ultimately succeeds. When no EXPLICIT
/// transaction is open at that point (<c>Database.CurrentTransaction is null</c> — EF's implicit per-save
/// transaction has already committed and cleared inside the save internals), the rows are dispatched THERE.
/// With an explicit <c>BeginTransaction … Commit</c> open, <c>SavedChanges</c> sees a non-null
/// <c>Database.CurrentTransaction</c>, so it SEALS the save's rows into the accumulator and DEFERS: it records
/// the open transaction's id and the handoff fires from <see cref="TransactionCommitted"/>/
/// <see cref="TransactionCommittedAsync"/> after the outermost commit. Dispatching while the money-path
/// transaction is still open would (a) let a sink throw roll back the money write and (b) leave a phantom
/// audit row for an action that abandons without commit — both forbidden.
///
/// <para><b>Retry-execution-strategy safety (F1).</b> On a RETRYING <c>IExecutionStrategy</c>
/// (<c>EnableRetryOnFailure</c>), EF re-runs the save on a transient failure (Postgres 40001/40P01). The
/// once-per-top-level-save interceptor events (<c>SavingChanges</c> / <c>SavedChanges</c> /
/// <c>SaveChangesFailed</c>) fire above the retry loop, whereas EF's implicit per-attempt transaction may
/// begin and end once PER ATTEMPT. This interceptor is therefore correct under EITHER interceptor-pairing,
/// without depending on whether <c>SavingChanges</c> re-fires per attempt or whether a failed attempt raises
/// <see cref="TransactionRolledBack"/> vs a silent dispose:
/// (1) <b>No duplicate.</b> <c>SavingChanges</c> REPLACES the in-flight save's contribution
/// (<c>Current</c>), never appends, so a per-attempt re-fire cannot double a row.
/// (2) <b>No silent loss.</b> <see cref="TransactionRolledBack"/> and <see cref="TransactionCommitted"/> act
/// ONLY on the transaction id that <c>SavedChanges</c> recorded as the deferred EXPLICIT transaction; a failed
/// retry attempt's implicit-transaction end never matches, so it cannot wipe a stash that no re-fire would
/// repopulate. The FINAL outcome is authoritative: <c>SavedChanges</c> (fires once, after retries succeed)
/// dispatches; <c>SaveChangesFailed</c> (fires once, after retries are exhausted) discards.
/// The legitimate accumulate case — two DISTINCT saves in ONE explicit transaction — is preserved because each
/// completed save SEALS its <c>Current</c> into the accumulating <c>Committed</c> set at <c>SavedChanges</c>.
///
/// <para><b>Abandoned-transaction reconciliation (FF1).</b> A <c>using var tx = …BeginTransaction(); …;
/// tx.Commit();</c> whose body THROWS before <c>Commit</c> disposes the transaction with neither an explicit
/// <c>Commit</c> nor <c>Rollback</c>. EF raises NEITHER <see cref="TransactionRolledBack"/> NOR a handled
/// dispose callback on that path, so the sealed <c>Committed</c> rows + <c>DeferredTransactionId</c> would
/// otherwise LEAK in the weak table and — because a pooled DbContext reuses the SAME instance across leases —
/// phantom-attach to the NEXT save on that instance. The capture and seal entry points therefore RECONCILE
/// stale deferral (<see cref="ReconcileStaleDeferred"/>) BEFORE stashing or sealing the new save's rows: a
/// still-set <c>DeferredTransactionId</c> whose transaction is no longer <c>Database.CurrentTransaction</c>
/// means the deferred explicit transaction ended WITHOUT a matched commit (a matched commit clears the marker
/// at dispatch, a matched rollback clears it at discard), so its sealed rows are DISCARDED — an abandoned
/// action emits NO audit row (correct loss, never a phantom). A still-open explicit transaction spanning
/// multiple saves keeps <c>CurrentTransaction.TransactionId == DeferredTransactionId</c>, so accumulation is
/// untouched; a per-attempt retry rollback never set the marker in the first place, so it is unaffected.</para>
///
/// <para><b>Exception isolation (M2) &amp; accepted loss (M3).</b> The ENTIRE synchronous capture phase —
/// actor resolution, delta-read, payload build (<see cref="MaterializeAuditRow"/>/<see cref="BuildSnapshotJson"/>/
/// <see cref="BuildChangesJson"/>) AND <c>[PersonalData]</c> masking (<see cref="MaskIfSensitive"/>) — runs inside
/// ONE swallow-all catch: on ANY throw it emits a structured log plus the <c>audit_capture_failed</c> metric and
/// RETURNS NORMALLY, so the business <c>SavingChanges</c> pipeline is never interrupted and the source save never
/// aborts (a guaranteed single-event audit loss, the third accepted-loss mode). The capture phase performs no
/// awaited call and takes no <see cref="CancellationToken"/>, so no <see cref="OperationCanceledException"/> can
/// originate from the caller's token there; the swallow deliberately does NOT rethrow (a rethrow would abort the
/// business save, violating the never-block invariant). The post-commit handoff is likewise swallow-all and
/// tokenless.</para>
///
/// <para><b>Per-save state (M4).</b> Per-save built rows live in a static
/// <see cref="ConditionalWeakTable{TKey,TValue}"/> keyed by <see cref="DbContext"/>, NEVER instance fields:
/// a pooled DbContext captures this <c>SingleInstance</c> interceptor across scopes, so instance state would
/// bleed between contexts. Each entry holds <c>Committed</c> (rows SEALED from completed saves, accumulating
/// across the open explicit transaction) and <c>Current</c> (the in-flight save's rows, replaced on every
/// <c>SavingChanges</c>). State is TAKEN-AND-REMOVED exactly once on BOTH the success (post-handoff) and the
/// discard (rollback / failed save) paths, so no stale payload survives to double-emit on pooled-context reuse.
/// A capture-phase throw discards only THIS save's partial rows (committed to <c>Current</c> only after a fully
/// successful build), leaving rows already sealed by a prior save in the same open transaction intact.</para>
///
/// <para><b>Activation gate.</b> Capture is a natural no-op on any context that has not mapped
/// <see cref="AuditTrail"/> (<c>context.Model.FindEntityType(typeof(AuditTrail)) is null</c>).
/// <b>Recursion exclusion.</b> <see cref="AuditTrail"/> is hard-excluded from eligibility, so audit rows are
/// never themselves audited.</para>
/// </summary>
public sealed class AuditCaptureSaveChangesInterceptor : SaveChangesInterceptor, IDbTransactionInterceptor
{
    private const string CaptureBuildReason = "capture-build";
    private const string HandoffReason = "handoff";

    private static readonly LogWrapper Logger = LogManager.GetLogger(nameof(AuditCaptureSaveChangesInterceptor));

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>
    /// Per-save built rows keyed by the business <see cref="DbContext"/>. NEVER instance fields (see class
    /// remarks): a pooled context captures this singleton interceptor across scopes. A context runs one
    /// <c>SaveChanges</c> at a time, so context-keyed state is race-free, and the weak table cannot leak
    /// contexts.
    /// </summary>
    private static readonly ConditionalWeakTable<DbContext, PendingAuditCapture> PendingByContext = [];

    private readonly IAuditActorProvider _actorProvider;
    private readonly AuditCaptureRegistry _registry;
    private readonly AuditFieldMasker _masker;
    private readonly IAuditSink? _sink;

    /// <summary>
    /// Resolves the actor seam, the fluent registry, the masking options, and (optionally) the out-of-band
    /// <see cref="IAuditSink"/>. <paramref name="actorProvider"/> MUST be singleton-safe (the interceptor is
    /// captured across scopes by pooled contexts). <paramref name="sink"/> is OPTIONAL: the framework does not
    /// register a concrete sink, so it resolves to <c>null</c> in a framework-only container (the interceptor
    /// then stashes and discards without persisting — it NEVER writes to the business context). The consumer
    /// registers a singleton sink, which the interceptor's post-commit handoff resolves.
    /// </summary>
    public AuditCaptureSaveChangesInterceptor(
        IAuditActorProvider actorProvider,
        AuditCaptureRegistry registry,
        AuditCaptureOptions options,
        IAuditSink? sink = null)
    {
        _actorProvider = actorProvider ?? throw new ArgumentNullException(nameof(actorProvider));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _masker = new AuditFieldMasker(options);
        _sink = sink;
    }

    // ── Capture (synchronous, at SavingChanges) ────────────────────────────────

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // Capture is fully synchronous (change-tracker reads + POCO build, no I/O), so the sync and async
        // entry points share ONE implementation — no duplicated logic to drift.
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken: cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        // Activation gate: no-op on any context that has not mapped AuditTrail.
        if (context is null || context.Model.FindEntityType(typeof(AuditTrail)) is null)
        {
            return;
        }

        try
        {
            // FF1: before stashing this save's rows, reconcile any stale deferral left by a prior explicit
            // transaction that ended without a matched commit (abandoned dispose), so its sealed rows never
            // phantom-attach to this save on a reused pooled context. No-op unless a prior deferral leaked.
            if (PendingByContext.TryGetValue(context, out var priorPending))
            {
                ReconcileStaleDeferred(context, priorPending);
            }

            List<AuditTrail>? rows = null;
            var actor = _actorProvider.GetCurrentActor();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                {
                    continue;
                }

                if (!IsCaptureEligible(entry.Metadata.ClrType))
                {
                    continue;
                }

                var row = MaterializeAuditRow(entry, actor);
                if (row is not null)
                {
                    (rows ??= []).Add(row);
                }
            }

            if (rows is null)
            {
                // Nothing eligible this save. Reset any prior re-fire's in-flight contribution so a stale
                // Current cannot leak (retry-safety, F1); leave rows already SEALED by prior saves intact.
                if (PendingByContext.TryGetValue(context, out var existing))
                {
                    existing.Current.Clear();
                }

                return;
            }

            // REPLACE (never append) the in-flight save's contribution (F1 duplicate-safety): a per-attempt
            // re-fire of the SAME logical save under a retrying execution strategy rebuilds the same rows and
            // must not double them. Rows are committed to Current only after a fully successful build (M3/M4):
            // a mid-build throw is caught below, leaving Current untouched and prior saves' sealed rows intact.
            PendingByContext.GetOrCreateValue(context).SetCurrent(rows);
        }
        catch (Exception ex)
        {
            // Swallow-all (M2/M3): the capture phase is fully synchronous — no awaited call and no
            // CancellationToken — so NO OperationCanceledException can originate from the caller's token here.
            // The swallow is deliberate and MUST NOT rethrow: a rethrow would abort the business SaveChanges,
            // violating the never-block invariant. Emits the audit_capture_failed signal and returns normally
            // (a guaranteed single-event audit loss — §2 M3). Current is written atomically only after a fully
            // successful build, so no partial in-flight state exists to unwind.
            LogAndCountFailure(ex, CaptureBuildReason);
        }
    }

    // ── Persist trigger (M1): outermost-business-transaction-committed ──────────

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        SealAndMaybeDispatch(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await SealAndMaybeDispatchAsync(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        DiscardCurrentSave(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DiscardCurrentSave(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        => DispatchIfDeferred(eventData.Context, eventData.TransactionId);

    /// <inheritdoc />
    public Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
        => DispatchIfDeferredAsync(eventData.Context, eventData.TransactionId);

    /// <inheritdoc />
    public void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
        => DiscardIfDeferred(eventData.Context, eventData.TransactionId);

    /// <inheritdoc />
    public Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DiscardIfDeferred(eventData.Context, eventData.TransactionId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// M1 dispatch decision, invoked once per successful top-level <c>SaveChanges</c>. Seals the in-flight
    /// save's rows into the accumulator, then either dispatches (no explicit transaction open) or defers to
    /// the outermost explicit commit (records the open transaction id for <see cref="DispatchIfDeferred"/>).
    ///
    /// <para><b>Ambient-transaction limitation (F2).</b> The open-transaction test reads ONLY EF's
    /// <c>Database.CurrentTransaction</c>; it is blind to an ambient <c>System.Transactions</c>/
    /// <c>TransactionScope</c>. A WRITE-path <c>TransactionScope</c> (none exists today — the only ambient use
    /// is read-only <c>NoLock</c>) would leave <c>CurrentTransaction</c> null, so this method would dispatch
    /// BEFORE the ambient scope committed, leaving a phantom audit row if that scope then rolled back.
    /// Introducing a write-path <c>TransactionScope</c> would require revisiting this trigger to also honour
    /// the ambient transaction (e.g. via <c>Transaction.Current</c> / a <c>TransactionCompleted</c> hook).</para>
    ///
    /// <para><b>Savepoint over-capture limitation (FF3).</b> A <c>RollbackToSavepoint</c> raises
    /// <c>RolledBackToSavepoint</c>, which this interceptor does NOT implement. A row already sealed for an
    /// audited write that an inner savepoint rollback later undoes still flushes at the outer
    /// <see cref="TransactionCommitted"/> — an OVER-capture (an audit row for work that did not persist). This
    /// is currently unreachable for audited writes in the consumer: the only savepoint users (Elfrique's
    /// <c>ListingProjectionInterceptor</c> and <c>ConfigService</c>) wrap a savepoint around ONLY non-audited
    /// projection / config statements written AFTER the audited source rows, so a rollback-to-savepoint there
    /// never undoes an audited entity's own write. Introducing a savepoint that spans an audited write would
    /// require honouring <c>RolledBackToSavepoint</c> here (dropping the rows sealed since that savepoint).</para>
    /// </summary>
    private void SealAndMaybeDispatch(DbContext? context)
    {
        if (context is null || !PendingByContext.TryGetValue(context, out var pending))
        {
            return;
        }

        // FF1: reconcile a leaked deferral before folding this save's rows, so an abandoned prior explicit
        // transaction's sealed rows are not dispatched together with this save's rows (see class remarks).
        ReconcileStaleDeferred(context, pending);

        pending.Seal();

        if (context.Database.CurrentTransaction is { } tx)
        {
            // Explicit transaction still open: accumulate and defer to its outermost commit (M1). Record the
            // transaction id so a per-attempt implicit-transaction end (retry) is ignored by the end hooks (F1).
            pending.DeferredTransactionId = tx.TransactionId;
            return;
        }

        DispatchPending(context);
    }

    /// <summary>Async twin of <see cref="SealAndMaybeDispatch"/> (see its remarks, incl. the F2 note).</summary>
    private async ValueTask SealAndMaybeDispatchAsync(DbContext? context)
    {
        if (context is null || !PendingByContext.TryGetValue(context, out var pending))
        {
            return;
        }

        // FF1: reconcile a leaked deferral before folding this save's rows (see SealAndMaybeDispatch remarks).
        ReconcileStaleDeferred(context, pending);

        pending.Seal();

        if (context.Database.CurrentTransaction is { } tx)
        {
            pending.DeferredTransactionId = tx.TransactionId;
            return;
        }

        await DispatchPendingAsync(context);
    }

    private void DiscardCurrentSave(DbContext? context)
    {
        if (context is null || !PendingByContext.TryGetValue(context, out var pending))
        {
            return;
        }

        // The in-flight save FINALLY failed — SaveChangesFailed fires ONCE per top-level SaveChanges, never
        // per retry attempt (F1). Drop only THIS save's un-sealed rows; rows already sealed from prior saves
        // in an open explicit transaction remain valid until that transaction itself ends.
        pending.Current.Clear();
        if (pending.IsEmpty)
        {
            PendingByContext.Remove(context);
        }
    }

    private void DispatchIfDeferred(DbContext? context, Guid transactionId)
    {
        // Act ONLY on the explicit transaction SavedChanges recorded as deferred; a per-attempt implicit
        // transaction commit (retry) never matches, so it cannot double-dispatch (F1).
        if (context is null
            || !PendingByContext.TryGetValue(context, out var pending)
            || pending.DeferredTransactionId != transactionId)
        {
            return;
        }

        DispatchPending(context);
    }

    private async Task DispatchIfDeferredAsync(DbContext? context, Guid transactionId)
    {
        if (context is null
            || !PendingByContext.TryGetValue(context, out var pending)
            || pending.DeferredTransactionId != transactionId)
        {
            return;
        }

        await DispatchPendingAsync(context);
    }

    private static void DiscardIfDeferred(DbContext? context, Guid transactionId)
    {
        // Act ONLY on the deferred explicit transaction's rollback; a per-attempt implicit-transaction
        // rollback (a retried transient failure) never matches, so it CANNOT wipe a stash that no
        // SavingChanges re-fire would repopulate — the crux of the F1 silent-loss guard.
        if (context is null
            || !PendingByContext.TryGetValue(context, out var pending)
            || pending.DeferredTransactionId != transactionId)
        {
            return;
        }

        PendingByContext.Remove(context);
    }

    private void DispatchPending(DbContext? context)
    {
        if (context is null || !TryTakePending(context, out var rows) || rows.Count == 0)
        {
            return;
        }

        var sink = _sink;
        if (sink is null)
        {
            // Unwired (framework-only container, or before the consumer wires a sink): the interceptor NEVER
            // writes to the business context, so the rows are discarded. Post-take-and-remove (M4).
            return;
        }

        try
        {
            // Tokenless fire-and-forget (cooperative-cancellation carve-out): the business action has ALREADY
            // committed, so the out-of-band audit persist must run to completion regardless of the caller's
            // token and MUST NOT surface OperationCanceledException (which would misreport the committed action
            // as cancelled). A synchronous SaveChanges cannot await, so the async sink call is observed via a
            // fault-swallowing continuation.
            var task = sink.PersistAsync(rows, cancellationToken: CancellationToken.None);
            if (!task.IsCompletedSuccessfully)
            {
                _ = ObserveHandoffAsync(task);
            }
        }
        catch (Exception ex)
        {
            LogAndCountFailure(ex, HandoffReason);
        }
    }

    private async Task DispatchPendingAsync(DbContext? context)
    {
        if (context is null || !TryTakePending(context, out var rows) || rows.Count == 0)
        {
            return;
        }

        var sink = _sink;
        if (sink is null)
        {
            return;
        }

        try
        {
            // Tokenless (CancellationToken.None): post-commit out-of-band persist, per the carve-out documented
            // on DispatchPending. Swallow-all (M2) — a sink is swallow-all by contract, but a contract-violating
            // throw must never propagate out of a post-commit hook and roll back nothing / misreport success.
            await sink.PersistAsync(rows, cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            LogAndCountFailure(ex, HandoffReason);
        }
    }

    private static async Task ObserveHandoffAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            LogAndCountFailure(ex, HandoffReason);
        }
    }

    /// <summary>
    /// FF1 stale-deferred-state reconciliation. <see cref="PendingAuditCapture.DeferredTransactionId"/> is set
    /// ONLY while an explicit transaction is open (recorded at <see cref="SealAndMaybeDispatch"/>); a matched
    /// commit CLEARS it at dispatch and a matched rollback CLEARS it at discard. So a still-set
    /// <c>DeferredTransactionId</c> whose transaction is no longer the context's <c>CurrentTransaction</c> means
    /// that explicit transaction ended WITHOUT a matched commit — the abandoned-dispose case, for which EF
    /// raises neither <see cref="TransactionRolledBack"/> nor a handled dispose callback, so the sealed rows
    /// would otherwise leak and phantom-attach to the next save on a reused pooled context. Discard the leaked
    /// sealed rows (an abandoned action emits no audit row — correct loss). A still-open explicit transaction
    /// keeps <c>CurrentTransaction.TransactionId == DeferredTransactionId</c> → no discard. Never-block: a bare
    /// property read + list clear that cannot throw under normal EF operation.
    /// </summary>
    private static void ReconcileStaleDeferred(DbContext context, PendingAuditCapture pending)
    {
        if (pending.DeferredTransactionId is not { } deferred)
        {
            return;
        }

        var current = context.Database.CurrentTransaction;
        if (current is null || current.TransactionId != deferred)
        {
            pending.DiscardDeferred();
        }
    }

    // ── Per-save state (M4: take-and-remove exactly once) ──────────────────────

    private static bool TryTakePending(DbContext context, out List<AuditTrail> rows)
    {
        if (PendingByContext.TryGetValue(context, out var pending))
        {
            PendingByContext.Remove(context);
            pending.Seal(); // defensive: fold any un-sealed in-flight rows into the handoff before dispatch
            rows = pending.Committed;
            return true;
        }

        rows = [];
        return false;
    }

    /// <summary>Test-only probe (M4 assertions): whether any per-save payload is currently stashed for the context.</summary>
    internal static bool HasPendingCapture(DbContext context) => PendingByContext.TryGetValue(context, out _);

    // ── Test-only deterministic drivers (retry-safety simulation, F1) ──────────
    // These invoke the SAME private logic the public ISaveChangesInterceptor / IDbTransactionInterceptor
    // callbacks invoke, letting the SQLite-in-memory suite reproduce an ExecutionStrategy retry sequence — a
    // per-attempt transaction end that is NOT the deferred transaction, with or without a SavingChanges
    // re-fire — WITHOUT injecting a real transient DB failure. They add no production behaviour.

    /// <summary>Test seam: drives the <c>SavingChanges</c> capture phase.</summary>
    internal void SimulateSavingChanges(DbContext context) => Capture(context);

    /// <summary>Test seam: drives the <c>SavedChanges</c> seal-and-dispatch decision (M1).</summary>
    internal void SimulateSavedChanges(DbContext context) => SealAndMaybeDispatch(context);

    /// <summary>Test seam: drives the <c>SaveChangesFailed</c> discard of the in-flight save.</summary>
    internal void SimulateSaveChangesFailed(DbContext context) => DiscardCurrentSave(context);

    /// <summary>Test seam: drives the <c>TransactionCommitted</c> deferred-dispatch decision.</summary>
    internal void SimulateTransactionCommitted(DbContext context, Guid transactionId)
        => DispatchIfDeferred(context, transactionId);

    /// <summary>Test seam: drives the <c>TransactionRolledBack</c> deferred-discard decision.</summary>
    internal void SimulateTransactionRolledBack(DbContext context, Guid transactionId)
        => DiscardIfDeferred(context, transactionId);

    private static void LogAndCountFailure(Exception exception, string reason)
    {
        Logger.LogError(exception,
            "Audit capture failed and was swallowed under the never-block invariant (reason: {Reason}); "
            + "accepted single-event audit loss (§2 M3).",
            reason);
        AuditCaptureDiagnostics.CaptureFailed.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }

    // ── Payload build (unchanged from the shipped shape; masking preserved) ─────

    /// <summary>
    /// The pinned eligibility predicate (R2): (1) hard-exclude <see cref="AuditTrail"/> (recursion); (2) an
    /// ignore in EITHER source wins — class <c>[SolhigsonAuditIgnore]</c> (inherit:false) OR registry-ignored;
    /// (3) captured iff class <c>[SolhigsonAuditInclude]</c> (inherit:false) OR <see cref="AuditCaptureRegistry.IsIncluded"/>.
    /// </summary>
    private bool IsCaptureEligible(Type type)
    {
        if (type == typeof(AuditTrail))
        {
            return false;
        }

        if (type.IsDefined(typeof(SolhigsonAuditIgnoreAttribute), inherit: false) || _registry.IsIgnored(type))
        {
            return false;
        }

        return type.IsDefined(typeof(SolhigsonAuditIncludeAttribute), inherit: false) || _registry.IsIncluded(type);
    }

    private AuditTrail? MaterializeAuditRow(EntityEntry entry, AuditActor actor)
    {
        var row = new AuditTrail
        {
            Category = AuditEventCategory.DataChange,
            // Added → created, Modified → updated, Deleted → deleted: disambiguates INSERT from DELETE,
            // which both ride Snapshot. Any other state was filtered out before this method; the null arm
            // is unreachable-but-safe (the default arm of the switch below returns no row anyway).
            Action = entry.State switch
            {
                EntityState.Added => AuditActions.Created,
                EntityState.Modified => AuditActions.Updated,
                EntityState.Deleted => AuditActions.Deleted,
                _ => null,
            },
            EntityType = entry.Metadata.ClrType.Name,
            EntityId = BuildEntityId(entry),
            // TYPE-MEMBERSHIP check (not an inherit:false attribute probe), so a base-class implementation
            // binds for every derived entity. A throwing consumer getter rides the capture phase's
            // swallow-all boundary (M2/M3) like any consumer-seam failure — no nested guard, so the bug
            // stays visible via the audit_capture_failed metric instead of silently nulling the name.
            SubjectDisplayName = entry.Entity is IAuditSubjectNamed named
                ? AuditTrail.Truncate(named.AuditSubjectDisplayName, AuditTrail.SubjectDisplayNameMaxLength)
                : null,
            ActorUserId = actor.ActorUserId,
            UserDisplayName = actor.UserDisplayName,
            UserIp = actor.UserIp,
            SourceType = actor.SourceType,
            SourceId = actor.SourceId,
        };

        switch (entry.State)
        {
            case EntityState.Added:
                row.Snapshot = BuildSnapshotJson(entry, useOriginalValues: false);
                break;
            case EntityState.Deleted:
                row.Snapshot = BuildSnapshotJson(entry, useOriginalValues: true);
                break;
            case EntityState.Modified:
                var changes = BuildChangesJson(entry);
                if (changes is null)
                {
                    // A "Modified" entry with no captured scalar delta (only ignored/unmodified columns
                    // changed) yields no audit row.
                    return null;
                }

                row.Changes = changes;
                break;
            default:
                return null;
        }

        return row;
    }

    private static string BuildEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return string.Empty;
        }

        var useOriginal = entry.State == EntityState.Deleted;
        var parts = new List<string>(key.Properties.Count);
        foreach (var keyProperty in key.Properties)
        {
            var propertyEntry = entry.Property(keyProperty.Name);
            var value = useOriginal ? propertyEntry.OriginalValue : propertyEntry.CurrentValue;
            parts.Add(value?.ToString() ?? string.Empty);
        }

        return string.Join(",", parts);
    }

    private string BuildSnapshotJson(EntityEntry entry, bool useOriginalValues)
    {
        var snapshot = new Dictionary<string, object?>();
        foreach (var propertyEntry in entry.Properties)
        {
            if (IsPropertyIgnored(propertyEntry.Metadata.PropertyInfo))
            {
                continue;
            }

            var rawValue = useOriginalValues ? propertyEntry.OriginalValue : propertyEntry.CurrentValue;
            snapshot[propertyEntry.Metadata.Name] = MaskIfSensitive(propertyEntry, rawValue);
        }

        return JsonSerializer.Serialize(snapshot, PayloadJsonOptions);
    }

    private string? BuildChangesJson(EntityEntry entry)
    {
        List<FieldChange>? changes = null;
        foreach (var propertyEntry in entry.Properties)
        {
            if (!propertyEntry.IsModified)
            {
                continue;
            }

            if (IsPropertyIgnored(propertyEntry.Metadata.PropertyInfo))
            {
                continue;
            }

            // Value-unchanged skip, on the RAW pre-mask values: a full-entity Modified save (every property
            // IsModified — the UserManager.UpdateAsync shape) must emit only genuine changes, and a post-mask
            // comparison would read "***" == "***" and drop a genuinely-changed sensitive field. Zero
            // survivors leave `changes` null ⇒ no audit row (the Modified no-delta path in MaterializeAuditRow).
            if (Equals(propertyEntry.OriginalValue, propertyEntry.CurrentValue))
            {
                continue;
            }

            // Both sides masked (pin item 5): a new-side-only mask would leak the pre-change plaintext.
            var oldValue = MaskIfSensitive(propertyEntry, propertyEntry.OriginalValue);
            var newValue = MaskIfSensitive(propertyEntry, propertyEntry.CurrentValue);
            (changes ??= []).Add(new FieldChange(propertyEntry.Metadata.Name, oldValue, newValue));
        }

        return changes is null ? null : JsonSerializer.Serialize(changes, PayloadJsonOptions);
    }

    private object? MaskIfSensitive(PropertyEntry propertyEntry, object? rawValue)
        => _masker.ShouldMask(propertyEntry.Metadata.Name, propertyEntry.Metadata.PropertyInfo)
            ? AuditFieldMasker.MaskMarker
            : rawValue;

    private static bool IsPropertyIgnored(PropertyInfo? propertyInfo)
        => propertyInfo is not null
           && propertyInfo.IsDefined(typeof(SolhigsonAuditIgnoreAttribute), inherit: false);

    /// <summary>The pinned E7-consumed UPDATE-delta element shape: an array of these.</summary>
    private sealed record FieldChange(
        [property: JsonPropertyName("field")] string Field,
        [property: JsonPropertyName("old")] object? Old,
        [property: JsonPropertyName("new")] object? New);

    /// <summary>
    /// Per-save built rows keyed by <see cref="DbContext"/> in <see cref="PendingByContext"/>. <c>Committed</c>
    /// holds rows SEALED from completed saves, accumulating across the open explicit transaction (M1);
    /// <c>Current</c> holds the in-flight save's rows, REPLACED on every <c>SavingChanges</c> so a per-attempt
    /// retry re-fire cannot duplicate (F1). <c>DeferredTransactionId</c> is the explicit transaction the handoff
    /// is deferred to (recorded at <c>SavedChanges</c> when a transaction is open); null ⇒ no deferral.
    /// </summary>
    private sealed class PendingAuditCapture
    {
        public List<AuditTrail> Committed { get; } = [];

        public List<AuditTrail> Current { get; } = [];

        public Guid? DeferredTransactionId { get; set; }

        public bool IsEmpty => Committed.Count == 0 && Current.Count == 0;

        /// <summary>Replaces (never appends) the in-flight save's rows — the F1 duplicate guard.</summary>
        public void SetCurrent(List<AuditTrail> rows)
        {
            Current.Clear();
            Current.AddRange(rows);
        }

        /// <summary>
        /// FF1: discards the SEALED rows and the deferral marker of a previously-deferred explicit transaction
        /// that ended without a matched commit (abandoned dispose / unhandled rollback). Leaves <c>Current</c>
        /// — the new save's in-flight rows — intact, so reconciliation at a new save's entry does not drop the
        /// row being captured right now.
        /// </summary>
        public void DiscardDeferred()
        {
            Committed.Clear();
            DeferredTransactionId = null;
        }

        /// <summary>Folds a completed save's rows into the accumulator (idempotent when <c>Current</c> is empty).</summary>
        public void Seal()
        {
            if (Current.Count == 0)
            {
                return;
            }

            Committed.AddRange(Current);
            Current.Clear();
        }
    }
}
