# Deploy to Azure Container Apps

This deploy requires owner access on subscription and the rights to create service principals in the Azure Active Directory. 

Requirements:
* Azure Cli
* Azure subscription with owner access

```cmd
az extension add --name containerapp
```

First create an resource group (change location and name if you wish):
```cmd
az group create -l swedencentral -n rg-aca-dapr-demo
```

Deploy the Environment. **Choose a unique name for the name parameter (only text without special signs and space, some resources are global unique and will create a uri)**

```cmd
az deployment group create -g rg-aca-dapr-demo --template-file env.bicep --parameters name=<Your name>
```
Next step is to build the images: [Build and push the images to your Container Registry](../../README.md#build-and-publish-the-containers-to-azure-container-registry)

Then deploy the container applications:

```cmd
az deployment group create -g rg-aca-dapr-demo --template-file apps.bicep --parameters name=<Your name>
```

To monitor the application deploy the opentelemetry collector:

```cmd
az deployment group create -g rg-aca-dapr-demo --template-file otel-collector.bicep --parameters name=<Your name>
```