/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

/* eslint-disable radix */

import React, { Component } from 'react';
import {
    Pivot,
    PivotItem,
    PivotLinkFormat,
    PivotLinkSize
} from 'office-ui-fabric-react/lib/Pivot';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import { Link as LinkRoute } from 'react-router-dom';
import { Trans } from "react-i18next";
import { PrimaryButton, IconButton } from 'office-ui-fabric-react/lib/Button';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import TemplatesCommon from './TemplatesCommon';
import { ProcessTab } from './ProcessTab';
import ShowPreviewTemplateModel from './ShowPreviewTemplateModel';

export class AddTemplate extends Component {
    displayName = AddTemplate.name

    constructor(props) {
        super(props);

        this.authHelper = window.authHelper;
        this.apiService = this.props.apiService;
        this.templatesCommon = new TemplatesCommon(this.apiService);

        this.hardcodedGroupNos = [];
        this.state = {
            pageLoading: true,
            templateItems: [],
            template: {
                "id": "",
                "templateName": "",
                "description": "Process Description",
                "processes": []
            },
            orderNumber: 0,
            selectedOrderNumber: 0,
            processNumber: 1,
            processAlreadyExistErrText: "",
            isProcessExist: false,
            selectedProcessGroup: [],
            processGroupNumberList: [],
            showTemplateNameError: "",
            messagebarTemplateName: "",
            showModel: false,
            groupName: "",
            messagebarGroupName: "",
            showGroupNameError: false,
            processItems: [],
            enbaleOrderingBtwnProcess: false,
            originalGroupItems: [],
            groupItems: []
        };
    }

    async componentDidMount() {
        console.log("code-review commments implementation");
        console.log("AddTemplate_componentDidMount isauth: " + this.authHelper.isAuthenticated());
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            try {
                await this.authHelper.callCheckAccess(["Administrator", "Opportunity_ReadWrite_Template", "Opportunities_ReadWrite_All"]);
                console.log("Templatelist_componentDidUpdate callCheckAccess success");
                this.accessGranted = true;
                await this.fnGetTemplates();
                await this.fnGetProcess();
                await this.fnGetGroups();

            } catch (error) {
                this.accessGranted = false;
                console.log("Templatelist_componentDidUpdate error_callCheckAccess:");
                console.log(error);
            }
        }
    }

    componentDidUpdate() {
        console.log("AddTemplate_componentDidUpdate isauth: " + this.authHelper.isAuthenticated() + " this.accessGranted: " + this.accessGranted);
    }

    async fnGetTemplates() {
        let templateItems = [];
        templateItems = await this.templatesCommon.getAllTemplatesList();
        if (templateItems.length > 0) {
            this.setState({
                pageLoading: false,
                templateItems
            });
        } else {
            this.setState({
                pageLoading: false,
                items: templateItems
            });
        }

        console.log(this.state.templateItems);
    }

    async fnGetProcess() {
        let processItems = [], processGroupNumberList = [];
        processItems = await this.templatesCommon.getAllProcess();
        if (processItems.length > 0) {

            processItems.map((process, key) => {
                return processGroupNumberList.push(key + 1);
            });
            this.hardcodedGroupNos.forEach(value => { processGroupNumberList.splice(processGroupNumberList.indexOf(value), 1); });
            this.setState({
                pageLoading: false,
                processItems,
                processGroupNumberList
            });
        } else {
            this.setState({
                pageLoading: false,
                processItems,
                processGroupNumberList
            });
        }
        console.log(this.state.processGroupNumberList);
    }

    async fnGetGroups() {
        let groupItems;
        groupItems = await this.templatesCommon.getGroups();
        if (groupItems.length > 0) {
            groupItems = groupItems.map(group => { return { key: group.id, text: group.groupName, processes: group.processes }; });
            this.setState({
                pageLoading: false,
                groupItems,
                originalGroupItems: groupItems
            });
        } else {
            this.setState({
                pageLoading: false,
                groupItems,
                originalGroupItems: groupItems
            });
        }
    }

    setProcessGroupNumberNo(e) {
        let selectedProcessGroup = this.state.selectedProcessGroup;
        selectedProcessGroup.push(e);

        let curGroupItems = this.state.groupItems;
        let groupItems = curGroupItems.filter(g => g.text !== e.text);
        this.setState({ selectedProcessGroup, groupItems });
    }

    removeSelGroup(group) {
        console.log(group);
        let curGroupItems = this.state.groupItems;
        curGroupItems.push(group);
        let selGroups = this.state.selectedProcessGroup;
        selGroups = selGroups.filter(g => g.text !== group.text);
        this.setState({ selectedProcessGroup: selGroups, groupItems: curGroupItems });
    }

    onBlurTemplateName(e) {
        let template = JSON.parse(JSON.stringify(this.state.template));
        template.templateName = e.target.value;
        let showTemplateNameError = false;
        let messagebarTemplateName = "";
        if (template.templateName.length === 0) {
            messagebarTemplateName = <Trans>templateNameNotEmpty</Trans>;
            showTemplateNameError = true;
        }
        this.setState({ template, messagebarTemplateName, showTemplateNameError });
    }

    selectGroupWithProcess() {
        this.setState({ showModel: true, processNumber: 1, disableGroupListDropDown: false });
    }

    closeModal() {
        this.setState({ showModel: false });
    }

    showAddTemplateName() {
        return (
            <div className="ms-Grid-row bg-grey">
                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg4 pl10'>
                    <TextField
                        id='templateName'
                        label={<Trans>templateName</Trans>}
                        value={this.state.template.templateName}
                        onBlur={this.onBlurTemplateName.bind(this)}
                    />
                    {this.showMessageBar(this.state.showTemplateNameError, this.state.messagebarTemplateName)}
                </div>
                <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg6">
                    {this.showGroupNamedropdown()}
                </div>
                <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12 p15">
                    {this.state.template.processes.length === 0 ?
                        <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12 bg-white select-headin'>
                            <h5><Trans>Please select groups from drop down to add to the Template.</Trans></h5>
                        </div> : ""}
                </div>
            </div>
        );
    }

    showGroupNamedropdown() {
        return (
            <div className="ms-Grid-row bg-grey">
                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg4 pl10'>
                    <Dropdown
                        placeHolder="Select Group"
                        label="Select Group"
                        onChanged={(e) => this.setProcessGroupNumberNo(e)}
                        id='ddlProcessGroupNamedropdown'
                        options={this.state.groupItems}
                    />
                </div>
            </div>
        );
    }

    showMessageBar(flag, message) {
        return flag ?
            <MessageBar messageBarType={MessageBarType.error} isMultiline={false}>
                {message}
            </MessageBar>
            : null;
    }
    previewTemplateType() {
        let templateObj = {};
        let template = JSON.parse(JSON.stringify(this.state.template));
        let allTemplateProcess = [];
        this.state.selectedProcessGroup.map((g, gidx) => {
            let tempProcess = g.processes;
            tempProcess.map((p, pidx) => {
                p.groupNumber = gidx;
                p.processNumber = pidx;
                allTemplateProcess.push(p);
            });
        });
        templateObj.id = template.id || "";
        templateObj.templateName = template.templateName;
        templateObj.description = template.description;
        templateObj.processes = allTemplateProcess;
        templateObj.isSelectProcess = false;
        // Check "Select Process" exist in process list
        const checkSelectProcess = obj => obj.processStep === 'Selcect Process';
        if (checkSelectProcess) templateObj.isSelectProcess = true;
        console.log(templateObj);
        this.setState({ templateObj, showPreviewModel: true });
    }

    showPreviewModel() {
        return (
            <div className="ms-Grid-row bg-grey">
                <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12 p15 AddDealScrollEdit'>
                    <div className="ms-Grid-row p-10">
                        {ShowPreviewTemplateModel.call(this)}
                    </div>
                </div>
            </div>);
    }

    async saveTemplate() {
        this.setState({ isUpdate: true });
        let templateObject = this.state.templateObj;
        console.log(templateObject);

        this.apiService.callApi('Template', templateObject.id ? 'PATCH' : 'POST', { body: JSON.stringify(templateObject) })
            .then(() => {
                this.setMessage(false, true, MessageBarType.success, <Trans>dealTypeAddSuccess</Trans>);
            })
            .catch(error => {
                this.setMessage(false, true, MessageBarType.error, `${<Trans>errorOoccuredPleaseTryAgain</Trans>} : ${error.message}`);
            })
            .finally(() => {
                window.location = '/tab/generalConfigurationTab#templates';
            });
    }

    setMessage(isUpdate, isUpdateMsg, MessageBarType, MessagebarText) {
        //Show message
        this.setState({ isUpdate, isUpdateMsg, MessageBarType, MessagebarText });

        //Schedule message hide
        setTimeout(function () {
            this.setState({ isUpdate: false, isUpdateMsg: false, MessageBarType: "", MessagebarText: "" });
        }.bind(this), 3000);
    }

    closePreviewModal() {
        this.setState({ showPreviewModel: false });
    }

    editGroupProcess(groupNumber) {
        let orderNumber = groupNumber;
        this.setState({ orderNumber });
        let processNumber = 1;
        let template = JSON.parse(JSON.stringify(this.state.template));
        let selectedProcessGroup = [];
        for (let index = 0; index < template.processes.length; index++) {
            if (groupNumber === parseInt(template.processes[index].order)) {
                selectedProcessGroup.push(template.processes[index]);
                processNumber = parseInt((template.processes[index].order - parseInt(template.processes[index].order)) * 10) + 1;
            }
        }
        this.setState({ showModel: true, selectedProcessGroup, processNumber });
    }

    deleteGroup(groupNumber) {
        console.log("deleteGroup: ", groupNumber);
        let processGroupNumberList = this.state.processGroupNumberList.slice();
        let template = JSON.parse(JSON.stringify(this.state.template));
        let count = template.processes.filter(process => groupNumber === parseInt(process.order)).length;
        if (count > 1) {
            for (let index = 1; index < count; index++) {
                processGroupNumberList.push(processGroupNumberList[processGroupNumberList.length - 1] + 1);
            }
        }
        template.processes = template.processes.filter(process => groupNumber !== parseInt(process.order));
        processGroupNumberList.unshift(groupNumber);

        this.setState({ processGroupNumberList, template });
    }

    _processGrpObjtBasedOrderNo() {
        let template = JSON.parse(JSON.stringify(this.state.template));
        let tempObj = {};
        for (let index = 0; index < template.processes.length; index++) {
            let key = parseInt(template.processes[index].order);
            if (!(key in tempObj)) {
                tempObj[key] = [];
            }
            tempObj[key].push(template.processes[index]);
        }
        console.log("_processGrpObjtBasedOrderNo: ", tempObj);
        return tempObj;
    }


    showSelectedProcessTypeGroups() {
        let selGroups = this.state.selectedProcessGroup;
        return (
            <div className="ms-Grid-row bg-grey">
                <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12 p15 AddDealScrollEdit bg-white'>
                    {
                        selGroups.length > 0 ?
                            <div className="ms-Grid-row p-10">
                                {
                                    selGroups.map((g, idx) => {
                                        return (
                                            <div className="ms-Grid-col ms-sm4 ms-md4 ms-lg3 processBoxes" key={idx}>
                                                <div className="ms-Grid-row DealNameBG">
                                                    <div className="ms-Grid-col ms-sm4 ms-md4 ms-lg8">
                                                        <h5>{g.text}</h5>
                                                    </div>
                                                    <div className="ms-Grid-col ms-sm2 ms-md2 ms-lg4">
                                                        <IconButton iconProps={{ iconName: 'remove' }} className="pull-right" onClick={this.removeSelGroup.bind(this, g)} />
                                                    </div>
                                                </div>
                                                {
                                                    g.processes.length > 0 ?
                                                        <div className="ms-Grid-row bg-grey">
                                                            {
                                                                g.processes.map((p, pidx) => {
                                                                    return (
                                                                        <div className="ms-Grid-row p-10 " key={pidx}>

                                                                            <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12 processBg">
                                                                                <div className="ms-Grid-row ">
                                                                                    <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg8">
                                                                                        <h5 className="font12 font-normal">{p.processStep}</h5>
                                                                                    </div>
                                                                                </div>
                                                                            </div>
                                                                        </div>
                                                                    );
                                                                })
                                                            }
                                                        </div>
                                                        : ""
                                                }
                                            </div>
                                        );
                                    })
                                }
                            </div>
                            : ""
                    }
                    <div className="ms-Grid-row bg-grey p-10">
                        <div className='ms-Grid-col ms-sm4 ms-md6 ms-lg12'><br />
                            <PrimaryButton
                                text={<Trans>continue</Trans>}
                                onClick={this.previewTemplateType.bind(this)}
                                className="pull-right"
                                disabled={this.state.template.templateName.length === 0 || this.state.selectedProcessGroup.length === 0}
                            />
                        </div>
                    </div>
                </div>
            </div>);
    }

    renderDetails() {
        return (
            <div className='ms-Grid bg-white ibox-content border-none p-10'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 adddealtypeheading'>
                        <h3><span className="dealtype" ><Trans>templates</Trans><i className="ms-Icon ms-Icon--ChevronRightMed font-20" aria-hidden="true" /> </span><Trans>addTemplate</Trans></h3>
                    </div>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                        <LinkRoute to={'/tab/generalConfigurationTab#templates'} className='pull-right'><Trans>backToList</Trans> </LinkRoute>
                    </div>
                </div>

                <div className='ms-Grid-row pt10 '>
                    <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12 flexBoxy">
                        <div className='ms-Grid-row'>
                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 bg-white'>
                                <Pivot className='tabcontrols pt35' linkFormat={PivotLinkFormat.tabs} linkSize={PivotLinkSize.large} selectedKey={this.state.selectedTabName}>
                                    <PivotItem linkText={<Trans>Templates</Trans>} width='100%' itemKey="templateTab" >
                                        <div className='ms-Grid-row'>
                                            <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg4 bg-white'>
                                                <Dropdown
                                                    placeHolder={<Trans>selectTemplate</Trans>}
                                                    label={<Trans>templates</Trans>}
                                                    ariaLabel=""
                                                    value=''
                                                    options={this.state.templateItems.map(x => { return { 'key': x.id, 'text': x.templateName }; })}
                                                    componentRef=''
                                                    onChanged=""
                                                />
                                            </div>
                                            <div className='ms-Grid-col ms-sm8 ms-md8 ms-lg8 bg-white'>
                                                &nbsp;&nbsp;
                                            </div>
                                        </div>
                                        <div className='ms-Grid-row'>
                                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 bg-white'>
                                                {this.showAddTemplateName()}
                                                {this.showSelectedProcessTypeGroups()}
                                                {this.showPreviewModel()}
                                            </div>
                                        </div>
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>Process</Trans>} itemKey="processTab">
                                        <ProcessTab apiService={this.apiService} />
                                    </PivotItem>
                                </Pivot>
                            </div>
                        </div>
                    </div>
                </div>
            </div>);
    }

    render() {
        return (
            this.state.pageLoading ? this.templatesCommon.renderSpinner() : this.renderDetails()
        );
    }
}