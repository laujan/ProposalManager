﻿/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Link as LinkRoute } from 'react-router-dom';
import { TeamMembers } from './TeamMembers';
import { Persona, PersonaSize } from 'office-ui-fabric-react/lib/Persona';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { userRoles } from '../../../common';
import { PeoplePickerTeamMembers } from './PeoplePickerTeamMembers';
import { I18n, Trans } from "react-i18next";
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import { TooltipHost } from 'office-ui-fabric-react/lib/Tooltip';

export class OpportunitySummary extends Component {
    displayName = OpportunitySummary.name
    constructor(props) {
        super(props);

        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        this.authHelper = window.authHelper;
        this.utils = window.utils;
        const opportunityData = this.props.opportunityData;
        const teamsContext = this.props.teamsContext;
        
        this.state = {
            teamsContext: teamsContext,
            loading: true,
            loanOfficer: [],
            teamMembers: [],
            showPicker: false,
            peopleList: [],
            currentSelectedItems: [],
            oppData: opportunityData,
            btnSaveDisable: false,
            usersPickerLoading: true,
            loanOfficerPic: '',
            loanOfficerName: '',
            loanOfficerRole: '',
            userAssignedRole: "",
            oppStatusAll: [],
            isUpdate: false,
            isStatusUpdate: false,
            dealTypeItems: [],
            dealTypeList: [],
            isUpdateOpp: false,
            isUpdateOppMsg: false,
            updateOppMessagebarText: "",
            updateMessageBarType: "",
            dealTypeLoading: true,
            dealTypeSelectMsgShow: false,
            dealTypeUpdated: false,
            userId: "",
            haveAccessToChangeLO: false,
            haveAccessToChangeStatus: false,
            haveAccessToEditTeam: false,
            haveAccessToEditDealType: false
        };

        this.onStatusChange = this.onStatusChange.bind(this);
    }

    async componentDidMount() {
        this.logService.log("OpportunityDetails_componentDidMount");

        try {
            if (this.state.oppData) {
                await this.getUserProfiles();
                await this.getOppStatusAll();
                await this.getDealTypeLists();
                await this.getOppDetails();
            } else {
                this.logService.log("OpportunitySummary_componentDidUpdate 2", this.state.loading);
                if (typeof this.state.teamsContext !== 'undefined' && this.state.loading) {
                    await this.getOpportunityForTeams(this.state.teamsContext.teamName);
                }
            }
        } catch (error) {
            this.logService.log("OpportunitySummary_componentDidUpdate error : ", error);
        }
    }
    
    async getOpportunityForTeams(teamName) {
        let oppData = "";
        try {
            let response = await this.apiService.callApi('Opportunity', 'GET', { query: `name=${teamName}` });
            if (response.ok) {
                oppData = await response.json();
            }
            else {
                this.logService.log("getOpportunityForTeams", response.statusText);
            }

            this.setState({ oppData });
            return oppData;
        }
        catch (err) {
            this.logService.log("OpportunitySummar_getOppDetails err:", err);
            return oppData;
        }
    }

    async getOppStatusAll() {
        this.logService.log("OpportunitySummary_getOppStatusAll ");

        try {
            let response = await this.apiService.callApi('Context', 'GET', { id: 'GetOpportunityStatusAll' });
            if (response.ok) {
                let data = await response.json();
                let filteredData = [];
                if (this.state.oppData.opportunityState !== 11) // if the current state is not archived, remove the archive option from the array
                {
                    filteredData = data.filter(x => x.Name !== 'Archived');
                }

                let oppStatusAll = [];
                for (let i = 0; i < filteredData.length; i++) {
                    let oppStatus = {};
                    oppStatus.key = data[i].Value;
                    oppStatus.text = data[i].Name;
                    oppStatusAll.push(oppStatus);
                }
                this.setState({ oppStatusAll });
                return true;
            }
            else {
                this.logService.log("OpportunitySummary_getOppStatusAll error: ", response.statusText);
                return false;
            }
        } catch (error) {
            this.logService.log("OpportunitySummary_getOppStatusAll error : ", error);
            return false;
        }
    }

    async getUserProfiles()
    {
        try
        {
            let peopleList = [];
            let response = await this.apiService.callApi('UserProfile', 'GET');
            if (response.ok) {
                let data = await response.json();

                if (data.ItemsList.length > 0) {
                    for (let i = 0; i < data.ItemsList.length; i++) {
                        let item = data.ItemsList[i];
                        let newItem = {};
                        newItem.id = item.id;
                        newItem.displayName = item.displayName;
                        newItem.mail = item.mail;
                        newItem.userPrincipalName = item.userPrincipalName;
                        newItem.userRoles = item.userRoles;

                        peopleList.push(newItem);
                    }
                }

                this.logService.log("OpportunitySummary_getUserProfiles peopleList : ", peopleList);
                let teamlist = this.utils.getMembersWithTemplateProperties(data.ItemsList);

                this.logService.log("OpportunitySummary_getUserProfiles peopleList : ", teamlist);
                this.setState({ peopleList: teamlist, usersPickerLoading: peopleList > 0 ? true : false });
                return true;
            }
            else {
                this.logService.log("OpportunitySummary_getUserProfiles error : ", response.statusText);
                return false;
            }
        } catch (error) {
            this.logService.log("OpportunitySummary_getUserProfiles error : ", error);
            return false;
        }
    }

    async getDealTypeLists()
    {       
        try
        {
            this.logService.log("OpportunitySummary_getDealTypeLists");
            let response = await this.apiService.callApi('Template', 'GET');
            if (response.ok) {
                let data = await response.json();
                let dealTypeItemsList = [];
                let dealTypeList = [];
                for (let i = 0; i < data.itemsList.length; i++) {
                    dealTypeItemsList.push(data.itemsList[i]);
                    let dealType = {};
                    dealType.key = data.itemsList[i].id;
                    dealType.text = data.itemsList[i].templateName;
                    dealType.defaultTemplate = data.itemsList[i].defaultTemplate;
                    dealTypeList.push(dealType);
                }
                this.setState({
                    dealTypeItems: dealTypeItemsList,
                    dealTypeList: dealTypeList,
                    dealTypeLoading: false
                });
                return true;
            }
            else {
                this.logService.log("OpportunitySummary_getDealTypeLists error: ", response.statusText);
                return false;
            }
        } catch (error) {
            this.logService.log("OpportunitySummary_getDealTypeLists error ", error);
            return false;
        }
    }

    async getOppDetails() {

        try {
            let data = this.state.oppData;
            if (data) {
                this.logService.log("OpportunitySummary_getOppDetails data: ", data.teamMembers);
                let userDetails = this.props.userProfile;
                let teamMembers = [];
                teamMembers = data.teamMembers;
                let loanOfficerObj = this.utils.getLoanOficers(data.teamMembers);
                let officer = {};
                this.logService.log("OpportunitySummary_getOppDetails loanOfficerObj: ", loanOfficerObj);
                if (loanOfficerObj.length > 0) {
                    officer.loanOfficerPic = "";
                    officer.loanOfficerName = loanOfficerObj[0].text;
                    officer.loanOfficerRole = "";
                }

                let currentUserId = userDetails.id;
                if (!currentUserId) {
                    let userpro = await this.authHelper.callGetUserProfile();
                    currentUserId = userpro.id;
                }
                this.logService.log("OpportunitySummary_getOppDetails currentUserId: ", currentUserId);
                let teamMemberDetails = teamMembers.filter(function (k) {
                    return k.id === currentUserId;
                });
                let userAssignedRole = teamMemberDetails.displayName;
                this.logService.log("OpportunitySummary_getOppDetails teamMemberDetails: ", teamMemberDetails);
                // Check access to edit dealtype
                this.authHelper.callCheckAccess(["Opportunity_ReadWrite_Dealtype"]).then((data) => {
                    this.setState({ haveAccessToEditDealType: data });
                }).catch(err => { this.setState({ haveAccessToEditDealType: false }); });

                // Check access to edit team member
                this.authHelper.callCheckAccess(["Opportunity_Readwrite_Team"]).then((data) => {
                    this.setState({ haveAccessToEditTeam: data });
                }).catch(err => { this.setState({ haveAccessToEditTeam: false }); });

                // Check access to change Status, enable loan officer  link
                this.authHelper.callCheckAccess(["Opportunity_Create"]).then((data) => {
                    this.setState({ haveAccessToChangeLO: data, haveAccessToChangeStatus: data });
                }).catch(err => { this.setState({ haveAccessToChangeStatus: false }); });

                this.setState({
                    teamMembers: teamMembers,
                    loanOfficer: loanOfficerObj.length === 0 ? loanOfficerObj : [],
                    showPicker: loanOfficerObj.length === 0 ? true : false,
                    userAssignedRole: userAssignedRole,
                    loading: false
                });
            } else
                throw Error("Data is null");
        }
        catch (err) {
            this.setState({
                loading: false
            });
            this.logService.log("OpportunitySummary_getOppDetails error : ", err);
            return;
        }

    }

    onChangeDealType(e) {
        this.logService.log(e);
        let selDealType = this.state.dealTypeItems.filter(function (d) {
            return d.id === e.key;
        });
        this.logService.log("OPportunity_summary onChangeDealType : ", selDealType);
        this.logService.log("OPportunity_summary oppData : ", this.state.oppData.template);

        let oppData = JSON.parse(JSON.stringify(this.state.oppData));
        oppData.template = selDealType[0];
        oppData.template.processes.forEach(obj => {
            if (obj.processStep === "Start Process") obj.status = 3;
        });
        this.setState({ oppData });
    }

    async startProcessClick() {

        this.setState({ isUpdateOpp: true, dealTypeUpdated : true });
        this.logService.log("OpportunitySummary_startProcessClick : ", this.state.oppData);
        let msg = "";
        let type = null;
        let dealTypeUpdated = false;
        try {
            await this.updateOpportunity(this.state.oppData);
            msg = "Opportunity Updated successfully.";
            type = MessageBarType.success;
            dealTypeUpdated = true;
        } catch (error) {
            this.logService.log("OpportunitySummary_startProcessClick : ", error.message);
            msg = error.message;
            type = MessageBarType.error;
            dealTypeUpdated = false;
        } finally {
            this.setState({
                isUpdateOpp: false,
                isUpdateOppMsg: true,
                updateOppMessagebarText: msg,
                updateMessageBarType: type,
                dealTypeUpdated
            });
            this.hideMessagebar();
        }

    }

    async updateOpportunity(opportunity) {
        this.logService.log("OpportubitySummary_updateOpportunity");
        try {
            return await this.apiService.callApi('Opportunity', 'PATCH', { body: JSON.stringify(opportunity)});
        } catch (error) {
            this.logService.log("OpportubitySummary_updateOpportunity error: ", error.message);
            throw new Error(error);
        }
    }

    hideMessagebar() {
        setTimeout(function () {
            this.setState({ isUpdateOpp: false, isUpdateOppMsg: false, updateOppMessagebarText: "", updateMessageBarType: "" });
            this.hidePending = false;
        }.bind(this), 3000);
    }

    toggleHiddenPicker() {
        this.setState({
            showPicker: !this.state.showPicker
        });
    }

    onMouseEnter() {
        let dealTypeSelectMsgShow = true;
        this.setState({ dealTypeSelectMsgShow });
    }

    onMouseLeave() {
        let dealTypeSelectMsgShow = false;
        this.setState({ dealTypeSelectMsgShow });
    }

    renderSummaryDetails(oppDeatils) {
        let loanOfficerArr = [];
        loanOfficerArr = this.utils.getLoanOficers(oppDeatils.teamMembers);
        this.logService.log("OPportunity_summary : renderSummaryDetails,loanOfficerArr ", oppDeatils);
        this.logService.log("Opportunity_summary : this.state.showPicker ", this.state.showPicker);
        let loanOfficerADName = <Trans>loanOfficer</Trans>; // TODO getting it from appsettings js
        if (loanOfficerArr.length > 0) {
             loanOfficerADName = loanOfficerArr[0].adGroupName;
        }

        let metaFields = oppDeatils.metaDataFields.map((field, index) => {
            if (field.values && !["Customer", "Opportunity"].includes(field.displayName))
                return (
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10' key={index}>
                    <Label><Trans>{field.displayName}</Trans> </Label>
                    <span>{field.values}</span>
                    </div>
                );
        });

        return (

            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 p10A'>
                <div className='ms-Grid-row bg-white'>
                    {metaFields}
                </div>
                <div className='ms-Grid-row bg-white none'>
                    <div className='ms-Grid ms-sm12 ms-md12 ms-lg12 pb10'>
                        &nbsp;
                    </div>
                </div>
                <div className='ms-Grid-row bg-white'>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg2 pb10'>
                        <I18n>
                            {
                                t => {
                                    return (
                                        <Dropdown
                                            label={t('status')}
                                            selectedKey={this.state.oppData.opportunityState}
                                            onChanged={(e) => this.onStatusChange(e)}
                                            id='statusDropdown'
                                            disabled={this.state.oppData.opportunityState === 1 || this.state.oppData.opportunityState === 3 || this.state.oppData.opportunityState === 5 || !this.state.haveAccessToChangeStatus}
                                            options={this.state.oppStatusAll}
                                        />
                                    );
                                }
                            }
                        </I18n>

                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg2 pb10'>
                        {this.state.isStatusUpdate
                            ? <div className='ms-BasicSpinnersExample'>
                                <Spinner size={SpinnerSize.small} label={<Trans>saving</Trans>} ariaLive='assertive' />
                            </div>
                            :
                            ""
                        }
                    </div>
                </div>
                <div className='ms-Grid-row bg-white none'>
                    <div className='ms-Grid ms-sm12 ms-md12 ms-lg12  '>
                        &nbsp;
                    </div>
                </div>
                <div className='ms-Grid-row bg-white'>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        <Label>{loanOfficerADName}</Label>
                        {
                            loanOfficerArr.length > 0 ?
                                <div>
                                    {this.state.showPicker ? "" :
                                        <div>
                                            <div>
                                                <Persona
                                                    {...{ imageUrl: loanOfficerArr[0].UserPicture }}
                                                    size={PersonaSize.size40}
                                                    text={loanOfficerArr[0].displayName}
                                                    secondaryText={loanOfficerADName}
                                                />
                                            </div>
                                            <div>
                                                <br />
                                                {
                                                    this.state.oppData.opportunityState === 10 || !this.state.haveAccessToChangeLO ?
                                                        <Link className="pull-left" disabled><Trans>change</Trans></Link>
                                                        :
                                                        <Link onClick={this.toggleHiddenPicker.bind(this)} className="pull-leftt pr100"><Trans>change</Trans></Link>
                                                }
                                            </div>
                                        </div>
                                    }
                                </div>
                                :
                                ""

                        }
                        {this.state.showPicker ?
                            <div>
                                {this.state.usersPickerLoading
                                    ? <div className='ms-BasicSpinnersExample'>
                                        <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                    </div>
                                    :
                                    <div>
                                        <PeoplePickerTeamMembers teamMembers={this.state.peopleList} onChange={(e) => this.fnChangeLoanOfficer(e)} defaultSelectedUsers={[]} apiService={this.props.apiService} logService={this.props.logService}/>
                                        <br />
                                        <PrimaryButton
                                            buttonType={0}
                                            onClick={this._fnUpdateLoanOfficer.bind(this)}
                                            disabled={(!(this.state.currentSelectedItems.length === 1))}
                                        >
                                            <Trans>save</Trans>
                                        </PrimaryButton>
                                    </div>
                                }
                                {
                                    this.state.isUpdate ?
                                        <Spinner size={SpinnerSize.large} label={<Trans>updating</Trans>} ariaLive='assertive' />
                                        : ""
                                }

                            </div>
                            : ""
                        }
                        <br />

                        {
                            this.state.result &&
                            <MessageBar
                                messageBarType={this.state.result.type}
                            >
                                {this.state.result.text}
                            </MessageBar>
                        }

                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        {
                            this.state.dealTypeLoading
                                ? <div className='ms-BasicSpinnersExample'>
                                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                </div>
                                :
                                <div className="dropdownContainer">
                                    <Dropdown
                                        placeHolder={<Trans>selectDealType</Trans>}
                                        label={<Trans>dealType</Trans>}
                                        defaultSelectedKey={this.state.oppData.template === null ? "" : this.state.oppData.template.id}
                                        disabled={this.state.oppData.templateLoaded || !this.state.haveAccessToEditDealType || this.state.dealTypeUpdated}
                                        options={this.state.dealTypeList.filter(name => !name.defaultTemplate)}
                                        onChanged={(e) => this.onChangeDealType(e)}
                                    />
                                    <br /><br />
                                    <TooltipHost content={<Trans>dealtypeselectmsg</Trans>} id="myID" calloutProps={{ gapSpace: 0 }}>
                                        <PrimaryButton
                                            disabled={this.state.oppData.templateLoaded || !this.state.haveAccessToEditDealType || this.state.dealTypeUpdated}
                                            onClick={(e) => this.startProcessClick()}
                                        >
                                            <Trans>save</Trans>
                                        </PrimaryButton>
                                    </TooltipHost>
                                    <br />
                                    {
                                        this.state.isUpdateOpp ?
                                            <div className='ms-BasicSpinnersExample'>
                                                <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                            </div>
                                            : ""
                                    }<br />
                                    {
                                        this.state.isUpdateOppMsg ?
                                            <MessageBar
                                                messageBarType={this.state.updateMessageBarType}
                                                isMultiline={false}
                                            >
                                                {this.state.updateOppMessagebarText}
                                            </MessageBar>
                                            : ""
                                    }<br />
                                    {
                                        this.state.dealTypeSelectMsgShow ? <MessageBar> {<Trans>dealtypeselectmsg</Trans>}</MessageBar> : ""
                                    }
                                </div>

                        }
                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg2 pb10'>
                        &nbsp;
                    </div>
                </div>
                <div className='ms-Grid-row bg-white none'>
                    <div className='ms-Grid ms-sm12 ms-md12 ms-lg12  '>
                        &nbsp;
                    </div>
                </div>

                <div className='ms-Grid-row bg-white'>
                    <div className='ms-Grid ms-sm12 ms-md12 ms-lg12'>
                        &nbsp;
                    </div>
                </div>

            </div>

        );
    }

    _renderSubComp() {
        let oppDetails = this.state.loading ? <div className='bg-white'><p><em>Loading...</em></p></div> : this.renderSummaryDetails(this.state.oppData);
        return (
            <div>
                <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg9 p-l-30 bg-grey'>
                    <div className='ms-Grid-row'>
                        <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg6 pageheading'>
                            {
                                typeof this.state.teamsContext !== 'undefined'
                                    ?
                                    <h3><Trans>opportunityDetails</Trans></h3>
                                    :
                                    <h3>{this.state.oppData.displayName}</h3>
                            }

                        </div>
                        <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg6'><br />
                            {
                                typeof this.state.teamsContext !== 'undefined'
                                    ?
                                    ""
                                    :
                                    <LinkRoute to={'./generalDashboardTab'} className='pull-right'><Trans>backToDashboard</Trans> </LinkRoute>
                            }

                        </div>
                    </div>
                    <div className='ms-Grid-row  p-r-10'>
                        {oppDetails}
                    </div>
                </div>
            </div>
        );
    }

    addBaseProcessPersonal(value, role, processstep) {

        let newMember = {};

        newMember.status = 0;
        newMember.id = value[0].id;
        newMember.displayName = value[0].text;
        newMember.mail = value[0].mail;
        newMember.userPrincipalName = value[0].userPrincipalName;
        newMember.roleId = role ? role.id : "";
        newMember.permissions = role ? role.permissions : [];
        newMember.teamsMembership = role ? role.teamsMembership : [];
        newMember.ProcessStep = processstep;
        newMember.roleName = role ? role.displayName : "";
        newMember.adGroupName = role ? role.displayName : "";

        return newMember;
    }

    fnChangeLoanOfficer(item) {
        this.setState({ currentSelectedItems: item });
        if (this.state.currentSelectedItems.length > 1) {
            this.setState({
                btnSaveDisable: true
            });
        } else {
            this.setState({
                btnSaveDisable: false
            });
        }
    }

    async _fnUpdateLoanOfficer() {
        let oppDetails = this.state.oppData;
        let selLoanOfficer = this.state.currentSelectedItems;

        this.setState({
            loanOfficerName: selLoanOfficer[0].text,
            loanOfficerPic: '', 
            loanOfficerRole: userRoles[0]
        });
        this.logService.log(selLoanOfficer);
        let role = selLoanOfficer[0].userRoles.find(role => {
            if (role.permissions.find(permission => permission.name === "Opportunity_ReadWrite_Dealtype"))
                return role.id;
        });
        
        let updatedTeamMembers = oppDetails.teamMembers;
        let exitingLoanOfficer = this.utils.getLoanOficers(oppDetails.teamMembers);
        if (exitingLoanOfficer.length > 0) {
            updatedTeamMembers = updatedTeamMembers.filter(t => t.mail !== exitingLoanOfficer[0].mail);
        }
        // Process : Start Process
        updatedTeamMembers.push(this.addBaseProcessPersonal(selLoanOfficer, role, "Start Process"));
        //Process Customer Decision
        updatedTeamMembers.push(this.addBaseProcessPersonal(selLoanOfficer, role, "Customer Decision"));

        oppDetails.teamMembers = updatedTeamMembers;
        this.logService.log(oppDetails.teamMembers);

        await this.fnUpdateOpportunity(oppDetails, "LO");
    }

    onStatusChange = async (event) =>
    {
        let oppDetails = this.state.oppData;
        oppDetails.opportunityState = event.key;

        await this.fnUpdateOpportunity(oppDetails, "Status");
    }

    async fnUpdateOpportunity(oppViewData, Updtype) {

        if (Updtype === "LO") {
            this.setState({ isUpdate: true, showPicker: true });
        }
        else if (Updtype === "Status") {
            this.setState({ isStatusUpdate: true });
        }

        try {
            await this.updateOpportunity(oppViewData);
        } catch (e) {
            this.logService.log("error", e);
        } finally {
            if (Updtype === "LO") {
                this.setState({ isUpdate: false, showPicker: false, loading: false });
            }
            else if (Updtype === "Status") {
                this.setState({ isStatusUpdate: false });
            }
        }
    }

    render() {
        let filteredTeammembers =  [];
        let tempArray = [];
        if(!this.state.loading){
            filteredTeammembers = JSON.parse(JSON.stringify(this.state.oppData.teamMembers));
            if(!this.state.loading)
                filteredTeammembers = filteredTeammembers.filter(obj => {
                    let key = obj.displayName.toLowerCase() + obj.adGroupName.toLowerCase();
                if(!tempArray.includes(key)){
                    tempArray.push(key);
                    return obj;
                }
            });
            this.logService.log("General_createopportunity metadata filteredTeammembers: ", filteredTeammembers);
            this.logService.log("General_createopportunity metadata teamMembers: ", this.state.oppData.teamMembers);
        }

        const TeamMembersView = () => {
            return (
                <TeamMembers
                    memberslist={filteredTeammembers}
                    createTeamId={this.state.oppData.id}
                    opportunityName={this.state.oppData.displayName}
                    opportunityState={this.state.oppData.opportunityState}
                    haveAccessToEditTeam={this.state.haveAccessToEditTeam}
                    logService={this.props.logService}
                />
            );
        };

        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label='loading...' ariaLive='assertive' />
                </div>
            );
        } else {
            return (
                <div className='ms-Grid'>
                    <div className='ms-Grid-row'>
                        {this._renderSubComp()}
                        {
                            typeof this.state.teamsContext !== 'undefined' ?
                                <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg3 p-l-10 TeamMembersBG'>
                                    <h3><Trans>teamMembers</Trans></h3>
                                    <TeamMembersView />
                                </div> : null
                        }
                    </div>
                </div>
            );
        }
    }
}