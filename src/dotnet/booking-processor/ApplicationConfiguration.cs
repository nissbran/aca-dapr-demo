using BookingProcessor.Telemetry;

namespace BookingProcessor;

internal static class ApplicationConfiguration
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers().AddDapr();
        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton<EventConsumedMetrics>();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseCloudEvents();
        app.MapSubscribeHandler();
        app.MapControllers();
        app.UseHealthChecks("/healthz");
        
        return app;
    }
}