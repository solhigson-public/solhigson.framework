using System;
using System.ComponentModel.DataAnnotations;
using Solhigson.Framework.EfCore;

namespace Solhigson.Framework.Persistence.EntityModels;

/// <summary>
/// General-purpose platform audit-trail row (see <see cref="AuditEventCategory"/> for the
/// DataChange / SecurityEvent / BusinessEvent classification).
/// <para>
/// This is a framework-owned, <b>provider-agnostic</b> POCO. It carries NO <c>DbSet</c> on any
/// framework <c>DbContext</c> (<c>SolhigsonDbContext</c> / <c>SolhigsonIdentityDbContext</c>) and it
/// implements <see cref="IEfCoreGenIgnore"/>, so the solhigson-ef generator never scaffolds
/// repositories, DTOs, cache-models, or tests for it. The consumer owns ALL persistence mapping
/// in its own <c>OnModelCreating</c> and migration.
/// </para>
/// <para>
/// CONSUMER-MIGRATION CONTRACT (the real DDL lands in the consumer's migration, never here):
/// <list type="bullet">
///   <item>
///     PRIMARY KEY: composite <c>(Id, Created)</c>. <see cref="Id"/> is a uuidv7 generated
///     app-side via <see cref="Guid.CreateVersion7()"/>; <see cref="Created"/> is the second key
///     column so the key is range-partition friendly (the row is the consumer's 32nd
///     range-partitioned parent).
///   </item>
///   <item>INDEX <c>(EntityType, EntityId, Created)</c>: "changes to this entity/subject" lookups.</item>
///   <item>INDEX <c>(ActorUserId, Created)</c>: "activity by this actor" lookups.</item>
///   <item>No GIN index at the hot tier.</item>
///   <item>
///     <see cref="Snapshot"/> and <see cref="Changes"/> map to <c>jsonb</c> (lz4 TOAST) via the
///     consumer's <c>OnModelCreating</c> (<c>HasColumnType("jsonb")</c>). They are plain
///     <see cref="string"/> here to keep the framework provider-agnostic (no Npgsql attribute).
///     <see cref="Snapshot"/> carries the full row on INSERT/DELETE; <see cref="Changes"/> carries
///     the per-field {old,new} delta on UPDATE.
///   </item>
///   <item>
///     <see cref="Action"/> (varchar 128) and <see cref="SubjectDisplayName"/> (varchar 256) are plain
///     nullable columns adopted by an additive consumer migration; no index of their own. The framework
///     stamp sites truncate to the declared widths app-side, so no consumer-sourced value can fault the
///     audit INSERT.
///   </item>
/// </list>
/// </para>
/// </summary>
public record AuditTrail : IEfCoreGenIgnore
{
    /// <summary>Declared column width of <see cref="Action"/>, enforced app-side at the stamp sites.</summary>
    internal const int ActionMaxLength = 128;

    /// <summary>Declared column width of <see cref="SubjectDisplayName"/>, enforced app-side at the stamp sites.</summary>
    internal const int SubjectDisplayNameMaxLength = 256;

    /// <summary>
    /// Primary-key component 1. A sortable uuidv7 generated app-side at construction via
    /// <see cref="Guid.CreateVersion7()"/>; NOT a NewId varchar and NOT a bigint.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Primary-key component 2 and the range-partition key. Defaults to <see cref="DateTime.UtcNow"/>;
    /// always stored in UTC.
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>Top-level classification of the event.</summary>
    public AuditEventCategory Category { get; set; }

    /// <summary>
    /// Action label refining <see cref="Category"/>. Interceptor-captured
    /// <see cref="AuditEventCategory.DataChange"/> rows carry the pinned lowercase
    /// <see cref="Solhigson.Framework.AuditCapture.AuditActions"/> values (created / updated / deleted —
    /// disambiguating INSERT from DELETE, which both ride <see cref="Snapshot"/>); explicit events carry
    /// their eventType VERBATIM (never forced lowercase). Deliberately a string, not an enum, so consumer
    /// event vocabularies extend without a framework change.
    /// </summary>
    [StringLength(ActionMaxLength)]
    public string? Action { get; set; }

    /// <summary>
    /// The audited entity's type name, or — for a security event — the subject type. Part of the
    /// <c>(EntityType, EntityId, Created)</c> lookup index. Sourced from one shared constant set by
    /// writer, query, and UI alike (consumer concern).
    /// </summary>
    [StringLength(255)]
    public string? EntityType { get; set; }

    /// <summary>
    /// The audited entity's (polymorphic) key, or the subject user id for a security event. No FK is
    /// possible because the id is polymorphic. Part of the <c>(EntityType, EntityId, Created)</c> index.
    /// </summary>
    [StringLength(450)]
    public string? EntityId { get; set; }

    /// <summary>
    /// Denormalized display name of the audited SUBJECT — the entity/user the row is about, keyed by
    /// <see cref="EntityType"/>/<see cref="EntityId"/> — NOT the acting user (see
    /// <see cref="UserDisplayName"/>). Stamped from
    /// <see cref="Solhigson.Framework.AuditCapture.IAuditSubjectNamed"/> on interceptor capture, or passed
    /// explicitly to <c>LogAsync</c>. Name-class personal data like <see cref="UserDisplayName"/>
    /// (a pseudonymization target under GDPR Art-17); truncated to <see cref="SubjectDisplayNameMaxLength"/>
    /// at the stamp sites so an overlong consumer value can never fault the audit INSERT.
    /// </summary>
    [StringLength(SubjectDisplayNameMaxLength)]
    public string? SubjectDisplayName { get; set; }

    /// <summary>
    /// Full-row payload on INSERT and DELETE. Provider-agnostic <c>jsonb</c>-as-string; the consumer maps
    /// it to <c>jsonb</c> in <c>OnModelCreating</c>. Null on UPDATE (see <see cref="Changes"/>).
    /// </summary>
    public string? Snapshot { get; set; }

    /// <summary>
    /// Per-field {old,new} delta on UPDATE. Provider-agnostic <c>jsonb</c>-as-string; masking (F2) applies
    /// to BOTH sides of each pair. Null on INSERT/DELETE (see <see cref="Snapshot"/>).
    /// </summary>
    public string? Changes { get; set; }

    /// <summary>
    /// The acting user's identity id, or null when the write is unattributed. Part of the
    /// <c>(ActorUserId, Created)</c> lookup index.
    /// </summary>
    [StringLength(450)]
    public string? ActorUserId { get; set; }

    /// <summary>Denormalized display name of the acting user (retained for accountability; a pseudonymization target under GDPR Art-17).</summary>
    [StringLength(256)]
    public string? UserDisplayName { get; set; }

    /// <summary>The acting user's request IP, when resolvable (IPv6-max length).</summary>
    [StringLength(45)]
    public string? UserIp { get; set; }

    /// <summary>
    /// The write's source identity: <c>"web"</c>, a background-job type name, or the unattributed
    /// marker (<see cref="Solhigson.Framework.AuditCapture.AuditActor.Unattributed"/>).
    /// </summary>
    [StringLength(256)]
    public string? SourceType { get; set; }

    /// <summary>The source's correlation id when one exists (e.g. the Hangfire job id); otherwise null.</summary>
    [StringLength(256)]
    public string? SourceId { get; set; }

    /// <summary>
    /// Defensive length clamp used by the framework's stamp sites (<see cref="Action"/>
    /// <see cref="ActionMaxLength"/>, <see cref="SubjectDisplayName"/>
    /// <see cref="SubjectDisplayNameMaxLength"/>): a consumer-sourced value longer than the declared
    /// column width is truncated rather than allowed to fault the audit INSERT and lose the row.
    /// </summary>
    internal static string? Truncate(string? value, int maxLength)
        => value is not null && value.Length > maxLength ? value[..maxLength] : value;
}
