using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Pinned <see cref="AuditTrail.Action"/> values for interceptor-captured
/// <see cref="AuditEventCategory.DataChange"/> rows: <see cref="AuditCaptureSaveChangesInterceptor"/>
/// maps Added → <see cref="Created"/>, Modified → <see cref="Updated"/>, Deleted → <see cref="Deleted"/>.
/// Deliberately lowercase STRINGS, not an enum: explicit events
/// (<see cref="IAuditTrailService{TContext}.LogAsync"/>) carry their own eventType verbatim in the same
/// column, so the action vocabulary is open-ended by design.
/// </summary>
public static class AuditActions
{
    /// <summary>Data-change INSERT (<c>EntityState.Added</c>).</summary>
    public const string Created = "created";

    /// <summary>Data-change UPDATE with a genuine delta (<c>EntityState.Modified</c>).</summary>
    public const string Updated = "updated";

    /// <summary>Data-change DELETE (<c>EntityState.Deleted</c>).</summary>
    public const string Deleted = "deleted";
}
