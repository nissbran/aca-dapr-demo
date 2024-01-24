using Aspire.Hosting;
using Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

var daprResourcePath = Path.Combine(builder.AppHostDirectory, "../../../dapr/components/");
var daprComponentOptions = new DaprComponentOptions() { LocalPath = daprResourcePath };

var pubSubComponent = builder.AddDaprComponent("pubsub", "pubsub.redis", daprComponentOptions); 
var bookingStoreComponent = builder.AddDaprComponent("bookingstore", "state.redis", daprComponentOptions);
var creditStoreComponent = builder.AddDaprComponent("creditstore", "state.redis", daprComponentOptions);

var interestRateApi = builder.AddContainer("interest-rate-api", "interest-rate-api", "1.0")
       .WithDaprSidecar("interest-rate-api")
       .WithOtlpExporter()
       .WithEnvironment("SERVICE_NAME", "interest-rate-api")
       .WithEnvironment("GIN_MODE", "debug")
       .WithEnvironment("INSECURE_MODE", "true")
       .WithServiceBinding(80, 5041, "http");

builder.AddProject<Projects.CreditApi>("credit-api")
       .WithDaprSidecar("credit-api")
       .WithReference(pubSubComponent)
       .WithReference(creditStoreComponent);

builder.AddProject<Projects.BookingProcessor>("booking-processor")
       .WithDaprSidecar("booking-processor")
       .WithReference(pubSubComponent)
       .WithReference(bookingStoreComponent);


builder.AddContainer("currency-rate-api", "currency-rate-api", "1.0")
       .WithDaprSidecar("currency-rate-api")
       .WithOtlpExporter()
       .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
       .WithEnvironment("OTEL_SERVICE_NAME", "currency-rate-api")
       .WithEnvironment("OTEL_LOGS_EXPORTER", "otlp")
       .WithEnvironment("OTEL_TRACES_EXPORTER", "otlp")
       .WithEnvironment("OTEL_METRICS_EXPORTER", "otlp")
       .WithEnvironment("JAVA_TOOL_OPTIONS", "-javaagent:opentelemetry-javaagent.jar")
       .WithEnvironment("OTEL_RESOURCE_ATTRIBUTES", "application=currency-rate-api,team=team java");

builder.Build().Run();