function RegisterApp {
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$false)]
        [string]$ApplicationName,
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential,
        [Parameter(Mandatory=$false)]
        [string[]]$RelativeReplyUrls,
        [Parameter(Mandatory=$false)]
        [string[]]$DelegatedPermissions,
        [Parameter(Mandatory=$false)]
        [string[]]$ApplicationPermissions,
        [Parameter(Mandatory=$false)]
        [string[]]$AdditionalPreAuthorizedAppIds,
        [Parameter(Mandatory=$false)]
        [string]$AddTo,
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )

    if(!$RelativeReplyUrls)
    {
        $RelativeReplyUrls = @('Setup', 'tab/config', 'tab/tabauth', 'tab', 'tabMob/generalDashboardTab', 'tabMob/generalAdministrationTab', 
        'tabMob/generalConfigurationTab', 'tabMob/customerDecisionTab', 'tabMob/checklistTab', 'tabMob/proposalStatusTab', 'tabMob/rootTab', [string]::Empty)
    }

    Connect-AzureAD -Credential $Credential

    # ADAL JSON token - necessary for making requests to Graph API
    $token = GetAuthToken -Credential $Credential

    $secretGuid = New-Guid
    $guidBytes = [System.Text.Encoding]::UTF8.GetBytes($secretGuid)
    $secretText =[System.Convert]::ToBase64String($guidBytes)
    $clientSecret = @{
        'endDateTime'=[DateTime]::UtcNow.AddDays(365).ToString('u').Replace(' ', 'T');
        'keyId'=$secretGuid;
        'hint'=$secretGuid;
        'startDateTime'=[DateTime]::UtcNow.AddDays(-1).ToString('u').Replace(' ', 'T');  
        'secretText'=$secretText;
    }

    $replyUrls = $RelativeReplyUrls | % { "https://$ApplicationName.azurewebsites.net/" + $_ } | ConvertTo-Json
    Write-Debug $replyUrls

    # REST API header with auth token
    $authHeader = @{
        'Content-Type'='application/json';
        'Authorization'=$token.CreateAuthorizationHeader()
    }

    Write-Information "Creating application $ApplicationName"
    $uri = "https://graph.microsoft.com/beta/applications"
    [bool]$reused = $false

    # Check if registration with given name already exists
    $result = Invoke-RestMethod -Uri ($uri + "?`$filter=displayName eq '$ApplicationName'") -Headers $authHeader -Method GET

    if ($result.value.Length -gt 0)
    {
        if($Force)
        {
            Write-Warning "$ApplicationName app registration already exists. The -Force flag was specified so the existing registration will be reused with new credentials."

            # Obtain last registration
            $appId = $result.value[$result.value.Length - 1].appId;

            # Generate new secret
            $data = "{
                `"passwordCredentials`": [$(ConvertTo-Json -InputObject $clientSecret)]
            }";

            $result = Invoke-RestMethod -Uri "$($uri)/$($result.value[$result.value.Length - 1].id)" -Headers $authHeader -Body $data -Method PATCH

            $reused = $true
        }
        else
        {
            Write-Error "An application with the name $ApplicationName already exists. If you want to overwrite an existing installation of Proposal Manager, use the -Force flag."
        }
    }
    else
    {    
        $data = "{
            `"displayName`": `"$ApplicationName`",
            `"passwordCredentials`": [$(ConvertTo-Json -InputObject $clientSecret)],
            `"web`": {
                `"logoutUrl`": `"https://$ApplicationName.azurewebsites.net`",
                `"redirectUris`": $replyUrls,
                `"implicitGrantSettings`": {
                    `"enableIdTokenIssuance`": true,
                    `"enableAccessTokenIssuance`": true
                }
            }
        }";

        $result = Invoke-RestMethod -Uri $uri -Headers $authHeader -Body $data -Method POST

        $appId = $result.appId
        # Add permissions
        $scopeGuid = New-Guid
        $oauthPerms = "{
            `"adminConsentDescription`": `"Allow the client application to access Proposal Manager WebApi on behalf of the signed-in user.`",
            `"adminConsentDisplayName`": `"Access Proposal Manager WebApi`",
            `"id`": `"$scopeGuid`",
            `"isEnabled`": true,
            `"type`": `"User`",
            `"userConsentDescription`": `"Allow the client application to access Proposal Manager WebApi on your behalf.`",
            `"userConsentDisplayName`": `"Access Proposal Manager WebApi`",
            `"value`": `"access_as_user`"
        }"

        $api = "{
            `"requestedAccessTokenVersion`": 2,
            `"oauth2PermissionScopes`": [ $oauthPerms ]
        }"

        Write-Information "Setting oauth2 Permissions"
        $update = "{
            `"identifierUris`": [`"api://$appId`"],
            `"api`": $api
        }"

        Invoke-RestMethod -Uri "$($uri)/$($result.id)" -Headers $authHeader -Body $update -Method PATCH

        Write-Information "Setting Graph Permissions"

        if(!$DelegatedPermissions)
        {
            $DelegatedPermissions =
                '64a6cdd6-aab1-4aaf-94b8-3cc8405e90d0', # email
                '7427e0e9-2fba-42fe-b0c0-848c9e6a8182', # offline_access
                '37f7f235-527c-4136-accd-4a02d197296e', # openid
                '14dad69e-099b-42c9-810b-d002981feec1', # profile
                'e1fe6dd8-ba31-4d61-89e7-88639da4683d', # User.Read
                '4e46008b-f24c-477d-8fff-7bb4ec7aafe0', # Group.ReadWrite.All
                '1ca167d5-1655-44a1-8adf-1414072e1ef9'  # AppCatalog.ReadWrite.All
        }

        if(!$ApplicationPermissions)
        {
            $ApplicationPermissions =
                '75359482-378d-4052-8f01-80520e7db3cd', # Files.ReadWrite.All
                '62a82d76-70ea-41e2-9197-370581804d09', # Group.ReadWrite.All
                '0c0bf378-bf22-4481-8f81-9e89a9b4960a', # Sites.Manage.All
                '741f803b-c850-494e-b5df-cde7c675a1ca'  # User.ReadWrite.All
        }

        $ofType = [System.Linq.Enumerable].GetMethod("OfType").MakeGenericMethod([System.Object]);
    
        #Delegated: Scope
        #Application: Role
        $graphPermissions = ConvertTo-Json (
            ($ApplicationPermissions | % { @{ id = $_ ; type = 'Role' } }) +
            ($DelegatedPermissions | % { @{ id = $_ ; type = 'Scope' } }))
        

        $requiredResourceAccess = "[
            {
              `"resourceAppId`": `"00000003-0000-0000-c000-000000000000`",
              `"resourceAccess`": $graphPermissions
            },
            {
                `"resourceAppId`": `"$appId`",
                `"resourceAccess`": [
                    {
                        `"id`": `"$scopeGuid`",
                        `"type`": `"Scope`"
                    }
                ]
            }
        ]"

        $updatePerm = "{
            `"requiredResourceAccess`": $requiredResourceAccess
        }"

        Write-Debug $updatePerm

        Invoke-RestMethod -Uri "$($uri)/$($result.id)" -Headers $authHeader -Body $updatePerm -Method PATCH

        Write-Information "Add PreAuthorized apps"
        # Add PreAuthorized applications
        $preAuthorizedApps = "[ {
            `"appId`": `"$appId`",
            `"permissionIds`": [ `"$scopeGuid`" ]
        }]"

        if($AdditionalPreAuthorizedAppIds)
        {
            $parsedPreAuthorizedApps = $preAuthorizedApps | ConvertFrom-Json
            $preAuthorizedApps = $parsedPreAuthorizedApps + ($AdditionalPreAuthorizedAppIds | % { @{ appId = $_ ; permissionIds = , $scopeGuid } }) | ConvertTo-Json
        }

        $addPreAuth = "{
        `"api`": { 
                `"preAuthorizedApplications`": $preAuthorizedApps
            }
        }"

        Invoke-RestMethod -Uri "$($uri)/$($result.id)" -Headers $authHeader -Body $addPreAuth -Method PATCH

        if($AddTo)
        {

            $resultAddTo = Invoke-RestMethod -Uri "$($uri)/?filter=appId eq '$AddTo'" -Headers $authHeader

            $authScope = $resultAddTo.value.api.oauth2PermissionScopes[0].id

            $newPreAuthorized = $resultAddTo.value.api.preAuthorizedApplications + @{ appId = $appId; permissionIds = @(, $authScope)}
                
            $addPreAuth = "{
            `"api`": { 
                    `"preAuthorizedApplications`": $($newPreAuthorized | ConvertTo-Json)
                }
            }"

            Invoke-RestMethod -Uri "$($uri)/$($resultAddTo.value.id)" -Headers $authHeader -Body $addPreAuth -Method PATCH
        }
    }

    Disconnect-AzureAD

    Write-Information "The application $ApplicationName has been successfully registered"

    return @{ AppId = $appId; AppSecret = $secretText; Reused = $reused ; }
}

Function GetAuthToken
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential
    )
    Import-Module Azure

    $clientId = "1950a258-227b-4e31-a9cf-717495945fc2"     
    $resourceAppIdURI = "https://graph.microsoft.com"
    $authority = "https://login.microsoftonline.com/common"
    $authContext = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext" -ArgumentList $authority

    if(!$Credential)
    {
        Write-Information "Please enter your Office 365 credentials in the dialog to connect to Azure."

        $redirectUri = "urn:ietf:wg:oauth:2.0:oob"
        $promptBehavior = [Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior]::Always

        $authResult = $authContext.AcquireToken($resourceAppIdURI, $clientId, $redirectUri, $promptBehavior)        
    }
    else 
    {
        $AADCredential = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.UserCredential" -ArgumentList $credential.UserName,$credential.Password

        $authResult = $authContext.AcquireToken($resourceAppIdURI, $clientId, $AADCredential)
    }

    return $authResult
}