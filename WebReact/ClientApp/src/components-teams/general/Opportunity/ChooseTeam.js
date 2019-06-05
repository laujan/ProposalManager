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
import { PeoplePickerTeamMembers } from './PeoplePickerTeamMembers';
import { Trans } from "react-i18next";
import { getQueryVariable } from '../../../common';

export class ChooseTeam extends Component {
    displayName = ChooseTeam.name
    constructor(props) {
        super(props);

        this.apiService = this.props.apiService;
        this.accessGranted = false;
        const oppID = getQueryVariable('opportunityId') ? getQueryVariable('opportunityId') : "";

        // Suppress icon warnings.
        setIconOptions({
            disableWarnings: false
        });

        this.state = {
            selectorFiles: [],
            currentSelectedItems: [],
            oppName: "",
            messagebarText: "",
            messagebarTextFinalizeTeam: "",
            messageBarTypeFinalizeTeam: "",
            otherPeopleList: [],
            loading: true,
            usersPickerLoading: true,
            oppID: oppID,
            proposalDocumentFileName: "",
            userRoleMapList: [],
            teamsObject: []
        };

        this.onFinalizeTeam = this.onFinalizeTeam.bind(this);
        this.handleFileUpload = this.handleFileUpload.bind(this);
        this.saveFile = this.saveFile.bind(this);
        this.selectedTeamMemberFromDropDown = this.selectedTeamMemberFromDropDown.bind(this);
    }

    async componentDidMount() {
        console.log("Dashboard_componentDidMount");

        this.accessGranted = true;
        await this.getUserRoles();
        await this.getOpportunity();
    }

    async getOpportunity() {
        try {
            let response = await this.apiService.callApi('Opportunity', 'GET', { query: `id=${this.state.oppID}` });
            if (response.ok) {
                let data = await response.json();

                let teamsObject = await this.getUserProfiles();
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
                console.log("ChooseTeams_Log getOpportunity : ", teamsObject);
                console.log("ChooseTeams_Log getOpportunity : ", oppSelTeam);
                //TODO
                teamsObject.forEach(team => {
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
            }
            else {
                console.log("ChooseTeam_getOpportunityForTeams error retrieving:", response.statusText);
            }
        } catch (error) {
            console.log("ChooseTeam_getOpportunityForTeams error:", error.message);
        }
    }

    async getUserRoles() {
        //WAVE-4 : Changing RoleMappong to Roles:
        try {
            let response = await this.apiService.callApi('Roles', 'GET', {});
            let userRoleList = [];
            if (response.ok) {
                let data = await response.json();

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
            }
            else {
                console.log("ChooseTeams_Log getUserRoles error retrieving roles: ", response.statusText);
            }

            this.setState({ userRoleMapList: userRoleList });
        } catch (error) {
            console.log("ChooseTeams_Log getUserRoles error: ", error);
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
        try {
            let response = await this.apiService.callApi('UserProfile', 'GET', {});
            let itemslist = [];
            let teamsObject = [];

            if (response.ok) {
                let data = await response.json();

                this.state.userRoleMapList.forEach(role => {
                    if (role.roleName.toLowerCase() !== "administrator") {
                        teamsObject.push({ "role": role.roleName.toLowerCase(), "memberList": [], "selectedMemberList": [] });
                    }
                });

                if (data.ItemsList.length > 0) {
                    for (let i = 0; i < data.ItemsList.length; i++) {
                        let item = data.ItemsList[i];

                        teamsObject.forEach(team => {
                            item.userRoles.forEach(role => {
                                if (role.displayName.toLowerCase() === team.role.toLowerCase())
                                    team.memberList.push(item);
                            });
                        });

                        itemslist.push(item);
                    }
                }
            }
            else {
                console.log("ChooseTeam_getUserProfiles error retrieving data: " + response.statusText);
            }

            this.setState({
                allOfficersList: itemslist,
                usersPickerLoading: false,
                otherPeopleList: [],
                isDisableFinalizeTeamButton: teamsObject.length > 0 ? false : true,
                teamsObject: teamsObject
            });

            console.log("ChooseTeams_Log getUserProfiles eamsObject: ", teamsObject);

            return teamsObject;

        } catch (error) {
            console.log("ChooseTeam_getUserProfiles error: " + JSON.stringify(error));
        }
    }

    async saveFile() {
        let files = this.state.selectorFiles;
        for (let i = 0; i < files.length; i++) {
            let fd = new FormData();
            fd.append('opportunity', "ProposalDocument");
            fd.append('file', files[0]);
            fd.append('opportunityName', this.state.oppName);
            fd.append('fileName', files[0].name);

            this.setState({
                isfileUpload: true
            });

            try {
                let response = await this.apiService.callApi('Document', 'PUT', { id: `UploadFile/${encodeURIComponent(this.state.oppName)}/ProposalTemplate`, formData: fd });

                if (response.ok) {
                    this.setState({ isfileUpload: false, fileUploadMsg: true, messagebarText: <Trans>templateUploadedSuccessfully</Trans> });
                    setTimeout(() => { this.setState({ fileUploadMsg: false, messagebarText: "" }); }, 3000);
                }
                else {
                    console.log("ChooseTeam_saveFile error: ", response.statusText);
                }
            }
            catch (err) {
                this.setState({
                    isfileUpload: false,
                    fileUploadMsg: true,
                    messagebarText: <Trans>errorWhileUploadingTemplatePleaseTryAgain</Trans>
                });
            }
        }
    }

    handleFileUpload(file) {
        this.setState({ selectorFiles: this.state.selectorFiles.concat([file]) });
    }

    async onFinalizeTeam() {
        let teamsSelected = this.state.currentSelectedItems;
        console.log("ChooseTeam_onFinalizeTeam teamsSelected : ", teamsSelected);
        this.setState({
            isFinalizeTeam: true
        });

        let data = this.state.oppData;
        data.teamMembers = teamsSelected;

        try {
            await this.apiService.callApi('Opportunity', 'PATCH', { body: JSON.stringify(data) });
            this.setState({ isFinalizeTeam: false, finalizeTeamMsg: true, messagebarTextFinalizeTeam: <Trans>finalizeTeamComplete</Trans>, messageBarTypeFinalizeTeam: MessageBarType.success });
            setTimeout(() => {
                this.setState({ finalizeTeamMsg: false, messagebarTextFinalizeTeam: "" });
            }, 3000);
        }
        catch (error) {
            console.error('ChooseTeam_onFinalizeTeam error:', error);
        }
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
        let teamMembersObject = JSON.parse(JSON.stringify(this.state.teamsObject));

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
                                apiService={this.props.apiService}
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
                                                    this.state.finalizeTeamMsg ?
                                                        <MessageBar
                                                            messageBarType={this.state.messageBarTypeFinalizeTeam}
                                                            isMultiline={false}
                                                        >
                                                            {this.state.messagebarTextFinalizeTeam}
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
                                                this.state.isfileUpload ?
                                                    <Spinner size={SpinnerSize.small} ariaLive='assertive' className="pull-right p-5" />
                                                    : ""
                                            }


                                            <PrimaryButton className='pull-right' onClick={this.saveFile} disabled={
                                                //Bug Fix, proposaldocument coming as null start
                                                this.state.isfileUpload ||
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
                                                        {this.state.messagebarText}
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