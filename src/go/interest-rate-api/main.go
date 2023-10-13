package main

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracegrpc"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracehttp"
	"go.opentelemetry.io/otel/propagation"
	"google.golang.org/grpc/credentials"

	"go.opentelemetry.io/otel/sdk/resource"
	sdktrace "go.opentelemetry.io/otel/sdk/trace"
)

var (
	serviceName  = "interest-rate-api"
	version      = os.Getenv("APP_VERSION")
	collectorURL = os.Getenv("OTEL_EXPORTER_OTLP_ENDPOINT")
	protocol     = os.Getenv("OTEL_EXPORTER_OTLP_PROTOCOL")
	insecure     = os.Getenv("INSECURE_MODE")
)

func main() {
	handler := initializeRoutes()

	startServerWithCleanShutdown(handler)
}

func startServerWithCleanShutdown(handler http.Handler) {
	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
	defer stop()

	tracerCleanup := initTracer()

	port, ok := os.LookupEnv("LISTENING_PORT")
	if !ok {
		port = "5041"
	}

	log.Println("Starting server on port", port)

	address := fmt.Sprintf("localhost:%v", port)

	srv := &http.Server{
		Addr:    address,
		Handler: handler,
	}

	// Initializing the server in a goroutine so that
	// it won't block the graceful shutdown handling below
	go func() {
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatalf("listen: %s\n", err)
		}
	}()

	// Listen for the interrupt signal.
	<-ctx.Done()

	// Restore default behavior on the interrupt signal and notify user of shutdown.
	stop()

	log.Println("shutting down gracefully, press Ctrl+C again to force")

	// The context is used to inform the server it has 5 seconds to finish
	// the request it is currently handling
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	if tracerCleanup != nil {
		if err := tracerCleanup(ctx); err != nil {
			log.Fatal("Tracer cleanup failed: ", err)
		}
	}
	if err := srv.Shutdown(ctx); err != nil {
		log.Fatal("Server forced to shutdown: ", err)
	}

	log.Println("Server exiting")
}

func initTracer() func(context.Context) error {

	if len(collectorURL) == 0 {
		log.Println("No collector URL provided, skipping tracer initialization")
		return nil
	}

	var client otlptrace.Client
	if protocol == "http/protobuf" {
		if len(insecure) > 0 {
			client = otlptracehttp.NewClient(otlptracehttp.WithInsecure(), otlptracehttp.WithEndpoint(collectorURL), otlptracehttp.WithURLPath("v1/traces"), otlptracehttp.WithCompression(otlptracehttp.NoCompression))
		} else {
			client = otlptracehttp.NewClient(otlptracehttp.WithEndpoint(collectorURL), otlptracehttp.WithCompression(otlptracehttp.NoCompression))
		}
	} else {
		secureOption := otlptracegrpc.WithTLSCredentials(credentials.NewClientTLSFromCert(nil, ""))
		if len(insecure) > 0 {
			secureOption = otlptracegrpc.WithInsecure()
		}
		client = otlptracegrpc.NewClient(secureOption, otlptracegrpc.WithEndpoint(collectorURL))
	}
	exporter, err := otlptrace.New(context.Background(), client)

	if err != nil {
		log.Fatal(err)
	}
	resources, err := resource.New(
		context.Background(),
		resource.WithAttributes(
			attribute.String("service.namespace", "credits"),
			attribute.String("service.name", serviceName),
			attribute.String("service.version", version),
			attribute.String("service.team", "go team"),
			attribute.String("library.language", "go"),
		),
	)
	if err != nil {
		log.Printf("Could not set resources: ", err)
	}

	otel.SetTracerProvider(
		sdktrace.NewTracerProvider(
			sdktrace.WithSampler(sdktrace.AlwaysSample()),
			sdktrace.WithBatcher(exporter),
			sdktrace.WithResource(resources),
		),
	)
	otel.SetTextMapPropagator(propagation.NewCompositeTextMapPropagator(propagation.TraceContext{}, propagation.Baggage{}))

	return exporter.Shutdown
}
