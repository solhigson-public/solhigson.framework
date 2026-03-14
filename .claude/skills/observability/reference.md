## Health Checks Setup

### Registration
```csharp
public static class HealthCheckExtensions
{
    public static IServiceCollection AddDefaultHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "database",
                tags: ["ready"])
            .AddRedis(
                configuration.GetConnectionString("Redis")!,
                name: "redis",
                tags: ["ready"])
            .AddUrlGroup(
                new Uri(configuration["ExternalApi:BaseUrl"]!),
                name: "external-api",
                tags: ["ready"]);

        return services;
    }
}
```

### Endpoint Mapping
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false // liveness — no dependency checks
});

app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteReadinessResponse
});

static Task WriteReadinessResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var result = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds
        })
    };
    return context.Response.WriteAsJsonAsync(result);
}
```

---

## NLog Structured Logging

### NLog.config (JSON structured output)
```xml
<?xml version="1.0" encoding="utf-8"?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

  <targets>
    <!-- Development: console with colors -->
    <target name="console" xsi:type="ColoredConsole"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:${newline}${exception:format=tostring}}" />

    <!-- Production: structured JSON file -->
    <target name="jsonFile" xsi:type="File"
            fileName="logs/${shortdate}.json"
            archiveEvery="Day"
            maxArchiveFiles="30">
      <layout xsi:type="JsonLayout" includeEventProperties="true">
        <attribute name="time" layout="${longdate}" />
        <attribute name="level" layout="${level:uppercase=true}" />
        <attribute name="logger" layout="${logger}" />
        <attribute name="message" layout="${message}" />
        <attribute name="correlationId" layout="${mdlc:CorrelationId}" />
        <attribute name="exception" layout="${exception:format=tostring}"
                   encode="false" />
      </layout>
    </target>
  </targets>

  <rules>
    <logger name="Microsoft.*" maxlevel="Info" final="true" />
    <logger name="*" minlevel="Info" writeTo="console,jsonFile" />
  </rules>
</nlog>
```

### Logging with structured parameters
```csharp
// GOOD: structured parameters — searchable, parseable
_logger.Info("Processing order {OrderId} for amount {Amount}", orderId, amount);

// BAD: string interpolation — loses structure
_logger.Info($"Processing order {orderId} for amount {amount}");
```

---

## OpenTelemetry SDK Setup

### Registration (no Aspire)
```csharp
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(o => o.SetDbStatementForText = true)
                    .AddSource("AppName.*");

                var otlpEndpoint = configuration["OpenTelemetry:Endpoint"];
                if (!string.IsNullOrEmpty(otlpEndpoint))
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                else
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("AppName.*");

                var otlpEndpoint = configuration["OpenTelemetry:Endpoint"];
                if (!string.IsNullOrEmpty(otlpEndpoint))
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                else
                    metrics.AddConsoleExporter();
            });

        return services;
    }
}
```

---

## Custom Business Metrics

### Defining a Meter
```csharp
public static class AppMetrics
{
    private static readonly Meter Meter = new("AppName.Business");

    public static readonly Counter<long> OrdersProcessed =
        Meter.CreateCounter<long>("orders.processed", "orders", "Total orders processed");

    public static readonly Counter<long> PaymentsFailed =
        Meter.CreateCounter<long>("payments.failed", "payments", "Total payment failures");

    public static readonly Histogram<double> OrderProcessingDuration =
        Meter.CreateHistogram<double>("orders.processing_duration", "ms", "Order processing duration");
}
```

### Usage in service code
```csharp
var stopwatch = Stopwatch.StartNew();
// ... process order ...
stopwatch.Stop();

AppMetrics.OrdersProcessed.Add(1, new KeyValuePair<string, object?>("status", "success"));
AppMetrics.OrderProcessingDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
```

---

## Correlation ID Middleware

### Middleware
```csharp
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Set in NLog MappedDiagnosticsLogicalContext for structured logging
        NLog.MappedDiagnosticsLogicalContext.Set("CorrelationId", correlationId);

        await next(context);
    }
}
```

### Propagation to outbound HTTP calls
```csharp
public class CorrelationIdHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (httpContextAccessor.HttpContext?.Items["CorrelationId"] is string correlationId)
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
```

---

## Alert Thresholds Configuration

### appsettings.json
```json
{
  "Alerting": {
    "ErrorRateThreshold": 0.05,
    "LatencyP99ThresholdMs": 2000,
    "HealthCheckFailureCount": 3,
    "CircuitBreakerOpenAlertEnabled": true
  }
}
```

These thresholds are read by monitoring infrastructure — the application exposes the metrics, external tooling evaluates thresholds and triggers alerts.

### Error Alerting Principles

- MUST define alert-worthy conditions: error rate spike, health check failure, latency degradation
- MUST configure thresholds at deployment (appsettings/environment) — MUST NOT hardcode
- MUST instrument: request rate, error rate, p95/p99 latency, queue depth, active connections
- MUST add business metrics per domain (e.g., orders processed, payments settled)
