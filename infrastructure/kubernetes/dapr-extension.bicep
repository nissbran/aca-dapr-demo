param name string

resource aks 'Microsoft.ContainerService/managedClusters@2022-07-02-preview' existing = {
  name: 'aks-${name}'
}

resource daprExtension 'Microsoft.KubernetesConfiguration/extensions@2022-04-02-preview' = {
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
