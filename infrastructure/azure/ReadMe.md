# Deploy to Azure

This deploy requires owner access on subscription and the rights to create service principals in the Azure Active Directory. 

Requirements:
* Azure Cli
* Azure subscription with owner access

```powershell
az extension add --name containerapp
```

First create an resource group (change location and name if you wish):
```powershell
az group create -l northeurope -n rg-aca-dapr-demo-test
```

Deploy the Container Registry. Choose a unique name for the name parameter (only text without special signs and space, some resources are global unique and will create a uri)

```powershell
az deployment group create -g rg-aca-dapr-demo-test --template-file registry.bicep --parameters name=<Your name>
```

Build and push the images to your Container Registry

For .NET:
```powershell
cd ../..
cd src/dotnet/credit-api
az acr build --registry acr<Your name> --image credits/credit-api:0.1 . -f .\Dockerfile
cd ..
cd booking-processor
az acr build --registry acr<Your name> --image credits/booking-processor:0.1 . -f .\Dockerfile
```

```powershell
az deployment group create -g rg-aca-dapr-demo-test --template-file main.bicep --parameters name=<Your name>
```