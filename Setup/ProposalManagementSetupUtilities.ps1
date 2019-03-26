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
        [string]$OfficeTenantName,
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential,
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    process {
        if(!$Credential)
        {
            Write-Information "Please enter your Office 365 credentials in the dialog to connect to Sharepoint Online."
        }

        Connect-SPOService -Url $AdminSiteUrl -Credential $Credential
        try
        {
            $pmSiteUrl = Get-SPOSite -Identity "https://$OfficeTenantName.sharepoint.com/sites/$PMSiteAlias" -ErrorAction SilentlyContinue
        } catch { }
        if(!$pmSiteUrl)
        {
            # We open a PnP connection and do all we need in sequence to avoid having many connections open unnecessarily
            if(!$Credential)
            {
                Write-Information "If necessary, enter again your Office 365 credentials in the web dialog to connect to Pattern and Practices API."
                Connect-PnPOnline -Url $AdminSiteUrl -UseWebLogin
            }
            else 
            {
                Connect-PnPOnline -Url $AdminSiteUrl -Credentials $Credential
            }
            
            # First we create the site with the specified alias and store the url of the created site
            Write-Information "Creating the Proposal Manager SharePoint site..."
            $pmSiteUrl = New-PnPSite -Type TeamSite -Title "Proposal Management" -Alias $PMSiteAlias
            Write-Information "SharePoint site succesfully created"
            # And we immediately close the PnP connection
            Disconnect-PnPOnline

            # Afterwards, we use a regular SPO connection to make the desired PM admin the primary admin of the new site
            Write-Information "Setting the SharePoint site administrator..."
            Set-SPOUser -Site $pmSiteUrl -LoginName $PMAdminUpn -IsSiteCollectionAdmin $true
            Write-Information "SharePoint site administrator set correctly."
            Disconnect-SPOService
            return $pmSiteUrl
        }
        else
        {
            Disconnect-SPOService
            if($Force)
            {
                Write-Warning "SharePoint site for Proposal Manager already exists. The -Force flag was specified so the existing site will be used."
                return $pmSiteUrl
            }
            else
            {
                Write-Error "A SharePoint site with the name $PMSiteAlias already exists in tenant $OfficeTenantName. If you want to overwrite an existing installation of Proposal Manager, use the -Force flag."
            }
        }
    }
}

function New-PMGroupStructure {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential,
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    process {
        if(!$Credential)
        {
            Write-Information "Please enter your Office 365 credentials in the dialog to connect to Azure."
        }
        
        Connect-AzureAD -Credential $Credential

        # Common attributes that should be applied to all Office 365 groups being created
        $groupsCommonAttributes = @{ }
        
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
            if(!(Get-AzureADMSGroup -SearchString $group))
            {
                Write-Information "$group group does not exist. Creating..."
                New-AzureADMSGroup -DisplayName $group -MailEnabled $false -MailNickname "TestGroup" -SecurityEnabled $true -GroupTypes “Unified” 
                Write-Information "$group group successfully created."
            }
            else
            {
                if($Force)
                {
                    Write-Warning "$group group already exists. The -Force flag was specified so the existing group will be used."
                }
                else
                {
                    Write-Error "A group with the name $group already exists in your tenant. If you want to overwrite an existing installation of Proposal Manager, use the -Force flag."
                }
            }
        }

        Write-Information "Group creation has been succesfully completed"
    }
}

function New-PMSite {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PMSiteLocation,
        [Parameter(Mandatory = $false)]
        [string]$ResourceGroupName,
        [Parameter(Mandatory = $true)]
        [string]$ApplicationName,
        [Parameter(Mandatory = $true)]
        [string]$Subscription,
        [Parameter(Mandatory = $false)]
        [switch]$ExcludeProposalManager,
        [Parameter(Mandatory = $false)]
        [switch]$IncludeProposalCreation,
        [Parameter(Mandatory = $false)]
        [switch]$IncludeProjectSmartLink,
        [Parameter(Mandatory = $false)]
        [string]$SqlServerAdminUsername,
        [Parameter(Mandatory = $false)]
        [string]$SqlServerAdminPassword,
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    process {
        if(!$ResourceGroupName)
        {
            $ResourceGroupName = $ApplicationName
        }

        Write-Information "Starting resource group deployment in Azure. Please provide credentials."
        Connect-AzureRmAccount -Subscription $Subscription

        $existingResourceGroup = Get-AzureRmResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
        if($existingResourceGroup)
        {
            # Check if the group contains items
            $resourceGroupContents = Get-AzureRmResource | Where-Object {$_.ResourceGroupName –eq $ResourceGroupName}
            if($resourceGroupContents)
            {
                # If we're installing the addins, it is to be expected that the resource group has items in it.
                if(!$ExcludeProposalManager)
                {
                    if($Force)
                    {
                        Write-Warning "$ResourceGroupName resource group already exists, and has resources associated. The -Force flag was specified so the existing resource group will be emptied."

                        # Empty the resource group by deploying an empty template in Complete mode. This will automatically remove any resource without needing to iterate through all of them
                        $cleaningResult = New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName -Mode Complete -TemplateFile .\ResourceGroupCleanup.json -Force

                        if ($cleaningResult.ProvisioningState -eq "Failed") 
                        { 
                            Write-Error "Resource removal failed. Please check deployment status for deploy named 'ResourceGroupCleanup' in the Azure Portal for more details."
                        }

                        Write-Information "The existing resource group was successfully emptied to be able to redeploy to the same resource group."
                    }
                    else
                    {
                        Write-Error "A resource group with the name $ResourceGroupName already exists, and has resources associated. If you want to overwrite an existing installation of Proposal Manager, use the -Force flag."
                    }
                }
            }
            else 
            {
                Write-Information "The resource group $ResourceGroupName already exists, but it is empty. Installation will continue."
            }
        }
        else
        {
            New-AzureRmResourceGroup -Name $ResourceGroupName -Location $PMSiteLocation
            Write-Information "The resource group $ResourceGroupName has been created."
        }

        if($IncludeProjectSmartLink)
        {
            $sqlPassword = ($SqlServerAdminPassword | ConvertTo-SecureString -AsPlainText -Force)
            $deploymentResult = New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile .\ProposalManagerARMTemplate.json -includeProposalManager $(if($ExcludeProposalManager) {$false} else {$true})`
            -siteName $ApplicationName -siteLocation $PMSiteLocation -includeProposalCreation $(if($IncludeProposalCreation) {$true} else {$false}) `
            -includeProjectSmartLink $true -sqlServerAdminUsername $SqlServerAdminUsername -sqlServerAdminPassword $sqlPassword
        }
        else
        {
            $deploymentResult = New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile .\ProposalManagerARMTemplate.json -includeProposalManager $(if($ExcludeProposalManager) {$false} else {$true})`
            -siteName $ApplicationName -siteLocation $PMSiteLocation -includeProposalCreation $(if($IncludeProposalCreation) {$true} else {$false}) `
            -includeProjectSmartLink $false
        }

        if ($deploymentResult.ProvisioningState -eq "Failed") 
        { 
            Write-Error "Deployment failed. Please check deployment status in the Azure Portal for more details."
        }

        Write-Information "Resource group deployment succeeded"
        Write-Information "Retrieving deployment credentials..."

        $returnedInformation = @{}

        if(!$ExcludeProposalManager)
        {
            $xml = [xml](Get-AzureRmWebAppPublishingProfile -ResourceGroupName $ResourceGroupName -Name $ApplicationName -OutputFile .\settings.xml)    
            # Extract connection information from publishing profile
            $username = [System.Linq.Enumerable]::Last($xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userName").value.Split('\'))
            $password = $xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userPWD").value
            $url = $xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@publishUrl").value
            $returnedInformation = @{ Username = $username; Password = $password; Url = $url }
        }
        
        if($IncludeProposalCreation)
        {
            $pcxml = [xml](Get-AzureRmWebAppPublishingProfile -ResourceGroupName $ResourceGroupName -Name "$ApplicationName-propcreation" -OutputFile .\settings-propcreation.xml)
            $pcusername = [System.Linq.Enumerable]::Last($pcxml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userName").value.Split('\'))
            $pcpassword = $pcxml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userPWD").value
            $pcurl = $pcxml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@publishUrl").value
            $returnedInformation.PCUsername = $pcusername
            $returnedInformation.PCPassword = $pcpassword
            $returnedInformation.PCUrl = $pcurl
        }
        if($IncludeProjectSmartLink)
        {
            $pslxml = [xml](Get-AzureRmWebAppPublishingProfile -ResourceGroupName $ResourceGroupName -Name "$ApplicationName-projectsmartlink" -OutputFile .\settings-projectsmartlink.xml)
            $pslusername = [System.Linq.Enumerable]::Last($pslxml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userName").value.Split('\'))
            $pslpassword = $pslxml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userPWD").value
            $pslurl = $pslxml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@publishUrl").value
            $returnedInformation.PSLUsername = $pslusername
            $returnedInformation.PSLPassword = $pslpassword
            $returnedInformation.PSLUrl = $pslurl
        }
        Disconnect-AzureRmAccount
        Write-Information "Deployment credentials successfully retrieved"
        return $returnedInformation
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
        [Parameter(Mandatory = $false)]
        [string]$ResourceGroupName,
        [Parameter(Mandatory = $true)]
        [string]$ApplicationName,
        [Parameter(Mandatory = $false)]
        [pscredential]$Credential,
        [Parameter(Mandatory = $true)]
        [string]$AppId,
        [Parameter(Mandatory = $true)]
        [string]$AppSecret
    )
    process {
        if(!$ResourceGroupName)
        {
            $ResourceGroupName = $ApplicationName
        }
        
        Write-Information "Beginning bot registration..."

        if(!$Credential)
        {
            Write-Information "Please enter your Office 365 credentials in the dialog to connect to Azure."

            az login
        }
        else 
        {
            az login -u $Credential.UserName -p $Credential.GetNetworkCredential().Password
        }
        
        az account set -s $Subscription
        $botJson = az bot create -k registration -v v3 -n $ApplicationName -g $ResourceGroupName --appid $AppId -p $AppSecret -e https://smba.trafficmanager.net/amer-client-ss.msg/
        $bot = $botJson | ConvertFrom-Json
        az bot msteams create -n $bot.name -g $bot.resourceGroup
        az logout
        return $bot
    }
}