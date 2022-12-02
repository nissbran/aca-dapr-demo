FROM golang:1.19 AS builder

WORKDIR /build/

COPY go.mod ./
COPY go.sum ./

RUN go mod download

COPY . .

RUN go build -o booking-processor

FROM golang:1.19 AS app
COPY --from=builder /build/booking-processor ./

EXPOSE 8080

CMD [ "./booking-processor" ]