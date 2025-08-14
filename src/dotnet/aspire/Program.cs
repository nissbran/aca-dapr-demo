
var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddAzureRedis("redisState").WithAccessKeyAuthentication().RunAsContainer();

// local development still uses dapr redis state container
var bookingStoreStateStore = builder.AddDaprStateStore("bookingstore")
       .WithReference(redis);
var creditStoreStateStore = builder.AddDaprStateStore("creditstore")
       .WithReference(redis);

var pubSub = builder.AddDaprPubSub("pubsub")
       .WithMetadata("redisHost", "localhost:6379")
       .WaitFor(redis);
// var daprResourcePath = Path.Combine(builder.AppHostDirectory, "../../../dapr/components/");
// var daprComponentOptions = new DaprComponentOptions() { LocalPath = daprResourcePath };
//
// var pubSubComponent = builder.AddDaprComponent("pubsub", "pubsub.redis", daprComponentOptions); 
// var bookingStoreComponent = builder.AddDaprComponent("bookingstore", "state.redis", daprComponentOptions);
// var creditStoreComponent = builder.AddDaprComponent("creditstore", "state.redis", daprComponentOptions);

builder.AddProject<Projects.CreditApi>("credit-api")
       .WithDaprSidecar("credit-api")
       .WithReference(pubSub)
       .WithReference(creditStoreStateStore);

builder.AddProject<Projects.BookingProcessor>("booking-processor")
       .WithDaprSidecar("booking-processor")
       .WithReference(pubSub)
       .WithReference(bookingStoreStateStore);

builder.AddContainer("interest-rate-api", "interest-rate-api", "1.0")
       .WithDaprSidecar()
       .WithOtlpExporter()
       .WithEnvironment("GIN_MODE", "debug")
       .WithEnvironment("INSECURE_MODE", "true")
       .WithHttpEndpoint(5041, 80);

builder.AddContainer("currency-rate-api", "currency-rate-api", "1.0")
       .WithDaprSidecar("currency-rate-api")
       .WithOtlpExporter()
       .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
       .WithEnvironment("OTEL_LOGS_EXPORTER", "otlp")
       .WithEnvironment("OTEL_TRACES_EXPORTER", "otlp")
       .WithEnvironment("OTEL_METRICS_EXPORTER", "otlp")
       .WithEnvironment("JAVA_TOOL_OPTIONS", "-javaagent:opentelemetry-javaagent.jar")
       .WithHttpEndpoint(5042, 8080);

builder.Build().Run();