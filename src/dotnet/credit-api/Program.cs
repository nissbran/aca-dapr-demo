using Carter;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
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
                ["service.name"] = "credit-api",
            };
        }));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();
builder.Services.AddDaprClient();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder
            .AddOtlpExporter()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "credit-api", serviceVersion: "0.1"))
            .AddHttpClientInstrumentation()
            .AddGrpcClientInstrumentation()
            .AddAspNetCoreInstrumentation(options => options.Filter = context => !context.Request.Path.StartsWithSegments("/metrics"));
    })
    .WithMetrics(metricsBuilder =>
    {
        metricsBuilder
            .AddOtlpExporter()
            .AddMeter("credit-api")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "credit-api", serviceVersion: "0.1"))
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation();
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.MapCarter();
app.Run();