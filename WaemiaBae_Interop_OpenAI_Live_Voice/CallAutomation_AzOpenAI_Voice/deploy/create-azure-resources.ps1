<#
.SYNOPSIS
    Provisions Azure resources for the ACS Call Automation + OpenAI Voice application.

.DESCRIPTION
    Creates a Resource Group, Windows App Service Plan, and App Service with all required
    configuration for hosting the Call Automation application.

.PARAMETER ResourceGroupName
    Name of the Azure Resource Group to create.

.PARAMETER Location
    Azure region for resources (e.g., eastus, westus2, westeurope).

.PARAMETER AppServicePlanName
    Name of the App Service Plan.

.PARAMETER AppName
    Name of the App Service (will be the subdomain: <AppName>.azurewebsites.net).

.PARAMETER AppServicePlanSku
    Pricing tier for the App Service Plan. Default: B1 (minimum for WebSockets + 64-bit).

.EXAMPLE
    .\create-azure-resources.ps1 -ResourceGroupName "rg-callautomation" -Location "eastus" -AppName "my-call-bot"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$Location,

    [Parameter(Mandatory = $false)]
    [string]$AppServicePlanName = "$ResourceGroupName-plan",

    [Parameter(Mandatory = $true)]
    [string]$AppName,

    [Parameter(Mandatory = $false)]
    [ValidateSet("B1", "B2", "B3", "S1", "S2", "S3", "P1v2", "P2v2", "P3v2", "P1v3", "P2v3", "P3v3")]
    [string]$AppServicePlanSku = "B1"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " ACS Call Automation - Azure Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check Azure CLI is installed and logged in
Write-Host "Checking Azure CLI..." -ForegroundColor Yellow
try {
    $account = az account show 2>&1 | ConvertFrom-Json
    Write-Host "Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "Subscription: $($account.name)" -ForegroundColor Green
}
catch {
    Write-Error "Azure CLI not logged in. Run 'az login' first."
    exit 1
}

# 1. Create Resource Group
Write-Host ""
Write-Host "[1/4] Creating Resource Group '$ResourceGroupName' in '$Location'..." -ForegroundColor Yellow
az group create `
    --name $ResourceGroupName `
    --location $Location `
    --output none
Write-Host "  Resource Group created." -ForegroundColor Green

# 2. Create App Service Plan (Windows)
Write-Host "[2/4] Creating Windows App Service Plan '$AppServicePlanName' (SKU: $AppServicePlanSku)..." -ForegroundColor Yellow
az appservice plan create `
    --name $AppServicePlanName `
    --resource-group $ResourceGroupName `
    --sku $AppServicePlanSku `
    --output none
Write-Host "  App Service Plan created." -ForegroundColor Green

# 3. Create App Service
Write-Host "[3/4] Creating App Service '$AppName'..." -ForegroundColor Yellow
az webapp create `
    --name $AppName `
    --resource-group $ResourceGroupName `
    --plan $AppServicePlanName `
    --runtime "dotnet:8" `
    --output none

# Enable 64-bit platform
az webapp config set `
    --name $AppName `
    --resource-group $ResourceGroupName `
    --use-32bit-worker-process false `
    --output none

# Enable WebSockets
az webapp config set `
    --name $AppName `
    --resource-group $ResourceGroupName `
    --web-sockets-enabled true `
    --output none

# Enable always-on (keeps app warm)
az webapp config set `
    --name $AppName `
    --resource-group $ResourceGroupName `
    --always-on true `
    --output none

Write-Host "  App Service created (64-bit, WebSockets enabled, Always On)." -ForegroundColor Green

# 4. Configure Application Settings
Write-Host "[4/4] Configuring Application Settings..." -ForegroundColor Yellow

$appBaseUrl = "https://$AppName.azurewebsites.net"

az webapp config appsettings set `
    --name $AppName `
    --resource-group $ResourceGroupName `
    --settings `
        AppBaseUrl="$appBaseUrl" `
        AcsConnectionString="<REPLACE_WITH_ACS_CONNECTION_STRING>" `
        MediaSDKConnectionString="<REPLACE_WITH_MEDIA_SDK_CONNECTION_STRING>" `
        AzureOpenAIServiceKey="<REPLACE_WITH_OPENAI_KEY>" `
        AzureOpenAIServiceEndpoint="<REPLACE_WITH_OPENAI_ENDPOINT>" `
        AzureOpenAIDeploymentModelName="<REPLACE_WITH_MODEL_NAME>" `
        SystemPrompt="You are an AI assistant that helps people find information." `
    --output none

# Set health check path
az webapp config set `
    --name $AppName `
    --resource-group $ResourceGroupName `
    --generic-configurations '{"healthCheckPath": "/api/health"}' `
    --output none

Write-Host "  Application Settings configured." -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Deployment Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "App URL:          $appBaseUrl" -ForegroundColor White
Write-Host "Webhook Endpoint: $appBaseUrl/api/incomingCall" -ForegroundColor White
Write-Host "Health Check:     $appBaseUrl/api/health" -ForegroundColor White
Write-Host "Dashboard:        $appBaseUrl" -ForegroundColor White
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host "  1. Update Application Settings with real values:" -ForegroundColor White
Write-Host "     az webapp config appsettings set --name $AppName --resource-group $ResourceGroupName --settings AcsConnectionString='...' ..." -ForegroundColor DarkGray
Write-Host ""
Write-Host "  2. Deploy the application:" -ForegroundColor White
Write-Host "     .\deploy.ps1 -AppName $AppName -ResourceGroupName $ResourceGroupName" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  3. Register EventGrid webhook for IncomingCall event:" -ForegroundColor White
Write-Host "     Endpoint: $appBaseUrl/api/incomingCall" -ForegroundColor DarkGray
Write-Host "     See: https://learn.microsoft.com/azure/communication-services/concepts/call-automation/incoming-call-notification" -ForegroundColor DarkGray
