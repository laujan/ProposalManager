# Pre-requisites
* .NET Core 2.1 (https://dotnet.microsoft.com/download/thank-you/dotnet-sdk-2.1.500-windows-x64-installer)
* .NET Framework 4.6.1 Developer Pack (https://www.microsoft.com/en-us/download/details.aspx?id=49978)
* Node.js (https://nodejs.org/en/download/)
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

To run the script, you need to provide the following parameters:

Parameter|Meaning
---------|-------
PMAdminUpn|The upn (user principal name, for example: john.doe@domain.com) of the user that will be made administrator of this instance of Proposal Manager. It can be yourself or someone else. It will be added as the administrator of the Proposal Manager SharePoint site. **This user will later have to be added manually to the PM admins group in the Setup page.**
PMSiteAlias|The name of the SharePoint site to create for Proposal Manager (`proposalmanager` is ok most of the times).
OfficeTenantName|The name of the office tenant. For example, if your mail domain is @contoso.onmicrosoft.com, then the name of the tenant is "contoso".
AzureResourceLocation|The azure region in which you want the resources to be allocated (for example, "East US").
AzureSubscription|The name (id also works) of the azure subscription you want the resource group to be deployed to.
ApplicationName|The name of the application (for example, "proposalmanager").
IncludeBot|FLAG; Include this parameter only if you also want the bot to be deployed by this script. Otherwise, don't include it.
IncludeAddins|FLAG; Specify only if you want the addins (Proposal Creation & Project Smart Link) to be deployed alongside the application. **Important: if you include this parameter, a SQL database will be created for the Project Smart Link add-in. To avoid incurring in costs, this db will be deployed in the free tier. Azure only allows for a single free db to be provisioned in the same subscription for each region, so if you already have another free db in that region and subscription, either change the subscription, change the region, or delete the existing free database before running the script.** **Important: to install addins on an existing Proposal Manager instance, please refer to the "Installing Add-Ins only" section.**
SqlServerAdminUsername|If IncluddeAddins was specified, this is the sql server admin username for the project smart link sql server. This sql server is created by this script; it does not exist beforehand. Therefore, you don't need to look up the value for this parameter but rather invent it now and take note of what you input. If IncludeAddins was not specified, this parameter is ignored.
SqlServerAdminPassword|If IncluddeAddins was specified, this is the sql server admin password for the project smart link sql server. This sql server is created by this script; it does not exist beforehand. Therefore, you don't need to look up the value for this parameter but rather invent it now and take note of what you input. If IncludeAddins was not specified, this parameter is ignored.
BotAzureSubscription|OPTIONAL; The name or id of the Azure subscription to register the bot in; it has to belong to the tenant identified by the OfficeTenantName parameter; if not included, you have to register the bot by hand by following the getting started guide and provide the bot name when prompted so.
AdminSharePointSiteUrl|OPTIONAL; The url of the admin sharepoint site; if none is provided, the default one will be used.
Force|FLAG; Specify only if you explicitly intend to overwrite an existing installation of Proposal Manager.

To find the subscription info, navigate to the [Azure Portal](https://portal.azure.com) and select Subscriptions. Pick the subscription  name or ID from the displayed list, for the subscription where you are planning to deploy the solution to.

You will be prompted for credentials two times. The first time, you need to log in to office 365 with your **office tenant global administrator credentials**. Once this is done, the script will start setting up office 365 to prepare it for the installation of Proposal Manager.

Once the tenant is ready, you will be asked to enter your **Azure contributor** credentials to deploy the Proposal Manager application to your azure account. The application will be installed in the default subscription. This can be changed later from the portal.

## Invocation example including all components
`.\Install-PMInstance.ps1 -PMAdminUpn admin@contoso.onmicrosoft.com -PMSiteAlias proposalmanager -OfficeTenantName contoso -AzureResourceLocation "Central US" -IncludeBot -IncludeAddins -SqlServerAdminUsername adminSL -SqlServerAdminPassword Pa$$w0rd1`
Note: -SqlServerAdminUsername cannot be "admin" nor an UPN (user principal name) 

## Invocation example only for Proposal Manager
`.\Install-PMInstance.ps1 -PMAdminUpn admin@contoso.onmicrosoft.com -PMSiteAlias proposalmanager -OfficeTenantName contoso -AzureResourceLocation "Central US"`

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
    4.1) Log in to your SharePoint admin site: https://{tenant}-admin.sharepoint.com/_layouts/15/online/tenantadminapps.aspx - You need to be a Global Administrator
    4.2) Select the apps menu item for managing your Applications
    4.3) Click on the App Catalog
    4.4) Select the tile named Distribute apps for Office
    4.5) Click on the Upload button
    4.6) Choose the manifest files of the add-ins:
        a) Proposal Creation: 
          a.1) Addins\ProposalCreation\Manifest\proposal-creation-manifest.xml
          a.2) Addins\ProposalCreation\Manifest\proposal-creation-powerpoint-manifest.xml
        b) Project Smart Link:
          b.1) Addins\ProjectSmartLink\ProjectSmartLinkExcel\ProjectSmartLinkExcelManifest\ProjectSmartLinkExcel.xml
          b.2) Addins\ProjectSmartLink\ProjectSmartLinkPowerPoint\ProjectSmartLinkPowerPointManifest\ProjectSmartLinkPowerPoint.xml
    4.7) Finally click on the OK button.
    
Note: The automated setup will add the O365 Global Administrator as the owner and member of all the created groups. If required, please login into https://portal.office.com and remove the user to enhance security.

# Installing Add-Ins only

If you only want to install Proposal Creation or Project Smart Link to an existing instance of Proposal Manager, you can now do so using the additional scripts `Install-PMProposalCreationInstance` and `Install-PMProjectSmartLinkInstance`.

**Important: when you install Project Smart Link, a SQL database will be created for the add-in. To avoid incurring in costs, this db will be deployed in the free tier. Azure only allows for a single free db to be provisioned in the same subscription for each region, so if you already have another free db in that region and subscription, either change the subscription, change the region, or delete the existing free database before running the script.**

To run the `Install-PMProjectSmartLinkInstance`, you need to provide the following parameters:

Parameter|Meaning
---------|-------
OfficeTenantName|The name of the office tenant. For example, if your mail domain is @contoso.onmicrosoft.com, then the name of the tenant is "contoso".
AzureResourceLocation|The azure region in which you want the resources to be allocated (for example, "East US").
AzureSubscription|The name (id also works) of the azure subscription you want the resource group to be deployed to.
ApplicationName|The name of the application (for example, "proposalmanager").
SqlServerAdminUsername|This is the sql server admin username for the project smart link sql server. This sql server is created by this script; it does not exist beforehand. Therefore, you don't need to look up the value for this parameter but rather invent it now and take note of what you input.
SqlServerAdminPassword|This is the sql server admin password for the project smart link sql server. This sql server is created by this script; it does not exist beforehand. Therefore, you don't need to look up the value for this parameter but rather invent it now and take note of what you input.
ProposalManagerAppId|The app id of the existing Proposal Manager Instance to attach this instance of Project Smart Link to.

Here is an example of how to invoke the script:

`.\Install-PMProjectSmartLinkInstance.ps1 -OfficeTenantName contoso -ApplicationName contosopm -ProposalManagerAppId '5ba0f5f3-66e0-4826-becb-02988ca3f911' -AzureResourceLocation "South Central US" -AzureSubscription "Pay-As-You-Go Dev/Test" -SqlServerAdminUsername 'contosoSa' -SqlServerAdminPassword 'tattoine'`

To run the `Install-PMProposalCreationInstance`, you need to provide the following parameters:

Parameter|Meaning
---------|-------
OfficeTenantName|The name of the office tenant. For example, if your mail domain is @contoso.onmicrosoft.com, then the name of the tenant is "contoso".
AzureResourceLocation|The azure region in which you want the resources to be allocated (for example, "East US").
AzureSubscription|The name (id also works) of the azure subscription you want the resource group to be deployed to.
ApplicationName|The name of the application (for example, "proposalmanager").
ProposalManagerDomain|The domain of the existing Proposal Manager instance (for example, propmgr-contoso5.azurewebsites.net)
ProjectSmartLinkUrl|The url of an existing instance of Project Smart Link. This will enable opening Project Smart Link from the Proposal Creation section on the ribbon in Word.
ProposalManagerAppId|The app id of the existing Proposal Manager Instance to attach this instance of Proposal Creation to.

Here is an example of how to invoke the script:

`.\Install-PMProposalCreationInstance.ps1 -OfficeTenantName contoso -ApplicationName contosopm -ProposalManagerAppId '5ba0f5f3-66e0-4826-becb-02988ca3f911' -AzureResourceLocation 'South Central US' -AzureSubscription 'Pay-As-You-Go Dev/Test'  -ProposalManagerDomain "contosopm.azurewebsites.net" -ProjectSmartLinkUrl "https://contosopm-projectsmartlink.azurewebsites.net"`