namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Default <see cref="IAuditActorProvider"/> for writes with no resolvable human/job actor
/// (startup migrations, ad-hoc maintenance). Always yields <see cref="AuditActor.UnattributedActor"/>.
/// Registered in <c>SolhigsonAutofacModule</c> with <c>PreserveExistingDefaults()</c> so a consumer's
/// web/Hangfire provider registration takes precedence.
/// </summary>
public sealed class UnattributedAuditActorProvider : IAuditActorProvider
{
    /// <inheritdoc />
    public AuditActor GetCurrentActor() => AuditActor.UnattributedActor;
}
