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
      * The **Proposal Manager's client id**
      * The **Proposal Manager's client secret**
      * The **Proposal Manager SharePoint site url**.
      * The **Proposal Manager SharePoint site id**.
      * The **name of the Proposal Manager SharePoint site root drive**. This is usually `Shared Documents`, but can vary depending on the tenant and the site.
      * The **Proposal Manager application url**.
      * The **Proposal Manager API url**.
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
      `ClientId`|**Proposal Manager's client id**
      `ClientSecret`|**Proposal Manager's client secret**
      `OrganizationUri`|**Organization's web API url**
      `ProposalManagerSite`|**Proposal Manager SharePoint site url** (only the protocol and domain name)
      `RootDrive`|**Name of the Proposal Manager SharePoint site root drive**
      `TemporaryFolder`|Leave the default value: `TempFolder`
      `OpportunityMapping`|More documentation on this topic is coming. For the moment, the settings that ship with the solution will satisfy most of your needs.
   2. In the `OneDrive` section:
      Key|Value
      ---|-----
      `WebhookSecret`|An arbitrary security string. You need to come up with some secret and you write it here. That's it.
      `FormalProposalCallbackUrl`|**Proposal Manager API url** + `dynamics/FormalProposal`
      `ProposalManagerBaseSiteId`|**Proposal Manager SharePoint site id**
      `RootTempFolderName`|Leave the default value: `/TempFolder`
      `AttachmentCallbackUrl`|**Proposal Manager API url** + `dynamics/Attachment`
   3. In the `ProposalManager` section:
      Key|Value
      ---|-----
      `AppUrl`|**Proposal Manager application url**
      `ApiUrl`|**Proposal Manager API url** (this is the **Proposal Manager application url** + `/api`)
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
   7. Right click "Proposal Manager connections" and click "Register new Step". Fill the form as follows:
      * On "Message", choose "Create"
      * On "Primary Entity", choose "Connection"
      * On the "Execution mode", choose **Synchronous**.
      * Click "Register new step".

This concludes setup of the integration