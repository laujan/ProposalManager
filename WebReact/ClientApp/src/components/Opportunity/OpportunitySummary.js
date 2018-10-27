/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { PrimaryButton, Button } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Link as LinkRoute } from 'react-router-dom';
import { TeamMembers } from '../../components/Opportunity/TeamMembers';
import {
    Persona,
    PersonaSize
} from 'office-ui-fabric-react/lib/Persona';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { userRoles, oppStatusText, oppStatusClassName, oppStatus } from '../../common';
import { PeoplePickerTeamMembers } from '../PeoplePickerTeamMembers';
import { I18n, Trans } from "react-i18next";
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import i18n from '../../i18n';


export class OpportunitySummary extends Component {
    displayName = OpportunitySummary.name
    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
		this.authHelper = window.authHelper;

		const userProfile = this.props.userProfile;
		
		const oppId = this.props.opportunityId;

		const opportunityData = this.props.opportunityData;

		const teamUrl = "https://teams.microsoft.com";

		let isAdmin = false;

		if (this.props.userProfile.roles.filter(x => x.displayName === "Administrator").length > 0) {
			isAdmin = true;
		}

		this.isDealTypeAlreadyUpdated = opportunityData.dealType===null?false:true;
        this.state = {
            loading: true,
            loadView: 'summary',
            menuLevel: 'Level2',
            LoanOfficer: [],
            teamMembers: [],
            oppId: oppId,
            showPicker: false,
            peopleList: [],
            mostRecentlyUsed: [],
            currentSelectedItems: [],
			oppData: opportunityData,
			btnSaveDisable: false,
			//userRoles: userProfile.roles,
			userId: userProfile.id,
            usersPickerLoading: true,
            loanOfficerPic: '',
            loanOfficerName: '',
            loanOfficerRole:'',
			teamUrl: teamUrl ,
			isAdmin: isAdmin,
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
            dealTypeSelectMsgShow:false,
            dealTypeUpdated :false
		};


		this.onStatusChange = this.onStatusChange.bind(this);
    }

    componentWillMount() {
        if (this.state.peopleList.length === 0) {
            this.getUserProfiles();
        }
        this.getDealTypeLists();
		this.getOppDetails();
		this.getOppStatusAll();
    }

	getOppStatusAll() {
		
		let requestUrl = 'api/context/GetOpportunityStatusAll';
			
			fetch(requestUrl, {
				method: "GET",
				headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
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

    getOppDetails() {
      
                    try {
						let data = this.state.oppData;
                        // filter loan officers

						let teamMembers = [];

						teamMembers = data.teamMembers;

						let loanOfficerObj = teamMembers.filter(function (k) {
                            return k.assignedRole.displayName === "LoanOfficer";
                        });
                        let officer = {};
                        if (loanOfficerObj.length > 0) {
                            officer.loanOfficerPic = "";
                            officer.loanOfficerName = loanOfficerObj[0].text;
                            officer.loanOfficerRole = "";
                        }
                       
						let currentUserId = this.state.userId;

						let teamMemberDetails = teamMembers.filter(function (k) {
							return k.id === currentUserId;
						});

						let userAssignedRole = teamMemberDetails[0].assignedRole.displayName;

                        this.setState({
                            loading: false,
                            teamMembers: teamMembers,
                           // oppData: data,
                            LoanOfficer: loanOfficerObj.length === 0 ? loanOfficerObj : [],
							showPicker: loanOfficerObj.length === 0 ? true : false,
							userAssignedRole: userAssignedRole

                        });
                    }
                    catch (err) {
                        //console.log("Error")
                    }
       
    }

    getUserProfiles() {
        let requestUrl = 'api/UserProfile/';
        fetch(requestUrl, {
            method: "GET",
            headers: {
                'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
            }
        })
            .then(response => {
                if (response.ok) {
                    return response.json();
                } else {
                    this.setState({ usersPickerLoading: false });
                    this.fetchResponseHandler(response, "getUserProfiles");
                    return [];
                }
            })
            .then(data => {
                let itemslist = [];

                if (data.ItemsList.length > 0) {
                    for (let i = 0; i < data.ItemsList.length; i++) {

                        let item = data.ItemsList[i];

                        let newItem = {};

                        newItem.id = item.id;
                        newItem.displayName = item.displayName;
                        newItem.mail = item.mail;
                        newItem.userPrincipalName = item.userPrincipalName;
                        newItem.userRoles = item.userRoles;

                        itemslist.push(newItem);
                    }
                }

                // filter to just loan officers
                //let filteredList = itemslist.filter(itm => itm.userRole === 1);
                
                this.setState({
                    usersPickerLoading: true,
                    peopleList: itemslist
                });

                this.filterUserProfiles();
            })
            .catch(err => {
                console.log("Opportunities_getUserProfiles error: " + JSON.stringify(err));
            });
    }

    filterUserProfiles() {
        let data = this.state.peopleList;
        let teamlist = [];

        for (let i = 0; i < data.length; i++) {
            let item = data[i];

            if ((item.userRoles.filter(x => x.displayName === "LoanOfficer")).length > 0) {
                teamlist.push(item);
            }
        }

        this.setState({
            usersPickerLoading: false,
            peopleList: teamlist
        });
    }

    fetchResponseHandler(response, referenceCall) {
        if (response.status === 401) {
            // TODO_ Next version handling of refresh token
        }
    }

    errorHandler(err, referenceCall) {
        console.log("Opportunities Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    // Get all DealTypes
    getDealTypeLists() {
       // return new Promise((resolve, reject) => {
            let opportunityObj;
            let requestUrl = "api/template/";

            fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            })
                .then(response => response.json())
                .then(data => {
                    //get dealtype list
                    try {
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
                            loading: false,
                            dealTypeItems: dealTypeItemsList,
                            dealTypeList: dealTypeList,
                            dealTypeLoading: false
                        });
                    }
                    catch (err) {
                        return false;
                    }

                })
                .catch(err => {
                    this.errorHandler(err, "getDealTypeList");
                    this.setState({
                        loading: false,
                        dealTypeItems: [],
                        dealTypeList: [],
                        dealTypeLoading: false
                    });
                    //reject(err);
                });
       // });
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
        this.setState({oppData});
    }

    startProcessClick() {
        console.log("this.state.oppData : ", this.state.oppData);
        // return false;
        this.setState({ isUpdateOpp: true , dealTypeUpdated : true, dealTypeSelectMsgShow:false});
        this.updateOpportunity(this.state.oppData)
            .then(res => {
                console.log(res);
                if (res.ok === true) {
                    // opportunity success
                    this.setState({ isUpdateOpp: false, 
                                isUpdateOppMsg: true, 
                                updateOppMessagebarText: "Opportunity Updated successfully.", 
                                updateMessageBarType: MessageBarType.success });
                }
                else {
                    this.setState({
                        isUpdateOpp: false,
                        isUpdateOppMsg: true,
                        updateOppMessagebarText: <Trans>errorWhileUpdatingPleaseTryagain</Trans>,
                        updateMessageBarType: MessageBarType.error
                    });
                    this.hideMessagebar();
                    //reject(err);
                }
                
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
                    this.errorHandler(err, "OppSummary_updateOpportunity");
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

    onMouseEnter(){
        let dealTypeSelectMsgShow = true;
        this.setState({dealTypeSelectMsgShow})
    }
    
    onMouseLeave(){
        let dealTypeSelectMsgShow = false;
        this.setState({dealTypeSelectMsgShow})
    }

    renderSummaryDetails(oppDeatils) {
        let loanOfficerArr = [];
        loanOfficerArr = oppDeatils.teamMembers.filter(function (k) {
			return k.assignedRole.displayName === "LoanOfficer";


        });
        
		let enableConnectWithTeam;
		if (this.state.oppData.opportunityState !== 1 ) {
			enableConnectWithTeam = true;
		}
		else {
			enableConnectWithTeam = false;
		}
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
                                                <br/>
                                                {
                                                    this.state.oppData.opportunityState === 10 ?
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
                                        defaultSelectedKey={this.state.oppData.dealType === null ? "" : this.state.oppData.dealType.id }
                                        disabled={
                                            this.state.oppData.opportunityState === 2 ||
                                            this.state.oppData.opportunityState === 3 || 
                                            this.state.oppData.opportunityState === 5 || 
                                            this.state.userAssignedRole.toLowerCase() !== "loanofficer" ||
                                            this.state.dealTypeUpdated  || this.isDealTypeAlreadyUpdated ? true : false}
                                        options={this.state.dealTypeList}
                                        onChanged={(e) => this.onChangeDealType(e)}
                                    />
                                </div>
                        }
                        <br /><br />
                        <PrimaryButton 
                                className='' 
                                disabled={
                                        this.state.oppData.opportunityState === 2 || 
                                        this.state.oppData.opportunityState === 3 || 
                                        this.state.oppData.opportunityState === 5 || 
                                        this.state.userAssignedRole.toLowerCase() !== "loanofficer" 
                                        || this.state.isUpdateOpp || this.isDealTypeAlreadyUpdated || this.state.dealTypeUpdated ? true : false
                                        }  
                                onClick      = {(e) => this.startProcessClick()}
                                onMouseEnter = {(e) =>this.onMouseEnter()}
                                onMouseLeave = {(e) =>this.onMouseLeave()}
                                        ><Trans>save</Trans>
                        </PrimaryButton> <br />
                        {
                            this.state.isUpdateOpp ? 
                                <div className='ms-BasicSpinnersExample'>
                                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                </div>
                                :""
                        }<br/>
                        {
                            this.state.isUpdateOppMsg ?
                                <MessageBar
                                    messageBarType={this.state.updateMessageBarType}
                                    isMultiline={false}
                                >
                                    {this.state.updateOppMessagebarText}
                                </MessageBar>
                                : ""
                        }<br/>
                        {
                            this.state.dealTypeSelectMsgShow ? <MessageBar> {<Trans>dealtypeselectmsg</Trans>}</MessageBar>: ""
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
					<div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg3 '>
                        {
							enableConnectWithTeam
                                ?
                                <a href={this.state.teamUrl} target="_blank" rel="noopener noreferrer"><PrimaryButton className='' ><Trans>connectWithTeam</Trans></PrimaryButton></a>
                                :
                                <PrimaryButton className='' disabled ><Trans>connectWithTeam</Trans></PrimaryButton>
                            
						}
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
        switch (this.state.loadView) {
            case 'summary': return (
                <div>
                    <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg9 p-5 bg-grey'>
                        <div className='ms-Grid-row'>
                            <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg6 pageheading'>
                                <h3><Trans>opportunityDetails</Trans></h3>
                            </div>
                            <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg6'><br />
                                <LinkRoute to={'/'} className='pull-right'><Trans>backToDashboard</Trans> </LinkRoute>
                            </div>
                        </div>
                        <div className='ms-Grid-row  p-r-10'>
                            {oppDetails}
                        </div>
                    </div>
                </div>

            );
            case 'chooseteam': return (
                <div>
                    <h2>Choose Team</h2>
                </div>
            );
            default:
                break;

        }
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
            loanOfficerName : selLoanOfficer[0].text,
            loanOfficerPic : '', //selLoanOfficer[0].imageUrl,
            loanOfficerRole : userRoles[0]
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
        this.fnUpdateOpportunity(oppDetails,"LO");
    }

	onStatusChange = (event) => {
		
		let oppDetails = this.state.oppData; 
		
		oppDetails.opportunityState = event.key;
		

		this.fnUpdateOpportunity(oppDetails,"Status");
		
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
				else if (Updtype === "Status")
				{
					this.setState({ isStatusUpdate: false});
				}
            });


    }

	render() {
		
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
						<div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg3 p-l-10 TeamMembersBG'>
							<h3>Team Members</h3>
                            <TeamMembers 
                                memberslist={this.state.oppData.teamMembers} 
                                createTeamId={this.state.oppData.id} 
                                opportunityName={this.state.oppData.displayName} 
                                opportunityState={this.state.oppData.opportunityState} 
                                userRole={this.state.userAssignedRole} />
						</div>
                    </div>
                </div>
            );
        }
    }
    
}