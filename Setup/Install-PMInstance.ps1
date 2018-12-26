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
.PARAMETER IncludeBot
    Specify only if you want the bot to be deployed alongside the application.
.PARAMETER IncludeAddins
    Specify only if you want the addins (Proposal Creation & Project Smart Link) to be deployed alongside the application.
.PARAMETER SqlServerAdminUsername
    If IncluddeAddins was specified, this is the sql server admin username for the project smart link sql server. This sql server is created by this script; it does not exist beforehand. Therefore, you don't need to look up the value for this parameter but rather invent it now and take note of what you input. If IncludeAddins was not specified, this parameter is ignored.
.PARAMETER SqlServerAdminPassword
    If IncluddeAddins was specified, this is the sql server admin password for the project smart link sql server. This sql server is created by this script; it does not exist beforehand. Therefore, you don't need to look up the value for this parameter but rather invent it now and take note of what you input. If IncludeAddins was not specified, this parameter is ignored.
.PARAMETER BotAzureSubscription
    The name or id of the Azure subscription to register the bot in. It has to belong to the tenant identified by the OfficeTenantName parameter.
.PARAMETER AdminSharePointSiteUrl
    OPTIONAL. The url of the admin sharepoint site. If none is provided, the default one will be used.
.PARAMETER Force
    Specify only if you explicitly intend to overwrite an existing installation of Proposal Manager.
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
    [switch]$IncludeAddins,
    [Parameter(Mandatory = $false)]
    [string]$SqlServerAdminUsername,
    [Parameter(Mandatory = $false)]
    [string]$SqlServerAdminPassword,
    [Parameter(Mandatory = $false)]
    [string]$BotAzureSubscription,
    [Parameter(Mandatory = $false)]
    [string]$AdminSharePointSiteUrl,
    [Parameter(Mandatory = $false)]
    [switch]$Force
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

$applicationDomain = "$ApplicationName.azurewebsites.net"
$applicationUrl = "https://$applicationDomain"

if(!$AdminSharePointSiteUrl)
{
    $AdminSharePointSiteUrl = "https://$OfficeTenantName-admin.sharepoint.com"
}

# Create SharePoint Site
$pmSiteUrl = New-PMSharePointSite -AdminSiteUrl $AdminSharePointSiteUrl -Credential $credential -PMAdminUpn $PMAdminUpn -PMSiteAlias $PMSiteAlias -OfficeTenantName $OfficeTenantName -Force:$Force

New-PMGroupStructure -Credential $Credential -Force:$Force

if(!$ApplicationName)
{
    $ApplicationName = "propmgr-$OfficeTenantName"
}

$preAuthorizedAppIds = @()

if($IncludeAddins)
{
    Write-Information "Registering the Proposal Creation add-in."
    $replyUrls = @([string]::Empty, 'auth', 'auth/end')
    [array]$delegatedPermissions = @(,'e1fe6dd8-ba31-4d61-89e7-88639da4683d')
    $applicationPermissions = @()
    $proposalCreationRegistration = RegisterApp -ApplicationName "$ApplicationName-propcreation" -RelativeReplyUrls $replyUrls -DelegatedPermissions $delegatedPermissions -Credential $credential
    $preAuthorizedAppIds += $proposalCreationRegistration.AppId
    Write-Information "Proposal Creation add-in successfully registered."

    Write-Information "Registering the Project Smart Link add-in."
    $replyUrls = @([string]::Empty, 'auth', 'auth/end')
    [array]$delegatedPermissions =
        '7427e0e9-2fba-42fe-b0c0-848c9e6a8182',
        '37f7f235-527c-4136-accd-4a02d197296e',
        '14dad69e-099b-42c9-810b-d002981feec1',
        '89fe6a52-be36-487e-b7d8-d061c450a026',
        'e1fe6dd8-ba31-4d61-89e7-88639da4683d'
    $applicationPermissions = @()
    $projectSmartLinkRegistration = RegisterApp -ApplicationName "$ApplicationName-projectsmartlink" -RelativeReplyUrls $replyUrls -DelegatedPermissions $delegatedPermissions -Credential $credential
    #$preAuthorizedAppIds += $projectSmartLinkRegistration.AppId
    Write-Information "Proposal Creation add-in successfully registered."
}

# Register Azure AD application (Endpoint v2)
$appRegistration = RegisterApp -ApplicationName $ApplicationName -AdditionalPreAuthorizedAppIds $preAuthorizedAppIds -Credential $credential

# Create Service Principal
Write-Information "Creating Service Principal"
Connect-AzureRmAccount -Credential $credential
[int]$retriesLeft = 3
[bool]$success = $false
while(!$success)
{
    try
    {
        New-AzureRmADServicePrincipal -ApplicationId $appRegistration.AppId
        $success = $true
    }
    catch
    {
        if($retriesLeft)
        {
            $retriesLeft -= 1
            Write-Warning "Service Principal creation failed. Retrying..."
            Start-Sleep -Seconds 10
        }
        else
        {
            Write-Error "Service Principal creation failed after 3 retries."
        }
    }
}
Disconnect-AzureRmAccount

Connect-AzureAD -Credential $credential
$tenantId = (Get-AzureADTenantDetail).ObjectId
Disconnect-AzureAD

$deploymentCredentials = New-PMSite -PMSiteLocation $AzureResourceLocation -ApplicationName $ApplicationName -Subscription $AzureSubscription -Force:$Force `
    -IncludeProposalCreation:$IncludeAddins -IncludeProjectSmartLink:$IncludeAddins `
    -SqlServerAdminUsername $SqlServerAdminUsername -SqlServerAdminPassword $SqlServerAdminPassword

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

if($IncludeAddins)
{
    Write-Information "Initiating Proposal Creation add-in deployment..."

    $proposalCreationSettings = @{
        SiteId = $appSettings.SharePointHostName;
        ProposalManagerUrl = $applicationUrl;
        ClientId = $proposalCreationRegistration.AppId;
        ClientSecret = $proposalCreationRegistration.AppSecret;
        TenantId = $tenantId;
        ProposalManagerApiId = $appRegistration.AppId;
    }
    UpdateAppSettings -pathToJson ..\Addins\ProposalCreation\Web\ProposalCreationWeb\appsettings.json -inputParams $proposalCreationSettings -ProposalCreation
    
    $proposalCreationClientConfigFilePath = "..\Addins\ProposalCreation\UI\src\config\appconfig.ts"

    $proposalCreationManifestTemplateFilePath = "..\Addins\ProposalCreation\Manifest\proposal-creation-manifest.xml"
    $proposalCreationManifestFileName = "$ApplicationName-proposal-creation-manifest.xml"
    $proposalCreationManifestFilePath = "..\Addins\ProposalCreation\Manifest\"
    $proposalCreationManifestFullName = "$proposalCreationManifestFilePath$proposalCreationManifestFileName"

    (Get-Content $proposalCreationClientConfigFilePath).
        Replace('<APPLICATION_ID>', $proposalCreationRegistration.AppId) `
        | Set-Content $proposalCreationClientConfigFilePath

    New-Item -Path $proposalCreationManifestFilePath -Name $proposalCreationManifestFileName -ItemType File
    (Get-Content $proposalCreationManifestTemplateFilePath).
        Replace('<NEW_GUID>', (New-Guid)).
        Replace('<PROPOSAL_CREATION_URL>', "$ApplicationName-propcreation.azurewebsites.net").
        Replace('<PROJECT_SMARTLINK_URL>', "$ApplicationName-projectsmartlink.azurewebsites.net") `
        | Set-Content $proposalCreationManifestFullName

    cd ..\Addins\ProposalCreation\UI

    $ErrorActionPreference = 'Inquire'
    npm install
    npm run build
    $ErrorActionPreference = 'Stop'

    cd ..\..\..\Setup

    $solutionDir = (Get-Item -Path "..\Addins\ProposalCreation\Web").FullName

    Write-Information "Proposal Creation: Restoring Nuget solution packages..."
    .\nuget.exe restore "..\Addins\ProposalCreation\Web\ProposalCreationWeb.sln" -SolutionDirectory $solutionDir
    Write-Information "Proposal Creation: Nuget solution packages successfully retrieved"

    cd "..\Addins\ProposalCreation\Web\ProposalCreation.Core"
    dotnet msbuild "ProposalCreation.Core.csproj" "/p:SolutionDir=`"$($solutionDir)\\`";Configuration=Release;DebugSymbols=false;DebugType=None"
    cd ..\..\..\..\Setup
    rd ..\Addins\ProposalCreation\Web\ProposalCreationWeb\bin\Release\netcoreapp2.1\publish -Recurse -ErrorAction Ignore
    dotnet publish ..\Addins\ProposalCreation\Web\ProposalCreationWeb -c Release

    .\ZipDeploy.ps1 -sourcePath ..\Addins\ProposalCreation\Web\ProposalCreationWeb\bin\Release\netcoreapp2.1\publish\* -username $deploymentCredentials.PCUsername -password $deploymentCredentials.PCPassword -appName "$ApplicationName-propcreation"
    Write-Information "Proposal Creation: Web app deployment has completed!"

    #===============================================================================================

    Write-Information "Initiating Project Smart Link add-in deployment..."

    $projectSmartLinkSettings = @{
        ClientId = $projectSmartLinkRegistration.AppId;
        ClientSecret = $projectSmartLinkRegistration.AppSecret;
        TenantId = $tenantId;
        AllowedTenants = @(,$tenantId);
        SharePointUrl = "https://$OfficeTenantName.sharepoint.com/.default";
        ConnectionString = "Data Source=tcp:$ApplicationName.database.windows.net,1433;Initial Catalog=ProjectSmartLink;User Id=$SqlServerAdminUsername;Password=$SqlServerAdminPassword;"
    }
    UpdateAppSettings -pathToJson ..\Addins\ProjectSmartLink\ProjectSmartLink.Web\appsettings.json -inputParams $projectSmartLinkSettings -ProjectSmartLink
    
    $projectSmartLinkManifestTemplateFilePath = "..\Addins\ProjectSmartLink\ProjectSmartLinkExcel\ProjectSmartLinkExcelManifest\ProjectSmartLinkExcel.xml"
    $projectSmartLinkManifestFileName = "$ApplicationName-project-smart-link-excel-manifest.xml"
    $projectSmartLinkManifestFilePath = "..\Addins\ProjectSmartLink\"
    $projectSmartLinkManifestFullName = "$projectSmartLinkManifestFilePath$projectSmartLinkManifestFileName"
    
    New-Item -Path $projectSmartLinkManifestFilePath -Name $projectSmartLinkManifestFileName -ItemType File
    (Get-Content $projectSmartLinkManifestTemplateFilePath).
        Replace('{NEW_GUID}', (New-Guid)).
        Replace('{PROJECT_SMART_LINK_WEB_URL}', "$ApplicationName-projectsmartlink.azurewebsites.net") `
        | Set-Content $projectSmartLinkManifestFullName

    $projectSmartLinkManifestTemplateFilePath = "..\Addins\ProjectSmartLink\ProjectSmartLinkPowerPoint\ProjectSmartLinkPowerPointManifest\ProjectSmartLinkPowerPoint.xml"
    $projectSmartLinkManifestFileName = "$ApplicationName-project-smart-link-powerpoint-manifest.xml"
    $projectSmartLinkManifestFilePath = "..\Addins\ProjectSmartLink\"
    $projectSmartLinkManifestFullName = "$projectSmartLinkManifestFilePath$projectSmartLinkManifestFileName"
    
    New-Item -Path $projectSmartLinkManifestFilePath -Name $projectSmartLinkManifestFileName -ItemType File
    (Get-Content $projectSmartLinkManifestTemplateFilePath).
        Replace('{NEW_GUID}', (New-Guid)).
        Replace('{PROJECT_SMART_LINK_WEB_URL}', "$ApplicationName-projectsmartlink.azurewebsites.net") `
        | Set-Content $projectSmartLinkManifestFullName

    $solutionDir = (Get-Item -Path "..\Addins\ProjectSmartLink").FullName

    Write-Information "Project Smart Link: Restoring Nuget solution packages..."
    .\nuget.exe restore "..\Addins\ProjectSmartLink\ProjectSmartLink.sln" -SolutionDirectory $solutionDir
    Write-Information "Project Smart Link: Nuget solution packages successfully retrieved"

    cd "..\Addins\ProjectSmartLink\ProjectSmartLink.Common"
    dotnet msbuild "ProjectSmartLink.Common.csproj" "/p:SolutionDir=`"$($solutionDir)\\`";Configuration=Release;DebugSymbols=false;DebugType=None"
    cd "..\ProjectSmartLink.Entity"
    dotnet msbuild "ProjectSmartLink.Entity.csproj" "/p:SolutionDir=`"$($solutionDir)\\`";Configuration=Release;DebugSymbols=false;DebugType=None"
    cd "..\ProjectSmartLink.Service"
    dotnet msbuild "ProjectSmartLink.Service.csproj" "/p:SolutionDir=`"$($solutionDir)\\`";Configuration=Release;DebugSymbols=false;DebugType=None"
    cd ..\..\..\Setup
    rd ..\Addins\ProjectSmartLink\ProjectSmartLink.Web\bin\Release\netcoreapp2.1\publish -Recurse -ErrorAction Ignore
    dotnet publish ..\Addins\ProjectSmartLink\ProjectSmartLink.Web -c Release

    .\ZipDeploy.ps1 -sourcePath ..\Addins\ProjectSmartLink\ProjectSmartLink.Web\bin\Release\netcoreapp2.1\publish\* -username $deploymentCredentials.PSLUsername -password $deploymentCredentials.PSLPassword -appName "$ApplicationName-projectsmartlink"
    Write-Information "Proposal Creation: Web app deployment has completed!"

}

Write-Information "Initiating main app deployment..."

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
Write-Information "Web app deployment has completed!"

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