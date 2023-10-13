using Azure.Monitor.OpenTelemetry.Exporter;
using CreditApi.Modules.Credit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;

namespace CreditApi.Telemetry;

public static class ObservabilityConfiguration
{
    public static WebApplicationBuilder ConfigureTelemetry(this WebApplicationBuilder builder, string serviceNamespace, string application, string team)
    {
        var version = builder.Configuration["APP_VERSION"] ?? "0.1";
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(
            serviceName: application,
            serviceNamespace: serviceNamespace,
            serviceVersion: version);

        builder.Host.UseSerilog((context, configuration) =>
        {
            var serilogConfiguration = configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.WithProperty("namespace", serviceNamespace)
                .Enrich.WithProperty("application", application)
                .Enrich.WithProperty("team", team)
                .Filter.With<IgnoredEndpointsLogFilter>()
                .Enrich.FromLogContext();

            if (context.HostingEnvironment.IsDevelopment() || builder.Configuration.GetValue<bool>("USE_CONSOLE_LOG_OUTPUT"))
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
                        ["service.name"] = application,
                        ["service.namespace"] = serviceNamespace,
                        ["service.team"] = team,
                        ["service.version"] = builder.Configuration["APP_VERSION"] ?? "0.1"
                    };
                });
            }

            var seqEndpoint = context.Configuration["SEQ_ENDPOINT"];
            if (!string.IsNullOrEmpty(seqEndpoint))
            {
                serilogConfiguration.WriteTo.Seq(seqEndpoint);
            }
        });
        
        var appInsightsConnectionString = builder.Configuration["APPLICATION_INSIGHTS_CONNECTION_STRING"];

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracingBuilder =>
            {
                if (!string.IsNullOrEmpty(appInsightsConnectionString))
                    tracingBuilder.AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = appInsightsConnectionString;
                        options.SamplingRatio = 0.1f; // This is sampled by hashing the traceid
                    });

                tracingBuilder
                    .AddOtlpExporter()
                    .SetResourceBuilder(resourceBuilder)
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddAspNetCoreInstrumentation(opt => opt.Filter = TraceEndpointsFilter);
            })
            .WithMetrics(metricsBuilder =>
            {
                metricsBuilder
                    .AddOtlpExporter()
                    .SetResourceBuilder(resourceBuilder)
                    .AddCreditMetrics()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation();
            });

        return builder;
    }

    private static bool TraceEndpointsFilter(HttpContext httpContext)
    {
        try
        {
            return httpContext.Request.Path.Value != "/healthz" && httpContext.Request.Path.Value != "/metrics";
        }
        catch
        {
            return true;
        }
    }
}