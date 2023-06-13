
param name string
param location string = resourceGroup().location

var acrPullRole = resourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

// Existing resources ---------------------------------------------------------
resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' existing = {
  name: 'acr${name}'
}

resource aca_env 'Microsoft.App/managedEnvironments@2022-11-01-preview' existing = {
  name: 'acaenv${name}'
}

resource sb_ns 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: 'sb${name}'
}

resource sbSharedKey 'Microsoft.ServiceBus/namespaces/authorizationRules@2021-11-01' existing = {
  name: 'credits'
  parent: sb_ns
}

// Credit api -----------------------------------------------------------------
resource credit_api_uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
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

resource credit_api 'Microsoft.App/containerApps@2022-10-01' = {
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
          image: '${acr.name}.azurecr.io/credits/credit-api:0.1'
          name: 'credit-api'
          env: [
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'http://otel-collector-app'
            }
            {
              name: 'OTEL_EXPORTER_OTLP_PROTOCOL'
              value: 'http/protobuf'
            }
          ]
          resources:{
            cpu: json('.25')
            memory: '.5Gi'
          }
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

// Booking processor ----------------------------------------------------------
resource booking_processor_uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
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

resource booking_processor 'Microsoft.App/containerApps@2022-10-01' = {
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
          name: 'sb-connection-string'
          value: sbSharedKey.listKeys().primaryConnectionString
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
          image: '${acr.name}.azurecr.io/credits/booking-processor:0.1'
          name: 'booking-processor'
          env: [
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'http://otel-collector-app'
            }
            {
              name: 'OTEL_EXPORTER_OTLP_PROTOCOL'
              value: 'http/protobuf'
            }
          ]
          resources:{
            cpu: json('.25')
            memory: '.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'sb-scale-rule'
            custom: {
              type: 'azure-servicebus'
              auth: [
                {
                  secretRef: 'sb-connection-string'
                  triggerParameter: 'connection'
                }
              ]
              metadata: {
                topicName: 'bookings'
                subscriptionName: 'booking-processor'
                queueLength: '64'
                // consumerGroup: 'booking-processor'
                // unprocessedEventThreshold: '64'
                // checkpointStrategy: 'blobMetadata'
                // blobContainer: 'eventhub-subscriptions'
              }
            }
          }
        ]
      }
    }
  }
}
