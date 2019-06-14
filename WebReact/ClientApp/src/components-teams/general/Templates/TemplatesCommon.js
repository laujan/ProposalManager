import React from 'react';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { Trans } from "react-i18next";

export default class TemplatesCommon {
    constructor(apiService, logService) {
        this.apiService = apiService;
        this.logService = logService;
    }

    async getAllTemplatesList() {
        let templateItemList = [];

        return await this.apiService.callApi('Template', 'GET')
            .then(response => {
                if (response.ok) {
                    return response.json();
                }
            })
            .then(data => {
                for (let i = 0; i < data.itemsList.length; i++) {
                    data.itemsList[i].createdDisplayName = data.itemsList[i].createdBy.displayName;
                    templateItemList.push(data.itemsList[i]);
                }

                return templateItemList;
            })
            .catch(error => this.logService.log("TemplatesList getTemplatesList: ", error));
    }

    async getAllProcess() {
        let processList = [], processGroupNumberList = [];

        return await this.apiService.callApi('Process', 'GET')
            .then(response => this.handleErrors(response).json())
            .then(data => {
                this.logService.log(data);
                processList = data.itemsList.map((process, key) => {
                    process.order = 0;
                    process.status = 0;
                    process.daysEstimate = 0;
                    processGroupNumberList.push(key + 1);
                    return process;
                });

                return processList;
            })
            .catch(error => this.logService.log(error.message));
    }

    async getGroups() {
        let groupList = [];

        return await this.apiService.callApi('Groups', 'GET')
            .then(response => this.handleErrors(response).json())
            .then(data => {
                this.logService.log(data);
                groupList = data;

                return groupList;
            })
            .catch(error => this.logService.log(error.message));
    }

    handleErrors(response) {
        this.logService.log("handleErrors: ", response);
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

    renderSpinner() {
        return (
            <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
            </div>
        );
    }
}