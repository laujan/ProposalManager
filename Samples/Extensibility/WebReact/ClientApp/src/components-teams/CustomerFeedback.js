/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';

import { IconButton, PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { DatePicker } from 'office-ui-fabric-react/lib/DatePicker';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { TeamsComponentContext, ConnectedComponent, ThemeStyle, Panel, PanelHeader, PanelFooter, PanelBody } from 'msteams-ui-components-react';
import { Anchor } from 'msteams-ui-components-react';
import { getQueryVariable } from '../common';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import './checklist.css';
import Utils from '../helpers/Utils';
import { I18n, Trans } from "react-i18next";
import AccessDenied from '../helpers/AccessDenied';
//Granular Access Start
import AuthHelper from '../helpers/AuthHelper';
//Granular Access end

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

export class CustomerFeedback extends Component {
    displayName = CustomerFeedback.name

    constructor(props) {
        super(props);

        this.authHelper = window.authHelper;
        this.sdkHelper = window.sdkHelper;
        this.accessGranted = false;
        //Granular Access Start
        if (window.authHelper) {
            this.authHelper = window.authHelper;
        } else {
            // Initilize the AuthService and save it in the window object.
            this.authHelper = new AuthHelper();
            window.authHelper = this.authHelper;
        }
        //Granular Access End

        this.utils = new Utils();

        let tmpCustomerFeedback = {
            id: "",
            customerFeedbackChannel: ""
        };

        let tmpItems = [
            {
                key: 1,
                id: this.utils.guid(),
                feedbackDate: "",
                feedbackContactMeans: "",
                feedbackSummary: "",
                feedbackDetails: ""
            }
        ];

        this.hidePending = false;

        const columns = [
            {
                key: 'column1',
                name: 'Date',
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'feedbackName',
                minWidth: 150,
                maxWidth: 350,
                isRowHeader: false,
                isResizable: false,
                onRender: (item, index) => {
                    return (
                        <DatePicker strings={DayPickerStrings}
                            showWeekNumbers={false}
                            firstWeekOfYear={1}
                            showMonthPickerAsOverlay='true'
                            iconProps={{ iconName: 'Calendar' }}
                            value={this._setItemDate(this.state.items[index].feedbackDate) }
                            onSelectDate={(date) => this.onSelectFeedbackDate(date, index)}
                            formatDate={this._onFormatDate}
                            parseDateFromString={this._onParseDateFromString}
                            id={'dtpFeedbackDate' + item.id}
                        />
                    );
                }
            },
            {
                key: 'column2',
                name: 'Contact means',
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'feedbackContactMeans',
                minWidth: 150,
                maxWidth: 350,
                isRowHeader: false,
                isResizable: true,
                isCollapsable: true,
                onRender: (item, index) => {
                    return (
                        <Dropdown                            
                            selectedKey={this.state.items[index].feedbackContactMeans}
                            onChanged={(e) => this.onBlurFeedbackContactMeans(e, index)}
                            id={'txtfeedbackContact' + item.id}
                            options={
                                [
                                    { key: 0, text: 'Telephone' },
                                    { key: 1, text: 'Email' },
                                    { key: 2, text: 'Meeting' },
                                    { key: 3, text: 'Unkwown' }
                                ]
                            }
                        />
                    );
                }
            },
            {
                key: 'column3',
                name: 'Summary',
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'feedbackContactSummary',
                minWidth: 250,
                maxWidth: 400,
                isRowHeader: false,
                isResizable: true,
                isCollapsable: true,
                onRender: (item) => {
                    return (
                        <TextField id={'txtfeedbackSummary' + item.id}
                            defaultValue={item.feedbackSummary} 
                            onBlur={(e) => this.onBlurFeedbackSummary(e, item)}
                        />
                    );
                }
            },
            {
                key: 'column4',
                name: 'Details',
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'feedbackContactDetails',
                minWidth: 400,
                maxWidth: 500,
                isRowHeader: false,
                isResizable: true,
                isCollapsable: true,
                onRender: (item) => {
                    return (
                        <TextField id={'txtfeedbackDetails' + item.id}
                            defaultValue={item.feedbackDetails} multiline rows={5}
                            onBlur={(e) => this.onBlurFeedbackDetails(e, item)}
                        />
                    );
                }
            },
            {
                key: 'column5',
                name: 'Actions',
                headerClassName: 'ms-List-th',
                className: 'ms-Grid-col ms-sm12 ms-md12 ms-lg4',
                minWidth: 100,
                maxWidth: 100,
                onRender: (item) => {
                    return (
                        <div>
                            <IconButton iconProps={{ iconName: 'Save' }} onClick={(e) => this.saveRow()} />&nbsp;&nbsp;&nbsp;
                            <IconButton iconProps={{ iconName: 'Delete' }} onClick={(e) => this.deleteRow(item)} />
                        </div>
                    );
                }
            },
            {
                key: 'column6',
                name: "",
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3',
                minWidth: 10,
                maxWidth: 10,
                onRender: (item) => {
                    return (
                        <div />
                    );
                }
            }
        ];

        this.state = {
            isLoading: true,
            opportunity: "",
            channelName: "",
            teamName: "",
            groupId: "",
            customerFeedback: tmpCustomerFeedback,
            items: tmpItems,
            rowItemCounter: 1,
            columns: columns,
            isCompactMode: false,
            fontSize: 16,
            theme: ThemeStyle.Light,
            spinnerLabel: <Trans>loading</Trans>,
            MessagebarText: '',
            fileIsUploading: false,
            errorStatus: false,
            errorMessage: "",
            authorized: false,
            haveGranularAccess: false,
            isReadOnly: false
        };
    }

    async componentWillMount() {
        console.log("CustomerFeedback_componentWillMount isauth: " + this.authHelper.isAuthenticated());
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            try {
                this.accessGranted = true;
                await this.getOppDetails();
            } catch (error) {
                this.accessGranted = false;
                console.log("CustomerFeedback_componentDidUpdate error_callCheckAccess:");
                console.log(error);
            }
        }
    }

    componentDidMount() {
        this.getOppDetails();
    }

    getTeamsContext() {
        microsoftTeams.getContext(context => {
            if (context) {
                this.setState({
                    channelName: context.channelName,
                    channelId: context.channelId,
                    teamName: context.teamName,
                    groupId: context.groupId,
                    contextUpn: context.upn
                });
            }
        });
    }

    errorHandler(err, referenceCall) {
        console.log("Checklist Ref: " + referenceCall + " error: ");
        console.log(err);
    }

    async getOppDetails() {
        let teamName = getQueryVariable('teamName');
        let channelName = getQueryVariable('channelName');
        console.log("customerFeedback_Log callCheckAccess channelName, teamName : ", channelName + " **** " + teamName);
        this.setState({
            teamName,
            channelName
        });
        await this.getOpportunity(teamName, channelName);
    }

    async getOpportunity(teamName, channelName) {
        let data;
        let customerFeedbackObj = [];
        let itemsList = [];
        this.setState({ isLoading: true });
        try {

            let requestUrl = `api/Opportunity?name=${teamName}`;

            let response = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            data = await response.json();

            if (data === 'undefined' || data === null) {
                throw new Error("Opportunity is null");
            }

            customerFeedbackObj = data.customerFeedback;
            console.log("customerFeedback_Log customerFeedbackObj  1: ", customerFeedbackObj);
            itemsList = customerFeedbackObj.customerFeedbackList;

            this.setState({
                opportunity: data,
                customerFeedback: customerFeedbackObj,
                items: itemsList,
                rowItemCounter: itemsList.length,
                isLoading: false,
                haveGranularAccess: true,
                teamName,
                channelName
            });

        } catch (error) {

            console.log("customerFeedback_Log error : ", error.message);
            this.setState({
                opportunity: data,
                checklist: customerFeedbackObj,
                items: itemsList,
                rowItemCounter: itemsList.length,
                isLoading: false,
                haveGranularAccess: false,
                teamName,
                channelName
            });
        }
    }

    updateOpportunity(opportunity) {
        return new Promise((resolve, reject) => {
            let requestUrl = 'api/opportunity';

            let options = {
                method: "PATCH",
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
                },
                body: JSON.stringify(opportunity)
            };

            fetch(requestUrl, options)
                .then(response => {
                    console.log("CustomerFeedback_updateOpportunity_fetch response: " + response.status + " - " + response.statusText);
                    if (response.status === 401) {
                        // TODO: For v2 see how we pass to authHelper to force token refresh
                    }
                    return response;
                })
                .then(data => {
                    resolve(data);
                })
                .catch(err => {
                    this.errorHandler(err, "CustomerFeedback_updateOpportunity");
                    this.setState({
                        updateStatus: true,
                        MessagebarText: <Trans>errorWhileUpdatingPleaseTryagain</Trans>
                    });
                    this.hideMessagebar();
                    reject(err);
                });
        });
    }

    async updateCurrentItems(currItems, opportunity, updateOpp) {
        if (opportunity === null) {
            opportunity = this.state.opportunity;
        }
        if (currItems === null) {
            currItems = opportunity.customerFeedback.customerFeedbackList;
        }

        opportunity.customerFeedback.customerFeedbackList = currItems;

        this.setState({
            opportunity: opportunity,
            customerFeedback: opportunity.customerFeedback,
            items: currItems,
            rowItemCounter: currItems.length,
            updateStatus: true,
            MessagebarText: 'Updating feedback list...'
        });

        if (updateOpp) {

            let requestUrl = 'api/opportunity';

            let options = {
                method: "PATCH",
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
                },
                body: JSON.stringify(opportunity)
            };
            try {
                let response = await fetch(requestUrl, options);
                console.log("CustomerFeedback_updateOpportunity_fetch response: " + response.status + " - " + response.statusText);
                if (response.ok) {
                    await this.getOppDetails();
                }

            } catch (e) {
                console.log("Checklist UpdateOpportunity: " + e);
                this.errorHandler(e, "CustomerFeedback_updateOpportunity");
                this.setState({
                    updateStatus: true,
                    MessagebarText: <Trans>errorWhileUpdatingPleaseTryagain</Trans>
                });
                this.hideMessagebar();
            } finally {
                this.setState({
                    updateStatus: false,
                    MessagebarText: ""
                });
            }

        } else {
            this.setState({ updateStatus: false, MessagebarText: "" });
        }

    }

    createListItem(key) {
        return {
            key: key,
            id: this.utils.guid(),
            feedbackDate: "",
            feedbackContactMeans: "",
            feedbackSummary: "",
            feedbackDetails: ""
        };
    }

    onAddRow() {
        let rowCounter = this.state.rowItemCounter + 1;
        let newItems = [];
        newItems.push(this.createListItem(rowCounter));
        let currentItems = newItems.concat(this.state.items);
        this.updateCurrentItems(currentItems, null, false);
    }

    deleteRow(item) {
        if (this.state.items.length > 0) {
            this.setState({ updateStatus: true, MessagebarText: 'Updating...' });
            let currentItems = this.state.items.filter(x => x.id !== item.id);
            this.updateCurrentItems(currentItems, null, true);
        }
    }

    saveRow() {
        this.updateCurrentItems(null, null, true);
    }

    onBlurFeedbackSummary(e, item) {
        if (e.target.value === "") {
            this.setState({
                errorStatus: true,
                errorMessage: <Trans>itemFieldCannotbeEmpty</Trans>
            });
            setTimeout(function () { this.setState({ errorStatus: false, errorMessage: "" }); }.bind(this), 3000);
            return;
        }

        let currentItems = this.state.items;
        let itemIdx = currentItems.indexOf(item);

        if (e.target.value !== currentItems[itemIdx].feedbackSummary) {
            currentItems[itemIdx].feedbackSummary = e.target.value;
            this.updateCurrentItems(currentItems, null, false);
        }
    }

    onBlurFeedbackDetails(e, item) {
        if (e.target.value === "") {
            this.setState({
                errorStatus: true,
                errorMessage: <Trans>itemFieldCannotbeEmpty</Trans>
            });
            setTimeout(function () { this.setState({ errorStatus: false, errorMessage: "" }); }.bind(this), 3000);
            return;
        }

        let currentItems = this.state.items;
        let itemIdx = currentItems.indexOf(item);

        if (e.target.value !== currentItems[itemIdx].feedbackDetails) {
            currentItems[itemIdx].feedbackDetails = e.target.value;
            this.updateCurrentItems(currentItems, null, false);
        }
    }

    onBlurFeedbackContactMeans(e, index) {
        let currentItems = this.state.items;

        if (e.key !== currentItems[index].feedbackContactMeans) {
            currentItems[index].feedbackContactMeans = e.key;
            this.updateCurrentItems(currentItems, null, false);
        }
    }

    onSelectFeedbackDate = (date, index) => {
        let currentItems = this.state.items;

        if (date !== currentItems[index].feedbackDate) {
            currentItems[index].feedbackDate = date;
            this.updateCurrentItems(currentItems, null, false);
        }
    }

    _onFormatDate = (date) => {
        return date === null ? '' : (
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

    hideMessagebar() {
        if (this.hidePending === false) {
            this.hidePending = true;
            setTimeout(function () {
                this.setState({ updateStatus: false, MessagebarText: "" });
                this.hidePending = false;
            }.bind(this), 3000);
        }
    }

    onColumnClick = (ev, column) => {
        const { columns, items } = this.state;
        let newItems = items.slice();
        const newColumns = columns.slice();
        const currColumn = newColumns.filter((currCol, idx) => {
            return column.key === currCol.key;
        })[0];

        newColumns.forEach((newCol) => {
            if (newCol === currColumn) {
                currColumn.isSortedDescending = !currColumn.isSortedDescending;
                currColumn.isSorted = true;
            } else {
                newCol.isSorted = false;
                newCol.isSortedDescending = true;
            }
        });

        newItems = this.sortItems(newItems, currColumn.fieldName, currColumn.isSortedDescending);

        this.setState({
            columns: newColumns,
            items: newItems
        });
    }

    sortItems = (items, sortBy, descending = false) => {
        if (descending) {
            return items.sort((a, b) => {
                if (a[sortBy] < b[sortBy]) {
                    return 1;
                }
                if (a[sortBy] > b[sortBy]) {
                    return -1;
                }
                return 0;
            });
        } else {
            return items.sort((a, b) => {
                if (a[sortBy] < b[sortBy]) {
                    return -1;
                }
                if (a[sortBy] > b[sortBy]) {
                    return 1;
                }
                return 0;
            });
        }
    }

    render() {
        const { columns, isCompactMode, items } = this.state;
        console.log("CustomerFeedback_render : ", this.state.haveGranularAccess);
        return (
            <TeamsComponentContext fontSize={this.state.fontSize} theme={this.state.theme}>
                <div className='ms-Grid bg-white'>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' >
                            {
                                this.state.isLoading ?
                                    <div className='ms-Grid'>
                                        <div className='ms-Grid-row bg-white'>
                                            <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12 p-10' >
                                                <br />
                                                <Spinner size={SpinnerSize.medium} label={this.state.spinnerLabel} ariaLive='assertive' />
                                            </div>
                                        </div>
                                    </div>
                                    :
                                    this.state.haveGranularAccess
                                        ?
                                        <div className='ms-Grid'>
                                            <div className='ms-Grid-row'>
                                                <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12 p-10' >
                                                    <Panel>
                                                        <PanelHeader>
                                                            <div >
                                                                <h3>Customer Feedback &nbsp;<Anchor className='' onClick={e => this.onAddRow(e)} ><i className="ms-Icon ms-Icon--Add font-16" aria-hidden="true" /> </Anchor></h3>
                                                            </div>
                                                        </PanelHeader>

                                                        <PanelBody>
                                                            <div className='ms-Grid'>
                                                                <div className='ms-Grid-row ibox-content'>
                                                                    <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg12'>
                                                                        <DetailsList
                                                                            items={items}
                                                                            compact={isCompactMode}
                                                                            columns={columns}
                                                                            selectionMode={SelectionMode.none}
                                                                            selectionPreservedOnEmptyClick='true'
                                                                            setKey='set'
                                                                            layoutMode={DetailsListLayoutMode.justified}
                                                                            enterModalSelectionOnTouch='false'
                                                                        />
                                                                    </div>
                                                                </div>
                                                                <div className='ms-grid-row'>
                                                                    <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg12'>
                                                                        <hr />
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        </PanelBody>
                                                        <PanelFooter>
                                                            <div className='ms-Grid'>
                                                                <div className='ms-Grid-row'>
                                                                    <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg8' />
                                                                    <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg4'>
                                                                        {this.state.updateStatus === true ?
                                                                            <MessageBar messageBarType={MessageBarType.success} isMultiline={false}>
                                                                                {this.state.MessagebarText}
                                                                            </MessageBar>
                                                                            : ""
                                                                        }
                                                                    </div>

                                                                    <div className='ms-Grid-row'>
                                                                        <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg8' />
                                                                        <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg4'>
                                                                            {this.state.errorStatus === true ?
                                                                                <MessageBar messageBarType={MessageBarType.error} isMultiline={false}>
                                                                                    {this.state.errorMessage}
                                                                                </MessageBar>
                                                                                : ""
                                                                            }
                                                                        </div>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        </PanelFooter>
                                                    </Panel>
                                                </div>
                                            </div>
                                        </div>
                                        :
                                        <AccessDenied />
                            }
                        </div>
                    </div>
                </div>
            </TeamsComponentContext>
        );

    }
}