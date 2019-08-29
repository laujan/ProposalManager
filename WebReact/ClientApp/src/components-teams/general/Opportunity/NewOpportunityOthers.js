/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { PeoplePickerTeamMembers } from './PeoplePickerTeamMembers';
import { Trans } from "react-i18next";
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';

export class NewOpportunityOthers extends Component {
    displayName = NewOpportunityOthers.name

    constructor(props) {
        super(props);

        let teamlist = [];
        this.props.teamMembers.forEach(element => {
            element.userRoles.forEach((userrole) => {
                userrole.permissions.forEach((per) => {
                    if (per.name.toLowerCase() === "Opportunity_ReadWrite_Dealtype".toLocaleLowerCase() && !teamlist.includes(element)) {
                        teamlist.push(element);
                    }
                });
            });
        });

        this.logService = this.props.logService;
        this.opportunity = this.props.opportunity;
        this.metaData = this.props.metaDataList.length > 0 ? this.props.metaDataList.filter(prop=>prop.screen ==="Screen3") : [];
        this.state = {
            showModal: false,
            currentPicker: 1,
            delayResults: false,
            adGroupUsers: [],
            templateList: this.props.templateList,
            templateItems: this.props.templateItems ,// will have all template list with all process
            disableSubmit: true,
            teamMembers: teamlist,
            teamMembersAll: this.props.teamMembers
        };
    }
    
    getSelectedUsers() {
        // Wave4 changes - who has "opportunity_edit_team_member" permission dispaly those user as selected in dropdown 
        let selectedLO = [];
        this.opportunity.teamMembers.forEach(element => {
            element.permissions.forEach((userrole) => {
                if (userrole.name.toLowerCase() === "Opportunity_ReadWrite_Dealtype".toLocaleLowerCase() && !selectedLO.includes(element)) {
                        selectedLO.push(element);
                    }
            });
        });
        let tempArray = [];
        let filteredLoUser = JSON.parse(JSON.stringify(selectedLO));
        filteredLoUser = filteredLoUser.filter(obj => {
            let key = obj.displayName.toLowerCase() + obj.adGroupName.toLowerCase();
            if (!tempArray.includes(key)) {
                obj.text = obj.displayName;
                tempArray.push(key);
                return obj;
            }
        });
        
        return filteredLoUser; 
    }

    onChangeLoanOfficer(value) {
        let updatedTeamMembers = JSON.parse(JSON.stringify(this.opportunity.teamMembers));

        updatedTeamMembers = this.opportunity.teamMembers.filter(x => {
            if(!x.permissions.includes("Opportunity_ReadWrite_Dealtype"))
                return x;
        });

        if (value.length > 0) {
           let role = value[0].userRoles.find(role => {
                if (role.permissions.find(permission => permission.name === "Opportunity_ReadWrite_Dealtype"))
                    return role.id;
            });
            
            updatedTeamMembers.push(this.addBaseProcessPersonal(value,role,"Start Process"));
            // "Customer Decision";
            updatedTeamMembers.push(this.addBaseProcessPersonal(value,role,"Customer Decision"));
			
        }else if(value.length===0){
            updatedTeamMembers.splice(-2,2);
        }
        this.opportunity.teamMembers = updatedTeamMembers;

        this.checkNextEnabled();
    }

    addBaseProcessPersonal(value,role,processstep){

        let newMember = {};

        newMember.status = 0;
        newMember.id = value[0].id;
        newMember.displayName = value[0].text;
        newMember.mail = value[0].mail;
        newMember.userPrincipalName = value[0].userPrincipalName;
        newMember.roleId = role ? role.id : "";
        newMember.permissions= role ? role.permissions:[];
        newMember.teamsMembership = role ? role.teamsMembership:[];
        newMember.ProcessStep =processstep;
        newMember.roleName = role ? role.displayName : "";
        newMember.adGroupName = role ? role.adGroupName : "";

        return newMember;
    }

    onChangeTemplate(e) {
        // templateItems
        let selTemplate = this.state.templateItems.filter(function (d) {
            return d.id === e.key;
        });
        this.opportunity.template = selTemplate[0];
    }

    onBlurProperty(e, item) {
        item.values = e.target.value;
        this.checkNextEnabled();
    }

    checkNextEnabled() {
        
        let disableSubmit = this.getSelectedUsers().length === 0;
        let shouldDisable = false;

        let requiredItems = this.opportunity.metaDataFields.filter(item => item.required && item.screen === "Screen3");
        for (let element of requiredItems) {
            if (element.values.length === 0) {
                shouldDisable = true;
                break;
            }
        }


        this.setState({ disableSubmit: disableSubmit || shouldDisable });
    }

    _rendermetaData(){
        let metaDataComponents = null;
        if(this.metaData.length>0){
            metaDataComponents= this.metaData.map((metaDataObj)=>{
                let value = this.opportunity.metaDataFields.find(x => x.id === metaDataObj.uniqueId);
               
                return (
                    <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg4' key={metaDataObj.id}>
                                <TextField
                                label={metaDataObj.displayName}
                                value={value.values}
                                onBlur={(e) => this.onBlurProperty(e, value)}
                                />
                    </div>
                );

            });
        }
        return metaDataComponents;
    }

    render() {

        let selectedUsers = this.getSelectedUsers();
        let loanOfficerADName =  <Trans>loanOfficer</Trans>; //TODO from appsettings
        if(this.state.teamMembers.length>0){
            if(this.state.teamMembers[0].userRoles.length>0){
                loanOfficerADName = this.state.teamMembers[0].userRoles[0].adGroupName;
            }
        }

        let defaultTemplateAvailable = this.state.templateList.some(name=>name.defaultTemplate);
        return (
            <div>
                <div className='ms-Grid'>
                    <div className='ms-grid-row'>
                        <h3 className="pageheading"><Trans>opportunityProperties</Trans></h3>
                        <div className='ms-lg12 ibox-content pb20'>
                            {this._rendermetaData()}
                        </div>
                    </div>
                </div>

                {
                    defaultTemplateAvailable ? null :             
                        <div className='ms-Grid'>
                            <div className='ms-grid-row'>
                                <h3 className="pageheading"><Trans>template</Trans></h3>
                                <div className='ms-lg12 ibox-content pb20'>
                                    <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                                        <div className="dropdownContainer">
                                            <Dropdown
                                                placeHolder={<Trans>selectTemplate</Trans>}
                                                defaultSelectedKey={this.opportunity.template.id}
                                                options={this.state.templateList.filter(name=>name.defaultTemplate === false)}
                                                onChanged={(e) => this.onChangeTemplate(e)}
                                            />
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                }
                <div className='ms-Grid'>
                    <div className='ms-grid-row'>
                        <h3 className="pageheading">{loanOfficerADName}</h3>
                        <div className='ms-lg12 ibox-content pb20'>
                            <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                                <PeoplePickerTeamMembers teamMembers={this.state.teamMembers} defaultSelectedUsers={selectedUsers} onChange={(e) => this.onChangeLoanOfficer(e)} apiService={this.props.apiService} logService={this.props.logService} />
                            </div>
                        </div>
                    </div>
                </div>

                <div className='ms-Grid'>
                    <div className='ms-grid-row '>
                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pb20'><br />
                            <PrimaryButton className='backbutton pull-left' onClick={this.props.onClickBack}><Trans>back</Trans></PrimaryButton>
                        </div>
                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pb20'><br />
                            <PrimaryButton disabled={this.state.disableSubmit} className='pull-right' onClick={this.props.onClickNext}><Trans>submit</Trans></PrimaryButton>
                        </div>
                    </div><br /><br />
                </div>
            </div>
        );
    }
}