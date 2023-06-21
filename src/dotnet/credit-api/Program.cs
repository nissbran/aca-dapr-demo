using Azure.Monitor.OpenTelemetry.Exporter;
using Carter;
using CreditApi.Modules.Credit;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog.Sinks.OpenTelemetry;

const string appName = "credit-api";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    var serilogConfiguration = configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("Application", appName)
        .Enrich.FromLogContext();

    if (context.HostingEnvironment.IsDevelopment() || builder.Configuration["USE_CONSOLE_LOG_OUTPUT"] == "true")
        serilogConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Sixteen);

    var otlpEndpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
    if (!string.IsNullOrEmpty(otlpEndpoint))
    {
        var protocol = context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] == "http/protobuf"
            ? OtlpProtocol.HttpProtobuf
            : OtlpProtocol.Grpc;
        serilogConfiguration.WriteTo.OpenTelemetry(options =>
        {
            options.Protocol = protocol;
            options.Endpoint = protocol == OtlpProtocol.HttpProtobuf ? $"{otlpEndpoint}/v1/logs" : otlpEndpoint;
            options.ResourceAttributes = new Dictionary<string, object>()
            {
                ["service.name"] = appName,
            };
        });
    }

    var seqEndpoint = context.Configuration["SEQ_ENDPOINT"];
    if (!string.IsNullOrEmpty(seqEndpoint))
    {
        serilogConfiguration.WriteTo.Seq(seqEndpoint);
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();
builder.Services.AddDaprClient();
builder.Services.AddHealthChecks();

builder.Services.AddCreditModule();

var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName: appName, serviceVersion: "0.1");
var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
            tracingBuilder.AddAzureMonitorTraceExporter(options => options.ConnectionString = appInsightsConnectionString);	

        tracingBuilder
            .AddOtlpExporter()
            .SetResourceBuilder(resourceBuilder)
            .AddHttpClientInstrumentation()
            .AddGrpcClientInstrumentation()
            .AddAspNetCoreInstrumentation();
    })
    .WithMetrics(metricsBuilder =>
    {
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
            metricsBuilder.AddAzureMonitorMetricExporter(options => options.ConnectionString = appInsightsConnectionString);	

        metricsBuilder
            .AddOtlpExporter()
            .AddCreditMetrics()
            .SetResourceBuilder(resourceBuilder)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHealthChecks("/healthz");
app.UseSerilogRequestLogging();
app.MapCarter();
app.Run();