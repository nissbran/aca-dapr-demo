# aca-dapr-demo
Demo container app showcasing Dapr in both Go lang, .NET and Java. The app showcase an app that service transactions on a bank credit account where the order is important. The purpose is to show how to use Dapr to build a portable app that can run on any cloud provider. It also shows how to monitor the application with OpenTelemetry and the OpenTelemetry Collector.

Prerequisites
* .NET 7 SDK
* Azure Cli
* Docker
* Dapr Cli
* Go SDK
* Java SDK

## Running the app locally 

### Build and run with docker-compose

Then build the images and run the containers with Docker compose:

```bash
docker-compose build
docker-compose up
```

To stop the containers:

```bash
docker-compose down
```

### Local observability

In the docker compose environment, the OpenTelemetry Collector acts as the hub for all telemetry. The collector is configured to forward telemetry to Zipkin, Prometheus and Seq. All running locally in Docker containers.

* To see traces in Zipkin go: http://localhost:9411
* To see metrics in Prometheus go: http://localhost:9090
* To see logs in Seq go: http://localhost:5341

### Use Application Insights locally

If you want to configure the apps to send directly to Application Insights, create a .env file with the following content:

```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=<your app insights connection string>
APPLICATIONINSIGHTS_INSTRUMENTATION_KEY='<your app insights instrumentation key>'
```

For java container (currency-rate-api) you also need to switch to the Application Insights agent in the compose file.
Switch from this:
```bash
JAVA_TOOL_OPTIONS: "-javaagent:opentelemetry-javaagent.jar"
OTEL_RESOURCE_ATTRIBUTES: "application=currency-rate-api,team=team java"
# APPLICATIONINSIGHTS_CONNECTION_STRING: ${APPLICATIONINSIGHTS_CONNECTION_STRING}
# JAVA_TOOL_OPTIONS: "-javaagent:applicationinsights-agent-3.4.17.jar"
```

To this:
```bash
#JAVA_TOOL_OPTIONS: "-javaagent:opentelemetry-javaagent.jar"
#OTEL_RESOURCE_ATTRIBUTES: "application=currency-rate-api,team=team java"
APPLICATIONINSIGHTS_CONNECTION_STRING: ${APPLICATIONINSIGHTS_CONNECTION_STRING}
JAVA_TOOL_OPTIONS: "-javaagent:applicationinsights-agent-3.4.17.jar"
```

## Publish to Azure

This demo can be published to either Azure Container Apps or Azure Kubernetes Service. The following sections describe how to publish to each of these services.

[Deploy to Azure Container Apps](infrastructure/azure-container-apps/Readme.md)

## Build and publish the containers to Azure Container Registry

```powershell
$ENV:ACR="your_acr_name"
az acr build --registry $ENV:ACR --image credits/credit-api:0.1 src/dotnet/credit-api/.
az acr build --registry $ENV:ACR --image credits/booking-processor:0.1 src/dotnet/booking-processor/.
az acr build --registry $ENV:ACR --image credits/interest-rate-api:0.1 src/go/interest-rate-api/.
az acr build --registry $ENV:ACR --image credits/currency-rate-api:0.1 src/java/currency-rate-api/.
```

## Architecture

The demo is built using Dapr. That means that each service is a Dapr app. The Dapr sidecar is injected into each container and is responsible for the communication between the services. This means that it is possible to change the infrastructure without changing the code. For example, it is possible to change the pub/sub implementation from Kafka to Azure Service Bus without changing the code.

