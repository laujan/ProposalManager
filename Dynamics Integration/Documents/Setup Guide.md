# Dynamics Integration Setup Guide
## Overview
### Audience
This guide is targeted to IT and Operations professionals who need to integrate an existing installation of Proposal Manager with an existing Dynamics 365 for Sales organization. It can also be used as a starting point by software developers testing and customizing their own forks of the integration.
### Scope
This document focuses exclusively in the integration between existing installations of Proposal Manager and Dynamics 365 for Sales. The installation of those products is beyond the scope of this document.
## Installation
### Prerequisites
The professional performing the setup needs to have appropriate administrative privileges in the Office 365 tenant and the Dynamics 365 organization being integrated. For Office 365, we recommend being a member of the Global Admin role. For Dynamics 365, we recommend being a member of the System Administrator role.
### Set up
1. First of all, we need to gather some data about both Proposal Manager and the Dynamics 365 organization. We will need that data in the following steps.
   1. The data we need to retrieve about the Dynamics 365 organization are:
      * The **organization's web API url**.
   2. The data we need to retrieve about the Proposal Manager instance are:
      * The **name of the Proposal Manager SharePoint site root drive**. This is usually `Shared Documents`, but can vary depending on the tenant and the site.
      * The **group id of the Proposal Manager role that creates opportunities** (generally, this is the Relationship Managers group, but it depends on the Proposal Manager configuration).
      * The **name of the Azure AD group that corresponds to the Proposal Manager role that creates opportunities**.
      * The **names of the Azure AD groups that corresponds to the Proposal Manager roles that leads opportunities** (generally, this is just the Loan Officer group, but it depends on the Proposal Manager configuration).
      * The **name of the Proposal Manager role that creates opportunities**.
2. Install (import) the Proposal Manager solution (available in this repo) in the Dynamics 365 organization. For instructions on how to work with solutions in Dynamics 365 Customer Engagement, check [this doc](https://docs.microsoft.com/en-us/dynamics365/customer-engagement/customize/import-update-export-solutions).
3. Create a user for the Proposal Manager application in the Dynamics 365 organization. For information on how to do that, please check [this doc](https://docs.microsoft.com/en-us/dynamics365/customer-engagement/developer/use-multi-tenant-server-server-authentication#manually-create-a--application-user)
4. Create the appropriate SharePoint sites and locations in the Dynamics 365 organization. To do this, you need Document Management to be enabled for your organization. If it's not (or if you don't know), follow these [steps](https://docs.microsoft.com/en-us/dynamics365/customer-engagement/admin/set-up-dynamics-365-online-to-use-sharepoint-online#configure-a-new-organization). Then, create the following sites and locations:

   Type|Name|Parent|Absolute URL|Relative URL
   ----|----|------|------------|------------
   Site|Default Site|-|**Tenant SharePoint site URL**|-
   Site|Proposal Manager Site|Default Site|-|**Proposal Manager SharePoint site relative url** (for example, `sites/proposalmanager`)
   Location|Proposal Manager Site Drive|Proposal Manager Site|-|**Name of the Proposal Manager SharePoint site root drive**
   Location|Proposal Manager Temporary Folder|Proposal Manager Site Drive|-|`TempFolder`

**Important**: If your Dynamics 365 instance is configured with Unified Interface (UCI), as of May 2019 when invoking the new Sharepoint Site form, you will be informed with an error message that "the selected entity is read-only for this client", thus you will not be able to complete the steps of this guide. For now, until the Unified Interface allows edition of this entity, you will be forced to, at least temporally, revert to the legacy interface. Please refer to the Troubleshooting section of this guide for instruction on how to do so.

5. Go to the `appsettings.json` file, in the _WebReact_ project, and fill the following keys with the specified values:
   1. In the `Dynamics365` section:
   
      Key|Value
      ---|-----
      `OrganizationUri`|**Organization's web API url**
      `RootDrive`|**Name of the Proposal Manager SharePoint site root drive**
      `DefaultDealType`|You can specify a default Deal Type here to be assigned to all opportunities created through Dynamics 365. This causes an automatic Team creation in Microsoft Teams with all the channels dictated by the Deal Type. If this is desired, you must enter the full display name of the Deal Type between quotes.
      `OpportunityMapping`|Please refer to the Mapping configuration document included in this repo for more details on this field. The settings that ship with the solution will satisfy most of your needs.
   2. In the `OneDrive` section:
   
      Key|Value
      ---|-----
      `WebhookSecret`|An arbitrary security string. You need to come up with some secret and you write it here. That's it.
      `FormalProposalCallbackUrl`|Leave the default: `/api/dynamics/FormalProposal`
      `AttachmentCallbackUrl`|Leave the default: `/api/dynamics/Attachment`
   3. In the `ProposalManager` section:
   
      Key|Value
      ---|-----
      `CreatorRole:Id`|**Group ID of the Azure AD group that corresponds to the Proposal Manager role that creates opportunities**
      `CreatorRole:AdGroupName`|**Name of the Azure AD group that corresponds to the Proposal Manager role that creates opportunities**
      `CreatorRole:DisplayName`|**Name of the Proposal Manager role that creates opportunities**
      `LeadRoles`|**Names of the Azure AD groups that corresponds to the Proposal Manager roles that leads opportunities**. Use array notation, for example: [ "Loan Officer", "Another Group" ]
   4. In the `WebHooks:DynamicsCrm:SecretKey` section:
   
      Key|Value
      ---|-----
      `opportunity`|An arbitrary security string. You need to come up with some secret and you write it here.
      `connection`|An arbitrary security string. You need to come up with some secret and you write it here.
6. Register the webhooks using the Dynamics *Plugin Registration Tool*. For information on how to install the tool, check [this link](https://docs.microsoft.com/en-us/dynamics365/customer-engagement/developer/download-tools-nuget). To do this:
   1. Run the Plugin Registration Tool.
   2. Click "CREATE NEW CONNECTION" and log in to Dynamics 365 with your administrator credentials.
   3. Click "Register" > "Register new Web Hook".
   4. Fill the form as follows:
      * On "Name", write "Proposal Manager opportunities"
      * On "Endpoint URL", put the **Proposal Manager application url**, followed by this string: `/api/webhooks/incoming/dynamicscrm/opportunity`
      * On "Authentication", choose "WebhookKey"
      * On "Value", put the same secret you chose in step 5.4 for `opportunity`
      * Hit save
   5. Click "Register" > "Register new Web Hook".
   6. Fill the form as follows:
      * On "Name", write "Proposal Manager connections"
      * On "Endpoint URL", put the **Proposal Manager application url**, followed by this string: `/api/webhooks/incoming/dynamicscrm/connection`
      * On "Authentication", choose "WebhookKey"
      * On "Value", put the same secret you chose in step 5.4 for `connection`
      * Hit save
   7. Right click "Proposal Manager opportunities" and click "Register new Step". Fill the form as follows:
      * On "Message", choose "Create"
      * On "Primary Entity", choose "Opportunity"
      * On the "Execution mode", choose **Asynchronous**. _This is extremely important. If the execution mode is not marked as Asynchronous, the integration will not work._
      * Click "Register new step".
   8. Right click "Proposal Manager connections" and click "Register new Step". Fill the form as follows:
      * On "Message", choose "Create"
      * On "Primary Entity", choose "Connection"
      * On the "Execution mode", choose **Synchronous**.
      * Click "Register new step".

7. Give the integration engine permission to access the Proposal Manager API:
   1. Open Microsoft Teams (keep in mind that you need to be a Proposal Manager administrator)
   2. Go to the "Configuration" channel, in the Proposal Manager team
   3. In the radio button set at the bottom of the tab, select the "Permissions" view
   4. In the upper right corner of the tab, click on _"+ Add"_
   5. Fill the new row with the following values:
      * For _AD Group Name_, provide the string "aud_", followed by the **Proposal Manager app id** from its app registration. You can obtain it by going to the [app registration portal](apps.dev.microsoft.com). **Note**: this is not an _actual_ AD group name; it is the way that Proposal Manager has to authenticate applications instead of regular users.
      * For _Role_, type "Administrator"
      * For _Permissions_, select the following:
         * Opportunity_Create
         * Opportunities_ReadWrite_All
         * Opportunity_ReadWrite_Team
         * Opportunity_ReadWrite_Dealtype
      * For _Type_, select "Member"
   6. Once you provided all the necessary values, click in the save icon to save the changes. A green toast notification should appear below the list stating that the changes have been saved.


## Troubleshooting
### SharePoint Site entity is read-only in Unified Interface mode (UCI)
1. Open any notepad application, and paste the following code:

```javascript
var updateSetting = function(url, newValue) {
    var baseUrl = (url.endsWith("/") ? url + "api/data/v9.0/organizations" : url + "/api/data/v9.0/organizations");

    var xhrGet = new XMLHttpRequest();
    xhrGet.open("GET", baseUrl + "?$select=organizationid", true);
    xhrGet.onreadystatechange = function(e) {
        if (this.readyState == 4 && this.status == 200) {
            console.log('Recieved organization id');
            var orgId = JSON.parse(this.responseText)["value"][0].organizationid;

            var xhrPut = new XMLHttpRequest();
            xhrPut.open("PUT", baseUrl + "(" + orgId + ")/allowlegacyclientexperience", true);
            xhrPut.setRequestHeader('Content-type', 'application/json; charset=utf-8');
            xhrPut.onreadystatechange = function(e) {}
            xhrPut.send(JSON.stringify({
                value: newValue
            }));
            if (this.readyState == 4 && this.status == 200) {
                console.log('Succesfully updated setting');
            }
        }
    }
    xhrGet.send();
};

updateSetting("https://<YOUR_TENANT_NAME_HERE>.crm.dynamics.com", true);
```

2. Replace the <YOUR_TENANT_NAME_HERE> tag in the last line with the name of your tenant. The result should be something similar to https://contoso.crm.dynamics.com.
3. Keep that code in handy. With a PC browser, login to your Dynamics 365 tenant as any user member of the System Administrator role.
4. After the login, and after the main view has been loaded, open the JavaScript console of your browser. On Edge, this can be done pressing the F12 key; by default it should open a new window with the Console tab already open.
5. Copy the entire code from your notepad, paste it onto the console, and execute the code by pressing ENTER.
6. If everything went well, you should see both a *"Recieved organization id"* message, followed by a *"Succesfully updated setting message"*. If not, check that your user has permissions to update settings in your organization, and that the URL in the last line of the code points to your organization's Dynamics 365 instance.
7. Refresh the application pressing F5; you should see now the legacy interface. You can perform now Step 4 of this guide. 
8. When finished, you can go back to the new Interface perfoming these same steps but replacing the "true" of the last line of the code to a "false". The result should be something similar to `updateSetting("https://<YOUR_TENANT_NAME_HERE>.crm.dynamics.com",  false);`

### Not able to share files from Dynamics to the Proposal Manager Opportunity

This can happen if the Temporary Folder is not created during the initial Proposal Management configuration. You can check its existance and create it manually by following the next steps:

1. Go to the Proposal Manager SharePoint site.
2. On the left, click on "Site contents".
3. Open up the Documents folder.
4. If the folder is empty, then the Temporary Folder failed to create. Create it manually by clicking the "New" dropdown button, and selecting "Folder". For the name, type in "TempFolder", without the quotes.