# GitHub Actions Deployment Setup

This document explains how to configure GitHub Actions for automated deployments to Azure.

## Workflow Overview

We have **two separate workflows** for application and infrastructure:

### `deploy.yml` - Application Deployment
1. **Build and Test** - Runs on every PR and push to main
2. **Build Containers** - Builds Docker images and pushes to ACR
3. **Deploy to Dev** - Deploys to dev environment on PRs
4. **Deploy to Prod** - Deploys to production on main branch

### `infrastructure.yml` - Infrastructure Management (OpenTofu)
1. **Plan** - Shows infrastructure changes on PRs (both dev and prod)
2. **Apply** - Applies infrastructure changes when merged to main
3. **Manual Trigger** - Allows manual plan/apply/destroy via GitHub UI
4. **Only runs when** `terraform/*` files change

## Required GitHub Secrets

You need to configure secrets in your GitHub repository settings.

### 1. ACR Environment Secrets

Create an **environment** called `acr` with these secrets:

- `ACR_LOGIN_SERVER` - Your ACR login server (e.g., `ffwodacr.azurecr.io`)
- `ACR_USERNAME` - ACR admin username
- `ACR_PASSWORD` - ACR admin password

**How to get these values:**
After running `terraform/bootstrap.sh`, the script outputs these values.

Or retrieve them manually:
```bash
# Login server
az acr show --name ffwodacr --query loginServer -o tsv

# Username and password
az acr credential show --name ffwodacr
```

### 2. Azure Federated Identity Credentials (OIDC)

We use **Federated Identity** (OIDC) instead of service principals with secrets. This is more secure as no long-lived credentials are stored.

**Step 1: Create Azure Application (Service Principal)**

```bash
# Create the app registration (replace with your GitHub org/username and repo)
GITHUB_ORG="Geertvdc"  # Or your organization name
GITHUB_REPO="family-workout"
APP_NAME="github-actions-ffwod"

# Create the app
APP_ID=$(az ad app create \
  --display-name "${APP_NAME}" \
  --query appId -o tsv)

echo "Application ID: ${APP_ID}"

# Create service principal
SP_ID=$(az ad sp create --id ${APP_ID} --query id -o tsv)
echo "Service Principal Object ID: ${SP_ID}"

# Assign Contributor role to subscription
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
az role assignment create \
  --role contributor \
  --subscription ${SUBSCRIPTION_ID} \
  --assignee-object-id ${SP_ID} \
  --assignee-principal-type ServicePrincipal

# Get tenant ID
TENANT_ID=$(az account show --query tenantId -o tsv)
echo "Tenant ID: ${TENANT_ID}"
```

**Step 2: Configure Federated Credentials**

```bash
# Create federated credential for main branch
az ad app federated-credential create \
  --id ${APP_ID} \
  --parameters '{
    "name": "github-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"${GITHUB_ORG}"'/'"${GITHUB_REPO}"':ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for pull requests
az ad app federated-credential create \
  --id ${APP_ID} \
  --parameters '{
    "name": "github-pr",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"${GITHUB_ORG}"'/'"${GITHUB_REPO}"':pull_request",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for dev environment
az ad app federated-credential create \
  --id ${APP_ID} \
  --parameters '{
    "name": "github-env-dev",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"${GITHUB_ORG}"'/'"${GITHUB_REPO}"':environment:dev",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for prod environment
az ad app federated-credential create \
  --id ${APP_ID} \
  --parameters '{
    "name": "github-env-prod",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"${GITHUB_ORG}"'/'"${GITHUB_REPO}"':environment:prod",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Summary - Save these values!
echo "======================================"
echo "Add these secrets to GitHub:"
echo "======================================"
echo "AZURE_CLIENT_ID: ${APP_ID}"
echo "AZURE_TENANT_ID: ${TENANT_ID}"
echo "AZURE_SUBSCRIPTION_ID: ${SUBSCRIPTION_ID}"
echo "======================================"
```

**Step 3: Add Secrets to GitHub**

1. Go to your repository → Settings → Secrets and variables → Actions
2. Create these **Repository secrets**:
   - `AZURE_CLIENT_ID` - The Application (client) ID from above
   - `AZURE_TENANT_ID` - Your Azure tenant ID
   - `AZURE_SUBSCRIPTION_ID` - Your Azure subscription ID (b8affb75-1fd8-4116-8065-dc871fdbe8bc)

### 3. Optional: Environment-Specific Secrets

If you want to add protection rules or approvals:

**Dev Environment:**
- Go to Settings → Environments → Create `dev` environment
- No approval required (deploys automatically on PR)

**Prod Environment:**
- Go to Settings → Environments → Create `prod` environment
- Add required reviewers (recommended for production)
- Set deployment branch to `main` only

## Workflow Triggers

### On Pull Request
1. Runs tests
2. Builds containers and pushes to ACR (tagged with commit SHA)
3. Deploys to **dev** environment
4. Comments on PR with dev URLs

### On Push to Main
1. Runs tests
2. Builds containers and pushes to ACR (tagged with commit SHA + latest)
3. Deploys to **production** environment
4. Creates deployment summary

## Testing the Workflow

### Test Dev Deployment
1. Create a new branch: `git checkout -b test-deployment`
2. Make a small change (e.g., update README)
3. Commit and push: `git push -u origin test-deployment`
4. Open a Pull Request to main
5. GitHub Actions will automatically:
   - Run tests
   - Build containers
   - Deploy to dev
   - Comment on PR with URLs

### Test Prod Deployment
1. Merge the PR to main
2. GitHub Actions will automatically:
   - Run tests
   - Build containers
   - Deploy to production
   - Create deployment summary

## Binary Promotion

The workflow implements binary promotion:
- Same container image built once
- Tagged with commit SHA (e.g., `abc1234`)
- Also tagged as `latest`
- Dev gets the SHA-tagged image
- Prod gets the same SHA-tagged image (tested in dev first)

## Workflow Jobs

```
┌─────────────────┐
│ Build and Test  │ (Always runs)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Build Containers│ (Builds & pushes to ACR)
└────────┬────────┘
         │
         ├──────────────┬──────────────┐
         ▼              ▼              ▼
    ┌─────────┐   ┌─────────┐   (On main only)
    │Deploy to│   │Deploy to│
    │  Dev    │   │  Prod   │
    │ (on PR) │   │(on main)│
    └─────────┘   └─────────┘
```

## Monitoring Deployments

### View Workflow Runs
- Go to your repository → Actions tab
- Click on a workflow run to see detailed logs

### View Container App Logs
```bash
# Dev API logs
az containerapp logs show \
  --name ffwod-dev-api \
  --resource-group ffwod-dev-rg \
  --follow

# Prod API logs
az containerapp logs show \
  --name ffwod-prod-api \
  --resource-group ffwod-prod-rg \
  --follow
```

### Check Deployed Image Versions
```bash
# List images in ACR
az acr repository show-tags \
  --name ffwodacr \
  --repository familyfitness-api \
  --orderby time_desc \
  --output table

# Check what's running in dev
az containerapp show \
  --name ffwod-dev-api \
  --resource-group ffwod-dev-rg \
  --query "properties.template.containers[0].image" -o tsv
```

## Troubleshooting

### "Failed to login to ACR"
- Verify ACR secrets are correct in the `acr` environment
- Check ACR admin user is enabled: `az acr update --name ffwodacr --admin-enabled true`

### "Failed to update container app"
- Verify Azure credentials are correct
- Check service principal has Contributor role on the subscription
- Ensure container apps exist (run OpenTofu apply first)

### "Image not found in ACR"
- Check the build-containers job succeeded
- Verify images were pushed: `az acr repository list --name ffwodacr -o table`

### "Migrations failing after deployment"
- Check container app logs for detailed error messages
- Verify managed identity has PostgreSQL permissions (see terraform/README.md)
- Ensure PostgreSQL firewall allows Azure services

## Next Steps

1. Set up the required secrets (ACR + Azure credentials)
2. Create optional environments (dev/prod) with protection rules
3. Test the workflow with a PR
4. Monitor the first production deployment
5. Configure custom domains (optional)
6. Set up Application Insights alerts (optional)

---

# Infrastructure Workflow (OpenTofu)

The `infrastructure.yml` workflow manages your Azure infrastructure separately from application deployments.

## When Does It Run?

### Automatically (on infrastructure changes):
- **On PR**: Runs `tofu plan` for both dev and prod, comments results on PR
- **On Merge to Main**: Runs `tofu apply` for both dev and prod

**Triggers only when these files change:**
- `terraform/**` (any file in terraform directory)
- `.github/workflows/infrastructure.yml` (the workflow itself)

### Manually (workflow_dispatch):
You can manually trigger infrastructure changes from GitHub UI:
1. Go to Actions → Infrastructure (OpenTofu) → Run workflow
2. Choose environment (dev or prod)
3. Choose action (plan, apply, or destroy)

## Workflow Jobs

```
On PR (terraform/* changes):
├─ Plan Dev Infrastructure → Comments on PR
└─ Plan Prod Infrastructure → Comments on PR

On Merge to Main (terraform/* changes):
├─ Apply Dev Infrastructure → Updates dev resources
└─ Apply Prod Infrastructure → Updates prod resources

Manual Trigger:
└─ Manual Infrastructure Action → Plan/Apply/Destroy chosen env
```

## Infrastructure Change Process

### 1. Make Infrastructure Changes

```bash
# Edit terraform files locally
vim terraform/dev.tfvars  # or main.tf, etc.

# Test locally first (recommended)
cd terraform
tofu init -backend-config="key=dev.tfstate"
tofu plan -var-file="dev.tfvars"

# Commit and push
git checkout -b infra-update-database-size
git add terraform/
git commit -m "Increase database size for dev"
git push -u origin infra-update-database-size
```

### 2. Create Pull Request

The workflow automatically:
- Runs `tofu fmt -check` (ensures formatting)
- Runs `tofu validate` (checks configuration)
- Runs `tofu plan` for **both dev and prod**
- Comments the plans on your PR

**Review the plan** - make sure the changes are what you expect!

### 3. Merge PR

When you merge to main:
- Workflow automatically runs `tofu apply`
- Updates both dev and prod infrastructure
- Shows outputs in job summary

### 4. Verify Changes

Check the GitHub Actions summary for:
- Resource changes applied
- New output values (URLs, IPs, etc.)

## Manual Infrastructure Operations

Sometimes you need to run infrastructure changes manually (e.g., fix drift, destroy resources).

### Run Manual Plan

1. Go to **Actions** → **Infrastructure (OpenTofu)**
2. Click **Run workflow**
3. Select:
   - Environment: `dev` or `prod`
   - Action: `plan`
4. Click **Run workflow**
5. Check the job logs for plan output

### Run Manual Apply

1. Go to **Actions** → **Infrastructure (OpenTofu)**
2. Click **Run workflow**
3. Select:
   - Environment: `dev` or `prod`
   - Action: `apply`
4. Click **Run workflow**
5. Approve if environment requires it
6. Check job summary for results

### Destroy Environment (⚠️ Dangerous)

1. Go to **Actions** → **Infrastructure (OpenTofu)**
2. Click **Run workflow**
3. Select:
   - Environment: `dev` or `prod`
   - Action: `destroy`
4. Click **Run workflow**
5. **Double-check** - this deletes all resources!

## Differences from Application Workflow

| Aspect | Application Workflow | Infrastructure Workflow |
|--------|---------------------|------------------------|
| **Runs on** | Every PR/push | Only when `terraform/*` changes |
| **Speed** | Fast (~2-5 min) | Slower (~5-15 min) |
| **What it does** | Updates container images | Updates Azure resources |
| **Frequency** | Multiple times per day | Rarely (weeks/months) |
| **Risk** | Low (just app code) | Higher (can break infra) |

## Troubleshooting Infrastructure Workflow

### "State lock error"
- Another workflow run is in progress
- Wait for it to complete, or manually release lock:
  ```bash
  az storage blob lease break \
    --account-name ffwodtfstate \
    --container-name tfstate \
    --blob-name dev.tfstate
  ```

### "Plan differs from apply"
- Someone made manual changes in Azure Portal
- Run `tofu plan` locally to see drift
- Either: apply to fix drift, or update terraform to match reality

### "Backend initialization failed"
- Check Azure credentials are valid
- Ensure storage account `ffwodtfstate` exists
- Verify blob container `tfstate` exists

### "Format check failed"
- Run `tofu fmt` locally in terraform directory
- Commit the formatted files
- Push again

## Best Practices

✅ **Always review plans** before merging infrastructure PRs
✅ **Test locally first** with `tofu plan` before pushing
✅ **Keep changes small** - one logical change per PR
✅ **Add comments** explaining why infrastructure is changing
✅ **Use manual workflows** for one-off operations or troubleshooting

❌ **Don't skip the PR** - always go through plan review
❌ **Don't apply directly** via Azure CLI/Portal (causes drift)
❌ **Don't commit secrets** to tfvars files (use GitHub secrets if needed)

---

# Combined Deployment Scenarios

## Scenario 1: Application Update Only

Changes to `src/**` code without infrastructure changes:

```bash
git checkout -b new-feature
# Make code changes
git commit -m "Add new feature"
git push
# Create PR
```

**What runs:**
- ✅ `deploy.yml` - Builds, tests, deploys app to dev
- ❌ `infrastructure.yml` - Does NOT run (no terraform changes)

## Scenario 2: Infrastructure Update Only

Changes to `terraform/**` without code changes:

```bash
git checkout -b increase-database-storage
# Edit terraform/dev.tfvars
git commit -m "Increase DB storage to 64GB"
git push
# Create PR
```

**What runs:**
- ❌ `deploy.yml` - Does NOT run (no code changes)
- ✅ `infrastructure.yml` - Plans infrastructure changes

## Scenario 3: Both Application and Infrastructure

Changes to both code and infrastructure:

```bash
git checkout -b add-redis-cache
# Add Redis to terraform/main.tf
# Update app code to use Redis
git commit -m "Add Redis cache layer"
git push
# Create PR
```

**What runs:**
- ✅ `deploy.yml` - Builds, tests, deploys app
- ✅ `infrastructure.yml` - Plans infrastructure changes

**Merge order:**
1. Infrastructure gets applied first (creates Redis)
2. App gets deployed second (uses new Redis)

---

# Quick Reference

## Application Deployment
- **Trigger**: Any code change
- **Dev**: Automatic on PR
- **Prod**: Automatic on merge to main
- **Time**: ~2-5 minutes

## Infrastructure Deployment  
- **Trigger**: `terraform/*` changes only
- **Dev**: Automatic on merge to main
- **Prod**: Automatic on merge to main  
- **Manual**: Via GitHub Actions UI
- **Time**: ~5-15 minutes

## Next Steps

1. Set up the required secrets (ACR + Azure credentials)
2. Create optional environments (dev/prod) with protection rules
3. Test the workflow with a PR
4. Monitor the first production deployment
5. Configure custom domains (optional)
6. Set up Application Insights alerts (optional)
