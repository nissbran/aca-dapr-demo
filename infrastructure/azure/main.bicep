param name string
param location string = resourceGroup().location

var acrPullRole = resourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

resource acr 'Microsoft.ContainerRegistry/registries@2021-12-01-preview' existing = {
  name: 'acr${name}'
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-01-01' = {
  name: 'sto${name}'
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

resource loganalytics_workspace 'Microsoft.OperationalInsights/workspaces@2020-08-01' = {
  name: 'logs${name}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      searchVersion: 1
      legacy: 0
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
    Flow_Type: 'Redfield'
    Request_Source: 'CustomDeployment'
    WorkspaceResourceId: loganalytics_workspace.id
  }
}

resource eventHubNs 'Microsoft.EventHub/namespaces@2021-11-01' = {
  name: 'eventhub${name}ns'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    isAutoInflateEnabled: false
    maximumThroughputUnits: 0
  }
}

resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = {
  parent: eventHubNs
  name: 'bookings'
  properties: {
    messageRetentionInDays: 7
    partitionCount: 30
  }
}

resource eventHubConsumerBookingProcessor 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2022-01-01-preview' = {
  parent: eventHub
  name: 'booking-processor'
}

resource eventHubSharedKey 'Microsoft.EventHub/namespaces/authorizationRules@2021-11-01' = {
  name: 'string'
  parent: eventHubNs
  properties: {
    rights: [ 'Send', 'Listen', 'Manage' ]
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' = {
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

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-08-15' = {
  parent: cosmos
  name: 'credits'
  properties: {
    resource: {
      id: 'credits'
    }
  }
}

resource creditstore 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
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

resource bookingstore 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
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

resource aca_env 'Microsoft.App/managedEnvironments@2022-06-01-preview' = {
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
    daprAIConnectionString: appinsights.properties.ConnectionString
  }
}

resource pubsub_component 'Microsoft.App/managedEnvironments/daprComponents@2022-06-01-preview' = {
  name: 'pubsub'
  parent: aca_env
  properties: {
    componentType: 'pubsub.azure.eventhubs'
    version: 'v1'
    initTimeout: '30s'
    metadata: [
      {
        name: 'connectionString'
        value: eventHubSharedKey.listKeys().primaryConnectionString
      }
      {
        name: 'storageAccountName'
        value: storageAccount.name
      }
      {
        name: 'storageAccountKey'
        value: storageAccount.listKeys().keys[0].value
      }
      {
        name: 'storageContainerName'
        value: 'eventhub-subscriptions'
      }
    ]
    scopes: [
      'credit-api', 'booking-processor'
    ]
  }
}

resource creditstore_component 'Microsoft.App/managedEnvironments/daprComponents@2022-06-01-preview' = {
  name: 'creditstore'
  parent: aca_env
  properties: {
    componentType: 'state.azure.cosmosdb'
    version: 'v1'
    initTimeout: '30s'
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

resource bookingstore_component 'Microsoft.App/managedEnvironments/daprComponents@2022-06-01-preview' = {
  name: 'bookingstore'
  parent: aca_env
  properties: {
    componentType: 'state.azure.cosmosdb'
    version: 'v1'
    initTimeout: '30s'
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

// Credit api

resource credit_api_uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' = {
  name: 'id-credit-api'
  location: location
}

resource credit_api_uaiRbac 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, credit_api_uai.id, acrPullRole)
  properties: {
    roleDefinitionId: acrPullRole
    principalId: credit_api_uai.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource credit_api 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'credit-api'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${credit_api_uai.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: aca_env.id
    configuration: {
      activeRevisionsMode: 'single'
      ingress: {
        external: true
        targetPort: 80
      }
      dapr: {
        enabled: true
        appId: 'credit-api'
      }
      registries: [
        {
          identity: credit_api_uai.id
          server: acr.properties.loginServer
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${acr.name}.azurecr.io/credits/credit-api:1.0'
          name: 'credit-api'
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

// Booking processor

resource booking_processor_uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' = {
  name: 'id-booking-processor'
  location: location
}

resource booking_processor_uaiRbac 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, booking_processor_uai.id, acrPullRole)
  properties: {
    roleDefinitionId: acrPullRole
    principalId: booking_processor_uai.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource booking_processor 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'booking-processor'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${booking_processor_uai.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: aca_env.id
    configuration: {
      activeRevisionsMode: 'single'
      dapr: {
        appId: 'booking-processor'
        appPort: 80
        enabled: true
      }
      secrets: [
        {
          name: 'eventhub-connection-string'
          value: '${eventHubSharedKey.listKeys().primaryConnectionString};EntityPath=${eventHub.name}'
        }
        {
          name: 'blobstorage-connection-string'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
      ]
      registries: [
        {
          identity: booking_processor_uai.id
          server: acr.properties.loginServer
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${acr.name}.azurecr.io/credits/booking-processor:1.0'
          name: 'booking-processor'
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
        rules: [
          {
            name: 'eventhub-scale-rule'
            custom: {
              type: 'azure-eventhub'
              auth: [
                {
                  secretRef: 'eventhub-connection-string'
                  triggerParameter: 'connection'
                }
                {
                  secretRef: 'blobstorage-connection-string'
                  triggerParameter: 'storageConnection'
                }
              ]
              metadata: {
                consumerGroup: 'booking-processor'
                unprocessedEventThreshold: '64'
                checkpointStrategy: 'dapr' // KEDA 2.9 feature
                blobContainer: 'eventhub-subscriptions'
              }
            }
          }
        ]
      }
    }
  }
}
