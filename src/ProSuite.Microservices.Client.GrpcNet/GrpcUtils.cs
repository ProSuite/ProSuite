using System;
using System.Net;
using System.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Client.GrpcNet
{
	public static class GrpcUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static GrpcChannelOptions CreateChannelOptions(int maxMessageLength,
		                                                      bool disableProxy = false)
		{
			var result = new GrpcChannelOptions();

			result.MaxReceiveMessageSize = maxMessageLength;
			result.MaxSendMessageSize = maxMessageLength;

			if (disableProxy)
			{
				// TODO: Test this (s. https://stackoverflow.com/questions/66500195/net-5-grpc-client-call-throws-exception-requesting-http-version-2-0-with-versi)
				HttpClient.DefaultProxy = new WebProxy();
			}

			return result;
		}

		public static GrpcChannel CreateChannel(
			[NotNull] string host, int port,
			[CanBeNull] ChannelCredentials credentials,
			int maxMessageLength)
		{
			// Sometimes the localhost is not configured as exception in the proxy settings:
			bool disableProxy =
				string.Equals(host, "localhost", StringComparison.InvariantCultureIgnoreCase);

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
	}
}
