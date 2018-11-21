/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import {
    Pivot,
    PivotItem,
    PivotLinkFormat,
    PivotLinkSize
} from 'office-ui-fabric-react/lib/Pivot';
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

        this.authHelper = window.authHelper;
        this.sdkHelper = window.sdkHelper;
        this.utils = new Utils();
        this.accessGranted = false;
        //const userProfile = this.props.userProfile;

        try {
            //microsoftTeams.initialize();
        }
        catch (err) {
            console.log(err);
        }
        finally {
            const userProfile = { id: "", displayName: "", mail: "", phone: "", picture: "", userPrincipalName: "", roles: [] };
            let isMobile = false;
            // check is Mobile url
            if (window.location.pathname.indexOf("/tabMob/") === 0) {
                isMobile = true;
            }

            this.state = {
                userProfile: userProfile,
                teamName: "",
                groupId: "",
                haveGranularAccess: false,
                dashboardList: [],
                loading: true,
                viewState: "dashboard",
                industryList: [],
                regionList: [],
                categoryList: [],
                teamMembers: [],
                messageBarEnabled: false,
                isMobile: isMobile
            };
        }
        this.onClickCreateOpp = this.onClickCreateOpp.bind(this);
        this.onClickOppCancel = this.onClickOppCancel.bind(this);
        this.onClickOppBack = this.onClickOppBack.bind(this);
    }

    componentDidMount() {
        console.log("Dashboard_componentDidMount isauth: " + this.authHelper.isAuthenticated());
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            this.accessGranted = true;
            this.authHelper.callGetUserProfile()
                .then(userProfile => {
                    this.setState({
                        userProfile: userProfile,
                        loading: true
                    });
                });
            if (this.state.regionList.length === 0) {
                this.getRegions();
            }
            if (this.state.industryList.length === 0) {
                this.getIndustries();
            }
            if (this.state.categoryList.length === 0) {
                this.getCategories();
            }
            if (this.state.teamMembers.length === 0) {
                this.getUserProfiles();
            }
            if (this.state.dashboardList.length === 0) {
                this.getOpportunityIndex();
            }
        }
    }
    
    componentDidUpdate() {
        console.log("Dashboard_componentDidUpdate isauth: " + this.authHelper.isAuthenticated());
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            this.accessGranted = true;
            this.authHelper.callGetUserProfile()
                .then(userProfile => {
                    this.setState({
                        userProfile: userProfile,
                        loading: true
                    });
                });
            if (this.state.regionList.length === 0) {
                this.getRegions();
            }
            if (this.state.industryList.length === 0) {
                this.getIndustries();
            }
            if (this.state.categoryList.length === 0) {
                this.getCategories();
            }
            if (this.state.teamMembers.length === 0) {
                this.getUserProfiles();
            }
            if (this.state.dashboardList.length === 0) {
                this.getOpportunityIndex();
            }
        }
    }

    getOpportunityIndex() {
        return new Promise((resolve, reject) => {
            // To get the List of Opportunities to Display on Dashboard page
            let requestUrl = 'api/Opportunity?page=1';

            fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            })
                .then(response => {
                    if (response.ok) {
                        return response.json();
                    } else {
                        this.fetchResponseHandler(response, "getOpportunityIndex");
                        reject(response);
                    }
                })
                .then(data => {
                    let itemslist = [];
                    if (data.ItemsList.length > 0) {
                        for (let i = 0; i < data.ItemsList.length; i++) {

                            let item = data.ItemsList[i];

                            let newItem = {};

                            newItem.id = item.id;
                            newItem.opportunity = item.displayName;
                            newItem.client = item.customer.displayName;
                            newItem.dealsize = item.dealSize;
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
                        loading: false,
                        dashboardList: sortedList,
                        haveGranularAccess: true
                    });

                    resolve(true);
                })
                .catch(err => {
                    this.errorHandler(err, "getOpportunityIndex");
                    this.setState({
                        loading: false,
                        dashboardList: [],
                        haveGranularAccess: true
                    });
                    reject(err);
                });
        });
    }

    getRegions() {
        // call to API fetch Regions
        let requestUrl = 'api/Region';
        fetch(requestUrl, {
            method: "GET",
            headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
        })
            .then(response => response.json())
            .then(data => {
                try {
                    let regionList = [];
                    for (let i = 0; i < data.length; i++) {
                        let region = {};
                        region.key = data[i].id;
                        region.text = data[i].name;
                        regionList.push(region);
                    }
                    this.setState({ regionList: regionList });
                }
                catch (err) {
                    return false;
                }

            })
            .catch(err => {
                console.log("Opportunities_getRegions error: ");
                console.log(err);
                return false;
            });
    }

    getIndustries() {
        // call to API fetch Industry
        let requestUrl = 'api/Industry';
        fetch(requestUrl, {
            method: "GET",
            headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
        })
            .then(response => response.json())
            .then(data => {
                try {
                    let industryList = [];
                    for (let i = 0; i < data.length; i++) {
                        let industry = {};
                        industry.key = data[i].id;
                        industry.text = data[i].name;
                        industryList.push(industry);
                    }
                    this.setState({ industryList: industryList });
                }
                catch (err) {
                    return false;
                }

            })
            .catch(err => {
                console.log("Opportunities_getIndustries error: ");
                console.log(err);
                return false;
            });
    }

    getCategories() {
        // call to API fetch Categories
        let requestUrl = 'api/Category';
        fetch(requestUrl, {
            method: "GET",
            headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
        })
            .then(response => response.json())
            .then(data => {
                try {
                    let categoryList = [];
                    for (let i = 0; i < data.length; i++) {
                        let category = {};
                        category.key = data[i].id;
                        category.text = data[i].name;
                        categoryList.push(category);
                    }
                    this.setState({ categoryList: categoryList });
                }
                catch (err) {
                    return false;
                }

            })
            .catch(err => {
                console.log("Opportunities_getCategories error: ");
                console.log(err);
                return false;
            });
    }

    getUserProfiles() {
        let requestUrl = 'api/UserProfile/';
        fetch(requestUrl, {
            method: "GET",
            headers: {
                'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
            }
        })
            .then(response => {
                if (response.ok) {
                    return response.json();
                } else {
                    this.fetchResponseHandler(response, "getUserProfiles");
                    return [];
                }
            })
            .then(data => {
                let itemslist = [];

                if (data.ItemsList.length > 0) {
                    for (let i = 0; i < data.ItemsList.length; i++) {

                        let item = data.ItemsList[i];

                        let newItem = {};

                        newItem.id = item.id;
                        newItem.displayName = item.displayName;
                        newItem.mail = item.mail;
                        newItem.userPrincipalName = item.userPrincipalName;
                        newItem.userRoles = item.userRoles;

                        itemslist.push(newItem);
                    }
                }

                this.setState({
                    teamMembers: itemslist
                });
            })
            .catch(err => {
                console.log("Opportunities_getUserProfiles error: " + JSON.stringify(err));
            });
    }

    fetchResponseHandler(response, referenceCall) {
        if (response.status === 401) {
            // Handle refresh token
        }

    }

    errorHandler(err, referenceCall) {
        console.log("Opportunities Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    onClickCreateOpp() {
        this.newOpportunity = {
            id: "",
            displayName: "",
            customer: {
                id: "",
                displayName: "",
                referenceId: ""
            },
            dealSize: 0,
            annualRevenue: 0,
            openedDate: new Date(),
            industry: {
                id: "",
                name: ""
            },
            region: {
                id: "",
                name: ""
            },
            margin: 0,
            rate: 0,
            debtRatio: 0,
            purpose: "",
            disbursementSchedule: "",
            collateralAmount: 0,
            guarantees: "",
            riskRating: 0,
            teamMembers: [
                {
                    status: 0,
                    id: this.state.userProfile.id,
                    displayName: this.state.userProfile.displayName,
                    mail: this.state.userProfile.mail,
                    userPrincipalName: this.state.userProfile.userPrincipalName,
                    userRoles: this.state.userProfile.roles,
                    assignedRole: this.state.userProfile.roles.filter(x => x.displayName === "RelationshipManager")[0]
                }
            ],
            notes: [],
            documentAttachments: [],
            targetDate: ""
        };
        this.setState({
            viewState: "createStep1"
        });
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

            let requestUrl = 'api/opportunity/';

            let options = {
                method: "POST",
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'authorization': 'Bearer    ' + this.authHelper.getWebApiToken()
                },
                body: JSON.stringify(this.newOpportunity)
                //body: this.newOpportunity
            };

            fetch(requestUrl, options)
                .then(response => this.fetchResponseHandler(response, "createOpportunity"))
                .then(data => {
                    resolve(data);
                })
                .catch(err => {
                    this.setMessageBar(true, i18n.t('errorSavingOpportunityData'), MessageBarType.error);
                    reject(err);
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

                let requestUrl = 'api/document/UploadFile/' + encodeURIComponent(this.newOpportunity.displayName) + "/Attachment";

                let options = {
                    method: "PUT",
                    headers: {
                        'authorization': 'Bearer    ' + this.authHelper.getWebApiToken()
                    },
                    body: fd
                };

                fetch(requestUrl, options)
                    .then(response => this.fetchResponseHandler(response, "uploadFile"))
                    .then(data => {
                        resolve(data);
                    })
                    .catch(err => {
                        reject(false);
                    });
            }
        });
    }

    render() {
        const viewState = this.state.viewState;
        console.log("General ==>appSettings", this.props.appSettings);
        const DashboardView = ({ match }) => {
            return <Dashboard appSettings={this.props.appSettings} />;
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
                                                            userProfile={this.state.userProfile}
                                                            dashboardList={this.state.dashboardList}
                                                            onClickCreateOpp={this.onClickCreateOpp}
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
                                                userProfile={this.state.userProfile}
                                                opportunity={this.newOpportunity}
                                                regions={this.state.regionList}
                                                industries={this.state.industryList}
                                                dashboardList={this.state.dashboardList}
                                                onClickCancel={this.onClickOppCancel}
                                                onClickNext={this.onClickCreateOppNext.bind(this, this.newOpportunity)}
                                            />
                                        }

                                        {
                                            viewState === "createStep2" &&
                                            <NewOpportunityDocuments
                                                userProfile={this.state.userProfile}
                                                opportunity={this.newOpportunity}
                                                categories={this.state.categoryList}
                                                onClickBack={this.onClickOppBack}
                                                onClickNext={this.onClickCreateOppNext.bind(this, this.newOpportunity)}
                                            />
                                        }

                                        {
                                            viewState === "createStep3" &&
                                            <NewOpportunityOthers
                                                userProfile={this.state.userProfile}
                                                opportunity={this.newOpportunity}
                                                teamMembers={this.state.teamMembers}
                                                onClickBack={this.onClickOppBack}
                                                onClickNext={this.onClickCreateOppNext.bind(this, this.newOpportunity)}
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