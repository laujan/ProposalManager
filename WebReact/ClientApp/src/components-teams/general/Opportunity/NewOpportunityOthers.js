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

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;
        this.utils = window.utils;

        let teamlist = [];
        this.props.teamMembers.forEach(element => {
            element.userRoles.forEach((userrole) => {
                userrole.permissions.forEach((per) => {
                    if (per.name.toLowerCase() === "Opportunity_ReadWrite_Dealtype".toLocaleLowerCase()) {
                        console.log("NewOpportunityOther_filterUserProfiles_element : ", element);
                        if (!teamlist.includes(element)) teamlist.push(element);
                    }
                });
            });
        });

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
        console.log(this.opportunity.teamMembers);
        let selectedLO = [];
        this.opportunity.teamMembers.forEach(element => {
            element.permissions.forEach((userrole) => {
                if (userrole.name.toLowerCase() === "Opportunity_ReadWrite_Dealtype".toLocaleLowerCase()) {
                        console.log("NewOpportunityOther_filterUserProfiles_element : ", element);
                    if (!selectedLO.includes(element)) selectedLO.push(element);
                    }
            });
        });
        let tempArray = [];
        let filteredLoUser = JSON.parse(JSON.stringify(selectedLO));
        filteredLoUser = filteredLoUser.filter(obj => {
            console.log(obj);
            let key = obj.displayName.toLowerCase() + obj.adGroupName.toLowerCase();
            if (!tempArray.includes(key)) {
                obj.text = obj.displayName;
                tempArray.push(key);
                return obj;
            }
        });
        console.log(filteredLoUser);
        return filteredLoUser; // this.opportunity.teamMembers.filter(x => x.permissions.includes("Opportunity_ReadWrite_Dealtype"));
    }

    onChangeLoanOfficer(value) {
        console.log("NewOpportunityOther_filterUserProfiles : ", value);

        let updatedTeamMembers = JSON.parse(JSON.stringify(this.opportunity.teamMembers));

        updatedTeamMembers = this.opportunity.teamMembers.filter(x => {
            if(!x.permissions.includes("Opportunity_ReadWrite_Dealtype"))
                return x;
        });

        console.log("NewOpportunityOther_filterUserProfiles : ", updatedTeamMembers);
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
        console.log(this.opportunity.teamMembers);
        // let disableSubmit = this.utils.getLoanOficers(this.opportunity.teamMembers).length>0?false:true;
        let selLO = [];
        this.opportunity.teamMembers.forEach(element => {
            element.permissions.forEach((userrole) => {
                if (userrole.name.toLowerCase() === "Opportunity_ReadWrite_Dealtype".toLocaleLowerCase()) {
                    if (!selLO.includes(element)) selLO.push(element);
                }
            });
        });
        let disableSubmit = selLO.length > 0 ? false : true;
        console.log("NewOpportunityOther_filterUserProfiles : ", disableSubmit);
        this.setState({ disableSubmit });
        
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
        console.log(selTemplate);
        this.opportunity.template = selTemplate[0];
        console.log(this.opportunity);
    }

    onBlurProperty(e,key) {
        if (e.target.value.length !== 0) {
            this.opportunity.metaDataFields.forEach(obj=>{
                if(obj.id===key){
                    console.log("NewOpportunityOthers_onBlurProperty : ", obj.id);
                    obj.values=e.target.value;
                }
            });
        } 
    }

    _rendermetaData(){
        let metaDataComponents = null;
        if(this.metaData.length>0){
            metaDataComponents= this.metaData.map((metaDataObj)=>{
                let component = null;
                let id = metaDataObj.displayName.toLowerCase().replace(/\s/g, '');
                let value = this.opportunity.metaDataFields.find(x=>x.id===id).values;
                return (
                    <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg4' key={metaDataObj.id}>
                                <TextField
                                    label={metaDataObj.displayName} 
                                    value={value}
                                    onBlur={(e) => this.onBlurProperty(e,id)}
                                />
                    </div>
                );

            });
        }
        return metaDataComponents;
    }


    render() {

        let selectedUsers = this.getSelectedUsers();
        let disableSubmit = selectedUsers.length > 0 ? false : true;
        console.log("NewOpportunityOther_getSelectedUser : ", disableSubmit);
        disableSubmit = this.setState.disableSubmit ? this.setState.disableSubmit : disableSubmit;

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
                                <PeoplePickerTeamMembers teamMembers={this.state.teamMembers} defaultSelectedUsers={selectedUsers} onChange={(e) => this.onChangeLoanOfficer(e)} />
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
                            <PrimaryButton disabled={disableSubmit} className='pull-right' onClick={this.props.onClickNext}><Trans>submit</Trans></PrimaryButton>
                        </div>
                    </div><br /><br />
                </div>

            </div>
        );
    }
}