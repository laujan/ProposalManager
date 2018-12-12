<#
.SYNOPSIS
    Deploys Proposal Manager
.DESCRIPTION
    Installs all the required Proposal Manager assets and deploys Proposal Manager website.
.PARAMETER PMSharePointSiteAlias
    The name of the SharePoint site to create for Proposal Manager (`proposalmanager` is ok most of the times).
.PARAMETER PMAdminUpn
    The upn (user principal name, for example: john.doe@domain.com) of the user that will be made administrator of this instance of Proposal Manager. It can be yourself or someone else.
.PARAMETER OfficeTenantName
    The name of the office tenant. For example, if your mail domain is @contoso.onmicrosoft.com, then the name of the tenant is "contoso".
.PARAMETER AzureResourceLocation
    The azure region in which you want the resources to be allocated (for example, "East US").
.PARAMETER ApplicationName
    The name of the application (for example "proposalmanager")
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PMAdminUpn,
    [Parameter(Mandatory = $true)]
    [string]$PMSiteAlias,
    [Parameter(Mandatory = $true)]
    [string]$OfficeTenantName,
    [Parameter(Mandatory = $true)]
    [string]$AzureResourceLocation,
    [Parameter(Mandatory = $true)]
    [string]$AzureSubscription,
    [Parameter(Mandatory = $true)]
    [string]$ApplicationName,
    [Parameter(Mandatory = $false)]
    [string]$PMSharePointSiteAlias
)

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

# Check Pre-requisites
Write-Information "Checking pre-requisites"

# Verify npm is installed
try
{
    Write-Information "The NPM version is: $(npm -v)"
}
catch
{
    Write-Error "You need to install npm. Please do so by navigating to http://nodejs.org"
}

# Verify dotnet core is installed
try
{
    Write-Information "The .NET Core version is: $(dotnet --version)"
}
catch
{
    Write-Error "You need to install the dotnet core sdk 2.1. Please do so by navigating to https://dotnet.microsoft.com/download/thank-you/dotnet-sdk-2.1.500-windows-x64-installer"
}

# Add references to utility scripts
. .\CheckDevPack.ps1
. .\CheckPowerShellModule.ps1
. .\ProposalManagementSetupUtilities.ps1
. .\RegisterAppV2.ps1
. .\UpdateAppSettings.ps1
. .\UpdateAppSettingsJS.ps1

$isInstalled = CheckDevPack
if($isInstalled -eq $false)
{
    Write-Error "You need to install the .NET Framework 4.6.1 Developer Pack or later. Please do so by navigating to https://www.microsoft.com/en-us/download/details.aspx?id=49978"
}

# Verify required modules are available
Write-Information "Checking required PS modules"
$modules = @("AzureRm", "Microsoft.Online.SharePoint.Powershell", "SharePointPnPPowerShellOnline", "Azure", "AzureAD")

foreach($module in $modules)
{
    Verify-Module -ModuleName $module
}

# Prompt the user one time for credentials to use across all necessary connections (like SSO)
Write-Information "Installation of Proposal Manager will begin. You need to be a Global Administrator to continue."
$global:credential = Get-Credential -Message "Enter your Office 365 tenant global administrator credentials"

if(!$PMSharePointSiteAlias)
{
    $PMSharePointSiteAlias = "https://$OfficeTenantName-admin.sharepoint.com"
}

# Create SharePoint Site
$pmSiteUrl = New-PMSharePointSite -AdminSiteUrl $PMSharePointSiteAlias -PMAdminUpn $PMAdminUpn -PMSiteAlias $PMSiteAlias

New-PMGroupStructure

if(!$ApplicationName)
{
    $ApplicationName = "propmgr-$OfficeTenantName"
}

# Register Azure AD application (Endpoint v2)
RegisterApp -applicationName $ApplicationName

# Create Service Principal
Write-Information "Creating Service Principal"
Connect-AzureRmAccount -Credential $global:credential
New-AzureRmADServicePrincipal -ApplicationId $global:appId
Disconnect-AzureRmAccount

Connect-AzureAD -Credential $global:credential
$tenantId = (Get-AzureADTenantDetail).ObjectId
Disconnect-AzureAD

$deploymentCredentials = New-PMSite -PMSiteLocation $AzureResourceLocation -ApplicationName $ApplicationName -Subscription $AzureSubscription

$appSettings = @{
    ClientId = $global:appId; 
    ClientSecret = $global:secretText; 
    TenantId = $tenantId; 
    TenantName = $OfficeTenantName; 
    BaseUrl = "https://$ApplicationName.azurewebsites.net"; 
    SharePointSiteRelativeName = $PMSiteAlias;
    SharePointHostName = "$OfficeTenantName.sharepoint.com"; 
    WebhookAddress = "https://$ApplicationName.scm.azurewebsites.net/api/triggeredwebjobs/OpportunitySiteProvisioner/run";
    WebhookUsername = $deploymentCredentials.Username;
    WebhookPassword = $deploymentCredentials.Password
}

# Update Proposal Manager application settings
UpdateAppSettings -pathToJson ..\WebReact\appsettings.json -inputParams $appSettings
UpdateAppSettingsClient -pathToJson ..\WebReact\ClientApp\src\helpers\AppSettings.js -appId $global:appId -appUri "https://$ApplicationName.azurewebsites.net" -tenantId $tenantId

cd ..\WebReact\ClientApp

# Install all required dependencies
$ErrorActionPreference = 'Inquire'
npm install
$ErrorActionPreference = 'Stop'

cd ..\..\Setup

# Publish Proposal Manager
$solutionDir = (Get-Item -Path "..\").FullName

Write-Information "Restoring Nuget solution packages..."
.\nuget.exe restore "..\ProposalManagement.sln" -SolutionDirectory ..\
Write-Information "Nuget solution packages successfully retrieved"

cd "..\Dynamics Integration\OneDriveSubscriptionRenewal"
dotnet restore
dotnet msbuild "OneDriveSubscriptionRenewal.csproj" "/p:SolutionDir=`"$($solutionDir)\\`";Configuration=Release;DebugSymbols=false;DebugType=None"
cd ..\..\Setup
cd "..\Utilities\OpportunitySiteProvisioner"
dotnet msbuild "OpportunitySiteProvisioner.csproj" "/p:SolutionDir=`"$($solutionDir)\\`";Configuration=Release;DebugSymbols=false;DebugType=None"
cd ..\..\Setup
rd ..\WebReact\bin\Release\netcoreapp2.1\publish -Recurse -ErrorAction Ignore
dotnet publish ..\WebReact -c Release

.\ZipDeploy.ps1 -sourcePath ..\WebReact\bin\Release\netcoreapp2.1\publish\* -username $deploymentCredentials.Username -password $deploymentCredentials.Password -appName $ApplicationName
Write-Information "Web app deployment has completed"

$applicationDomain = "$ApplicationName.azurewebsites.net"
$applicationUrl = "https://$applicationDomain"

New-PMTeamsAddInManifest -AppUrl $applicationUrl -AppDomain $applicationDomain

# Grant Admin Consent
$adminConsentUrl = "https://login.microsoftonline.com/common/adminconsent?client_id=$($global:appId)&state=12345&redirect_uri=$applicationUrl"

Start-Process $adminConsentUrl

Write-Information "INSTALLATION COMPLETE"
Write-Information "============ ========"
Write-Information "Installation Information following"
Write-Information "App url: $applicationUrl"
Write-Information "App id: $global:appId"
Write-Information "Site: $pmSiteUrl"
Write-Information "Consent page: $adminConsentUrl"
.\ProposalManager.zip