/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';
import { oppStatusClassName } from '../../common';
import { Pivot, PivotItem, PivotLinkFormat, PivotLinkSize } from 'office-ui-fabric-react/lib/Pivot';
import '../teams.css';
import { Trans } from "react-i18next";
import { AdminArchivedOpportunities } from "./AdminArchivedOpportunities";
import { AdminActionRequired } from "./AdminActionRequired";
import { AdminAllOpportunities } from './AdminAllOpportunities';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import Utils from '../../helpers/Utils';

export class Administration extends Component {
    displayName = Administration.name

    constructor(props) {
        super(props);

        this.authHelper = window.authHelper;
        this.sdkHelper = window.sdkHelper;
        this.utils = new Utils();

        try {
            microsoftTeams.initialize();
            console.log('in try');

        }
        catch (err) {
            console.log(err);
        }
        finally {
            const userProfile = { id: "", displayName: "", mail: "", phone: "", picture: "", userPrincipalName: "", roles: [] };
            this.state = {
                userProfile: userProfile,
                teamName: "",
                channelId: "",
                groupId: "",
                errorLoading: false,
                teamMembers: [],
                oppIndexData: [],
                items: [],
                userRoleList: [],
                isAuthenticated: false,
                loading: true,
                isAdmin: false,
                haveGranularAccess: false
            };
        }
    }

    componentWillMount() {

        if (this.authHelper.isAuthenticated()) {

            this.authHelper.callGetUserProfile()
                .then(userProfile => {
                    this.acquireGraphAdminTokenSilent(userProfile); // Call acquire token so it is ready when calling graph using admin token
                    this.setState({
                        userProfile: userProfile

                    });
                });
        }

        this.authHelper.callCheckAccess(["Administrator"]).then((data) => {
            let haveGranularAccess = data;
            this.setState({ haveGranularAccess: haveGranularAccess });
        });

        if (this.state.items.length === 0) {
            console.log("Administration_componentWillMount getOpportunityIndex");
            this.getOpportunityIndex()
                .then(data => {
                    if (data) {
                        this.setState({
                            loading: false
                        });
                    }
                })
                .catch(err => {
                    // TODO: Add error message
                    this.errorHandler(err, "Administration_componentWillMount_getOpportunityIndex");
                });
        }

    }

    resetToken() {
        this.authHelper.logout().then(() => {
            window.location.reload();
        });
    }



    fetchResponseHandler(response, referenceCall) {
        if (response.status === 401) {
            //TODO: Handle refresh token in vNext;
        }
    }

    errorHandler(err, referenceCall) {
        console.log("Administration Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    acquireGraphAdminTokenSilent(userProfile) {
        if (this.utils.getQueryVariable("admin_consent")) {
            let isAdmin = userProfile.roles.filter(x => x.displayName === "Administrator");

            if (isAdmin) {
                this.authHelper.loginPopupGraphAdmin()
                    .then(access_token => {
                        // TODO: For future expansion sice the toke has been handled by authHelper
                    })
                    .catch(err => {
                        console.log(err);
                        this.errorHandler(err, "Administration_acquireGraphAdminTokenSilent");
                    });
            }
        } else {
            let isAdmin = userProfile.roles.filter(x => x.displayName === "Administrator");
            console.log("Administration_acquireGraphAdminTokenSilent getQueryVariable:adminconsent 01 : ", userProfile);
            console.log("Administration_acquireGraphAdminTokenSilent getQueryVariable:adminconsent 03:", isAdmin);
            if (isAdmin) {
                this.authHelper.acquireGraphAdminTokenSilent()
                    .then(access_token => {
                        // TODO: For future expansion sice the toke has been handled by authHelper
                    })
                    .catch(err => {
                        console.log(err);
                        this.errorHandler(err, "Administration_acquireGraphAdminTokenSilent");
                        console.log("Administration_acquireGraphAdminTokenSilent getQueryVariable:adminconsent 06:", err);
                        //this.showMessageBar("Error while requesting an admin token for Graph API, please try refreshing your browser and sign-in again.", MessageBarType.error);
                    });
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
                        this.fetchResponseHandler(response, "Administration_getOpportunityIndex");
                        reject(response);
                    }
                })
                .then(data => {
                    if (data.error && data.error.code.toLowerCase() === "badrequest") {
                        this.setState({
                            loading: false,
                            haveGranularAccess: false
                        });
                        resolve(true);
                    } else {
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
                                newItem.statusValue = item.opportunityState;
                                newItem.status = oppStatusClassName[item.opportunityState];
                                newItem.createTeamDisable = item.dealType !== null && item.dealType.id !== null ? false : true;
                                newItem.saved = true;
                                itemslist.push(newItem);
                            }
                        }

                        console.log("Administration_getOpportunityIndex ----");
                        console.log(itemslist);
                        if (itemslist.length > 0) {
                            this.setState({ reverseList: true });
                        }
                        let sortedList = this.state.reverseList ? itemslist.reverse() : itemslist;
                        this.setState({
                            items: sortedList,
                            loading: false

                        });

                        resolve(true);
                    }
                })
                .catch(err => {
                    this.errorHandler(err, "Administration_getOpportunityIndex");
                    this.setState({
                        loading: false,
                        items: []

                    });
                    reject(err);
                });
        });
    }

    getUserRoles() {
        // call to API fetch data
        return new Promise((resolve, reject) => {
            let requestUrl = 'api/RoleMapping';
            fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            })
                .then(response => response.json())
                .then(data => {
                    try {
                        let userRoleList = [];
                        //console.log(data);
                        for (let i = 0; i < data.length; i++) {
                            let userRole = {};
                            userRole.id = data[i].id;
                            userRole.adGroupName = data[i].adGroupName;
                            userRole.roleName = data[i].roleName;
                            userRole.processStep = data[i].processStep;
                            userRole.channel = data[i].channel;
                            userRole.adGroupId = data[i].adGroupId;
                            userRole.processType = data[i].processType;

                            userRoleList.push(userRole);
                        }
                        this.setState({ userRoleList: userRoleList });
                        console.log("Administration_getUserRoles userRoleList lenght: " + userRoleList.length);
                        resolve(true);
                    }
                    catch (err) {
                        reject(err);
                    }

                });
        });
    }



    render() {
        const team = this.state.teamMembers;
        const channelId = this.state.channelId;
        let isAdmin = this.state.userProfile.roles.filter(x => x.displayName === "Administrator");

        const AdminActionRequiredView = ({ match }) => {
            return (
                <AdminActionRequired
                    appSettings={this.props.appSettings}
                    items={this.state.items}
                    userRoleList={this.state.userRoleList}
                    userProfile={this.state.userProfile}
                />
            );
        };

        console.log("in render");
        console.log(this.state);

        return (

            //<TeamsComponentContext>
            <div className='ms-Grid'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg12 bgwhite tabviewUpdates' >
                        {this.state.haveGranularAccess
                            ?
                            this.state.loading
                                ?
                                <div className='ms-BasicSpinnersExample'>
                                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                </div>
                                :
                                <Pivot className='tabcontrols pt35' linkFormat={PivotLinkFormat.tabs} linkSize={PivotLinkSize.large}>
                                    <PivotItem linkText={<Trans>requiresAction</Trans>} width='100%' >
                                        <AdminActionRequiredView />
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>allOpportunities</Trans>}>
                                        <AdminAllOpportunities items={this.state.items} userRoleList={this.state.userRoleList} />
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>archivedOpportunities</Trans>}>
                                        <AdminArchivedOpportunities items={this.state.items} userRoleList={this.state.userRoleList} />
                                    </PivotItem>
                                </Pivot>


                            :
                            <div className="p-10"><h2><Trans>accessDenied</Trans></h2></div>
                        }
                    </div>
                </div>
            </div>
            //</TeamsComponentContext>
        );
    }

}