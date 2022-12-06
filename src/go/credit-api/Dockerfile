FROM golang:1.19 AS builder

WORKDIR /build/

COPY go.mod ./
COPY go.sum ./

RUN go mod download

COPY . .

RUN go build -o credit-api

FROM golang:1.19 AS app
COPY --from=builder /build/credit-api ./

ENV GIN_MODE=release
ENV LISTENING_PORT=80

EXPOSE 80

CMD [ "./credit-api" ]