using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
        .WriteTo.OpenTelemetry(options =>
        {
            var protocol = OtlpProtocol.GrpcProtobuf;
            if (context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] != null)
            {
                protocol = context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] == "http/protobuf" ? OtlpProtocol.HttpProtobuf : OtlpProtocol.GrpcProtobuf;
            }
            
            var endpoint = $"{context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]}/v1/logs";

            options.Protocol = protocol;
            options.Endpoint = endpoint;
            options.ResourceAttributes = new Dictionary<string, object>()
            {
                ["service.name"] = "booking-processor",
            };
        }));
builder.Services.AddControllers().AddDapr();
builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder
            .AddOtlpExporter()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "booking-processor", serviceVersion: "0.1"))
            .AddHttpClientInstrumentation()
            .AddGrpcClientInstrumentation()
            .AddAspNetCoreInstrumentation();
    })
    .WithMetrics(metricsBuilder =>
    {
        metricsBuilder
            .AddOtlpExporter()
            .AddMeter("booking-processor")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "booking-processor", serviceVersion: "0.1"))
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation();
    });

var app = builder.Build();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();
app.UseHealthChecks("/healthz");
app.Run();