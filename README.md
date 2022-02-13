# Exfiller
C# tool that can exfiltrate files to an external server using DNS requests as this port is often often allowed when a firewall blocks other outbound services.

The Python DNS Server is intended to be run on an external server and logs requests to a file named dns.log, but only if they include the domain being used in Exfiller.cs as the target domain to send the file to.

![image](https://user-images.githubusercontent.com/58894272/153729622-13e636af-b503-4c00-a8ff-e38fee457bff.png)

The Python server currently detects the beginning and end of the file, extracts and writes the file to disk, then clears the log and waits for more files.

![image](https://user-images.githubusercontent.com/58894272/153723639-d9e6405e-4ed9-4d52-8b1e-448ffadbdc43.png)


It is **NOT** stealthy at all at the moment, as seen above where 500+ DNS requests are sent in a matter of seconds.

**NOTE**: Currently needs to be built in .NET Core as a standalone app in a single file or there will be dependency errors due to the NuGet packages used for command-line arguments and DNS clients.  This unfortunately results in a 30mb file, but I'm not sure of a way around this as the built-in DNS queries for .NET don't allow specifying a DNS server other than the system defaults.

# Usage

```bash
Exfiller.exe --help

  -f, --file              Required. Target file to send.

  -s, --dns-server        Required. Target DNS Server.

  -p, --dns-port          (Default: 53) Target DNS Port (Defaults to 53).

  -d, --delay             (Default: 0) Delay (in milliseconds) between each request.

  -l, --request-length    (Default: 30) Length of the string to send in each request (Defaults to 30).

  --help                  Display this help screen.

  --version               Display version information.
```


# Estimated transfer times
The times below were seen between two local virtual machines, so real world times across the internet will likely be longer.  Each request is also sending 30 characters at a time, but the times could be shortened with longer requests (up to a maximum of 63 characters).  However, they also get more suspicious the longer they are.

|File Size|Transfer Time|Total Requests|Delay Added|
|---|---|---|---|
|1 MB|49 seconds|46000|0 ms|
|1 MB|1451 seconds (24 minutes, 11 seconds)|46000|20 ms|
|10 MB|486 seconds (8 mins, 6 secs)|486000|0 ms|
|10 MB|Around 4 hours? (haven't tested yet)|486000|20 ms|

# Manually extract files from Base64 requests
```bash
cat dns.log | grep -v "11111" | awk -F":" '{print$3}' | awk -F "." '{print$1}' | sed -z 's/\n//g' | sed -z 's/-/=/g' | base64 -d > NAME_OF_FILE.docx
```

![image](https://user-images.githubusercontent.com/58894272/153721860-f71d8d32-66df-4143-9db8-ef32d99323de.png)


# TO-DO
- Add other methods for exfiltration
  - HTTP/HTTPS over GET/POST requests
  - PowerShell alternative to avoid large file size



# Advisory
This tool should be used for authorized penetration testing and/or educational purposes only. Any misuse of this software will not be the responsibility of the author or of any other collaborator. Use it at your own machines and/or with the owner's permission.
