Param(
    $OrganizationName,
    $OrganizationRegion,
    $TenantDomain,
    $BusinessUnitName,
    $ProposalManagerAppId,
    $ProposalManagerApplicationUrl,
    $SharePointDomain,
    $ProposalManagerSharePointSiteName,
    $DriveName = "Shared Documents",
    $Credential
)

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

if(!$Credential)
{
    $Credential = Get-Credential
}

.\RegisterXrmTooling.ps1
.\RegisterXRMPackageDeployment.ps1

$crmConnection = Get-CrmConnection -OrganizationName $OrganizationName -DeploymentRegion $OrganizationRegion -OnLineType Office365 -Credential $Credential

Import-CrmPackage -PackageDirectory (Resolve-Path .) -PackageName ProposalManager.dll -CrmConnection $crmConnection -LogWriteDirectory (Resolve-Path .) `
    -RuntimePackageSettings "TenantDomain=$TenantDomain|BusinessUnit=$BusinessUnitName|ProposalManagerApplicationId=$ProposalManagerAppId|ProposalManagerApplicationUrl=$ProposalManagerApplicationUrl|SharePointDomain=$SharePointDomain|ProposalManagerSharePointSiteName=$ProposalManagerSharePointSiteName|DriveName=$DriveName"