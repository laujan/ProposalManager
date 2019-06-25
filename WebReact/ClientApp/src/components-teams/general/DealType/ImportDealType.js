/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { useState, useEffect, useRef } from 'react';
import ShowDealTypeModel from './ShowImportDealTypeModel';
import {getOpportuity,getRoleMappingObject,getProcessObject,getTemplateObject,getAdminRoleObject} from './ImportDealTypeJsonObjects';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import {  Trans } from "react-i18next";
import { PrimaryButton} from 'office-ui-fabric-react/lib/Button';
import { MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { MessageBar } from 'office-ui-fabric-react/lib/MessageBar';


/**
 *This component helps to import the dealType(business process) from template json file present in the solution
 * */

let tempModelMsgObject = {
    modelHeader: "",
    modelMsg: "",
    modelButtonFlag: false
}

const ImportDelaTypeJson = (props) => {

    const [dealTypeJson, setDealTypeJson] = useState({
        processes: [],
        rolemapping: [],
        template: {}
    });
    const [modelMsgObject, setModelMsgObject] = useState(tempModelMsgObject);
    const [loading, setLoading] = useState(false);
    const [showModel, setShowModel] = useState(false);
    const [processes, setProcesses] = useState([]);
    const [templates, setTemplates] = useState([]);
    const [roles, setRoles] = useState([]);
    const [messagebarObject, setMessagebarObject] = useState({
        isUpdate: false,
        isUpdateMsg: false,
        MessageBarType: MessageBarType.success,
        MessagebarText: ""
    });
    const [createOpportunityTeam, setcreateOpportunityTeam] = useState(false);
    const [installedTemplates, setInstalledTemplates] = useState([]);
    const [UserProfile,setUserProfile] = useState(null);

    const inputRef = useRef();

    useEffect(() => {
        getRolesProcessesAndDealTypes();
    }, []);

    const showError = async (message) =>{
        setMessagebarObject({
            isUpdate: false,
            isUpdateMsg: true,
            MessageBarType: MessageBarType.error,
            MessagebarText: message
        });
        let x = await delay(3000);
        setMessagebarObject({
            isUpdate: false,
            isUpdateMsg: true,
            MessageBarType: null,
            MessagebarText: ""
        });
    };

    const getRolesProcessesAndDealTypes = async () => {
        setLoading(true);
        let processes = [];
        let templates = [];
        let roles = [];
        let installedTemplates = [];
        let UserProfile = null;

        try {
            let data = await getData("template");
            if (data && data.hasOwnProperty("itemsList")) {
                templates = data.itemsList.map(template => template.templateName.toLowerCase());
                installedTemplates = data.itemsList.filter(template => template.description.length === 0).map(temp => temp.templateName);
            }

            data = await getData("process");
            if (data && data.hasOwnProperty("itemsList")) processes = data.itemsList.map(process => process.processStep.toLowerCase());

            data = await getData("roles");
            if (data && data.length > 0) roles = data.map(role => role.adGroupName.toLowerCase());

            UserProfile = await window.authHelper.callGetUserProfile();

        } catch (error) {
            await showError(`getRolesProcessesAndDealType ${error.message}`);
        }

        setUserProfile(UserProfile)
        setInstalledTemplates(installedTemplates);
        setProcesses(processes);
        setTemplates(templates);
        setRoles(roles);
        setLoading(false);
    };

    const checkTemplateFileContent = (myObj) => {
        let flag = false;
        if (myObj.hasOwnProperty('rolemapping') && myObj.hasOwnProperty('processes') && myObj.hasOwnProperty('template')) {
            if (myObj["processes"].length > 0 && myObj["rolemapping"].length > 0)
                flag = true;
        }
        return flag;
    };

    //After reading parse the file to JSON object 
    const handleRead = async event => {
        if (!event.target || !event.target.files) {
            return;
        }

        const fileList = event.target.files;
        const latestUploadedFile = fileList.item(fileList.length - 1);

        try {
            const fileContents = await handleFileChosen(latestUploadedFile);
            let tempDealTypeJson = JSON.parse(fileContents);

            if (checkTemplateFileContent(tempDealTypeJson)) {

                tempModelMsgObject.modelHeader = tempDealTypeJson.template.templateName;

                if (templates.indexOf(tempDealTypeJson.template.templateName.toLowerCase()) > -1) {
                    tempModelMsgObject.modelButtonFlag = false;
                    tempModelMsgObject.modelMsg = `${tempModelMsgObject.modelHeader} Bussiness Process is already present, so not able to add again.`;
                } else {
                    tempModelMsgObject.modelButtonFlag = true;
                    tempModelMsgObject.modelMsg = `By clicking save button , ${tempModelMsgObject.modelHeader} Bussiness Process will get added.`;

                    tempDealTypeJson.processes = tempDealTypeJson.processes.filter(function (obj) {
                        return !(processes.indexOf(obj.processStep.toLowerCase()) > -1);
                    });

                    tempDealTypeJson.rolemapping = tempDealTypeJson.rolemapping.filter(function (obj) {
                        return !(roles.indexOf(obj.adGroupName.toLowerCase()) > -1);
                    });
                }

                setModelMsgObject(tempModelMsgObject);
                setDealTypeJson(tempDealTypeJson);
                setShowModel(true);
                inputRef.current.value = '';
            }
            else
                throw new Error("Invalid File Content");

        } catch (e) {
            await showError(`handleRead ${e.message}`);
        }
    };

    //REad the file from the disk as a promise
    const handleFileChosen = file => {
        const fileReader = new FileReader();

        return new Promise((res, rej) => {
            fileReader.onerror = () => {
                fileReader.abort();
                rej(new DOMException("Problem parsing input file"));
            };

            fileReader.onload = () => {
                res(fileReader.result);
            };

            fileReader.readAsText(file);
        });
    };

    const getData = async (requestUrl) => {
        let data;
         try {
            let response = await props.apiService.callApi(requestUrl, "GET");
            if (response.ok) {
                data = await response.json();
            }
        } catch (err) {
            data = null;
            await showError(`addData ${err.message}`);
        }
        return data;
    }

    const addData = async (requestUrl, jsonObject, method = "POST") => {
        try {
            let options = {
                body: JSON.stringify(jsonObject)
            };
            await props.apiService.callApi(requestUrl,method, options);
        } catch (error) {
            await showError(`addData ${error.message}`);
        }
    };

    const delay = async (ms) => {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
    //Create and update opportunity using the saved template
    const handleSaveButton = async () => {
        setMessagebarObject({
            isUpdate: true,
            isUpdateMsg: true,
            MessageBarType: MessageBarType.info,
            MessagebarText: "Template is creating so dont close the modal."
        });
        let x = await addData("roles", getAdminRoleObject(UserProfile,"updated"), "PATCH");
        try {
            try {
                for (const role of dealTypeJson.rolemapping) {
                    let rolemappingobject = getRoleMappingObject(role);
                    await addData("roles", rolemappingobject);
                }

                for (const process of dealTypeJson.processes) {
                    let processObject = getProcessObject(process)
                    await addData("process", processObject);
                }

                let templateObject = getTemplateObject(dealTypeJson);
                await addData("template", templateObject);

            } catch (error) {
                await showError(`handleSaveButton (inner try) ${error.message}`);
            }

            x = await getRolesProcessesAndDealTypes();

            setMessagebarObject({
                isUpdate: false,
                isUpdateMsg: true,
                MessageBarType: MessageBarType.success,
                MessagebarText: `Bussiness Process added successfully`
            });

            x = await delay(2000);

            if (createOpportunityTeam) {
                x = await createOpportunityForTheTeamplate(dealTypeJson.template.templateName)               
            }

            x = await addData("roles", getAdminRoleObject(UserProfile), "PATCH");
        } catch (error) {
            await showError(`handleSaveButton (outer try) ${error.message}`);
        } finally {
            await setShowModel(false);
            setMessagebarObject({
                isUpdate: false,
                isUpdateMsg: false,
                MessageBarType: null,
                MessagebarText: ""
            });
        }
    };

    //Create and update opportunity using the saved template
    const createOpportunityForTheTeamplate = async (templateName) => {
        setMessagebarObject({
            isUpdate: true,
            isUpdateMsg: true,
            MessageBarType: MessageBarType.info,
            MessagebarText: "Opportunity team is creating so dont close the modal."
        });

        try {
            let opportunity = getOpportuity(templateName,UserProfile);
            let data = await addData("opportunity", opportunity);
            let templates = await getData("template");
            opportunity = await getData(`Opportunity/?name=${opportunity.displayName}`);
            templates = templates.itemsList.filter(template => template.templateName.toLowerCase() === templateName.toLowerCase())

            opportunity.template = templates[0];
            opportunity.template.processes.forEach(obj => {
                if (obj.processStep === "Start Process") obj.status = 3;
            });
            await addData("opportunity", opportunity, "PATCH");

            data = await delay(2000);

            setMessagebarObject({
                isUpdate: false,
                isUpdateMsg: true,
                MessageBarType: MessageBarType.success,
                MessagebarText: "Opportunity Team created successfully."
            });

            data = await delay(2000);

        } catch (error) {
            await showError(`createOpportunityForTheTeamplate ${error.message}`);
        }
    };

    const handleModelCheckbox = async (e, item) => {
        setcreateOpportunityTeam(item);
    };

    const closeModel = () => {
        setShowModel(false);
    };

    return (<div>
        {loading ? (
            <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
            </div>) : (
                <div>
                    <div className="file-input-wrapper">
                        <PrimaryButton className="btn-file-input" iconProps={{ iconName: 'Add' }} >&nbsp;<Trans>Upload Bussiness Process</Trans></PrimaryButton>
                        <input type='file' accept='.json' onChange={e => handleRead(e)} ref={inputRef} />
                    </div>
                    <div>
                        <h5>Installed Templates</h5>
                        {
                            installedTemplates.length > 0 ?
                                <ul>
                                    {
                                        installedTemplates.map((y, id) => {
                                            return (
                                                <li key={id}>{y}</li>
                                            )
                                        })
                                    }
                                </ul> : <ul>No installed templates</ul>
                        }
                    </div>
                    <div>
                    {
                        messagebarObject.isUpdateMsg ?
                            <MessageBar
                                messageBarType={messagebarObject.MessageBarType}
                                isMultiline={false}
                            >
                                {messagebarObject.MessagebarText}
                            </MessageBar>
                            : ""
                    }
                    </div>
                    {showModel ? <ShowDealTypeModel
                        showModel={showModel}
                        dealTypeJson={dealTypeJson}
                        modelMsgObject={modelMsgObject}
                        handleSaveButton={handleSaveButton}
                        closeModel={closeModel}
                        handleModelCheckbox={handleModelCheckbox}
                        createOpportunityTeam={createOpportunityTeam}
                        messagebarObject={messagebarObject} /> : null}
                </div>)
        }
    </div>);

};

export default ImportDelaTypeJson;