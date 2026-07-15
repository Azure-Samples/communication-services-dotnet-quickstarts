# Configure App Service Settings for ACS Call Automation
# This script sets the required configuration for Azure Communication Services

param(
	[string]$SubscriptionId = "273e5c93-9407-4977-b458-f4a266cd849a",
	[string]$ResourceGroupName = "waferwire-rg",
	[string]$AppServiceName = "gcch-contoso-app",

	# ACS Configuration - REQUIRED
	[Parameter(Mandatory=$true)]
	[string]$AcsConnectionString,

	[Parameter(Mandatory=$true)]
	[string]$AcsPhoneNumber,

	# Optional settings
	[string]$PmaEndpoint = "https://govtx-02.pma.gov.teams.microsoft.us:38000",
	[string]$AudioFileUrl = ""
)

Write-Host "`n=================================================" -ForegroundColor Cyan
Write-Host "Configure App Service Settings for ACS" -ForegroundColor Cyan
Write-Host "=================================================`n" -ForegroundColor Cyan

# Connect to Azure
Write-Host "[1/4] Connecting to Azure US Government..." -ForegroundColor Yellow
try {
	$context = Get-AzContext
	if ($null -eq $context -or $context.Environment.Name -ne "AzureUSGovernment") {
		Connect-AzAccount -Environment AzureUSGovernment -ErrorAction Stop | Out-Null
	}
	Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop | Out-Null
	Write-Host "✓ Connected to subscription: $SubscriptionId" -ForegroundColor Green
} catch {
	Write-Host "✗ Failed to connect: $_" -ForegroundColor Red
	exit 1
}

# Get current app settings
Write-Host "`n[2/4] Reading current App Service settings..." -ForegroundColor Yellow
try {
	$webApp = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -ErrorAction Stop
	$currentSettings = @{}
	if ($webApp.SiteConfig.AppSettings) {
		foreach ($setting in $webApp.SiteConfig.AppSettings) {
			$currentSettings[$setting.Name] = $setting.Value
		}
	}
	Write-Host "✓ Found $($currentSettings.Count) existing settings" -ForegroundColor Green
} catch {
	Write-Host "✗ Failed to read settings: $_" -ForegroundColor Red
	exit 1
}

# Build the App Service callback URL
$callbackHost = "https://$AppServiceName.azurewebsites.us"

# Prepare new settings
Write-Host "`n[3/4] Preparing configuration settings..." -ForegroundColor Yellow
$newSettings = @{
	"CommunicationSettings__AcsConnectionString" = $AcsConnectionString
	"CommunicationSettings__AcsPhoneNumber" = $AcsPhoneNumber
	"CommunicationSettings__CallbackUriHost" = $callbackHost
	"CommunicationSettings__PmaEndpoint" = $PmaEndpoint
	"ASPNETCORE_ENVIRONMENT" = "Production"
}

if (-not [string]::IsNullOrWhiteSpace($AudioFileUrl)) {
	$newSettings["CommunicationSettings__AudioFileUrl"] = $AudioFileUrl
}

# Merge with existing settings (preserve system settings like WEBSITE_*)
foreach ($key in $currentSettings.Keys) {
	if (-not $newSettings.ContainsKey($key) -and $key -like "WEBSITE_*") {
		$newSettings[$key] = $currentSettings[$key]
	}
}

Write-Host "  Configuration to be applied:" -ForegroundColor Gray
foreach ($key in $newSettings.Keys) {
	if ($key -like "*ConnectionString*") {
		Write-Host "    $key = ***REDACTED***" -ForegroundColor Gray
	} else {
		Write-Host "    $key = $($newSettings[$key])" -ForegroundColor Gray
	}
}

# Apply settings
Write-Host "`n[4/4] Applying configuration to App Service..." -ForegroundColor Yellow
try {
	Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -AppSettings $newSettings -ErrorAction Stop | Out-Null
	Write-Host "✓ Configuration applied successfully" -ForegroundColor Green
} catch {
	Write-Host "✗ Failed to apply settings: $_" -ForegroundColor Red
	exit 1
}

# Restart the app to apply changes
Write-Host "`nRestarting App Service to apply changes..." -ForegroundColor Yellow
try {
	Restart-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -ErrorAction Stop | Out-Null
	Write-Host "✓ App Service restarted" -ForegroundColor Green
} catch {
	Write-Host "⚠ Failed to restart: $_" -ForegroundColor Yellow
}

Write-Host "`n=================================================" -ForegroundColor Cyan
Write-Host "✓ Configuration Complete!" -ForegroundColor Green
Write-Host "=================================================`n" -ForegroundColor Cyan

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Verify app is running: $callbackHost/swagger" -ForegroundColor White
Write-Host "  2. Configure Event Grid subscription (see README.md)" -ForegroundColor White
Write-Host "  3. Test by making a call to: $AcsPhoneNumber" -ForegroundColor White
Write-Host "  4. Monitor logs in Azure Portal > App Service > Log stream`n" -ForegroundColor White
