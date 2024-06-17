using System;
using System.Collections.Generic;
using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Client.GrpcCore
{
	public static class GrpcUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static IList<ChannelOption> CreateChannelOptions(int maxMessageLength,
		                                                        bool disableProxy = false)
		{
			var maxMsgSendLengthOption = new ChannelOption(
				"grpc.max_send_message_length", maxMessageLength);

			var maxMsgReceiveLengthOption = new ChannelOption(
				"grpc.max_receive_message_length", maxMessageLength);

			var channelOptions = new List<ChannelOption>
			                     {
				                     maxMsgSendLengthOption,
				                     maxMsgReceiveLengthOption
			                     };

			if (disableProxy)
			{
				channelOptions.Add(new ChannelOption("grpc.enable_http_proxy", 0));
			}

			return channelOptions;
		}

		public static Channel CreateChannel(
			[NotNull] string host, int port,
			[CanBeNull] ChannelCredentials credentials,
			int maxMessageLength)
		{
			// Sometimes the localhost is not configured as exception in the proxy settings:
			bool disableProxy =
				string.Equals(host, "localhost", StringComparison.InvariantCultureIgnoreCase);

			_msg.DebugFormat("Creating channel to {0} on port {1}. Disabling proxy: {2}",
			                 host, port, disableProxy);

			return new Channel(host, port, credentials,
			                   CreateChannelOptions(maxMessageLength, disableProxy));
		}

		public static string GetAddress([CanBeNull] Channel channel,
		                                string serviceName)
		{
			string address = "<none>";
			if (channel?.State != ChannelState.Shutdown)
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
