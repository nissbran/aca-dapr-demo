package credit

import "github.com/google/uuid"

type Credit struct {
	Id           string        `json:"id"`
	Name         string        `json:"name"`
	Transactions []Transaction `json:"transactions"`

	etag            string
	newTransactions []Transaction
}

type Transaction struct {
	Id    string `json:"id"`
	Value int    `json:"value"`
}

func (c *Credit) AddTransaction(value int) {
	transaction := Transaction{Id: uuid.New().String(), Value: value}
	c.Transactions = append(c.Transactions, transaction)
	c.newTransactions = append(c.newTransactions, transaction)
}

func (c *Credit) GetNewTransactions() (transactions []Transaction) {
	return c.newTransactions
}

func (c *Credit) HasNewTransactions() bool {
	return len(c.newTransactions) > 0
}

func (c *Credit) SetEtag(etag string) {
	c.etag = etag
}

func (c *Credit) GetEtag() string {
	return c.etag
}
