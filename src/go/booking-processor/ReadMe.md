# booking-processor
Start with dapr:
```powershell
dapr run --app-id booking-processor --app-port 8080 --components-path ..\..\dapr\components -- go run booking-processor
```