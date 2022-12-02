package main

import (
	"context"
	"log"
	"math/rand"
	"net/http"
	"time"

	"github.com/dapr/go-sdk/service/common"
	daprd "github.com/dapr/go-sdk/service/http"
)

var bookingSub = &common.Subscription{
	PubsubName: "pubsub",
	Topic:      "bookings",
	Route:      "/booking",
	Match:      `event.data.type == "BookingEvent"`,
	Priority:   1,
}

func main() {
	service := daprd.NewService(":8080")

	if err := service.AddTopicEventHandler(bookingSub, eventHandler); err != nil {
		log.Fatalf("error adding topic subscription: %v", err)
	}

	if err := service.Start(); err != nil && err != http.ErrServerClosed {
		log.Fatalf("server error: %v", err)
	}
}

func eventHandler(ctx context.Context, e *common.TopicEvent) (retry bool, err error) {

	log.Printf("Starting  - Data: %v", e.Data)
	time.Sleep(time.Duration(rand.Intn(5)) * time.Second)
	log.Printf("Processed - Data: %v", e.Data)

	return false, nil
}
