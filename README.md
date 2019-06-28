# Overview
Built on the Microsoft 365 platform, the Proposal Manager solution enables banking organizations to streamline and automate the proposal process, create more effective proposals and better collaborate across the bank with due confidentiality. Proposal Manager streamlines and automates key stages of the proposal process, and will help improved front-office collaboration in a bank that helps increase win rates and accelerate client proposals. The solution leverages the powerful capabilities of Microsoft Teams, SharePoint Online, Power BI, Office 365, Azure, and Microsoft Graph. It is also capable of integrating with Dynamics 365 for Sales business process workflows through Proposal Manager APIs.  

The Proposal Management process consists of four key steps 
  - Opportunity information is collected from client and uploaded to Proposal Manager.
  - An internal team is constituted to conduct assessments on this opportunity.
  - Based on their role, each team member drives their respective process (legal, risk, credit check, analysis) to validate opportunity. 
  - They jointly develop a formal proposal document that can be shared with the client.
  
An opportunity is a potential deal identified by the Relationship Manager for one of his/her own clients based on knowledge gathered from customer discussions and market analysis. He then uses the Proposal Management process to convert the opportunity, by means of well-defined corporate lending processes and a hand-picked team of experts brought together to execute the process, to a proposal document that can then be presented to the customer, offering a loan or line of credit that they can use in line with the terms and conditions.

# Components
Proposal Manager consists of four key components:
  - Teams add-in to enable the core capabilities of Proposal Manager, including centralized administration, analytics capabilities and collaborative handling of individual opportunities. Each opportunity will be created as a Team in Microsoft Teams with channels configured to meet banking process needs. There is a centralized Team in Microsoft Teams – Proposal Manager master created as part of setup – is used by Relationship manager to create individual opportunities and manage administration and analytics tasks. Team members are added by Loan officer in respective opportunities based on their role.
  - Office add-in [Project Smart Link] to enable specific content in a proposal document to be dynamically linked to another office document such as a spreadsheet. 
  - Office add-in [Proposal Creation] to enable different sections of a proposal document to be assigned to specific owners along with associated tasks and status. 
  - Dynamics 365 integration sample enables an opportunity to be created from Dynamics 365 which can then be managed by Proposal Manager in Microsoft Teams. Team members and documents can be updated from Dynamics business processes.

New features in the latest release include:
  - All Proposal Manager features are available directly in Microsoft Teams.
  - Master team for proposal creation, administration, and configuration.
  - Individual teams are created automatically for each opportunity.
  -	Role based access control to Proposal Manager.
  -	Customizable process types and team channels for different banking/insurance scenarios.
  - Supporting corporate/commercial lending and investment banking pitchbook scenarios.
  - Analytics dashboard on commercial lending progress.
  - Automated deployment experience to get started quickly.
  - Project Smart Link enables adding on demand data in Word and PowerPoint from different external data sources including Excel or Webservices.
  - Integration with Dynamics 365 sales business processes through APIs.
  - Extensibility features for partners: Proposal Manager APIs, application channel tab customization, and Project Smart Link integration from external webservice.
  - Improved documentation and getting started videos in GitHub.
 
Detailed documentation and links to instructional videos are available in Documents folders present in the root folder and respective Add-Ins. 

# Before you start
Make sure that you have the following handy before starting the deployment process:
  - Admin access on an Office 365 tenant to register the app and manage content in SharePoint.
  - Owner or contributor access on an Azure Subscription to provision the web app.

# Getting Started
This repository consists of the source code for the Proposal Manager solution and all associated add-in components. The most recent release version is published at 'master'. Please refer to the [Getting Started Guide](https://github.com/OfficeDev/ProposalManager/blob/master/Documents/Proposal_Manager_Getting_Started_Guide.docx) to get started.

To facilitate quick deployment, Proposal Manager provides an automated experience detailed [here](https://github.com/OfficeDev/ProposalManager/blob/master/Setup/Automated%20Setup.md).

We have also published the following high level **walk-through videos** to help with the deployment process and to get a functional understanding:
  - [Proposal Manager - Automated Deployment](https://youtu.be/IXEX-tgD2Lg)
  - [Proposal Manager - Functional Overview](https://youtu.be/WfcyKNIVluM)
  - [Office Add-ins: Functional Overview](https://youtu.be/hy5TLFVum1E)
  - [Dynamics 365 Integration - Automated Deployment](https://youtu.be/22UyMAvEMeM)
  - [Dynamics 365 and Office add-ins - Functional Overview](https://youtu.be/cQfYfxT5a-I)

The Proposal Manager Automated Deployment walkthrough video covers how the deployment can be done for both the Proposal Manager Teams add-in as well as the Office add-ins and Dynamics 365 Webhook.

# Roadmap
The upcoming versions of Proposal Manager are set to incrementally add several value-added features, including:
  - Q & A Bot
  - Advanced Predictive Analytics 
 
Please use the Issues tab in GitHub to ask any questions, request help with troubleshooting or for new feature requests. 

# Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
