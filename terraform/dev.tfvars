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
entra_tenant_id = "12094d72-73f9-4374-8d1e-8181315429a1"
entra_authority = "https://12094d72-73f9-4374-8d1e-8181315429a1.ciamlogin.com/12094d72-73f9-4374-8d1e-8181315429a1/v2.0"
entra_audience  = "2b8a282a-98b0-4162-9553-4c5b8882bdcc" # Blazor app client ID (this is what appears in the token's audience)
entra_issuer    = "https://12094d72-73f9-4374-8d1e-8181315429a1.ciamlogin.com/12094d72-73f9-4374-8d1e-8181315429a1/v2.0"
entra_api_scope = "api://2b8a282a-98b0-4162-9553-4c5b8882bdcc/user_access"
# Note: entra_blazor_client_id and entra_blazor_client_secret are passed via TF_VAR environment variables from GitHub secrets
