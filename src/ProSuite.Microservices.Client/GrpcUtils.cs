using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Health.V1;
using ProSuite.Commons.Cryptography;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Client
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

		[NotNull]
		public static ChannelCredentials CreateChannelCredentials(
			bool useTls,
			[CanBeNull] string clientCertificate = null)
		{
			if (! useTls)
			{
				_msg.DebugFormat("Using insecure channel credentials");

				return ChannelCredentials.Insecure;
			}

			string rootCertificatesAsPem =
				CertificateUtils.GetUserRootCertificatesInPemFormat();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Trusted root credentials provided: {0}",
				                 rootCertificatesAsPem);
			}

			KeyCertificatePair sslClientCertificate = null;
			if (! string.IsNullOrEmpty(clientCertificate))
			{
				KeyPair keyPair = CertificateUtils.FindKeyCertificatePairFromStore(
					clientCertificate, new[]
					                   {
						                   X509FindType.FindBySubjectDistinguishedName,
						                   X509FindType.FindByThumbprint,
						                   X509FindType.FindBySubjectName
					                   }, StoreName.My, StoreLocation.CurrentUser);

				if (keyPair != null)
				{
					_msg.Debug("Using client-side certificate");

					sslClientCertificate =
						new KeyCertificatePair(keyPair.PublicKey, keyPair.PrivateKey);
				}
				else
				{
					throw new ArgumentException(
						$"Could not usable find client certificate {clientCertificate} in certificate store.");
				}
			}

			var result = new SslCredentials(rootCertificatesAsPem, sslClientCertificate);

			return result;
		}

		public static Channel CreateChannel(
			[NotNull] string host, int port,
			ChannelCredentials credentials,
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

		/// <summary>
		/// Determines whether the specified endpoint is connected to the specified service
		/// that responds with health status 'Serving' .
		/// </summary>
		/// <param name="healthClient"></param>
		/// <param name="serviceName"></param>
		/// <param name="statusCode">Status code from the RPC call</param>
		/// <returns></returns>
		public static bool IsServing([NotNull] Health.HealthClient healthClient,
		                             [NotNull] string serviceName,
		                             out StatusCode statusCode)
		{
			statusCode = StatusCode.Unknown;

			try
			{
				HealthCheckResponse healthResponse =
					healthClient.Check(new HealthCheckRequest()
					                   { Service = serviceName });

				statusCode = StatusCode.OK;

				return healthResponse.Status == HealthCheckResponse.Types.ServingStatus.Serving;
			}
			catch (RpcException rpcException)
			{
				_msg.Debug($"Error checking health of service {serviceName}", rpcException);
				statusCode = rpcException.StatusCode;
			}
			catch (Exception e)
			{
				_msg.Debug($"Error checking health of service {serviceName}", e);
				return false;
			}

			return false;
		}

		/// <summary>
		/// Determines whether the specified endpoint is connected to the specified service
		/// that responds with health status 'Serving' .
		/// </summary>
		/// <param name="healthClient"></param>
		/// <param name="serviceName"></param>
		/// <returns>StatusCode.OK if the service is healthy.</returns>
		public static async Task<StatusCode> IsServingAsync(
			[NotNull] Health.HealthClient healthClient,
			[NotNull] string serviceName)
		{
			StatusCode statusCode = StatusCode.Unknown;

			try
			{
				HealthCheckResponse healthResponse =
					await healthClient.CheckAsync(new HealthCheckRequest()
					                              { Service = serviceName });

				statusCode =
					healthResponse.Status == HealthCheckResponse.Types.ServingStatus.Serving
						? StatusCode.OK
						: StatusCode.ResourceExhausted;
			}
			catch (RpcException rpcException)
			{
				_msg.Debug($"Error checking health of service {serviceName}", rpcException);
				statusCode = rpcException.StatusCode;
			}
			catch (Exception e)
			{
				_msg.Debug($"Error checking health of service {serviceName}", e);
				return statusCode;
			}

			return statusCode;
		}
	}
}
