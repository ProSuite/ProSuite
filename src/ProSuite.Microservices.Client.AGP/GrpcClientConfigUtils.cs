using System;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Xml;

namespace ProSuite.Microservices.Client.AGP
{
	public static class GrpcClientConfigUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static async Task<GeometryProcessingClient> StartGeometryProcessingClient(
			[NotNull] string executablePath, string configFilePath)
		{
			ClientChannelConfig clientChannelConfig;

			if (string.IsNullOrEmpty(configFilePath))
			{
				_msg.Debug(
					"Geometry processing microservice client configuration not found, using default settings...");

				clientChannelConfig = new ClientChannelConfig
				                      {
					                      HostName = "localhost",
					                      Port = 5153
				                      };
			}
			else
			{
				clientChannelConfig = GetClientChannelConfig(configFilePath);
			}

			var result = new GeometryProcessingClient(clientChannelConfig);

			await result.AllowStartingLocalServerAsync(executablePath).ConfigureAwait(false);

			return result;
		}

		private static ClientChannelConfig GetClientChannelConfig([NotNull] string configFilePath)
		{
			ClientChannelConfig clientChannelConfig;
			_msg.DebugFormat("Geometry processing microservice configuration from {0}",
			                 configFilePath);

			try
			{
				XmlSerializationHelper<ClientChannelConfigs> configHelper =
					new XmlSerializationHelper<ClientChannelConfigs>();

				clientChannelConfig =
					configHelper.ReadFromFile(configFilePath).Channels.First();
			}
			catch (Exception e)
			{
				_msg.Debug($"Error reading configuration from {configFilePath}.", e);
				throw new InvalidConfigurationException(
					$"Error reading geometry processing microservice configuration from {configFilePath}",
					e);
			}

			return clientChannelConfig;
		}
	}
}
