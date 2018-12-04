<#
.SYNOPSIS
    Deploys Proposal Manager
.DESCRIPTION
    Installs all the required Proposal Manager assets and deploys Proposal Manager website.
.PARAMETER PMSharePointSiteAlias
    The name of the SharePoint site to create for Proposal Manager (`proposalmanager` is ok most of the times).
.PARAMETER PMAdminUpn
    The upn of the user that will be made administrator of this instance of Proposal Manager. It can be yourself or someone else.
.PARAMETER OfficeTenantName
    The name of the office tenant. For example, if your mail domain is @contoso.onmicrosoft.com, then the name of the tenant is "contoso".
.PARAMETER AzureResourceLocation
    The azure region in which you want the resources to be allocated (for example, "East US").
.PARAMETER ApplicationName
    The name of the application (for example "proposalmanager")
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory = $false)]
  [string]$PMSharePointSiteAlias,
  [Parameter(Mandatory = $true)]
  [string]$PMAdminUpn,
  [Parameter(Mandatory = $true)]
  [string]$PMSiteAlias,
  [Parameter(Mandatory = $true)]
  [string]$OfficeTenantName,
  [Parameter(Mandatory = $true)]
  [string]$AzureResourceLocation,
  [Parameter(Mandatory = $false)]
  [string]$ApplicationName
)

# Check Pre-requisites
Write-Host ""
Write-Host -ForegroundColor Cyan "Checking pre-requisites"

# Verify npm is installed
try
{
    write-host "The NPM version is: $(npm -v)"
}
catch
{
    Write-Host "You need to install npm. Please do so by navigating to http://nodejs.org"
    exit
}

# Verify dotnet core is installed
try
{
    write-host "The .NET Core version is: $(dotnet --version)"
}
catch
{
    Write-Host "You need to install the dotnet core sdk 2.1. Please do so by navigating to https://dotnet.microsoft.com/download/thank-you/dotnet-sdk-2.1.500-windows-x64-installer"
    exit
}

# Add references to utility scripts
. .\CheckPowerShellModule.ps1
. .\ProposalManagementSetupUtilities.ps1
. .\RegisterAppV2.ps1
. .\UpdateAppSettings.ps1
. .\UpdateAppSettingsJS.ps1

# Verify required modules are available
Write-Host -ForegroundColor Cyan "Checking required PS modules"
$modules = @("AzureRm", "Microsoft.Online.SharePoint.Powershell", "SharePointPnPPowerShellOnline", "Azure", "AzureAD")

foreach($module in $modules)
{
    Verify-Module -ModuleName $module
}

# Prompt the user one time for credentials to use across all necessary connections (like SSO)
Write-Host "Installation of Proposal Manager will begin. You need to be a Global Administrator to continue." -ForegroundColor Magenta
$global:credential = Get-Credential -Message "Enter your credentials"

if(!$PMSharePointSiteAlias)
{
    $PMSharePointSiteAlias = "https://$OfficeTenantName-admin.sharepoint.com"
}

# Create SharePoint Site
$pmSiteUrl = New-PMSharePointSite -AdminSiteUrl $PMSharePointSiteAlias -PMAdminUpn $PMAdminUpn -PMSiteAlias $PMSiteAlias

New-PMGroupStructure -PMAdminUpn $PMAdminUpn

if(!$ApplicationName)
{
    $ApplicationName = "propmgr-$OfficeTenantName"
}

# Register Azure AD application (Endpoint v2)
RegisterApp -applicationName $ApplicationName

# Create Service Principal
Write-Host "Creating Service Principal" -ForegroundColor Cyan
Connect-AzureRmAccount -Credential $global:credential
New-AzureRmADServicePrincipal -ApplicationId $global:appId
Disconnect-AzureRmAccount

Connect-AzureAD -Credential $global:credential
$tenantId = (Get-AzureADTenantDetail).ObjectId
Disconnect-AzureAD

$appSettings = @{ClientId = $global:appId; ClientSecret = $global:secretText; TenantId = $tenantId; TenantName = $OfficeTenantName; BaseUrl = "https://$ApplicationName.azurewebsites.net";}

# Update Proposal Manager application settings
UpdateAppSettings -pathToJson ..\WebReact\appsettings.json -inputParams $appSettings
UpdateAppSettingsClient -pathToJson ..\WebReact\ClientApp\src\helpers\AppSettings.js -appId $global:appId -appUri "https://$ApplicationName.azurewebsites.net" -tenantId $tenantId

cd ..\WebReact\ClientApp

# Install all required dependencies
npm install

cd ..\..\Setup

# Publish Proposal Manager
$solutionDir = (Get-Item -Path "..\").FullName
.\nuget.exe restore ..\Dynamics Integration\OneDriveSubscriptionRenewal\OneDriveSubscriptionRenewal.csproj -SolutionDirectory ..\
cd "..\Dynamics Integration\OneDriveSubscriptionRenewal"
dotnet msbuild "OneDriveSubscriptionRenewal.csproj" "/p:SolutionDir=`"$($solutionDir)\\`""
cd ..\..\Setup
.\nuget.exe restore ..\Utilities\OpportunitySiteProvisioner\OpportunitySiteProvisioner.csproj -SolutionDirectory ..\
cd "..\Utilities\OpportunitySiteProvisioner"
dotnet msbuild "OpportunitySiteProvisioner.csproj" "/p:SolutionDir=`"$($solutionDir)\\`""
cd ..\..\Setup
rd ..\WebReact\bin\Release\netcoreapp2.1\publish -Recurse -ErrorAction Ignore
dotnet publish ..\WebReact -c Release

New-PMSite -PMSiteLocation $AzureResourceLocation -ApplicationName $ApplicationName

$applicationDomain = "$ApplicationName.azurewebsites.net"
$applicationUrl = "https://$applicationDomain"

New-PMTeamsAddInManifest -AppUrl $applicationUrl -AppDomain $applicationDomain

# Grant Admin Consent
$adminConsentUrl = "https://login.microsoftonline.com/common/adminconsent?client_id=$($global:appId)&state=12345&redirect_uri=$applicationUrl"

Start-Process $adminConsentUrl

Write-Host "INSTALLATION COMPLETE" -ForegroundColor Green
Write-Host "============ ========" -ForegroundColor Green
Write-Host "Installation Information following" -ForegroundColor Green
Write-Host "App url: $applicationUrl" -ForegroundColor Green
Write-Host "App id: $global:appId" -ForegroundColor Green
Write-Host "Site: $pmSiteUrl" -ForegroundColor Green
.\ProposalManager.zip