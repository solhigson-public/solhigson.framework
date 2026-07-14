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
/// </list>
/// </para>
/// </summary>
public record AuditTrail : IEfCoreGenIgnore
{
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
}
