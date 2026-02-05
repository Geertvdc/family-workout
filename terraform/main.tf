# Data source for shared ACR
data "azurerm_container_registry" "shared" {
  name                = "ffwodacr"
  resource_group_name = "ffwod-shared-rg"
}

# Resource Group for this environment
resource "azurerm_resource_group" "main" {
  name     = "ffwod-${var.environment}-rg"
  location = var.location
}

# Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "main" {
  name                = "ffwod-${var.environment}-logs"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = var.environment == "prod" ? 90 : 30
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = "ffwod-${var.environment}-ai"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
}

# PostgreSQL Flexible Server
resource "azurerm_postgresql_flexible_server" "main" {
  name                = "ffwod-${var.environment}-db"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  version             = "17"

  # Use managed identity for admin instead of password
  authentication {
    active_directory_auth_enabled = true
    password_auth_enabled         = false
  }

  storage_mb   = var.db_storage_mb
  storage_tier = "P4"

  sku_name = var.db_sku_name

  backup_retention_days        = 7
  geo_redundant_backup_enabled = false

  public_network_access_enabled = true

  # Ignore zone changes since we don't use high availability
  lifecycle {
    ignore_changes = [zone]
  }
}

# PostgreSQL Firewall Rule - Allow Azure Services
resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure" {
  name             = "allow-azure-services"
  server_id        = azurerm_postgresql_flexible_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# PostgreSQL Database
resource "azurerm_postgresql_flexible_server_database" "main" {
  name      = "family_fitness"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}

# Get current client (for assigning PostgreSQL admin)
data "azurerm_client_config" "current" {}

# Assign current user/SP as PostgreSQL Entra ID admin
resource "azurerm_postgresql_flexible_server_active_directory_administrator" "main" {
  server_name         = azurerm_postgresql_flexible_server.main.name
  resource_group_name = azurerm_resource_group.main.name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  object_id           = data.azurerm_client_config.current.object_id
  principal_name      = "PostgreSQL-Admin"
  principal_type      = "ServicePrincipal"
}

# Container Apps Environment
resource "azurerm_container_app_environment" "main" {
  name                       = "ffwod-${var.environment}-env"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
}

# API Container App
resource "azurerm_container_app" "api" {
  name                         = "ffwod-${var.environment}-api"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  registry {
    server   = data.azurerm_container_registry.shared.login_server
    identity = "system"
  }

  secret {
    name  = "appinsights-connection-string"
    value = azurerm_application_insights.main.connection_string
  }

  template {
    container {
      name = "api"
      # Use placeholder image initially - deploy workflow will update with real app
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = var.api_cpu
      memory = var.api_memory

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "dev" ? "Development" : "Production"
      }

      # PostgreSQL connection using managed identity
      env {
        name  = "ConnectionStrings__family-fitness"
        value = "Host=${azurerm_postgresql_flexible_server.main.fqdn};Database=family_fitness;Username=ffwod-${var.environment}-api;SSL Mode=Require"
      }

      env {
        name        = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        secret_name = "appinsights-connection-string"
      }

      # Azure Entra ID config for user authentication
      env {
        name  = "AzureAd__Authority"
        value = var.entra_authority
      }

      env {
        name  = "AzureAd__Audience"
        value = var.entra_audience
      }

      env {
        name  = "AzureAd__Issuer"
        value = var.entra_issuer != "" ? var.entra_issuer : var.entra_authority
      }
    }

    min_replicas = var.api_min_replicas
    max_replicas = var.api_max_replicas
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  lifecycle {
    # Ignore changes to container image - managed by deploy workflow
    ignore_changes = [template[0].container[0].image]
  }
}

# Grant API managed identity ACR pull access
resource "azurerm_role_assignment" "api_acr_pull" {
  scope                = data.azurerm_container_registry.shared.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_container_app.api.identity[0].principal_id
}

# Assign API Container App managed identity as PostgreSQL user
resource "azurerm_postgresql_flexible_server_active_directory_administrator" "api" {
  server_name         = azurerm_postgresql_flexible_server.main.name
  resource_group_name = azurerm_resource_group.main.name
  tenant_id           = azurerm_container_app.api.identity[0].tenant_id
  object_id           = azurerm_container_app.api.identity[0].principal_id
  principal_name      = azurerm_container_app.api.name
  principal_type      = "ServicePrincipal"
}

# Blazor Container App
resource "azurerm_container_app" "blazor" {
  name                         = "ffwod-${var.environment}-blazor"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  registry {
    server   = data.azurerm_container_registry.shared.login_server
    identity = "system"
  }

  secret {
    name  = "appinsights-connection-string"
    value = azurerm_application_insights.main.connection_string
  }

  template {
    container {
      name = "blazor"
      # Use placeholder image initially - deploy workflow will update with real app
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = var.blazor_cpu
      memory = var.blazor_memory

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "dev" ? "Development" : "Production"
      }

      env {
        name  = "ApiUrl"
        value = "https://${azurerm_container_app.api.ingress[0].fqdn}"
      }

      env {
        name        = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        secret_name = "appinsights-connection-string"
      }

      # Azure Entra ID config for user authentication
      env {
        name  = "AzureAd__Authority"
        value = var.entra_authority
      }

      env {
        name  = "AzureAd__Audience"
        value = var.entra_audience
      }

      env {
        name  = "AzureAd__Issuer"
        value = var.entra_issuer != "" ? var.entra_issuer : var.entra_authority
      }
    }

    min_replicas = var.blazor_min_replicas
    max_replicas = var.blazor_max_replicas
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  lifecycle {
    # Ignore changes to container image - managed by deploy workflow
    ignore_changes = [template[0].container[0].image]
  }
}

# Grant Blazor managed identity ACR pull access
resource "azurerm_role_assignment" "blazor_acr_pull" {
  scope                = data.azurerm_container_registry.shared.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_container_app.blazor.identity[0].principal_id
}
