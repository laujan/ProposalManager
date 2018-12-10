function Check-DotNetFrameworkVersion
{
    [CmdletBinding()]
    param(
        [string]$Version = "4.6.1"
    )

    $dotNetRegistry  = 'SOFTWARE\Microsoft\NET Framework Setup\NDP'
    $dotNet4Registry = 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full'
    $dotNet4Builds = @{
        '30319'  = @{ Version = [System.Version]'4.0' }
        '378389' = @{ Version = [System.Version]'4.5' }
        '378675' = @{ Version = [System.Version]'4.5.1' }
        '378758' = @{ Version = [System.Version]'4.5.1' }
        '379893' = @{ Version = [System.Version]'4.5.2' }
        '380042' = @{ Version = [System.Version]'4.5' }
        '393295' = @{ Version = [System.Version]'4.6' }
        '393297' = @{ Version = [System.Version]'4.6' }
        '394254' = @{ Version = [System.Version]'4.6.1' }
        '394271' = @{ Version = [System.Version]'4.6.1' }
        '394802' = @{ Version = [System.Version]'4.6.2' }
        '394806' = @{ Version = [System.Version]'4.6.2' }
        '460798' = @{ Version = [System.Version]'4.7' }
        '460805' = @{ Version = [System.Version]'4.7' }
        '461308' = @{ Version = [System.Version]'4.7.1' }
        '461310' = @{ Version = [System.Version]'4.7.1' }
        '461808' = @{ Version = [System.Version]'4.7.0356' }
    }

    $v461 = new-object System.Version($Version)

    foreach($computer in $env:COMPUTERNAME)
    {
        if($regKey = [Microsoft.Win32.RegistryKey]::OpenRemoteBaseKey('LocalMachine', $computer))
        {
            if ($net4RegKey = $regKey.OpenSubKey("$dotNet4Registry"))
            {
                if(-not ($net4Release = $net4RegKey.GetValue('Release')))
                {
                    $net4Release = 30319
                }

                $result = $dotNet4Builds["$net4Release"].Version.CompareTo($v461);

                if($result -gt 0 -or $result -eq 0)
                {
                    return $true;
                }
            }
            else
            {
                return $false;
            }
        }
    }

    return $false
}