using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Roydl;
namespace HealthCheckService.Configuration;

public record HealthEndpointsConfiguration
{
    public HealthEndpointsConfiguration(IConfiguration configuration)
    {
        Endpoints = configuration.GetSection(nameof(Endpoints)).GetChildren().Select(config =>
        {
            var endpointConfig = new HealthEndpointConfiguration() { Key = config.Key };
            config.Bind(endpointConfig);
            return endpointConfig;
        }).ToArray().OrderBy(c => c.Key!, new AlphaNumericComparer<string>()).ToArray();
    }

    public HealthEndpointConfiguration[] Endpoints { get; init; }
}

public record HealthEndpointConfiguration
{
    public string? Key { get; set; }
    public string? Url { get; set; }

    public string? HealthUrl { get; set; } = "/healthz";
    public double HealthInterval { get; set; } = 15.0; // seconds
    public int HealthThreshold { get; set; } = 2;

    public string? ReadinessUrl { get; set; } = "/healthz/ready";
    public double ReadinessInterval { get; set; } = 10.0;
    public int ReadinessThreshold { get; set; } = 3;

    public string? LivenessUrl { get; set; } = "/healthz/live";
    public double LivenessInterval { get; set; } = 30.0;
    public int LivenessThreshold { get; set; } = 2;

    public string? MetricsUrl { get; set; } = "/metrics";
    public double MetricsInterval { get; set; } = 30.0;
}