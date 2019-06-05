/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

export default class ApiService {
    constructor(token) {
        this.token = token;
        this.endpoint = `api`;
        this.authHelper = window.authHelper;
    }

    callApi(controller, method, args) {
        return new Promise((resolve, reject) => {
            let endpoint = `${this.endpoint}/${controller}`;
            let body, formData = null;
            let headers = { "Authorization": "Bearer " + this.token, 'Accept': '*/*' };

            if (args) {
                if (args.query) {
                    endpoint += `?${args.query}`;
                }
                else if (args.id) {
                    endpoint += `/${args.id}`;
                }

                if (args.body) {
                    body = args.body;
                    headers = { 'Content-Type': 'application/json', ...headers};
                }
                else if (args.formData) {
                    formData = args.formData;
                }
            }

            fetch(
                endpoint,
                {
                    headers: headers,
                    method,
                    body: !formData ? body : formData
                })
                .then(response => {
                    if (response.status === 401) {
                        this.authHelper.clearCache();
                        window.location.reload();
                    }

                    return resolve(response);
                })
            .catch(error => {
                    return reject(error);
                });
        });
    }
}

