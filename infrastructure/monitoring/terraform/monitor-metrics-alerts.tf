# resource "azurerm_monitor_metric_alert" "node_cpu_usage_percentage" {
#   name                = "alert-node-cpu-usage-percentage-${data.azurerm_kubernetes_cluster.akscluster.name}"
#   resource_group_name = data.azurerm_resource_group.aks.name
#   scopes              = [data.azurerm_kubernetes_cluster.akscluster.id]
#   description         = "Aggregated average CPU utilization measured in percentage across the cluster"

#   severity      = 3
#   window_size   = "PT5M"
#   frequency     = "PT5M"
#   auto_mitigate = false

#   criteria {
#     metric_namespace = "Microsoft.ContainerService/managedClusters"
#     metric_name      = "node_cpu_usage_percentage"
#     aggregation      = "Average"
#     operator         = "GreaterThan"
#     threshold        = 95.0
#   }

# #   action {
# #     action_group_id = azurerm_monitor_action_group.main.id
# #   }
# }

resource "azurerm_monitor_metric_alert" "node_memory_working_set_percentage" {
  name                = "alert-node-memory-orking-set-percentage-${data.azurerm_kubernetes_cluster.akscluster.name}"
  resource_group_name = data.azurerm_resource_group.aks.name
  scopes              = [data.azurerm_kubernetes_cluster.akscluster.id]
  description         = "Container working set memory used in percent"

  severity      = 3
  window_size   = "PT5M"
  frequency     = "PT5M"
  auto_mitigate = false

  criteria {
    metric_namespace = "Microsoft.ContainerService/managedClusters"
    metric_name      = "node_memory_working_set_percentage"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 100.0
  }

#   action {
#     action_group_id = azurerm_monitor_action_group.main.id
#   }
}