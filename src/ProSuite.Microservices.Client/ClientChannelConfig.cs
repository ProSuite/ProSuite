using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Client
{
	[UsedImplicitly]
	public class ClientChannelConfig
	{
		/// <summary>
		/// Parses a connection url of the form http://localhost:5151 or
		/// https://127.0.0.1:5150.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public static ClientChannelConfig Parse(string url)
		{
			ClientChannelConfig result;

			string message;
			if (TryParse(url, out result, out message))
			{
				return result;
			}

			throw new ArgumentException(message);
		}

		public static bool TryParse(string url, out ClientChannelConfig result,
		                            out string message)
		{
			result = new ClientChannelConfig();
			message = null;

			Uri uri = new Uri(url, UriKind.Absolute);

			if (uri.Scheme.Equals("https", StringComparison.InvariantCultureIgnoreCase))
			{
				result.UseTls = true;
			}
			else if (uri.Scheme.Equals("http", StringComparison.InvariantCultureIgnoreCase))
			{
				result.UseTls = false;
			}
			else
			{
				message = $"Invalid address: {url}. It must start with http or https.";
				return false;
			}

			if (string.IsNullOrEmpty(uri.Host))
			{
				message = $"Invalid address: {url}. Host name is empty.";
				return false;
			}

			result.HostName = uri.Host;

			result.Port = uri.Port;

			return true;
		}

		public string HostName { get; set; }

		public int Port { get; set; }

		public bool UseTls { get; set; }

		[CanBeNull]
		public string ClientCertificate { get; set; }
	}
}
