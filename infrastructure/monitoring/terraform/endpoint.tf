

resource "azurerm_monitor_data_collection_endpoint" "collection" {
  name                          = "dce-aks-${var.cluster_name}"
  resource_group_name           = data.azurerm_resource_group.aks.name
  location                      = data.azurerm_resource_group.aks.location
  kind                          = "Linux"
  public_network_access_enabled = false

  lifecycle {
    create_before_destroy = true
  }
}
