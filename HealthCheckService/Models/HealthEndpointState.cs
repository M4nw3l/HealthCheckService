using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckService.Models;

public record HealthEndpointState
{
    private HealthEndpointState()
    {
    }

    public HealthEndpointHealthCheckResult Health { get; init; } = HealthEndpointHealthCheckResult.Empty;
    public HealthEndpointHealthCheckResult Readiness { get; init; } = HealthEndpointHealthCheckResult.Empty;
    public HealthEndpointHealthCheckResult Liveness { get; init; } = HealthEndpointHealthCheckResult.Empty;

    public HealthEndpointMetricsResult Metrics { get; init; } = HealthEndpointMetricsResult.Empty;

    public HealthEndpointState Combine(HealthEndpointState other)
    {
        return Combine(other.Health, other.Readiness, other.Liveness, other.Metrics);
    }

    public HealthEndpointState Combine(HealthEndpointHealthCheckResult? health = null, HealthEndpointHealthCheckResult? readiness = null, HealthEndpointHealthCheckResult? liveness = null, HealthEndpointMetricsResult? metrics = null)
    {
        if (health == HealthEndpointHealthCheckResult.Empty)
            health = null;
        if (readiness == HealthEndpointHealthCheckResult.Empty)
            readiness = null;
        if (liveness == HealthEndpointHealthCheckResult.Empty)
            liveness = null;
        if (metrics == HealthEndpointMetricsResult.Empty)
            metrics = null;
        return new HealthEndpointState()
        {
            Health = health ?? Health,
            Readiness = readiness ?? Readiness,
            Liveness = liveness ?? Liveness,
            Metrics = metrics ?? Metrics,
        };
    }

    public HealthEndpointState Copy()
    {
        return new HealthEndpointState()
        {
            Health = Health,
            Readiness = Readiness,
            Liveness = Liveness,
            Metrics = Metrics
        };
    }

    public static HealthEndpointState Empty { get; } = new HealthEndpointState();

    public static HealthEndpointState New(HealthEndpointHealthCheckResult? health = null, HealthEndpointHealthCheckResult? rediness = null, HealthEndpointHealthCheckResult? liveness = null, HealthEndpointMetricsResult? metrics = null)
    {
        return Empty.Combine(health, rediness, liveness, metrics);
    }
}

public record HealthEndpointResult
{
    public Uri? Uri { get; }
    public DateTime Timestamp { get; }

    public HealthEndpointResult(Uri? uri = null, DateTime? timestamp = null)
    {
        Uri = uri;
        Timestamp = timestamp ?? DateTime.Now;
    }
}

public record HealthEndpointHealthCheckResult : HealthEndpointResult
{
    public HealthCheckResult Result { get; }
    public HealthEndpointHealthCheckResult(HealthCheckResult result, Uri? uri = null, DateTime? timestamp = null) : base(uri, timestamp)
    {
        Result = result;
    }
    private HealthEndpointHealthCheckResult() : base(null, DateTime.MinValue)
    {
    }

    public static HealthEndpointHealthCheckResult Empty { get; } = new HealthEndpointHealthCheckResult();
}

public record HealthEndpointMetricsResult : HealthEndpointResult
{
    public string Metrics { get; }
    public HealthEndpointMetricsResult(string metrics, Uri? uri = null, DateTime? timestamp = null) : base(uri, timestamp)
    {
        Metrics = metrics;
    }
    private HealthEndpointMetricsResult() : base(null, DateTime.MinValue)
    {
        Metrics = string.Empty;
    }
    public static HealthEndpointMetricsResult Empty { get; } = new HealthEndpointMetricsResult();
}