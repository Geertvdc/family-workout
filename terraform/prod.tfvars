environment     = "prod"
location        = "swedencentral"
subscription_id = "b8affb75-1fd8-4116-8065-dc871fdbe8bc"

# Database - Still burstable for cost optimization (can upgrade later)
db_sku_name   = "B_Standard_B2s" # 2 vCores, better than B1ms but still cost-effective
db_storage_mb = 32768

# API Container - Slightly more resources, but still minimal
api_cpu          = 0.5   # Half core
api_memory       = "1Gi" # 1 GB
api_min_replicas = 1     # Always at least one running
api_max_replicas = 5

# Blazor Container - Slightly more resources
blazor_cpu          = 0.25
blazor_memory       = "0.5Gi"
blazor_min_replicas = 1 # Always at least one running
blazor_max_replicas = 5

# Azure Entra External ID
entra_authority = "https://ffworkoutoftheday.ciamlogin.com/"
entra_audience  = "3d9bde47-ee26-443f-9593-1ebb936982b2" # API app registration client ID
entra_issuer    = ""
entra_api_scope = "api://2b8a282a-98b0-4162-9553-4c5b8882bdcc/user_access"
# Note: entra_blazor_client_id and entra_blazor_client_secret are passed via TF_VAR environment variables from GitHub secrets
