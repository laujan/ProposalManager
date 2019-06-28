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
import { AdminArchivedOpportunities } from "./Administration/AdminArchivedOpportunities";
import { AdminAllOpportunities } from './Administration/AdminAllOpportunities';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import Utils from '../../helpers/Utils';
import Accessdenied from '../../helpers/AccessDenied';
import { Audit } from './Audit';

export class Administration extends Component {
    displayName = Administration.name

    constructor(props) {
        super(props);

        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        this.authHelper = window.authHelper;
        this.utils = new Utils();
        this.accessGranted = false;

        const userProfile = this.props.userProfile;
        try {
            microsoftTeams.initialize();
        }
        catch (err) {
            this.logService.log(err);
        }
        finally {
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

    componentDidMount() {
        this.logService.log("Administration_componentDidMount");
        if (this.authHelper.isAuthenticated()) {

            this.authHelper.callGetUserProfile()
                .then(userProfile => {
                    this.setState({
                        userProfile: userProfile

                    });
                });
        }

        this.authHelper.callCheckAccess(["Administrator"])
            .then(async (data) => {
                let userRoleList, itemList = [];
                if (data) {
                    this.logService.log("Administration_componentDidMount getOpportunityIndex");
                    userRoleList = await this.getUserRoles();
                    itemList = await this.getOpportunityIndex();
                }

                this.setState({ haveGranularAccess: data, items: itemList, userRoleList: userRoleList, loading: false });
            })
            .catch(err => {
                this.errorHandler(err, "Administration_callCheckAccess");
                this.setState({ haveGranularAccess: false });
            });
    }

    errorHandler(err, referenceCall) {
        this.logService.log("Administration Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    async getOpportunityIndex()
    {
        let itemsList = [];
        try {
            let response = await this.apiService.callApi('Opportunity', 'GET', { query: 'page=1' });
            if (response.ok) {
                let data = await response.json();
                if (data.error && data.error.code.toLowerCase() === "badrequest") {
                    throw new Error(data.error);
                }
                else {
                    let list = [];
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
                            list.push(newItem);
                        }
                    }

                    this.logService.log("Administration_getOpportunityIndex ----", list);
                    itemsList = list.length > 0 ? list.reverse() : list;
                }
            }
            else {
                throw new Error(response.statusText);
            }
        } catch (err) {
            this.errorHandler(err, "Administration_getOpportunityIndex");
        }
        finally {
            return itemsList;
        }
    }

    async getUserRoles() {
        let userRoleList = [];
        try {
            let response = await this.apiService.callApi('Roles', 'GET');

            if (response.ok) {
                let data = await response.json();
                for (let i = 0; i < data.length; i++) {
                    let userRole = {};
                    userRole.id = data[i].id;
                    userRole.adGroupName = data[i].adGroupName;
                    userRole.displayName = data[i].displayName;
                    userRole.permissions = data[i].permissions;
                    userRole.teamsMembership = data[i].teamsMembership;
                    userRoleList.push(userRole);
                }
                this.logService.log("Administration_getUserRoles userRoleList length: " + userRoleList.length);
            }
            else {
                this.errorHandler(response.statusText, "Administration_getUserRoles");
            }
        }
        catch (err) {
            this.errorHandler(err, "Administration_getUserRoles");
        }
        finally {
            return userRoleList;
        }
    }

    render() {
        return (
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
                                    <PivotItem linkText={<Trans>allOpportunities</Trans>} itemKey="allOpportunities">
                                        <AdminAllOpportunities items={this.state.items} userRoleList={this.state.userRoleList} apiService={this.props.apiService} logService={this.props.logService} />
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>archivedOpportunities</Trans>} itemKey="archivedOpportunities">
                                        <AdminArchivedOpportunities items={this.state.items} userRoleList={this.state.userRoleList} apiService={this.props.apiService} logService={this.props.logService} />
                                    </PivotItem>
                                    {
                                        this.props.appSettings.auditEnabled ?
                                        <PivotItem linkText="Audit Logs" itemKey="audit">
                                            <Audit apiService={this.props.apiService} logService={this.props.logService} appSettings={this.props.appSettings} />
                                        </PivotItem>
                                        :
                                        <div />
                                    }
                                </Pivot>
                            :
                            <Accessdenied />
                        }
                    </div>
                </div>
            </div>
        );
    }
}