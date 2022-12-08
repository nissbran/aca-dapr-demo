param name string
param acrname string = 'acr${name}'
param nodeResourceGroup string = 'rg-aks-${name}-nodes'
param location string = resourceGroup().location

param kubernetesVersion string = '1.23.8'

var agentVMSize = 'Standard_D2s_v3'

// Aks rbac roles assigned to the managed identity
var acrPullRole = resourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var managedIdentityOperatorRole = resourceId('Microsoft.Authorization/roleDefinitions', 'f1a07417-d97a-45cb-824c-7a7467783830')

resource acr 'Microsoft.ContainerRegistry/registries@2021-12-01-preview' existing = {
  name: acrname
}

resource aks_uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' = {
  name: 'uai-aks-${name}'
  location: location
}

resource aks_uai_pull_rbac 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: acr
  name: guid(aks_uai.id, acrPullRole, name)
  properties: {
    roleDefinitionId: acrPullRole
    principalId: aks_uai.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource aks_uai_mio_rbac 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: resourceGroup()
  name: guid(aks_uai.id, managedIdentityOperatorRole, name)
  properties: {
    roleDefinitionId: managedIdentityOperatorRole
    principalId: aks_uai.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource aks 'Microsoft.ContainerService/managedClusters@2022-07-02-preview' = {
  name: 'aks-${name}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${aks_uai.id}': {}
    }
  }
  properties: {
    kubernetesVersion: kubernetesVersion
    enableRBAC: true
    dnsPrefix: 'aks-${name}'
    identityProfile: {
      kubeletidentity: {
        resourceId: aks_uai.id
        clientId: aks_uai.properties.clientId
        objectId: aks_uai.properties.principalId
      }
    }

    agentPoolProfiles: [
      {
        name: 'systempool'
        count: 1
        minCount: 1
        maxCount: 3
        mode: 'System'
        vmSize: agentVMSize
        type: 'VirtualMachineScaleSets'
        osType: 'Linux'
        enableAutoScaling: true
        upgradeSettings: {
          maxSurge: '33%'
        }
        nodeTaints: [
          'CriticalAddonsOnly=true:NoSchedule'
        ]
      }
      {
        name: 'workpool'
        count: 1
        minCount: 1
        maxCount: 3
        mode: 'User'
        vmSize: agentVMSize
        type: 'VirtualMachineScaleSets'
        osType: 'Linux'
        enableAutoScaling: true
        upgradeSettings: {
          maxSurge: '33%'
        }
      }
    ]

    servicePrincipalProfile: {
      clientId: 'msi'
    }
    nodeResourceGroup: nodeResourceGroup

    autoUpgradeProfile: {
      upgradeChannel: 'stable'
    }

  }
}
