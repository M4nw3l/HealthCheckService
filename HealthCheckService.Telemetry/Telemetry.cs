using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HealthCheckService.Telemetry;

public class TelemetryConfiguration
{
    public int HealthPort { get; set; } = 9090;
    public int MetricsPort { get; set; } = 9090;

    public List<string> Meters { get; } = new List<string>();

    public Action<MeterProviderBuilder>? ConfigureMeterProvider { get; set; }
}

public class ApplicationReadinessHealthCheck(HealthService healthService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (healthService.IsRunning)
            return HealthCheckResult.Healthy();
        return new HealthCheckResult(context.Registration.FailureStatus);
    }
}

public class ApplicationLivenessHealthCheck(HealthService healthService) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return healthService.GetLivenessAsync(context);
    }
}


public static class TelemetryExtensions
{
    public static WebApplicationBuilder AddTelemetry(this WebApplicationBuilder builder, Action<TelemetryConfiguration>? configure = null)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null)
            entryAssembly = Assembly.GetCallingAssembly();
        var executingAssembly = Assembly.GetExecutingAssembly();
        var configurationFactory = () =>
        {
            var configuration = new TelemetryConfiguration();
            configure?.Invoke(configuration);
            return configuration;
        };
        var configuration = configurationFactory.Invoke();
        builder.Services.TryAddTransient(sp => configurationFactory.Invoke());
        builder.Services.TryAddSingleton<HealthService>();
        var healthChecksBuilder = builder.Services.AddHealthChecks();

        builder.Services.TryAddSingleton<ApplicationReadinessHealthCheck>();
        healthChecksBuilder.AddCheck<ApplicationReadinessHealthCheck>(nameof(ApplicationReadinessHealthCheck), null, [HealthService.ReadinessTag]);
        builder.Services.TryAddSingleton<ApplicationLivenessHealthCheck>();
        healthChecksBuilder.AddCheck<ApplicationLivenessHealthCheck>(nameof(ApplicationLivenessHealthCheck), null, [HealthService.LivenessTag]);

        builder.Services.AddMetrics();
        var meterName = entryAssembly.GetName().Name!;
        builder.Services.AddOpenTelemetry().WithMetrics(builder =>
        {
            //add standard .net runtime, asp.net core and process meters
            builder.AddRuntimeInstrumentation();
            builder.AddAspNetCoreInstrumentation();
            builder.AddProcessInstrumentation();

            //automatic meters configurtion for aiming for entry assembly (but settles for a calling assembly assuming it will come from DI container initialisation)
            //executingAssembly would include any custom 'universal' meters provided with this telemetry library itself.
            var meterNames = configuration.Meters.Concat([meterName, executingAssembly.GetName().Name!])
                                                 .Where(meter => !string.IsNullOrWhiteSpace(meter))
                                                 .Distinct()
                                                 .ToArray();
            builder.AddMeter(meterNames);
            builder.AddPrometheusExporter();
            configuration.ConfigureMeterProvider?.Invoke(builder);
        });
        builder.Services.TryAddSingleton<Meter>(sp => sp.GetRequiredService<IMeterFactory>().Create(meterName));
        builder.Services.TryAddSingleton<MetricsService>();

        return builder;
    }

    public static WebApplication MapTelemetry(this WebApplication application)
    {
        var configuration = application.Services.GetRequiredService<TelemetryConfiguration>();
        var healthPort = configuration.HealthPort;
        var metricsPort = configuration.MetricsPort;
        var healthService = application.Services.GetRequiredService<HealthService>();
        var metricsService = application.Services.GetRequiredService<MetricsService>();

        if (healthPort == metricsPort)
        { // maps a branched pipeline for health and metrics on same port
            var port = healthPort;
            application.MapWhen(context => context.Connection.LocalPort == port, builder =>
            {
                builder.UseRouting();
                builder.UseEndpoints(endpoints =>
                {
                    healthService.MapEndpoints(application, builder, endpoints);
                    metricsService.MapEndpoints(application, builder, endpoints);
                });
            });
            return application;
        }
        //map separate branched pipelines for health port and metrics port respectively
        application.MapWhen(context => context.Connection.LocalPort == healthPort, builder =>
        {
            builder.UseRouting();
            builder.UseEndpoints(endpoints => healthService.MapEndpoints(application, builder, endpoints));
        });
        application.MapWhen(context => context.Connection.LocalPort == metricsPort, builder =>
        {
            builder.UseRouting();
            builder.UseEndpoints(endpoints => metricsService.MapEndpoints(application, builder, endpoints));
        });
        return application;
    }
}
