

resource "azurerm_monitor_data_collection_rule" "logs" {
  name                        = "dcr-logs-${var.cluster_name}"
  resource_group_name         = data.azurerm_resource_group.aks.name
  location                    = data.azurerm_resource_group.aks.location
  kind                        = "Linux"
  data_collection_endpoint_id = azurerm_monitor_data_collection_endpoint.collection.id

  data_sources {
    extension {
      name           = "ContainerInsightsExtension"
      streams        = ["Microsoft-ContainerLog","Microsoft-ContainerLogV2", "Microsoft-KubeEvents", "Microsoft-KubePodInventory"]
      extension_name = "ContainerInsights"
      extension_json = jsonencode({
        "dataCollectionSettings" : {
          "interval" : "1m",
          "namespaceFilteringMode" : "Include",
          "namespaces" : [
            "frontend"
          ],
          "enableContainerLogV2" : true
        }
      })
    }
  }
  
  destinations {
    log_analytics {
      workspace_resource_id = data.azurerm_log_analytics_workspace.logsws.id
      name                  = "destination-log"
    }
  }

  data_flow {
    streams       = ["Microsoft-ContainerLogV2"]
    destinations  = ["destination-log"]
    #transform_kql = "source | where PodNamespace == 'frontend' or PodNamespace == 'backend'"
    transform_kql = "source | where PodNamespace == 'frontend' or PodNamespace == 'backend' or PodNamespace == 'kafka'"
  }

  data_flow {
    streams      = ["Microsoft-KubeEvents", "Microsoft-KubePodInventory"]
    destinations = ["destination-log"]
  }

  depends_on = [
    azurerm_monitor_data_collection_endpoint.collection
  ]
}

resource "azurerm_monitor_data_collection_rule_association" "logs" {
  name                        = "dcra-logs-${var.cluster_name}"
  target_resource_id          = data.azurerm_kubernetes_cluster.akscluster.id
  data_collection_rule_id     = azurerm_monitor_data_collection_rule.logs.id
  description                 = "Association of data collection rule. Deleting this association will break the data collection for this AKS Cluster."
  depends_on = [
    azurerm_monitor_data_collection_rule.logs
  ]
}

resource "azurerm_monitor_diagnostic_setting" "security_logs" {
  name                           = "diag-logs-security-${var.cluster_name}"
  target_resource_id             = data.azurerm_kubernetes_cluster.akscluster.id
  log_analytics_workspace_id     = data.azurerm_log_analytics_workspace.logssecurity.id
  log_analytics_destination_type = "Dedicated"

  enabled_log {
    category = "kube-audit-admin"
  }

  lifecycle {
    ignore_changes = [ metric ]
  }
}
