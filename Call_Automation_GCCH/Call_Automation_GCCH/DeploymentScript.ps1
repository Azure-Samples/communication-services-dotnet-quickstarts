# ===============================================================================
# DEPLOYMENT SCRIPT - Call Automation Dialog Application
# ===============================================================================
#
# WHERE TO RUN THIS SCRIPT:
# -------------------------
# This script should be run from your LOCAL DEVELOPMENT MACHINE where:
# 1. You have the source code of the .NET application
# 2. You have admin/elevated PowerShell permissions
# 3. You are in the root directory of your .NET project (where the .csproj file is located)
#
# PREREQUISITES:
# --------------
# Before running this script, ensure you have:
# 1. Azure PowerShell module installed: Install-Module -Name Az -Force
# 2. .NET SDK installed (version compatible with your project)
# 3. Valid Azure US Government credentials with permissions to:
#    - Access the specified subscription
#    - Manage VMs in the resource group
#    - Execute run commands on the target VM
# 4. The target VM should have:
#    - IIS installed and configured
#    - PowerShell execution policy allowing script execution
#    - C:\inetpub\wwwroot directory accessible
#
# USAGE:
# ------
# 1. Fill in the required variables below (SubscriptionId, RESOURCE_GROUP_NAME, VM_NAME)
# 2. Open PowerShell as Administrator
# 3. Navigate to your .NET project root directory: cd "path\to\your\project"
# 4. Run: .\DeploymentScript.ps1
#
# WHAT THIS SCRIPT DOES:
# ----------------------
# 1. Connects to Azure US Government cloud
# 2. Publishes your .NET application locally
# 3. Compresses the published files into a ZIP
# 4. Uploads the ZIP to the target VM via Azure Run Command
# 5. Extracts and deploys the app directly to IIS web root (C:\inetpub\wwwroot)
# 6. Restarts IIS to apply changes
#
# ===============================================================================

# --- [User Configurable Variables] ---
$SubscriptionId = ''
$RESOURCE_GROUP_NAME = ''
$VM_NAME = ''
$PublishFolder = ".\publish"
$ZipOutputPath = ".\publish.zip"

# --- [Login & Set Context] ---
# Note: This will prompt for device authentication in Azure US Government
Connect-AzAccount -Environment AzureUSGovernment -UseDeviceAuthentication
Set-AzContext -SubscriptionId $SubscriptionId

# --- [Publish .NET App] ---
# This command runs locally and creates the publish folder with compiled application
dotnet publish -c Release -o $PublishFolder

if (!(Test-Path -Path $PublishFolder)) {
    Write-Host "Publish folder not found."; exit 1
}

# Compress only the CONTENTS of the publish folder
Compress-Archive -Path (Get-ChildItem -Path $PublishFolder) -DestinationPath $ZipOutputPath -Force


# --- [Upload ZIP to VM] ---
# The following commands execute ON THE TARGET VM via Azure Run Command
$encodedZip = [Convert]::ToBase64String([IO.File]::ReadAllBytes($ZipOutputPath))
$uploadScript = @"
`$encoded = @'
$encodedZip
'@
New-Item -Path 'C:\Temp' -ItemType Directory -Force | Out-Null
[System.IO.File]::WriteAllBytes('C:\Temp\publish.zip', [Convert]::FromBase64String(`$encoded))
"@

# This executes the upload script on the remote VM
Invoke-AzVMRunCommand -ResourceGroupName $RESOURCE_GROUP_NAME -VMName $VM_NAME `
  -CommandId 'RunPowerShellScript' `
  -ScriptString $uploadScript

# --- [Deploy App to IIS Web Root] ---
# This script also executes ON THE TARGET VM
# Note: This deploys directly to the wwwroot directory, overwriting existing files
$deployScript = @'
$webAppPath = "C:\inetpub\wwwroot"
Expand-Archive -Path "C:\Temp\publish.zip" -DestinationPath $webAppPath -Force
iisreset
'@

# This executes the deployment script on the remote VM
Invoke-AzVMRunCommand -ResourceGroupName $RESOURCE_GROUP_NAME -VMName $VM_NAME `
  -CommandId 'RunPowerShellScript' `
  -ScriptString $deployScript
 