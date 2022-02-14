# Exfiller
C#/PowerShell tool that can exfiltrate files to an external server using DNS requests as this port is often often allowed when a firewall blocks other outbound services.

The Python DNS Server is intended to be run on an external server and logs requests to a file named dns.log, but only if they include the domain being used in Exfiller.cs as the target domain to send the file to.

**Edit**: Added a Nim version as well mainly to practice using Nim, but also because the binary is much smaller than the standalone C# one while still being self-contained.

![image](https://user-images.githubusercontent.com/58894272/153729622-13e636af-b503-4c00-a8ff-e38fee457bff.png)

The Python server currently detects the beginning and end of the file, extracts and writes the file to disk, then clears the log and waits for more files.

![image](https://user-images.githubusercontent.com/58894272/153723639-d9e6405e-4ed9-4d52-8b1e-448ffadbdc43.png)


It is **NOT** stealthy at all at the moment, as seen above where 500+ DNS requests are sent in a matter of seconds.

**NOTE**: The C# version currently needs to be built in .NET Core as a standalone app in a single file or there will be dependency errors due to the NuGet packages used for command-line arguments and DNS clients.  This unfortunately results in a 30mb file, but I'm not sure of a way around this as the built-in DNS queries for .NET don't allow specifying a DNS server other than the system defaults.

# Usage

### Exfiller (C#/PowerShell)
The PowerShell version does not currently support using a different port and defaults to UDP 53.

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

### DNSServer.py
The Python script should be run on the external server, but should not require any additional libraries other than those installed by default.

```bash
python3 dnsserver.py --udp --port 53                                                                                                                                             

[*] Starting nameserver...

[*] UDP server loop running in thread: Thread-1

[*] Clearing log to prepare for new files...

----------------------------------------------

[*] Log cleared and waiting for new files...
----------------------------------------------
```


# Estimated transfer times
The times below were for the C# version (PowerShell is slightly slower) and seen between two local virtual machines, so real world times across the internet will likely be longer.  Each request is also sending 30 characters at a time, but the times could be shortened with longer requests (up to a maximum of 63 characters).  However, they also get more suspicious the longer they are.

|File Size|Transfer Time|Total Requests|Delay Added|
|---|---|---|---|
|1 MB|49 seconds|46000|0 ms|
|1 MB|1451 seconds (24 minutes, 11 seconds)|46000|20 ms|
|10 MB|486 seconds (8 mins, 6 secs)|486000|0 ms|
|10 MB|Around 4 hours? (haven't tested yet)|486000|20 ms|

# Manually extract files from Base64 requests
The Python server automatically extracts files and clears the log after the ending identifier is seen, but if for some reason this doesn't happen and you want to manually extract the file from an existing log the command below can be used.

```bash
cat dns.log | grep -v '11111' | awk -F':' '{{print$3}}' | awk -F '.' '{{print$1}}' | sed -z 's/\\n//g' | sed -z 's/-00-/+/g' | sed -z 's/-0-/\\//g' | sed -z 's/-/=/g' | base64 -d > files/NAME_OF_FILE.docx
```

![image](https://user-images.githubusercontent.com/58894272/153721860-f71d8d32-66df-4143-9db8-ef32d99323de.png)


# TO-DO
- Add other methods for exfiltration
  - HTTP/HTTPS over GET/POST requests
- (**DONE**) PowerShell alternative to avoid large file size
- Nim version for a smaller, portable binary (and for Nim practice)



# Advisory
This tool should be used for authorized penetration testing and/or educational purposes only. Any misuse of this software will not be the responsibility of the author or of any other collaborator. Use it at your own machines and/or with the owner's permission.
