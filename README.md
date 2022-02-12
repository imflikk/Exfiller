# Usage

```bash
Exfiller.exe --help

  -f, --file          Required. Target file to send.

  -s, --dns-server    Required. Target DNS Server.

  -p, --dns-port      Target DNS Port (Defaults to 53).

  -d, --delay         Delay (in milliseconds) between each request.

  --help              Display this help screen.

  --version           Display version information.
```

# Exfiller
C# tool that can exfiltrate files to an external server using DNS requests as this port is often often allowed when a firewall blocks other outbound services.

The Python DNS Server is intended to be run on an external server and logs requests to a file named dns.log, but only if they include the domain being used in Exfiller.cs as the target domain to send the file to.

![image](https://user-images.githubusercontent.com/58894272/153729622-13e636af-b503-4c00-a8ff-e38fee457bff.png)

The Python server currently detects the beginning and end of the file, extracts and writes the file to disk, then clears the log and waits for more files.

![image](https://user-images.githubusercontent.com/58894272/153723639-d9e6405e-4ed9-4d52-8b1e-448ffadbdc43.png)


It is **NOT** stealthy at all at the moment, as seen above where 500+ DNS requests are sent in a matter of seconds.


# Manually extract file from Base64 requests
```bash
cat dns.log | grep -v "11111" | awk -F":" '{print$3}' | awk -F "." '{print$1}' | sed -z 's/\n//g' | sed -z 's/-/=/g' | base64 -d > NAME_OF_FILE.docx
```

![image](https://user-images.githubusercontent.com/58894272/153721860-f71d8d32-66df-4143-9db8-ef32d99323de.png)


# TO-DO
- Add other methods for exfiltration
  - HTTP/HTTPS over GET/POST requests
