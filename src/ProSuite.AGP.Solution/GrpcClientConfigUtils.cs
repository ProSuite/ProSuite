using System;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Xml;
using ProSuite.Microservices.Client;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.AGP.Solution
{
	public static class GrpcClientConfigUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static async Task<GeometryProcessingClient> StartGeometryProcessingClient(
			[CanBeNull] string executablePath,
			[CanBeNull] string configFilePath,
			int fallbackPort = 5153)
		{
			ClientChannelConfig clientChannelConfig;

			if (string.IsNullOrEmpty(configFilePath))
			{
				clientChannelConfig = FallbackToDefaultChannel("Geometry Service", fallbackPort);
			}
			else
			{
				clientChannelConfig = GetClientChannelConfig(configFilePath);
			}

			var result = new GeometryProcessingClient(clientChannelConfig);

			if (! string.IsNullOrEmpty(executablePath))
			{
				await result.AllowStartingLocalServerAsync(executablePath).ConfigureAwait(false);
			}

			return result;
		}

		[NotNull]
		public static async Task<QualityVerificationServiceClient> StartQaServiceClient(
			[CanBeNull] string executablePath,
			[CanBeNull] string configFilePath,
			int fallbackPort = 5151)
		{
			ClientChannelConfig clientChannelConfig;

			if (string.IsNullOrEmpty(configFilePath))
			{
				clientChannelConfig = FallbackToDefaultChannel("QA", fallbackPort);
			}
			else
			{
				clientChannelConfig = GetClientChannelConfig(configFilePath);
			}

			var result = new QualityVerificationServiceClient(clientChannelConfig);

			if (! string.IsNullOrEmpty(executablePath))
			{
				await result.AllowStartingLocalServerAsync(executablePath).ConfigureAwait(false);
			}

			return result;
		}

		private static ClientChannelConfig FallbackToDefaultChannel(
			[NotNull] string serviceName, int fallbackPort)
		{
			const string fallbackHost = "localhost";

			_msg.DebugFormat(
				"{0} microservice client configuration not found, using default settings {1}:{2}",
				serviceName, fallbackHost, fallbackPort);

			var clientChannelConfig = new ClientChannelConfig
			                          {
				                          HostName = fallbackHost,
				                          Port = fallbackPort
			                          };

			return clientChannelConfig;
		}

		private static ClientChannelConfig GetClientChannelConfig([NotNull] string configFilePath)
		{
			ClientChannelConfig clientChannelConfig;
			_msg.DebugFormat("Geometry microservice configuration from {0}",
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
					$"Error reading geometry microservice configuration from {configFilePath}",
					e);
			}

			return clientChannelConfig;
		}
	}
}
