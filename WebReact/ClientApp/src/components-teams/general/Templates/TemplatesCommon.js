import React from 'react';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { Trans } from "react-i18next";

async function getAllTemplatesList() {
    const data = await getTemplatesList();
    return data;
}

async function getAllProcess() {
    const data = await getProcess();
    return data;
}

 async function getTemplatesList(){
    let requestUrl = "api/template/";
    let templateItemList = [];
    let options = {
        method: "GET",
        headers: { 'authorization': 'Bearer    ' + window.authHelper.getWebApiToken() }
    };

    try {
        let response = await fetch(requestUrl, options);
        if (response.ok) {
            let data = await response.json();
            for (let i = 0; i < data.itemsList.length; i++) {
                data.itemsList[i].createdDisplayName = data.itemsList[i].createdBy.displayName;
                templateItemList.push(data.itemsList[i]);
            }
        }

    } catch (err) {
        console.log("TemplatesList getTemplatesList: " + err);
    } finally {
        return templateItemList;
    }
}

async function getProcess() {
    let processList = [], processGroupNumberList = [];
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
                    'authorization': 'Bearer ' + window.authHelper.getWebApiToken()
                }
            });
        let data = await handleErrors(response).json();
        console.log(data);
        processList = data.itemsList.map((process, key) => {
            process.order = 0;
            process.status = 0;
            process.daysEstimate = 0;
            processGroupNumberList.push(key + 1);
            return process;
        });
        this.hardcodedGroupNos.forEach(value => { processGroupNumberList.splice(processGroupNumberList.indexOf(value), 1); });

    } catch (error) {
        console.log(error.message);
    } finally {
        return processList;
    }
}

// GetGroups
async function getGroups() {
    let groupList = [];
    try {
        let requestUrl = "api/Groups";
        let response = await fetch(
            requestUrl,
            {
                method: "GET",
                headers:
                {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                    'authorization': 'Bearer ' + window.authHelper.getWebApiToken()
                }
            });
        let data = await handleErrors(response).json();
        console.log(data);
        groupList = data;

    } catch (error) {
        console.log(error.message);
    } finally {
        return groupList;
    }
}

//generic error handling functions
function handleErrors(response) {
    console.log("handleErrors==>", response);
    let ok = response.ok;
    if (!ok) {
        let status = response.status;
        let statusText = response.statusText;

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

function renderSpinner() {
    return (
        <div className='ms-BasicSpinnersExample ibox-content pt15 '>
            <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
        </div>
    );
}


export { getAllTemplatesList, getAllProcess, getGroups, renderSpinner };