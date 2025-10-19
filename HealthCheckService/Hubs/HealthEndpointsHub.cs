using Microsoft.AspNetCore.SignalR;

using HealthCheckService.Services;
using HealthCheckService.Models;
using Microsoft.VisualStudio.Threading;

namespace HealthCheckService.Hubs;

public class HealthEndpointsHub : Hub
{
    private readonly HealthEndpointsService endpointsService;
    public HealthEndpointsHub(HealthEndpointsService endpointsService)
    {
        this.endpointsService = endpointsService ?? throw new ArgumentNullException(nameof(endpointsService));
        this.endpointsService.EndpointStateChanged += EndpointsService_EndpointStateChanged;
    }

    private void EndpointsService_EndpointStateChanged(object? sender, HealthEndpointStateChangedEventArgs e)
    {
        var key = e.Endpoint.Key;
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException();

        var healthChanged = e.Previous.Health.Timestamp != e.Current.Health.Timestamp;
        var readinessChanged = e.Previous.Readiness.Timestamp != e.Current.Readiness.Timestamp;
        var livenessChanged = e.Previous.Liveness.Timestamp != e.Current.Liveness.Timestamp;
        var metricsChanged = e.Previous.Metrics.Timestamp != e.Current.Metrics.Timestamp;
        if (healthChanged || readinessChanged || livenessChanged)
        {
            EndpointStatusUpdatedAsync(key,
                      HealthEndpointViewModel.GetHealthResultText(e.Current.Health),
                      HealthEndpointViewModel.GetHealthResultText(e.Current.Readiness),
                      HealthEndpointViewModel.GetHealthResultText(e.Current.Liveness)).Forget();
        }
        if (metricsChanged)
            EndpointMetricsUpdatedAsync(key, HealthEndpointViewModel.GetMetricsText(e.Current.Metrics)).Forget();
    }

    public async Task EndpointStatusUpdatedAsync(string key, string health, string readiness, string liveness)
    {
        await Clients.All.SendAsync(nameof(EndpointStatusUpdatedAsync), key, health, readiness, liveness);
    }

    public async Task EndpointMetricsUpdatedAsync(string key, string metrics)
    {
        await Clients.All.SendAsync(nameof(EndpointMetricsUpdatedAsync), key, metrics);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            endpointsService.EndpointStateChanged -= EndpointsService_EndpointStateChanged;
        base.Dispose(disposing);
    }
}
