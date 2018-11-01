# Overview
The Proposal Management process is built around three main entities – the Opportunity that gets processed by an internal team, the specific process/workflow identified to validate and move the opportunity forward, and the associated Proposal that is prepared as an outcome of the process.

An Opportunity is a potential deal identified by the Relationship Manager for one of his/her own clients based on knowledge gathered from customer discussions and market analysis. He then uses the Proposal Management process to convert the opportunity, by means of well-defined corporate lending processes and a hand-picked team of experts brought together to execute the process, to a proposal document that can then be presented to the customer, offering a loan or line of credit that they can use in line with the terms and conditions.

The Proposal Manager solution leverages the collaborative capabilities of Microsoft Teams to enable an end-to-end proposal management experience. 

# Components
Proposal Manager consists of four key components:
  - Teams add-in to enable the core capabilities of Proposal Manager, including centralized administration, analytics capabilities and collaborative handling of opportunities
  - Office add-in [Project Smart Link] to enable specific content in a proposal document to be dynamically linked to another office document such as a spreadsheet
  - Office add-in [Proposal Creation] to enable different sections of a proposal document to be assigned to specific owners along with associated tasks and status
  - Dynamics 365 web-hook to enable an opportunity to be created from Dynamics 365 which can then be managed further from Microsoft Teams

# Before you start
Make sure that you have the following handy before starting the deployment process:
  - Admin access on an Office 365 tenant to register the app and manage content in SharePoint
  - Owner or contributor access on an Azure Subscription to provision the web app

# Getting Started
This repository consists of the source code for the Proposal Manager solution and all associated add-in components. The most recent release version is published at 'master'. Please refer to the [Getting Started Guide](https://github.com/OfficeDev/ProposalManager/blob/master/Documents/Proposal_Manager_Getting_Started_Guide.docx) to get started.

We have also published the following high level **walk-through videos** to help with the deployment process and to get a functional understanding:
  - [Proposal Manager Deployment Overview](https://youtu.be/mlmzLMFDxcQ)
  - [Proposal Manager Functional Overview](https://youtu.be/lNjG9e9U0p0)

Videos for Dynamics 365 Web Hook and the Office add-ins are in progress and will be published soon.

# Roadmap
The upcoming versions of Proposal Manager are set to incrementally add several value-added features, including:
  - Q & A Bot
  - Advanced Predictive Analytics
  - Support for more scenarios such as Pitchbook
 
Please use the Issues tab in GitHub to ask any questions, request help with troubleshooting or for new feature requests. 
# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
