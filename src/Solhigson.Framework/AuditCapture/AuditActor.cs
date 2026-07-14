namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// The resolved actor behind a write, as stamped onto an
/// <see cref="Solhigson.Framework.Persistence.EntityModels.AuditTrail"/> row. Populated by an
/// <see cref="IAuditActorProvider"/> at capture time.
/// </summary>
public sealed record AuditActor
{
    /// <summary>
    /// The explicit marker for a write with no resolvable human/job actor (startup migrations,
    /// ad-hoc maintenance). This deliberately replaces the legacy <c>"System Initiated"</c> constant.
    /// </summary>
    public const string Unattributed = "unattributed";

    /// <summary>The acting user's identity id, or null when unattributed.</summary>
    public string? ActorUserId { get; init; }

    /// <summary>The acting user's denormalized display name.</summary>
    public string? UserDisplayName { get; init; }

    /// <summary>The acting user's request IP, when resolvable.</summary>
    public string? UserIp { get; init; }

    /// <summary>Source identity: <c>"web"</c>, a background-job type name, or <see cref="Unattributed"/>.</summary>
    public string? SourceType { get; init; }

    /// <summary>Source correlation id (e.g. the Hangfire job id), when one exists.</summary>
    public string? SourceId { get; init; }

    /// <summary>The shared singleton describing an unattributed writer.</summary>
    public static AuditActor UnattributedActor { get; } = new()
    {
        UserDisplayName = Unattributed,
        SourceType = Unattributed,
    };
}
