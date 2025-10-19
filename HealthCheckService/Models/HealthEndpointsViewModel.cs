using HealthCheckService.Configuration;
using HealthCheckService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;

namespace HealthCheckService.Models;

public class HealthEndpointsViewModel
{
    private readonly HealthEndpointsService healthEndpointsService;

    public IEnumerable<HealthEndpointViewModel> Endpoints { get; }

    public HealthEndpointsViewModel(HealthEndpointsService healthEndpointsService)
    {
        this.healthEndpointsService = healthEndpointsService ?? throw new ArgumentNullException(nameof(healthEndpointsService));
        Endpoints = healthEndpointsService.Endpoints.Select(item =>
        {
            var config = item.Key;
            var state = item.Value;
            return new HealthEndpointViewModel(config, state);
        });
    }
}

public class HealthEndpointViewModel(HealthEndpointConfiguration config, HealthEndpointState state)
{
    public string Key { get; init; } = config.Key ?? string.Empty;
    public string Url { get; init; } = config.Url ?? string.Empty;
    public string Health { get; init; } = GetHealthResultText(state.Health);
    public string Readiness { get; init; } = GetHealthResultText(state.Readiness);
    public string Liveness { get; init; } = GetHealthResultText(state.Liveness);
    public string Metrics { get; init; } = GetMetricsText(state.Metrics);

    public static string GetHealthResultText(HealthEndpointHealthCheckResult? endpointResult, bool includeTimestamp = true)
    {
        var timestamp = endpointResult?.Timestamp ?? DateTime.Now;
        var result = endpointResult?.Result;
        var text = result?.Description ?? "Unknown";
        if (!includeTimestamp)
            return text;
        return string.Format("{0} ({1:dd/MM/yyyy HH:mm:ss})",text , timestamp);
    }

    public static string GetMetricsText(HealthEndpointMetricsResult? metricsResult)
    {
        var builder = new StringBuilder();
        builder.AppendFormat("Last Updated: {0:dd/MM/yyyy HH:mm:ss}", metricsResult?.Timestamp ?? DateTime.Now)
               .AppendLine();
        builder.AppendLine(metricsResult?.Metrics ?? "Metrics unavailable.");
        return builder.ToString();
    }
}