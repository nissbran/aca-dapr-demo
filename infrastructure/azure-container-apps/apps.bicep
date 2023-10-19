
param name string
param location string = resourceGroup().location

var acrPullRole = resourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var sbDataOwnerRole = resourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')

// Existing resources ---------------------------------------------------------
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: 'acrdapr${name}'
}

resource aca_env 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: 'acaenv${name}'
}

resource sb_ns 'Microsoft.ServiceBus/namespaces@2021-11-01' existing = {
  name: 'sb${name}'
}

resource sbSharedKey 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2021-11-01' existing = {
  name: 'credits'
  parent: sb_ns
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' existing = {
  name: 'cosmos${name}'
}

resource sqlRoleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2023-04-15' existing = {
  name: guid('sql-rw-role-definition', cosmos.id)
  parent: cosmos
}

// Credit api -----------------------------------------------------------------
resource credit_api 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'credit-api'
  location: location
  identity: {
    type: 'SystemAssigned'
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
      secrets: [
        {
          name: 'registry-password'
          value: acr.listCredentials().passwords[0].value
        }
      ]
      registries: [
        {
          username: acr.name
          passwordSecretRef: 'registry-password'
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
            {
              name: 'USE_CONSOLE_LOG_OUTPUT'
              value: 'true'
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

resource credit_api_pull_assignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, credit_api.name, acrPullRole)
  properties: {
    roleDefinitionId: acrPullRole
    principalId: credit_api.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource credit_api_cosmos_assignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(cosmos.id, credit_api.name, sqlRoleDefinition.id)
  parent: cosmos
  properties: {
    roleDefinitionId: sqlRoleDefinition.id
    principalId: credit_api.identity.principalId
    scope: cosmos.id
  }
}

resource credit_api_sb_assignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sb_ns.id, credit_api.name, sbDataOwnerRole)
  properties: {
    roleDefinitionId: sbDataOwnerRole
    principalId: credit_api.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Booking processor ----------------------------------------------------------
resource booking_processor 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'booking-processor'
  location: location
  identity: {
    type: 'SystemAssigned'
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
        {
          name: 'registry-password'
          value: acr.listCredentials().passwords[0].value
        }
      ]
      registries: [
        {
          username: acr.name
          passwordSecretRef: 'registry-password'
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
            {
              name: 'USE_CONSOLE_LOG_OUTPUT'
              value: 'true'
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

resource booking_processor_pull_assignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, booking_processor.name, acrPullRole)
  properties: {
    roleDefinitionId: acrPullRole
    principalId: booking_processor.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource booking_processor_cosmos_assignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(cosmos.id, booking_processor.name, sqlRoleDefinition.id)
  parent: cosmos
  properties: {
    roleDefinitionId: sqlRoleDefinition.id
    principalId: booking_processor.identity.principalId
    scope: cosmos.id
  }
}

resource booking_processor_sb_assignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sb_ns.id, booking_processor.name, sbDataOwnerRole)
  properties: {
    roleDefinitionId: sbDataOwnerRole
    principalId: booking_processor.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Interest rate api ----------------------------------------------------------
resource interest_rate_api 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'interest-rate-api'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: aca_env.id
    workloadProfileName: 'consumption'
    configuration: {
      activeRevisionsMode: 'single'
      dapr: {
        appId: 'interest-rate-api'
        appPort: 80
        enabled: true
      }
      secrets: [
        {
          name: 'registry-password'
          value: acr.listCredentials().passwords[0].value
        }
      ]
      registries: [
        {
          username: acr.name
          passwordSecretRef: 'registry-password'
          server: acr.properties.loginServer
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${acr.name}.azurecr.io/credits/interest-rate-api:0.1'
          name: 'interest-rate-api'
          env: [
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'otel-collector-app'
            }
            {
              name: 'OTEL_EXPORTER_OTLP_PROTOCOL'
              value: 'http/protobuf'
            }
            {
              name: 'INSECURE_MODE'
              value: 'true'
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
      }
    }
  }
}

// 
resource currency_rate_api 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'currency-rate-api'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: aca_env.id
    configuration: {
      activeRevisionsMode: 'single'
      dapr: {
        appId: 'currency-rate-api'
        appPort: 8080
        enabled: true
      }
      secrets: [
        {
          name: 'registry-password'
          value: acr.listCredentials().passwords[0].value
        }
      ]
      registries: [
        {
          username: acr.name
          passwordSecretRef: 'registry-password'
          server: acr.properties.loginServer
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${acr.name}.azurecr.io/credits/currency-rate-api:0.1'
          name: 'currency-rate-api'
          env: [
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'http://otel-collector-app'
            }
            {
              name: 'OTEL_EXPORTER_OTLP_PROTOCOL'
              value: 'http/protobuf'
            }
            {
              name: 'OTEL_SERVICE_NAME'
              value: 'currency-rate-api'
            }
            {
              name: 'OTEL_LOGS_EXPORTER'
              value: 'otlp'
            }
            {
              name: 'OTEL_TRACES_EXPORTER'
              value: 'otlp'
            }
            {
              name: 'OTEL_METRICS_EXPORTER'
              value: 'otlp'
            }
            {
              name: 'OTEL_RESOURCE_ATTRIBUTES'
              value: 'application=currency-rate-api,team=team java'
            }
            {
              name: 'JAVA_TOOL_OPTIONS'
              value: '-javaagent:opentelemetry-javaagent.jar'
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
      }
    }
  }
}
