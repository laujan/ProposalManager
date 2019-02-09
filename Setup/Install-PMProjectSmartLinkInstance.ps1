[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [pscredential]$Credential,
    [Parameter(Mandatory = $true)]
    [string]$OfficeTenantName,
    [Parameter(Mandatory = $false)]
    [string]$ApplicationName,
    [Parameter(Mandatory = $true)]
    [string]$ProposalManagerAppId,
    [Parameter(Mandatory = $true)]
    [string]$AzureResourceLocation,
    [Parameter(Mandatory = $true)]
    [string]$AzureSubscription,
    [Parameter(Mandatory = $false)]
    [string]$SqlServerAdminUsername,
    [Parameter(Mandatory = $false)]
    [string]$SqlServerAdminPassword,
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

# Add references to utility scripts
. .\CheckDevPack.ps1
. .\CheckPowerShellModule.ps1

$isInstalled = CheckDevPack
if($isInstalled -eq $false)
{
    Write-Error "You need to install the .NET Framework 4.6.1 Developer Pack or later. Please do so by navigating to https://www.microsoft.com/en-us/download/details.aspx?id=49978"
}

# Verify required modules are available
Write-Information "Checking required PS modules"
$modules = @("AzureRm", "Azure", "AzureAD")

foreach($module in $modules)
{
    Verify-Module -ModuleName $module
}

if(!$Credential)
{
    $Credential = Get-Credential
}

if(!$ApplicationName)
{
    $ApplicationName = "propmgr-$OfficeTenantName"
}

$applicationDomain = "$ApplicationName.azurewebsites.net"
$applicationUrl = "https://$applicationDomain"

Connect-AzureAD -Credential $Credential
$tenantId = (Get-AzureADTenantDetail).ObjectId
Disconnect-AzureAD

. .\ProposalManagementSetupUtilities.ps1

. .\RegisterAppV2.ps1

. .\UpdateAppSettings.ps1

Write-Information "Registering the Project Smart Link add-in."
$replyUrls = @([string]::Empty, 'auth', 'auth/end')
[array]$delegatedPermissions =
    '7427e0e9-2fba-42fe-b0c0-848c9e6a8182',
    '37f7f235-527c-4136-accd-4a02d197296e',
    '14dad69e-099b-42c9-810b-d002981feec1',
    '89fe6a52-be36-487e-b7d8-d061c450a026',
    'e1fe6dd8-ba31-4d61-89e7-88639da4683d'
$applicationPermissions = @()
$projectSmartLinkRegistration = RegisterApp -ApplicationName "$ApplicationName-projectsmartlink" -RelativeReplyUrls $replyUrls -DelegatedPermissions $delegatedPermissions -Credential $credential -AddTo $ProposalManagerAppId
#$preAuthorizedAppIds += $projectSmartLinkRegistration.AppId
Write-Information "Proposal Creation add-in successfully registered."

$appSettings = @{
    SharePointHostName = "$OfficeTenantName.sharepoint.com";
}

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

$deploymentCredentials = New-PMSite -PMSiteLocation $AzureResourceLocation -ApplicationName $ApplicationName -Subscription $AzureSubscription -Force:$Force `
    -IncludeProposalCreation:$false -IncludeProjectSmartLink:$true -ExcludeProposalManager `
    -SqlServerAdminUsername $SqlServerAdminUsername -SqlServerAdminPassword $SqlServerAdminPassword

.\ZipDeploy.ps1 -sourcePath ..\Addins\ProjectSmartLink\ProjectSmartLink.Web\bin\Release\netcoreapp2.1\publish\* -username $deploymentCredentials.PSLUsername -password $deploymentCredentials.PSLPassword -appName "$ApplicationName-projectsmartlink"
Write-Information "Project Smart Link: Web app deployment has completed!"
