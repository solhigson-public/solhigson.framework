namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Seam that resolves the <see cref="AuditActor"/> for the current write. Consumers register
/// context-specific implementations (a web provider backed by <c>IHttpContextAccessor</c>, a
/// Hangfire provider backed by an AsyncLocal actor set); the framework registers
/// <see cref="UnattributedAuditActorProvider"/> as the default with <c>PreserveExistingDefaults()</c>,
/// so any consumer registration wins.
/// </summary>
public interface IAuditActorProvider
{
    /// <summary>Resolves the actor behind the write in progress. Never returns null.</summary>
    AuditActor GetCurrentActor();
}
