using Azure.Monitor.OpenTelemetry.Exporter;
using BookingProcessor.Telemetry;
using Microsoft.ApplicationInsights.Extensibility;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TelemetryConfiguration>(_ =>
{
    var configuration = TelemetryConfiguration.CreateDefault();
    configuration.ConnectionString = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_CONNECTION_STRING");
    configuration.TelemetryInitializers.Add(new RoleNameTelemetryInitializer("booking-processor"));
    return configuration;
});
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
        .WriteTo.ApplicationInsights(services.GetRequiredService<TelemetryConfiguration>(), new CustomTraceTelemetryConverter()));
builder.Services.AddControllers().AddDapr();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder
            //.AddConsoleExporter()
            // .AddAzureMonitorTraceExporter(options =>
            // {
            //     options.ConnectionString =
            //         Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_CONNECTION_STRING");
            // })
            //.AddZipkinExporter()
            .AddOtlpExporter()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "booking-processor", serviceVersion: "0.1"))
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation(options => options.Filter = context => !context.Request.Path.StartsWithSegments("/metrics"));
    })
    .WithMetrics(metricsBuilder =>
    {
        metricsBuilder
            //.AddConsoleExporter()
            // .AddAzureMonitorMetricExporter(options =>
            // {
            //     options.ConnectionString =
            //         Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_CONNECTION_STRING");
            // })
            .AddPrometheusExporter()
            .AddMeter("booking-processor")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "booking-processor", serviceVersion: "0.1"))
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
        // .AddRuntimeInstrumentation()
    });

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();
app.Run();