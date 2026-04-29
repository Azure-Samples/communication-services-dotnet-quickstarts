<#
.SYNOPSIS
    Publishes and deploys the application to Azure App Service.

.PARAMETER AppName
    Name of the Azure App Service.

.PARAMETER ResourceGroupName
    Name of the Azure Resource Group containing the App Service.

.EXAMPLE
    .\deploy.ps1 -AppName "my-call-bot" -ResourceGroupName "rg-callautomation"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$AppName,

    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectDir "bin\publish"
$zipPath = Join-Path $projectDir "bin\deploy.zip"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Publishing & Deploying to Azure" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Clean previous output
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

# 2. Publish for win-x64
Write-Host ""
Write-Host "[1/3] Publishing for win-x64..." -ForegroundColor Yellow
dotnet publish $projectDir `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed."
    exit 1
}

# Verify native binaries are included
$nativeDir = Join-Path $publishDir "runtimes\win-x64\native"
if (Test-Path $nativeDir) {
    $nativeFiles = Get-ChildItem $nativeDir | Measure-Object
    Write-Host "  Native binaries: $($nativeFiles.Count) files in runtimes\win-x64\native" -ForegroundColor Green
}
else {
    Write-Warning "  Native binaries directory not found at $nativeDir"
}

# Verify mediasdk.toml in both root and native runtime folder
$tomlRootPath = Join-Path $publishDir "mediasdk.toml"
$tomlNativePath = Join-Path $nativeDir "mediasdk.toml"

if (Test-Path $tomlRootPath) {
    Write-Host "  mediasdk.toml (root): present" -ForegroundColor Green
}
else {
    Write-Warning "  mediasdk.toml not found in publish root"
}

if (Test-Path $tomlNativePath) {
    Write-Host "  mediasdk.toml (native): present" -ForegroundColor Green
}
else {
    Write-Warning "  mediasdk.toml not found in native directory"
}

# 3. Create zip
Write-Host "[2/3] Creating deployment zip..." -ForegroundColor Yellow
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force
$zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "  Zip created: $zipSize MB" -ForegroundColor Green

# 4. Deploy to App Service (zip deploy to wwwroot)
Write-Host "[3/3] Deploying to Azure App Service '$AppName'..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --name $AppName `
    --resource-group $ResourceGroupName `
    --src $zipPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed."
    exit 1
}

$appUrl = "https://$AppName.azurewebsites.net"
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Deployment Successful!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "App URL:     $appUrl" -ForegroundColor White
Write-Host "Dashboard:   $appUrl" -ForegroundColor White
Write-Host "Health:      $appUrl/api/health" -ForegroundColor White
Write-Host "Webhook:     $appUrl/api/incomingCall" -ForegroundColor White

# Clean up
Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
Write-Host ""
Write-Host "Cleaned up build artifacts." -ForegroundColor DarkGray
