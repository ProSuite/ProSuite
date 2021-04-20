using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Grpc.Health.V1;
using ProSuite.Commons.Cryptography;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Client
{
	public static class GrpcUtils
	{
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
			_msg.DebugFormat("Creating channel to {0} on port {1}", host, port);

			return new Channel(host, port, credentials,
			                   CreateChannelOptions(maxMessageLength));
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
					                   {Service = serviceName});

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
	}
}
