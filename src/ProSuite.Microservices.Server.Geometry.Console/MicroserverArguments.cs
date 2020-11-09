using System.Text;
using CommandLine;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Server.Geometry.Console
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
			        "to this executable. Alternatively, a PEM file containing the certificate " +
			        "chain (including the root certificate shared with the client).")]
		public string Certificate { get; set; }

		[Option('k', "key", Required = false,
		        HelpText =
			        "Only relevant if the certificate is a PEM file: The private key PEM file (to remain on the server).")]
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

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"HostName: {HostName}, Port: {Port}");

			if (MaxParallel != 0)
			{
				sb.AppendLine($"MaxParallel: {MaxParallel}");
			}

			if (! string.IsNullOrEmpty(Certificate))
			{
				sb.AppendLine($"Certificate: {Certificate}");

				if (! string.IsNullOrEmpty(PrivateKeyFile))
				{
					sb.AppendLine("PrivateKey: ***********");
				}

				sb.Append($"EnforceMutualTLS: {EnforceMutualTls}");
			}

			return sb.ToString();
		}
	}
}
