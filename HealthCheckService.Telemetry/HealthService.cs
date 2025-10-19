using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthCheckService.Telemetry;

public class HealthService(TelemetryConfiguration telemetryConfiguration)
{
    public const string ReadinessTag = "ready";
    public const string LivenessTag = "live";

    public virtual bool IsStarted { get => ApplicationLifetime?.ApplicationStarted.IsCancellationRequested ?? false; }

    public virtual bool IsStopping { get => ApplicationLifetime?.ApplicationStopping.IsCancellationRequested ?? false; }

    public virtual bool IsStopped { get => ApplicationLifetime?.ApplicationStopped.IsCancellationRequested ?? false; }

    public virtual bool IsStarting
    {
        get => !IsStarted && !IsStopping && !IsStopped;
    }

    public virtual bool IsRunning
    {
        get => IsStarted && !IsStopping && !IsStopped;
    }

    public virtual Exception? LastException { get; set; }
    public virtual bool Degraded { get; set; }
    public virtual bool Failed { get; set; }

    protected virtual IHostApplicationLifetime? ApplicationLifetime { get; set; }

    public virtual void MapEndpoints(WebApplication application, IApplicationBuilder builder, IEndpointRouteBuilder endpoints)
    {
        ApplicationLifetime = application.Lifetime;
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
        {
        });

        endpoints.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains(ReadinessTag)
        });

        endpoints.MapHealthChecks("/healthz/live", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains(LivenessTag)
        });
    }

    public virtual async Task<HealthCheckResult> GetHealth(HealthCheckContext context)
    {
        if (Failed)
            return context.FailedHealthCheckResult(nameof(Failed), LastException);
        else if (Degraded)
            return HealthCheckResult.Degraded(nameof(Degraded), LastException);
        return HealthCheckResult.Healthy();
    }

    public virtual async Task<HealthCheckResult> GetReadinessAsync(HealthCheckContext context)
    {
        if (IsRunning)
            return HealthCheckResult.Healthy();
        return context.FailedHealthCheckResult(nameof(Failed), LastException);
    }

    public virtual async Task<HealthCheckResult> GetLivenessAsync(HealthCheckContext context)
    {
        if (Failed)
            return context.FailedHealthCheckResult(nameof(Failed), LastException);
        else if (Degraded)
            return HealthCheckResult.Degraded(nameof(Degraded), LastException);
        return HealthCheckResult.Healthy();
    }
}

public static class HealthCheckContextExtensions
{
    public static HealthCheckResult FailedHealthCheckResult(this HealthCheckContext context, string? description = null, Exception? exception = null, IReadOnlyDictionary<string, object>? data = null)
    {
        return new HealthCheckResult(context.Registration.FailureStatus, description, exception, data);
    }
}