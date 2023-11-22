variable "agent_count" {
  default = 3
}
variable "cluster_name" {
  default = "aks1"
}

variable "metric_labels_allowlist" {
  default = null
}

variable "metric_annotations_allowlist" {
  default = null
}

variable "monitor_workspace_name" {
  default = "prom-workspace-hub"
}


variable "resource_group_location" {
  default     = "northeurope"
  description = "Location of the resource group."
}

variable "main_subscription_id" {
  default = "00000000-0000-0000-0000-000000000000"
}

variable "secondary_subscription_id" {
  default = "00000000-0000-0000-0000-000000000000"
}

locals {
  monitor_workspace_id = "/subscriptions/${var.secondary_subscription_id}/resourcegroups/rg-op-telemetry-demo/providers/microsoft.monitor/accounts/${var.monitor_workspace_name}"
}