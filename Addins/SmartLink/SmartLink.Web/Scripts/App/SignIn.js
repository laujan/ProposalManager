$(function () {
    Office.initialize = function (reason) {
        $(document).ready(function () {
            var isDev = window.location.href.indexOf("localhost") > -1;
            var mode = { word: false, excel: false, powerPoint: false };
            var isInIframe = function () {
                try {
                    return window.self !== window.top;
                }
                catch (e) {
                    return true;
                }
            };

            if (Office.context.requirements.isSetSupported("WordApi")) {
                mode.word = true;
            }
            else if (Office.context.requirements.isSetSupported("ExcelApi")) {
                mode.excel = true;
            }
            else if (Office.context.requirements.isSetSupported("ActiveView")) {
                mode.powerPoint = true;
            }

            if (mode.word || mode.excel || mode.powerPoint) {
                if (isDev || (Office.context.document.url
                    && (Office.context.document.url.toUpperCase().indexOf("HTTP") > -1 || Office.context.document.url.toUpperCase().indexOf("HTTPS") > -1))) {
                    if ((Office.context.requirements.isSetSupported('ExcelApi', 1.3) && mode.excel) || (Office.context.requirements.isSetSupported("WordApi", 1.2) && mode.word) || mode.powerPoint) {
                        $(".sign-in").show();
                        $("#btnSignIn").click(function () {
                            
                            $(".loading-login").show();

                            var location;
                            if (mode.word) {
                                location = "/Word/Point";
                            }
                            else if (mode.excel) {
                                location = "/Excel/Point";
                            }
                            else if (mode.powerPoint) {
                                location = "/PowerPoint/Point";
                            }

                            if (isInIframe()) {
                                microsoftTeams.initialize();

                                microsoftTeams.authentication.authenticate({
                                    url: '/auth',
                                    width: 600,
                                    height: 535,
                                    successCallback: function (result) {
                                        sessionStorage["token"] = result.idToken;
                                        window.location.replace(location);
                                    },
                                    failureCallback: function (err) {
                                        console.log(err);
                                    }
                                });
                            }
                            else {
                                var authenticationContext = new AuthenticationContext(config);

                                // Check For & Handle Redirect From AAD After Login
                                if (authenticationContext.isCallback(window.location.hash)) {
                                    authenticationContext.handleWindowCallback();
                                }
                                else {
                                    var user = authenticationContext.getCachedUser();
                                    if (user && window.parent === window && !window.opener) {

                                        authenticationContext.acquireToken(config.clientId,
                                            function (errorDesc, token, error) {
                                                if (error) {
                                                    console.log("AzureAD error:", error, errorDesc);
                                                    authenticationContext.acquireTokenRedirect(config.clientId, null, null);
                                                }
                                                else {
                                                    sessionStorage["token"] = token;
                                                    window.location.replace(location);
                                                }
                                            });
                                    }
                                    else {
                                        authenticationContext.login();
                                    }
                                }
                            }

                        });
                    }
                    else {
                        $("#error-message").addClass(mode.word ? "word-version" : "excel-version");
                    }
                }
                else {
                    $("#error-message").addClass(mode.word ? "word-mode" : (mode.excel ? "excel-mode" : "powerpoint-mode"));
                }
            }
        });
    };
});