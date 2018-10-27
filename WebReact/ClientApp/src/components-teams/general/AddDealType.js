/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import * as ReactDOM from 'react-dom';
import * as microsoftTeams from '@microsoft/teams-js';
import { DefaultButton, PrimaryButton, IconButton, ActionButton } from 'office-ui-fabric-react/lib/Button';
import { I18n, Trans } from "react-i18next";
import { Link as LinkRoute } from 'react-router-dom';
import { AddTemplate } from './AddTemplate';
import { Modal } from 'office-ui-fabric-react/lib/Modal';
import { AddProcessType } from './AddProcessType';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Checkbox } from 'office-ui-fabric-react/lib/Checkbox';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { getQueryVariable } from '../../common';
import { PreviewDealType } from './PreviewDealType';
import { Label } from 'office-ui-fabric-react/lib/Label';



export class AddDealType extends Component {
    displayName = AddDealType.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;
        this.accessGranted = false;
        this.state = {
            loading: true,
            channelName: "",
            showModal: false,
            templateObj: {},
            allProcessTypesOriginal: [],
            processTypes: [],
            selectedProcess: [],
            processMaxOrder: 2,
            sortedTempSelectedProcess: [],
            groupSelectedProcess: [],
            dealTypeName: "",
            dealTypeId: "",
            operation: "add",
            groupOrder: "",
            groupOperation: "add",
            showPreviewModel: false,
            dealTypeObj: [],
            isCheckProcessOrder: false,
            processMessagebarText: "",
            isProcessExist: false,
            loadingProcessTypes: true,
            editDealGroup: false
        };



        // this._showModal = this._showModal.bind(this);
        this._closeModal = this._closeModal.bind(this);
        this.addGroupProcess = this.addGroupProcess.bind(this);
        this.editGroupProcess = this.editGroupProcess.bind(this);
        this.saveDealType = this.saveDealType.bind(this);
        this.previewDealType = this.previewDealType.bind(this);
        this._closePreviewModal = this._closePreviewModal.bind(this);
        this.moveProcessDown = this.moveProcessDown.bind(this);
        this.moveProcessUp = this.moveProcessUp.bind(this);
        this._onCheckboxChangeEnableOrder = this._onCheckboxChangeEnableOrder.bind(this);
    }

    async componentWillMount() {
        // Get the teams context
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            if (this.state.channelName.length === 0) {
                // this.getTeamsContext();
            }

            // Check Access
            //let dealTypeAccess  = await this.authHelper.callCheckAccess(["Opportunity_ReadWrite_Dealtype"]);
            this.accessGranted = await this.authHelper.callCheckAccess(["Administrator", "Opportunity_ReadWrite_Dealtype", "Opportunities_ReadWrite_All"]);
            //let superOpportunitiesWriteAccess = await this.authHelper.callCheckAccess(["Opportunities_ReadWrite_All"]);

            //let accessGranted = (dealTypeAccess || adminAccess || superOpportunitiesWriteAccess) ? true : false;
            //this.accessGranted = accessGranted
            // Check Access
            if (!this.accessGranted) {
                this.setState({ loading: false });
                return;
            }

            //Get All ProcessTypes
            this.getAllProcessTypes();

            let dealTypeId = getQueryVariable('dealTypeId');
            if (dealTypeId !== null) {
                this.setState({ operation: "edit" });
                this.getSelectedDealTypeById(dealTypeId);
            } else {
                //let tempSelectedProcess = this.state.templateObj.processes.filter(function (k) {
                //    return k.processType.toLowerCase() !== "base" && k.processType.toLowerCase() !== "customerdecisiontab";
                //});
                let tempSelectedProcess = {};
                tempSelectedProcess.processes = [];

                this.getTemplateProcess(tempSelectedProcess);
            }

        }
    }
    getAllProcessTypes() {
        this.setState({
            loadingProcessTypes: true
        });
        return new Promise((resolve, reject) => {
            let opportunityObj;
            let requestUrl = "api/process/";

            fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            })
                .then(response => response.json())
                .then(data => {
                    //get dealtype list
                    try {
                        console.log("Process type list");
                        console.log(data.itemsList);
                        // To display the ProcessTypes
                        let allProcessTypes = data.itemsList;
                        let processTypes = allProcessTypes.filter(function (k) {
                            return k.processType.toLowerCase() !== "base" && k.processType.toLowerCase() !== "customerdecisiontab" && k.processType.toLowerCase() !== "proposalstatustab";
                        });
                        console.log(processTypes);
                        this.setState({ loadingProcessTypes: false, processTypes: processTypes, allProcessTypesOriginal: processTypes });
                    }
                    catch (err) {
                        return false;
                    }
                })
                .catch(err => {
                    this.errorHandler(err, "getProcessList");
                    this.setState({
                        loadingProcessTypes: false,
                        processTypes: [],
                        allProcessTypesOriginal: []
                    });
                    reject(err);
                });
        });
    }


    //return [{ id: 1, processStep: "Risk Assesment", selected: false },
    //{ id: 2, processStep: "Compliance Review", selected: false },
    //{ id: 3, processStep: "Credit Check", selected: false },
    //{ id: 4, processStep: "Audit", selected: false },
    //{ id: 5, processStep: "Underwriting", selected: false }];

    getSelectedDealTypeById(dealTypeId) {
        return new Promise((resolve, reject) => {
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
                        let dealTypeItemList = [];
                        for (let i = 0; i < data.itemsList.length; i++) {
                            //data.itemsList[i].createdDisplayName = data.itemsList[i].createdBy.displayName;
                            if (data.itemsList[i].id === dealTypeId) {
                                dealTypeItemList.push(data.itemsList[i]);
                            }

                        }
                        console.log("vishnu getSelectedDealTypeById 1: ", tempSelectedProcess);
                        let tempSelectedProcess = dealTypeItemList[0].processes.filter(function (k) {
                            return k.processType.toLowerCase() !== "base" && k.processType.toLowerCase() !== "customerdecisiontab" &&
                                k.processType.toLowerCase() !== "start process" && k.processType.toLowerCase() !== "new opportunity" &&
                                k.processType.toLowerCase() !== "proposalstatustab";
                        });
                        console.log("vishnu getSelectedDealTypeById 2: ", tempSelectedProcess);
                        let maxOrder = tempSelectedProcess[tempSelectedProcess.length - 1].order;
                        this.setState({
                            loading: false,
                            dealTypeId: dealTypeItemList[0].id,
                            dealTypeName: dealTypeItemList[0].templateName,
                            templateObj: tempSelectedProcess,
                            operation: "edit",
                            dealTypeObj: dealTypeItemList[0],
                            processMaxOrder: maxOrder
                        });
                        this.getTemplateProcess(tempSelectedProcess);
                    }
                    catch (err) {
                        return false;
                    }

                })
                .catch(err => {
                    this.errorHandler(err, "getDealTypeList");
                    this.setState({
                        loading: false,
                        items: [],
                        itemsOriginal: []
                    });
                    reject(err);
                });
        });
    }

    getTemplateProcess(tempSelectedProcess) {

        if (parseInt(tempSelectedProcess.length) > 0) {

            let sortedTempSelectedProcess = tempSelectedProcess.sort(function (a, b) {
                return a.order > b.order ? 1 : 0;
            });
            let prev, next;
            prev = sortedTempSelectedProcess[0].order;
            let counter = 0;
            //let maxOrder = sortedTempSelectedProcess[sortedTempSelectedProcess.length - 1].order;
            //this.setState({ processMaxOrder: maxOrder });
            for (var i = 0; i < sortedTempSelectedProcess.length; i++) {
                sortedTempSelectedProcess[i].groupTag = "none";
                if (i > 0) {
                    let groupTag = "none";
                    next = sortedTempSelectedProcess[i].order;
                    let afterNext = 0;
                    if (i !== (sortedTempSelectedProcess.length - 1))
                        afterNext = sortedTempSelectedProcess[i + 1].order;
                    if (parseInt(prev) === parseInt(next)) {
                        counter = counter + 1;
                        if (counter === 1 && sortedTempSelectedProcess[i - 1].groupTag !== "start") {
                            if (i === 1) {   // handling the condition when the first element is a group and this is the second object of the group
                                sortedTempSelectedProcess[i - 1].groupTag = "start";
                            }

                            if (i === (sortedTempSelectedProcess.length - 1)) {
                                groupTag = "end";
                            } else {
                                groupTag = "next";
                            }
                        }
                        else {
                            if (i === (sortedTempSelectedProcess.length - 1)) {
                                groupTag = "end";
                            } else {
                                groupTag = "next";
                            }
                        }

                    } else if (counter > 0 && parseInt(next) !== parseInt(afterNext)) {
                        sortedTempSelectedProcess[i - 1].groupTag = "end";
                        counter = 0;
                    } else if (counter > 0 && parseInt(next) === parseInt(afterNext)) {
                        sortedTempSelectedProcess[i - 1].groupTag = "end";
                        groupTag = "start";
                        counter = 0;
                    } else if (counter === 0 && parseInt(next) === parseInt(afterNext)) {
                        groupTag = "start";
                    } else if (counter === 0) {
                        groupTag = "none";
                    }
                    sortedTempSelectedProcess[i].groupTag = groupTag;
                }
                prev = sortedTempSelectedProcess[i].order;
            }
            sortedTempSelectedProcess = sortedTempSelectedProcess.sort(function (a, b) {
                return a.order > b.order ? 1 : 0;
            });
            this.setState({ selectedProcess: sortedTempSelectedProcess });
        }

        this.setState({
            loading: false
        });

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
        console.log("Get DealTypeList Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    onBlurDealTypeName(e) {
        if (e.target.value.length > 0) {
            this.setState({
                dealTypeName: e.target.value,
                messagebarDealTypeName: "",
                dealTypeNameError: false
            });
        } else {
            this.setState({
                dealTypeName: "",
                messagebarDealTypeName: <Trans>dealTypeNameNotEmpty</Trans>,
                dealTypeNameError: false
            });
        }
    }

    // Preview dealtype
    previewDealType() {
        let dealTypeObject = {};
        dealTypeObject.id = this.state.operation === "edit" ? this.state.dealTypeId : "";
        dealTypeObject.templateName = this.state.dealTypeName;
        dealTypeObject.description = "test desc";
        dealTypeObject.processes = [];

        let editDealGroup = dealTypeObject.id ? JSON.stringify(this.state.templateObj) === JSON.stringify(this.state.selectedProcess) : false;

        // Add NewOpportunity/Start process type to object - Add
        let defaultProcessTypes1 = [
            {
                "processStep": "New Opportunity",
                "channel": "None",
                "processType": "New Opportunity",
                "order": "1",
                "daysEstimate": "0",
                "status": 0
            },
            {
                "processStep": "Start Process",
                "channel": "None",
                "processType": "Start Process",
                "order": "2",
                "daysEstimate": "0",
                "status": 0
            }

        ];
        // Add Draft Proposal process type to end  - Add
        //Changing name to Customer Decision
        let defaultProcessTypes2 = [{
            "processStep": "Customer Decision",
            "channel": "Customer Decision",
            "processType": "CustomerDecisionTab",
            "order": this.state.selectedProcess.length > 0 ? parseInt(this.state.selectedProcess[this.state.selectedProcess.length - 1].order) + 2 : "",
            "daysEstimate": "0",
            "status": 0
        }];

        //Dynamic flow formalproposal : Start
        let formalProposal = [{
            "processStep": "Formal Proposal",
            "channel": "Formal Proposal",
            "processType": "ProposalStatusTab",
            "order": this.state.selectedProcess.length > 0 ? parseInt(this.state.selectedProcess[this.state.selectedProcess.length - 1].order) + 1 : "",
            "daysEstimate": "0",
            "status": 0
        }];

        dealTypeObject.processes = defaultProcessTypes1.concat(this.state.selectedProcess).concat(defaultProcessTypes2).concat(formalProposal);

        //Dynamic flow formalproposal : end
        this.setState({
            dealTypeObj: dealTypeObject,
            showPreviewModel: true,
            editDealGroup: editDealGroup
        });


        //let selProcess = this.state.selectedProcess;
        //for (let s = 0; s < selProcess.length; s++) {
        //    delete selProcess[s].groupTag;
        //}
    }

    _closePreviewModal() {
        this.setState({
            showPreviewModel: false,
            isUpdate: false,
            isUpdateMsg: ""
        });
    }

    // Save DealType
    saveDealType() {
        //Removing groupTag - while passing to MT
        let dealTypeObject = this.state.dealTypeObj;
        for (let s = 0; s < dealTypeObject.processes.length; s++) {
            delete dealTypeObject.processes[s].groupTag;
        }

        // To display the selected group with Process 
        //let tempSelectedProcess = dealTypeObject.processes.filter(function (k) {
        //    return k.processType.toLowerCase() !== "base" && k.processType.toLowerCase() !== "customerdecisiontab";
        //});

        //this.getTemplateProcess(tempSelectedProcess);


        this.setState({ isUpdate: true });
        // API Add/Update call
        this.requestUpdUrl = 'api/template/';
        let options = "";
        if (this.state.operation === "edit") {
            options = {
                method: "PATCH",
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
                },
                body: JSON.stringify(dealTypeObject)
            };
        } else {
            options = {
                method: "POST",
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
                },
                body: JSON.stringify(dealTypeObject)
            };
        }


        fetch(this.requestUpdUrl, options)
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    this.setState({
                        MessagebarText: <Trans>dealTypeAddSuccess</Trans>,
                        isUpdate: false,
                        isUpdateMsg: true
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.success, MessagebarText: "" }); }.bind(this), 3000);
                    window.location = '/tab/generalConfigurationTab#dealType';
                    return response.json;
                } else {
                    this.setState({
                        MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
                        isUpdate: false,
                        isUpdateMsg: true
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
                }
            }).then(json => {
                //console.log(json);
                this.setState({ isUpdate: false });
            });

    }

    // Remove selected processtype
    uniqueProcess(allProcessTypes4, groupProcess) {
        let allProcessTypes5 = this.state.processTypes; //this.getAllProcessTypes();
        for (let m = 0; m < allProcessTypes5.length; m++) {

            for (let pt = 0; pt < groupProcess.length; pt++) {
                if (groupProcess[pt].processStep === allProcessTypes5[m].processStep) {
                    allProcessTypes5.pop(m);
                }
            }
        }
        return allProcessTypes4;
    }

    editGroupProcess(object, selGroupNum) {
        let allProcessTypes3 = this.state.processTypes;
        for (let m = 0; m < allProcessTypes3.length; m++) {
            allProcessTypes3[m].selected = false;
        }
        let groupProcess = this.state.selectedProcess.filter(function (i) {
            return parseInt(i.order) === parseInt(object.order);
        });
        //allProcessTypes3 = this.uniqueProcess(allProcessTypes3, groupProcess);



        this.setState({
            //processTypes: allProcessTypes,
            groupSelectedProcess: groupProcess,
            groupOperation: "edit",
            groupOrder: object.order,
            showModal: true
        });
    }

    addGroupProcess() {
        //let allProcessTypes = this.getAllProcessTypes();
        this.setState({
            groupSelectedProcess: [],
            //processTypes: this.getAllProcessTypes(),
            groupOperation: "add",
            showModal: true
        });
    }



    _closeModal() {
        this.setState({
            groupSelectedProcess: [],
            showModal: false
        });
    }

    addProcess(item) {
        //check processStep already exist in selected Group items
        for (let p = 0; p < this.state.selectedProcess.length; p++) {
            if (this.state.selectedProcess[p].processStep === item.processStep) {
                this.setState({
                    processMessagebarText: <Trans>processStepAlreadyExistOtherGroup</Trans>,
                    isProcessExist: true
                });
                setTimeout(function () { this.setState({ isProcessExist: false, processMessagebarText: "" }); }.bind(this), 3000);
                return false;
            }
        }

        // Check processStep exist in Current Group
        for (let p = 0; p < this.state.groupSelectedProcess.length; p++) {
            if (this.state.groupSelectedProcess[p].processStep === item.processStep) {
                this.setState({
                    processMessagebarText: <Trans>processStepAlreadyExist</Trans>,
                    isProcessExist: true
                });
                setTimeout(function () { this.setState({ isProcessExist: false, processMessagebarText: "" }); }.bind(this), 3000);
                return false;
            }
        }

        let newSelectedProcess = {
            "processStep": item.processStep,
            "channel": item.processStep,
            "processType": "CheckListTab",
            "order": this.state.processMaxOrder + 1,
            "daysEstimate": "",
            "status": 0
        };

        //let allProcessTypes1 = this.state.processTypes;
        //for (let m = 0; m < allProcessTypes1.length; m++) {
        //    if (allProcessTypes1[m].processStep === item.processStep) {
        //        allProcessTypes1[m].selected = true;
        //        allProcessTypes1.splice(m, 1);
        //    }
        //}

        this.setState({
            groupSelectedProcess: this.state.groupSelectedProcess.concat(item),
            processTypes: this.state.allProcessTypesOriginal // this.getAllProcessTypes()
        });

    }

    removeProcess(item) {
        let updSelectedProcess = this.state.groupSelectedProcess.filter(function (p) {
            return p.processStep !== item.processStep;
        });

        //let allProcessTypes2 = this.state.processTypes;
        //for (let m = 0; m < allProcessTypes2.length; m++) {
        //    if (allProcessTypes2[m].processStep === item.processStep) {
        //        allProcessTypes2[m].selected = false;
        //        allProcessTypes2[m].daysEstimate = "";
        //    }
        //}

        this.setState({
            //processTypes: allProcessTypes2,
            groupSelectedProcess: updSelectedProcess
        });
    }

    onBlurEstimatedDays(e, item) {

        let updEstDaysProcessType = this.state.groupSelectedProcess;
        for (let m = 0; m < updEstDaysProcessType.length; m++) {
            if (updEstDaysProcessType[m].processStep === item.processStep) {
                updEstDaysProcessType[m].daysEstimate = e.target.value;
            }
        }

        this.setState({
            groupSelectedProcess: updEstDaysProcessType
        });
    }

    _onCheckboxChangeEnableOrder() {
        this.setState({ isCheckProcessOrder: !this.state.isCheckProcessOrder });
    }

    moveProcessDown(p, i) {
        this.swapProcessOrder(i, i + 1);
    }

    moveProcessUp(p, i) {
        this.swapProcessOrder(i, i - 1);
    }

    swapProcessOrder(p1, p2) {
        console.log(this.state.groupSelectedProcess);
        Array.prototype.swapItems = function (a, b) {
            this[a] = this.splice(b, 1, this[a])[0];
            return this;
        };

        let processArr = this.state.groupSelectedProcess;
        processArr.swapItems(p1, p2);
        console.log(processArr);
        this.setState({ groupSelectedProcess: processArr });
    }

    saveGroupWithProcess(groupOrder, groupOperation) {
        let newOrder;
        let updGroupList = this.state.selectedProcess;
        let selGroupProcess = this.state.groupSelectedProcess;

        if (parseInt(groupOrder) > 0 && groupOperation === "edit") {
            newOrder = parseInt(groupOrder);
            updGroupList = updGroupList.filter(function (t) {
                return parseInt(groupOrder) !== parseInt(t.order);
            });
        } else {
            newOrder = parseInt(this.state.processMaxOrder) + 1;
        }



        if (selGroupProcess.length === 1) {
            let pItem = {
                "processStep": selGroupProcess[0].processStep,
                "channel": selGroupProcess[0].processStep,
                "processType": "CheckListTab",
                "order": newOrder.toString(),
                "daysEstimate": selGroupProcess[0].daysEstimate,
                "status": 0
            };

            updGroupList = updGroupList.concat(pItem);

        } else {
            for (let t1 = 0; t1 < selGroupProcess.length; t1++) {
                let pItem;
                if (t1 === 0) {
                    pItem = {
                        "processStep": selGroupProcess[t1].processStep,
                        "channel": selGroupProcess[t1].processStep,
                        "processType": "CheckListTab",
                        "order": newOrder.toString(),
                        "daysEstimate": selGroupProcess[t1].daysEstimate,
                        "status": 0
                    };
                } else {
                    pItem = {
                        "processStep": selGroupProcess[t1].processStep,
                        "channel": selGroupProcess[t1].processStep,
                        "processType": "CheckListTab",
                        "order": newOrder + "." + t1,
                        "daysEstimate": selGroupProcess[t1].daysEstimate,
                        "status": 0
                    };
                }

                updGroupList = updGroupList.concat(pItem);

            }

        }
        this.getTemplateProcess(updGroupList);
        //let allPrTypes = this.getAllProcessTypes(); //this.state.allProcessTypesOriginal;
        this.setState({
            processTypes: this.state.allProcessTypesOriginal, //this.getAllProcessTypes(),
            processMaxOrder: newOrder,
            showModal: false,
            groupSelectedProcess: [],
            groupOperation: "add",
            groupOrder: ""
        });

    }


    swapItems(list, iA, positionTo) {
        //   get element from arr1ay with iA
        if (positionTo === "down") {   // handling single process with down arrow
            let j = iA;
            if (list[iA].groupTag === "none") {
                list[iA].order = parseFloat(list[iA].order) + 1.0;
            }
            else if (list[j].groupTag === "start") {
                do {
                    list[j].order = parseFloat(list[j].order) + 1.0;
                    j++;
                } while (list[j].groupTag !== "end");
                list[j].order = parseFloat(list[j].order) + 1.0;

            }

            if (list[j + 1].groupTag === "none") { //next element. single process
                list[j + 1].order = parseFloat(list[j + 1].order) - 1.0;

            }
            else if (list[j + 1].groupTag === "start") { // next element is group
                let i = j + 1;
                do {
                    list[i].order = parseFloat(list[i].order) - 1.0;
                    i++;
                } while (list[i].groupTag !== "end");
                list[i].order = parseFloat(list[i].order) - 1.0;

            }
        }
        else if (positionTo === "up") {   // handling single process with UP arrow
            if (list[iA].groupTag === "none") {
                list[iA].order = parseFloat(list[iA].order) - 1.0;
            }
            else if (list[iA].groupTag === "start") {
                let j1 = iA;
                do {
                    list[j1].order = parseFloat(list[j1].order) - 1.0;
                    j1++;
                } while (list[j1].groupTag !== "end");
                list[j1].order = parseFloat(list[j1].order) - 1.0;
            }
            if (list[iA - 1].groupTag === "none") { //prev element. single process
                list[iA - 1].order = parseFloat(list[iA - 1].order) + 1.0;

            }
            else if (list[iA - 1].groupTag === "end") { // prev element is group
                let i1 = iA - 1;
                do {
                    list[i1].order = parseFloat(list[i1].order) + 1.0;
                    i1--;
                } while (list[i1].groupTag !== "start");

                list[i1].order = parseFloat(list[i1].order) + 1.0;

            }
        }
        var sortedTempSelectedProcess = list.sort(function (a, b) {
            return a.order > b.order ? 1 : 0;
        });
        this.setState({ selectedProcess: list });
        return list;
    }
    moveOrderDown(item, index) {
        let updatedTemplateProcess = this.state.selectedProcess; //this.state.sortedTempSelectedProcess;
        this.swapItems(updatedTemplateProcess, index, "down");

    }
    moveOrderUp(item, index) {
        let updatedTemplateProcess = this.state.selectedProcess; //this.state.sortedTempSelectedProcess;
        this.swapItems(updatedTemplateProcess, index, "up");

    }


    deleteGroup(selOrder) {

        Array.prototype.selectedProcessGroupBy = function (prop) {
            return this.reduce(function (groups, item) {
                const val = parseInt(item[prop]);
                groups[val] = groups[val] || [];
                groups[val].push(item);
                return groups;
            }, {});
        };

        let groupedByOrder = this.state.selectedProcess.selectedProcessGroupBy('order');

        if (Object.keys(groupedByOrder).length > 1) {
            let updSelectedProcess = this.state.selectedProcess;
            updSelectedProcess = updSelectedProcess.filter(function (k) {
                return parseInt(k.order) !== parseInt(selOrder);
            });

            this.setState({ selectedProcess: updSelectedProcess });
        } else if (Object.keys(groupedByOrder).length === 1) {
            alert("Can not delete all process groups. Atleast one process group should exist.");
        }


    }

    render() {
        const { processTypes, loading, selectedProcess } = this.state;
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            return (
                this.accessGranted
                    ?
                    <div className='ms-Grid bg-white ibox-content border-none p-10'>
                        <div className='ms-Grid-row'>
                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 adddealtypeheading'>
                                <h3><span className="dealtype" ><Trans>dealTypes</Trans><i className="ms-Icon ms-Icon--ChevronRightMed font-20" aria-hidden="true"/> </span><Trans>addDealType</Trans></h3>
                            </div>
                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                                <LinkRoute to={'/tab/generalConfigurationTab#dealType'} className='pull-right'><Trans>backToList</Trans> </LinkRoute>
                            </div>
                        </div>

                        <div className='ms-Grid-row pt10'>
                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 flexBoxy'>
                                <div className='ms-Grid-row'>
                                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                                        {
                                            this.state.loadingProcessTypes ?
                                                <div className='ms-BasicSpinnersExample bg-white '>
                                                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                                </div>
                                                :
                                                <div className="ms-Grid-row bg-white">
                                                    {
                                                        this.state.processTypes.map((template, idx) =>
                                                            <div className="ms-Grid-col ms-sm4 ms-md4 ms-lg4 processBoxes" key={idx}>
                                                                <div className="ms-Grid-row DealNameBG">
                                                                    <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12">
                                                                        <h5>{template.processStep}</h5>
                                                                    </div>
                                                                </div>

                                                            </div>

                                                        )
                                                    }
                                                </div>
                                        }

                                    </div>
                                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 bg-white'>
                                        <div className="ms-Grid-row bg-grey">
                                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg3 select-heading'>
                                                <h3><Trans>selected</Trans></h3>
                                            </div>
                                            <div className='ms-Grid-col ms-sm5 ms-md5 ms-lg9 pull-right'>
                                                <DefaultButton iconProps={{ iconName: 'FabricNewFolder' }} className="pull-right LinkAction-Button font10" onClick={e => this.addGroupProcess()} text={<Trans>addGroup</Trans>} />
                                            </div>
                                        </div>
                                        <div className="ms-Grid-row bg-grey">
                                            <div className='ms-Grid-col ms-sm12 ms-md7 ms-lg12 pull-left font12 pb15'>
                                                <span><Trans>arrowsChangeTheOrder</Trans></span>
                                            </div>

                                        </div>
                                        <div className="ms-Grid-row bg-grey">
                                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg4 pl10'>
                                                <TextField
                                                    id='dealTypeName'
                                                    label={<Trans>dealTypeName</Trans>}
                                                    value={this.state.dealTypeName}
                                                    errorMessage={this.state.messagebarDealTypeName}
                                                    onBlur={(e) => this.onBlurDealTypeName(e)}
                                                />
                                                {this.state.dealTypeNameError ?
                                                    <MessageBar messageBarType={MessageBarType.error} isMultiline={false}>
                                                        {this.state.messagebarDealTypeName}
                                                    </MessageBar>
                                                    : ""
                                                }
                                            </div>

                                        </div>

                                        <div className="ms-Grid-row bg-grey">
                                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12 p15 AddDealScrollEdit'>
                                                <div className="ms-Grid-row p-10">
                                                    {
                                                        this.state.selectedProcess.length === 0 ?
                                                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12 bg-white select-headin'>
                                                                <h5><Trans>addGroupMessage</Trans></h5>
                                                            </div>
                                                            : ""
                                                    }
                                                    {this.state.selectedProcess.map((object, i) =>
                                                        object.groupTag === "none" ?
                                                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg3 displayInline' key={i}>
                                                                <div className="ms-Grid-row">
                                                                    <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg6'>
                                                                        {i === 0 ?
                                                                            <ActionButton
                                                                                onClick={e => this.moveOrderDown(object, i)}
                                                                                className="f20 groupRight"
                                                                            />
                                                                            : i === (this.state.selectedProcess.length - 1) ?
                                                                                <div >
                                                                                    <ActionButton
                                                                                        onClick={e => this.moveOrderUp(object, i)}
                                                                                        className="f20 groupLeft"
                                                                                    />

                                                                                </div>
                                                                                :
                                                                                <div className="ResponsiveArrowAlign">
                                                                                    <ActionButton
                                                                                        onClick={e => this.moveOrderUp(object, i)}
                                                                                        className="f20 groupLeft"
                                                                                    />&nbsp;&nbsp;&nbsp;&nbsp;
                                                                                    <ActionButton
                                                                                        onClick={e => this.moveOrderDown(object, i)}
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
                                                                                    onClick={e => this.deleteGroup(object.order)}
                                                                                >
                                                                                    <i className="ms-Icon ms-Icon--StatusCircleErrorX pull-right f20" aria-hidden="true" />
                                                                                </ActionButton>
                                                                            </div>
                                                                            <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12">
                                                                                <AddProcessType displayProcess={object} key={i} />
                                                                            </div>

                                                                            <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12">
                                                                                <i className="ms-Icon ms-Icon--Edit  f20 linkbutton f10" aria-hidden="true" onClick={e => this.editGroupProcess(object, i)} > <Trans>editGroup</Trans> </i>
                                                                            </div>
                                                                        </div>

                                                                    </div>
                                                                </div>
                                                            </div>

                                                            : object.groupTag === "start" ?
                                                                <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg3 displayInline' key={i}>
                                                                    <div className="ms-Grid-row ">
                                                                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                                                                            {i === 0 ?
                                                                                <ActionButton
                                                                                    onClick={e => this.moveOrderDown(object, i)}
                                                                                    className="f20 groupRight"
                                                                                />
                                                                                : parseInt(this.state.processMaxOrder) === parseInt(object.order) ?
                                                                                    <ActionButton
                                                                                        onClick={e => this.moveOrderUp(object, i)}
                                                                                        className="f20 groupLeft"
                                                                                    />
                                                                                    :
                                                                                    <div>
                                                                                        <ActionButton
                                                                                            onClick={e => this.moveOrderUp(object, i)}
                                                                                            className="f20 groupLeft"
                                                                                        />&nbsp;&nbsp;&nbsp;&nbsp;

                                                                                        <ActionButton
                                                                                            onClick={e => this.moveOrderDown(object, i)}
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
                                                                                        onClick={e => this.deleteGroup(object.order)}
                                                                                    >
                                                                                        <i className="ms-Icon ms-Icon--StatusCircleErrorX pull-right f20" aria-hidden="true" />
                                                                                    </ActionButton>
                                                                                </div>
                                                                                <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12">
                                                                                    <AddProcessType displayProcess={object} key={i} />
                                                                                    {
                                                                                        this.state.selectedProcess.map((subObject, subIdx) =>
                                                                                            <div className="bg-white" key={subIdx}>
                                                                                                {
                                                                                                    parseInt(subObject.order) === parseInt(object.order) && (subObject.groupTag === "next" || subObject.groupTag === "end") ?
                                                                                                        <AddProcessType displayProcess={subObject} key={subIdx} />
                                                                                                        : ""
                                                                                                }
                                                                                            </div>
                                                                                        )

                                                                                    }
                                                                                </div>
                                                                                <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12">
                                                                                    <i className="ms-Icon ms-Icon--Edit  f20 linkbutton f10" aria-hidden="true" onClick={e => this.editGroupProcess(object, i)} > <Trans>editGroup</Trans> </i>
                                                                                </div>
                                                                            </div>
                                                                        </div>
                                                                    </div>
                                                                </div>

                                                                : ""
                                                    )}

                                                </div>
                                            </div>
                                        </div>
                                        <div className="ms-Grid-row bg-grey p-10">
                                            <div className='ms-Grid-col ms-sm4 ms-md6 ms-lg12'><br />
                                                <PrimaryButton text={<Trans>continue</Trans>} onClick={e => this.previewDealType()} className="pull-right" />
                                            </div>
                                        </div>

                                        <div className="ms-Grid-row bg-grey">
                                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg8'>
                                                <Modal
                                                    isOpen={this.state.showModal}
                                                    onDismiss={this._closeModal}
                                                    isBlocking={true}
                                                    containerClassName="ms-modalExample-container"
                                                >
                                                    <div className="ms-modalExample-header">
                                                        <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12'>
                                                            <ActionButton
                                                                className="pull-right"
                                                                onClick={this._closeModal}
                                                            >
                                                                <i className="ms-Icon ms-Icon--StatusCircleErrorX pull-right f30" aria-hidden="true"/>
                                                            </ActionButton>
                                                        </div>
                                                    </div>
                                                    <div className="ms-modalExample-body">
                                                        <div className="ms-Grid-row"/>
                                                        <div className="ms-Grid-row">
                                                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg7'>
                                                                <div className="ms-Grid-row bg-white">
                                                                    <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12 p15">
                                                                        {
                                                                            this.state.isProcessExist ?
                                                                                <MessageBar
                                                                                    messageBarType={MessageBarType.error}
                                                                                    isMultiline={false}
                                                                                >
                                                                                    {this.state.processMessagebarText}
                                                                                </MessageBar>
                                                                                : ""
                                                                        }
                                                                    </div>
                                                                </div>
                                                                <div className="ms-Grid-row bg-white">
                                                                    {
                                                                        this.state.processTypes.map((process, idx) =>
                                                                            <div className="ms-Grid-col ms-sm10 ms-md6 ms-lg4 p15" key={idx}>
                                                                                <div className="ms-Grid-row bg-grey GrayBorder text-center">
                                                                                    <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12 bg-white">
                                                                                        <IconButton iconProps={{ iconName: 'Add' }} onClick={e => this.addProcess(process)} className={process.selected ? "hide" : ""} />
                                                                                        <IconButton iconProps={{ iconName: 'Accept' }} className={process.selected ? "" : "hide"} />
                                                                                    </div>
                                                                                    <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12 purpleBG text-center">
                                                                                        <h5>{process.processStep}</h5>
                                                                                    </div>
                                                                                </div>

                                                                            </div>

                                                                        )
                                                                    }

                                                                </div>
                                                            </div>
                                                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg5 CheckBoxWidth p-l-30'>
                                                                <Trans>Selected</Trans>
                                                                <Checkbox
                                                                    label={<Trans>enableOrdering</Trans>}
                                                                    onChange={this._onCheckboxChangeEnableOrder}
                                                                />
                                                                {
                                                                    this.state.groupSelectedProcess.map((p, idx) =>
                                                                        <div className="ms-Grid-row p-10 " key={idx}>
                                                                            <div className="ms-Grid-col ms-sm1 ms-md1 ms-lg1 ">
                                                                                <Label>{idx + 1}</Label>
                                                                            </div>
                                                                            <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg8 processBg">
                                                                                <div className="ms-Grid-row ">
                                                                                    <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg8">
                                                                                        <h5 className="font12 font-normal">{p.processStep}</h5>
                                                                                    </div>
                                                                                    <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg4 ">
                                                                                        <IconButton iconProps={{ iconName: 'remove' }} className="pull-right" onClick={e => this.removeProcess(p)} />
                                                                                    </div>
                                                                                </div>
                                                                                <div className="ms-Grid-row processBg pb15">
                                                                                    <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg6 font10">
                                                                                        <Trans>estimateDays</Trans>
                                                                                        <TextField
                                                                                            className="textboxSize"
                                                                                            value={p.daysEstimate}
                                                                                            onBlur={(e) => this.onBlurEstimatedDays(e, p)}
                                                                                        />
                                                                                    </div>
                                                                                </div>

                                                                            </div>
                                                                            <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg3 ">
                                                                                {
                                                                                    this.state.groupSelectedProcess.length > 1 ?
                                                                                        idx === 0 ?
                                                                                            <ActionButton
                                                                                                onClick={e => this.moveProcessDown(p, idx)}
                                                                                                disabled={!this.state.isCheckProcessOrder}
                                                                                            >
                                                                                                <i className="ms-Icon ms-Icon--SortDown f20" aria-hidden="true"/>
                                                                                            </ActionButton>
                                                                                            : parseInt(this.state.groupSelectedProcess.length - 1) === parseInt(idx) ?

                                                                                                <ActionButton
                                                                                                    onClick={e => this.moveProcessUp(p, idx)}
                                                                                                    disabled={!this.state.isCheckProcessOrder}
                                                                                                >
                                                                                                    <i className="ms-Icon ms-Icon--SortUp f20" aria-hidden="true"/>
                                                                                                </ActionButton>
                                                                                                :
                                                                                                <div>
                                                                                                    <ActionButton
                                                                                                        onClick={e => this.moveProcessUp(p, idx)}
                                                                                                        disabled={!this.state.isCheckProcessOrder}
                                                                                                    >
                                                                                                        <i className="ms-Icon ms-Icon--SortUp f20" aria-hidden="true"/>
                                                                                                    </ActionButton><br /><br />
                                                                                                    <ActionButton
                                                                                                        onClick={e => this.moveProcessDown(p, idx)}
                                                                                                        disabled={!this.state.isCheckProcessOrder}
                                                                                                    >
                                                                                                        <i className="ms-Icon ms-Icon--SortDown f20" aria-hidden="true"/>
                                                                                                    </ActionButton>
                                                                                                </div>
                                                                                        : ""

                                                                                }
                                                                            </div>
                                                                        </div>
                                                                    )
                                                                }
                                                                <div className="ms-Grid-row p-10 ">
                                                                    <PrimaryButton text={<Trans>save</Trans>} onClick={e => this.saveGroupWithProcess(this.state.groupOrder, this.state.groupOperation)} disabled={this.state.groupSelectedProcess.length > 0 ? false : true} />
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </Modal>
                                            </div>
                                        </div>
                                        <div className="ms-Grid-row bg-grey">
                                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg8'>
                                                <Modal
                                                    isOpen={this.state.showPreviewModel}
                                                    onDismiss={this._closePreviewModal}
                                                    isBlocking={true}
                                                    containerClassName="ms-modalExample-container"
                                                >
                                                    <div className="ms-modalExample-header">
                                                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                                                            <div className="ms-Grid-row bg-white">
                                                                <h4>Display Preview</h4>
                                                            </div>
                                                        </div>
                                                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                                                            <ActionButton
                                                                className="pull-right"
                                                                onClick={this._closePreviewModal}
                                                            >
                                                                <i className="ms-Icon ms-Icon--StatusCircleErrorX pull-right f30" aria-hidden="true"/>
                                                            </ActionButton>
                                                        </div>
                                                    </div>
                                                    <div className="ms-modalExample-body">

                                                        <div className="ms-Grid-row">
                                                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg12 ibox-content'>
                                                                <div className="ms-Grid-row bg-white">
                                                                    <PreviewDealType dealTypeObject={this.state.dealTypeObj} />

                                                                </div>
                                                            </div>

                                                        </div>
                                                        <div className="ms-Grid-row">
                                                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg9'>
                                                                <div className='ms-BasicSpinnersExample p-10 pull-right'>
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
                                                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg3'><br />
                                                                <PrimaryButton text={<Trans>save</Trans>} className="pull-right p-10" onClick={e => this.saveDealType()} disabled={this.state.selectedProcess.length === 0 || this.state.isUpdate || this.state.editDealGroup ? true : false} />
                                                            </div>
                                                        </div>
                                                    </div>

                                                </Modal>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                    </div>
                    :
                    <div className='ms-Grid bg-white  p-10 ibox-content'>
                        <div className='ms-Grid-row'>
                            <div className="ms-Grid-col ms-sm6 ms-md8 ms-lg12 p-10"><h2><Trans>accessDenied</Trans></h2></div>
                        </div>
                    </div>
            );
        }
    }
}