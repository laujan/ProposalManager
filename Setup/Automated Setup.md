# Pre-requisites

If running in _Full_, _BuildOnly_ or _NoDeploy_ mode:

* .NET Core 2.1 (https://dotnet.microsoft.com/download/thank-you/dotnet-sdk-2.1.500-windows-x64-installer)
* .NET Framework 4.6.1 Developer Pack (https://www.microsoft.com/en-us/download/details.aspx?id=49978)
* Node.js (https://nodejs.org/en/download/)

If including the bot in the setup:

* Microsoft Azure CLI (https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)

**Important**: after installing any of the pre-requisites displayed above, you will need to exit powershell and re-launch it to make sure all the environment variables are correctly picked up by the shell.

**Important**: when you run any of these scripts, a free-tiered app service plan will be created. If your subscription already hit the 10 free app service plans limit, either change subscriptions, delete a free app service plan or move it to a different pricing tier so the setup can be completed.

# Installing Proposal Manager
Proposal Manager can be easily installed using PowerShell. In this folder, a script called `Install-PMInstance.ps1` is included for your convenience.

This script is intended to run in that folder, with the whole repo downloaded to your machine. Trying to download only the scripts and running them without the code **will not work**.

Refer to this Automated Deployment Process [walk-through video](https://youtu.be/Pd62rhF6Cy0) for an overview of the process before you start. After this deployment process refer to [configure-proposalmanager video](https://youtu.be/WmOT6D2mQPs) to configure the system or see the Getting Started guide. Refer [this video](https://youtu.be/_Y_SAhd3sBc) for a comprehensive walk-through including add-ins.

Before running the script, please execute the following:

`Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`

**Important**: if you exited the shell and re-launched it, you will need to run the former command again; it is only good for the lifespan of the instance of the shell you're working with.

## Parameters

To run the script, you need to provide the following parameters:

Parameter|Meaning
---------|-------
PMAdminUpn|The upn (user principal name, for example: john.doe@domain.com) of the user that will be made administrator of this instance of Proposal Manager. It can be yourself or someone else. It will be added as the administrator of the Proposal Manager SharePoint site. **This user will later have to be added manually to the PM admins group in the Setup page.**
PMSiteAlias|The name of the SharePoint site to create for Proposal Manager (`proposalmanager` is ok most of the times).
OfficeTenantName|The name of the office tenant. For example, if your mail domain is @contoso.onmicrosoft.com, then the name of the tenant is "contoso".
AzureResourceLocation|The azure region in which you want the resources to be allocated (for example, "East US").
AzureSubscription|The name (Id also works) of the Azure subscription you want the resource group to be deployed to.
ResourceGroupName|OPTIONAL; The name of the resource group to deploy to. If nonexistent, one will be created. If not provided, defaults to the ApplicationName parameter value.
ApplicationName|OPTIONAL; The name of the application (for example, "proposalmanager"). If not provided, defaults to `propmgr-<Tenant name>`.
IncludeBot|FLAG; Include this parameter only if you also want the bot to be deployed by this script. Otherwise, don't include it.
IncludeAddins|FLAG; Specify only if you want the addins (Proposal Creation & Project Smart Link) to be deployed alongside the application. **Important: if you include this parameter, a SQL database will be created for the Project Smart Link add-in. To avoid incurring in costs, this db will be deployed in the free tier. Azure only allows for a single free db to be provisioned in the same subscription for each region, so if you already have another free db in that region and subscription, either change the subscription, change the region, or delete the existing free database before running the script.** **Important: to install addins on an existing Proposal Manager instance, please refer to the "Installing Add-Ins only" section.**
SqlServerAdminUsername|If IncluddeAddins was specified, this is the SQL Server admin username for the Project Smart Link SQL Server. This SQL Server is created by this script; it does not exist beforehand. Therefore, you don't need to look up the value for this parameter but rather invent it now and take note of what you input. If IncludeAddins was not specified, this parameter is ignored.
SqlServerAdminPassword|If IncluddeAddins was specified, this is the SQL Server admin password for the Project Smart Link SQL Server. This SQL Server is created by this script; it does not exist beforehand. Therefore, you don't need to look up the value for this parameter but rather invent it now and take note of what you input. If IncludeAddins was not specified, this parameter is ignored.
BotAzureSubscription|OPTIONAL; The name or id of the Azure subscription to register the bot in; it has to belong to the tenant identified by the OfficeTenantName parameter; if not included, you have to register the bot by hand by following the getting started guide and provide the bot name when prompted so.
AdminSharePointSiteUrl|OPTIONAL; The URL of the admin SharePoint site; if none is provided, the default one will be used.
MFA|FLAG; Specify only if your user login requires MFA. You will be required to enter your credentials multiple times throught the setup process.
Force|FLAG; Specify only if you explicitly intend to overwrite an existing installation of Proposal Manager.
Verbose|FLAG; Specify only for troubleshooting purposes to include detailed information of the installation process.
Mode|The *mode* of execution determines what tasks get done during the execution of the script. This is useful when you need to decouple, for example, the build from the deploy, to be able to build the application offline and then deploy them from a different security context in a different machine. See "available modes" for more details. **The default mode is _FULL_.**

### Execution modes

The available running modes are:

Mode|Builds the application|Registers the apps and generates manifests|Deploys to azure
----|----------------------|------------------------------------------|----------------
NoDeploy|Yes|Yes|No
DeployOnly|No|No|Yes
BuildOnly|Yes|No|No
RegisterDeploy|No|Yes|Yes
Full|Yes|Yes|Yes

**Important**: If your user account login is enforced with Multi-Factor Authentication (MFA), you _must_ use the -MFA flag. Otherwise, the installation will fail.

**Note**: If no mode is specified, the _FULL_ mode will be used.

**Note**: The "Registers the apps and generates manifests" column also includes the bot registration, if the -IncludeBot flag is included in the call, and the Group and SharePoint sites creation.

Execution modes allow you to build the application offline and then deploy them using an online machine.

For example, you might run the script in _BuildOnly_ mode in your workstation, hand off the full folder of Proposal Manager as is to the Ops team (it will contain the built applications), and let them run the script in _RegisterDeploy_ mode from the datacenter. This way, they don't need compilers and SDKs such as .NET Framework or npm; they only need a powershell console and a connection to Azure.

An alternative is to build and register the applications yourself, using the _NoDeploy_ mode, and then let the ops team deploy the already registered applications by running the script in _DeployOnly_ mode.

Following are the required parameters for each execution mode.

#### NoDeploy

The required parameters for this mode are:
- PMAdminUpn
- PMSiteAlias
- OfficeTenantName

#### DeployOnly

The required parameters for this mode are:
- AzureResourceLocation
- AzureSubscription

#### BuildOnly

The _BuildOnly_ mode requires no parameters. It simply builds the applications with no configuration attached, so the result will always be the same.

#### RegisterDeploy

The required parameters for this mode are:
- PMAdminUpn
- PMSiteAlias
- OfficeTenantName
- AzureResourceLocation
- AzureSubscription

#### Full

The required parameters for this mode are:
- PMAdminUpn
- PMSiteAlias
- OfficeTenantName
- AzureResourceLocation
- AzureSubscription

## Procedure

To find the subscription info, navigate to the [Azure Portal](https://portal.azure.com) and select Subscriptions. Pick the subscription  name or ID from the displayed list, for the subscription where you are planning to deploy the solution to.

You will be prompted for credentials two times. The first time, you need to log in to office 365 with your **office tenant global administrator credentials**. Once this is done, the script will start setting up office 365 to prepare it for the installation of Proposal Manager.

Once the tenant is ready, you will be asked to enter your **Azure contributor** credentials to deploy the Proposal Manager application to your azure account. The application will be installed in the default subscription. This can be changed later from the portal.

## Examples

### Invocation example including all components
`.\Install-PMInstance.ps1 -PMAdminUpn admin@contoso.onmicrosoft.com -PMSiteAlias proposalmanager -OfficeTenantName contoso -AzureResourceLocation "Central US" -AzureSubscription "Subscription name" -IncludeBot -IncludeAddins -SqlServerAdminUsername adminSL -SqlServerAdminPassword Pa$$w0rd1`
Note: -SqlServerAdminUsername cannot be "admin" nor an UPN (user principal name) 

### Invocation example only for Proposal Manager
`.\Install-PMInstance.ps1 -PMAdminUpn admin@contoso.onmicrosoft.com -PMSiteAlias proposalmanager -OfficeTenantName contoso -AzureResourceLocation "Central US" -AzureSubscription "Subscription name"`

### Invocation example for a Proposal Manager only reinstallation, with custom application and resource group names. 
`.\Install-PMInstance.ps1 -PMAdminUpn admin@contoso.onmicrosoft.com -PMSiteAlias proposalmanager -OfficeTenantName contoso -AzureResourceLocation "Central US" -AzureSubscription "Subscription name" -ApplicationName proposalmanager-custom -ResourceGroupName proposalmanager-group -Force`

### Invocation example in BuildOnly mode
`.\Install-PMInstance.ps1 -Mode BuildOnly`

### Invocation example in DeployOnly mode
`.\Install-PMInstance.ps1 -Mode DeployOnly -AzureResourceLocation "Central US" -AzureSubscription "Subscription name"`

## Further Steps

After deploying the app, the script will do 3 things to help you get started:
1. It will show you some useful data about the deployment, such as:
   * The url to the app
   * The SharePoint site url
   * The app id
2. It will generate a zip file that you can sideload to Microsoft Teams as the Proposal Manager teams add-in (the zip file will be automatically opened at the end of the execution so you don't have to locate it manually). If you also specified the -IncludeAddins parameter, two manifests will be generated, one for each add-in, in `\Addins\ProjectSmartLink\` and `\Addins\ProposalCreation\Manifest\` respectevely. Instructions on how to upload these manifests are in the getting started guide of each of them.
3. It will open your default browser in the first consent page. In order to get started with Proposal Manager:
   1. Log in to the page that was opened, using your admin credentials.
   2. Give consent for the permissions displayed. You will be redirected to the Proposal Manager login page.
   3. Log in to Proposal Manager, again with your admin credentials. You will be asked for a second consent. Give consent on behalf of the organization.
   4. After having given consent the second time, you will still see the Proposal Manager login page. At this point, change the url to go to /Setup (under the same domain). You'll see there the same login page.
   5. In the /Setup login page, sign in again, as always with your admin account. You'll see a third and final consent screen, which you need to accept on behalf of your organization once again.
   6. You'll still see the sign in page. Click "Sign in" one last time, and the Setup page should show up. At this point, you can continue with step 8 of the Getting Started Guide (Guided Setup).
4. Upload add-in manifest files to SharePoint Application Catalog:
    1. Log in to your SharePoint admin site: https://{tenant}-admin.sharepoint.com/_layouts/15/online/tenantadminapps.aspx - You need to be a Global Administrator
    2. Select the apps menu item for managing your Applications
    3. Click on the App Catalog
    4. Select the tile named Distribute apps for Office
    5. Click on the Upload button
    6. Choose the manifest files of the add-ins:
       1. Proposal Creation:
          1. Setup\$ApplicationName-proposal-creation-manifest.xml
          2. Setup\$ApplicationName-proposal-creation-powerpoint-manifest.xml
       2. Project Smart Link:
          1. Setup\$ApplicationName-project-smart-link-excel-manifest.xml
    7. Finally click on the OK button.
    
Note: The automated setup will add the O365 Global Administrator as the owner and member of all the created groups. If required, please login into https://portal.office.com and remove the user to enhance security.

# Installing Add-Ins only

If you only want to install Proposal Creation or Project Smart Link to an existing instance of Proposal Manager, you can now do so using the additional scripts `Install-PMProposalCreationInstance` and `Install-PMProjectSmartLinkInstance`.

## Project Smart Link

**Important: when you install Project Smart Link, a SQL database will be created for the add-in. To avoid incurring in costs, this db will be deployed in the free tier. Azure only allows for a single free db to be provisioned in the same subscription for each region, so if you already have another free db in that region and subscription, either change the subscription, change the region, or delete the existing free database before running the script.**

To run the `Install-PMProjectSmartLinkInstance`, you need to provide the following parameters:

Parameter|Meaning
---------|-------
OfficeTenantName|The name of the office tenant. For example, if your mail domain is @contoso.onmicrosoft.com, then the name of the tenant is "contoso".
AzureResourceLocation|The azure region in which you want the resources to be allocated (for example, "East US").
AzureSubscription|The name (id also works) of the azure subscription you want the resource group to be deployed to.
ResourceGroupName|OPTIONAL; The name of the resource group where the Proposal Manager main app resides.
ApplicationName|The name of the application (for example, "proposalmanager").
SqlServerAdminUsername|This is the sql server admin username for the project smart link sql server. This sql server is created by this script; it does not exist beforehand. Therefore, you don't need to look up the value for this parameter but rather invent it now and take note of what you input.
SqlServerAdminPassword|This is the sql server admin password for the project smart link sql server. This sql server is created by this script; it does not exist beforehand. Therefore, you don't need to look up the value for this parameter but rather invent it now and take note of what you input.
Mode|The *mode* of execution determines what tasks get done during the execution of the script. It has the same behavior and purpose as the main script detailed above.
MFA|FLAG; Specify only if your user login is enforced with MFA. 

Here is an example of how to invoke the script:

`.\Install-PMProjectSmartLinkInstance.ps1 -OfficeTenantName contoso -ResourceGroupName proposalmanager-group -ApplicationName contosopm -ProposalManagerAppId '5ba0f5f3-66e0-4826-becb-02988ca3f911' -AzureResourceLocation "South Central US" -AzureSubscription "Pay-As-You-Go Dev/Test" -SqlServerAdminUsername 'contosoSa' -SqlServerAdminPassword 'tattoine'`

## Proposal Creation

To run the `Install-PMProposalCreationInstance`, you need to provide the following parameters:

Parameter|Meaning
---------|-------
OfficeTenantName|The name of the office tenant. For example, if your mail domain is @contoso.onmicrosoft.com, then the name of the tenant is "contoso".
AzureResourceLocation|The azure region in which you want the resources to be allocated (for example, "East US").
AzureSubscription|The name (id also works) of the azure subscription you want the resource group to be deployed to.
ResourceGroupName|OPTIONAL; The name of the resource group where the Proposal Manager main app resides.
ApplicationName|The name of the application (for example, "proposalmanager").
ProposalManagerDomain|The domain of the existing Proposal Manager instance (for example, propmgr-contoso5.azurewebsites.net)
ProjectSmartLinkUrl|The url of an existing instance of Project Smart Link. This will enable opening Project Smart Link from the Proposal Creation section on the ribbon in Word.
ProposalManagerAppId|The app id of the existing Proposal Manager Instance to attach this instance of Proposal Creation to.
Mode|The *mode* of execution determines what tasks get done during the execution of the script. It has the same behavior and purpose as the main script detailed above.
MFA|FLAG; Specify only if your user login is enforced with MFA. 

Here is an example of how to invoke the script:

`.\Install-PMProposalCreationInstance.ps1 -OfficeTenantName contoso -ResourceGroupName proposalmanager-group -ApplicationName contosopm -ProposalManagerAppId '5ba0f5f3-66e0-4826-becb-02988ca3f911' -AzureResourceLocation 'South Central US' -AzureSubscription 'Pay-As-You-Go Dev/Test'  -ProposalManagerDomain "contosopm.azurewebsites.net" -ProjectSmartLinkUrl "https://contosopm-projectsmartlink.azurewebsites.net"`