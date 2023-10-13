FROM golang:1.20 AS builder

WORKDIR /build/

COPY go.mod ./
COPY go.sum ./

RUN go mod download

COPY . .

RUN go build -o interest-rate-api

FROM golang:1.20 AS app
COPY --from=builder /build/interest-rate-api ./

ENV GIN_MODE=release
ENV LISTENING_PORT=80
ENV APP_VERSION=$DOCKER_TAG

EXPOSE 80

CMD [ "./interest-rate-api" ]