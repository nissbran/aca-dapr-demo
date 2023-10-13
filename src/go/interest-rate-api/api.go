package main

import (
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/trace"
	"log"

	"github.com/gin-gonic/gin"
	"go.opentelemetry.io/contrib/instrumentation/github.com/gin-gonic/gin/otelgin"
	"go.opentelemetry.io/otel/baggage"
)

func initializeRoutes() *gin.Engine {
	router := gin.Default()
	router.Use(otelgin.Middleware(serviceName))

	v1 := router.Group("v1")

	v1.GET("/interest-rates", func(c *gin.Context) {
		var creditId = baggage.FromContext(c.Request.Context()).Member("creditId").Value()
		log.Println("Interest rate endpoint hit creditId: ", creditId)
		span := trace.SpanFromContext(c.Request.Context())
		span.SetAttributes(attribute.String("creditId", creditId))
		c.JSON(200, InterestRate{InterestRate: 0.22})
	})

	router.GET("/", func(c *gin.Context) {
		log.Println("Endpoint hit", c.Request.Header)
		c.String(200, "Ok")
	})

	return router
}

type InterestRate struct {
	InterestRate float64 `json:"interest_rate"`
}
