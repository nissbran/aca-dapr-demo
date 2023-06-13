param name string
param location string = resourceGroup().location

resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
  name: 'acr${name}'
  location: location
  sku: {
    name: 'Basic'
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
    Flow_Type: 'Redfield'
    Request_Source: 'CustomDeployment'
    WorkspaceResourceId: loganalytics_workspace.id
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-01-01' = {
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

resource fileServices 'Microsoft.Storage/storageAccounts/fileServices@2022-09-01' = {
  parent: storageAccount
  name: 'default'
}

// Service Bus ------------------------------------------------
resource sb_ns 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
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

resource sbSharedKey 'Microsoft.ServiceBus/namespaces/authorizationRules@2021-11-01' = {
  name: 'credits'
  parent: sb_ns
  properties: {
    rights: [ 'Send', 'Listen', 'Manage' ]
  }
}

// Cosmos DB --------------------------------------------------
resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2022-11-15' = {
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

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-11-15' = {
  parent: cosmos
  name: 'credits'
  properties: {
    resource: {
      id: 'credits'
    }
  }
}

resource creditstore 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-11-15' = {
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

resource bookingstore 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-11-15' = {
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

resource aca_env 'Microsoft.App/managedEnvironments@2022-11-01-preview' = {
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
    daprAIInstrumentationKey: appinsights.properties.InstrumentationKey
    daprAIConnectionString: appinsights.properties.ConnectionString
  }
}

resource pubsub_component 'Microsoft.App/managedEnvironments/daprComponents@2022-10-01' = {
  name: 'pubsub'
  parent: aca_env
  properties: {
    componentType: 'pubsub.azure.servicebus.topics'
    version: 'v1'
    initTimeout: '30s'
    metadata: [
      {
        name: 'connectionString'
        value: sbSharedKey.listKeys().primaryConnectionString
      }
      {
        name: 'maxActiveMessages'
        value: '1'
      }
    ]
    scopes: [
      'credit-api', 'booking-processor'
    ]
  }
}

resource creditstore_component 'Microsoft.App/managedEnvironments/daprComponents@2022-10-01' = {
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
        name: 'masterKey'
        value: cosmos.listKeys().primaryMasterKey
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
      'credit-api', 'booking-processor'
    ]
  }
}

resource bookingstore_component 'Microsoft.App/managedEnvironments/daprComponents@2022-10-01' = {
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
        name: 'masterKey'
        value: cosmos.listKeys().primaryMasterKey
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
