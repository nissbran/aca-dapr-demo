package credit

type CreateCreditRequest struct {
	Name string `json:"name" binding:"required"`
}

type AddTransactionRequest struct {
	Value int `json:"value" binding:"required"`
}

type GetTransactionsResponse struct {
	Count int `json:"count"`
}
