terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.0"
    }
  }
}

provider "azurerm" {
  subscription_id = var.main_subscription_id
  features {}
}

# Configuration for the "secondary" subscription
provider "azurerm" {
  alias           = "secondary"
  subscription_id = var.secondary_subscription_id
  features {}
}