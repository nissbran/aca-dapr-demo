param name string
param location string = resourceGroup().location

resource acr 'Microsoft.ContainerRegistry/registries@2021-12-01-preview' = {
  name: 'acr${name}'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    adminUserEnabled: true
  }
}
