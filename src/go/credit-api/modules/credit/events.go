package credit

type BookingEvent struct {
	CreditId string `json:"creditId"`
	Value    int    `json:"value"`
	Type     string `json:"type"`
	ETag     string `json:"etag"`
}
