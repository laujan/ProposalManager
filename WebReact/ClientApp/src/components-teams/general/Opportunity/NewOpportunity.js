/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import Utils from '../../../helpers/Utils';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import {  Trans } from "react-i18next";
import { DatePicker } from 'office-ui-fabric-react/lib/DatePicker';


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

export class NewOpportunity extends Component {
    displayName = NewOpportunity.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;
        this.utils = new Utils();

        this.state = {
            industryList: this.props.industries,
            regionList: this.props.regions,
            custNameError: false,
            messagebarTextCust: "",
            oppNameError: false,
            messagebarTextOpp: "",
            dealSizeError: false,
            messagebarTextDealSize: "",
            annualRevenueError: false,
            messagebarTextAnnualRev: "",
            nextDisabled: true
        };
    }

    componentWillMount() {
        this.opportunity = this.props.opportunity;
        this.dashboardList = this.props.dashboardList;
    }


    // Class methods
    onBlurCustomer(e) {
        if (e.target.value.length === 0) {
            this.setState({
                messagebarTextCust: <Trans>customerNameNotEmpty</Trans>,
                custNameError: false
            });
            this.opportunity.customer.displayName = "";
        } else {
            this.setState({
                messagebarTextCust: "",
                custNameError: false
            });
            this.opportunity.customer.displayName = e.target.value;
        }
    }

    onBlurOpportunityName(e) {
        if (e.target.value.length > 0) {
            this.opportunity.displayName = e.target.value;
            this.setState({
                messagebarTextOpp: "",
                oppNameError: false
            });
            // let uniqueResponse = this.oppNameIsUnique(e.target.value);
        } else {
            this.opportunity.displayName = "";
            this.setState({
                messagebarTextOpp: <Trans>opportunityNameNotEmpty</Trans>,
                oppNameError: false
            });
        }
    }

    onBlurDealSize(e) {
        this.opportunity.dealSize = e.target.value;
    }

    onBlurAnnualRevenue(e) {
        this.opportunity.annualRevenue = e.target.value;
    }

    onChangeIndustry(e) {
        this.opportunity.industry.id = e.key;
        this.opportunity.industry.name = e.text;
    }

    onChangeRegion(e) {
        this.opportunity.region.id = e.key;
        this.opportunity.region.name = e.text;
    }

    onBlurNotes(e) {
        // TODO: Add createdby propeties
        let note = {
            id: this.utils.guid(),
            noteBody: e.target.value,
            createdDateTime: "",
            createdBy: {
                id: "",
                displayName: "",
                userPrincipalName: "",
                userRoles: []
            }
        };

        this.opportunity.notes.push(note);
    }

    oppNameIsUnique(name) {
        if (this.opportunity.displayName.length > 0) {
            if (this.dashboardList.find(itm => itm.opportunity === name)) {
                this.setState({
                    messagebarTextOpp: <Trans>opportunityNameUnique</Trans>,
                    oppNameError: false
                });
                return true;
            } else {
                return false;
            }
        } else {
            // If empty also return false
            return false;
        }
    }

    _onSelectTargetDate = (date) => {
        this.opportunity.targetDate = date;

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

    render() {
        let nextDisabled = true;
        if (this.opportunity.customer.displayName.length > 0 && this.opportunity.displayName.length > 0) {
            nextDisabled = false;
        }

        //TODO: set focus on initial load of component: this.customerName.focusInput()

        return (
            <div className='ms-Grid'>
                <div className='ms-Grid-row'>
                    <h3 className='pageheading'><Trans>createNewOpportunity</Trans></h3>
                    <div className='ms-lg12 ibox-content'>
                        <div className="ms-Grid-row">
                            <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                                <TextField
                                    id='customerName'
                                    label={<Trans>customerName</Trans>} value={this.opportunity.customer.displayName}
                                    errorMessage={this.state.messagebarTextCust}
                                    onBlur={(e) => this.onBlurCustomer(e)}
                                />
                                {this.state.custNameError ?
                                    <MessageBar messageBarType={MessageBarType.error} isMultiline={false}>
                                        {this.state.messagebarTextCust}
                                    </MessageBar>
                                    : ""
                                }
                            </div>


                            <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                                <TextField
                                    label={<Trans>opportunityName</Trans>} value={this.opportunity.displayName}
                                    errorMessage={this.state.messagebarTextOpp}
                                    onBlur={(e) => this.onBlurOpportunityName(e)}

                                />
                                {this.state.oppNameError ?
                                    <MessageBar messageBarType={MessageBarType.error} isMultiline={false}>
                                        {this.state.messagebarTextOpp}
                                    </MessageBar>
                                    : ""
                                }
                            </div>
                        </div>
                        <div className="ms-Grid-row">
                            <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                                <TextField
                                    label={<Trans>dealSize</Trans>} value={this.opportunity.dealSize}
                                    onBlur={(e) => this.onBlurDealSize(e)}
                                />
                                {this.state.dealSizeError ?
                                    <MessageBar messageBarType={MessageBarType.error} isMultiline={false}>
                                        {this.state.messagebarTextDealSize}
                                    </MessageBar>
                                    : ""
                                }
                            </div>
                            <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                                <TextField
                                    label={<Trans>annualRevenue</Trans>} value={this.opportunity.annualRevenue}
                                    onBlur={(e) => this.onBlurAnnualRevenue(e)}

                                />
                                {this.state.annualRevenueError ?
                                    <MessageBar messageBarType={MessageBarType.error} isMultiline={false}>
                                        {this.state.messagebarTextAnnualRev}
                                    </MessageBar>
                                    : ""
                                }
                            </div>
                        </div>

                        <div className="ms-Grid-row">
                            <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                                <Dropdown
                                    placeHolder={<Trans>selectIndustry</Trans>}
                                    label={<Trans>industry</Trans>}
                                    id='Basicdrop1'
                                    ariaLabel={<Trans>industry</Trans>}
                                    value={this.opportunity.industry.id}
                                    options={this.state.industryList}
                                    defaultSelectedKey={this.opportunity.industry.id}
                                    componentRef={this.ddlIndustry}
                                    onChanged={(e) => this.onChangeIndustry(e)}
                                />
                            </div>
                            <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                                <Dropdown
                                    placeHolder={<Trans>selectRegion</Trans>}
                                    label={<Trans>region</Trans>}
                                    id='ddlRegion'
                                    ariaLabel={<Trans>region</Trans>}
                                    value={this.opportunity.region.id}
                                    options={this.state.regionList}
                                    defaultSelectedKey={this.opportunity.region.id}
                                    componentRef=''
                                    onChanged={(e) => this.onChangeRegion(e)}
                                />
                            </div>
                        </div>
                        <div className="ms-Grid-row">
                            <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                                <DatePicker strings={DayPickerStrings}
                                    label={<Trans>targetDate</Trans>}
                                    showWeekNumbers={false}
                                    firstWeekOfYear={1}
                                    showMonthPickerAsOverlay='true'
                                    iconProps={{ iconName: 'Calendar' }}
                                    value={this.opportunity.targetDate ? this._setItemDate(this.opportunity.targetDate) : ""}
                                    onSelectDate={this._onSelectTargetDate}
                                    formatDate={this._onFormatDate}
                                    parseDateFromString={this._onParseDateFromString}
                                    minDate={new Date()}
                                />
                            </div>
                        </div>
                        <div className="ms-Grid-row">
                            <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                                <TextField
                                    label={<Trans>notes</Trans>}
                                    multiline
                                    rows={6}
                                    value={this.opportunity.notes.noteBody}
                                    onBlur={(e) => this.onBlurNotes(e)}
                                />
                            </div>
                        </div>
                    </div>
                </div>
                <div className='ms-Grid-row pb20'>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pl0'><br />
                        <PrimaryButton className='backbutton pull-left' onClick={this.props.onClickCancel}>{<Trans>cancel</Trans>}</PrimaryButton>
                    </div>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pr0'><br />
                        <PrimaryButton className='pull-right' onClick={this.props.onClickNext} disabled={nextDisabled}>{<Trans>next</Trans>}</PrimaryButton>
                    </div>
                </div>
            </div>
        );
    }
}