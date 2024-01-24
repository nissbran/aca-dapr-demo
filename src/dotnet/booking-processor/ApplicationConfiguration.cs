using BookingProcessor.Telemetry;

namespace BookingProcessor;

internal static class ApplicationConfiguration
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers().AddDapr();
        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton<EventConsumedMetrics>();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.UseServiceDiscovery();
        });

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseCloudEvents();
        app.MapSubscribeHandler();
        app.MapControllers();
        app.UseHealthChecks("/healthz");
        if (ObservabilityConfiguration.UsePrometheusEndpoint)
        {
            app.MapPrometheusScrapingEndpoint().RequireHost("*:9090");
        }
        
        return app;
    }
}