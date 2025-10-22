using HealthCheckService.Configuration;
using HealthCheckService.Hubs;
using HealthCheckService.Models;
using HealthCheckService.Services;
using HealthCheckService.Telemetry;

using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddTelemetry();

    builder.Services.AddSerilog();
    builder.Services.AddSignalR();

    builder.Services.AddSingleton<HealthEndpointsConfiguration>();
    builder.Services.AddSingleton<HealthEndpointsService>();
    builder.Services.AddSingleton<HealthEndpointsScrapeService>();
    builder.Services.AddHttpClient<HealthEndpointsScrapeService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<HealthEndpointsScrapeService>());
    builder.Services.AddSingleton<HealthEndpointsHub>();

    builder.Services.AddTransient<HealthEndpointsViewModel>();
    builder.Services.AddTransient<HomeViewModel>();
    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddControllersWithViews();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();
    app.MapTelemetry();
    // Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseExceptionHandler("/Home/Error");

    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.MapHub<HealthEndpointsHub>("/hub");
    app.MapControllers();
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}