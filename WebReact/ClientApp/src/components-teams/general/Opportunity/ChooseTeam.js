/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox';
import { setIconOptions } from 'office-ui-fabric-react/lib/Styling';
import { Link as LinkRoute } from 'react-router-dom';
import { FilePicker } from '../FilePicker';
import { Persona, PersonaSize } from 'office-ui-fabric-react/lib/Persona';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { PeoplePickerTeamMembers } from '../PeoplePickerTeamMembers';
import { Trans } from "react-i18next";
import { getQueryVariable } from '../../../common';


export class ChooseTeam extends Component {
    displayName = ChooseTeam.name
    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;
        this.accessGranted = false;
        const oppID = getQueryVariable('opportunityId') ? getQueryVariable('opportunityId') : "";

        // Suppress icon warnings.
        setIconOptions({
            disableWarnings: false
        });

        this.state = {
            isChecked: false,
            checked: false,
            OfficersList: [],
            teamcount: 0,
            Team: [],
            selectedRole: {},
            selectorFiles: [],
            selectedTeamMember: '',
            filterOfficersList: [],
            currentSelectedItems: [],
            peopleList: [],
            OppDetails: {},
            mostRecentlyUsed: [],
            oppName: "",
            MessagebarText: "",
            MessagebarTextFinalizeTeam: "",
            MessageBarTypeFinalizeTeam: "",
            otherPeopleList: [],
            loading: true,
            usersPickerLoading: true,
            oppID: oppID,
            proposalDocumentFileName: "",
            UserRoleMapList: [],
            isEnableFinalizeTeamButton: false,
            TeamsObject: []
        };

        this.onFinalizeTeam = this.onFinalizeTeam.bind(this);
        this.handleFileUpload = this.handleFileUpload.bind(this);
        this.saveFile = this.saveFile.bind(this);
        this.selectedTeamMemberFromDropDown = this.selectedTeamMemberFromDropDown.bind(this);
    }

    async componentDidMount() {
        console.log("Dashboard_componentDidMount isauth: " + this.authHelper.isAuthenticated());
        if (this.authHelper.isAuthenticated()) {
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
    }

    async componentDidUpdate() {
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            this.accessGranted = true;
            await this.getUserRoles();
            await this.getOpportunity();
        }
    }

    async getOpportunity() {
        let requestUrl = 'api/Opportunity/?id=' + this.state.oppID;
        try {
            let response = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });

            let data = await response.json();
            let TeamsObject = await this.getUserProfiles();
            console.log("ChooseTeams_log getOpportunity : ", data);
            let oppSelTeam = [];
            if (data.teamMembers.length > 0) {
                for (let m = 0; m < data.teamMembers.length; m++) {
                    let item = data.teamMembers[m];
                    if (item.displayName.length > 0) {
                        oppSelTeam.push(item);
                    }
                }
            }
            console.log("ChooseTeams_Log getOpportunity : ", TeamsObject);
            console.log("ChooseTeams_Log getOpportunity : ", oppSelTeam);
            //TODO
            TeamsObject.forEach(team => {
                oppSelTeam.forEach(selectedTeam => {
                    if (selectedTeam.roleName && team.role) {
                        if (selectedTeam.roleName.toLowerCase() === team.role.toLowerCase()) {
                            selectedTeam.text = selectedTeam.displayName;
                            team.selectedMemberList.push(selectedTeam);
                        }                          
                    }
                });
            });


            let fileName = data.proposalDocument !== null ? this.getDocumentName(data.proposalDocument["documentUri"]) : "";

            this.setState({
                oppData: data,
                oppName: data.displayName,
                oppID: data.id,
                currentSelectedItems: oppSelTeam,
                loading: false,
                proposalDocumentFileName: fileName
            });

        } catch (error) {
            console.log("Choose Team Error : ", error.message);
        }

    }

    async getUserRoles() {
        //WAVE-4 : Changing RoleMappong to Roles:
        let requestUrl = 'api/Roles';
        try {
            let response = await fetch(requestUrl, { method: "GET", headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() } });
            let data = await response.json();
            console.log("ChooseTeams_Log getUserRoles : ", data);
            try {
                let userRoleList = [];
                for (let i = 0; i < data.length; i++) {
                    let userRole = {};
                    userRole.id = data[i].id;
                    userRole.roleName = data[i].displayName;
                    userRole.adGroupName = data[i].adGroupName;
                    userRole.permissions = data[i].permissions;
                    userRole.teamsMembership = data[i].teamsMembership;
                    userRoleList.push(userRole);
                }
                console.log("ChooseTeams_Log getUserRoles userRoleList: ", userRoleList);
                this.setState({ UserRoleMapList: userRoleList });
            }
            catch (err) {
                return false;
            }
        } catch (error) {
            return false;
        }
    }

    getDocumentName(fileUri) {
        const vars = fileUri.split('&');
        for (const varPairs of vars) {
            const pair = varPairs.split('=');
            if (decodeURIComponent(pair[0]) === "file") {
                return decodeURIComponent(pair[1]);
            }
        }
    }

    async getUserProfiles() {
        let requestUrl = 'api/UserProfile/';
        try {
            let response = await fetch(requestUrl, {
                method: "GET",
                headers: {
                    'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
                }
            });
            let data = await response.json();
            console.log("ChooseTeams_Log getUserProfiles : ", data);

            let itemslist = [];
            let TeamsObject = [];

            this.state.UserRoleMapList.forEach(role => {
                if (role.roleName.toLowerCase() !== "administrator") {
                    TeamsObject.push({ "role": role.roleName.toLowerCase(), "memberList": [], "selectedMemberList": [] });
                }
            });

            if (data.ItemsList.length > 0) {
                for (let i = 0; i < data.ItemsList.length; i++) {
                    let item = data.ItemsList[i];

                    TeamsObject.forEach(team => {
                        item.userRoles.forEach(role => {
                            if (role.displayName.toLowerCase() === team.role.toLowerCase())
                                team.memberList.push(item);
                        });
                    });

                    itemslist.push(item);
                }
            }

            console.log("ChooseTeams_Log getUserProfiles TeamsObject: ", TeamsObject);

            this.setState({
                allOfficersList: itemslist,
                usersPickerLoading: false,
                otherPeopleList: [],
                isDisableFinalizeTeamButton: TeamsObject.length > 0 ? false : true,
                TeamsObject: TeamsObject
            });

            return TeamsObject;

        } catch (error) {
            console.log("Opportunities_getUserProfiles error: " + JSON.stringify(error));
        }
    }

    saveFile() {
        let files = this.state.selectorFiles;
        for (let i = 0; i < files.length; i++) {
            let fd = new FormData();
            fd.append('opportunity', "ProposalDocument");
            fd.append('file', files[0]);
            fd.append('opportunityName', this.state.oppName);
            fd.append('fileName', files[0].name);

            this.setState({
                IsfileUpload: true
            });

            let requestUrl = "api/document/UploadFile/" + encodeURIComponent(this.state.oppName) + "/ProposalTemplate";

            let options = {
                method: "PUT",
                headers: {
                    'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
                },
                body: fd
            };
            try {
                fetch(requestUrl, options)
                    .then(response => {
                        if (response.ok) {
                            return response.json;
                        } else {
                            console.log('Error...: ');
                        }
                    }).then(data => {
                        this.setState({ IsfileUpload: false, fileUploadMsg: true, MessagebarText: <Trans>templateUploadedSuccessfully</Trans> });
                        setTimeout(function () { this.setState({ fileUploadMsg: false, MessagebarText: "" }); }.bind(this), 3000);
                    });
            }
            catch (err) {
                this.setState({
                    IsfileUpload: false,
                    fileUploadMsg: true,
                    MessagebarText: <Trans>errorWhileUploadingTemplatePleaseTryAgain</Trans>
                });
                //alert("Error Uploading File");
                return false;
            }
        }
    }

    handleFileUpload(file) {
        this.setState({ selectorFiles: this.state.selectorFiles.concat([file]) });
    }

    onFinalizeTeam() {
        let teamsSelected = this.state.currentSelectedItems;
        console.log("ChooseTeam_onFinalizeTeam teamsSelected : ", teamsSelected);
        this.setState({
            isFinalizeTeam: true
        });

        let data = this.state.oppData;
        data.teamMembers = teamsSelected;

        let fetchData = {
            method: 'PATCH',
            body: JSON.stringify(data),
            headers: {
                'Content-Type': 'application/json',
                'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
            }
        };

        let requestUrl = 'api/opportunity';

        fetch(requestUrl, fetchData)
            .catch(error => console.error('Error:', error))
            .then(response => {
                this.setState({ isFinalizeTeam: false, finazlizeTeamMsg: true, MessagebarTextFinalizeTeam: <Trans>finalizeTeamComplete</Trans>, MessageBarTypeFinalizeTeam: MessageBarType.success });
                setTimeout(function () {
                    this.setState({ finazlizeTeamMsg: false, MessagebarTextFinalizeTeam: "" });
                }.bind(this), 3000);
            });
    }

    selectedTeamMemberFromDropDown(item, roleName, processStep) {
        console.log("ChooseTeams_Log selectedTeamMemberFromDropDown item : ", item);
        console.log("ChooseTeams_Log selectedTeamMemberFromDropDown processStep : ", roleName);


        let tempSelectedTeamMembers = this.state.currentSelectedItems;
        let finalTeam = [];

        for (let i = 0; i < tempSelectedTeamMembers.length; i++) {

            if (tempSelectedTeamMembers[i].processStep !== roleName) {

                finalTeam.push(tempSelectedTeamMembers[i]);
            }
        }
        if (item.length === 0) {
            this.setState({
                currentSelectedItems: finalTeam
            });
            return;
        }
        else {
            let role = item[0].userRoles.find(role => {
                if (role.displayName.toLowerCase() === roleName.toLowerCase()) return role.id;
            });
            console.log("ChooseTeams_Log selectedTeamMemberFromDropDown role : ", role);
            let newMember = {};
            newMember.status = 0;
            newMember.id = item[0].id;
            newMember.displayName = item[0].text;
            newMember.mail = item[0].mail;
            newMember.userPrincipalName = item[0].userPrincipalName;
            newMember.roleId = role ? role.id : "";
            newMember.permissions = role ? role.permissions : [];
            newMember.teamsMembership = role ? role.teamsMembership : [];
            newMember.processStep = processStep;
            newMember.adGroupName = role.adGroupName;
            newMember.roleName = roleName;

            finalTeam.push(newMember);

            this.setState({
                currentSelectedItems: finalTeam
            });
        }
    }

    getPeoplePickerTeamMembers() {
        let processes = JSON.parse(JSON.stringify(this.state.oppData.template.processes));
        let teamMembersObject = JSON.parse(JSON.stringify(this.state.TeamsObject));

        let teammembertemplate = processes.map((process, index) => {
            if (process.processStep.toLowerCase() !== "start process" &&
                process.processType.toLowerCase() !== "none") {
                let members = teamMembersObject.find(team => {
                    if (process.roleName.toLowerCase() === team.role.toLowerCase()) {
                        return team;
                    }
                });
                console.log("getPeoplePickerTeamMembers : ", members);

                if (typeof members !== 'undefined') {
                    // get unique values from selectedMemberList
                    const selUsers = [];
                    const map = new Map();
                    for (const item of members.selectedMemberList) {
                        if (!map.has(item.id)) {
                            map.set(item.id, true);
                            selUsers.push(item);
                        }
                    }

                    return (
                        <div className='ms-Grid-col ms-sm11 ms-md11 ms-lg11 light-grey' key={index}>
                            <h5>{process.processStep}</h5>
                            <span className="p-b-10" />
                            <PeoplePickerTeamMembers
                                teamMembers={members.memberList}
                                defaultSelectedUsers={selUsers}
                                onChange={(e) => this.selectedTeamMemberFromDropDown(e, process.roleName, process.processStep)}
                                itemLimit={1}
                            />
                        </div>
                    );
                }
            }
        });
        teammembertemplate = teammembertemplate.filter(obj => typeof obj !== 'undefined');
        console.log("ChooseTeams_Log getPeoplePickerTeamMembers : ", teammembertemplate);
        return <div className='ms-Grid-row bg-white'>{teammembertemplate}</div>;
    }

    render() {
        let uploadedFile = { name: this.state.proposalDocumentFileName };
        let disableBrowseButton = false;

        if (!this.state.loading) {
            disableBrowseButton = this.state.oppData.proposalDocument === null ?
                true : this.state.oppData.proposalDocument.documentUri ? false : true;
            console.log("somevalue : ", this.state.oppData.proposalDocument === null);
        }

        let filteredTeammembers = JSON.parse(JSON.stringify(this.state.currentSelectedItems));
        let tempArray = [];
        filteredTeammembers = filteredTeammembers.filter(obj => {
            let key = obj.displayName.toLowerCase() + obj.adGroupName.toLowerCase();
            if (!tempArray.includes(key)) {
                tempArray.push(key);
                return obj;
            }
        });

        console.log("ChooseTeams_Log_render currentselected : ", this.state.currentSelectedItems);
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            return (
                <div className='ms-Grid'>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg8 '>
                            <div className='ms-Grid-row'>
                                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6 pageheading'>
                                    <h3><Trans>updateTeam</Trans></h3>
                                </div>
                                <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg6'><br />
                                    <LinkRoute to={"./rootTab?channelName=General&teamName=" + this.state.oppName} className='pull-right'> <Trans>backToOpportunity</Trans> </LinkRoute><br />
                                </div>
                            </div>
                            <div className='ms-Grid-row'>

                                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg3 hide'>
                                    <span><Trans>search</Trans></span>
                                    <SearchBox
                                        placeholder='Search'
                                        className='bg-white'
                                    />
                                </div>
                                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6 '>
                                    <span />
                                </div>
                            </div>

                            {
                                this.state.usersPickerLoading
                                    ?
                                    <div className='ms-Grid-row bg-white '>
                                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 TeamsBGnew pull-right pb15'>
                                            <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                                                <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                            </div>
                                        </div>
                                    </div>
                                    :
                                    <div>

                                        {this.getPeoplePickerTeamMembers()}

                                        <div className='ms-Grid-row bg-white'>
                                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg10 TeamsBGnew pb15'>
                                                {
                                                    this.state.isFinalizeTeam ?
                                                        <Spinner size={SpinnerSize.small} label={<Trans>finalizingTeam</Trans>} ariaLive='assertive' className="pull-right p-5" />
                                                        : ""
                                                }
                                                {
                                                    this.state.finazlizeTeamMsg ?
                                                        <MessageBar
                                                            messageBarType={this.state.MessageBarTypeFinalizeTeam}
                                                            isMultiline={false}
                                                        >
                                                            {this.state.MessagebarTextFinalizeTeam}
                                                        </MessageBar>
                                                        : ""
                                                }
                                            </div>
                                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg4 pull-right TeamsBGnew pb15'>

                                                <PrimaryButton onClick={this.onFinalizeTeam} className='pull-right' disabled={this.state.isFinalizeTeam || this.state.isDisableFinalizeTeamButton} ><Trans>finalizeTeam</Trans></PrimaryButton >

                                            </div>

                                        </div>
                                    </div>
                            }
                        </div>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg3 bg-white p10 pr0 pull-right'>
                            <div className='ms-Grid-row'>
                                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 pl0'>
                                    <h4 className='p15'> <Trans>selectedTeam</Trans></h4>
                                    {
                                        filteredTeammembers.map((member, index) =>
                                            member.displayName !== "" ?
                                                <div className='ms-Grid-col ms-sm6 ms-md4 ms-lg12 p15' key={index}>
                                                    <Persona
                                                        {...{ imageUrl: member.UserPicture, imageInitials: '' }}
                                                        size={PersonaSize.size40}
                                                        primaryText={member.displayName}
                                                        secondaryText={member.adGroupName}
                                                    />

                                                </div>
                                                : ""

                                        )

                                    }
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg8 mt20 '>
                            <div className='ms-Grid-row'>
                                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 pageheading bg-white pb20'>
                                    <h4 className=" mb0 pt15"><Trans>updateTemplate</Trans></h4>
                                    <div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg12 pt10 '>
                                        <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg7 pl0 pull-left' >
                                            <FilePicker
                                                id='filePicker'
                                                //Bug Fix, proposaldocument coming as null start
                                                fileUri={this.state.oppData.proposalDocument !== null ? this.state.oppData.proposalDocument.documentUri : ""}
                                                //Bug Fix, proposaldocument coming as null end
                                                file={uploadedFile}
                                                //Bug Fix, proposaldocument coming as null start
                                                showBrowse={
                                                    disableBrowseButton
                                                }
                                                //Bug Fix, proposaldocument coming as null end
                                                showLabel='true'
                                                onChange={(e) => this.handleFileUpload(e)}
                                                //Bug Fix, proposaldocument coming as null start
                                                btnCaption={!disableBrowseButton ? "Change File" : ""}
                                            //Bug Fix, proposaldocument coming as null end
                                            />
                                        </div>
                                        <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg5 '>
                                            {
                                                this.state.IsfileUpload ?
                                                    <Spinner size={SpinnerSize.small} ariaLive='assertive' className="pull-right p-5" />
                                                    : ""
                                            }


                                            <PrimaryButton className='pull-right' onClick={this.saveFile} disabled={
                                                //Bug Fix, proposaldocument coming as null start
                                                this.state.IsfileUpload ||
                                                (this.state.oppData.proposalDocument !== null ?
                                                    this.state.oppData.proposalDocument.documentUri ? true : false : false)
                                                //Bug Fix, proposaldocument coming as null end
                                            }
                                            >
                                                <Trans>save</Trans></PrimaryButton >
                                            {
                                                this.state.fileUploadMsg ?
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

                            </div>
                        </div>
                    </div>
                </div>

            );
        }
    }

}
