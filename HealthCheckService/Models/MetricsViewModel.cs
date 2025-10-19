namespace HealthCheckService.Models;

public class MetricsViewModel(HealthEndpointViewModel endpointViewModel)
{
    public HealthEndpointViewModel Endpoint { get; } = endpointViewModel;
    public string Metrics { get => Endpoint.Metrics; }
}
