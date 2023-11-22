data "azurerm_resource_group" "aks" {
  name = "rg-aks1-telemetry-demo"
}

data "azurerm_resource_group" "ops" {
  provider = azurerm.secondary
  name     = "rg-op-telemetry-demo"
}

data "azurerm_kubernetes_cluster" "akscluster" {
  name                = "aks-${var.cluster_name}"
  resource_group_name = data.azurerm_resource_group.aks.name
}

data "azurerm_log_analytics_workspace" "logsws" {
  name                = "logs-${var.cluster_name}"
  resource_group_name = data.azurerm_resource_group.aks.name
}

data "azurerm_log_analytics_workspace" "logssecurity" {
  provider            = azurerm.secondary
  name                = "logs-security"
  resource_group_name = data.azurerm_resource_group.ops.name
}

# data "azurerm_monitor_workspace" "amw" {
#   provider            = azurerm.secondary
#   name                = var.monitor_workspace_name
#   resource_group_name = data.azurerm_resource_group.ops.name
# }
