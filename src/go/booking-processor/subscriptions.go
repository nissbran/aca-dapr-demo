package main

import "github.com/dapr/go-sdk/service/common"

var bookingEventSub = &common.Subscription{
	PubsubName: "pubsub",
	Topic:      "bookings",
	Route:      "/booking",
	Match:      `event.data.type == "BookingEvent"`,
	Priority:   2,
}

var bookingSub1 = &common.Subscription{
	PubsubName: "pubsub",
	Topic:      "bookings",
	Route:      "/booking",
	Match:      `event.data.type == "BookingEvent"`,
	Priority:   1,
}

var bookingSub2 = &common.Subscription{
	PubsubName: "pubsub",
	Topic:      "bookings",
	Route:      "/booking",
	Match:      `event.data.type == "BookingEvent"`,
	Priority:   3,
}
