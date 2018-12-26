Function UpdateAppSettings {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$pathToJson,
        [Parameter(Mandatory = $true)] $inputParams,
        [Parameter(Mandatory = $false)][switch]$ProposalCreation,
        [Parameter(Mandatory = $false)][switch]$ProjectSmartLink
    )

    $appSettings = Get-Content -Path $pathToJson | ConvertFrom-Json -ErrorAction Stop
    
    if($ProposalCreation)
    {
        
        $appSettings.General.SiteId = $inputParams.SiteId
        $appSettings.General.ProposalManagerApiUrl = $inputParams.ProposalManagerUrl

        $appSettings.ProposalManager.ApiUrl = "$($inputParams.ProposalManagerUrl)/api"

        $appSettings.AzureAd.ClientId = $inputParams.ClientId
        $appSettings.AzureAd.ClientSecret = $inputParams.ClientSecret
        $appSettings.AzureAd.TenantId = $inputParams.TenantId
        $appSettings.AzureAd.ProposalManagerApiId = $inputParams.ProposalManagerApiId

    }
    elseif($ProjectSmartLink)
    {
        $appSettings.AzureAd.ClientId = $inputParams.ClientId
        $appSettings.AzureAd.ClientSecret = $inputParams.ClientSecret
        $appSettings.AzureAd.TenantId = $inputParams.TenantId
        $appSettings.AzureAd.SharePointUrl = $inputParams.SharePointUrl
        $appSettings.AzureAd.AllowedTenants = $inputParams.AllowedTenants

        $appSettings.ConnectionStrings.DefaultConnection.ConnectionString = $inputParams.ConnectionString
    }
    else
    {
    
        # AzureAd settings
        $appSettings.AzureAd.ClientId = $inputParams.ClientId
        $appSettings.AzureAd.ClientSecret = $inputParams.ClientSecret
        $appSettings.AzureAd.TenantId = $inputParams.TenantId
        $appSettings.AzureAd.Audience = $inputParams.ClientId
        $appSettings.AzureAd.Domain = "$($inputParams.TenantName).sharepoint.com"
        $appSettings.AzureAd.Authority = "https://login.microsoftonline.com/$($inputParams.TenantId)"
        $appSettings.AzureAd.BaseUrl = $inputParams.BaseUrl

        # Proposal Management settings
        $appSettings.ProposalManagement.SharePointHostName = $inputParams.SharePointHostName
        $appSettings.ProposalManagement.SharePointSiteRelativeName = $inputParams.SharePointSiteRelativeName
        $appSettings.ProposalManagement.BotName = $inputParams.BotName
        $appSettings.ProposalManagement.BotId = $inputParams.BotId
        $appSettings.ProposalManagement.MicrosoftAppId = $inputParams.MicrosoftAppId
        $appSettings.ProposalManagement.MicrosoftAppPassword = $inputParams.MicrosoftAppPassword
        $appSettings.ProposalManagement.AllowedTenants = $inputParams.AllowedTenants

        # Opportunity site provisioner webjob settings
        $appSettings.DocumentIdActivator.WebhookAddress = $inputParams.WebhookAddress
        $appSettings.DocumentIdActivator.WebhookUsername = $inputParams.WebhookUsername
        $appSettings.DocumentIdActivator.WebhookPassword = $inputParams.WebhookPassword
    
    }

    $appSettings | ConvertTo-Json | Set-Content $pathToJson

    Write-Information "AppSettings.json has been updated"
}