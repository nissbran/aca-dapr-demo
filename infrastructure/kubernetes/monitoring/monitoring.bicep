param name string
param location string = resourceGroup().location

resource loganalytics_workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'logs${name}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource appinsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appinsights${name}'
  kind: 'web'
  location: location
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: loganalytics_workspace.id
  }
}

resource prometheus_workspace 'Microsoft.Monitor/accounts@2023-04-03' = {
  name: 'prom-workspace-${name}'
  location: location
  
}

resource grafana 'Microsoft.Dashboard/grafana@2022-08-01' = {
  name: 'grafana${name}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Standard'
  }
  properties: {
    autoGeneratedDomainNameLabelScope: 'TenantReuse'
    publicNetworkAccess: 'Enabled'
    grafanaIntegrations: {
      azureMonitorWorkspaceIntegrations: [
        {
          azureMonitorWorkspaceResourceId: prometheus_workspace.id
        }
      ]
    }
  }
}
