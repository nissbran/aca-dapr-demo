package credit

import (
	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
)

func InitializeRoutes(versionGroup *gin.RouterGroup) {
	g := versionGroup.Group("credits")

	g.GET(":id", GetCredit)
	g.POST("", CreateCredit)

	g.POST(":id/transactions", AddCreditTransaction)
	g.GET(":id/transactions", GetTransactions)
}

func GetTransactions(c *gin.Context) {

	credit := getCreditById(c.Param("id"))
	if credit == nil {
		c.AbortWithStatus(404)
		return
	}
	c.JSON(200, GetTransactionsResponse{
		Count: len(credit.Transactions),
	})
}

func GetCredit(c *gin.Context) {

	credit := getCreditById(c.Param("id"))
	if credit == nil {
		c.AbortWithStatus(404)
		return
	}
	c.JSON(200, credit)
}

func CreateCredit(c *gin.Context) {
	var requestBody CreateCreditRequest
	if err := c.BindJSON(&requestBody); err != nil {
		c.AbortWithStatus(400)
		return
	}

	credit := Credit{Id: uuid.New().String(), Name: requestBody.Name}

	saveCredit(&credit)

	c.JSON(201, credit.Id)
}

func AddCreditTransaction(c *gin.Context) {
	var requestBody AddTransactionRequest
	if err := c.BindJSON(&requestBody); err != nil {
		c.AbortWithStatus(400)
		return
	}

	credit := getCreditById(c.Param("id"))
	if credit == nil {
		c.AbortWithStatus(404)
		return
	}

	credit.AddTransaction(requestBody.Value)

	if err := saveCredit(credit); err != nil {
		c.AbortWithStatus(409)
		return
	}

	c.JSON(201, nil)
}
