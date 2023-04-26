param name string

resource aks 'Microsoft.ContainerService/managedClusters@2023-02-01' existing = {
  name: 'aks-${name}'
}

resource daprExtension 'Microsoft.KubernetesConfiguration/extensions@2022-11-01' = {
  name: 'dapr'
  scope: aks
  properties: {
    extensionType: 'Microsoft.Dapr'
    autoUpgradeMinorVersion: true
    releaseTrain: 'Stable'
    configurationSettings: {
      'global.ha.enabled': 'false'
    }
    scope: {
      cluster: {
        releaseNamespace: 'dapr-system'
      }
    }
    configurationProtectedSettings: {}
  }
}
