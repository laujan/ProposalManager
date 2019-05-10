/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { DefaultButton, PrimaryButton, ActionButton } from 'office-ui-fabric-react/lib/Button';
import { Trans } from "react-i18next";
import { Modal } from 'office-ui-fabric-react/lib/Modal';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import { getAllProcess, getGroups, renderSpinner } from './TemplatesCommon';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import Utils from '../../../helpers/Utils';
import ProcessGroupModel from './ProcessGroupModel';


export class ProcessTab extends Component {
    displayName = ProcessTab.name

    constructor(props) {
        super(props);
        this.utils = new Utils();
        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;

        this.state = {
            pageLoading: true,
            processItems: [],
            showAddProcessModel: false,
            groupItems: [],
            processName: "",
            messagebarProcessName: "",
            showProcessNameError: false,
            groupName: "",
            messagebarGroupName: "",
            showGroupNameError: false,
            isProcessAdd: false,
            isGroupAdd: false,
            selectedProcessGroup: [],
            processGroupNumberList: [],
            showModel: false,
            templateItems: [],
            template: {
                "id": "",
                "templateName": "",
                "description": "Process Description",
                "processes": []
            },
            processAlreadyExistErrText: "",
            isProcessExist: false,
            dispGroup: []
            
        };
    }

    async componentDidMount() {
        console.log("code-review commments implementation");
        console.log("ProcessTab_componentDidMount isauth: " + this.authHelper.isAuthenticated());
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            try {
                await this.authHelper.callCheckAccess(["Administrator", "Opportunity_ReadWrite_Template", "Opportunities_ReadWrite_All"]);
                console.log("ProcessTab_componentDidMount callCheckAccess success");
                this.accessGranted = true;
                await this.fnGetProcess();
                await this.fnGetGroups();

            } catch (error) {
                this.accessGranted = false;
                console.log("ProcessTab_componentDidUpdate error_callCheckAccess:");
                console.log(error);
            }
        }
    }

    componentDidUpdate() {
        console.log("ProcessTab_componentDidUpdate isauth: " + this.authHelper.isAuthenticated() + " this.accessGranted: " + this.accessGranted);
    }

    async fnGetProcess() {
        let processItems = [];
        processItems = await getAllProcess();
        // Add Select Process - to add dynamically
        let selectProcess = {
            "channel": "Select Process",
            "daysEstimate": 0,
            "id": processItems.length + 1,
            "order": 0,
            "processStep": "Selcect Process",
            "processType": "CheckListTab",
            "status": 0
        };
        processItems.push(selectProcess);
        if (processItems.length > 0) {
            this.setState({
                pageLoading: false,
                processItems
            });
        } else {
            this.setState({
                pageLoading: false,
                processItems
            });
        }
    }

    async fnGetGroups() {
        let groupItems = [];
        groupItems = await getGroups();
        if (groupItems.length > 0) {
            groupItems = groupItems.map(group => { return { key: group.id, text: group.groupName, processes: group.processes }; });
            this.setState({
                pageLoading: false,
                groupItems
            });
        } else {
            this.setState({
                pageLoading: false,
                groupItems
            });
        }
    }

    addNewProcess() {
        this.setState({ showAddProcessModel: true, isProcessAdd: true });
    }

    closeModal() {
        this.setState({ showAddProcessModel: false, isProcessAdd: false, isGroupAdd: false, showModel: false});
    }

    onBlurProcessName(e) {
        let processName = e.target.value;
        let showProcessNameError = false;
        let messagebarProcessName = "";
        if (processName.length === 0) {
            messagebarProcessName = <Trans>processNameNotEmpty</Trans>;
            showProcessNameError = true;
        }
        this.setState({ processName, messagebarProcessName, showProcessNameError });
    }

    async saveNewProcess() {
        this.setState({ isUpdate: true });
        let id = this.state.processItems.length + 1;
        let value = this.state.processName;
        let requestUpdUrl = 'api/Process';

        try {
            //Checking item is already present
            if (this.checkProcessTypeIsAlreadyPresent(value)) return;

            let response = await fetch(requestUpdUrl, {
                method: "POST",
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
                },
                body: JSON.stringify({
                    "id": id, "processStep": value, "channel": value, "processType": "CheckListTab"
                })
            });
            this.utils.handleErrors(response);
            this.setMessage(false, true, MessageBarType.success, <Trans>processTypeAddSuccess</Trans>);
        } catch (error) {
            this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans> + " " + error.message);
        } finally {
            setTimeout(function () { this.setMessage(false, false, "", ""); }.bind(this), 2000);
            await this.fnGetProcess();
            this.setState({ processName: "", isProcessAdd: false, showAddProcessModel: false });
        }
        
    }

    checkProcessTypeIsAlreadyPresent(value) {
        let flag = false;
        let items = this.state.processItems.slice(0);
        let index = items.findIndex(process => process.processStep.toLowerCase() === value.toLowerCase());
        if (index !== -1) {
            this.setState({
                isUpdate: false,
                isUpdateMsg: true,
                MessagebarText: <Trans>processTypeAlreadyExist</Trans>,
                MessageBarType: MessageBarType.error
            });
            setTimeout(function () {
                this.setMessage(false, false, "", "");
                this.setState({ items });
            }.bind(this), 2000);
            flag = true;
        }
        return flag;
    }

    setMessage(isUpdate, isUpdateMsg, MessageBarType, MessagebarText) {
        this.setState({ isUpdate, isUpdateMsg, MessageBarType, MessagebarText });
    }

    // Groups functions
    addNewGroup() {
        this.setState({
            //showAddProcessModel: true, 
            //isGroupAdd: true, 
            showModel: true
        });
    }

    onBlurGroupName(e) {
        let groupName = e.target.value;
        let showGroupNameError = false;
        let messagebarGroupName = "";
        if (groupName.length === 0) {
            messagebarGroupName = <Trans>groupNameNotEmpty</Trans>;
            showGroupNameError = true;
        }
        let allGroups = this.state.groupItems;
        let grpIndex = allGroups.findIndex(g => {
            return g.text.toLowerCase() === groupName.toLowerCase();
        });
        console.log("Group check..... " + grpIndex); 
        //check group name exist or not
        if (grpIndex !== -1) {
            messagebarGroupName = <Trans>groupNameAlreadyExist</Trans>;
            showGroupNameError = true;
        } 

        this.setState({ groupName, messagebarGroupName, showGroupNameError });
    }

    saveNewGroup() {
        let allGroups = this.state.groupItems;
        allGroups.push({
            "id": allGroups.length + 1,
            "groupName": this.state.groupName
        }
        );
        this.setState({
            pageLoading: false,
            groupItems: allGroups,
            showAddProcessModel: false,
            isGroupAdd: false,
            showModel: false
        });

    }

    showMessageBar(flag, message) {
        return flag ?
            <MessageBar messageBarType={MessageBarType.error} isMultiline={false}>
                {message}
            </MessageBar>
            : null;
    }

    addProcess(process) {
        console.log("addProcess==>", process);
        let template = JSON.parse(JSON.stringify(this.state.template));
        let { processNumber, orderNumber } = this.state;
        let selectedProcessGroup = this.state.selectedProcessGroup.slice();
        console.log(selectedProcessGroup);
        let isProcessExist = false;
        let processAlreadyExistErrText = "";

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
        console.log("======> Selected process");
        console.log(this.state.selectedProcessGroup);
    }

    _findProcessInArray(tempList, process) {
        let index = tempList.findIndex(p => {
            return p.processStep === process.processStep;
        });
        return index;
    }

    enableOrdering() {
        this.setState({ enbaleOrderingBtwnProcess: !this.state.enbaleOrderingBtwnProcess });
    }

    async saveGroupWithProcess(selectedProcessGroup) {
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
        console.log(template);
        let allGroups = this.state.groupItems;
        

        let groupDetails = {};
        groupDetails.id = this.state.groupItems.length + 1;
        groupDetails.groupName = this.state.groupName;
        groupDetails.processes = template.processes;
        console.log(groupDetails);
        //allGroups.push(groupDetails);
        console.log(allGroups);
        console.log("============== Groups");


        try {

            let response = await fetch('api/Groups', {
                method: "POST",
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
                },
                body: JSON.stringify(groupDetails)
            });
            response = this.utils.handleErrors(response);
            console.log(response);
            this.setMessage(false, true, MessageBarType.success, <Trans>groupAddedSuccess</Trans>);
        } catch (error) {
            this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans> + " " + error.message);
        } finally {
            setTimeout(function () { this.setMessage(false, false, "", ""); }.bind(this), 2000);
            await this.fnGetGroups();
            this.setState({ groupName: "", isGroupAddUpdate: false, showModel: false, selectedProcessGroup: [] });
        }
        /*
         * this.setState({
            template,
            processNumber: 1,
            showModel: false,
            selectedProcessGroup: [],
            processGroupNumberList,
            orderNumber: 0,
            groupItems: allGroups
        });
        */
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

    showSelecteGroup(e) {
        let dispGroup = [];
        dispGroup.push(e);
        this.setState({ dispGroup });
    }
    showSelectedProcessTypeGroups() {
        //-- let processGroupObject = this._processGrpObjtBasedOrderNo();
        let selGroups = this.state.dispGroup;
        console.log(selGroups);
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
                                                    <div className="ms-Grid-col ms-sm2 ms-md2 ms-lg4"/>
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
                    
                </div>
            </div>);
    }

    render() {
        return (
            this.state.pageLoading ? renderSpinner() :
                <div className="ms-Grid-row bg-white">
                    <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12 pull-right">
                        <DefaultButton
                            iconProps={{ iconName: 'FabricNewFolder' }}
                            className="pull-right LinkAction-Button font10"
                            onClick={this.addNewProcess.bind(this)}
                            text={<Trans>addProcess</Trans>}
                        />
                    </div>
                    <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12">
                        <div className="ms-Grid-row bg-white">
                            {
                                this.state.processItems.map((process, idx) => {
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
                        <div className="ms-Grid-row bg-white">
                            <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12 pull-right"><br/>
                                <DefaultButton
                                    iconProps={{ iconName: 'FabricNewFolder' }}
                                    className="pull-right LinkAction-Button font10"
                                    onClick={this.addNewGroup.bind(this)}
                                    text={<Trans>addGroup</Trans>}
                                />
                            </div>
                            
                        </div>
                        <div className="ms-Grid-row bg-white">
                            <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12 pull-right">
                                <Dropdown
                                    placeHolder={<Trans>selectGroup</Trans>}
                                    label={<Trans>groupList</Trans>}
                                    ariaLabel=""
                                    value=''
                                    onChanged={(e) => this.showSelecteGroup(e)}
                                    options={this.state.groupItems}
                                    componentRef=''
                                />
                            </div>
                        </div>
                        <div className="ms-Grid-row bg-white">
                            <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12 pull-right">
                                {this.showSelectedProcessTypeGroups()}
                            </div>
                        </div>
                        <div className="ms-Grid-row bg-white">
                            <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12">
                                <div className="ms-Grid-row">
                                    <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg-12">
                                        {ProcessGroupModel.call(this)}
                                    </div>
                                </div>
                                <Modal isOpen={this.state.showAddProcessModel}
                                    onDismiss={this.closeModal}
                                    isBlocking='true'
                                    containerClassName="ms-modalExample-container"
                                >
                                    <div className="ms-modalExample-header">
                                        <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12'>
                                            <ActionButton className="pull-right" onClick={this.closeModal.bind(this)}>
                                                <i className="ms-Icon ms-Icon--StatusCircleErrorX pull-right f30" aria-hidden="true" />
                                            </ActionButton>
                                        </div>
                                    </div>
                                    <div className={this.state.isProcessAdd ? "ms-modalExample-body addProcessDiv" : "ms-modalExample-body addProcessDiv hide"} >
                                        <div className='ms-Grid-row'>
                                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                                                <div className='ms-BasicSpinnersExample p-10'>
                                                    {
                                                        this.state.isUpdate ?
                                                            <Spinner size={SpinnerSize.large} ariaLive='assertive' />
                                                            : ""
                                                    }
                                                    {
                                                        this.state.isUpdateMsg ?
                                                            <MessageBar
                                                                messageBarType={this.state.MessageBarType}
                                                                isMultiline={false}
                                                            >
                                                                {this.state.MessagebarText}
                                                            </MessageBar>
                                                            : ""
                                                    }
                                                </div>
                                            </div>
                                        </div>
                                        <div className="ms-Grid-row">
                                            <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg4'>
                                                <Trans>processName</Trans>
                                            </div>
                                        </div>
                                        <div className="ms-Grid-row">
                                            <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg4'>
                                                <TextField
                                                    className=""
                                                    value={this.state.processName}
                                                    onBlur={this.onBlurProcessName.bind(this)}
                                                />
                                                {this.showMessageBar(this.state.showProcessNameError, this.state.messagebarProcessName)}
                                            </div>
                                            <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg4'>
                                                <PrimaryButton text={<Trans>add</Trans>}
                                                    onClick={this.saveNewProcess.bind(this)}
                                                    disabled=''
                                                />
                                            </div>
                                        </div>
                                    </div>
                                </Modal>
                            </div>
                        </div>
                    </div>
                </div>
        );
    }
}