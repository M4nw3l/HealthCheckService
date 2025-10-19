namespace HealthCheckService.Models;

public class HomeViewModel(HealthEndpointsViewModel healthEndpointsViewModel)
{
    public HealthEndpointsViewModel HealthEndpoints { get; } = healthEndpointsViewModel;
}
