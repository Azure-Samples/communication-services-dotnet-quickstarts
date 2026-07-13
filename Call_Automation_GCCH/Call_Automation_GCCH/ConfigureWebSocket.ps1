# Azure App Service WebSocket Configuration Script
# Run this after deploying to ensure WebSocket support is enabled

param(
	[Parameter(Mandatory=$true)]
	[string]$ResourceGroupName,

	[Parameter(Mandatory=$true)]
	[string]$AppServiceName
)

Write-Host "====================================================================" -ForegroundColor Cyan
Write-Host "  Azure App Service - WebSocket Configuration" -ForegroundColor Cyan
Write-Host "====================================================================" -ForegroundColor Cyan
Write-Host ""

# Check if logged in to Azure
try {
	$context = Get-AzContext
	if (!$context) {
		Write-Host "Not logged in to Azure. Running Connect-AzAccount..." -ForegroundColor Yellow
		Connect-AzAccount
	}
} catch {
	Write-Host "Not logged in to Azure. Running Connect-AzAccount..." -ForegroundColor Yellow
	Connect-AzAccount
}

Write-Host "Current Subscription: $($context.Subscription.Name)" -ForegroundColor Green
Write-Host ""

# Enable WebSocket
Write-Host "[1/5] Enabling WebSocket support..." -ForegroundColor Yellow
try {
	$webApp = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName
	$webApp.SiteConfig.WebSocketsEnabled = $true
	Set-AzWebApp -WebApp $webApp
	Write-Host "✓ WebSocket enabled successfully" -ForegroundColor Green
} catch {
	Write-Host "✗ Failed to enable WebSocket: $_" -ForegroundColor Red
	exit 1
}

# Enable Always On (recommended for WebSocket)
Write-Host "[2/5] Enabling Always On..." -ForegroundColor Yellow
try {
	$webApp.SiteConfig.AlwaysOn = $true
	Set-AzWebApp -WebApp $webApp
	Write-Host "✓ Always On enabled successfully" -ForegroundColor Green
} catch {
	Write-Host "✗ Failed to enable Always On: $_" -ForegroundColor Red
}

# Set HTTPS Only
Write-Host "[3/5] Configuring HTTPS Only..." -ForegroundColor Yellow
try {
	Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -HttpsOnly $true
	Write-Host "✓ HTTPS Only enabled successfully" -ForegroundColor Green
} catch {
	Write-Host "✗ Failed to configure HTTPS Only: $_" -ForegroundColor Red
}

# Set minimum TLS version
Write-Host "[4/5] Setting minimum TLS version to 1.2..." -ForegroundColor Yellow
try {
	$webApp.SiteConfig.MinTlsVersion = "1.2"
	Set-AzWebApp -WebApp $webApp
	Write-Host "✓ TLS 1.2 configured successfully" -ForegroundColor Green
} catch {
	Write-Host "✗ Failed to set TLS version: $_" -ForegroundColor Red
}

# Add Application Settings for WebSocket
Write-Host "[5/5] Configuring Application Settings..." -ForegroundColor Yellow
try {
	$appSettings = @{
		"WEBSITE_WEBHOST_ENABLED" = "true"
		"ASPNETCORE_ENVIRONMENT" = "Production"
	}

	Set-AzWebApp -ResourceGroupName $ResourceGroupName `
				 -Name $AppServiceName `
				 -AppSettings $appSettings
	Write-Host "✓ Application Settings configured successfully" -ForegroundColor Green
} catch {
	Write-Host "✗ Failed to configure Application Settings: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "====================================================================" -ForegroundColor Cyan
Write-Host "  Configuration Complete!" -ForegroundColor Green
Write-Host "====================================================================" -ForegroundColor Cyan
Write-Host ""

# Display current configuration
Write-Host "Current Configuration:" -ForegroundColor Cyan
Write-Host "  - WebSocket Enabled: $($webApp.SiteConfig.WebSocketsEnabled)" -ForegroundColor White
Write-Host "  - Always On: $($webApp.SiteConfig.AlwaysOn)" -ForegroundColor White
Write-Host "  - HTTPS Only: $(Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName).HttpsOnly" -ForegroundColor White
Write-Host "  - Min TLS Version: $($webApp.SiteConfig.MinTlsVersion)" -ForegroundColor White
Write-Host ""

Write-Host "App Service URL: https://$AppServiceName.azurewebsites.us" -ForegroundColor Yellow
Write-Host "WebSocket URL: wss://$AppServiceName.azurewebsites.us/ws" -ForegroundColor Yellow
Write-Host ""

# Test WebSocket endpoint
Write-Host "Testing WebSocket endpoint availability..." -ForegroundColor Yellow
try {
	$response = Invoke-WebRequest -Uri "https://$AppServiceName.azurewebsites.us" -Method GET
	if ($response.StatusCode -eq 200) {
		Write-Host "✓ App Service is responding" -ForegroundColor Green
	}
} catch {
	Write-Host "⚠ App Service may still be starting up: $_" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Test the application at: https://$AppServiceName.azurewebsites.us" -ForegroundColor White
Write-Host "  2. Test WebSocket connection at: wss://$AppServiceName.azurewebsites.us/ws" -ForegroundColor White
Write-Host "  3. Check logs in Azure Portal if needed" -ForegroundColor White
Write-Host ""
