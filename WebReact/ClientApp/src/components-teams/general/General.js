/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import { Pivot, PivotItem, PivotLinkFormat, PivotLinkSize } from 'office-ui-fabric-react/lib/Pivot';
import { Trans } from "react-i18next";
import Utils from '../../helpers/Utils';
import { OpportunityList } from './Opportunity/OpportunityList';
import { Dashboard } from './Dashboard';
import { oppStatusClassName } from '../../common';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { NewOpportunity } from './Opportunity/NewOpportunity';
import { NewOpportunityDocuments } from './Opportunity/NewOpportunityDocuments';
import { NewOpportunityOthers } from './Opportunity/NewOpportunityOthers';
import i18n from '../../i18n';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import '../../Style.css';
import AccessDenied from '../../helpers/AccessDenied';

export class General extends Component {
    displayName = General.name

    constructor(props) {
        super(props);

        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        this.authHelper = window.authHelper;
        this.utils = new Utils();
        this.accessGranted = false;
        try {
            this.logService.log("General_Constructor");
        }
        catch (err) {
            this.logService.log("General_Constructor: error => ", err.message);
        }
        finally {
            // Wave4 - adding permission key for userprofiles
            const userProfile = { id: "", displayName: "", mail: "", phone: "", picture: "", userPrincipalName: "", roles: [], permissions: "" };
            let isMobile = false;
            if (window.location.pathname.indexOf("/tabMob/") === 0) {
                isMobile = true;
            }

            this.state = {
                userProfile: userProfile,
                haveGranularAccess: false,
                dashboardList: [],
                loading: true,
                viewState: "dashboard",
                categoryList: [],
                teamMembers: [],
                messageBarEnabled: false,
                isMobile: isMobile,
                templateItems: [],
                templateList: [],
                templateLoading: true,
                metaDataList: []
            };
        }
        this.onClickCreateOpp = this.onClickCreateOpp.bind(this);
        this.onClickOppCancel = this.onClickOppCancel.bind(this);
        this.onClickOppBack = this.onClickOppBack.bind(this);
    }

    async componentDidMount() {
        this.logService.log("Dashboard_componentDidMount");
        await this.getAllData();
    }

    async getAllData() {
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            this.accessGranted = true;
            let userProfile = await this.authHelper.callGetUserProfile();

            this.setState({
                userProfile: userProfile,
                loading: true
            });

            if (this.state.metaDataList.length === 0) {
                await this.getMetaData();
            }
            if (this.state.teamMembers.length === 0) {
                await this.getUserProfiles();
            }
            if (this.state.dashboardList.length === 0) {
                await this.getOpportunityIndex();
            }
            if (this.state.templateList.length === 0) {
                await this.getTemplatesLists();
            }
        }
    }

    async getOpportunityIndex() {
        //Opportunity list
        this.apiService.callApi('Opportunity', 'GET', { query: 'page=1' })
            .then(async (response) => {
                if (response.ok) {
                    let data = await response.json();
                    let itemslist = [];

                    if (data.ItemsList.length > 0) {
                        for (let i = 0; i < data.ItemsList.length; i++) {

                            let item = data.ItemsList[i];
                            this.logService.log("General_getOpportunityIndex item : ", item);
                            let newItem = {};

                            newItem.id = item.id;
                            newItem.opportunity = item.displayName;
                            newItem.client = item.customer.displayName;
                            newItem.dealsize = item.dealsize;
                            newItem.openedDate = new Date(item.openedDate).toLocaleDateString();
                            newItem.stausValue = item.opportunityState;
                            newItem.status = oppStatusClassName[item.opportunityState];
                            itemslist.push(newItem);
                        }
                    }
                    if (itemslist.length > 0) {
                        this.setState({ reverseList: true });
                    }

                    let sortedList = this.state.reverseList ? itemslist.reverse() : itemslist;
                    this.setState({
                        dashboardList: sortedList
                    });
                }
            })
            .catch(error => {
                this.errorHandler(error, "Opportunities_getOpportunityIndex");
            })
            .finally(() => {
                this.setState({
                    loading: false,
                    haveGranularAccess: true
                });
            });
    }

    async getMetaData() {
        this.apiService.callApi('MetaData', 'GET')
            .then(async (response) => {
                if (response.ok) {
                    let data = await response.json();
                    let categoryList = [];
                    let metaDataList = [];

                    for (let i = 0; i < data.length; i++) {
                        metaDataList.push(data[i]);
                    }
                    let temp = metaDataList.find(x => x.displayName === "Category" && x.fieldType.name === "DropDown").values;
                    categoryList = temp.map(x => { return { 'key': x.id, 'text': x.name }; });
                    this.setState({ metaDataList, categoryList });
                }
            })
            .catch(error => {
                this.errorHandler(error, "Opportunities_CreateNew_getCategories");
            });
    }

    async getUserProfiles() {
        this.apiService.callApi('UserProfile', 'GET')
            .then(async (response) => {
                if (response.ok) {
                    let data = await response.json();
                    let teamMembers = [];

                    if (data.ItemsList.length > 0) {
                        teamMembers = data.ItemsList;
                    }
                    this.logService.log("General_getUserProfiles : ", teamMembers);

                    this.setState({ teamMembers });
                }
            })
            .catch(error => {
                this.setState({ teamMembers: [] });
                this.errorHandler(error, "Opportunities_CreateNew_getUserProfiles");
            });
    }

    // Wave4 - Get all templates
    async getTemplatesLists() {
        this.apiService.callApi('template', 'GET')
            .then(async (response) => {
                let data = await response.json();
                let templateItemsList = [];
                let templateList = [];

                for (let i = 0; i < data.itemsList.length; i++) {
                    templateItemsList.push(data.itemsList[i]);
                    let template = {};
                    template.key = data.itemsList[i].id;
                    template.text = data.itemsList[i].templateName;
                    template.defaultTemplate = data.itemsList[i].defaultTemplate;
                    templateList.push(template);
                }

                this.setState({
                    templateItems: templateItemsList,
                    templateList: templateList,
                    templateLoading: false
                });
            })
            .catch(error => {
                this.logService.log("OpportunitySummary_getDealTypeLists error ", error);
            });
    }

    fetchResponseHandler(response, referenceCall) {
        if (response.status === 401) {
            // Handle refresh token
        }
    }

    errorHandler(err, referenceCall) {
        this.logService.log("Opportunities Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    async onClickCreateOpp() {
        try {
            let res = await this.authHelper.callCheckAccess(["Opportunity_Create"]);

            let role = this.state.userProfile.roles.find(role => {
                if (role.permissions.find(permission => permission.name === "Opportunity_Create"))
                    return role.id;
            });

            let metaData = [];
            this.state.metaDataList.forEach((metaobj) => {
                if (metaobj.screen === "Screen1" || metaobj.screen === "Screen2" || metaobj.screen === "Screen3") {
                    metaData.push({
                        id: metaobj.displayName.toLowerCase().replace(/\s/g, ''),
                        displayName: metaobj.displayName,
                        values: "",
                        screen: metaobj.screen,
                        fieldType: metaobj.fieldType
                    });
                }
            });

            if (res) {
                this.newOpportunity = {
                    id: "",
                    displayName: "",
                    customer: {
                        id: "",
                        displayName: "",
                        referenceId: ""
                    },
                    metaDataFields: metaData,
                    teamMembers: [{
                        status: 0,
                        id: this.state.userProfile.id,
                        displayName: this.state.userProfile.displayName,
                        mail: this.state.userProfile.mail,
                        userPrincipalName: this.state.userProfile.mail,
                        roleId: role ? role.id : "",
                        permissions: role ? role.permissions : [],
                        teamsMembership: role ? role.teamsMembership : [],
                        roleName: role ? role.displayName : ""
                    }],
                    notes: [],
                    documentAttachments: [],
                    template: {
                        id: "",
                        templateName: ""
                    }
                };
                this.logService.log("General_onClickCreateOpp : ", this.newOpportunity);
                this.setState({
                    viewState: "createStep1"
                });
            }

        } catch (err) {
            this.logService.log(err);
        }
    }

    onClickOppCancel() {
        this.setState({
            viewState: "dashboard"
        });
    }

    onClickOppBack() {
        if (this.state.viewState === "createStep1") {
            this.setState({
                viewState: "dashboard"
            });

        } else if (this.state.viewState === "createStep2") {
            this.setState({
                viewState: "createStep1"
            });

        } else if (this.state.viewState === "createStep3") {
            this.setState({
                viewState: "createStep2"
            });

        } else {
            this.setState({
                viewState: "dashboard"
            });
        }
    }

    onClickCreateOppNext() {
        this.logService.log("General_onClickCreateOppNext : ", this.newOpportunity);

        if (this.state.viewState === "createStep1") {
            this.setState({
                viewState: "createStep2"
            });

        } else if (this.state.viewState === "createStep2") {
            this.setState({
                viewState: "createStep3"
            });

        } else if (this.state.viewState === "createStep3") {
            this.setState({
                viewState: "dashboard"
            });

            // Save data
            this.setMessageBar(true, i18n.t('savingOpportunityData'), MessageBarType.info);
            this.createOpportunity()
                .then(res => {
                    this.setMessageBar(true, i18n.t('uploadingFiles'), MessageBarType.info);
                    this.uploadFiles()
                        .then(res => {
                            this.setMessageBar(false, "", MessageBarType.info);
                            this.setState({
                                loading: true
                            });
                            this.getOpportunityIndex()
                                .then(data => {
                                    this.setMessageBar(false, "", MessageBarType.info);
                                })
                                .catch(err => {
                                    this.setMessageBar(false, "", MessageBarType.info);
                                    this.errorHandler(err, "Opportunities_onClickCreateOppNext_getOpportunityIndex");
                                });
                        })
                        .catch(err => {
                            this.setMessageBar(false, "", MessageBarType.info); // TODO: Set error message with timer
                            this.errorHandler(err, "Opportunities_onClickCreateOppNext_uploadFiles");
                            this.setState({
                                loading: true
                            });
                            this.getOpportunityIndex()
                                .then(data => {
                                    this.setMessageBar(false, "", MessageBarType.info);
                                })
                                .catch(err => {
                                    this.errorHandler(err, "Opportunities_onClickCreateOppNext_getOpportunityIndex");
                                });
                        });
                })
                .catch(err => {
                    this.errorHandler(err, "Opportunities_onClickCreateOppNext_createOpportunity");
                });

        } else {
            this.setState({
                viewState: "dashboard"
            });
        }
    }

    setMessageBar(enabled, text, type) {
        this.setState({
            messageBarEnabled: enabled,
            messageBarText: text,
            messageBarType: type
        });
    }

    // Create New Opportunity
    createOpportunity() {
        return new Promise((resolve, reject) => {
            // Clean attachments prior to submit then put them back so upload has the actual file to upload
            let currentAttchments = [];
            this.filesToUpload = currentAttchments.concat(this.newOpportunity.documentAttachments);
            let cleanAttachments = [];
            for (let i = 0; i < this.filesToUpload.length; i++) {
                cleanAttachments.push({
                    id: this.filesToUpload[i].id,
                    fileName: this.filesToUpload[i].file.name,
                    note: this.filesToUpload[i].note,
                    category: {
                        id: this.filesToUpload[i].category.id,
                        displayName: this.filesToUpload[i].category.name
                    },
                    tags: this.filesToUpload[i].tags,
                    documentUri: ""
                });
            }
            this.newOpportunity.documentAttachments = cleanAttachments;

            this.logService.log("General_createopportunity metadata: ", this.newOpportunity.metaDataFields);
            //adding default bussinees process
            if (this.newOpportunity.template.id.length === 0) {
                let defaultTemplateAvailable = this.state.templateList.some(name => name.defaultTemplate);
                if (defaultTemplateAvailable) {
                    this.newOpportunity.template = this.state.templateItems.filter(name => name.defaultTemplate)[0];
                }
            }

            this.apiService.callApi('Opportunity', 'POST', { body: JSON.stringify(this.newOpportunity) })
                .then((response) => {
                    this.fetchResponseHandler(response, "createOpportunity");
                    resolve(response);
                })
                .catch(error => {
                    this.setMessageBar(true, i18n.t('errorSavingOpportunityData'), MessageBarType.error);
                    reject(error);
                });
        });
    }

    // Upload files
    uploadFiles() {
        return new Promise((resolve, reject) => {
            let files = this.filesToUpload;
            for (let i = 0; i < files.length; i++) {
                this.setMessageBar(true, i18n.t('uploadingFiles') + (i + 1) + "/" + this.filesToUpload.length, MessageBarType.info);
                let fd = new FormData();
                fd.append('opportunity', "NewOpportunity");
                fd.append('file', files[i].file);
                fd.append('opportunityName', this.newOpportunity.displayName);
                fd.append('fileName', files[i].file.name);

                this.apiService.callApi(`Document/UploadFile/${encodeURIComponent(this.newOpportunity.displayName)}/Attachment`, 'PUT', { formData: fd })
                    .then((response) => {
                        this.fetchResponseHandler(response, "uploadFile");
                        resolve(response);
                    }).catch(error => {
                        reject(error);
                    });
            }
        });
    }

    render() {
        const viewState = this.state.viewState;
        this.logService.log("General: appSettings: ", this.props.appSettings);
        const DashboardView = () => {
            return <Dashboard appSettings={this.props.appSettings} apiService={this.props.apiService} logService={this.logService} />;
        };

        return (
            <div className='ms-Grid'>
                <div className='ms-Grid-row bg-white p-10'>
                    <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg12  tabviewUpdates' >
                        {
                            this.state.loading ?
                                <div>
                                    <br /><br />
                                    <Spinner size={SpinnerSize.medium} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                    <br /><br />
                                </div>
                                :
                                this.state.haveGranularAccess
                                    ?
                                    <div>
                                        {
                                            this.state.messageBarEnabled ?
                                                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                                                    <MessageBar messageBarType={this.state.messageBarType} isMultiline={false}>
                                                        {this.state.messageBarText}
                                                    </MessageBar>
                                                </div>
                                                : ""
                                        }
                                        {
                                            viewState === "dashboard" &&
                                            <Pivot className='tabcontrols pt35' linkFormat={PivotLinkFormat.tabs} linkSize={PivotLinkSize.large} selectedKey={this.state.selectedTabName}>
                                                <PivotItem linkText={<Trans>opportunities</Trans>} width='100%' itemKey="opportunitylist" >
                                                    {
                                                        viewState === "dashboard" &&
                                                        <OpportunityList
                                                            apiService={this.props.apiService}
                                                            userProfile={this.state.userProfile}
                                                            dashboardList={this.state.dashboardList}
                                                            onClickCreateOpp={this.onClickCreateOpp}
                                                            metaDataList={this.state.metaDataList}
                                                            logService={this.logService} 
                                                        />
                                                    }
                                                </PivotItem>
                                                {
                                                    this.state.isMobile
                                                        ? ""
                                                        :
                                                        <PivotItem linkText={<Trans>dashboard</Trans>} itemKey="dashboard">
                                                            <DashboardView />
                                                        </PivotItem>
                                                }
                                            </Pivot>
                                        }
                                        {
                                            viewState === "createStep1" &&
                                            <NewOpportunity
                                                opportunity={this.newOpportunity}
                                                dashboardList={this.state.dashboardList}
                                                onClickCancel={this.onClickOppCancel}
                                                onClickNext={this.onClickCreateOppNext.bind(this, this.newOpportunity)}
                                                metaDataList={this.state.metaDataList}
                                                logService={this.logService} 
                                            />
                                        }
                                        {
                                            viewState === "createStep2" &&
                                            <NewOpportunityDocuments
                                                opportunity={this.newOpportunity}
                                                categories={this.state.categoryList}
                                                onClickBack={this.onClickOppBack}
                                                onClickNext={this.onClickCreateOppNext.bind(this, this.newOpportunity)}
                                                metaDataList={this.state.metaDataList}
                                                logService={this.logService} 
                                            />
                                        }
                                        {
                                            viewState === "createStep3" &&
                                            <NewOpportunityOthers
                                                userProfile={this.state.userProfile}
                                                opportunity={this.newOpportunity}
                                                teamMembers={this.state.teamMembers}
                                                templateItems={this.state.templateItems}
                                                templateList={this.state.templateList}
                                                onClickBack={this.onClickOppBack}
                                                onClickNext={this.onClickCreateOppNext.bind(this, this.newOpportunity)}
                                                metaDataList={this.state.metaDataList}
                                                apiService={this.props.apiService}
                                                logService={this.logService} 
                                            />
                                        }
                                    </div>
                                    :
                                    <AccessDenied />
                        }
                    </div>
                </div>
            </div>
        );
    }
}