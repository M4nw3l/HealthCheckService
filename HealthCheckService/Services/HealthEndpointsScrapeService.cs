
using HealthCheckService.Configuration;
using HealthCheckService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;

namespace HealthCheckService.Services;

public class HealthEndpointsScrapeService(HealthEndpointsService healthEndpointsService, HttpClient client, Meter meter, ILogger<HealthEndpointsScrapeService> logger) : BackgroundService
{
    private Counter<int> EndpointRequestCounter = meter.CreateCounter<int>(nameof(EndpointRequestCounter));
    private Counter<int> HealthCounter = meter.CreateCounter<int>(nameof(HealthCounter));
    private Counter<int> ReadinessCounter = meter.CreateCounter<int>(nameof(ReadinessCounter));
    private Counter<int> LivenessCounter = meter.CreateCounter<int>(nameof(LivenessCounter));
    private Counter<int> MetricsCounter = meter.CreateCounter<int>(nameof(MetricsCounter));

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting background scraping task...");
        var configuration = healthEndpointsService.Configuration;
        var states = new Dictionary<string, EndpointScrapeState>();
        var intervals = new HashSet<double>();
        foreach (var endpoint in configuration.Endpoints)
        {
            Uri.TryCreate(endpoint.Url, UriKind.Absolute, out var endpointUri);
            var state = new EndpointScrapeState(endpoint, endpointUri, DateTime.Now);
            var endpointIntervals = new double[] { endpoint.ReadinessInterval, endpoint.HealthInterval, endpoint.LivenessInterval };

            states.Add(endpoint.Key!, state);
            foreach (var endpointInterval in endpointIntervals)
                intervals.Add(endpointInterval);

        }
        var interval = TimeSpan.FromSeconds(intervals.Min());
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        while (!cancellationToken.IsCancellationRequested)
        {
            var tasks = new List<Task>();
            foreach (var state in states.Values)
            {
                await semaphore.WaitAsync();
                var task = ExecuteEndpointAsync(state, cancellationToken).ContinueWith(t => semaphore.Release());
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            await Task.Delay(interval, cancellationToken);
        }
        logger.LogInformation("Background scraping task shutdown.");
    }

    protected virtual async Task ExecuteEndpointAsync(EndpointScrapeState state, CancellationToken cancellationToken)
    {
        logger.LogInformation("{EndpointKey}: Updating health and metrics endpoints.", state.Endpoint.Key);
        var endpoint = state.Endpoint;
        var uri = state.Uri;
        var timestamp = DateTime.Now;
        var tasks = new List<Task>();
        Task<HealthEndpointHealthCheckResult>? healthTask = null;
        if (timestamp >= state.NextHealth)
        {
            healthTask = Task.Run(async () =>
            {
                var health = await GetEndpointHealthStatusAsync(uri, endpoint.HealthUrl);
                state.NextHealth = DateTime.Now + TimeSpan.FromSeconds(endpoint.HealthInterval);
                HealthCounter.Add(1, new TagList() { { "key", endpoint.Key }, { "url", health.Uri?.ToString() } });
                return health;
            });
            tasks.Add(healthTask);
        }
        Task<HealthEndpointHealthCheckResult>? readinessTask = null;
        if (timestamp >= state.NextReadiness)
        {
            readinessTask = Task.Run(async () =>
            {
                var readiness = await GetEndpointHealthStatusAsync(uri, endpoint.ReadinessUrl);
                state.NextReadiness = DateTime.Now + TimeSpan.FromSeconds(endpoint.ReadinessInterval);
                ReadinessCounter.Add(1, new TagList() { { "key", endpoint.Key }, { "url", readiness.Uri?.ToString() } });
                return readiness;
            });
            tasks.Add(readinessTask);
        }
        Task<HealthEndpointHealthCheckResult>? livenessTask = null;
        if (timestamp >= state.NextLiveness)
        {
            livenessTask = Task.Run(async () =>
            {
                var liveness = await GetEndpointHealthStatusAsync(uri, endpoint.LivenessUrl);
                state.NextLiveness = DateTime.Now + TimeSpan.FromSeconds(endpoint.LivenessInterval);
                LivenessCounter.Add(1, new TagList() { { "key", endpoint.Key }, { "url", liveness.Uri?.ToString() } });
                return liveness;
            });
            tasks.Add(livenessTask);
        }
        Task<HealthEndpointMetricsResult>? metricsTask = null;
        if (timestamp >= state.NextMetrics)
        {
            metricsTask = Task.Run(async () =>
            {
                var metrics = await GetEndpointMetricsAsync(uri, endpoint.MetricsUrl);
                state.NextMetrics = DateTime.Now + TimeSpan.FromSeconds(endpoint.MetricsInterval);
                MetricsCounter.Add(1, new TagList() { { "key", endpoint.Key }, { "url", metrics.Uri?.ToString() } });
                return metrics;
            });
            tasks.Add(metricsTask);
        }
        await Task.WhenAll(tasks);
        var healthEndpointState = HealthEndpointState.New(healthTask?.Result, readinessTask?.Result, livenessTask?.Result, metricsTask?.Result);
        await healthEndpointsService.SetStateAsync(endpoint, healthEndpointState);
    }

    private Dictionary<string, object> GetHealthCheckResultData()
    {
        return new Dictionary<string, object>()
        {
            { "Timestamp", DateTime.Now },
        };
    }

    protected virtual async Task<HealthEndpointHealthCheckResult> GetEndpointHealthStatusAsync(Uri? uri, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new HealthEndpointHealthCheckResult(HealthCheckResult.Healthy("Endpoint disabled.", data: GetHealthCheckResultData()));
        if (!Uri.TryCreate(path, uri == null ? UriKind.Absolute : UriKind.RelativeOrAbsolute, out var endpointUri))
            return new HealthEndpointHealthCheckResult(HealthCheckResult.Degraded($"Invalid endpoint uri path '{path}' ('{uri?.ToString()}')"));
        if (!endpointUri.IsAbsoluteUri && !Uri.TryCreate(uri, path, out endpointUri))
            return new HealthEndpointHealthCheckResult(HealthCheckResult.Degraded($"Invalid endpoint uri path '{path}' ('{uri?.ToString()}')", data: GetHealthCheckResultData()));
        return new HealthEndpointHealthCheckResult(await GetHealthStatusAsync(endpointUri), endpointUri);
    }
    protected virtual async Task<HealthEndpointMetricsResult> GetEndpointMetricsAsync(Uri? uri, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new HealthEndpointMetricsResult("Endpoint disabled.");
        if (!Uri.TryCreate(path, uri == null ? UriKind.Absolute : UriKind.RelativeOrAbsolute, out var endpointUri))
            return new HealthEndpointMetricsResult($"Invalid endpoint uri/path '{path}' ('{uri?.ToString()}').");
        if (!endpointUri.IsAbsoluteUri && (uri == null || !Uri.TryCreate(uri, path, out endpointUri)))
            return new HealthEndpointMetricsResult($"Invalid endpoint uri/path '{path}' ('{uri?.ToString()}').");
        return new HealthEndpointMetricsResult(await GetMetricsAsync(endpointUri), endpointUri);
    }

    protected virtual async Task<HealthCheckResult> GetHealthStatusAsync(Uri endpointUri)
    {
        logger.LogInformation("GetHealthStatusAsync('{HealthEndpointUri}')", endpointUri);
        try
        {
            var response = await EndpointRequestAsync(endpointUri);
            var statusText = response.Body;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (string.Equals(statusText, "Degraded"))
                    return HealthCheckResult.Degraded(statusText, data: GetHealthCheckResultData());
                return HealthCheckResult.Unhealthy(statusText, data: GetHealthCheckResultData());
            }
            return HealthCheckResult.Healthy(statusText, data: GetHealthCheckResultData());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetHealthStatusAsync('{HealthEndpointUri}') failed.", endpointUri);
            return HealthCheckResult.Unhealthy(exception: ex, data: GetHealthCheckResultData());
        }
    }

    protected virtual async Task<string> GetMetricsAsync(Uri endpointUri)
    {
        logger.LogInformation("GetMetricsAsync('{MetricsEndpointUri}')", endpointUri);
        try
        {
            var response = await EndpointRequestAsync(endpointUri);
            if (response.StatusCode == HttpStatusCode.OK)
                return response.Body;
            return $"Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetMetricsAsync('{MetricsEndpointUri}') failed.", endpointUri);
            return $"Error: {ex.Message}";
        }
    }

    protected virtual async Task<(HttpStatusCode StatusCode, string Body)> EndpointRequestAsync(Uri endpointUri)
    {
        logger.LogInformation("EndpointRequestAsync('{EndpointRequestUri}'", endpointUri);
        try
        {
            using var response = await client.GetAsync(endpointUri);
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
            var body = await reader.ReadToEndAsync();
            body = body.Trim();
            EndpointRequestCounter.Add(1, new TagList() { { "uri", endpointUri.ToString() }, { "status", (int)response.StatusCode }, { "length", body.Length } });
            logger.LogInformation("EndpointRequestAsync('{EndpointRequestUri}' (status:{EndpointRequestStatusCode}, length {EndpointRequestBodyLength}).", endpointUri, response.StatusCode, body.Length);
            return (response.StatusCode, body.Trim());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EndpointRequestAsync('{EndpointRequestUri}' failed.", endpointUri);
            throw;
        }
    }

    protected record EndpointScrapeState
    {
        public HealthEndpointConfiguration Endpoint { get; }
        public Uri? Uri { get; }
        public DateTime NextHealth { get; set; }
        public DateTime NextReadiness { get; set; }
        public DateTime NextLiveness { get; set; }
        public DateTime NextMetrics { get; set; }

        public EndpointScrapeState(HealthEndpointConfiguration endpoint, Uri? uri, DateTime timestamp)
        {
            Endpoint = endpoint;
            Uri = uri;
            NextHealth = timestamp;
            NextReadiness = timestamp;
            NextLiveness = timestamp;
            NextMetrics = timestamp;
        }
    }
}