/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';
import { TeamsComponentContext, Panel, PanelBody, PanelFooter, PanelHeader } from 'msteams-ui-components-react';
import { DatePicker } from 'office-ui-fabric-react/lib/DatePicker';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { getQueryVariable } from '../common';
import { I18n, Trans } from "react-i18next";
import Accessdenied from '../helpers/AccessDenied';

let teamContext = {};
const DayPickerStrings = {
    months: [
        'January',
        'February',
        'March',
        'April',
        'May',
        'June',
        'July',
        'August',
        'September',
        'October',
        'November',
        'December'
    ],

    shortMonths: [
        'Jan',
        'Feb',
        'Mar',
        'Apr',
        'May',
        'Jun',
        'Jul',
        'Aug',
        'Sep',
        'Oct',
        'Nov',
        'Dec'
    ],

    days: [
        'Sunday',
        'Monday',
        'Tuesday',
        'Wednesday',
        'Thursday',
        'Friday',
        'Saturday'
    ],

    shortDays: [
        'S',
        'M',
        'T',
        'W',
        'T',
        'F',
        'S'
    ],

    goToToday: 'Go to today',
    prevMonthAriaLabel: 'Go to previous month',
    nextMonthAriaLabel: 'Go to next month',
    prevYearAriaLabel: 'Go to previous year',
    nextYearAriaLabel: 'Go to next year'
};

export class CustomerDecision extends Component {
    displayName = CustomerDecision.name
    constructor(props) {
        super(props);

        this.authHelper = window.authHelper;
        this.sdkHelper = window.sdkHelper;
        this.accessGranted = false;

        this.onChangedTxtApprovedDate = this.onChangedTxtApprovedDate.bind(this);
        this.onChangedTxtLoadDisbursed = this.onChangedTxtLoadDisbursed.bind(this);
        this.fnDdlCustomerApproved = this.fnDdlCustomerApproved.bind(this);

        this.state = {
            CustomerDecision: {},
            LoadDisbursed: "",
            ApprovedDate: "",
            ApprovedStatus: false,
            loading: true,
            oppData: [],
            isUpdate: false,
            MessagebarText: "",
            oppStatusAll: [],
            haveGranularAccess: false
        };

        this.onStatusChange = this.onStatusChange.bind(this);

    }
     
    componentWillMount() {
        console.log("CustomerDecision_componentWillMount isauth: " + this.authHelper.isAuthenticated());
    }

    componentDidMount() {
        console.log("CustomerDecision_componentDidMount isauth: " + this.authHelper.isAuthenticated());
        if (!this.state.isAuthenticated) {
            this.authHelper.callGetUserProfile()
                .then(userProfile => {
                    this.setState({
                        userProfile: userProfile,
                        loading: true
                    });
                });
        }
    }


    componentDidUpdate() {
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            console.log("CustomerDecision_componentDidUpdate callCheckAccess");
            this.accessGranted = true;
            this.getOppStatusAll();
            let teamName = getQueryVariable('teamName');
            this.fnGetOpportunityData(teamName);
        }
    }

    initialize({ groupId, channelName, teamName }) {

        let tc = {
            group: groupId,
            channel: channelName,
            team: teamName
        };
        teamContext = tc;

        this.fnGetOpportunityData(teamName);
    }

    getOppStatusAll() {
        let requestUrl = 'api/context/GetOpportunityStatusAll';

        fetch(requestUrl, {
            method: "GET",
            headers: { 'authorization': 'Bearer ' + window.authHelper.getWebApiToken() }
        })
            .then(response => response.json())
            .then(data => {
                try {

                    if (this.state.oppData.opportunityState !== 11) // if the current state is not archived, remove the archive option from the array
                    {
                        var filteredData = data.filter(x => x.Name !== 'Archived');
                    }

                    let oppStatusList = [];
                    for (let i = 0; i < filteredData.length; i++) {
                        let oppStatus = {};
                        oppStatus.key = data[i].Value;
                        oppStatus.text = data[i].Name;
                        oppStatusList.push(oppStatus);
                    }
                    this.setState({
                        oppStatusAll: oppStatusList
                    });
                }
                catch (err) {
                    console.log(err);
                }
            });
    }

    fnGetOpportunityData(teamName) {
        return new Promise((resolve, reject) => {
            // API - Fetch call
            //let requestUrl = "api/Opportunity?name='" + teamName + "'";
            //changing to template string
            this.requestUrl = `api/Opportunity?name=${teamName}`;
            fetch(this.requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + window.authHelper.getWebApiToken() }

            })
                .then(response => response.json())
                .then(data => {
                    // If badrequest - user Access Denied 
                    if (data.error && data.error.code.toLowerCase() === "badrequest") {
                        this.setState({
                            loading: false,
                            haveGranularAccess: false
                        });
                        resolve(true);
                    } else {
                        // Start Check Access
                        let permissionRequired = ["Opportunity_ReadWrite_All", "Opportunities_ReadWrite_All", "Administrator"];
                        this.authHelper.callCheckAccess(permissionRequired).then(checkAccess => {
                            if (checkAccess) {
                                let customerDesionObj = data.customerDecision;
                                this.setState({
                                    loading: false,
                                    CustomerDecision: customerDesionObj,
                                    CustomerDecisionId: customerDesionObj.id,
                                    LoadDisbursed: new Date(customerDesionObj.loanDisbursed),
                                    ApprovedDate: new Date(customerDesionObj.approvedDate),
                                    ApprovedStatus: customerDesionObj.approved,
                                    oppData: data,
                                    isUpdate: false,
                                    haveGranularAccess: true
                                });

                                resolve(true);
                            }
                            else {
                                this.setState({
                                    haveGranularAccess: false,
                                    loading: false
                                });
                                resolve(true);
                            }
                        })
                            .catch(err => {
                                //this.errorHandler(err, "CustomerDecision_checkUserAccess");
                                this.setState({
                                    loading: false,
                                    haveGranularAccess: false
                                });
                                //this.hideMessagebar();
                                reject(err);
                            });
                        // End Check Access
                        
                    }

                })
                .catch(function (err) {
                    this.setState({
                        loading: false,
                        haveGranularAccess: false
                    });
                    console.log("Error: OpportunityGetByName--");
                    reject(err);
                });
        });
    }

    onStatusChange = (event) => {

        let oppDetails = this.state.oppData;
        oppDetails.opportunityState = event.key;

        this.fnUpdateCustDecision(oppDetails, true);

    }

    fnUpdateCustDecision(obj, flagOppObj) {
        this.setState({ isUpdate: true, MessagebarText: <Trans>updating</Trans> });

        let oppViewData = this.state.oppData;
        if (flagOppObj) {
            oppViewData = obj;
        }
        else {
            oppViewData.customerDecision = obj;
        }

        // API Update call        
        this.requestUpdUrl = 'api/opportunity?id=' + oppViewData.id;
        let options = {
            method: "PATCH",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
            },
            body: JSON.stringify(oppViewData)
            //id: this.props.match.params.id
        };

        fetch(this.requestUpdUrl, options)
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    return response.json;
                } else {
                    console.log('Error...: ');
                }
            }).then(json => {
                this.setState({ MessagebarText: <Trans>updatedSuccessfully</Trans> });
                // this.setState({ isUpdate: false, MessagebarText: "" });
                setTimeout(function () { this.setState({ isUpdate: false, MessagebarText: "" }); }.bind(this), 3000);
            });


    }

    onChangedTxtApprovedDate(event) {
        this.setState(Object.assign({}, this.state, { txtApprovedDate: event.target.value }));
    }

    onChangedTxtLoadDisbursed(event) {
        this.setState(Object.assign({}, this.state, { txtLoadDisbursed: event.target.value }));
    }

    fnDdlCustomerApproved = (event) => {
        this.setState({ ApprovedStatus: event.key });
        let custDecisionObj = {
            "id": this.state.CustomerDecisionId,
            "approved": event.key,
            "approvedDate": this.state.ApprovedDate,
            "loanDisbursed": this.state.LoadDisbursed
        };
        this.fnUpdateCustDecision(custDecisionObj, false);
    }

    _onSelectApproved = (date) => {
        this.setState({ ApprovedDate: date });
        let custDecisionObj = {
            "id": this.state.CustomerDecisionId,
            "approved": this.state.ApprovedStatus,
            "approvedDate": date,
            "loanDisbursed": this.state.LoadDisbursed
        };
        this.fnUpdateCustDecision(custDecisionObj, false);
    }


    _onSelectLoanDisbursed = (date) => {
        this.setState({ LoadDisbursed: date });
        let custDecisionObj = {
            "id": this.state.CustomerDecisionId,
            "approved": this.state.ApprovedStatus,
            "approvedDate": this.state.ApprovedDate,
            "loanDisbursed": date
        };
        this.fnUpdateCustDecision(custDecisionObj, false);
    }

    _onFormatDate = (date) => {
        return (
            date.getMonth() + 1 +
            '/' +
            date.getDate() +
            '/' +
            date.getFullYear()
        );
    }

    _onParseDateFromString = (value) => {
        const date = this.state.value || new Date();
        const values = (value || '').trim().split('/');
        const day =
            values.length > 0
                ? Math.max(1, Math.min(31, parseInt(values[0], 10)))
                : date.getDate();
        const month =
            values.length > 1
                ? Math.max(1, Math.min(12, parseInt(values[1], 10))) - 1
                : date.getMonth();
        let year = values.length > 2 ? parseInt(values[2], 10) : date.getFullYear();
        if (year < 100) {
            year += date.getFullYear() - date.getFullYear() % 100;
        }
        return new Date(year, month, day);
    }

    _setItemDate(dt) {
        let lmDate = new Date(dt);
        if (lmDate.getFullYear() === 1 || lmDate.getFullYear() === 0) {
            return new Date();
        } else return new Date(dt);
    }


    renderContent(customerObj, isUpdate) {
        return (
            <div className='ms-Grid-row'>
                <div className='docs-TextFieldExample ms-Grid-col ms-sm6 ms-md8 ms-lg3'>
                    <Dropdown
                        label={<Trans>customerApproved</Trans>}
                        selectedKey={this.state.ApprovedStatus}
                        onChanged={this.fnDdlCustomerApproved}
                        options={
                            [
                                { key: true, text: "Yes" },
                                { key: false, text: "No" }
                            ]
                        }
                    />
                </div>
                <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg3 docs-TextFieldExample'>
                    <DatePicker strings={DayPickerStrings}
                        showWeekNumbers={false}
                        firstWeekOfYear={1}
                        showMonthPickerAsOverlay='true'
                        label={<Trans>approvedDate</Trans>}
                        placeholder={<Trans>approvedDate</Trans>}
                        iconProps={{ iconName: 'Calendar' }}
                        value={this._setItemDate(this.state.ApprovedDate)}
                        onSelectDate={this._onSelectApproved}
                        formatDate={this._onFormatDate}
                        parseDateFromString={this._onParseDateFromString}
                    />
                </div>
                <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg3 docs-TextFieldExample'>
                    <DatePicker strings={DayPickerStrings}
                        showWeekNumbers={false}
                        firstWeekOfYear={1}
                        showMonthPickerAsOverlay='true'
                        label={<Trans>loanDisbursed</Trans>}
                        placeholder={<Trans>loanDisbursed</Trans>}
                        iconProps={{ iconName: 'Calendar' }}
                        value={this._setItemDate(this.state.LoadDisbursed)}
                        onSelectDate={this._onSelectLoanDisbursed}
                        formatDate={this._onFormatDate}
                        parseDateFromString={this._onParseDateFromString}
                    />
                </div>
                <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg3 pb10'>

                    <I18n>
                        {
                            t => {
                                return (
                                    <Dropdown
                                        label={t('status')}
                                        onChanged={(e) => this.onStatusChange(e)}
                                        id='statusDropdown'
                                        disabled={this.state.oppData.opportunityState === 1 || this.state.oppData.opportunityState === 3 || this.state.oppData.opportunityState === 5 ? true : false}
                                        options={this.state.oppStatusAll}
                                        defaultSelectedKey={this.state.oppData.opportunityState}
                                    />
                                );
                            }

                        }
                    </I18n>

                </div>
                <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg2 hide'>
                    {
                        this.state.isUpdate ?
                            <Spinner size={SpinnerSize.large} label='' ariaLive='assertive' className="pt15 pull-center" />
                            : ""
                    }
                </div>
            </div>
        );
    }

    render() {
        let isUpdate = this.state.isUpdate;

        let content = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderContent(this.state.CustomerDecision, isUpdate);

        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample pull-center'>
                    <Spinner size={SpinnerSize.medium} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            return (
                <div>
                    <TeamsComponentContext>
                        {
                            this.state.haveGranularAccess
                                ?
                                <Panel>
                                    <PanelHeader>
                                        <h3 className="pl10"><Trans>customerDecision</Trans></h3>
                                    </PanelHeader>
                                    <PanelBody>
                                        <div className='ms-Grid'>
                                            <div className='ms-Grid-row'>
                                                <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg12 hide'>

                                                    {
                                                        isUpdate ?
                                                            <Spinner size={SpinnerSize.large} label='' ariaLive='assertive' className="pt15 pull-center" />
                                                            : ""
                                                    }
                                                </div>
                                            </div>
                                        </div>

                                        <div className='ms-Grid'>
                                            {content}
                                        </div>
                                        <br /><br /><br /><br />

                                    </PanelBody>
                                    <PanelFooter>
                                        <div className='ms-Grid'>
                                            <div className='ms-Grid-row'>
                                                <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg8' />
                                                <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg4'>
                                                    {this.state.isUpdate ?
                                                        <MessageBar
                                                            messageBarType={MessageBarType.success}
                                                            isMultiline={false}
                                                        >
                                                            {this.state.MessagebarText}
                                                        </MessageBar>
                                                        : ""
                                                    }

                                                </div>
                                            </div>
                                        </div>


                                    </PanelFooter>
                                </Panel>
                                :
                                <Accessdenied />
                        }



                    </TeamsComponentContext>


                </div>
            );
        }
    }
}