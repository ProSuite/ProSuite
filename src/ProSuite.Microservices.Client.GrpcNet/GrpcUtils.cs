using System;
using System.Net;
using System.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging.Abstractions;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Client.GrpcNet
{
	public static class GrpcUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

#if NET8_0
		// NOTE: We actually have to load the library in the desired version, otherwise Grpc.Net.Client will throw a System.IO.FileNotFoundException!
		// TODO: In case a previously loaded Add-in has already loaded a lower version, do not fail here!
		private static readonly object _unused = NullLogger.Instance;
#endif

		public static GrpcChannelOptions CreateChannelOptions(int maxMessageLength,
		                                                      bool disableProxy = false)
		{
			var result = new GrpcChannelOptions();

			result.MaxReceiveMessageSize = maxMessageLength;
			result.MaxSendMessageSize = maxMessageLength;

			if (disableProxy)
			{
				// Do not change HttpClient.DefaultProxy: it applies to every connection in the
				// process, for example to other add-in's internet connections.
				result.HttpHandler = new SocketsHttpHandler
				                     {
					                     UseProxy = false,
					                     EnableMultipleHttp2Connections = true
				                     };
				result.DisposeHttpClient = true;
			}

			return result;
		}

		public static GrpcChannel CreateChannel(
			[NotNull] string host, int port,
			[CanBeNull] ChannelCredentials credentials,
			int maxMessageLength)
		{
			// Sometimes the local host is not configured as exception in the proxy settings:
			bool disableProxy =
				IsLocalHost(host) ||
				EnvironmentUtils.GetBooleanEnvironmentVariableValue("PROSUITE_GRPC_DISABLE_PROXY");

			_msg.DebugFormat("Creating channel to {0} on port {1}. Disabling proxy: {2}",
			                 host, port, disableProxy);

			string scheme = credentials == ChannelCredentials.Insecure ? "http" : "https";

			string address = $"{scheme}://{host}:{port}";

			GrpcChannelOptions channelOptions =
				CreateChannelOptions(maxMessageLength, disableProxy);

			channelOptions.Credentials = credentials;

			return GrpcChannel.ForAddress(address, channelOptions);
		}

		public static string GetAddress(GrpcChannel channel,
		                                string serviceName)
		{
			string address = "<none>";
			if (channel?.State != ConnectivityState.Shutdown)
			{
				try
				{
					// In shutdown state, the ResolvedTarget property throws for certain:
					address = channel?.Target;
				}
				catch (Exception e)
				{
					_msg.Debug($"Error resolving target address for {serviceName}", e);
				}
			}

			return address;
		}

		private static bool IsLocalHost([NotNull] string host)
		{
			return string.Equals(host, "localhost", StringComparison.InvariantCultureIgnoreCase) ||
			       IPAddress.TryParse(host, out IPAddress address) && IPAddress.IsLoopback(address);
		}
	}
}
