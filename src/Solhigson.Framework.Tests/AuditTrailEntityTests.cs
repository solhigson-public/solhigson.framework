using System;
using System.Text.Json;
using Shouldly;
using Solhigson.Framework.Persistence.EntityModels;
using Xunit;

namespace Solhigson.Framework.Tests;

public class AuditTrailEntityTests
{
    [Fact]
    public void NewInstances_ReceiveDistinctVersion7Guids()
    {
        var first = new AuditTrail();
        var second = new AuditTrail();

        first.Id.ShouldNotBe(Guid.Empty);
        second.Id.ShouldNotBe(Guid.Empty);
        first.Id.ShouldNotBe(second.Id);
        first.Id.Version.ShouldBe(7);
        second.Id.Version.ShouldBe(7);
    }

    [Fact]
    public void Created_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var entity = new AuditTrail();
        var after = DateTime.UtcNow;

        entity.Created.Kind.ShouldBe(DateTimeKind.Utc);
        entity.Created.ShouldBeInRange(before, after);
    }

    [Fact]
    public void Payload_RoundTripsThroughSystemTextJson()
    {
        var entity = new AuditTrail
        {
            Category = AuditEventCategory.DataChange,
            EntityType = "Order",
            EntityId = "42",
            ActorUserId = "user-1",
            UserDisplayName = "Jane Doe",
            UserIp = "203.0.113.5",
            SourceType = "web",
            SourceId = null,
            Snapshot = """{"Amount":100,"Status":"Paid"}""",
            Changes = """[{"field":"Status","old":"Pending","new":"Paid"}]""",
        };

        var json = JsonSerializer.Serialize(entity);
        var restored = JsonSerializer.Deserialize<AuditTrail>(json);

        restored.ShouldNotBeNull();
        restored.Id.ShouldBe(entity.Id);
        restored.Created.ShouldBe(entity.Created);
        restored.Category.ShouldBe(AuditEventCategory.DataChange);
        restored.EntityType.ShouldBe("Order");
        restored.EntityId.ShouldBe("42");
        restored.Snapshot.ShouldBe(entity.Snapshot);
        restored.Changes.ShouldBe(entity.Changes);
        restored.SourceType.ShouldBe("web");
        restored.SourceId.ShouldBeNull();
    }

    [Fact]
    public void Category_DefaultsToDataChange()
    {
        var entity = new AuditTrail();

        entity.Category.ShouldBe(AuditEventCategory.DataChange);
    }
}
