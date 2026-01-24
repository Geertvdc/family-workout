output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "api_url" {
  description = "API application URL"
  value       = "https://${azurerm_container_app.api.ingress[0].fqdn}"
}

output "blazor_url" {
  description = "Blazor application URL"
  value       = "https://${azurerm_container_app.blazor.ingress[0].fqdn}"
}

output "api_identity_principal_id" {
  description = "API managed identity principal ID (for PostgreSQL permissions)"
  value       = azurerm_container_app.api.identity[0].principal_id
}

output "blazor_identity_principal_id" {
  description = "Blazor managed identity principal ID"
  value       = azurerm_container_app.blazor.identity[0].principal_id
}

output "database_fqdn" {
  description = "PostgreSQL server FQDN"
  value       = azurerm_postgresql_flexible_server.main.fqdn
}

output "application_insights_instrumentation_key" {
  description = "Application Insights instrumentation key"
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
}

output "application_insights_connection_string" {
  description = "Application Insights connection string"
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
}
