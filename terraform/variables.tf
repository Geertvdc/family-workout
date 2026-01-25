variable "environment" {
  description = "Environment name (dev or prod)"
  type        = string

  validation {
    condition     = contains(["dev", "prod"], var.environment)
    error_message = "Environment must be either 'dev' or 'prod'"
  }
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "swedencentral"
}

variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
}

# Database
variable "db_sku_name" {
  description = "PostgreSQL SKU (e.g., B_Standard_B1ms for dev, GP_Standard_D2s_v3 for prod)"
  type        = string
}

variable "db_storage_mb" {
  description = "Database storage in MB"
  type        = number
  default     = 32768
}

# API Container App
variable "api_cpu" {
  description = "CPU cores for API container"
  type        = number
}

variable "api_memory" {
  description = "Memory for API container (e.g., '1Gi')"
  type        = string
}

variable "api_min_replicas" {
  description = "Minimum replicas for API"
  type        = number
}

variable "api_max_replicas" {
  description = "Maximum replicas for API"
  type        = number
}

# Blazor Container App
variable "blazor_cpu" {
  description = "CPU cores for Blazor container"
  type        = number
}

variable "blazor_memory" {
  description = "Memory for Blazor container (e.g., '0.5Gi')"
  type        = string
}

variable "blazor_min_replicas" {
  description = "Minimum replicas for Blazor"
  type        = number
}

variable "blazor_max_replicas" {
  description = "Maximum replicas for Blazor"
  type        = number
}

# Azure Entra ID (for user authentication)
variable "entra_authority" {
  description = "Azure Entra External ID authority URL"
  type        = string
}

variable "entra_audience" {
  description = "Azure Entra External ID audience (client ID)"
  type        = string
}

variable "entra_issuer" {
  description = "Azure Entra External ID issuer URL (optional, defaults to authority)"
  type        = string
  default     = ""
}
