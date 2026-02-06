environment     = "dev"
location        = "swedencentral"
subscription_id = "b8affb75-1fd8-4116-8065-dc871fdbe8bc"

# Database - Burstable tier for cost savings (smallest available)
db_sku_name   = "B_Standard_B1ms"
db_storage_mb = 32768 # 32 GB minimum

# API Container - Very minimal resources for low traffic
api_cpu          = 0.25    # Quarter core
api_memory       = "0.5Gi" # 512 MB
api_min_replicas = 0       # Scale to zero when idle
api_max_replicas = 2

# Blazor Container - Very minimal resources
blazor_cpu          = 0.25    # Quarter core
blazor_memory       = "0.5Gi" # 512 MB
blazor_min_replicas = 0       # Scale to zero when idle
blazor_max_replicas = 2

# Azure Entra External ID
entra_authority = "https://ffworkoutoftheday.ciamlogin.com/"
entra_audience  = "3d9bde47-ee26-443f-9593-1ebb936982b2" # API app registration client ID
entra_issuer    = ""
entra_api_scope = "api://3d9bde47-ee26-443f-9593-1ebb936982b2/access_as_user"
# Note: entra_blazor_client_id and entra_blazor_client_secret are passed via TF_VAR environment variables from GitHub secrets
