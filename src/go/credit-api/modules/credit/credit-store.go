package credit

import (
	"context"
	"encoding/json"
	dapr "github.com/dapr/go-sdk/client"
	"log"
)

func getCreditById(id string) *Credit {

	client, err := dapr.NewClient()
	if err != nil {
		panic(err)
	}

	item, err := client.GetState(context.Background(), "creditstore", id, nil)

	if err != nil {
		log.Println(err.Error())
		return nil
	}
	if item == nil || item.Value == nil || string(item.Value) == "" {
		log.Println("Item is null")
		return nil
	}
	credit := Credit{}
	json.Unmarshal(item.Value, &credit)
	credit.SetEtag(item.Etag)
	return &credit
}

func saveCredit(credit *Credit) error {
	client, err := dapr.NewClient()
	if err != nil {
		return err
	}

	data, _ := json.Marshal(credit)
	err = client.SaveStateWithETag(context.Background(), "creditstore", credit.Id, data, credit.GetEtag(), nil,
		dapr.WithConcurrency(dapr.StateConcurrencyFirstWrite))
	if err != nil {
		log.Println(err.Error())
		return err
	}

	if credit.HasNewTransactions() {
		newTransactions := credit.GetNewTransactions()
		for i := 0; i < len(newTransactions); i++ {
			booking := BookingEvent{CreditId: credit.Id, Value: newTransactions[i].Value, Type: "BookingEvent", ETag: credit.GetEtag()}
			err = client.PublishEvent(context.Background(), "pubsub", "bookings", booking,
				dapr.PublishEventWithMetadata(map[string]string{
					"partitionKey": credit.Id,
				}))
			if err != nil {
				log.Println(err.Error())
				return err
			}
		}
	}
	return nil
}
