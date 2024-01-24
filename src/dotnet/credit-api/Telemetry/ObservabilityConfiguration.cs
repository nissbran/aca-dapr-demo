using Azure.Monitor.OpenTelemetry.Exporter;
using CreditApi.Modules.Credit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;

namespace CreditApi.Telemetry;

internal static class ObservabilityConfiguration
{
    internal static bool UsePrometheusEndpoint { get; private set; }
    internal static bool IsSerilogConfigured { get; private set; }

    public static WebApplicationBuilder ConfigureTelemetry(this WebApplicationBuilder builder, string serviceNamespace, string application, string team)
    {
        UsePrometheusEndpoint = builder.Configuration.GetValue<bool>("USE_PROMETHEUS_ENDPOINT");

        var resourceBuilder = ResourceBuilder.CreateDefault();

        var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            builder.Services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(builderOptions =>
                {
                    builderOptions.SetResourceBuilder(resourceBuilder);
                    builderOptions.IncludeFormattedMessage = true;
                    builderOptions.IncludeScopes = false;
                    builderOptions.AddAzureMonitorLogExporter(options => options.ConnectionString = appInsightsConnectionString);
                });
            });
        }
        else
        {
            builder.Host.UseSerilog((context, configuration) =>
            {
                var serilogConfiguration = configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Filter.With<IgnoredEndpointsLogFilter>()
                    .Enrich.FromLogContext();

                if (context.HostingEnvironment.IsDevelopment() || builder.Configuration.GetValue<bool>("USE_CONSOLE_LOG_OUTPUT"))
                {
                    if (builder.Configuration.GetValue<bool>("USE_CONSOLE_JSON_LOG_OUTPUT"))
                    {
                        serilogConfiguration.WriteTo.Console(formatter: new RenderedCompactJsonFormatter());
                    }
                    else
                    {
                        serilogConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Sixteen);
                    }
                }

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    var protocol = context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] == "http/protobuf"
                        ? OtlpProtocol.HttpProtobuf
                        : OtlpProtocol.Grpc;

                    serilogConfiguration.WriteTo.OpenTelemetry(options =>
                    {
                        options.Protocol = protocol;
                        options.Endpoint = protocol == OtlpProtocol.HttpProtobuf ? $"{otlpEndpoint}/v1/logs" : otlpEndpoint;
                        options.ResourceAttributes = resourceBuilder.Build().Attributes.ToDictionary();
                    });
                }
                else
                {
                    serilogConfiguration
                        .Enrich.WithProperty("namespace", serviceNamespace)
                        .Enrich.WithProperty("application", application)
                        .Enrich.WithProperty("team", team);
                }

                var seqEndpoint = context.Configuration["SEQ_ENDPOINT"];
                if (!string.IsNullOrEmpty(seqEndpoint))
                {
                    serilogConfiguration.WriteTo.Seq(seqEndpoint);
                }

                IsSerilogConfigured = true;
            });
        }

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracingBuilder =>
            {
                if (!string.IsNullOrEmpty(appInsightsConnectionString))
                    tracingBuilder.AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = appInsightsConnectionString;
                        // This is sampled by hashing the traceid with hash seed 5381
                        // This is the same in the java sampler as well
                        //options.SamplingRatio = 0.1f;
                    });

                if (!string.IsNullOrEmpty(otlpEndpoint))
                    tracingBuilder.AddOtlpExporter();
                
                tracingBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddAspNetCoreInstrumentation(opt => opt.Filter = TraceEndpointsFilter);
            })
            .WithMetrics(metricsBuilder =>
            {
                if (UsePrometheusEndpoint)
                {
                    metricsBuilder.AddPrometheusExporter();
                }

                if (!string.IsNullOrEmpty(appInsightsConnectionString))
                    metricsBuilder.AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = appInsightsConnectionString;
                        // This is sampled by hashing the traceid with hash seed 5381
                        // This is the same in the java sampler as well
                        //options.SamplingRatio = 0.1f;
                    });
                
                if (!string.IsNullOrEmpty(otlpEndpoint))
                    metricsBuilder.AddOtlpExporter();

                metricsBuilder
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