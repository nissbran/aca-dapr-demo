param name string
param location string = resourceGroup().location
param utcValue string = utcNow()
param azureMonitorIngestionUrl string

// Existing resources ----------------------------------------------------------
resource aca_env 'Microsoft.App/managedEnvironments@2022-10-01' existing = {
  name: 'acaenv${name}'
}

resource appinsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: 'appinsights${name}'
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-01-01' existing = {
  name: 'stoacc${name}'
}

resource fileServices 'Microsoft.Storage/storageAccounts/fileServices@2022-09-01' existing = {
  parent: storageAccount
  name: 'default'
}

// Otel configuration ----------------------------------------------------------
resource configShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2022-05-01' = {
  name: 'otel-config'
  parent: fileServices
  properties: {
    accessTier: 'Hot'
    enabledProtocols: 'SMB'
    shareQuota: 1024
  }
}

resource otelConfigShare 'Microsoft.App/managedEnvironments/storages@2022-03-01' = {
  name: 'otelconfig'
  parent: aca_env
  properties: {
    azureFile: {
      accountKey: storageAccount.listKeys().keys[0].value
      accessMode: 'ReadOnly'
      accountName: storageAccount.name
      shareName: configShare.name
    }
  }
}

resource deploymentScriptApp 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'deployscript-upload-otel-app-${utcValue}'
  location: location
  kind: 'AzureCLI'
  properties: {
    azCliVersion: '2.26.1'
    timeout: 'PT5M'
    retentionInterval: 'PT1H'
    environmentVariables: [
      {
        name: 'AZURE_STORAGE_ACCOUNT'
        value: storageAccount.name
      }
      {
        name: 'AZURE_STORAGE_KEY'
        secureValue: storageAccount.listKeys().keys[0].value
      }
      {
        name: 'CONTENT'
        value: loadTextContent('otel-collector-config/otel-collector-app.yaml')
      }
    ]
    scriptContent: 'echo "$CONTENT" > otel-collector-app.yaml && az storage file upload --source otel-collector-app.yaml -s ${configShare.name}'
  }
}

// Otel collector --------------------------------------------------------------
resource otel_collector_uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-otel-collector'
  location: location
}

resource otel_collector_metrics_app 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'otel-collector-app'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${otel_collector_uai.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: aca_env.id
    configuration: {
      activeRevisionsMode: 'single'
      ingress: {
        external: false
        targetPort: 4318
      }
    }
    template: {
      
      containers: [
        {
          image: 'otel/opentelemetry-collector-contrib:0.78.0'
          name: 'otel-collector'
          args: [
            '--config=/etc/otelcol/otel-collector-app.yaml'
          ]
          env: [
            {
              name: 'APPLICATION_INSIGHTS_INSTRUMENTATION_KEY'
              value: appinsights.properties.InstrumentationKey
            }
          ]
          volumeMounts: [
            {
              volumeName: 'otel-collector-config'
              mountPath: '/etc/otelcol'
            }
          ]
          resources:{
            cpu: json('.25')
            memory: '.5Gi'
          }
        }
        {
          image: 'mcr.microsoft.com/azuremonitor/prometheus/promdev/prom-remotewrite:prom-remotewrite-20230505.1'
          name: 'prom-remotewrite'
          env: [
           {
            name: 'INGESTION_URL'
            value: azureMonitorIngestionUrl
           }
           {
            name: 'LISTENING_PORT'
            value: '8081'
           }
           {
            name: 'IDENTITY_TYPE'
            value: 'userAssigned'
           }
           {
            name: 'CLUSTER'
            value: aca_env.name
           }
           {
            name: 'AZURE_CLIENT_ID'
            value: otel_collector_uai.properties.clientId
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
      volumes: [
        {
          name: 'otel-collector-config'
          storageType: 'AzureFile'
          storageName: 'otelconfig'
        }
      ]
    }
  }
  dependsOn: [
    otelConfigShare
    deploymentScriptApp
  ]
}
