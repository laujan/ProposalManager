Import-Module MicrosoftTeams

function Create-NewTeam
{   
   param (   
            [Parameter(Mandatory = $true)]
            [string]$TeamName,
            [Parameter(Mandatory = $false)]
            [pscredential]$Credential
         )   
  Process
    {
        Import-Module MicrosoftTeams

        $username = $Credential.UserName
        
        Connect-MicrosoftTeams -Credential $Credential

        [int]$retriesLeft = 3
        [bool]$success = $false
        while(!$success)
        {
            try
            {
                $getteam = Get-Team | Where-Object { $_.DisplayName -eq $TeamName}
                $channels = @("Setup")
        
                if($getteam -eq $null)
                {
                    Write-Information "Start creating the team: $TeamName"
                    $group = New-Team -DisplayName $TeamName -AccessType Public

                    Write-Information "Creating channels..."
                    foreach($channel in $channels)
                    {
                        New-TeamChannel -DisplayName $channel -GroupId $group.GroupId -Description "$channel Channel"
                        Write-Information "Channel $channel was created"
                    }
                }

                Write-Information "The team $TeamName has been created"
                $success = $true
            }
            catch
            {
                if($retriesLeft)
                {
                    $retriesLeft -= 1
                    Write-Warning "MS Team creation failed. Retrying..."
                    Start-Sleep -Seconds 10
                }
                else
                {
                    Write-Error "MS Team creation failed after 3 retries."
                }
            }
        }

        Disconnect-MicrosoftTeams
    }
}
