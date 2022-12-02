package main

import (
	"credit-api/modules/credit"

	"github.com/gin-gonic/gin"
)

func initializeRoutes() *gin.Engine {
	router := gin.Default()

	v1 := router.Group("v1")

	credit.InitializeRoutes(v1)

	router.GET("/", func(context *gin.Context) {
		context.String(200, "Test")
	})

	return router
}
