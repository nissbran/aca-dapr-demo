resource "azurerm_monitor_alert_prometheus_rule_group" "alert_group" {
  name                = "alert-group-${var.cluster_name}"
  location            = data.azurerm_resource_group.aks.location
  resource_group_name = data.azurerm_resource_group.aks.name
  cluster_name        = "aks-${var.cluster_name}"
  description         = "Alert group for ${var.cluster_name}"
  rule_group_enabled  = true
  interval            = "PT1M"
  scopes              = [local.monitor_workspace_id, data.azurerm_kubernetes_cluster.akscluster.id]

  rule {
    alert      = "KubePodCrashLooping"
    enabled    = true
    expression = <<EOF
max_over_time(kube_pod_container_status_waiting_reason{reason="CrashLoopBackOff", job="kube-state-metrics"}[5m]) >= 1
EOF
    for        = "PT1M"
    severity   = 2

    # action {
    #   action_group_id = azurerm_monitor_action_group.example.id
    # }

    alert_resolution {
      auto_resolved   = true
      time_to_resolve = "PT10M"
    }

    annotations = {
      annotationName = "annotationValue"
    }

    labels = {
      severity = "warning"
    }
  }
  
  rule {
    alert      = "KubePodImagePullBackOff"
    enabled    = true
    expression = <<EOF
max_over_time(kube_pod_container_status_waiting_reason{reason="ImagePullBackoff", job="kube-state-metrics"}[5m]) >= 1
EOF
    for        = "PT1M"
    severity   = 2

    # action {
    #   action_group_id = azurerm_monitor_action_group.example.id
    # }

    alert_resolution {
      auto_resolved   = true
      time_to_resolve = "PT10M"
    }

    annotations = {
      annotationName = "annotationValue"
    }

    labels = {
      severity = "warning"
    }
  }
}