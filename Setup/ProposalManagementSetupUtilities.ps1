. .\RegisterAppV2.ps1

function New-PMSharePointSite {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdminSiteUrl,
        [Parameter(Mandatory = $true)]
        [string]$PMAdminUpn,
        [Parameter(Mandatory = $true)]
        [string]$PMSiteAlias,
        [Parameter(Mandatory = $true)]
        [pscredential]$Credential
    )
    process {
        # We open a PnP connection and do all we need in sequence to avoid having many connections open unnecessarily
        Connect-PnPOnline -Url $AdminSiteUrl -Credentials $Credential
        # First we create the site with the specified alias and store the url of the created site
        Write-Information "Creating the Proposal Manager SharePoint site..."
        $pmSiteUrl = New-PnPSite -Type TeamSite -Title "Proposal Management" -Alias $PMSiteAlias
        Write-Information "SharePoint site succesfully created"
        # And we immediately close the PnP connection
        Disconnect-PnPOnline

        # Afterwards, we open a regular SPO connection to make the desired PM admin the primary admin of the new site
        Connect-SPOService -Url $AdminSiteUrl -Credential $Credential
        Write-Information "Setting the SharePoint site administrator..."
        Set-SPOUser -Site $pmSiteUrl -LoginName $PMAdminUpn -IsSiteCollectionAdmin $true
        Write-Information "SharePoint site administrator set correctly."
        Disconnect-SPOService
        return $pmSiteUrl
    }
}

function New-PMGroupStructure {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [pscredential]$Credential
    )
    process {
        # Common attributes that should be applied to all Office 365 groups being created
        $groupsCommonAttributes = @{ }
        # Then we create the Office 365 groups corresponding to the PM roles (as the Getting Started guide asks)
        Write-Information "Connecting to Office 365 to create unified groups..."
        $exchangeSession = New-PSSession -ConfigurationName Microsoft.Exchange -ConnectionUri https://outlook.office365.com/powershell-liveid/ -Credential $Credential -Authentication Basic -AllowRedirection
        Import-PSSession $exchangeSession -DisableNameChecking
        Write-Information "Group creation has started"
        $groups = @(
            "Relationship Managers",
            "Loan Officers",
            "Legal Counsel",
            "Risk Officers",
            "Credit Analysts"
        )
        foreach($group in $groups)
        {
            Write-Information "Checking pre-existance of the $group group."
            if(!(Get-UnifiedGroup -Identity $group -ErrorAction SilentlyContinue))
            {
                Write-Information "$group group does not exist. Creating..."
                New-UnifiedGroup @groupsCommonAttributes -DisplayName $group
                Write-Information "$group group successfully created."
            }
            else
            {
                Write-Warning "$group group already exists."
            }
        }
        Write-Information "Group creation has been succesfully completed"
        Remove-PSSession $exchangeSession
    }
}

function New-PMSite {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PMSiteLocation,
        [Parameter(Mandatory = $true)]
        [string]$ApplicationName,
        [Parameter(Mandatory = $true)]
        [string]$Subscription
    )
    process {
        Write-Information "Starting resource group deployment in Azure..."
        Connect-AzureRmAccount -Subscription $Subscription
        New-AzureRmResourceGroup -Name $ApplicationName -Location $PMSiteLocation
        New-AzureRmResourceGroupDeployment -ResourceGroupName $ApplicationName -TemplateFile .\ProposalManagerARMTemplate.json -siteName $ApplicationName -siteLocation $PMSiteLocation
        Write-Information "Resource group deployment succeeded"
        Write-Information "Retrieving deployment credentials..."
        $xml = [xml](Get-AzureRmWebAppPublishingProfile -ResourceGroupName $ApplicationName -Name $ApplicationName -OutputFile .\settings.xml)
        # Extract connection information from publishing profile
        $username = [System.Linq.Enumerable]::Last($xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userName").value.Split('\'))
        $password = $xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userPWD").value
        $url = $xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@publishUrl").value
        Disconnect-AzureRmAccount
        Write-Information "Deployment credentials successfully retrieved"
        return @{ Username = $username; Password = $password; Url = $url }
    }
}

function New-PMTeamsAddInManifest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$AppUrl,
        [Parameter(Mandatory = $true)]
        [string]$AppDomain,
        [Parameter(Mandatory = $false)]
        [string]$BotId
    )
    process {
        Write-Information "Creating teams add-in package..."
        if(!$BotId)
        {
            $BotId = (New-Guid).ToString()
        }
        $manifest = (Get-Content ..\TeamsAddinPackage\manifest.json).
                    Replace('<addInId>', (New-Guid).ToString()).
                    Replace('<webAppUrl>', $AppUrl).
                    Replace('<botId>', $BotId).
                    Replace('<webDomain>', $AppDomain)
        if(Get-Item ProposalManager -ErrorAction SilentlyContinue)
        {
            rd ProposalManager -Recurse
        }
        md ProposalManager
        New-Item -Path .\ProposalManager\ -Name manifest.json -ItemType File
        Set-Content .\ProposalManager\manifest.json $manifest
        copy ..\TeamsAddinPackage\outline.png .\ProposalManager\outline.png
        copy ..\TeamsAddinPackage\color.png .\ProposalManager\color.png
        Compress-Archive .\ProposalManager\* .\ProposalManager.zip -CompressionLevel Fastest -Force
        rd ProposalManager -Recurse
        Write-Information "Package created successfully"
    }
}

function New-PMBot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Subscription,
        [Parameter(Mandatory = $true)]
        [string]$ApplicationName,
        [Parameter(Mandatory = $true)]
        [pscredential]$Credential,
        [Parameter(Mandatory = $true)]
        [string]$AppId,
        [Parameter(Mandatory = $true)]
        [string]$AppSecret
    )
    process {
        Write-Information "Beginning bot registration..."
        az login -u $Credential.UserName -p $Credential.GetNetworkCredential().Password
        az account set -s $Subscription
        $botJson = az bot create -k registration -v v3 -n $ApplicationName -g $ApplicationName --appid $AppId -p $AppSecret -e https://smba.trafficmanager.net/amer-client-ss.msg/
        $bot = $botJson | ConvertFrom-Json
        az bot msteams create -n $bot.name -g $bot.resourceGroup
        az logout
        return $bot
    }
}