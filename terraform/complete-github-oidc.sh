#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}============================================${NC}"
echo -e "${GREEN}Complete GitHub OIDC Setup${NC}"
echo -e "${GREEN}============================================${NC}"
echo ""

# Use the existing application
APP_ID="59ae5c04-3196-494d-90bf-c4c2e70a7b8a"
SP_ID="4fec12cf-e64f-43d1-902a-bcec1fa5556d"
GITHUB_ORG="Geertvdc"
GITHUB_REPO="family-workout"

# Get subscription and tenant info
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

echo -e "${YELLOW}Using existing application:${NC}"
echo "  Application ID: ${APP_ID}"
echo "  Service Principal Object ID: ${SP_ID}"
echo ""

# Assign Contributor role
echo -e "${YELLOW}Assigning Contributor role to subscription...${NC}"
az role assignment create \
  --role contributor \
  --scope /subscriptions/${SUBSCRIPTION_ID} \
  --assignee-object-id ${SP_ID} \
  --assignee-principal-type ServicePrincipal \
  --output none

echo -e "${GREEN}✓ Contributor role assigned${NC}"

# Create federated credentials
echo ""
echo -e "${YELLOW}Creating federated credentials...${NC}"

# Main branch credential
az ad app federated-credential create \
  --id ${APP_ID} \
  --parameters "{
    \"name\": \"github-main\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:ref:refs/heads/main\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" \
  --output none

echo -e "${GREEN}✓ Main branch credential created${NC}"

# Pull request credential
az ad app federated-credential create \
  --id ${APP_ID} \
  --parameters "{
    \"name\": \"github-pr\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:pull_request\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" \
  --output none

echo -e "${GREEN}✓ Pull request credential created${NC}"

# Dev environment credential
az ad app federated-credential create \
  --id ${APP_ID} \
  --parameters "{
    \"name\": \"github-env-dev\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:dev\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" \
  --output none

echo -e "${GREEN}✓ Dev environment credential created${NC}"

# Prod environment credential
az ad app federated-credential create \
  --id ${APP_ID} \
  --parameters "{
    \"name\": \"github-env-prod\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:prod\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" \
  --output none

echo -e "${GREEN}✓ Prod environment credential created${NC}"

# ACR environment credential
az ad app federated-credential create \
  --id ${APP_ID} \
  --parameters "{
    \"name\": \"github-env-acr\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:acr\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" \
  --output none

echo -e "${GREEN}✓ ACR environment credential created${NC}"

# Summary
echo ""
echo -e "${GREEN}============================================${NC}"
echo -e "${GREEN}Setup Complete!${NC}"
echo -e "${GREEN}============================================${NC}"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo "1. Go to your GitHub repository: https://github.com/${GITHUB_ORG}/${GITHUB_REPO}"
echo "2. Navigate to: Settings → Secrets and variables → Actions"
echo "3. Create these Repository secrets:"
echo ""
echo -e "${GREEN}AZURE_CLIENT_ID${NC}=${APP_ID}"
echo -e "${GREEN}AZURE_TENANT_ID${NC}=${TENANT_ID}"
echo -e "${GREEN}AZURE_SUBSCRIPTION_ID${NC}=${SUBSCRIPTION_ID}"
echo ""
echo -e "${YELLOW}Copy and save these values!${NC}"
echo ""

# Also save to a file
OUTPUT_FILE="github-oidc-credentials.txt"
cat > ${OUTPUT_FILE} <<EOF
GitHub OIDC Credentials for ${GITHUB_ORG}/${GITHUB_REPO}
Generated: $(date)

Add these secrets to GitHub:
========================================
AZURE_CLIENT_ID=${APP_ID}
AZURE_TENANT_ID=${TENANT_ID}
AZURE_SUBSCRIPTION_ID=${SUBSCRIPTION_ID}
========================================

Application Name: github-actions-ffwod
Service Principal Object ID: ${SP_ID}

Federated Credentials Created:
- github-main (for main branch)
- github-pr (for pull requests)
- github-env-dev (for dev environment)
- github-env-prod (for prod environment)
- github-env-acr (for ACR environment)

GitHub Settings URL:
https://github.com/${GITHUB_ORG}/${GITHUB_REPO}/settings/secrets/actions
EOF

echo -e "${GREEN}✓ Credentials saved to: ${OUTPUT_FILE}${NC}"
echo ""
echo -e "${YELLOW}Security Note:${NC}"
echo "- No secrets/passwords are generated (OIDC uses federated trust)"
echo "- GitHub Actions will authenticate using short-lived tokens"
echo "- More secure than using service principal secrets"
echo ""
