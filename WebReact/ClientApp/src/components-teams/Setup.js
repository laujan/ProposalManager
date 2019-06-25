/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/
import React, { Component } from 'react';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { I18n, Trans } from "react-i18next";
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import ImportDealType from './general/DealType/ImportDealType';


/**
 * This component is for Setup page
 * This will render different sections in the page
 * Each section can be mandatory or optional.
 * Each section has its own render function and methods.
 */

export class Setup extends Component {
    displayName = Setup.name

    constructor(props) {
        super(props);
        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        
        this.logService.log("Setup : Constructor");
        this.state = {
            loading: true,
            isUpdateOpp: false,
            isUpdateOppMsg: false,
            updateMessageBarType: MessageBarType.success,
            updateOppMessagebarText: "nil",
            PMAddinName: "",
            appId: "",
            PMTeamName: "",
            ADGroupName: "",
            ProposalManagement_Misc: {
                "UserProfileCacheExpiration": 0,
                "GraphRequestUrl": "https://graph.microsoft.com/v1.0/",
                "GraphBetaRequestUrl": "https://graph.microsoft.com/beta/",
                "SetupPage": "",
                "SharePointListsPrefix": "e3_",
                "VaultBaseUrl": ""
            },
            ProposalManagement_Team: {
                "GeneralProposalManagementTeam": "",
                "ProposalManagerAddInName": "",
                "TeamsAppInstanceId": "",
                "ProposalManagerGroupID": ""
            },
            ProposalManagement_Sharepoint: {
                "SharePointHostName": "",
                "ProposalManagementRootSiteId": "",
                "SharePointSiteRelativeName": "",
                "TemplateListId": "",
                "RoleListId": "",
                "Permissions": "",
                "ProcessListId": "",
                "DashboardListId": "",
                "OpportunitiesListId": "",
                "GroupsListId": "",
                "OpportunityMetaDataId" :""
            },
            ProposalManagement_bot: {
                "BotServiceUrl": "https://smba.trafficmanager.net/amer-client-ss.msg/",
                "BotName": "Proposal Manager <tenant>",
                "BotId": "",
                "MicrosoftAppId": "",
                "MicrosoftAppPassword": "",
                "AllowedTenants": ""
            },
            ProposalManagement_BI: {
                "PBIUserName": "",
                "PBIUserPassword": "",
                "PBIApplicationId": "",
                "PBIWorkSpaceId": "",
                "PBIReportId": "",
                "PBITenantId": ""
            },
            DocumentIdActivator: {
                "WebhookAddress": "",
                "WebhookUsername": "",
                "WebhookPassword": "",
                "SharePointAppId": "",
                "SharePointAppSecret": ""
            },
            renderStep_0: false,
            renderStep_1: false,
            renderStep_2: false,
            renderStep_3: false,
            sharepoint: false,
            misc: false,
            bot: false,
            powerbi: false,
            documentid: false,
            finish: false
        };

        this.SetAppSetting_JsonKeys = this.SetAppSetting_JsonKeys.bind(this);
        this.CreateAdminPermissions = this.CreateAdminPermissions.bind(this);
        this.onBlurSetPM = this.onBlurSetPM.bind(this);
        this.onBlurOnBotSettings = this.onBlurOnBotSettings.bind(this);
        this.onBlurOnBISettings = this.onBlurOnBISettings.bind(this);
        this.onBlurOnDocumentIdActivatorSettings = this.onBlurOnDocumentIdActivatorSettings.bind(this);
        this.loadDataForPermision_Process_Roles = this.loadDataForPermision_Process_Roles.bind(this);
    }

    async componentDidMount() {
        this.logService.log("SetUp_componentDidMount");
        await this.setClientSettings();
    }

    async setClientSettings() {
        let ProposalManagement_Sharepoint = { ...this.state.ProposalManagement_Sharepoint };
        let ProposalManagement_BI = { ...this.state.ProposalManagement_BI };
        let DocumentIdActivator = { ...this.state.DocumentIdActivator };
        let ProposalManagement_bot = { ...this.state.ProposalManagement_bot };
        let ProposalManagement_Misc = { ...this.state.ProposalManagement_Misc };
        let ProposalManagement_Team = { ...this.state.ProposalManagement_Team };
        let SharepointObj = await this.getClientSettings();

        for (const key of Object.keys(SharepointObj)) {
            let value = SharepointObj[key] || this.defaultValue(key);
            if (ProposalManagement_Sharepoint.hasOwnProperty(key)) {
                ProposalManagement_Sharepoint[key] = value;
            }
            if (ProposalManagement_BI.hasOwnProperty(key)) {
                ProposalManagement_BI[key] = value;
            }
            if (DocumentIdActivator.hasOwnProperty(key)) {
                DocumentIdActivator[key] = value;
            }
            if (ProposalManagement_bot.hasOwnProperty(key)) {
                ProposalManagement_bot[key] = value;
            }
            if (ProposalManagement_Misc.hasOwnProperty(key)) {
                ProposalManagement_Misc[key] = value;
            }
            if (ProposalManagement_Team.hasOwnProperty(key)) {
                ProposalManagement_Team[key] = value;
            }
        }

        this.logService.log("SetUp_setClientSettings: ", ProposalManagement_Misc, ProposalManagement_Sharepoint, ProposalManagement_bot, ProposalManagement_BI, ProposalManagement_Team, DocumentIdActivator);
        this.setState(
            {
                ProposalManagement_Misc,
                ProposalManagement_Sharepoint,
                ProposalManagement_bot,
                ProposalManagement_BI,
                ProposalManagement_Team,
                DocumentIdActivator,
                loading: false
            });
    }

    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    async getClientSettings() {
        try {
            this.logService.log("Setup_getClientSettings");
            let data = await this.apiService.callApi('Context', 'GET', { id: 'GetClientSettings'});
            return await data.json();
        } catch (error) {
            this.logService.log("Setup_getClientSettings error: ", error.message);
            return { "setupPage": "" };
        }
    }

    showSpinnerAndMessage() {
        return (
            <div className='ms-Grid-row'>
                <div className="ms-Grid-col">
                    {
                        this.state.isUpdateOpp ?
                            <div className='ms-BasicSpinnersExample'>
                                <Spinner size={SpinnerSize.large} label={<Trans>Updating</Trans>} ariaLive='assertive' />
                            </div>
                            : ""
                    }
                </div>
                <div>
                    {
                        this.state.isUpdateOppMsg ?
                            <MessageBar messageBarType={this.state.updateMessageBarType}>
                                {this.state.updateOppMessagebarText}
                            </MessageBar>
                            : ""
                    }
                </div>
            </div>
        );
    }

    async hideMessagebar() {
        await this.delay(3500);
        this.setState({ isUpdateOpp: false, isUpdateOppMsg: false, updateOppMessagebarText: "", updateMessageBarType: MessageBarType.info });
    }

    placeholderForProposalManager() {
        let obj = {
            "SharePointHostName": "<tenant>.sharepoint.com",
            "SharePointSiteRelativeName": "Give Sharepoint relative web address (eg: proposalmanager)",
            "SharePointListsPrefix": "e3_",
            "CategoriesListId": "Categories",
            "TemplateListId": "Templates",
            "RoleListId": "Roles",
            "Permissions": "Permission",
            "ProcessListId": "WorkFlow Items",
            "IndustryListId": "Industry",
            "RegionsListId": "Regions",
            "DashboardListId": "DashBoard",
            "RoleMappingsListId": "RoleMappings",
            "OpportunitiesListId": "Opportunities",
            "TasksListId": "Tasks",
            "BotName": "Proposal Manager Bot",
            "BotId": "GUID",
            "PBIUserName": "Power BI user name",
            "PBIUserPassword": "Power BI user password",
            "PBIApplicationId": "Power BI App ID",
            "PBIWorkSpaceId": "Power BI Workspace ID",
            "PBIReportId": "Power BI Report ID",
            "PBITenantId": "Your Azure tenant ID",
            "UserProfileCacheExpiration": 0,
            "GraphRequestUrl": "https://graph.microsoft.com/v1.0/",
            "GraphBetaRequestUrl": "https://graph.microsoft.com/beta/",
            "BotServiceUrl": "https://smba.trafficmanager.net/amer-client-ss.msg/",
            "WebhookAddress": "https://<app_name>.scm.azurewebsites.net/api/triggeredwebjobs/DocumentIdActivator/run",
            "WebhookUsername": "The username to run the webjob",
            "WebhookPassword": "The username to run the webjob",
            "SharePointAppId": "The app id from the SharePoint application registration",
            "SharePointAppSecret": "The app secret from the SharePoint application registration",
            "VaultBaseUrl": "<name>.vault.azure.net/",
            "GroupsListId":"GroupsListId",
            "OpportunityMetaDataId" : "OpportunityMetaDataId"
        };
        return function (key) {
            return obj[key];
        };
    }

    defaultValue(key) {
        let obj = {
            "SharePointListsPrefix": "e3_",
            "CategoriesListId": "Categories",
            "TemplateListId": "Templates",
            "RoleListId": "Roles",
            "Permissions": "Permission",
            "ProcessListId": "WorkFlow Items",
            "OpportunityMetaDataId": "OpportunityMetaData",
            "GroupsListId": "Groups",
            "DashboardListId": "DashBoard",
            "RoleMappingsListId": "RoleMappings",
            "OpportunitiesListId": "Opportunities",
            "TasksListId": "Tasks",
            "BotName": "Proposal Manager <tenant>",
            "UserProfileCacheExpiration": 30,
            "SharePointSiteRelativeName": "proposalmanager",
            "SharePointHostName": "<tenant>.sharepoint.com",
            "VaultBaseUrl": ""
        };

        return obj[key] || "";

    }

    setSpinnerAndMsg(isUpdateOpp, isUpdateOppMsg, updateOppMessagebarText, updateMessageBarType = MessageBarType.info) {
        this.setState({ isUpdateOpp, isUpdateOppMsg, updateOppMessagebarText, updateMessageBarType });
    }

    async downloadJsonObject() {
        this.setState({ finish: true });
        this.setSpinnerAndMsg(true, false, "");

        try {
            let SharepointObj = await this.getClientSettings();
            let obj = { "ProposalManagement": SharepointObj };
            var data = "data:text/json;charset=utf-8," + encodeURIComponent(JSON.stringify(obj));
            let dlAnchorElem = document.getElementById('downloadFile');
            dlAnchorElem.setAttribute("href", data);
            dlAnchorElem.setAttribute("download", "appsettings_ProposalManagement.json");
            dlAnchorElem.click();
        } catch (error) {
            this.logService.log("Setup_downloadJsonObject error: ", error.message);
        }

        this.setSpinnerAndMsg(false, false, "");
        this.setState({ finish: false });
    }

    async UpdateAppSettings(key, value) {
        try {
            this.logService.log("SetUp_updateAppSettings");
            // check vaultbaseurl contains http(s) - remove it
            value = key === "VaultBaseUrl" ? value.replace(/https?:\/\//, "") : value;
            
            let data = await this.apiService.callApi('Setup', 'POST', { id: `${key}/${value}`});
            this.logService.log("SetUp_updateAppSettings response: ", data);
            return true;
        } catch (error) {
            this.logService.log("SetUp_updateAppSettings error: ", error.message);
            return false;
        }
    }

    async UpdateDocumentIdActivatorSettings(key, value) {
        try {
            this.logService.log("SetUp_updateDocumentIdActivatorSettings");
            let postData = {
                key: key,
                value: value
            };

            let data = await this.apiService.callApi('Setup', 'POST', { id: 'documentId', body: JSON.stringify(postData) });
            this.logService.log("SetUp_updateDocumentIdActivatorSettings response: ", data);
            return true;
        } catch (error) {
            this.logService.log("SetUp_updateDocumentIdActivatorSettings error: ", error.message);
            return false;
        }
    }

    //Setp 3 & //Setp 4
    async SetAppSetting_JsonKeys(ProposalManagement, group, key = false) {
        this.logService.log("SetAppSetting_JsonKeys: ", ProposalManagement);
        this.spinnerOff(group, true);
        this.setSpinnerAndMsg(true, false, "");

        try {
            for (const Objkey of Object.keys(ProposalManagement)) {
                try {
                    if (Objkey !== "ProposalManagementRootSiteId") {
                        const contents = await this.UpdateAppSettings(Objkey, ProposalManagement[Objkey]);
                        this.logService.log(`SetAppSetting_JsonKeys_${Objkey} : `, contents);
                    }
                } catch (error) {
                    this.logService.log(`SetAppSetting_JsonKeys_${Objkey}_err : `, error.message);
                }
            }

            this.setSpinnerAndMsg(false, true, "Updated", MessageBarType.success);

        } catch (error) {
            this.setSpinnerAndMsg(false, true, error.message, MessageBarType.error);
            this.logService.log(`SetAppSetting_JsonKeys_err : `, error.message);
        }
        this.hideMessagebar();
        this.spinnerOff(group, false);
    }

    async SetDocumentIdActivatorSetting_JsonKeys(DocumentIdActivator, group, key = false) {
        this.logService.log("SetDocumentIdActivatorSetting_JsonKeys   : ", DocumentIdActivator);
        this.logService.log("SetDocumentIdActivatorSetting_JsonKeys   : ", DocumentIdActivator.constructor.name);
        this.spinnerOff(group, true);

        if (!DocumentIdActivator.SharePointAppId || !DocumentIdActivator.SharePointAppSecret) {
            alert("All fields are mandatory");
        }
        else {
            this.setSpinnerAndMsg(true, false, "");
            try {

                for (const Objkey of Object.keys(DocumentIdActivator)) {
                    try {
                        const contents = await this.UpdateDocumentIdActivatorSettings(Objkey, DocumentIdActivator[Objkey]);
                        this.logService.log(`SetDocumentIdActivatorSetting_JsonKeys_${Objkey} : `, contents);
                    } catch (error) {
                        this.logService.log(`SetDocumentIdActivatorSetting_JsonKeys_${Objkey}_err : `, error.message);
                    }
                }

                this.setSpinnerAndMsg(false, true, "Updated", MessageBarType.success);

            } catch (error) {
                this.setSpinnerAndMsg(false, true, error.message, MessageBarType.error);
                this.logService.log(`SetDocumentIdActivatorSetting_JsonKeys_err : `, error.message);
            }
        }
        this.hideMessagebar();
        this.spinnerOff(group, false);
    }

    async CreateAllLists() {
        try {
            this.logService.log("Setup_createAllLists");
            let data = await this.apiService.callApi('Setup', 'POST', { id: 'CreateAllLists' });
            this.logService.log("Setup_createAllLists response: ", data);
            return true;
        } catch (error) {
            this.logService.log("Setup_createAllLists error: ", error.message);
            return false;
        }
    }

    //Setp 5
    async CreateAdminPermissions() {
        this.setState({ renderStep_5: true });
        let AdGroupName = this.state.ADGroupName || "Proposal Manager Administrators";
        this.setState({ renderStep_3: true });
        this.setSpinnerAndMsg(true, false, "");

        try {
            await this.UpdateAppSettings("UserProfileCacheExpiration", 30);
            await this.UpdateAppSettings("SharePointListsPrefix", "e3_");

            await this.CreateAllLists();
            await this.CreateProposalManagerAdminGroup(AdGroupName);

            await this.loadDataForPermision_Process_Roles();
            await this.CreateProposalManagerTeam(AdGroupName);

            if(!await this.CheckEverythingCreated()){
                throw new Error ("Lists are not created");
            }
            await this.UpdateAppSettings("SetupPage", "disabled");
            
            let  ProposalManagement_Misc = JSON.parse(JSON.stringify(this.state.ProposalManagement_Misc));
            ProposalManagement_Misc.SetupPage = "disabled";
            this.setState({ProposalManagement_Misc});

        } catch (error) {
            this.setSpinnerAndMsg(false, true, error.message, MessageBarType.error);
            this.logService.log("Setup_CreateAdminPermissions error : ", error.message);
        }

        await this.hideMessagebar();        
        this.setState({ renderStep_3: true });
    }

    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    async CheckEverythingCreated(){
        try {
           
            let response = await this.apiService.callApi('MetaData', 'GET');
            let data = await response.json();
            if (response.ok) {
                if (data.length > 10) {
                    return true;
                }
                else {
                    throw new Error("Meta data is empty");
                }
            }
            else
            {
                throw new Error(`Meta data get api error: ${data.error.message}`);
            }
        } catch (error) {
            this.logService.log("everything creeted : ", error.message);
            return false;
        }
    }
       //Setp 1
    async CreateProposalManagerTeam(PMTeamName) {
        try {
            this.logService.log("Setup_CreateProposalManagerTeam", PMTeamName);
            if (PMTeamName) {
                await this.apiService.callApi('Setup', 'POST', {id: `CreateProposalManagerTeam/${PMTeamName}` });
            } 
        } catch (error) {
            this.setSpinnerAndMsg(false, true, error.message, MessageBarType.error);
            this.logService.log("Setup_CreateProposalManagerTeam error : ", error.message);
        }
    }

    //step 5
    async loadDataForPermision_Process_Roles() {
        let requestUriArray = ["CreateSitePermissions","CreateSiteProcesses","CreateMetaDataList","CreateDefaultBusinessProcess"];
        try {
            this.logService.log("Setup_loadDataForPermision_Process_Roles");
            for (const uri of requestUriArray) {
                try {
                    let data = await this.apiService.callApi('Setup', 'POST', { id: uri });
                    this.logService.log("Setup_loadDataForPermision_Process_Roles response: ", data);
                } catch (error) {
                    this.logService.log(error.message);
                }
            }
            return true;
        } catch (error) {
            this.logService.log("Setup_loadDataForPermision_Process_Roles: ", error.message);
            return false;
        }
    }

    //step 1
    async CreateProposalManagerAdminGroup(AdGroupName) {
        try {
            this.logService.log("Setup_CreateProposalManagerAdminGroup", AdGroupName);
            if (AdGroupName) {
                let data = await this.apiService.callApi('Setup', 'POST', { id: `CreateProposalManagerAdminGroup/${AdGroupName}` });
                this.logService.log("Setup_CreateProposalManagerAdminGroup response: ", data);
                return true;
            } else
                throw new Error("PMAddinName cannot be empty");
        } catch (error) {
            this.logService.log("Setup_CreateProposalManagerAdminGroup error : ", error.message);
        }
    }

    async spinnerOff(group, flag) {
        switch (group) {
            case "sharepoint":
                this.setState({ sharepoint: flag });
                break;
            case "powerbi":
                this.setState({ powerbi: flag });
                break;
            case "bot":
                this.setState({ bot: flag });
                break;
            case "misc":
                this.setState({ misc: flag });
                break;
            case "DocumentIdActivator":
                this.setState({ documentid: flag });
                break;
            default:
                break;
        }
    }

    async onBlurSetPM(e, key) {
        let value = e.target.value;
        const ProposalManagement_Team = { ...this.state.ProposalManagement_Team };

        if (value) {
            value = value.trim();
            switch (key) {
                case "PMAddinName":
                    ProposalManagement_Team.ProposalManagerAddInName = value;
                    this.setState({ PMAddinName: value });
                    break;
                case "PMTeamName":
                    ProposalManagement_Team.GeneralProposalManagementTeam = value;
                    this.setState({ PMTeamName: value });
                    break;
                case "APPID":
                    ProposalManagement_Team.TeamsAppInstanceId = value;
                    this.setState({ appId: value });
                    break;
                case "ADGroupName":
                    this.setState({ ADGroupName: value });
                    break;
                default:
                    break;
            }
        }
        this.setState({ ProposalManagement_Team });
    }

    async onBlurOnBotSettings(e, key) {
        let value = e.target.value;
        let obj = {};
        const ProposalManagement_bot = { ...this.state.ProposalManagement_bot };
        if (ProposalManagement_bot.hasOwnProperty(key) && value) {
            ProposalManagement_bot[key] = value;
            obj[key] = value;
            this.setState({ ProposalManagement_bot });
        }
    }

    async onBlurOnDocumentIdActivatorSettings(e, key) {
        let value = e.target.value;
        let obj = {};
        const DocumentIdActivator = { ...this.state.DocumentIdActivator };
        if (DocumentIdActivator.hasOwnProperty(key) && value) {
            DocumentIdActivator[key] = value;
            obj[key] = value;
            this.setState({ DocumentIdActivator });
        }
    }

    async onBlurOnBISettings(e, key) {
        let value = e.target.value;
        let obj = {};
        const ProposalManagement_BI = { ...this.state.ProposalManagement_BI };
        if (ProposalManagement_BI.hasOwnProperty(key) && value) {
            ProposalManagement_BI[key] = value;
            obj[key] = value;
            this.setState({ ProposalManagement_BI });
        }
    }

    async onBlurOnWebhookAddressSettings(e, key) {
        let value = e.target.value;
        let obj = {};
        const ProposalManagement_WebhookAddress = { ...this.state.ProposalManagement_WebhookAddress };
        if (ProposalManagement_WebhookAddress.hasOwnProperty(key) && value) {
            ProposalManagement_WebhookAddress[key] = value;
            obj[key] = value;
            this.setState({ ProposalManagement_WebhookAddress });
        }
    }
    renderStep_10() {
        const margin = { margin: '10px' };
        const bold = { 'fontWeight': 'bold' };

        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>stepbusinnesprocess</Trans></h4>
                <div>
                    <span>
                        <Trans>stepbusinessprocesslabel</Trans>
                        <Trans>stepbusinessprocesslabel1</Trans>
                    </span>
                </div>
                <br />
                <ImportDealType apiService={this.apiService}/>
                <br />
                <div>
                    <span>
                        <Trans>stepbusinessprocesslabel2</Trans>
                    </span>
                </div>
            </div>);
    }

    renderStep_9() {
        let margin = { margin: '10px' };
        let bold = { 'fontWeight': 'bold' };
        let placeholders = this.placeholderForProposalManager();
        let TextBoxViewList = Object.keys(this.state.DocumentIdActivator).map(key => {
            return (
                <TextField
                    key={key}
                    label={<Trans>{key}</Trans>}
                    onBlur={(e) => this.onBlurOnDocumentIdActivatorSettings(e, key)}
                    required={"true"}
                    disabled={this.state.isUpdateOpp}
                    placeholder={`eg : <${placeholders(key)}>`}
                    value={this.state.DocumentIdActivator[key] ? this.state.DocumentIdActivator[key] : ""}
                />
            );
        });

        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step9</Trans></h4>
                <span>
                    <Trans>step9Label</Trans>
                </span>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        {TextBoxViewList}
                    </div>
                </div>
                <div className='ms-Grid-col ms-sm2 ms-md4 ms-lg4'>
                    <PrimaryButton
                        style={margin}
                        className='pull-right' onClick={(e) => this.SetDocumentIdActivatorSetting_JsonKeys(this.state.DocumentIdActivator, "DocumentIdActivator")}
                        disabled={this.state.isUpdateOpp}
                    >{<Trans>Configure</Trans>}</PrimaryButton>
                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <span><Trans>step9Complete</Trans></span>
                    </div>
                </div>
                {this.state.documentid ? this.showSpinnerAndMessage() : null}
            </div>);
    }

    renderStep_8() {
        const margin = { margin: '10px' };
        const bold = { 'fontWeight': 'bold' };
        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step8</Trans></h4>
                <PrimaryButton
                    style={margin} disabled={this.state.isUpdateOpp}
                    className='pull-right'
                    onClick={(e) => this.downloadJsonObject()}
                >{<Trans>downlaod</Trans>}</PrimaryButton>
                <a id="downloadFile" />
                {this.state.finish ? this.showSpinnerAndMessage(true) : null}
            </div>);
    }

    renderStep_7() {
        let margin = { margin: '10px' };
        let bold = { 'fontWeight': 'bold' };
        let disabled = Object.keys(this.state.ProposalManagement_BI).every(key => this.state.ProposalManagement_BI[key]);
        let placeholders = this.placeholderForProposalManager();
        let TextBoxViewList = Object.keys(this.state.ProposalManagement_BI).map(key => {
            return (
                <TextField
                    key={key}
                    label={<Trans>{key}</Trans>}
                    onBlur={(e) => this.onBlurOnBISettings(e, key)}
                    required={"true"}
                    disabled={this.state.isUpdateOpp}
                    placeholder={`eg : <${placeholders(key)}>`}
                    value={this.state.ProposalManagement_BI[key] ? this.state.ProposalManagement_BI[key] : ""}
                />
            );
        });

        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step7</Trans></h4>
                <span>
                    <Trans>step7label</Trans>
                </span>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        {TextBoxViewList}
                    </div>
                </div>
                <div className='ms-Grid-col ms-sm2 ms-md4 ms-lg4'>
                    <PrimaryButton
                        style={margin}
                        className='pull-right' onClick={(e) => this.SetAppSetting_JsonKeys(this.state.ProposalManagement_BI, "powerbi")}
                        disabled={this.state.isUpdateOpp}
                    >{<Trans>Configure</Trans>}</PrimaryButton>
                </div>
                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                    <span><Trans>step7Complete</Trans></span>
                </div>
                {this.state.powerbi ? this.showSpinnerAndMessage(true) : null}
            </div>);
    }

    renderStep_6() {
        let margin = { margin: '10px' };
        let bold = { 'fontWeight': 'bold' };
        let placeholders = this.placeholderForProposalManager();
        let TextBoxViewList = Object.keys(this.state.ProposalManagement_bot).map(key => {
            if (key !== "BotServiceUrl")
                return (
                    <TextField
                        key={key}
                        label={<Trans>{key}</Trans>}
                        onBlur={(e) => this.onBlurOnBotSettings(e, key)}
                        required={"true"}
                        disabled={this.state.isUpdateOpp}
                        placeholder={`eg : <${placeholders(key)}>`}
                        value={this.state.ProposalManagement_bot[key] ? this.state.ProposalManagement_bot[key] : ""}
                    />
                );
        });

        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step6</Trans></h4>
                <span>
                    <Trans>step6label</Trans>
                </span>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        {TextBoxViewList}
                    </div>
                </div>
                <div className='ms-Grid-col ms-sm2 ms-md4 ms-lg4'>
                    <PrimaryButton
                        style={margin}
                        className='pull-right' onClick={(e) => this.SetAppSetting_JsonKeys(this.state.ProposalManagement_bot, "bot")}
                        disabled={this.state.isUpdateOpp}
                    >{<Trans>Configure</Trans>}</PrimaryButton>
                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <span><Trans>step6Complete</Trans></span>
                    </div>
                </div>
                {this.state.bot ? this.showSpinnerAndMessage() : null}
            </div>);
    }

    renderStep_5() {
        const margin = { margin: '10px' };
        const bold = { 'fontWeight': 'bold' };
        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step5</Trans></h4>
                <span><Trans>step5Note1</Trans></span>
                <span><Trans>step5note</Trans></span>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        <TextField
                            id='appKey'
                            label={<Trans>step5label</Trans>}
                            onBlur={(e) => this.onBlurSetPM(e, "ADGroupName")}
                            required={"true"}
                            disabled={this.state.isUpdateOpp}
                            placeholder={`eg : < Proposal Manager >`}
                        />
                    </div>
                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <span><Trans>Step5complete</Trans> <b><Trans>defaultAdGroupName</Trans></b>.</span>
                    </div>
                </div>
                <div className='ms-Grid-row '>
                    <PrimaryButton style={margin} className='pull left' disabled={this.state.isUpdateOpp}
                        onClick={(e) => this.CreateAdminPermissions()}
                    >{<Trans>Step5bttn</Trans>}</PrimaryButton>
                </div>
                {this.state.renderStep_3 ? this.showSpinnerAndMessage() : null}
            </div>);
    }

    renderAppPrerequisites() {
        const bold = { fontWeight: 'bold' };
        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold}><Trans>PMPrerequesties</Trans></h4>
                <I18n>
                    {
                        t => {
                            return (
                                <div>
                                    <h5>{t('prerequiste')}</h5>
                                    <hr className="prereqLine" />
                                    <ul>
                                        <li>{t('prereq1')}</li>
                                        <li>{t('prereq2')}</li>
                                    </ul>
                                    {t('prereq5')}
                                    <br /><br />
                                </div>
                            );
                        }
                    }
                </I18n>
            </div>
        );
    }

    render() {
        const disabledClass = {
            'pointerEvents': 'none',
            'opacity': 0.4
        };
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            return (
                <div >
                    <div className="ms-Grid-row">
                        <div className="ms-Grid-col ms-sm6 ms-md4 ms-lg12"><h3><Trans>setupPage</Trans></h3></div>
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderAppPrerequisites()}
                    </div>
                    <div style={this.state.ProposalManagement_Misc.SetupPage === "disabled" ? disabledClass : null}>
                        <div className='ms-Grid bg-white ibox-content'>
                            {this.renderStep_9()}
                        </div>
                        <div className='ms-Grid bg-white ibox-content'>
                            {this.renderStep_5()}
                        </div>
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_6()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_7()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_10()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_8()}
                    </div>
                </div>
            );
        }
    }
}