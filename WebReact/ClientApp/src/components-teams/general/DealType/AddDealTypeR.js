/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { DefaultButton, PrimaryButton, ActionButton } from 'office-ui-fabric-react/lib/Button';
import { Trans } from "react-i18next";
import { Link as LinkRoute } from 'react-router-dom';
import { AddProcessTypeR } from './AddProcessTypeR';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import ShowAddProcessModel from './ShowAddProcessModel';
import ShowPreviewModel from './ShowPreviewModel';
import { getQueryVariable } from '../../../common';
export class AddDealTypeR extends Component {
    displayName = AddDealTypeR.name
    constructor(props) {
        super(props);
        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;
        //hardcoded group numbers for "START PROCESS and NEW OPPORTUNITY".
        //To make it even more dynamic, we need to remove the use of this two arrays
        this.processTypesNotToDisplay = ["Base", "customerDecisionTab", "ProposalStatusTab"];
        this.hardcodedGroupNos = [1,2];
        this.state = {
            isUpdate: false, isUpdateMsg: false, MessageBarType: null, MessagebarText: "",
            pageLoading: true,
            processList: [],
            showModel: false,
            showPreviewModel: false,
            disableGroupListDropDown: true,
            messagebarDealTypeName: "",
            showdealTypeError: false,
            selectedProcessGroup: [],
            orderNumber: 0,
            selectedOrderNumber: 0,
            processNumber: 1,
            processAlreadyExistErrText: "",
            isProcessExist: false,
            enbaleOrderingBtwnProcess: false,
            processGroupNumberList: [],
            template: {
                "id": "",
                "templateName": "",
                "description": "test desc",
                "processes": []
            },
            dealTypeObj: {}
        };
        this.setProcessGroupNumberNo = this.setProcessGroupNumberNo.bind(this);
    }

    async componentDidMount() {
        let process = await this.getAllProcessFrmSharepoint();
        let dealTypeId = getQueryVariable('dealTypeId');
        console.log("componentDidMount><===", dealTypeId);
        if (dealTypeId !== null) {
            console.log("componentDidMount><===", dealTypeId);
            await this.getSelectedDealTypeById(dealTypeId);
        }

    }

    _findProcessInArray(tempList, process) {
        let index = tempList.findIndex(p => {
            return p.processStep === process.processStep;
        });
        return index;
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
        console.log("_processGrpObjtBasedOrderNo==>", tempObj);
        return tempObj;
    }

    setProcessGroupNumberNo(e) {
        console.log("setProcessGroupNumberNo>===", e.key);
        let orderNumber = this.state.orderNumber;
        if (orderNumber !== e.key) {
            orderNumber = e.key;
        }
        let selectedOrderNumber = e.key;
        this.setState({ orderNumber, selectedOrderNumber, disableGroupListDropDown: true });
    }

    addGroupProcess() {
        this.setState({ showModel: true, processNumber: 1, disableGroupListDropDown: false });
    }

    onBlurDealTypeName(e) {
        let template = JSON.parse(JSON.stringify(this.state.template));
        template.templateName = e.target.value;
        let showdealTypeError = false;
        let messagebarDealTypeName = "";
        if (template.templateName.length === 0) {
            messagebarDealTypeName = <Trans>dealTypeNameNotEmpty</Trans>;
            showdealTypeError = true;
        }
        this.setState({ template, messagebarDealTypeName, showdealTypeError });
    }

    closeModal() {
        this.setState({ showModel: false, selectedProcessGroup: [], orderNumber: 0 });
    }

    addProcess(process) {
        console.log("addProcess==>", process);
        let template = JSON.parse(JSON.stringify(this.state.template));
        let { processNumber, orderNumber } = this.state;
        let selectedProcessGroup = this.state.selectedProcessGroup.slice();
        let isProcessExist = false;
        let processAlreadyExistErrText = "";

        if (this.state.orderNumber === 0) {
            processAlreadyExistErrText = <Trans>Please set a group number</Trans>;
            isProcessExist = true;
        }

        if (this._findProcessInArray(template.processes, process) !== -1) {
            processAlreadyExistErrText = <Trans>processStepAlreadyExistOtherGroup</Trans>;
            isProcessExist = true;
        }

        if (this._findProcessInArray(selectedProcessGroup, process) !== -1) {
            processAlreadyExistErrText = <Trans>processStepAlreadyExist</Trans>;
            isProcessExist = true;
        }

        if (isProcessExist) {
            this.setState({ isProcessExist, processAlreadyExistErrText });
            setTimeout(function () { this.setState({ isProcessExist: false, processAlreadyExistErrText: "" }); }.bind(this), 3000);
            return false;
        } else {
            process.order = orderNumber + processNumber / 10;
            ++processNumber;
            selectedProcessGroup.push(process);
            this.setState({ selectedProcessGroup, processNumber });
        }
    }

    enableOrdering() {
        this.setState({ enbaleOrderingBtwnProcess: !this.state.enbaleOrderingBtwnProcess });
    }

    saveGroupWithProcess(selectedProcessGroup) {
        let template = JSON.parse(JSON.stringify(this.state.template));
        let processGroupNumberList = this.state.processGroupNumberList.slice();

        if (this.state.orderNumber === this.state.selectedOrderNumber) {//if(this.state.orderNumber === processGroupNumberList[0]){
            //adding a group
            processGroupNumberList.splice(processGroupNumberList.indexOf(this.state.orderNumber), 1);
            if (selectedProcessGroup.length > 1) {
                selectedProcessGroup.forEach((p, i) => {
                    if (i !== 0) processGroupNumberList.pop();
                });
            }
            selectedProcessGroup.forEach(process => {
                if (this._findProcessInArray(template.processes, process) === -1) {
                    template.processes.push(process);
                }
            });
        } else {
            //editing a group
            template.processes = template.processes.filter(process => {
                if (this.state.orderNumber === parseInt(process.order)) { // needs to change hre
                    let index = this._findProcessInArray(selectedProcessGroup, process);
                    if (index !== -1) {
                        process.order = selectedProcessGroup[index].order;
                        return process;
                    } else
                        processGroupNumberList.push(processGroupNumberList[processGroupNumberList.length - 1] + 1);
                } else
                    return process;
            });
        }
        template.processes.sort((pA, pB) => pA.order - pB.order);
        this.setState({ template, processNumber: 1, showModel: false, selectedProcessGroup: [], processGroupNumberList, orderNumber: 0 });
    }

    removeProcess(process) {
        let selectedProcessGroup = this.state.selectedProcessGroup.slice();

        if (selectedProcessGroup.length === 1) {
            let processAlreadyExistErrText = <Trans>Please remove the process group.</Trans>;
            this.setState({ isProcessExist: true, processAlreadyExistErrText });
            setTimeout(function () { this.setState({ isProcessExist: false, processAlreadyExistErrText: "" }); }.bind(this), 3000);
            return false;
        }

        let index = this._findProcessInArray(selectedProcessGroup, process);
        if (index !== -1) {
            selectedProcessGroup = selectedProcessGroup.filter(p => process.order !== p.order);
        }

        //re-order the whole group.
        selectedProcessGroup.sort((pA, pB) => pA.order - pB.order);
        selectedProcessGroup.forEach((process, index) => {
            process.order = this.state.orderNumber + (index + 1) / 10;
        });
        this.setState({ selectedProcessGroup, processNumber: selectedProcessGroup.length + 1 });
    }

    onBlurEstimatedDays(process, e) {
        console.log("onBlurEstimatedDays==>", process, e.target.value);

        let template = JSON.parse(JSON.stringify(this.state.template));
        let selectedProcessGroup = this.state.selectedProcessGroup.slice();

        let index = this._findProcessInArray(selectedProcessGroup, process);
        if (index !== -1) {
            selectedProcessGroup[index].daysEstimate = e.target.value;
        }

        index = this._findProcessInArray(template.processes, process);
        if (index !== -1) {
            template.processes[index].daysEstimate = e.target.value;
        }

        this.setState({ selectedProcessGroup, template });
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
        console.log("deleteGroup >++++++++", groupNumber);
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

    swapProcess(process, processNumber, direction) {
        console.log("swapProcess===> ", process, processNumber, direction);
        let selectedProcessGroup = this.state.selectedProcessGroup.slice();
        let index = -1;
        let tempOrder = 0;
        switch (direction) {
            case "DOWN":
                index = this._findProcessInArray(selectedProcessGroup, process);
                tempOrder = selectedProcessGroup[index + 1].order;
                selectedProcessGroup[index + 1].order = selectedProcessGroup[index].order;
                selectedProcessGroup[index].order = tempOrder;
                break;
            case "UP":
                index = this._findProcessInArray(selectedProcessGroup, process);
                tempOrder = selectedProcessGroup[index - 1].order;
                console.log("swapProcess===> ", index, tempOrder);
                selectedProcessGroup[index - 1].order = selectedProcessGroup[index].order;
                selectedProcessGroup[index].order = tempOrder;
                break;
            default:
                break;
        }
        selectedProcessGroup.sort((pA, pB) => pA.order - pB.order);
        this.setState({ selectedProcessGroup });
    }

    swapProcessGroup(group, groupNumber, direction) {
        let groupNo = parseInt(groupNumber);

        let template = JSON.parse(JSON.stringify(this.state.template));
        let newGroupNumber = -1;
        let groupNumberList = [];
        template.processes.forEach(p => {
            if (!groupNumberList.includes(parseInt(p.order))) groupNumberList.push(parseInt(p.order));
        });
        switch (direction) {
            case "LEFT":
                newGroupNumber = groupNumberList[groupNumberList.indexOf(groupNo) - 1];
                break;
            case "RIGHT":
                newGroupNumber = groupNumberList[groupNumberList.indexOf(groupNo) + 1];
                break;
            default:
                break;
        }
        console.log("swapProcessGroup===> ", newGroupNumber, groupNo, direction);
        let count = 1;
        template.processes = template.processes.map(process => {
            if (groupNo === parseInt(process.order)) {
                process.order = Math.round((0 + count++ / 10) * 10) / 10;
            }
            return process;
        });
        console.log("swapProcessGroup===> ", template.processes);
        template.processes = template.processes.map(process => {
            if (newGroupNumber === parseInt(process.order)) {
                process.order = process.order - newGroupNumber + groupNo;
            }
            return process;
        });
        console.log("swapProcessGroup===> ", template.processes);
        template.processes = template.processes.map(process => {
            if (0 === parseInt(process.order)) {
                process.order = process.order + newGroupNumber;
            }
            return process;

        });
        console.log("swapProcessGroup===> ", template.processes);
        template.processes.sort((pA, pB) => pA.order - pB.order);
        this.setState({ template });
    }

    previewDealType() {
        let dealTypeObj = {};
        let template = JSON.parse(JSON.stringify(this.state.template));
        let processList = this.state.processList.slice();
        let processGroupNumberList = this.state.processGroupNumberList.slice();

        dealTypeObj.id = template.id || "";
        dealTypeObj.templateName = template.templateName;
        dealTypeObj.description = template.description;
        dealTypeObj.processes = [];
        // Add NewOpportunity/Start process type to object - Add
        template.processes.forEach(process => {
            dealTypeObj.processes.push(process);
        });

        processList.forEach(process => {
            if (this.processTypesNotToDisplay.includes(process.processType)) {
                if (process.processType === "Base") {
                    if (process.processStep === "Start Process")
                        process.order = this.hardcodedGroupNos.length>0?this.hardcodedGroupNos[1]:processGroupNumberList.pop();
                    else if (process.processStep === "New Opportunity")
                        process.order = this.hardcodedGroupNos.length>0?this.hardcodedGroupNos[0]:processGroupNumberList.pop();
                    else
                        process.order = process.order || processGroupNumberList.shift();
                } else
                    process.order = process.order || processGroupNumberList.pop();

                process.status = 0;
                process.daysEstimate = 0;
                dealTypeObj.processes.push(process);
            }
        });

        dealTypeObj.processes.sort((pA, pB) => pA.order - pB.order);

        this.setState({ dealTypeObj, showPreviewModel: true, processGroupNumberList });
    }

    async saveDealType() {
        this.setState({ isUpdate: true });
        try {
            let dealTypeObject = this.state.dealTypeObj;
            let requestUpdUrl = 'api/template/';
            let options = {
                method: dealTypeObject.id ? "PATCH" : "POST",
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
                },
                body: JSON.stringify(dealTypeObject)
            };
            let response = await fetch(requestUpdUrl, options);
            this.setState({ MessagebarText: <Trans>dealTypeAddSuccess</Trans>, isUpdate: false, isUpdateMsg: true });

        } catch (error) {
            this.setState({
                MessagebarText: `${<Trans>errorOoccuredPleaseTryAgain</Trans>} : ${error.message}`,
                isUpdate: false,
                isUpdateMsg: true
            });
        } finally {
            setTimeout(function () {
                this.setState({ isUpdate: false, isUpdateMsg: false, MessageBarType: MessageBarType.success, MessagebarText: "" }
                );
            }.bind(this), 3000);
            window.location = '/tab/generalConfigurationTab#dealType';
        }
        return;
    }

    closePreviewModal() {
        this.setState({ showPreviewModel: false });
    }



    //api call to get all process types, by which we will create Deal Types
    async getAllProcessFrmSharepoint() {
        let processList = [], pageLoading = false, processGroupNumberList = [];
        try {
            let requestUrl = "api/process";
            let response = await fetch(
                requestUrl,
                {
                    method: "GET",
                    headers:
                    {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json',
                        'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
                    }
                });
            let data = await this.handleErrors(response).json();
            processList = data.itemsList.map((process, key) => {
                process.order = 0;
                process.status = 0;
                process.daysEstimate = 0;
                processGroupNumberList.push(key + 1);
                return process;
            }); 
            this.hardcodedGroupNos.forEach(value=>{ processGroupNumberList.splice(processGroupNumberList.indexOf(value),1);});

        } catch (error) {
            console.log(error.message);
        } finally {
            this.setState({ processList, pageLoading, processGroupNumberList });
        }
    }

    async getSelectedDealTypeById(dealTypeId) {
        let template = JSON.parse(JSON.stringify(this.state.template));
        let processGroupNumberList = this.state.processGroupNumberList.slice();
        try {
            let requestUrl = "api/template/";
            let response = await fetch(requestUrl, {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            let data = await this.handleErrors(response).json();

            let tempObj = data.itemsList.filter(templ => templ.id === dealTypeId);
            template.id = tempObj[0].id;
            template.templateName = tempObj[0].templateName;
            template.description = tempObj[0].description;

            tempObj[0].processes.slice().forEach((process, key) => {
                if (!this.processTypesNotToDisplay.includes(process.processType)) {
                    template.processes.push(process);
                    processGroupNumberList.splice(processGroupNumberList.indexOf(parseInt(process.order)),1);
                }
            });

            this.setState({ template, processGroupNumberList });
        } catch (error) {
            console.log("getSelectedDealTypeById>== error", error.message);
        }
    }

    //generic error handling functions
    handleErrors(response) {
        console.log("handleErrors==>", response);
        let ok = response.ok;
        if (!ok) {
            let status = response.status;
            let statusText = response.statusText;
            let type = response.type;
            if (status >= 500) {
                throw new Error(`ServerError: ErrorMsg ${statusText} & status code ${status}`);
            }
            if (status <= 501) {
                throw new Error(`ApplicationError: ErrorMsg ${statusText} & status code ${status}`);
            }
            throw new Error(`NetworkError: ErrorMsg ${statusText} & status code ${status}`);
        }
        return response;
    }

    renderSpinner() {
        return (
            <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
            </div>
        );
    }

    renderError() {

    }

    showMessageBar(flag, message) {
        return flag ?
            <MessageBar messageBarType={MessageBarType.error} isMultiline={false}>
                {message}
            </MessageBar>
            : null;
    }

    showProcessTypes() {
        return (
            <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12">
                <div className="ms-Grid-row bg-white">
                    {
                        this.state.processList.map((process, idx) => {
                            if (!this.processTypesNotToDisplay.includes(process.processType))
                                return (
                                    <div className="ms-Grid-col ms-sm4 ms-md4 ms-lg4 processBoxes" key={idx}>
                                        <div className="ms-Grid-row DealNameBG">
                                            <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12">
                                                <h5>{process.processStep}</h5>
                                            </div>
                                        </div>

                                    </div>
                                );
                        })
                    }
                </div>
            </div>
        );
    }

    showAddDealTypeGroup() {
        return (
            <div>
                <div className="ms-Grid-row bg-grey">
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg3 select-heading'>
                        <h3><Trans>selected</Trans></h3>
                    </div>
                    <div className='ms-Grid-col ms-sm5 ms-md5 ms-lg9 pull-right'>
                        <DefaultButton
                            iconProps={{ iconName: 'FabricNewFolder' }}
                            className="pull-right LinkAction-Button font10"
                            onClick={this.addGroupProcess.bind(this)}
                            text={<Trans>addGroup</Trans>}
                        />
                    </div>
                </div>
                <div className="ms-Grid-row bg-grey">
                    <div className='ms-Grid-col ms-sm12 ms-md7 ms-lg12 pull-left font12 pb15'>
                        <span><Trans>arrowsChangeTheOrder</Trans></span>
                    </div>
                </div>
            </div>
        );
    }

    showAddDealTypeName() {
        return (
            <div className="ms-Grid-row bg-grey">
                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg4 pl10'>
                    <TextField
                        id='dealTypeName'
                        label={<Trans>dealTypeName</Trans>}
                        value={this.state.template.templateName}
                        onBlur={this.onBlurDealTypeName.bind(this)}
                    />
                    {this.showMessageBar(this.state.showdealTypeError, this.state.messagebarDealTypeName)}
                </div>
                <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12 p15">
                    {this.state.template.processes.length === 0 ?
                        <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12 bg-white select-headin'>
                            <h5><Trans>addGroupMessage</Trans></h5>
                        </div> : ""}
                </div>
            </div>
        );
    }

    showSelectedProcessTypeGroups() {
        let processGroupObject = this._processGrpObjtBasedOrderNo();
        let keys = Object.keys(processGroupObject);
        return (
            <div className="ms-Grid-row bg-grey">
                <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12 p15 AddDealScrollEdit'>
                    <div className="ms-Grid-row p-10">
                        {
                            keys.map(key => {
                                return (
                                    <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg3 displayInline' key={key}>
                                        <div className="ms-Grid-row">
                                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg6'>
                                                {key === keys[0] ?
                                                    <ActionButton
                                                        onClick={this.swapProcessGroup.bind(this, processGroupObject[key], key, "RIGHT")}
                                                        className="f20 groupRight"
                                                    />
                                                    : key === keys[keys.length - 1] ?
                                                        <div >
                                                            <ActionButton
                                                                onClick={this.swapProcessGroup.bind(this, processGroupObject[key], key, "LEFT")}
                                                                className="f20 groupLeft"
                                                            />

                                                        </div>
                                                        :
                                                        <div className="ResponsiveArrowAlign">
                                                            <ActionButton
                                                                onClick={this.swapProcessGroup.bind(this, processGroupObject[key], key, "LEFT")}
                                                                className="f20 groupLeft"
                                                            /> &nbsp;&nbsp;&nbsp;&nbsp;
                                                            <ActionButton
                                                                onClick={this.swapProcessGroup.bind(this, processGroupObject[key], key, "RIGHT")}
                                                                className="f20 groupRight"
                                                            />
                                                        </div>

                                                }
                                            </div>
                                        </div>
                                        <div className="ms-Grid-row">
                                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg10'>
                                                <div className="ms-Grid-row bg-white">
                                                    <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12'>
                                                        <ActionButton
                                                            className="pull-right"
                                                            onClick={this.deleteGroup.bind(this, parseInt(key))}
                                                        >
                                                            <i className="ms-Icon ms-Icon--StatusCircleErrorX pull-right f20" aria-hidden="true" />
                                                        </ActionButton>
                                                    </div>
                                                    <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12">
                                                        {processGroupObject[key].map((process) => {
                                                            return (
                                                                <div className="bg-white" key={process.order}>
                                                                    {
                                                                        <AddProcessTypeR displayProcess={process} key={process.order} />
                                                                    }
                                                                </div>
                                                            );
                                                        }
                                                        )
                                                        }
                                                    </div>
                                                    <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12">
                                                        <i className="ms-Icon ms-Icon--Edit  f20 linkbutton f10" aria-hidden="true" onClick={this.editGroupProcess.bind(this, parseInt(key))} > <Trans>editGroup</Trans> </i>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                );
                            })
                        }
                    </div>
                    <div className="ms-Grid-row bg-grey p-10">
                        <div className='ms-Grid-col ms-sm4 ms-md6 ms-lg12'><br />
                            <PrimaryButton
                                text={<Trans>continue</Trans>}
                                onClick={this.previewDealType.bind(this)}
                                className="pull-right"
                                disabled={this.state.template.templateName.length === 0 || this.state.template.processes.length === 0}
                            />
                        </div>
                    </div>
                </div>
            </div>);
    }

    showProcessModel() {
        return (
            <div className="ms-Grid-row bg-grey">
                <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12 p15 AddDealScrollEdit'>
                    <div className="ms-Grid-row p-10">
                        {ShowAddProcessModel.call(this)}
                    </div>
                </div>
            </div>);
    }

    showPreviewModel() {
        return (
            <div className="ms-Grid-row bg-grey">
                <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12 p15 AddDealScrollEdit'>
                    <div className="ms-Grid-row p-10">
                        {ShowPreviewModel.call(this)}
                    </div>
                </div>
            </div>);
    }

    renderDetails() {
        return (
            <div className='ms-Grid bg-white ibox-content border-none p-10'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 adddealtypeheading'>
                        <h3><span className="dealtype" ><Trans>dealTypes</Trans><i className="ms-Icon ms-Icon--ChevronRightMed font-20" aria-hidden="true" /> </span><Trans>addDealType</Trans></h3>
                    </div>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                        <LinkRoute to={'/tab/generalConfigurationTab#dealType'} className='pull-right'><Trans>backToList</Trans> </LinkRoute>
                    </div>
                </div>

                <div className='ms-Grid-row pt10 '>
                    <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12 flexBoxy">
                        <div className='ms-Grid-row'>
                            {this.showProcessTypes()}
                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 bg-white'>
                                {this.showAddDealTypeGroup()}
                                {this.showAddDealTypeName()}
                                {this.showSelectedProcessTypeGroups()}
                                {this.showProcessModel()}
                                {this.showPreviewModel()}
                            </div>
                        </div>
                    </div>
                </div>
            </div>);
    }

    render() {
        return (
            this.state.pageLoading ? this.renderSpinner() : this.renderDetails()
        );
    }

}