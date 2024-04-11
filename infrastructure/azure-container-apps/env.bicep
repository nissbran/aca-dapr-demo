param name string
param location string = resourceGroup().location

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'acrdapr${name}'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

resource loganalytics_workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'logs${name}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
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

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'stoacc${name}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource fileServices 'Microsoft.Storage/storageAccounts/fileServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

// Service Bus ------------------------------------------------
resource sb_ns 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: 'sb${name}'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }

  resource topic 'topics' = {
    name: 'bookings'
    properties: {
      enablePartitioning: true
      supportOrdering: true
    }

    resource subscription 'subscriptions' = {
      name: 'booking-processor'
      properties: {
        requiresSession: true
        deadLetteringOnFilterEvaluationExceptions: true
        deadLetteringOnMessageExpiration: true
        maxDeliveryCount: 10
      }
    }
  }
}

resource sbSharedKey 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2021-11-01' = {
  name: 'credits'
  parent: sb_ns
  properties: {
    rights: ['Send', 'Listen', 'Manage']
  }
}

// Cosmos DB --------------------------------------------------
resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2023-09-15' = {
  name: 'cosmos${name}'
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    databaseAccountOfferType: 'Standard'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-09-15' = {
  parent: cosmos
  name: 'credits'
  properties: {
    resource: {
      id: 'credits'
    }
  }
}

resource creditstore 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-09-15' = {
  parent: database
  name: 'creditstore'
  properties: {
    resource: {
      id: 'creditstore'
      partitionKey: {
        paths: [
          '/partitionKey'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource bookingstore 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-09-15' = {
  parent: database
  name: 'bookingstore'
  properties: {
    resource: {
      id: 'bookingstore'
      partitionKey: {
        paths: [
          '/partitionKey'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource sqlRoleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2023-04-15' = {
  name: guid('sql-rw-role-definition', cosmos.id)
  parent: cosmos
  properties: {
    roleName: 'application-rw-role'
    type: 'CustomRole'
    assignableScopes: [
      cosmos.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
        ]
      }
    ]
  }
}

// Azure Container Apps ---------------------------------------
resource aca_env 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: 'acaenv${name}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: loganalytics_workspace.properties.customerId
        sharedKey: loganalytics_workspace.listKeys().primarySharedKey
      }
    }
    appInsightsConfiguration: {
      connectionString: appinsights.properties.ConnectionString
    }
    openTelemetryConfiguration: {
      logsConfiguration: {
        destinations: ['appInsights']
      }
      metricsConfiguration: {
        destinations: [] // appInsights not supported yet
      }
      tracesConfiguration: {
        destinations: ['appInsights']
      }
      destinationsConfiguration: {
        otlpConfigurations: []
      }
      // destinationsConfiguration: {
      //   otlpConfigurations: [
      //     {
      //       name: 'dashboard'
      //       endpoint: 'http://dashboard:4317'
      //       insecure: true
      //     }
      //   ]
      // }
    }
    //daprAIConnectionString: appinsights.properties.ConnectionString
    daprAIInstrumentationKey: appinsights.properties.InstrumentationKey
    infrastructureResourceGroup: 'rg-aca-${name}-infra'
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

resource pubsub_component 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  name: 'pubsub'
  parent: aca_env
  properties: {
    componentType: 'pubsub.azure.servicebus.topics'
    version: 'v1'
    initTimeout: '30s'
    metadata: [
      {
        name: 'namespaceName'
        value: '${sb_ns.name}.servicebus.windows.net'
      }
      {
        name: 'maxActiveMessages'
        value: '1'
      }
    ]
    scopes: [
      'credit-api'
      'booking-processor'
    ]
  }
}

resource creditstore_component 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  name: 'creditstore'
  parent: aca_env
  properties: {
    componentType: 'state.azure.cosmosdb'
    version: 'v1'
    initTimeout: '5m'
    metadata: [
      {
        name: 'url'
        value: cosmos.properties.documentEndpoint
      }
      {
        name: 'database'
        value: 'credits'
      }
      {
        name: 'collection'
        value: 'creditstore'
      }
    ]
    scopes: [
      'credit-api'
      'booking-processor'
    ]
  }
}

resource bookingstore_component 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  name: 'bookingstore'
  parent: aca_env
  properties: {
    componentType: 'state.azure.cosmosdb'
    version: 'v1'
    initTimeout: '5m'
    metadata: [
      {
        name: 'url'
        value: cosmos.properties.documentEndpoint
      }
      {
        name: 'database'
        value: 'credits'
      }
      {
        name: 'collection'
        value: 'bookingstore'
      }
    ]
    scopes: [
      'booking-processor'
    ]
  }
}

resource keyvault_component 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  name: 'kv-secretstore'
  parent: aca_env
  properties: {
    componentType: 'secretstores.azure.keyvault'
    version: 'v1'
    initTimeout: '5m'
    metadata: [
      {
        name: 'vaultName'
        value: name
      }
    ]
  }
}

// Aspire Dashboard --------------------------------------------
// For direct access to logs, metrics, and traces.
resource dashboard 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: 'dashboard' 
  location: location
  properties: {
    managedEnvironmentId: aca_env.id
    configuration: {
      activeRevisionsMode: 'single'
      ingress: {
        external: true
        targetPort: 18888
        additionalPortMappings: [
          {
            external: false
            targetPort: 18889
            exposedPort: 4317
          }
        ]
      }
    }
    template: {
      containers: [
        {
          image: 'mcr.microsoft.com/dotnet/nightly/aspire-dashboard:8.0.0-preview.5'
          name: 'dashboard'
          resources:{
            cpu: json('.25')
            memory: '.5Gi'
          }
          env: [
            {
              name: 'DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS'
              value: 'true'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
        rules: []
      }
    }
  }
}
