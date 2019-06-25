/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import { UserAgentApplication, Logger } from 'msal';
import { appUri, clientId, graphScopes, webApiScopes, authority } from '../helpers/AppSettings';
import appSettingsObject from '../helpers/AppSettings';
import Promise from 'promise';
import LoggingService from '../helpers/LoggingService';
const localStorePrefix = appSettingsObject.localStorePrefix;
const webApiTokenStoreKey = localStorePrefix + 'WebApiToken';
const userProfilPermissions = localStorePrefix + "UserProfilPermissions";

const optionsUserAgentApp = {
    navigateToLoginRequestUrl: false,
    cacheLocation: 'localStorage',
    logger: new Logger((level, message, containsPII) => {
        logService.log(`AD: ${message}`);
    })
};

const logService = new LoggingService();
// Initialize th library
let userAgentApplication = new UserAgentApplication(
    clientId,
    authority,
    tokenReceivedCallback,
    optionsUserAgentApp);

function getUserAgentApplication() {
    return userAgentApplication;
}

function handleWebApiToken(idToken) {
    if (idToken) {
        logService.log("handleWebApiToken-not empty");
        localStorage.setItem(webApiTokenStoreKey, idToken);
    }
}

function handleUserProfilPermissions(userProfile) {
    if (userProfile) {
        logService.log("handleUserProfilPermissions-not empty");
        localStorage.setItem(userProfilPermissions, userProfile);
    }
}

function handleError(error) {
    logService.log(`AuthHelper: ${error}`);
}

function tokenReceivedCallback(errorMessage, token, error, tokenType) {
    //This function is called after loginRedirect and acquireTokenRedirect. Use tokenType to determine context. 
    //For loginRedirect, tokenType = "id_token". For acquireTokenRedirect, tokenType:"access_token".
    localStorage.setItem("tokenReceivedCallback", `${tokenType}|||${token}`);
    if (!errorMessage && token) {
        this.acquireTokenSilent(webApiScopes)
            .then(accessToken => {
                // Store token in localStore
                localStorage.setItem("tokenReceivedCallback_accessToken", `${tokenType}|||${accessToken}`);
                handleWebApiToken(accessToken);
            })
            .catch(error => {
                handleError("tokenReceivedCallback-acquireTokenSilent: " + error);
            });
    } else {
        handleError("tokenReceivedCallback: " + error);
    }
}

export default class AuthClient {
    constructor() {
        logService.log("AuthClient_ctor");
        // Get the instance of UserAgentApplication.
        this.authClient = getUserAgentApplication();
        logService.log("authclient", this.authClient);
        this.userProfile = [];
    }

    loginPopup() {
        return new Promise((resolve, reject) => {
            this.authClient.loginPopup(graphScopes)
                .then(function (idToken) {
                    handleWebApiToken(idToken);
                    resolve(idToken);
                })
                .catch((err) => {
                    reject(err);
                });
        });
    }

    loginRedirect() {
        return this.authClient.loginRedirect(graphScopes);
    }

    async acquireWebApiTokenSilentAsync() {
        try {
            const res = await this.authClient.acquireTokenSilent(webApiScopes, authority);
            handleWebApiToken(res);
            return res;
        } catch (err) {
            throw new Error("AuthHelper_acquireWebApiTokenSilentAsync error: " + err);
        }
    }

    async loginPopupAsync() {
        try {
            const res = await this.authClient.loginPopup(graphScopes);
            handleWebApiToken(res);
            logService.log("AuthHelper_loginPopupAsync", res);
            return res;
        } catch (err) {
            throw new Error("AuthHelper_loginPopupAsync error: " + err);
        }
    }

    async callGetUserProfile() {
        let returnObj = {
            roles: [],
            id: "",
            displayName: "",
            mail: "",
            userPrincipalName: "",
            permissions: [],
            permissionsObj: []
        };
        logService.log("AuthHelper_callGetUserProfile enter: ");
        try {
            const userPrincipalName = await this.getUser();
            logService.log("AuthHelper_callGetUserProfile getUser: " + userPrincipalName.displayableId);

            if (userPrincipalName.displayableId.length > 0) {
                const endpoint = appUri + "/api/UserProfile?upn=" + userPrincipalName.displayableId;
                let token = window.authHelper.getWebApiToken();
                let data = await this.callWebApiWithToken(endpoint, "GET");
                if (data) {
                    logService.log("AuthHelper_callGetUserProfile data: ", data);
                    this.userProfile = data;
                    if (data.userRoles.length > 0) {
                        let userpermissions = data.userRoles.map((userrole) => {
                            return userrole.permissions.map((per) => {
                                return per.name;
                            });
                        });

                        let uniqueUserPermissions = userpermissions.filter((item, index) => {
                            return userpermissions.indexOf(item) >= index;
                        });
                        logService.log("AuthHelper_callGetUserProfile userpermissions: ", uniqueUserPermissions);
                        handleUserProfilPermissions(uniqueUserPermissions);

                        returnObj.roles = data.userRoles;
                        returnObj.id = data.id;
                        returnObj.displayName = data.displayName;
                        returnObj.mail = data.mail;
                        returnObj.userPrincipalName = data.userPrincipalName;
                        returnObj.permissions = uniqueUserPermissions;
                        returnObj.permissionsObj = [];
                    }
                }
            } else {
                throw new Error("Error when calling endpoint in callGetUserProfile: no current user exists in context");
            }
        } catch (error) {
            throw new Error("AuthHelper_callGetUserProfile error" + error.message);
        }

        return returnObj;
    }

    clearCache() {
        logService.log("AuthHelper clearCache");
        localStorage.removeItem(webApiTokenStoreKey);
        localStorage.removeItem(userProfilPermissions);

        return this.authClient.clearCache();
    }

    getUser() {
        return this.authClient.getUser();
    }

    getUserProfilPermissions() {
        logService.log("getUserProfilPermissions enter");
        let permissions = localStorage.getItem(userProfilPermissions);
        logService.log("getUserProfilPermissions permissions : ", permissions);
        return permissions;
    }

    callCheckAccess(permissionRequested) {
        return new Promise((resolve, reject) => {
            let permissions = this.getUserProfilPermissions();

            if (permissions) {
                permissions = permissions.split(',').map(permission => permission.toLowerCase());
                logService.log("AuthHelper_callCheckAccess PermissionsUserHave: ", permissions);
                logService.log("AuthHelper_callCheckAccess PermissionsRequested: ", permissionRequested);
                if (permissions.length > 0) {
                    for (let i = 0; i < permissionRequested.length; i++) {
                        if (permissions.indexOf(permissionRequested[i].toLowerCase()) > -1) {
                            resolve(true);
                        }
                    }
                }
                else {
                    reject("AuthHelper_callCheckAccess permissions.length = 0");
                }
                reject("AuthHelper_callCheckAccess no permission match");
            }
            else {
                reject("AuthHelper_callCheckAccess permissions is null");
            }
        });
    }

    isAuthenticated() {
        let webapiTokenResult = localStorage.getItem(webApiTokenStoreKey);
        let userResult = this.getUser();
        let msalError = this.getAuthError();

        if (!userResult) {
            return false;
        }

        if (!webapiTokenResult) {
            return false;
        }

        if (msalError) {
            return false;
        }

        return true;
    }

    getWebApiToken() {
        if (!this.isAuthenticated()) {
            logService.log("getWebApiToken isAuth: false");
        }
        return localStorage.getItem(webApiTokenStoreKey);
    }

    getAuthError() {
        return localStorage.getItem("msal.error");
    }

    logout(softLogout = false) {
        return new Promise((resolve, reject) => {
            localStorage.removeItem(webApiTokenStoreKey);
            //Granular access start
            localStorage.removeItem(userProfilPermissions);
            //Granular access end

            if (!softLogout) {
                this.authClient.logout()
                    .then(res => {
                        resolve(res);
                    })
                    .catch(err => {
                        reject(err);
                    });
            }
            resolve("softLogout");
        });
    }

    callWebApiWithToken(endpoint, method) {
        return new Promise((resolve, reject) => {
            let token = window.authHelper.getWebApiToken();

            fetch(endpoint, {
                method: method,
                headers: { 'authorization': 'Bearer ' + token }
            })
                .then(function (response) {
                    var contentType = response.headers.get("content-type");
                    if (response.status === 200 && contentType && contentType.indexOf("application/json") !== -1) {
                        response.json()
                            .then(function (data) {
                                // return response
                                resolve(data);
                            })
                            .catch(function (err) {
                                logService.log("AuthHelper_callWebApiWithToken error:");
                                logService.log(err);

                                // Detect expired token and request interactive logon
                                let errorText = localStorage.getItem("AuthError");
                                if (errorText.includes("login is required") || errorText.includes("login_required")) {
                                    logService.log("user_login_required");
                                }
                                reject(err);
                            });
                    } else {
                        response.json()
                            .then(function (data) {
                                logService.log("AuthHelper_callWebApiWithToken data error: " + data.error.code);
                                // Display response as error in the page
                                reject("AuthHelper_callWebApiWithToken data error: " + data.error.code);
                            })
                            .catch(function (err) {
                                logService.log("AuthHelper_callWebApiWithToken end point: ", endpoint, " error:", err);
                                reject("callWebApiWithToken error: " + err);
                            });
                    }
                })
                .catch(function (err) {
                    logService.log("AuthHelper_callWebApiWithToken end point: ", endpoint, " error:", err);
                    reject("callWebApiWithToken fetch endpoint: " + endpoint + " error: " + err);
                });
        });
    }
}