/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';

import { IconButton } from 'office-ui-fabric-react/lib/Button';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { Checkbox } from 'office-ui-fabric-react/lib/Checkbox';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { TeamsComponentContext, ConnectedComponent, ThemeStyle, Panel, PanelHeader, PanelFooter, PanelBody } from 'msteams-ui-components-react';
import { Anchor } from 'msteams-ui-components-react';
import { getQueryVariable } from '../common';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import './checklist.css';
import { FilePicker } from '../components/FilePicker';
import Utils from '../helpers/Utils';
import { I18n, Trans } from "react-i18next";

//Granular Access Start
import AuthHelper from '../helpers/AuthHelper';
//Granular Access end

export class Checklist extends Component {
    displayName = Checklist.name

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

        let tmpChecklist = {
            id: "",
            checklistChannel: "",
            checklistStatus: 0
        };

        let tmpItems = [
            {
                key: 1,
                id: this.utils.guid(),
                completed: false,
                checklistItem: "",
                fileUri: "",
                file: {}
            }
        ];

        this.hidePending = false;

        const columns = [
            {
                key: 'column1',
                name: '',
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg1 mt6',
                fieldName: 'completed',
                minWidth: 30,
                maxWidth: 30,
                isRowHeader: false,
                isResizable: false,
                onRender: (item) => {
                    return (
                        this.state.opportunity.opportunityState === 8 || this.state.updateStatus === true
                            ?
                            <Checkbox
                                id={'chkCompleted' + item.id}
                                onChange={(e) => this.onCheckboxChange(e, item)}
                                ariaDescribedBy={'descriptionID'}
                                checked={item.completed}
                                disabled='true'
                            />
                            :
                            <Checkbox
                                id={'chkCompleted' + item.id}
                                onChange={(e) => this.onCheckboxChange(e, item)}
                                ariaDescribedBy={'descriptionID'}
                                checked={item.completed}
                            />
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>item</Trans>,
                headerClassName: 'ms-List-th textBoxHeader',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3 TextBoxAlignment',
                fieldName: 'checklistItem',
                minWidth: 150,
                maxWidth: 350,
                isRowHeader: false,
                isResizable: true,
                isCollapsable: true,
                onRender: (item) => {
                    return (
                        this.state.opportunity.opportunityState === 8 || this.state.updateStatus === true
                            ?
                            <TextField
                                id={'txtChecklistItem' + item.id}
                                defaultValue={item.checklistItem}
                                onBlur={(e) => this.onBlurChecklistItem(e, item)}
                                disabled='true'
                            />
                            :
                            <TextField
                                id={'txtChecklistItem' + item.id}
                                defaultValue={item.checklistItem}
                                onBlur={(e) => this.onBlurChecklistItem(e, item)}
                            />
                    );
                }
            },
            {
                key: 'column3',
                name: <Trans>file</Trans>,
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg4 Filearea ',
                fieldName: 'file',
                minWidth: 290,
                maxWidth: 380,
                isRowHeader: true,
                onRender: (item) => {
                    let itemFileUri = item.fileUri === "" ? "" : item.fileUri;
                    let fileName = "";

                    if (itemFileUri.length > 0) {
                        //fileName = itemFileUri.substring(itemFileUri.lastIndexOf('file='), itemFileUri.lastIndexOf('&action'));
                        //fileName = fileName.substring(5, fileName.length);

                        fileName = this.getDocumentName(itemFileUri);
                        if (!fileName) {
                            fileName = itemFileUri.substring(itemFileUri.lastIndexOf("/") + 1);
                        }
                    }

                    let uploadedFile = { "name": fileName };

                    return (
                        this.state.opportunity.opportunityState === 8 || this.state.updateStatus === true
                            ?
                            <FilePicker
                                id={'txtFile' + item.id}
                                fileUri={itemFileUri}
                                file={uploadedFile}
                                showBrowse='true'
                                showLabel='true'
                                onChange={(e) => this.onChangeFile(e, item)}
                                disabled='true'
                            />
                            :
                            <FilePicker
                                id={'txtFile' + item.id}
                                fileUri={itemFileUri}
                                file={uploadedFile}
                                showBrowse='true'
                                showLabel='true'
                                onChange={(e) => this.onChangeFile(e, item)}
                            />
                    );
                }
            },
            {
                key: 'column4',
                name: '',
                headerClassName: 'ms-List-th',
                className: 'DetailsListExample-cell--FileIcon ms-Grid-col ms-sm12 ms-md12 ms-lg1',
                minWidth: 20,
                maxWidth: 20,
                isRowHeader: false,
                isResizable: false,
                isCollapsable: true,
                onRender: (item) => {
                    return (
                        this.state.opportunity.opportunityState === 8
                            ?
                            <div>

                                <IconButton id={'btnDelete' + item.id} iconProps={{ iconName: 'Delete' }} onClick={e => this.deleteRow(item)} disabled />
                            </div>
                            :
                            <div>

                                <IconButton id={'btnDelete' + item.id} iconProps={{ iconName: 'Delete' }} onClick={e => this.deleteRow(item)} />
                            </div>
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
            checklist: tmpChecklist,
            items: tmpItems,
            rowItemCounter: 1,
            columns: columns,
            isCompactMode: false,
            fontSize: 16,
            theme: ThemeStyle.Light,
            checkListLoaded: false,
            spinnerLabel: <Trans>loading</Trans>,
            MessagebarText: '',
            fileIsUploading: false,
            selectedItemKey: 0,
            errorStatus: false,
            errorMessage: "",
            authorized: false,
            haveGranularAccess: false,
            isReadOnly: false
        };
    }


    componentWillMount() {
        console.log("checklist_componentWillMount isauth: " + this.authHelper.isAuthenticated());
    }

    componentDidMount() {
        console.log("checklist_componentDidMount isauth: " + this.authHelper.isAuthenticated());
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


    componentDidUpdate() {
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            console.log("checklist_componentDidUpdate callCheckAccess");
            this.accessGranted = true;
            let teamName = getQueryVariable('teamName');
            let channelName = getQueryVariable('channelName');
            this.getOpportunity(teamName, channelName);
        }

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

    userCheckAccess(oppObj) {
        return new Promise((resolve, reject) => {
            //channel name and permissions to check
            let channelName = this.state.channelName;
            let channelReadPermission = channelName + "_Read";
            let channelReadWritePermission = channelName + "_ReadWrite";
            let channelPermission = [channelReadWritePermission, channelReadPermission];
            let permissionRequired = ["Opportunity_ReadWrite_Partial", "Opportunity_Read_Partial", "Opportunities_ReadWrite_All"].concat(channelPermission);
            this.authHelper.callCheckAccess(permissionRequired).then(checkAccess => {
                if (checkAccess) {
                    resolve({
                        haveGranularAccess: checkAccess
                    });
                } else {
                    resolve({
                        haveGranularAccess: checkAccess
                    });
                }
            })
                .catch(err => {
                    this.errorHandler(err, "Checklist_checkUserAccess");
                    this.setState({
                        loading: false,
                        isReadOnly: true
                    });
                    this.hideMessagebar();
                    reject(err);
                });


        });
    }

    getOpportunity(oppName, channelName) {

        let opportunityObj;
        //changing to template string
        //let requestUrl = "api/Opportunity?name='" + oppName + "'";
        let requestUrl = `api/Opportunity?name=${oppName}`;
        fetch(requestUrl, {
            method: "GET",
            headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
        })
            .then(response => response.json())
            .then(data => {
                if (data === 'undefined' || data === null) {
                    console.log("Checklist_getOpportunity ERROR: can't load opp: " + oppName);
                    return;
                } else if (data.error) {
                    console.log(" ********** DATA Error");
                    console.log(data.error.code);
                    //alert(data.error.code);
                    this.setState({
                        isLoading: false,
                        teamName: oppName,
                        channelName: channelName
                    });
                }
                else {
                    this.setState({
                        channelName: channelName
                    });
                    opportunityObj = data;

                    let checkListObj = data.checklists;
                    if (checkListObj.length > 0) {
                        let checkListStatusKey = this.getChecklistStatus(opportunityObj);

                        let selChkItemInd;
                        for (let c = 0; c < checkListObj.length; c++) {
                            if (checkListObj[c].checklistChannel === channelName) {
                                selChkItemInd = c;
                                break;
                            }
                        }
                        if (selChkItemInd >= 0) {
                            let selChTaskList = checkListObj[selChkItemInd].checklistTaskList;

                            //assign checklistTask list to items in state only if the checkListTaskList array is not empty
                            // this is to avoid showing an empty checklist on the screen

                            for (let i = 0; i < selChTaskList.length; i++) {
                                selChTaskList[i].key = i + 1;
                            }
                            if (selChTaskList.length > 0) {
                                this.setState({
                                    opportunity: opportunityObj,
                                    checklist: checkListObj,
                                    items: selChTaskList,
                                    rowItemCounter: selChTaskList.length,
                                    selectedItemKey: checkListStatusKey,
                                    isLoading: false,
                                    checkListLoaded: true,
                                    haveGranularAccess: true,
                                    teamName: oppName,
                                    channelName: channelName
                                });
                            }
                            else {

                                this.setState({
                                    opportunity: opportunityObj,
                                    checklist: checkListObj,
                                    isLoading: false,
                                    checkListLoaded: true,
                                    haveGranularAccess: true,
                                    teamName: oppName,
                                    channelName: channelName
                                });

                            }
                        } else {

                            this.setState({

                                isLoading: false,
                                checkListLoaded: true,
                                haveGranularAccess: false,
                                teamName: oppName,
                                channelName: channelName
                            });

                        }

                    } else {
                        this.setState({
                            haveGranularAccess: false,
                            isLoading: false,
                            checkListLoaded: true,
                            teamName: oppName,
                            channelName: channelName
                        });

                    }
                }
            });
    }

    updateOpportunity(opportunity) {
        return new Promise((resolve, reject) => {
            // Foreach in opportunity.checklists to find this one then replace with state one, then replace items in checklist then add to oppotunity and update
            // when copy the items, get rid of file which holds the file for upload
            //this.state.items;

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
                    console.log("Checklist_updateOpportunity_fetch response: " + response.status + " - " + response.statusText);
                    if (response.status === 401) {
                        // TODO: For v2 see how we pass to authHelper to force token refresh
                    }
                    return response;
                })
                .then(data => {
                    resolve(data);
                })
                .catch(err => {
                    this.errorHandler(err, "Checklist_updateOpportunity");
                    this.setState({
                        updateStatus: true,
                        MessagebarText: <Trans>errorWhileUpdatingPleaseTryagain</Trans>
                    });
                    this.hideMessagebar();
                    reject(err);
                });
        });
    }

    updateCurrentItems(currItems, opportunity, updateOpp) {
        return new Promise((resolve, reject) => {
            if (opportunity === null) {
                opportunity = this.state.opportunity;
            }

            let currentItems = currItems.filter(x => x.id !== "");
            let currChecklist = opportunity.checklists.filter(x => x.checklistChannel === this.state.channelName);
            let checklists = opportunity.checklists.filter(x => x.checklistChannel !== this.state.channelName);

            currChecklist[0].checklistTaskList = currentItems;
            checklists.push(currChecklist[0]);
            opportunity.checklists = checklists;

            let checkListStatusKey = this.getChecklistStatus(opportunity);

            this.setState({
                opportunity: opportunity,
                checklist: checklists,
                items: currentItems,
                rowItemCounter: currentItems.length,
                selectedItemKey: checkListStatusKey,
                updateStatus: true,
                MessagebarText: <Trans>updatingChecklistItems</Trans>
            });

            if (updateOpp) {
                this.updateOpportunity(opportunity)
                    .then(res => {
                        //setTimeout(function () { this.setState({ updateStatus: false, MessagebarText: "" }); }.bind(this), 3000);
                        let teamName = getQueryVariable('teamName');
                        let channelName = getQueryVariable('channelName');
                        this.getOpportunity(teamName, channelName);
                        this.setState({ updateStatus: false, MessagebarText: "" });
                    })
                    .catch(err => {
                        // display error
                        this.setState({
                            updateStatus: true,
                            MessagebarText: <Trans>errorWhileUpdatingPleaseTryagain</Trans>
                        });
                        this.hideMessagebar();
                        reject(err);
                    });
            } else {
                this.setState({ updateStatus: false, MessagebarText: "" });
            }

            resolve(true);
        });
    }

    uploadFile(file, checklistItemId) {
        // Update fileUrl and upload file
        let fd = new FormData();
        fd.append('opportunity', "channel");
        fd.append('file', file);
        fd.append('opportunityName', this.state.opportunity.displayName);
        fd.append('fileName', file.name);
        let requestUrl = "api/document/UploadFile/" + encodeURIComponent(this.state.opportunity.displayName) + "/ChecklistDocument=" + this.state.channelName + "," + checklistItemId;

        let options = {
            method: "PUT",
            headers: {
                'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
            },
            body: fd
        };

        try {
            fetch(requestUrl, options)
                .catch(error => console.error('Error:', error))
                .then(response => {
                    if (response.ok) {
                        //TODO: Handle error
                        this.setState({ updateStatus: false, MessagebarText: "" });
                        return response;
                    } else {
                        console.log("Checklist_uploadFile_fetch response not ok:");
                        console.log(response);
                        this.setState({ updateStatus: true, MessagebarText: <Trans>errorWhileUploadingFile</Trans> });
                        this.hideMessagebar();
                        return response;
                    }
                }).then(json => {
                    if (json.ok) {
                        let fileUri = json.url;
                    }
                })
                .catch(err => {
                    console.log("Checklist_uploadFile_fetch Error:");
                    console.log(err);
                    this.setState({ updateStatus: true, MessagebarText: <Trans>errorWhileUploadingFile</Trans> });
                    this.hideMessagebar();
                });
        }
        catch (err) {
            console.log("Checklist_uploadFile_try Error:");
            console.log(err);
            this.setState({ updateStatus: true, MessagebarText: <Trans>errorWhileUploadingFile</Trans> });
            this.hideMessagebar();
            return false;
        }
    }

    getChecklistStatus(opportunity) {
        // divider: { key: 6, text: '-', itemType: DropdownMenuItemType.Divider },
        const checklistStatusOptions = [
            { key: 0, text: <Trans>Not Started</Trans> },
            { key: 1, text: <Trans>In Progress</Trans> },
            { key: 2, text: <Trans>Blocked</Trans> },
            { key: 3, text: <Trans>Completed</Trans> }
        ];

        let checkListObj = opportunity.checklists.filter(x => x.checklistChannel === this.state.channelName);
        let checkListStatusKey = 0;
        if (checkListObj.length > 0) {
            checkListStatusKey = checkListObj[0].checklistStatus;
        }


        return checkListStatusKey;
    }

    createListItem(key) {
        return {
            key: key,
            id: this.utils.guid(),
            completed: false,
            checklistItem: "",
            fileUri: "",
            file: { name: "" }
        };
    }

    onAddRow(e) {
        let rowCounter = this.state.rowItemCounter + 1;
        let newItems = [];
        newItems.push(this.createListItem(rowCounter));

        let currentItems = newItems.concat(this.state.items);

        this.updateCurrentItems(currentItems, null, false);
    }

    deleteRow(item) {
        if (this.state.items.length > 0) {
            this.setState({ updateStatus: true, MessagebarText: <Trans>updating</Trans> });

            let currentItems = this.state.items.filter(x => x.id !== item.id);

            this.updateCurrentItems(currentItems, null, true);
        }
    }


    onCheckboxChange(e, item) {
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
        if (currentItems[itemIdx].completed) {
            currentItems[itemIdx].completed = false;
        } else {
            currentItems[itemIdx].completed = true;
        }

        this.updateCurrentItems(currentItems, null, true);
    }

    onBlurChecklistItem(e, item) {
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

        if (e.target.value !== currentItems[itemIdx].checklistItem) {
            currentItems[itemIdx].checklistItem = e.target.value;
            this.updateCurrentItems(currentItems, null, true);
        }
    }

    onChangeFile(e, item) {
        if (item.checklistItem === "") {
            this.setState({
                errorStatus: true,
                errorMessage: <Trans>itemFieldCannotbeEmpty</Trans>
            });
            setTimeout(function () { this.setState({ errorStatus: false, errorMessage: "" }); }.bind(this), 3000);

            return;
        }

        this.setState({
            fileIsUploading: true, updateStatus: true, MessagebarText: <Trans>uploadingFile</Trans>
        });

        let currentItems = this.state.items;
        let itemIdx = currentItems.indexOf(item);
        currentItems[itemIdx].file = e;

        this.updateCurrentItems(currentItems, null, true)
            .then(res => {
                this.uploadFile(currentItems[itemIdx].file, currentItems[itemIdx].id);
            })
            .catch(err => {
                console.log("Checklist_onChangeFile Error:");
                console.log(err);
                this.setState({ updateStatus: true, MessagebarText: <Trans>errorWhileUploadingFile</Trans> });
                this.hideMessagebar();
            });
    }

    onStatusChange(e) {
        let currentItems = this.state.items;
        let checkListObj = this.state.checklist;
        let opportunity = this.state.opportunity;

        let selChkItemInd = 0;
        for (let c = 0; c < checkListObj.length; c++) {
            if (checkListObj[c].checklistChannel === this.state.channelName) {
                selChkItemInd = c;
                break;
            }
        }

        checkListObj[selChkItemInd].checklistStatus = e.key;
        opportunity.checklists = checkListObj;

        this.updateCurrentItems(currentItems, opportunity, true);
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

    getDocumentName(fileUri) {
        const vars = fileUri.split('&');
        for (const varPairs of vars) {
            const pair = varPairs.split('=');
            if (decodeURIComponent(pair[0]) === "file") {
                return decodeURIComponent(pair[1]);
            }
        }
    }


    render() {
        const { columns, isCompactMode, items, selectionDetails } = this.state;

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
                                        <div className='ms-Grid-row posAbs'>
                                            <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12 p-10' >
                                                <Panel>
                                                    <PanelHeader>
                                                        <div >
                                                            <h3>Checklist &nbsp;<Anchor className='' onClick={e => this.onAddRow(e)} ><i className="ms-Icon ms-Icon--Add font-16" aria-hidden="true" /> </Anchor></h3>
                                                        </div>
                                                    </PanelHeader>

                                                    <PanelBody>
                                                        <div>
                                                            <div className='ms-Grid-row ibox-content'>
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
                                                            <div className='ms-grid-row'>
                                                                <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg12'>
                                                                    <hr />
                                                                </div>
                                                            </div>
                                                            <div className='docs-TextFieldExample ms-Grid-col ms-sm6 ms-md8 ms-lg4'>
                                                                <I18n>
                                                                    {
                                                                        t => <Dropdown
                                                                            label={t('status')}
                                                                            selectedKey={this.state.selectedItemKey}
                                                                            onChanged={(e) => this.onStatusChange(e)}
                                                                            id='statusDropdown'
                                                                            options={
                                                                                [
                                                                                    { key: 0, text: t('Not Started') },
                                                                                    { key: 1, text: t('In Progress') },
                                                                                    { key: 2, text: t('Blocked') },
                                                                                    { key: 3, text: t('Completed') }
                                                                                ]
                                                                            }
                                                                        />

                                                                    }
                                                                </I18n>
                                                            </div>
                                                        </div>
                                                    </PanelBody>
                                                    <PanelFooter>
                                                        <div className='ms-Grid'>
                                                            <div className='ms-Grid-row'>
                                                                <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg8' />
                                                                <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg4'>
                                                                    {this.state.updateStatus === true ?
                                                                        <MessageBar messageBarType={MessageBarType.success} isMultiline={false}>
                                                                            {this.state.MessagebarText}
                                                                        </MessageBar>
                                                                        : ""
                                                                    }
                                                                </div>

                                                                <div className='ms-Grid-row'>
                                                                    <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg8' />
                                                                    <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg4'>
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
                                        : <div className="ms-Grid-col ms-sm12 ms-md8 ms-lg12 p-10"><h2><Trans>accessDenied</Trans></h2></div>
                            }
                        </div>
                    </div>
                </div>
            </TeamsComponentContext>
        );

    }
}