/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import { UserAgentApplication, Logger } from 'msal';
import { appUri, clientId , graphScopes, webApiScopes, graphScopesAdmin, authority} from '../helpers/AppSettings';
import appSettingsObject from '../helpers/AppSettings';
import Promise from 'promise';

const localStorePrefix = appSettingsObject.localStorePrefix;

const graphTokenStoreKey = localStorePrefix + 'GraphToken';
const webApiTokenStoreKey = localStorePrefix + 'WebApiToken';
const graphAdminTokenStoreKey = localStorePrefix + 'AdminGraphToken';
const userProfilPermissions = localStorePrefix + "UserProfilPermissions";

const optionsUserAgentApp = {
    navigateToLoginRequestUrl: true,
	cacheLocation: 'localStorage',
	logger: new Logger((level, message, containsPII) => {
		//const logger = level === 0 ? console.error : level === 1 ? console.warn : console.log;
		//logger(`AD: ${message}`);
		console.log(`AD: ${message}`);
    })
	//redirectUri: redirectUri
};

// Initialize th library
let userAgentApplication = new UserAgentApplication(
	clientId,
	authority,
	tokenReceivedCallback,
	optionsUserAgentApp);

function getUserAgentApplication() {
	return userAgentApplication;
}

function handleToken(accesstoken) {
	if (accesstoken) {
		localStorage.setItem(graphTokenStoreKey, accesstoken);
	}
}

function handleWebApiToken(idToken) {
	if (idToken) {
		console.log("handleWebApiToken-not empty");
		localStorage.setItem(webApiTokenStoreKey, idToken);
	}
}

function handleGraphAdminToken(idToken) {
	if (idToken) {
		console.log("handleGraphAdminToken-not empty");
		localStorage.setItem(graphAdminTokenStoreKey, idToken);
	}else
		console.log("handleGraphAdminToken is empty");
}

function handleUserProfilPermissions(userProfile) {
	if (userProfile) {
		console.log("handleUserProfilPermissions-not empty");
		localStorage.setItem(userProfilPermissions, userProfile);
	}
}

function handleError(error) {
    console.log(`AuthHelper: ${error}`);
}

function handleRemoveAuthFlags() {
    localStorage.setItem("AuthError", "");
    localStorage.setItem("AuthSeq", "start");
    localStorage.setItem("AuthSeqStatus", "");
    localStorage.setItem("AuthStatus", "");
    localStorage.setItem("AuthUserStatus", "");
    localStorage.setItem("AppTeams", "");
}

function tokenReceivedCallback(errorMessage, token, error, tokenType) {
	//This function is called after loginRedirect and acquireTokenRedirect. Use tokenType to determine context. 
	//For loginRedirect, tokenType = "id_token". For acquireTokenRedirect, tokenType:"access_token".
	localStorage.setItem("loginRedirect", "tokenReceivedCallback");
	if (!errorMessage && token) {
		this.acquireTokenSilent(graphScopes)
			.then(accessToken => {
				// Store token in localStore
				handleToken(accessToken);
				handleWebApiToken(token);
			})
			.catch(error => {
				handleError("tokenReceivedCallback-acquireTokenSilent: " + error);
				// TODO: need to add aquiretokenpopup or similar
			});
	} else {
		handleError("tokenReceivedCallback: " + error);
	}
}


export default class AuthClient
{
    constructor()
    {
		// Get the instance of UserAgentApplication.
		this.authClient = getUserAgentApplication();

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

	loginPopupGraphAdmin() {
        console.log("Admin: loginPopupGraphAdmin");
		return new Promise((resolve, reject) => {
			this.authClient.loginPopup(graphScopesAdmin)
				.then(function (idToken) {
					handleGraphAdminToken(idToken);
					resolve(idToken);
				})
				.catch((err) => {
					reject(err);
				});
		});
	}

	loginRedirectPromise() {
		return new Promise((resolve, reject) => {
			localStorage.setItem("loginRedirect", "loginRedirect start");
			this.authClient.loginRedirect(graphScopes)
				.then(function (idToken) {
					handleWebApiToken(idToken);
					localStorage.setItem("loginRedirect", "loginRedirect got access_token");
					resolve(idToken);
				})
				.catch((err) => {
					localStorage.setItem("loginRedirect", "AuthHelper_loginRedirect error: " + err);
					console.log("AuthHelper_loginRedirect error: " + err);
					reject(err);
				});
		});
	}

    acquireTokenSilent() {
        return new Promise((resolve, reject) => {
            this.authClient.acquireTokenSilent(graphScopes, authority)
                .then(function (accessToken) {
                    handleToken(accessToken);
                    resolve(accessToken);
                })
                .catch((err) => {
                    reject(err);
                });
        });
    }

    loginRedirect() {
        localStorage.setItem("loginRedirect", "start");
        localStorage.setItem("AuthRedirect", "start");
        return this.authClient.loginRedirect(graphScopes);
    }

    loginRedirectAdmin() {
        localStorage.setItem("loginRedirect", "start");
        localStorage.setItem("AuthRedirect", "start");
        return this.authClient.loginRedirect(graphScopesAdmin);
    }

    async acquireTokenSilentAdminAsync() {
        try {
            const res = await this.authClient.acquireTokenSilent(graphScopesAdmin, authority);
            handleGraphAdminToken(res);
            return res;
        } catch (err) {
            return "AuthHelper_acquireTokenSilentAdminAsync error: " + err;
        }
    }

    async acquireTokenSilentAsync() {
        try {
            const res = await this.authClient.acquireTokenSilent(graphScopes, authority);
            handleToken(res);
            return res;
        } catch (err) {
            return "AuthHelper_acquireTokenSilentAsync error: " + err;
        }
    }

    async acquireWebApiTokenSilentAsync() {
        try {
            const res = await this.authClient.acquireTokenSilent(webApiScopes, authority);
            handleWebApiToken(res);
            return res;
        } catch (err) {
            return "AuthHelper_acquireWebApiTokenSilentAsync error: " + err;
        }
    }

    async loginPopupAsync() {
        try {
            const res = await this.authClient.loginPopup(graphScopes);
            handleWebApiToken(res);
            return res;
        } catch (err) {
            return "AuthHelper_loginPopupAsync error: " + err;
        }
    }

    async loginPopupAdminAsync() {
        try {
            const res = await this.authClient.loginPopup(graphScopesAdmin);
            handleGraphAdminToken(res);
            return res;
        } catch (err) {
            return "AuthHelper_loginPopupAsync error: " + err;
        }
    }

    async userIsAuthenticatedAsync() {
        const graphTokenResult = localStorage.getItem(graphTokenStoreKey);
        const webapiTokenResult = localStorage.getItem(webApiTokenStoreKey);

        if (graphTokenResult && webapiTokenResult) {
            let userResult = this.getUser();
            if (userResult) {
                try {
                    const res = await this.apiGetUserProfile(userResult);
                    return res.userPrincipalName;
                } catch (err) {
                    return "AuthHelper_userIsAuthenticatedAsync error: " + err;
                }
            }

            return "error: getUser returned null";
        }

        return "error: user not authenticated one or more tokens missing";
    }

    async userHasGraphAdminToken() {
        const graphTokenResult = localStorage.getItem(graphAdminTokenStoreKey);

        if (graphTokenResult) {
            return true;
        } else {
            return false;
        }
    }

    async userHasWebApiToken() { // TODO: Need to refactor these functions
        const tokenResult = localStorage.getItem(webApiTokenStoreKey);

        if (tokenResult) {
            return true;
        } else {
            return false;
        }
    }

    async callGetUserProfile() {
        let returnObj = {
            roles: [],
            id: "",
            displayName: "",
            mail:"",
            userPrincipalName: "",
            permissions: [], 
            permissionsObj: [] 
        };
        console.log("AuthHelper_callGetUserProfile enter: ");
        try {
            localStorage.setItem("AuthStatusUP", "AuthHelper_callGetUserProfile start");
            const userPrincipalName = await this.getUser();
            console.log("AuthHelper_callGetUserProfile getUser: " + userPrincipalName.displayableId);

            if (userPrincipalName.displayableId.length > 0) {
				const endpoint = appUri + "/api/UserProfile?upn=" + userPrincipalName.displayableId;
                let token = window.authHelper.getWebApiToken();

                localStorage.setItem("AuthStatusUP", "AuthHelper_callGetUserProfile userPrincipalName: " + userPrincipalName.displayableId + " token: " + token);

                let data =  await this.callWebApiWithToken(endpoint, "GET");
                if(data){
                    console.log("AuthHelper_callGetUserProfile data: ",data);
                    this.userProfile = data;
                    if (data.userRoles.length > 0) {
                        let userpermissions = data.userRoles.map((userrole)=>{
                            return userrole.permissions.map((per)=>{
                                return per.name;
                            });
                        });
                        
                        let uniqueUserPermissions = userpermissions.filter((item, index)=>{
                            return userpermissions.indexOf(item) >= index;
                        });
                        console.log("AuthHelper_callGetUserProfile userpermissions: ",uniqueUserPermissions);
                        handleUserProfilPermissions(uniqueUserPermissions);

                        returnObj.roles = data.userRoles;
                        returnObj.id    = data.id;
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
            console.error("AuthHelper_callGetUserProfile error", error.message);
        }

        return returnObj;
    }

    getUserAsync() {
        return new Promise((resolve, reject) => {
            let res = this.authClient.getUser();
            if (res) {
                resolve(res);
            } else {
                reject(res);
            }
        });
    }

    getUser() {
        let userResult = this.authClient.getUser();
        if (userResult !== null && userResult !== undefined) {
            return userResult;
        }

        return false;
    }

    userIsAuthenticated() {
        let graphTokenResult = localStorage.getItem(graphTokenStoreKey);
        let webapiTokenResult = localStorage.getItem(webApiTokenStoreKey);

        if (graphTokenResult && webapiTokenResult) {
            let userResult = this.getUser();
            if (userResult) {
                this.apiGetUserProfile(userResult)
                    .then(res => {
                        return res.userPrincipalName;
                    })
                    .catch(err => {
                        return "error: " + err;
                    });
            }
        }

        return "error: user not authenticated";
    }

	acquireTokenSilentParam(extraQueryParameters) {
		return new Promise((resolve, reject) => {
            localStorage.setItem("AuthStatus", "AuthHelper_acquireTokenSilentParam started extraQueryParameters: " + extraQueryParameters.login_hint);
			this.authClient.acquireTokenSilent(graphScopes, authority, null, extraQueryParameters)
				.then(function (accessToken) {
                    console.log("AuthHelper_acquireTokenSilentParam got access_token");
                    localStorage.setItem("AuthStatus", "AuthHelper_acquireTokenSilentParam success resolving token");
					handleToken(accessToken);
					resolve(accessToken);
				})
                .catch((err) => {
                    localStorage.setItem("AuthRedirect", "err");
					localStorage.setItem("AuthError", "AuthHelper_acquireTokenSilentParam error: " + err);
					console.log("AuthHelper_acquireTokenSilentParam error: " + err);
					reject(err);
				});
		});
	}

	acquireWebApiTokenSilent() {
		return new Promise((resolve, reject) => {
			this.authClient.acquireTokenSilent(webApiScopes, authority)
				.then(function (accessToken) {
					handleWebApiToken(accessToken);
					resolve(accessToken);
				})
				.catch((err) => {
					reject(err);
				});
		});
	}

	acquireWebApiTokenSilentParam(extraQueryParameters) {
		return new Promise((resolve, reject) => {
            localStorage.setItem("AuthStatus", "AuthHelper_acquireTokenSilentParam started extraQueryParameters: " + extraQueryParameters.login_hint);
			this.authClient.acquireTokenSilent(webApiScopes, authority, null, extraQueryParameters)
				.then(function (accessToken) {
					console.log("AuthHelper_acquireTokenSilentParam got access_token");
					handleToken(accessToken);
					resolve(accessToken);
				})
                .catch((err) => {
                    localStorage.setItem("AuthRedirect", "err");
					localStorage.setItem("AuthError", "AuthHelper_acquireTokenSilentParam error: " + err);
					console.log("AuthHelper_acquireTokenSilentParam error: " + err);
					reject(err);
				});
		});
	}

	acquireGraphAdminTokenSilent() {
        console.log("Admin: acquireGraphAdminTokenSilent");
		return new Promise((resolve, reject) => {
            console.log("Admin: acquireGraphAdminTokenSilent in promise");
			this.authClient.acquireTokenSilent(graphScopesAdmin, authority)
				.then(function (accessToken) {
                    console.log("Admin: acquireGraphAdminTokenSilent calling handleGraphAdminToken : ", accessToken.length);
					handleGraphAdminToken(accessToken);
					resolve(accessToken);
				})
				.catch((err) => {
                    console.log("Admin: acquireGraphAdminTokenSilent err", err);
					reject(err);
				});
		});
	}

	accuireTokenAndWebTokenSilent() {
		this.acquireTokenSilent()
			.then(res => {
				this.acquireWebApiTokenSilent()
					.then(res => {
						localStorage.setItem("AuthError", "AuthHelper_accuireTokenAndWebTokenSilent_acquireWebApiTokenSilent done");
						return res;
					})
					.catch((err) => {
						localStorage.setItem("AuthError", "AuthHelper_accuireTokenAndWebTokenSilent_acquireWebApiTokenSilent error: " + err);
						console.log("AuthHelper_accuireTokenAndWebTokenSilent_acquireWebApiTokenSilent error: " + err);
						return err;
					});
			})
			.catch((err) => {
				localStorage.setItem("AuthError", "AuthHelper_accuireTokenAndWebTokenSilent_acquireTokenSilent error: " + err);
				console.log("AuthHelper_accuireTokenAndWebTokenSilent_acquireTokenSilent error: " + err);
				return err;
			});
	}

	getUserProfile() {
		return new Promise((resolve, reject) => {
			if (this.userProfile) {
				let userResult = this.getUser();
				if (userResult.displayableId === this.userProfile.userPrincipalName) {
					resolve(this.userProfile);
				}
				reject('null if');
			} else {
				reject('null if'); // TODO: Temporal return for debug
			}
		});
	}

	getUserProfilPermissions() {
        console.log("getUserProfilPermissions enter");
		if (!this.isAuthenticated()) {
			console.log("getUserProfilPermissions isAuth: false");
			return "";
		}
		let permissions = localStorage.getItem(userProfilPermissions);
        console.log("getUserProfilPermissions permissions : ", permissions);
		return permissions;
	}

	callCheckAccess(permissionRequested){
        return new Promise((resolve, reject) => {
			let permissions  = this.getUserProfilPermissions();

            if (permissions) {
                permissions = permissions.split(',').map(permission => permission.toLowerCase());
                console.log("AuthHelper_callCheckAccess PermissionsUserHave: ", permissions);
                console.log("AuthHelper_callCheckAccess PermissionsRequested: ", permissionRequested);
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

    callIsUserRWAccess() {
        return new Promise((resolve) => {
            let permission = {
                write: false,
                read: false
            };
            let permissions = this.getUserProfilPermissions();

            if (permissions) {
                permissions = permissions.split(',').map(permission => permission.toLowerCase());
                console.log("PermissionsUserHave: ", permissions);
                //console.log("PermissionsRequested: ", permissionRequested);
                if (permissions.length > 0) {
                    let checkReadWrite = ["Opportunities_ReadWrite_All", "Opportunity_ReadWrite_All", "Opportunity_ReadWrite_Partial"];
                    let chekckRead = ["Opportunities_Read_All", "Opportunity_Read_All","Opportunity_Read_Partial"];
                    let isReadWrite = false;
                    let isRead = false;
                    for (let i = 0; i < checkReadWrite.length; i++) {
                        if (permissions.indexOf(checkReadWrite[i].toLowerCase()) > -1) {
                            isReadWrite = true;
                            permission = {
                                write: true,
                                read: true
                            };
                            break;
                        }
                    }
                    // check Has readonly
                    if (!isReadWrite) {
                        for (let i = 0; i < chekckRead.length; i++) {
                            if (permissions.indexOf(chekckRead[i].toLowerCase()) > -1) {
                                isRead = true;
                                permission = {
                                    write: false,
                                    read: true
                                };
                            }
                            break;
                        }
                    }
                }

            }
            resolve(permission);
        });
    }

    apiGetUserProfile(userPrincipalName) {
        return new Promise((resolve, reject) => {
            // Call the Web API with the AccessToken

            if (userPrincipalName.displayableId.length > 0) {
                const endpoint = appUri + "/api/UserProfile?upn=" + userPrincipalName.displayableId;

                this.callWebApiWithToken(endpoint, "GET")
                    .then(data => {
                        if (data) {
                            resolve(data);
                        }
                        else {
                            reject("callGetUserProfile error in data: " + data);
                        }
                    })
                    .catch(function (err) {
                        handleError('Error when calling endpoint in callGetUserProfile: ' + JSON.stringify(err));
                        reject("callGetUserProfile error_callWebApiWithToken: " + err);
                    });
            } else {
                reject("Error when calling endpoint in callGetUserProfile: no current user exists in context");
            }
        });
    }

	isAuthenticated() {
        let graphTokenResult = localStorage.getItem(graphTokenStoreKey);
        let webapiTokenResult = localStorage.getItem(webApiTokenStoreKey);
		let userResult = this.getUser();
		let msalError = this.getAuthError();

		if (!userResult) {
			return false;
		}
        if (!graphTokenResult) {
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

	isCallBack(hash) {
		let isCallback = this.authClient.isCallback(hash);
		return isCallback;
	}

	getGraphToken() {
		if (!this.isAuthenticated()) {
			console.log("getGraphToken isAuth: false");
		}
		return localStorage.getItem(graphTokenStoreKey);
	}

	getWebApiToken() {
		if (!this.isAuthenticated()) {
			console.log("getWebApiToken isAuth: false");
		}
		return localStorage.getItem(webApiTokenStoreKey);
	}

	getGraphAdminToken() {
		if (!this.isAuthenticated()) {
			console.log("getGraphAdminToken isAuth: false");
		}
		return localStorage.getItem(graphAdminTokenStoreKey);
	}

	getAuthError() {
		return localStorage.getItem("msal.error");
	}

	getAuthRedirectState() {
		return localStorage.getItem("AuthRedirect");
	}

	getIdToken() {
		console.log("getIdToken");
		return localStorage.getItem('msal.idtoken');
	}

	logout(softLogout = false) {
        return new Promise((resolve, reject) => {
            localStorage.removeItem(graphTokenStoreKey);
            localStorage.removeItem(webApiTokenStoreKey);
            localStorage.removeItem(graphAdminTokenStoreKey);
            handleRemoveAuthFlags();
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
                                console.log("AuthHelper_callWebApiWithToken error:");
                                console.log(err);

                                // Detect expired token and request interactive logon
                                let errorText = localStorage.getItem("AuthError");
                                if (errorText.includes("login is required") || errorText.includes("login_required")) {
                                    localStorage.setItem("AuthSeq", "user_login_required");
                                }
								reject(err);
							});
					} else {
						response.json()
                            .then(function (data) {
                                console.log("AuthHelper_callWebApiWithToken data error: " + data.error.code);
                                //console.log(data);
								// Display response as error in the page
                                reject("AuthHelper_callWebApiWithToken data error: " + data.error.code);
							})
							.catch(function (err) {
                                console.log("AuthHelper_callWebApiWithToken end point: " + endpoint + " error:");
                                console.log(err);
                                reject("callWebApiWithToken error: " + err);
							});
					}
				})
				.catch(function (err) {
                    console.log("AuthHelper_callWebApiWithToken end point: " + endpoint + " error:");
                    console.log(err);
                    reject("callWebApiWithToken fetch endpoint: " + endpoint + " error: " + err);
				});
		});
	}
}