package credit

import (
	"testing"
)

func TestCredit_AddTransaction(t *testing.T) {
	credit := Credit{Id: "test", Name: "test"}

	credit.AddTransaction(231)

	if credit.HasNewTransactions() == false {
		t.Error()
	}
}
