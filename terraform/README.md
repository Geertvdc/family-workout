# Azure Deployment with OpenTofu

This directory contains Infrastructure as Code (IaC) for deploying FamilyFitness to Azure using OpenTofu.

## Architecture

### Resource Groups
- `ffwod-state-rg` - Terraform state storage
- `ffwod-shared-rg` - Shared Azure Container Registry (for binary promotion)
- `ffwod-dev-rg` - Development environment
- `ffwod-prod-rg` - Production environment

### Resources per Environment
- **Azure Container Apps** - API and Blazor containers
- **PostgreSQL Flexible Server 17** - Database with Entra ID authentication
- **Application Insights + Log Analytics** - Monitoring and logging
- **Managed Identities** - For secure service-to-service authentication

### Key Features
- **Managed Identity** for PostgreSQL (no passwords)
- **Managed Identity** for ACR pull (no credentials)
- **Binary Promotion** - Same containers deployed to dev and prod
- **Auto-scaling** - Scale to zero in dev, always-on in prod
- **Cost-optimized** - Minimal resources for low traffic

## Prerequisites

- [OpenTofu](https://opentofu.org/docs/intro/install/) (>= 1.6)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- Docker (for building container images)
- Azure subscription (ID: `b8affb75-1fd8-4116-8065-dc871fdbe8bc`)

## One-Time Setup

### 1. Bootstrap Azure Resources

Run the bootstrap script to create state storage and shared ACR:

```bash
cd terraform
./bootstrap.sh
```

This creates:
- State storage account in `ffwod-state-rg`
- Azure Container Registry in `ffwod-shared-rg`

**Important**: Save the ACR credentials from the output - you'll need them for GitHub Secrets.

### 2. Configure Entra External ID

Update both `dev.tfvars` and `prod.tfvars` with your Entra Client ID:

1. Go to Azure Portal → External ID tenant
2. Navigate to App registrations → Your app → Overview
3. Copy the "Application (client) ID"
4. Replace `YOUR_CLIENT_ID_HERE` in both tfvars files

## Deploying to Dev

### 1. Initialize OpenTofu

```bash
cd terraform
tofu init -backend-config="key=dev.tfstate"
```

### 2. Review the Plan

```bash
tofu plan -var-file="dev.tfvars"
```

### 3. Apply Infrastructure

```bash
tofu apply -var-file="dev.tfvars"
```

**Note the outputs** - you'll need the API and Blazor URLs.

### 4. Build and Push Container Images

```bash
# Login to ACR
az acr login --name ffwodacr

# Build and push API
docker build -t ffwodacr.azurecr.io/familyfitness-api:latest \
  -f src/FamilyFitness.Api/Dockerfile .
docker push ffwodacr.azurecr.io/familyfitness-api:latest

# Build and push Blazor
docker build -t ffwodacr.azurecr.io/familyfitness-blazor:latest \
  -f src/FamilyFitness.Blazor/Dockerfile .
docker push ffwodacr.azurecr.io/familyfitness-blazor:latest
```

### 5. Restart Container Apps

```bash
az containerapp revision restart \
  --name ffwod-dev-api \
  --resource-group ffwod-dev-rg

az containerapp revision restart \
  --name ffwod-dev-blazor \
  --resource-group ffwod-dev-rg
```

### 6. Grant PostgreSQL Permissions

After first deployment, the API's managed identity needs database permissions:

```bash
# Get the API's managed identity principal ID
API_IDENTITY=$(tofu output -raw api_identity_principal_id)

# Connect to PostgreSQL
az postgres flexible-server connect \
  --name ffwod-dev-db \
  --resource-group ffwod-dev-rg \
  --database-name family_fitness \
  --admin-user YOUR_ADMIN_USER

# In psql, grant permissions:
CREATE ROLE "ffwod-dev-api" WITH LOGIN;
GRANT ALL PRIVILEGES ON DATABASE family_fitness TO "ffwod-dev-api";
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO "ffwod-dev-api";
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO "ffwod-dev-api";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO "ffwod-dev-api";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO "ffwod-dev-api";
```

## Deploying to Prod

### 1. Initialize for Prod

```bash
tofu init -backend-config="key=prod.tfstate" -reconfigure
```

### 2. Apply Prod Infrastructure

```bash
tofu plan -var-file="prod.tfvars"
tofu apply -var-file="prod.tfvars"
```

### 3. Use Same Container Images (Binary Promotion)

The containers are already in ACR from the dev deployment. Just restart prod:

```bash
az containerapp revision restart \
  --name ffwod-prod-api \
  --resource-group ffwod-prod-rg

az containerapp revision restart \
  --name ffwod-prod-blazor \
  --resource-group ffwod-prod-rg
```

### 4. Grant PostgreSQL Permissions for Prod

Same as dev, but replace `ffwod-dev-api` with `ffwod-prod-api` and use prod resource group.

## Cost Estimates (Monthly)

### Dev Environment (~$15-25/month)
- Container Apps: $5-10 (scales to zero)
- PostgreSQL B1ms: $8-12
- Application Insights: $2-3

### Prod Environment (~$30-50/month)
- Container Apps: $15-25 (always-on, 1-2 replicas)
- PostgreSQL B2s: $12-18
- Application Insights: $3-7

### Shared (~$20/month)
- ACR Standard: $20

**Total**: ~$65-95/month for both environments

## Managed Identity Flow

1. **Local Development**: Uses your Azure CLI login via `DefaultAzureCredential()`
2. **Azure Container Apps**: Uses system-assigned managed identity
3. **PostgreSQL**: Authenticates via Entra ID (no passwords)
4. **ACR**: Container Apps pull images via managed identity

## Database Migrations

Migrations run **automatically** when the API starts:
- Executes `DbContext.Database.MigrateAsync()`
- Retries 30 times with 2-second delays
- Seeds data in Development environment only
- Logs all migration attempts

See `src/FamilyFitness.Api/Program.cs` around line 87.

## Useful Commands

### View Outputs
```bash
tofu output
tofu output -raw api_url
tofu output -raw blazor_url
```

### View Container App Logs
```bash
az containerapp logs show \
  --name ffwod-dev-api \
  --resource-group ffwod-dev-rg \
  --follow
```

### Update Container Apps with New Images
```bash
# After pushing new images to ACR, restart to pull latest:
az containerapp revision restart \
  --name ffwod-dev-api \
  --resource-group ffwod-dev-rg
```

### Destroy Environment
```bash
# Be careful! This deletes everything in the environment
tofu destroy -var-file="dev.tfvars"
```

## Troubleshooting

### "Unable to connect to PostgreSQL"
- Check that managed identity has been granted database permissions (step 6 above)
- Verify firewall rules allow Azure services
- Check Container App logs for detailed error messages

### "Failed to pull image from ACR"
- Verify managed identity has AcrPull role assignment
- Check that images exist in ACR: `az acr repository list --name ffwodacr`
- Ensure Container App has system-assigned identity enabled

### "Migrations failing on startup"
- Check PostgreSQL server is running and accessible
- Verify connection string in Container App environment variables
- Review Container App logs for specific migration errors

## File Structure

```
terraform/
├── bootstrap.sh           # One-time setup script
├── main.tf               # All resources (single file)
├── variables.tf          # Variable definitions
├── outputs.tf            # Output values
├── backend.tf            # State backend config
├── versions.tf           # OpenTofu/provider versions
├── dev.tfvars           # Dev environment values
└── prod.tfvars          # Prod environment values
```

## Next Steps

1. Set up GitHub Actions for automated deployments
2. Configure custom domain for production
3. Enable monitoring alerts in Application Insights
4. Consider VNet integration for private communication
5. Add automated backups for PostgreSQL (beyond 7-day retention)
