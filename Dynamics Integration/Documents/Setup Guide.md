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
5. Go to the `appsettings.json` file, in the _WebReact_ project, and fill the following keys with the specified values:
   1. In the `Dynamics365` section:
   
      Key|Value
      ---|-----
      `OrganizationUri`|**Organization's web API url**
      `RootDrive`|**Name of the Proposal Manager SharePoint site root drive**
      `OpportunityMapping`|More documentation on this topic is coming. For the moment, the settings that ship with the solution will satisfy most of your needs.
   2. In the `OneDrive` section:
   
      Key|Value
      ---|-----
      `WebhookSecret`|An arbitrary security string. You need to come up with some secret and you write it here. That's it.
      `FormalProposalCallbackUrl`|Leave the default: `/api/dynamics/FormalProposal`
      `AttachmentCallbackUrl`|Leave the default: `/api/dynamics/Attachment`
   3. In the `ProposalManager` section:
   
      Key|Value
      ---|-----
      `CreatorRole:Id`|**Group id of the Proposal Manager role that creates opportunities**
      `CreatorRole:AdGroupName`|**Name of the Azure AD group that corresponds to the Proposal Manager role that creates opportunities**
      `CreatorRole:DisplayName`|**Name of the Proposal Manager role that creates opportunities**
   4. In the `WebHooks:DynamicsCrm:SecretKey` section:
   
      Key|Value
      ---|-----
      `opportunity`|An arbitrary security string. You need to come up with some secret and you write it here.
      `connection`|An arbitrary security string. You need to come up with some secret and you write it here.
6. Register the webhooks using the dynamics plugin registration tool. For information on how to install the tool, check [this link](https://docs.microsoft.com/en-us/dynamics365/customer-engagement/developer/download-tools-nuget). To do this:
   1. Run the plugin registration tool.
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
      * For _Role_, select "Administrator"
      * For _Permissions_, select the following:
         * Opportunity_Create
         * Opportunities_ReadWrite_All
         * Opportunity_ReadWrite_Team
         * Opportunity_ReadWrite_Dealtype
   6. Once you provided all the necessary values, click in the empty white portion at the top of the tab to save the changes. A green toast notification should appear below the list stating that the changes have been saved.