using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthCheckService.Telemetry;

public class MetricsService(Meter meter, TelemetryConfiguration telemetryConfiguration)
{
    public Meter Meter { get; } = meter;
    public virtual void MapEndpoints(WebApplication application, IApplicationBuilder builder, IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPrometheusScrapingEndpoint("/metrics");
    }
}
