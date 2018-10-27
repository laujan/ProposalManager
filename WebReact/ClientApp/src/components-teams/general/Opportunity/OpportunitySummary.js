/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { PrimaryButton, Button } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Link as LinkRoute } from 'react-router-dom';
import { TeamMembers } from './TeamMembers';
import {
    Persona,
    PersonaSize
} from 'office-ui-fabric-react/lib/Persona';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { userRoles } from '../../../common';
import { PeoplePickerTeamMembers } from './PeoplePickerTeamMembers';
import { I18n, Trans } from "react-i18next";
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import i18n from '../../../i18n';
import { TooltipHost } from 'office-ui-fabric-react/lib/Tooltip';


export class OpportunitySummary extends Component {
    displayName = OpportunitySummary.name
    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;
        const opportunityData = this.props.opportunityData;
        const teamsContext = this.props.teamsContext;
        //this.isDealTypeAlreadyUpdated = opportunityData.dealType === null ? false : true;
        this.isDealTypeAlreadyUpdated = (opportunityData) ? (opportunityData.dealType === null ? false : true) : false;
        this.state = {
            teamsContext: teamsContext,
            loading: true,
            LoanOfficer: [],
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
            OppDetails: [],
            TeamMembersAll: [],
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
            isAuthenticated: false,
            isComponentDidUpdate: false,
            isRelationshipManager: false
        };


        this.onStatusChange = this.onStatusChange.bind(this);
    }


    async componentDidMount() {
        console.log("OpportunityDetails_componentWillMount isauth: " + this.authHelper.isAuthenticated() + " this.accessGranted: " + this.accessGranted);

        if (!this.state.isAuthenticated) {
            this.setState({
                isAuthenticated: this.authHelper.isAuthenticated()
            });
        }

    }

    componentWillReceiveProps(nextProps) {
        console.log("OpportunitySummary_componentWillReceiveProps : ", nextProps);
        console.log("Opportunity_summary_constructor : teamsContext : ", nextProps.teamsContext);
        this.isDealTypeAlreadyUpdated = (nextProps.opportunityData) ? (nextProps.opportunityData.dealType === null ? false : true) : false;
        this.setState({ oppData: nextProps.opportunityData });
    }


    async componentDidUpdate() {
        try {
            if (this.state.isAuthenticated && !this.state.isComponentDidUpdate && this.state.oppData) {
                console.log("OpportunitySummary_componentDidUpdate 1", this.state.loading, this.state.isComponentDidUpdate);
                let userProfile = await this.authHelper.callGetUserProfile();
                let value = await this.getUserProfiles();
                value = await this.getOppStatusAll();
                value = await this.getDealTypeLists();
                value = await this.getOppDetails(userProfile);
            } else {
                if (!this.state.oppData) {
                    console.log("OpportunitySummary_componentDidUpdate 2", this.state.loading, this.state.isComponentDidUpdate);
                    if (typeof this.state.teamsContext !== 'undefined' && this.state.loading) {
                        await this.getOpportunityForTeams(this.state.teamsContext.teamName);
                    }
                }
            }

        } catch (error) {
            console.log("OpportunitySummary_componentDidUpdate error : ", error);
        }

    }  

    async getOpportunityForTeams(teamname) {
        let oppData = "";
        let requestUrl = `api/Opportunity/?name=${teamname}`;
        console.log("OpportunitySummar_getOppDetails teamname :", requestUrl);
        try {
            let token = "";
            token = this.authHelper.getWebApiToken();
            console.log("OpportunitySummar_getOppDetails  token: ", token.length);
            let response = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + token }
            });
            oppData = await response.json();
            this.setState({ oppData });
            return oppData;
        }
        catch (err) {
            console.log("OpportunitySummar_getOppDetails err:", err);
            return oppData;
        }
    }

    async getOppStatusAll() {
        console.log("OpportunitySummary_getOppStatusAll ");
        let requestUrl = 'api/context/GetOpportunityStatusAll';

        try {
            let response = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            let data = await response.json();

            if (this.state.oppData.opportunityState !== 11) // if the current state is not archived, remove the archive option from the array
            {
                var filteredData = data.filter(x => x.Name !== 'Archived');
            }

            let oppStatusAll = [];
            for (let i = 0; i < filteredData.length; i++) {
                let oppStatus = {};
                oppStatus.key = data[i].Value;
                oppStatus.text = data[i].Name;
                oppStatusAll.push(oppStatus);
            }
            let loading = false;
            this.setState({ oppStatusAll });
            return true;
        } catch (error) {
            console.log("OpportunitySummary_getOppStatusAll error : ", error);
            return false;
        }
    }

    async getUserProfiles() {
        let requestUrl = 'api/UserProfile/';
        console.log("OpportunitySummary_getUserProfiles");
        try {
            let response = await fetch(requestUrl, {
                method: "GET",
                headers: {
                    'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
                }
            });
            console.log(response);
            let data = await response.json();
            let peopleList = [];

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
            let teamlist = [];
            for (let i = 0; i < peopleList.length; i++) {
                let item = peopleList[i];

                if (item.userRoles.filter(x => x.displayName === "LoanOfficer").length > 0) {
                    teamlist.push(item);
                }
            }
            this.setState({ peopleList: teamlist, usersPickerLoading: peopleList > 0 ? true : false, isComponentDidUpdate: true });
            return true;
        } catch (error) {
            console.log("OpportunitySummary_getUserProfiles error : ", error);
            return false;
        }


    }

    async getDealTypeLists() {
        let requestUrl = "api/template/";
        try {
            console.log("OpportunitySummary_getDealTypeLists");
            let response = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            let data = await response.json();
            let dealTypeItemsList = [];
            let dealTypeList = [];
            for (let i = 0; i < data.itemsList.length; i++) {
                dealTypeItemsList.push(data.itemsList[i]);
                let dealType = {};
                dealType.key = data.itemsList[i].id;
                dealType.text = data.itemsList[i].templateName;
                dealTypeList.push(dealType);
            }
            this.setState({
                dealTypeItems: dealTypeItemsList,
                dealTypeList: dealTypeList,
                dealTypeLoading: false
            });
            return true;
        } catch (error) {
            console.log("OpportunitySummary_getDealTypeLists error ", error);
            return false;
        }
    }

    async getOppDetails(userDetails) {

        try {
            let data = this.state.oppData;
            if (data) {
                console.log("OpportunitySummary_getOppDetails data: ", data);
                let teamMembers = [];
                teamMembers = data.teamMembers;
                let loanOfficerObj = teamMembers.filter(function (k) {
                    return k.assignedRole.displayName === "LoanOfficer";
                });
                let officer = {};
                console.log("OpportunitySummary_getOppDetails loanOfficerObj: ", loanOfficerObj);
                if (loanOfficerObj.length > 0) {
                    officer.loanOfficerPic = "";
                    officer.loanOfficerName = loanOfficerObj[0].text;
                    officer.loanOfficerRole = "";
                }

                let currentUserId = userDetails.id;
                if(!currentUserId){
                    let userpro = await this.authHelper.callGetUserProfile();
                    currentUserId = userpro.id;
                }
                console.log("OpportunitySummary_getOppDetails currentUserId: ", currentUserId);
                let teamMemberDetails = teamMembers.filter(function (k) {
                    return k.id === currentUserId;
                });
                let userAssignedRole = teamMemberDetails[0].assignedRole.displayName;
                console.log("OpportunitySummary_getOppDetails showPicker: ", loanOfficerObj.length === 0);
                
                // Loggedin user is RM
                let isRelationshipManager = userDetails.roles.filter(function (r) {
                    return r.displayName.toLowerCase() === 'relationshipmanager';
                });
                this.setState({
                    teamMembers: teamMembers,
                    LoanOfficer: loanOfficerObj.length === 0 ? loanOfficerObj : [],
                    showPicker: loanOfficerObj.length === 0 ? true : false,
                    userAssignedRole: userAssignedRole,
                    loading: false,
                    isRelationshipManager: isRelationshipManager.length > 0 ? true : false
                });
            } else
                throw Error("Data is null");
        }
        catch (err) {
            this.setState({
                loading: false
            });
            console.log("OpportunitySummary_getOppDetails error : ", err);
            return;
        }

    }

    onChangeDealType(e) {
        console.log(e);
        let selDealType = this.state.dealTypeItems.filter(function (d) {
            return d.id === e.key;
        });
        console.log(selDealType);
        //this.state.oppData.dealType.id = selDealType[0].id;
        let oppData = this.state.oppData;
        oppData.dealType = selDealType[0];
        this.setState({ oppData });
    }

    startProcessClick() {
        console.log("this.state.oppData : ", this.state.oppData);
        // return false;

        this.updateOpportunity(this.state.oppData)
            .then(res => {
                console.log(res);
                if (res.ok === true) {
                    // opportunity success
                    this.setState({
                        isUpdateOpp: false,
                        isUpdateOppMsg: true,
                        updateOppMessagebarText: "Opportunity Updated successfully.",
                        updateMessageBarType: MessageBarType.success
                    });
                }
                else {
                    this.setState({
                        isUpdateOpp: false,
                        isUpdateOppMsg: true,
                        updateOppMessagebarText: <Trans>DealTypeSelectMessage</Trans>,
                        updateMessageBarType: MessageBarType.error
                    });
                    this.hideMessagebar();
                    //reject(err);
                }
                this.setState({ isUpdateOpp: true, dealTypeUpdated: true, dealTypeSelectMsgShow: false });
                this.hideMessagebar();
            })
            .catch(err => {
                // display error
                this.setState({
                    isUpdateOpp: false,
                    isUpdateOppMsg: true,
                    updateOppMessagebarText: <Trans>errorWhileUpdatingPleaseTryagain</Trans>,
                    updateMessageBarType: MessageBarType.error
                });
                this.hideMessagebar();
                //reject(err);
            });
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

                    return response;
                })
                .then(data => {
                    resolve(data);
                })
                .catch(err => {
                    console.log("OpportunitySummary_updateOpportunity: error", JSON.stringify(err));
                    this.setState({
                        isUpdateOpp: false,
                        isUpdateOppMsg: true,
                        updateOppMessagebarText: <Trans>errorWhileUpdatingPleaseTryagain</Trans>,
                        updateMessageBarType: MessageBarType.error
                    });
                    this.hideMessagebar();
                    reject(err);
                });
        });
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
        loanOfficerArr = oppDeatils.teamMembers.filter(function (k) {
            return k.assignedRole.displayName === "LoanOfficer";


        });

        console.log("OPportunity_summary : renderSummaryDetails,loanOfficerArr ", loanOfficerArr);
        console.log("Opportunity_summary : this.state.showPicker ", this.state.showPicker);
        return (

            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 p10A'>
                <div className='ms-Grid-row bg-white pt15'>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        <Label><Trans>opportunityName</Trans> </Label>
                        <span>{oppDeatils.displayName}</span>
                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        <Label><Trans>clientName</Trans> </Label>
                        <span>{oppDeatils.customer.displayName}</span>
                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        <Label><Trans>openedDate</Trans>  </Label>
                        <span>{new Date(oppDeatils.openedDate).toLocaleDateString(i18n.language)} </span>
                    </div>
                </div>
                <div className='ms-Grid-row bg-white none'>
                    <div className='ms-Grid ms-sm12 ms-md12 ms-lg12 pb10'>
                        &nbsp;
                    </div>
                </div>
                <div className='ms-Grid-row bg-white'>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        <Label><Trans>dealSize</Trans> </Label>
                        <span>{oppDeatils.dealSize.toLocaleString()} </span>
                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        <Label><Trans>annualRevenue</Trans> </Label>
                        <span>{oppDeatils.annualRevenue.toLocaleString()}</span>
                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        <Label><Trans>targetDate</Trans> </Label>
                        <span>{new Date(oppDeatils.targetdate).toLocaleDateString(i18n.language)} </span>
                    </div>

                </div>
                <div className='ms-Grid-row bg-white none'>
                    <div className='ms-Grid ms-sm12 ms-md12 ms-lg12 pb10'>
                        &nbsp;
                    </div>
                </div>
                <div className='ms-Grid-row bg-white'>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        <Label><Trans>industry</Trans> </Label>
                        <span>{oppDeatils.industry.name} </span>
                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md6 ms-lg4 pb10'>
                        <Label><Trans>region</Trans> </Label>
                        <span>{oppDeatils.region.name} </span>

                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg2 pb10'>
                        <I18n>
                            {
                                t =>
                                    <Dropdown
                                        label={t('status')}
                                        selectedKey={this.state.oppData.opportunityState}
                                        onChanged={(e) => this.onStatusChange(e)}
                                        id='statusDropdown'
                                        disabled={this.state.oppData.opportunityState === 1 || this.state.oppData.opportunityState === 3 || this.state.oppData.opportunityState === 5 || this.state.userAssignedRole.toLowerCase() !== "relationshipmanager" ? true : false}
                                        options={this.state.oppStatusAll}
                                    />
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
                        <Label><Trans>margin</Trans> ($M) </Label>
                        <span>{oppDeatils.margin}</span>
                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        <Label><Trans>rate</Trans> </Label>
                        <span>{oppDeatils.rate} </span>
                    </div>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>
                        <Label><Trans>debtRatio</Trans> </Label>
                        <span>{oppDeatils.debtRatio}</span>
                    </div>

                </div>
                <div className='ms-Grid-row bg-white none'>
                    <div className='ms-Grid ms-sm12 ms-md12 ms-lg12  '>
                        &nbsp;
                    </div>
                </div>
                <div className='ms-Grid-row bg-white'>
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg4 pb10'>

                        <Label><Trans>loanOfficer</Trans> </Label>

                        {
                            //loanOfficerName.length > 0 ?
                            loanOfficerArr.length > 0 ?
                                <div>
                                    {this.state.showPicker ? "" :
                                        <div>
                                            <div>
                                                <Persona
                                                    {...{ imageUrl: loanOfficerArr[0].UserPicture }}
                                                    size={PersonaSize.size40}
                                                    text={loanOfficerArr[0].displayName}
                                                    secondaryText="Loan Officer"
                                                />
                                            </div>
                                            <div>
                                                <br />
                                                {
                                                    this.state.oppData.opportunityState === 10 || !this.state.isRelationshipManager ?
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
                                        <PeoplePickerTeamMembers teamMembers={this.state.peopleList} onChange={(e) => this.fnChangeLoanOfficer(e)} />
                                        <br />
                                        <Button
                                            buttonType={0}
                                            onClick={this._fnUpdateLoanOfficer.bind(this)}
                                            disabled={(!(this.state.currentSelectedItems.length === 1))}
                                        >
                                            <Trans>save</Trans>
                                        </Button>
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
                    <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg3 pb10'>
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
                                        defaultSelectedKey={this.state.oppData.dealType === null ? "" : this.state.oppData.dealType.id}
                                        disabled={
                                            this.state.oppData.opportunityState === 2 ||
                                                this.state.oppData.opportunityState === 3 ||
                                                this.state.oppData.opportunityState === 5 ||
                                                this.state.userAssignedRole.toLowerCase() !== "loanofficer" ||
                                                this.state.dealTypeUpdated || this.isDealTypeAlreadyUpdated ? true : false}
                                        options={this.state.dealTypeList}
                                        onChanged={(e) => this.onChangeDealType(e)}
                                    />
                                </div>
                        }
                        <br /><br />
                        <TooltipHost content={<Trans>dealtypeselectmsg</Trans>} id="myID" calloutProps={{ gapSpace: 0 }}>
                            <PrimaryButton
                                disabled={
                                    this.state.oppData.opportunityState === 2 ||
                                        this.state.oppData.opportunityState === 3 ||
                                        this.state.oppData.opportunityState === 5 ||
                                        this.state.userAssignedRole.toLowerCase() !== "loanofficer"
                                        || this.state.isUpdateOpp || this.isDealTypeAlreadyUpdated || this.state.dealTypeUpdated ? true : false
                                }
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

    _fnUpdateLoanOfficer() {
        let oppDetails = this.state.oppData; //oppData;
        let selLoanOfficer = this.state.currentSelectedItems;

        this.setState({
            loanOfficerName: selLoanOfficer[0].text,
            loanOfficerPic: '', //selLoanOfficer[0].imageUrl,
            loanOfficerRole: userRoles[0]
        });
        let updloanOfficer =
        {
            "id": selLoanOfficer[0].id,
            "displayName": selLoanOfficer[0].text,
            "mail": selLoanOfficer[0].mail,
            "phoneNumber": "",
            "userPrincipalName": selLoanOfficer[0].userPrincipalName,
            //"userPicture": selLoanOfficer[0].imageUrl,
            "userRole": selLoanOfficer[0].userRoles,
            "status": 0,
            "assignedRole": selLoanOfficer[0].userRoles.filter(x => x.displayName === "LoanOfficer")[0],
            "processStep": "Start Process"
        };

        let isLoanOfficerExists = false;
        for (let t = 0; t < oppDetails.teamMembers.length; t++) {
            if (oppDetails.teamMembers[t].assignedRole.displayName === "LoanOfficer") {
                oppDetails.teamMembers[t] = updloanOfficer;
                isLoanOfficerExists = true;
            }
        }

        if (!isLoanOfficerExists) {
            oppDetails.teamMembers.push(updloanOfficer);
        }

        this.setState({ memberslist: oppDetails.teamMembers });
        this.fnUpdateOpportunity(oppDetails, "LO");
    }

    onStatusChange = (event) => {

        let oppDetails = this.state.oppData;

        oppDetails.opportunityState = event.key;


        this.fnUpdateOpportunity(oppDetails, "Status");

    }


    fnUpdateOpportunity(oppViewData, Updtype) {

        if (Updtype === "LO") {
            this.setState({ isUpdate: true, showPicker: true });
        }
        else if (Updtype === "Status") {
            this.setState({ isStatusUpdate: true });
        }


        //let oppViewData = this.state.oppData;
        // oppViewData.teamMembers = updTeamMembersObj;

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
        };

        fetch(this.requestUpdUrl, options)
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    return response.json;
                } else {
                    //console.log('Error...: ');
                }
            }).then(json => {
                //console.log(json);
                if (Updtype === "LO") {
                    this.setState({ isUpdate: false, showPicker: false });
                }
                else if (Updtype === "Status") {
                    this.setState({ isStatusUpdate: false });
                }
            });


    }

    render() {

        const TeamMembersView = ({ match }) => {
            return (
                <TeamMembers
                    memberslist={this.state.oppData.teamMembers}
                    createTeamId={this.state.oppData.id}
                    opportunityName={this.state.oppData.displayName}
                    opportunityState={this.state.oppData.opportunityState}
                    userRole={this.state.userAssignedRole}
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