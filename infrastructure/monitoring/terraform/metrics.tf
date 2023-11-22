
resource "azurerm_monitor_data_collection_rule" "metrics" {
  name                        = "dcr-metrics-${var.cluster_name}"
  resource_group_name         = data.azurerm_resource_group.aks.name
  location                    = data.azurerm_resource_group.aks.location
  data_collection_endpoint_id = azurerm_monitor_data_collection_endpoint.collection.id
  kind                        = "Linux"

  destinations {
    monitor_account {
      monitor_account_id = local.monitor_workspace_id
      name               = "MonitoringAccount1"
    }
  }

  data_flow {
    streams      = ["Microsoft-PrometheusMetrics"]
    destinations = ["MonitoringAccount1"]
  }


  data_sources {
    prometheus_forwarder {
      streams = ["Microsoft-PrometheusMetrics"]
      name    = "PrometheusDataSource"
    }
  }

  description = "DCR for Azure Monitor Metrics Profile (Managed Prometheus)"
  depends_on = [
    azurerm_monitor_data_collection_endpoint.collection
  ]
}

resource "azurerm_monitor_data_collection_rule_association" "dcra" {
  name                    = "dcra-metrics-${var.cluster_name}"
  target_resource_id      = data.azurerm_kubernetes_cluster.akscluster.id
  data_collection_rule_id = azurerm_monitor_data_collection_rule.metrics.id
  description             = "Association of data collection rule. Deleting this association will break the data collection for this AKS Cluster."
  depends_on = [
    azurerm_monitor_data_collection_rule.metrics
  ]
}
