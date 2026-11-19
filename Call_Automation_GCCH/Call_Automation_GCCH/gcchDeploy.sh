#!/bin/bash

LOCATION='eastus'
RESOURCE_GROUP_NAME='waferwire-gcch-test'

WEB_APP_NAME='gcch-test-app'
APP_SERVICE_PLAN_NAME='gcch-test-app-service'
RUNTIME='DOTNETCORE|8.0'
SUBSCRIPTION_ID='<your subcription id>'

# Log in to Azure
az login

# Set the subscription
az account set --subscription $SUBSCRIPTION_ID

# Build the .NET API
dotnet publish -c Release -o ./publish

# Check if the publish directory exists
if [ -d "./publish" ]; then
    echo "Publish directory exists."
else
    echo "Publish directory does not exist."
    exit 1
fi

# Check if the resource group exists
if ! az group exists --name $RESOURCE_GROUP_NAME; then
    az group create --location $LOCATION --name $RESOURCE_GROUP_NAME
else
    echo "Resource group $RESOURCE_GROUP_NAME already exists."
fi

# Check if the app service plan exists
if ! az appservice plan show --name $APP_SERVICE_PLAN_NAME --resource-group $RESOURCE_GROUP_NAME > /dev/null 2>&1; then
    az appservice plan create --name $APP_SERVICE_PLAN_NAME --resource-group $RESOURCE_GROUP_NAME --sku B1 --is-linux
else
    echo "App service plan $APP_SERVICE_PLAN_NAME already exists."
fi

# Check if the web app exists
if ! az webapp show --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP_NAME > /dev/null 2>&1; then
    az webapp create --name $WEB_APP_NAME --runtime $RUNTIME --plan $APP_SERVICE_PLAN_NAME --resource-group $RESOURCE_GROUP_NAME
else
    echo "Web app $WEB_APP_NAME already exists."

fi
# Zip the published files using PowerShell
powershell.exe -Command "Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip"

# Check if the zip file was created
if [ -f "./publish.zip" ]; then
    echo "Zip file created successfully."
else
    echo "Failed to create zip file."
    exit 1
fi

# Deploy the .NET API
az webapp deployment source config-zip --resource-group $RESOURCE_GROUP_NAME --name $WEB_APP_NAME --src ./publish.zip
