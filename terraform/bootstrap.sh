#!/bin/bash
set -e

LOCATION="swedencentral"
STATE_RG="ffwod-state-rg"
SHARED_RG="ffwod-shared-rg"
STORAGE_ACCOUNT="ffwodtfstate"
CONTAINER_NAME="tfstate"
ACR_NAME="ffwodacr"

echo "========================================="
echo "FamilyFitness Bootstrap - Azure Setup"
echo "========================================="
echo ""

# Create state storage resource group
echo "Creating state storage resource group..."
az group create --name $STATE_RG --location $LOCATION

# Create storage account for Terraform state
echo "Creating storage account for Terraform state..."
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $STATE_RG \
  --location $LOCATION \
  --sku Standard_LRS \
  --encryption-services blob \
  --min-tls-version TLS1_2

# Get storage account key
echo "Getting storage account key..."
ACCOUNT_KEY=$(az storage account keys list \
  --resource-group $STATE_RG \
  --account-name $STORAGE_ACCOUNT \
  --query '[0].value' -o tsv)

# Create blob container for state
echo "Creating blob container for Terraform state..."
az storage container create \
  --name $CONTAINER_NAME \
  --account-name $STORAGE_ACCOUNT \
  --account-key $ACCOUNT_KEY

# Create shared resources resource group
echo "Creating shared resources resource group..."
az group create --name $SHARED_RG --location $LOCATION

# Create Azure Container Registry
echo "Creating Azure Container Registry..."
az acr create \
  --resource-group $SHARED_RG \
  --name $ACR_NAME \
  --sku Standard \
  --location $LOCATION \
  --admin-enabled true

# Get ACR credentials
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query "passwords[0].value" -o tsv)
ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --query loginServer -o tsv)

echo ""
echo "========================================="
echo "Bootstrap Complete!"
echo "========================================="
echo ""
echo "STATE STORAGE:"
echo "  Resource Group: $STATE_RG"
echo "  Storage Account: $STORAGE_ACCOUNT"
echo "  Container: $CONTAINER_NAME"
echo ""
echo "CONTAINER REGISTRY:"
echo "  Resource Group: $SHARED_RG"
echo "  Name: $ACR_NAME"
echo "  Login Server: $ACR_LOGIN_SERVER"
echo "  Username: $ACR_USERNAME"
echo ""
echo "========================================="
echo "NEXT STEPS:"
echo "========================================="
echo ""
echo "1. Add these GitHub Secrets:"
echo "   ACR_LOGIN_SERVER=$ACR_LOGIN_SERVER"
echo "   ACR_USERNAME=$ACR_USERNAME"
echo "   ACR_PASSWORD=$ACR_PASSWORD"
echo ""
echo "2. Your backend.tf is already configured with:"
echo "   resource_group_name  = \"$STATE_RG\""
echo "   storage_account_name = \"$STORAGE_ACCOUNT\""
echo "   container_name       = \"$CONTAINER_NAME\""
echo ""
echo "3. Deploy environments with:"
echo "   tofu init -backend-config=\"key=dev.tfstate\""
echo "   tofu apply -var-file=\"dev.tfvars\""
echo ""
