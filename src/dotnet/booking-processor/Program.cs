using BookingProcessor.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;

const string appName = "booking-processor";

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
builder.Services.AddControllers().AddDapr();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<EventConsumedMetrics>();

var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName: appName, serviceVersion: "0.1");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder
            .AddOtlpExporter()
            .SetResourceBuilder(resourceBuilder)
            .AddHttpClientInstrumentation()
            .AddGrpcClientInstrumentation()
            .AddAspNetCoreInstrumentation();
    })
    .WithMetrics(metricsBuilder =>
    {
        metricsBuilder
            .AddOtlpExporter()
            .AddConsumedEventsMetrics()
            .SetResourceBuilder(resourceBuilder);
    });

var app = builder.Build();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();
app.UseHealthChecks("/healthz");
app.Run();