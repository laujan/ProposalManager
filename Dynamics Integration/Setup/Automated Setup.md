# Installing Proposal Manager - Dynamics 365 for Sales Integration
The integration can be easily deployed using PowerShell. In this folder, a script called `Setup.ps1` is included for your convenience. This script is intended to run in that folder, with all of its files and folders.

The script will deploy a Dynamics 365 package using the [Dynamics 365 Package Deployer](https://docs.microsoft.com/en-us/dynamics365/customer-engagement/admin/deploy-packages-using-package-deployer-windows-powershell). After installing the managed solution, the package will perform most of the steps documented in the Setup Guide of the Integration (located in Dynamics Integration/Documents/Setup Guide of this repo). Some of the steps will need to be done manually for now; these are:

 1. Partial Webhook registration
 2. Proposal Manager configuration

The Procedure section of this guide will guide you in performing all the steps in the necessary order. Keep the mentioned Setup Guide handy as you will need to follow two of its steps.

## Parameters

To run the script, you need to provide the following parameters:

Parameter|Meaning|Example
---------|-------|-------
OrganizationName|The name of the Dynamics 365 organization. The unique name can be found in Settings > Customizations > Developer Resources, Instance Reference Information section, in a field called "Unique Name".|contoso
OrganizationRegion|The region of the Dynamics 365 instance. This information can be found either in the [Power Apps admin site](admin.powerplatform.microsoft.com) in the Enviroments section, or in the [Dynamics 365 admin site](https://port.crm.dynamics.com/G/Instances/InstancePicker.aspx?). |NorthAmerica
TenantDomain|The domain of the tenant where the Dynamics 365 instance is located.|contoso.onmicrosoft.com
BusinessUnitName|The name of the Dynamics 365 business unit.|contoso
ProposalManagerAppId|The Azure AD App Registration Id for the Proposal Manager instance. This can be found either in the Azure portal, in the Azure AD section, or in the appsettings.json file of the deployed solution.|00000000-0000-0000-0000-000000000000
ProposalManagerApplicationURL|The full URL of the Proposal Manager instance.|https://proposalmanager.azurewebsites.net
SharePointDomain|The full domain of the tenant's SharePoint site. Also included in the appsettings.json of the Proposal Manager solution.|contoso.sharepoint.com
ProposalManagerSharePointSiteName|The name of the Proposal Manager site in the afromentioned SharePoint site. Also included in the appsettings.json of the Proposal Manager solution.|proposalmanager
DriveName|The name of the Proposal Manager SharePoint site root drive. Defaults to `Shared Documents`, but can vary depending on the tenant and the site.|"Shared Documents"
Credential|*Optional*. A Credential object obtained with Get-Credential.|-

### Execution Example
```powershell
.\Setup.ps1 -SharePointDomain "contoso.sharepoint.com" -TenantDomain "contoso.onmicrosoft.com" -BusinessUnitName "contoso" -ProposalManagerAppId "00000000-0000-0000-0000-000000000000" -ProposalManagerApplicationUrl "https://proposalmanager.azurewebsites.net" -ProposalManagerSharePointSiteName "proposalmanager" -OrganizationName "contoso" -OrganizationRegion "NorthAmerica"
```

## Procedure
### 1. Script Execution
The first thing to do is running the script with PowerShell. All the parameters, and how to acquire them, are explained in the previous section.

When running the script, you will be prompted for credentials. Log in with your **Dynamics 365 global administrator credentials**. This is usually the Office 365 tenant global administrator, but it can vary in your organization. Make sure this user has both the System Administrator and System Customizer roles in Dynamics 365. 
Once this is done, the script will start importing some necessary Dynamics 365 PowerShell modules, open a connection to the Dynamics 365 instance, and deploy the Proposal Manager package using the Deployer.

The script responsibility ends there, as all remaining configuration is done via custom code in the package after the solution import finishes. This code will create a Proposal Manager Application user with all required permissions in the Dynamics 365 instance, register all necessary SharePoint sites, partially register the Webhooks, and generate an appsettings.json file with all the generated configuration that needs to be updated in the Proposal Manager solution.

### 2. Webhook steps registration
When the Deployer ends its execution successfully, you will need to manually register the Dynamics 365 Webhooks. Please reefer to the step 6 of the Setup Guide of the Integration. 

**Important**: Keep in mind that sub-steps number 3, 4, 5 and 6 will be already done; you should only follow sub-steps 1, 2, 7 and 8 to register the steps of each Webhook.

### 3. Proposal Manager configuration
After executing the script, it will generate an *appsettings-\<random-GUID>.json* file. This will include all the values you need to replace in the identically named file in the Proposal Manager deployed solution. All you need to do is copy and replace the sections in this file with the ones generated by the script.

**Important:** Do *not* replace the whole file, as the file generated by this script only includes a subset of the configurations needed by Proposal Manager. You need to manually open both files in your preferred text editor, and replace ONLY the sections generated by the script. 

Finally, you need to give the Integration permission to access the Proposal Manager API. This can be done following the step 7 of the Setup Guide.