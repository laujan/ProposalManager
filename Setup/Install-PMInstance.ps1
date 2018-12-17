<#
.SYNOPSIS
    Deploys Proposal Manager
.DESCRIPTION
    Installs all the required Proposal Manager assets and deploys an instance of the Proposal Manager website.
.PARAMETER PMAdminUpn
    The upn (user principal name, for example: john.doe@domain.com) of the user that will be made administrator of this instance of Proposal Manager. It can be yourself or someone else.
.PARAMETER PMSiteAlias
    The name of the SharePoint site to create for Proposal Manager (`proposalmanager` is ok most of the times).
.PARAMETER OfficeTenantName
    The name of the office tenant. For example, if your mail domain is @contoso.onmicrosoft.com, then the name of the tenant is "contoso".
.PARAMETER AzureResourceLocation
    The azure region in which you want the resources to be allocated (for example, "East US").
.PARAMETER AzureSubscription
    The name or id of the Azure subscription to deploy the Proposal Manager web app to. It can belong to any tenant (you will be asked to log in to azure in that tenant).
.PARAMETER ApplicationName
    The name of the application (for example "proposalmanager")
.PARAMETER BotAzureSubscription
    The name or id of the Azure subscription to register the bot in. It has to belong to the tenant identified by the OfficeTenantName parameter.
.PARAMETER AdminSharePointSiteUrl
    OPTIONAL. The url of the admin sharepoint site. If none is provided, the default one will be used.
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
    [switch]$IncludeBot,
    [Parameter(Mandatory = $false)]
    [string]$BotAzureSubscription,
    [Parameter(Mandatory = $false)]
    [string]$AdminSharePointSiteUrl
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

# Verify az is installed
try
{
    Write-Information "The Azure CLI version is: $(az -v)"
}
catch
{
    Write-Error "You need to install the Microsoft Azure CLI. Please do so by navigating to https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest"
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
$credential = Get-Credential -Message "Enter your Office 365 tenant global administrator credentials"

if(!$AdminSharePointSiteUrl)
{
    $AdminSharePointSiteUrl = "https://$OfficeTenantName-admin.sharepoint.com"
}

# Create SharePoint Site
$pmSiteUrl = New-PMSharePointSite -AdminSiteUrl $AdminSharePointSiteUrl -Credential $credential -PMAdminUpn $PMAdminUpn -PMSiteAlias $PMSiteAlias

New-PMGroupStructure -Credential $Credential

if(!$ApplicationName)
{
    $ApplicationName = "propmgr-$OfficeTenantName"
}

# Register Azure AD application (Endpoint v2)
$appRegistration = RegisterApp -ApplicationName $ApplicationName -Credential $credential

# Create Service Principal
Write-Information "Creating Service Principal"
Connect-AzureRmAccount -Credential $credential
New-AzureRmADServicePrincipal -ApplicationId $appRegistration.AppId
Disconnect-AzureRmAccount

Connect-AzureAD -Credential $credential
$tenantId = (Get-AzureADTenantDetail).ObjectId
Disconnect-AzureAD

$deploymentCredentials = New-PMSite -PMSiteLocation $AzureResourceLocation -ApplicationName $ApplicationName -Subscription $AzureSubscription

$appSettings = @{
    ClientId = $appRegistration.AppId; 
    ClientSecret = $appRegistration.AppSecret; 
    TenantId = $tenantId; 
    TenantName = $OfficeTenantName; 
    BaseUrl = "https://$ApplicationName.azurewebsites.net"; 
    SharePointSiteRelativeName = $PMSiteAlias;
    SharePointHostName = "$OfficeTenantName.sharepoint.com"; 
    WebhookAddress = "https://$ApplicationName.scm.azurewebsites.net/api/triggeredwebjobs/OpportunitySiteProvisioner/run";
    WebhookUsername = $deploymentCredentials.Username;
    WebhookPassword = $deploymentCredentials.Password
}

if($IncludeBot)
{
    Write-Information "Registering bot app..."
    $botRegistration = RegisterApp -ApplicationName "$ApplicationName-bot" -Credential $credential
    $appSettings.BotId = $botRegistration.AppId
    $appSettings.MicrosoftAppId = $botRegistration.AppId
    $appSettings.MicrosoftAppPassword = $botRegistration.AppSecret
    $appSettings.AllowedTenants = $tenantId
    Write-Information "Bot app registered. Creating bot..."
    if($BotAzureSubscription)
    {
        $bot = New-PMBot -Subscription $BotAzureSubscription -ApplicationName $ApplicationName -Credential $credential -AppId $botRegistration.AppId -AppSecret $botRegistration.AppSecret
        Write-Information "Bot created successfully."
        $appSettings.BotName = $bot.name
        
    }
    else
    {
        Write-Information "You have not provided an azure subscription name or ID for the bot. Please register the bot by following the Getting Started guide and enter the bot name here manually."
        $appSettings.BotName = Read-Host "Bot name"
    }
}

# Update Proposal Manager application settings
UpdateAppSettings -pathToJson ..\WebReact\appsettings.json -inputParams $appSettings
UpdateAppSettingsClient -pathToJson ..\WebReact\ClientApp\src\helpers\AppSettings.js -appId $appRegistration.AppId -appUri "https://$ApplicationName.azurewebsites.net" -tenantId $tenantId

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

New-PMTeamsAddInManifest -AppUrl $applicationUrl -AppDomain $applicationDomain -BotId $botRegistration.AppId

# Grant Admin Consent
$adminConsentUrl = "https://login.microsoftonline.com/common/adminconsent?client_id=$($appRegistration.AppId)&state=12345&redirect_uri=$applicationUrl"

Start-Process $adminConsentUrl

Write-Information "INSTALLATION COMPLETE"
Write-Information "============ ========"
Write-Information "Installation Information following"
Write-Information "App url: $applicationUrl"
Write-Information "App id: $($appRegistration.AppId)"
Write-Information "Site: $pmSiteUrl"
Write-Information "Consent page: $adminConsentUrl"
.\ProposalManager.zip