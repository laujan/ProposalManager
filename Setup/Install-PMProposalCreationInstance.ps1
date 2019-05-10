[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [pscredential]$Credential,
    [Parameter(Mandatory = $true)]
    [string]$OfficeTenantName,
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroupName,
    [Parameter(Mandatory = $false)]
    [string]$ApplicationName,
    [Parameter(Mandatory = $true)]
    [string]$ProposalManagerAppId,
    [Parameter(Mandatory = $true)]
    [string]$AzureResourceLocation,
    [Parameter(Mandatory = $true)]
    [string]$AzureSubscription,
    [Parameter(Mandatory = $true)]
    [string]$ProposalManagerDomain,
    [Parameter(Mandatory = $false)]
    [string]$ProjectSmartLinkUrl,
    [ValidateSet('Full', 'NoDeploy', 'DeployOnly', 'BuildOnly', 'RegisterDeploy')]
    [string]$Mode = 'Full',
    [Parameter(Mandatory = $false)]
    [switch]$Force,
    [Parameter(Mandatory = $false)]
    [switch]$MFA
)

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

if($Mode -in 'Full', 'NoDeploy', 'RegisterDeploy')
{
    $registers = $true
}
else
{
    $registers = $false
}

if($Mode -in 'Full', 'NoDeploy', 'BuildOnly')
{
    $builds = $true
}
else
{
    $builds = $false
}

if($Mode -in 'Full', 'DeployOnly', 'RegisterDeploy')
{
    $deploys = $true
}
else
{
    $deploys = $false
}

Write-Debug "Running in $Mode mode."

# Add references to utility scripts
. .\CheckDevPack.ps1
. .\CheckPowerShellModule.ps1
. .\ProposalManagementSetupUtilities.ps1
. .\RegisterAppV2.ps1
. .\UpdateAppSettings.ps1

# Check Pre-requisites
Write-Information "Checking pre-requisites"

if($builds)
{
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

    # Verify .NET Framework is installed
    if(!(CheckDevPack))
    {
        Write-Error "You need to install the .NET Framework 4.6.1 Developer Pack or later. Please do so by navigating to https://www.microsoft.com/en-us/download/details.aspx?id=49978"
    }
}

# Verify required modules are available
Verify-RequiredModules -AddInsOnly

if(!$ApplicationName)
{
    $ApplicationName = "propmgr-$OfficeTenantName"
}

# Must be lower case
$ApplicationName = $ApplicationName.ToLower()

if(!$ResourceGroupName)
{
    $ResourceGroupName = $ApplicationName
}

$applicationUrl = "https://$ProposalManagerDomain"

if(!$registers -and $deploys)
{
    $ExistingAppRegistration = Get-Content proposalCreationRegistration.json | ConvertFrom-Json
    $proposalCreationRegistration = @{ AppId = $ExistingAppRegistration.PCAppId; AppSecret = $ExistingAppRegistration.PCAppSecret }
}

if(!$MFA)
{
    if(!$Credential)
    {
        $Credential = Get-Credential -Message "Enter your Office 365 tenant global administrator credentials"
    }
}
else 
{
    Write-Information "MFA mode enabled. You will be required to enter credentials multiple times through the installation process."
}

Connect-AzureAD -Credential $Credential
$tenantId = (Get-AzureADTenantDetail).ObjectId
Disconnect-AzureAD

if($registers)
{
    Write-Information "Registering the Proposal Creation add-in."
    $replyUrls = @([string]::Empty, 'auth', 'auth/end')

    $applicationPermissions = @()
    [array]$delegatedPermissions = @(,'e1fe6dd8-ba31-4d61-89e7-88639da4683d')

    # Register Azure AD application (Endpoint v2)
    $proposalCreationRegistration = RegisterApp -ApplicationName "$ApplicationName-propcreation" -RelativeReplyUrls $replyUrls -Credential $Credential `
        -ApplicationPermissions $applicationPermissions -DelegatedPermissions $delegatedPermissions -AddTo $ProposalManagerAppId -Force:$Force

    $appSettings = @{
        SharePointHostName = "$OfficeTenantName.sharepoint.com";
    }

    Write-Information "Proposal Creation add-in successfully registered."

    @{
        ProposalCreationAppId = $proposalCreationRegistration.AppId;
        ProposalCreationSecret = $proposalCreationRegistration.AppSecret;
    } `
    | ConvertTo-Json | Set-Content proposalCreationRegistration.json -Force

    Write-Information "Initiating Proposal Creation add-in deployment..."
        
    $proposalCreationClientConfigFilePath = "..\Addins\ProposalCreation\UI\src\config\appconfig.ts"

    $proposalCreationManifestTemplateFilePath = "..\Addins\ProposalCreation\Manifest\proposal-creation-manifest.xml"
    $proposalCreationManifestFileName = "$ApplicationName-proposal-creation-manifest.xml"
    $proposalCreationManifestFilePath = ".\"
    $proposalCreationManifestFullName = "$proposalCreationManifestFilePath$proposalCreationManifestFileName"

    (Get-Content $proposalCreationClientConfigFilePath).
        Replace('<APPLICATION_ID>', $proposalCreationRegistration.AppId) `
        | Set-Content $proposalCreationClientConfigFilePath

    if(!$ProjectSmartLinkUrl)
    {
        $ProjectSmartLinkUrl = "$ApplicationName-projectsmartlink.azurewebsites.net"
    }

    New-Item -Path $proposalCreationManifestFilePath -Name $proposalCreationManifestFileName -ItemType File -Force
    (Get-Content $proposalCreationManifestTemplateFilePath).
        Replace('<NEW_GUID>', (New-Guid)).
        Replace('<PROPOSAL_CREATION_URL>', "$ApplicationName-propcreation.azurewebsites.net").
        Replace('<PROJECT_SMARTLINK_URL>', $ProjectSmartLinkUrl) `
        | Set-Content $proposalCreationManifestFullName

    $proposalCreationManifestTemplateFilePath = "..\Addins\ProposalCreation\Manifest\proposal-creation-powerpoint-manifest.xml"
    $proposalCreationManifestFileName = "$ApplicationName-proposal-creation-powerpoint-manifest.xml"
    $proposalCreationManifestFilePath = ".\"
    $proposalCreationManifestFullName = "$proposalCreationManifestFilePath$proposalCreationManifestFileName"

    New-Item -Path $proposalCreationManifestFilePath -Name $proposalCreationManifestFileName -ItemType File -Force
    (Get-Content $proposalCreationManifestTemplateFilePath).
        Replace('<NEW_GUID>', (New-Guid)).
        Replace('<PROPOSAL_CREATION_URL>', "$ApplicationName-propcreation.azurewebsites.net").
        Replace('<PROJECT_SMARTLINK_URL>', "$ApplicationName-projectsmartlink.azurewebsites.net") `
        | Set-Content $proposalCreationManifestFullName
}

if($builds)
{
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
}


if ($deploys)
{
    $proposalCreationSettings = @{
        SiteId = $appSettings.SharePointHostName;
        ProposalManagerUrl = $applicationUrl;
        ClientId = $proposalCreationRegistration.AppId;
        ClientSecret = $proposalCreationRegistration.AppSecret;
        TenantId = $tenantId;
        ProposalManagerApiId = $ProposalManagerAppId;
    }
    UpdateAppSettings -pathToJson ..\Addins\ProposalCreation\Web\ProposalCreationWeb\bin\Release\netcoreapp2.1\publish\appsettings.json -inputParams $proposalCreationSettings -ProposalCreation

    $deploymentCredentials = New-PMSite -PMSiteLocation $AzureResourceLocation -ApplicationName $ApplicationName -ResourceGroupName $ResourceGroupName `
    -IncludeProposalCreation:$true -IncludeProjectSmartLink:$false -ExcludeProposalManager -Subscription $AzureSubscription -Force:$Force 

    .\ZipDeploy.ps1 -sourcePath ..\Addins\ProposalCreation\Web\ProposalCreationWeb\bin\Release\netcoreapp2.1\publish\* -username $deploymentCredentials.PCUsername -password $deploymentCredentials.PCPassword -appName "$ApplicationName-propcreation"

    Write-Information "Proposal Creation: Web app deployment has completed!"
}