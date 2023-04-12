using Carter;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Azure.Monitor.OpenTelemetry.Exporter;
using CreditApi.Telemetry;
using Microsoft.ApplicationInsights.Extensibility;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TelemetryConfiguration>(_ =>
{
    var configuration = TelemetryConfiguration.CreateDefault();
    configuration.ConnectionString = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_CONNECTION_STRING");
    configuration.TelemetryInitializers.Add(new RoleNameTelemetryInitializer("credit-api"));
    return configuration;
});
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
        .WriteTo.ApplicationInsights(services.GetRequiredService<TelemetryConfiguration>(), new CustomTraceTelemetryConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();
builder.Services.AddDaprClient();

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
            .AddSource("credit-api")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "credit-api", serviceVersion: "0.1"))
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
            .AddMeter("credit-api")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "credit-api", serviceVersion: "0.1"))
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
        // .AddRuntimeInstrumentation()
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseSerilogRequestLogging();
app.MapCarter();
app.Run();