package main

import (
	"github.com/gin-gonic/gin"
	"go.opentelemetry.io/contrib/instrumentation/github.com/gin-gonic/gin/otelgin"
	"log"
)

func initializeRoutes() *gin.Engine {
	router := gin.Default()
	router.Use(otelgin.Middleware(serviceName))

	v1 := router.Group("v1")

	v1.GET("/interest-rates", func(c *gin.Context) {
		log.Println("Interest rate endpoint hit", c.Request.Header)
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
