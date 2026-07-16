using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Append-only correctness guard for <see cref="AuditTrail"/> (F2, ships day one). At
/// <c>SavingChanges[Async]</c> it throws <see cref="System.InvalidOperationException"/> on ANY tracked
/// <see cref="AuditTrail"/> entry in state <see cref="EntityState.Modified"/> or
/// <see cref="EntityState.Deleted"/> — an audit row may only ever be inserted.
///
/// <para>This is a guard against our OWN code (the <c>OrderFeeAllocationAppendOnlyInterceptor</c> precedent),
/// NOT tamper enforcement. There is deliberately NO carve-out: the sole legitimate mutation of an audit row
/// is GDPR Art-17 actor pseudonymization, which runs as a set-based <c>ExecuteUpdate</c> that never passes
/// through <c>SaveChanges</c> interceptors, so it is unaffected by this guard.</para>
///
/// <para>Under the never-block invariant (§2 Tamper evidence [Guard-path adjusted 2026-07-15]) this guard
/// rides the OUT-OF-BAND audit-write path — the sink's separate short-lived scope/DbContext and the deferred
/// persist job — NOT a same-transaction business <c>ChangeTracker</c>. The F2-prime capture interceptor no
/// longer <c>Add()</c>s audit rows into the business context (audit persistence has left the business
/// transaction), so this guard is registered onto the sink's write context, where the rows it inserts are in
/// state <see cref="EntityState.Added"/>, which this guard permits. The guard logic is context-agnostic —
/// it iterates <c>ChangeTracker.Entries&lt;AuditTrail&gt;()</c> on whatever context it is wired onto — so on
/// any context that tracks no <see cref="AuditTrail"/> entities it is a natural no-op.</para>
/// </summary>
public sealed class AuditTrailAppendOnlyInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        GuardAppendOnly(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        GuardAppendOnly(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken: cancellationToken);
    }

    private static void GuardAppendOnly(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<AuditTrail>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new System.InvalidOperationException(
                    $"AuditTrail is append-only: a tracked {entry.State} of an AuditTrail row is forbidden. " +
                    "Audit rows may only be inserted; the sole legitimate mutation (GDPR Art-17 actor " +
                    "pseudonymization) runs as a set-based ExecuteUpdate that bypasses SaveChanges interceptors.");
            }
        }
    }
}
