# Installing Proposal Manager
Proposal Manager can be easily installed using PowerShell. In this folder, a script called `Install-PMInstance.ps1` is included for your convenience.

This script is intended to run in that folder, with the whole repo downloaded to your machine. Trying to download only the scripts and running them without the code **will not work**.

Refer to this Automated Deployment Process [walk-through video](https://youtu.be/Pd62rhF6Cy0) for an overview of the process before you start. After this deployment process refer to [configure-proposal-manager-video](https://youtu.be/WmOT6D2mQPs) to configure the system or see the Getting Started guide. 

Before running the script, please execute the following:

`Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`

To run the script, you need to provide the following parameters:

Parameter|Meaning
---------|-------
PMAdminUpn|The upn of the user that will be made administrator of this instance of Proposal Manager. It can be yourself or someone else. It will be added as the administrator of the Proposal Manager SharePoint site. **This user will later have to be added manually to the PM admins group in the Setup page.**
PMSharePointSiteAlias|The name of the SharePoint site to create for Proposal Manager (`proposalmanager` is ok most of the times)
OfficeTenantName|The name of the office tenant. For example, if your mail domain is @contoso.onmicrosoft.com, then the name of the tenant is "contoso"
AzureResourceLocation|The azure region in which you want the resources to be allocated (for example, "East US")
AzureSubscription|The name (id also works) of the azure subscription you want the resource group to be deployed to.
ApplicationName|The name of the application (for example, "ProposalManager")

To find the subscription info, navigate to the [Azure Portal](https://portal.azure.com) and select Subscriptions. Pick the subscription  name or ID from the displayed list, for the subscription where you are planning to deploy the solution to.

You will be prompted for credentials two times. The first time, you need to log in to office 365 with your **office tenant global administrator credentials**. Once this is done, the script will start setting up office 365 to prepare it for the installation of Proposal Manager.

Once the tenant is ready, you will be asked to enter your **Azure contributor** credentials to deploy the Proposal Manager application to your azure account. The application will be installed in the default subscription. This can be changed later from the portal.

## Invocation example
`.\Install-PMInstance.ps1 -PMAdminUpn admin@contoso.onmicrosoft.com -PMSharePointSiteAlias proposalmanager -OfficeTenantName contoso -AzureResourceLocation "Central US"`

After deploying the app, the script will do 3 things to help you get started:
1. It will show you some useful data about the deployment, such as:
   * The url to the app
   * The SharePoint site url
   * The app id
2. It will generate a zip file that you can sideload to Microsoft Teams as the Proposal Manager teams add-in (the zip file will be automatically opened at the end of the execution so you don't have to locate it manually)
3. It will open your default browser in the first consent page. In order to get started with Proposal Manager:
   1. Log in to the page that was opened, using your admin credentials.
   2. Give consent for the permissions displayed. You will be redirected to the Proposal Manager login page.
   3. Log in to Proposal Manager, again with your admin credentials. You will be asked for a second consent. Give consent on behalf of the organization.
   4. After having given consent the second time, you will still see the Proposal Manager login page. At this point, change the url to go to /Setup (under the same domain). You'll see there the same login page.
   5. In the /Setup login page, sign in again, as always with your admin account. You'll see a third and final consent screen, which you need to accept on behalf of your organization once again.
   6. You'll still see the sign in page. Click "Sign in" one last time, and the Setup page should show up. At this point, you can continue with step 8 of the Getting Started Guide (Guided Setup).
   
   Note: The automated setup will add the O365 Global Administrator as the owner and member of all the created groups. If required, please login into https://portal.office.com and remove the user to enhance security.
