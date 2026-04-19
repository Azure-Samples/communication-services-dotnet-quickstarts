#!/bin/bash

# Configuration
LOCATION='eastus'
RESOURCE_GROUP_NAME='waferwire-dotnet-ga5-test-app'
WEB_APP_NAME='waferwire-dotnet-ga5-test-app'
APP_SERVICE_PLAN_NAME='waferwire-ga5-test-app-service'
RUNTIME='DOTNETCORE|8.0'  # Adjusted runtime format
SUBSCRIPTION_ID='50ad1522-5c2c-4d9a-a6c8-67c11ecb75b8'

# Log in to Azure
az login || { echo "Azure login failed"; exit 1; }

# Set the subscription
az account set --subscription "$SUBSCRIPTION_ID" || { echo "Failed to set subscription"; exit 1; }

# Confirm subscription context
echo "Using subscription:"
az account show --output table

# Build the .NET API
dotnet publish -c Release -o ./publish || { echo "Build failed"; exit 1; }

# Check if the publish directory exists
if [ ! -d "./publish" ]; then
    echo "Publish directory does not exist."
    exit 1
fi

# Check if the resource group exists
if [ "$(az group exists --name $RESOURCE_GROUP_NAME)" = "false" ]; then
    echo "Creating resource group $RESOURCE_GROUP_NAME..."
    az group create --location "$LOCATION" --name "$RESOURCE_GROUP_NAME" || { echo "Failed to create resource group"; exit 1; }
    sleep 5
else
    echo "Resource group $RESOURCE_GROUP_NAME already exists."
fi

# Check if the app service plan exists
if ! az appservice plan show --name "$APP_SERVICE_PLAN_NAME" --resource-group "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
    echo "Creating app service plan $APP_SERVICE_PLAN_NAME..."
    az appservice plan create --name "$APP_SERVICE_PLAN_NAME" --resource-group "$RESOURCE_GROUP_NAME" --sku B1 --is-linux || { echo "Failed to create app service plan"; exit 1; }
else
    echo "App service plan $APP_SERVICE_PLAN_NAME already exists."
fi

# Check if the web app exists
if ! az webapp show --name "$WEB_APP_NAME" --resource-group "$RESOURCE_GROUP_NAME" > /dev/null 2>&1; then
    echo "Creating web app $WEB_APP_NAME..."
    az webapp create --name "$WEB_APP_NAME" --runtime "$RUNTIME" --plan "$APP_SERVICE_PLAN_NAME" --resource-group "$RESOURCE_GROUP_NAME" || { echo "Failed to create web app"; exit 1; }
else
    echo "Web app $WEB_APP_NAME already exists."
fi

# Zip the published files using PowerShell
powershell.exe -Command "Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip"

# Deploy the .NET API
az webapp deployment source config-zip --resource-group "$RESOURCE_GROUP_NAME" --name "$WEB_APP_NAME" --src ./publish.zip || { echo "Deployment failed"; exit 1; }

echo "Deployment completed successfully."