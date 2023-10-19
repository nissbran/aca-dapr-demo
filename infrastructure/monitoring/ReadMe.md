# Monitoring

This section contains the guide to deploy the monitoring infrastructure. This will deploy the following components:
* Azure Application Insights
* Azure Log Analytics Workspace
* Managed Prometheus Workspace
* Managed Grafana

## Deploy 

You can choose to deploy the monitoring infrastructure together with the application or separately. The following sections describe how to deploy the monitoring infrastructure separately.

First create an resource group (change location and name if you wish):

```cmd
az group create -l swedencentral -n rg-dapr-monitoring-demo
```

Deploy the Environment. **Choose a unique name for the name parameter (only text without special signs and space, some resources are global unique and will create a uri)**

```cmd
az deployment group create -g rg-dapr-monitoring-demo --template-file monitoring.bicep --parameters name=<Your name>
```
