using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Server.AO
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class MicroserverArguments
	{
		[Option('h', "hostname", Required = false,
		        HelpText = "The host name.",
		        Default = "LOCALHOST")]
		public string HostName { get; set; }

		[Option('p', "port", Required = false,
		        HelpText = "The port.",
		        Default = 5151)]
		public int Port { get; set; }

		[Option('c', "certificate", Required = false,
		        HelpText =
			        "The server certificate subject or thumbprint from the certificate store " +
			        "(Local Computer). Note that the certificate's private key must be accessible " +
			        "to this executable, unless the --key parameter is also specified. " +
			        "Alternatively this can be a PEM file containing the certificate " +
			        "chain (including the root certificate shared with the client).")]
		public string Certificate { get; set; }

		[Option('k', "key", Required = false,
		        HelpText =
			        "The private key PEM file (to remain on the server). If not specified and the " +
			        "certificate found in the store has a private exportable key the private key " +
			        "will be extracted from the certificate.")]
		public string PrivateKeyFile { get; set; }

		[Option(
			't', "mutual_TLS", Required = false,
			HelpText = "Enforce mutual authentication for transport layer security (SSL/TLS).",
			Default = false)]
		public bool EnforceMutualTls { get; set; }

		[Option('m', "maxparallel", Required = false,
		        HelpText =
			        "The maximum number of parallel processes. 0 means one less than the CPU count",
		        Default = 0)]
		public int MaxParallel { get; set; }

		[Option('v', "verbose", Required = false,
		        HelpText = "Set log level to verbose.")]
		public bool VerboseLogging { get; set; }

		[Option("test", Hidden = true,
		        HelpText = "Hidden and undocumented option to use Test configuration.")]
		public bool TestConfiguration { get; set; }

		[Usage]
		public static IEnumerable<Example> Examples
		{
			get
			{
				yield return new Example(
					"Simple example",
					new MicroserverArguments
					{
						HostName = "localhost",
						Port = 5151
					});

				yield return new Example(
					"Using transport layer security",
					new MicroserverArguments
					{
						HostName = "mycomputer.ourdomain.ch",
						Port = 5151,
						Certificate = "021f85bc637e33df8d8b1583ea2058e92c73335d"
					});
			}
		}

		public override string ToString()
		{
			string privateKey =
				string.IsNullOrEmpty(PrivateKeyFile) ? PrivateKeyFile : "**********";

			return
				$"Host Name: {HostName}, Port: {Port}, Certificate: {Certificate}, " +
				$"Private Key: {privateKey}, Enforce Mutual TLS: {EnforceMutualTls}, " +
				$"Maximum parallel processes: {MaxParallel}";
		}
	}
}
