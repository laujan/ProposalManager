﻿/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import Utils from '../../../helpers/Utils';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
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

        this.utils = new Utils();
        this.metaData = this.props.metaDataList.length > 0 ? this.props.metaDataList.filter(prop=>prop.screen ==="Screen1") : [];
        this.opportunity = this.props.opportunity;
        this.dashboardList = this.props.dashboardList;
        this.logService = this.props.logService;

        this.state = {
            nextDisabled: false
        };
    }    

    onBlurProperty(e,item) {
        switch (item.uniqueId) {
            case "opportunity":
                this.opportunity.displayName = e.target.value;
                break;
            case "customer":
                this.opportunity.customer.displayName = e.target.value;
                break;
            case "notes":
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
                break;
            default:
                break;
        }

        item.values=e.target.value;
        this._checkNextEnabled();
    }

    onChangeDropDown(e, key) {
        //dropvalue.values = e.text;
        if(e.text.length>0){
            this.opportunity.metaDataFields.forEach(obj=>{
                if (obj.uniqueId===key){
                    obj.values=e.text;
                }
            });
        }
    }

    _checkNextEnabled(){
        let valid = true;

        let requiredItems = this.opportunity.metaDataFields.filter(item => item.required && item.screen === "Screen1");
        for (let element of requiredItems) {
            if (element.values.length === 0) {
                valid = false;
                break;
            }
        }

        this.setState({ nextDisabled: valid });
    }

    _onSelectTargetDate = (date, item) => {
        item.values = this._onFormatDate(date);

        if (item.uniqueId === "targetdate") {
            let openedDate = this.opportunity.metaDataFields.find(obj => obj.uniqueId === "openeddate");

            if (openedDate && openedDate.values.length === 0) {
                openedDate.values = this._onFormatDate(this._setItemDate(Date.now()));
            }
        }

        this._checkNextEnabled();
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

    _rendermetaData(){
        let metaDataComponents = null;
        if(this.metaData.length>0){
            metaDataComponents= this.metaData.map((metaDataObj)=>{
                let component = null;
                let id = metaDataObj.uniqueId;
                switch (metaDataObj.fieldType.name) {
                    case "Date":
                        let tardate = this.opportunity.metaDataFields.find(x => x.uniqueId === id);
                        let tarDateValue = tardate.values;
                        component = (<div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6' key={metaDataObj.uniqueId}>
                            <DatePicker strings={DayPickerStrings}
                                label={metaDataObj.displayName}
                                showWeekNumbers={false}
                                firstWeekOfYear={1}
                                showMonthPickerAsOverlay='true'
                                iconProps={{ iconName: 'Calendar' }}
                                value={tarDateValue ? this._setItemDate(tarDateValue) : ""}
                                onSelectDate={(date) => this._onSelectTargetDate(date, tardate)}
                                formatDate={this._onFormatDate}
                                parseDateFromString={this._onParseDateFromString}
                                minDate={new Date()}
                                isRequired={metaDataObj.required}
                            />
                        </div>);
                        break;

                    case "DropDown":
                        let placeHolder = `Select ${metaDataObj.displayName}`;
                        let dropvalue = this.opportunity.metaDataFields.find(x => x.uniqueId === id).values;
                        component = (<div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6' key={metaDataObj.uniqueId}>
                                <Dropdown
                                    placeHolder={placeHolder}
                                    label={metaDataObj.displayName}
                                    id={id}
                                    ariaLabel={metaDataObj.displayName}
                                    value={dropvalue}
                                    options={metaDataObj.values.map(x=>{return {'key':x.id,'text':x.name}})}
                                    defaultSelectedKey={metaDataObj.values.map(x => { if (x.name === dropvalue) return x.id; })}
                                    isRequired={metaDataObj.required}
                                    componentRef=''
                                onChanged={(e) => this.onChangeDropDown(e, id)}
                                />
                            </div>);
                        break;

                    default:
                        let textItem = this.opportunity.metaDataFields.find(x => x.uniqueId === id);
                        component = (<div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6' key={metaDataObj.uniqueId}>
                                <TextField
                                id={id}
                                value={textItem.values}
                                label={metaDataObj.displayName}
                                onBlur={(e) => this.onBlurProperty(e, textItem)}
                                required={metaDataObj.required}
                                />
                            </div>);
                        break;
                }
                return component;
            });
        }
        return metaDataComponents;
    }

    render()
    {
        //TODO: set focus on initial load of component: this.customerName.focusInput()
        return (
            <div className='ms-Grid'>
                <div className='ms-Grid-row'>
                    <h3 className='pageheading'><Trans>createNewOpportunity</Trans></h3>
                    <div className='ms-lg12 ibox-content'>
                        <div className="ms-Grid-row">
                            {this._rendermetaData()}
                        </div>
                    </div>
                </div>
                <div className='ms-Grid-row pb20'>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pl0'><br />
                        <PrimaryButton 
                            className='backbutton pull-left' 
                            onClick={this.props.onClickCancel}>{<Trans>cancel</Trans>}</PrimaryButton>
                    </div>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pr0' ><br />
                        <PrimaryButton className='pull-right' disabled = {!this.state.nextDisabled}  onClick={this.props.onClickNext} >{<Trans>next</Trans>}</PrimaryButton>
                    </div>
                </div>
            </div>
        );
    }
}