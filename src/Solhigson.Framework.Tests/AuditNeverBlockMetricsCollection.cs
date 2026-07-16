using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Serializes the never-block audit test classes that observe the process-global <c>audit_capture_failed</c>
/// meter (<see cref="Solhigson.Framework.AuditCapture.AuditCaptureDiagnostics"/>). The counter is a single
/// static instrument, so a <c>MeterListener</c> in one class would otherwise pick up emissions from a
/// concurrently-running sibling class; disabling parallelization removes that non-determinism.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AuditNeverBlockMetricsCollection
{
    public const string Name = "AuditNeverBlockMetrics";
}
