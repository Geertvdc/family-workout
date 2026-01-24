terraform {
  backend "azurerm" {
    resource_group_name  = "ffwod-state-rg"
    storage_account_name = "ffwodtfstate"
    container_name       = "tfstate"
    # key is passed via: tofu init -backend-config="key=dev.tfstate"
  }
}
