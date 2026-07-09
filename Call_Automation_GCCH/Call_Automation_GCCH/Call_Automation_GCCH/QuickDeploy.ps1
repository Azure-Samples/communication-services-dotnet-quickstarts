# Quick Deploy to Azure App Service
# Simplified deployment script for gcch-contoso-app

param(
	[string]$SubscriptionId = "273e5c93-9407-4977-b458-f4a266cd849a",
	[string]$ResourceGroupName = "waferwire-rg",
	[string]$AppServiceName = "gcch-contoso-app",
	[string]$AppServicePlan = "gcch-contoso-plan",
	[string]$Location = "USGov Texas"
)

$ErrorActionPreference = "Stop"

Write-Host "`n=================================================" -ForegroundColor Cyan
Write-Host "Quick Deploy to Azure App Service (GCCH)" -ForegroundColor Cyan
Write-Host "=================================================`n" -ForegroundColor Cyan

# Step 1: Connect to Azure US Government
Write-Host "[1/7] Connecting to Azure US Government..." -ForegroundColor Yellow
try {
	$context = Get-AzContext
	if ($null -eq $context -or $context.Environment.Name -ne "AzureUSGovernment") {
		Write-Host "  Not connected. Connecting..." -ForegroundColor Gray
		Connect-AzAccount -Environment AzureUSGovernment -ErrorAction Stop | Out-Null
		$context = Get-AzContext
	}
	Write-Host "  ✓ Connected as: $($context.Account.Id)" -ForegroundColor Green
} catch {
	Write-Host "  ✗ Failed to connect: $_" -ForegroundColor Red
	exit 1
}

# Step 2: Set subscription context
Write-Host "`n[2/7] Setting subscription context..." -ForegroundColor Yellow
try {
	Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop | Out-Null
	$context = Get-AzContext
	Write-Host "  ✓ Subscription: $($context.Subscription.Name)" -ForegroundColor Green
	Write-Host "  ✓ Environment: $($context.Environment.Name)" -ForegroundColor Green
} catch {
	Write-Host "  ✗ Failed to set context: $_" -ForegroundColor Red
	exit 1
}

# Step 3: Verify/Create Resource Group
Write-Host "`n[3/7] Checking resource group..." -ForegroundColor Yellow
try {
	$rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
	if ($null -eq $rg) {
		Write-Host "  Creating resource group: $ResourceGroupName..." -ForegroundColor Gray
		New-AzResourceGroup -Name $ResourceGroupName -Location $Location -ErrorAction Stop | Out-Null
		Write-Host "  ✓ Resource group created" -ForegroundColor Green
	} else {
		Write-Host "  ✓ Resource group exists: $ResourceGroupName" -ForegroundColor Green
	}
} catch {
	Write-Host "  ✗ Failed to create/verify resource group: $_" -ForegroundColor Red
	exit 1
}

# Step 4: Verify/Create App Service Plan
Write-Host "`n[4/7] Checking App Service Plan..." -ForegroundColor Yellow
try {
	$plan = Get-AzAppServicePlan -ResourceGroupName $ResourceGroupName -Name $AppServicePlan -ErrorAction SilentlyContinue
	if ($null -eq $plan) {
		Write-Host "  Creating App Service Plan: $AppServicePlan (B1)..." -ForegroundColor Gray
		New-AzAppServicePlan -ResourceGroupName $ResourceGroupName -Name $AppServicePlan -Location $Location -Tier "Basic" -NumberofWorkers 1 -WorkerSize "Small" -ErrorAction Stop | Out-Null
		Write-Host "  ✓ App Service Plan created" -ForegroundColor Green
	} else {
		Write-Host "  ✓ App Service Plan exists: $AppServicePlan" -ForegroundColor Green
		Write-Host "    Tier: $($plan.Sku.Tier), Size: $($plan.Sku.Size)" -ForegroundColor Gray
	}
} catch {
	Write-Host "  ✗ Failed to create/verify App Service Plan: $_" -ForegroundColor Red
	exit 1
}

# Step 5: Verify/Create Web App
Write-Host "`n[5/7] Checking Web App..." -ForegroundColor Yellow
try {
	$webApp = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -ErrorAction SilentlyContinue
	if ($null -eq $webApp) {
		Write-Host "  Creating Web App: $AppServiceName..." -ForegroundColor Gray
		New-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -Location $Location -AppServicePlan $AppServicePlan -ErrorAction Stop | Out-Null
		Write-Host "  ✓ Web App created" -ForegroundColor Green
	} else {
		Write-Host "  ✓ Web App exists: $AppServiceName" -ForegroundColor Green
		Write-Host "    URL: https://$($webApp.DefaultHostName)" -ForegroundColor Gray
		Write-Host "    State: $($webApp.State)" -ForegroundColor Gray
	}

	# Ensure HTTPS-only is enabled
	Write-Host "  Enabling HTTPS-only..." -ForegroundColor Gray
	Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -HttpsOnly $true -ErrorAction Stop | Out-Null
	Write-Host "  ✓ HTTPS-only enabled" -ForegroundColor Green
} catch {
	Write-Host "  ✗ Failed to create/configure Web App: $_" -ForegroundColor Red
	exit 1
}

# Step 6: Build and Publish the App
Write-Host "`n[6/7] Building and publishing application..." -ForegroundColor Yellow
$projectPath = Split-Path -Parent $PSScriptRoot
$publishPath = Join-Path $projectPath "publish"
$zipPath = Join-Path $projectPath "publish.zip"

try {
	# Clean previous publish
	if (Test-Path $publishPath) {
		Remove-Item $publishPath -Recurse -Force
	}
	if (Test-Path $zipPath) {
		Remove-Item $zipPath -Force
	}

	# Build and publish
	Write-Host "  Building project..." -ForegroundColor Gray
	Push-Location $projectPath
	dotnet build -c Release --nologo
	if ($LASTEXITCODE -ne 0) {
		throw "Build failed with exit code $LASTEXITCODE"
	}

	Write-Host "  Publishing to $publishPath..." -ForegroundColor Gray
	dotnet publish -c Release -o $publishPath --nologo
	if ($LASTEXITCODE -ne 0) {
		throw "Publish failed with exit code $LASTEXITCODE"
	}
	Pop-Location

	# Create ZIP package
	Write-Host "  Creating deployment package..." -ForegroundColor Gray
	Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force
	$zipSize = (Get-Item $zipPath).Length / 1MB
	Write-Host "  ✓ Package created: publish.zip ($([math]::Round($zipSize, 2)) MB)" -ForegroundColor Green
} catch {
	Pop-Location
	Write-Host "  ✗ Build/Publish failed: $_" -ForegroundColor Red
	exit 1
}

# Step 7: Deploy to App Service
Write-Host "`n[7/7] Deploying to Azure App Service..." -ForegroundColor Yellow
Write-Host "  This may take 2-3 minutes..." -ForegroundColor Gray
try {
	Publish-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -ArchivePath $zipPath -Force -ErrorAction Stop | Out-Null
	Write-Host "  ✓ Deployment successful!" -ForegroundColor Green
} catch {
	Write-Host "  ✗ Deployment failed: $_" -ForegroundColor Red
	Write-Host "  You can try deploying manually via Azure Portal or Visual Studio" -ForegroundColor Yellow
	exit 1
}

# Verify deployment
Write-Host "`nVerifying deployment..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
try {
	$webApp = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -ErrorAction Stop
	Write-Host "  ✓ App State: $($webApp.State)" -ForegroundColor Green
	Write-Host "  ✓ Default Hostname: $($webApp.DefaultHostName)" -ForegroundColor Green
	Write-Host "  ✓ HTTPS Only: $($webApp.HttpsOnly)" -ForegroundColor Green
} catch {
	Write-Host "  ⚠ Could not verify deployment status" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=================================================" -ForegroundColor Cyan
Write-Host "✓ Deployment Complete!" -ForegroundColor Green
Write-Host "=================================================`n" -ForegroundColor Cyan

Write-Host "App Service URL: https://$AppServiceName.azurewebsites.us" -ForegroundColor White
Write-Host "Swagger UI: https://$AppServiceName.azurewebsites.us/swagger`n" -ForegroundColor White

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Configure app settings: .\ConfigureAppServiceSettings.ps1" -ForegroundColor White
Write-Host "  2. Update Event Grid: .\UpdateEventGridToAppService.ps1" -ForegroundColor White
Write-Host "  3. Test the deployment: Invoke-WebRequest https://$AppServiceName.azurewebsites.us/swagger -UseBasicParsing`n" -ForegroundColor White

# Cleanup
Write-Host "Cleaning up temporary files..." -ForegroundColor Gray
if (Test-Path $zipPath) {
	Remove-Item $zipPath -Force
}
Write-Host "✓ Done`n" -ForegroundColor Green
