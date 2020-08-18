using System.Collections.Generic;
using System.Reflection;
using Grpc.Core;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Client
{
	public static class GrpcUtils
	{
		// TODO: Consider moving to new project ProSuite.Microservices
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static IList<ChannelOption> CreateChannelOptions(int maxMessageLength)
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

			return channelOptions;
		}

		public static Channel CreateChannel(
			string host, int port,
			ChannelCredentials credentials,
			int maxMessageLength)
		{
			_msg.DebugFormat("Creating channel");

			return new Channel(host, port, credentials,
			                   CreateChannelOptions(maxMessageLength));
		}
	}
}
