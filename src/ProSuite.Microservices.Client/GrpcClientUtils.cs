using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Health.V1;
using ProSuite.Commons.Cryptography;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using Quaestor.ServiceDiscovery;

namespace ProSuite.Microservices.Client
{
	public static class GrpcClientUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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

			if (string.IsNullOrEmpty(clientCertificate))
			{
				return new SslCredentials(rootCertificatesAsPem);
			}

			KeyCertificatePair sslClientCertificate;

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

			return new SslCredentials(rootCertificatesAsPem, sslClientCertificate);
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
				// TODO: Timeout!
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
				_msg.VerboseDebug(() => $"Starting health check for {serviceName}...");
				HealthCheckResponse healthResponse =
					await healthClient
					      .CheckAsync(new HealthCheckRequest() { Service = serviceName })
					      .ConfigureAwait(false);

				_msg.VerboseDebug(() => $"Health check for {serviceName} completed");

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

		public static IEnumerable<ServiceLocationMsg> GetServiceLocationsFromLoadBalancer(
			[NotNull] ChannelBase lbChannel,
			string serviceName, int maxCount)
		{
			ServiceDiscoveryGrpc.ServiceDiscoveryGrpcClient lbClient =
				new ServiceDiscoveryGrpc.ServiceDiscoveryGrpcClient(lbChannel);

			DiscoverServicesResponse lbResponse = lbClient.DiscoverTopServices(
				new DiscoverServicesRequest
				{
					ServiceName = serviceName,
					MaxCount = maxCount
				});

			foreach (ServiceLocationMsg serviceLocation in lbResponse.ServiceLocations)
			{
				yield return serviceLocation;
			}
		}

		public static async Task<T> TryAsync<T>(Func<CallOptions, Task<T>> func,
		                                        CancellationToken cancellationToken,
		                                        int deadlineMilliseconds = 30000,
		                                        bool noWarn = false)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Operation cancelled");
				return default;
			}

			CallOptions callOptions = GetCallOptions(cancellationToken, deadlineMilliseconds);

			T result;
			try
			{
				result = await func(callOptions);
			}
			catch (RpcException rpcException)
			{
				return HandleRpcException<T>(rpcException, noWarn);
			}

			return result;
		}

		public static T Try<T>(
			[NotNull] Func<CallOptions, T> func,
			CancellationToken cancellationToken,
			int deadlineMilliseconds = 30000,
			bool noWarn = false)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Operation cancelled");
				return default;
			}

			CallOptions callOptions = GetCallOptions(cancellationToken, deadlineMilliseconds);

			T result;
			try
			{
				result = func(callOptions);
			}
			catch (RpcException rpcException)
			{
				return HandleRpcException<T>(rpcException, noWarn);
			}

			return result;
		}

		public static CallOptions GetCallOptions(CancellationToken cancellationToken,
		                                         int deadlineMilliseconds)
		{
			CallOptions callOptions =
				new CallOptions(null, DateTime.UtcNow.AddMilliseconds(deadlineMilliseconds),
				                cancellationToken);
			return callOptions;
		}

		private static T HandleRpcException<T>(RpcException rpcException, bool noWarn)
		{
			_msg.Debug("Exception received from server", rpcException);

			const string exceptionBinKey = "exception-bin";

			if (rpcException.Trailers.Any(t => t.Key.Equals(exceptionBinKey)))
			{
				byte[] bytes = rpcException.Trailers.GetValueBytes(exceptionBinKey);

				if (bytes != null)
				{
					string serverException = Encoding.UTF8.GetString(bytes);
					_msg.DebugFormat("Server call stack: {0}", serverException);
				}
			}

			string message = rpcException.Status.Detail;

			if (rpcException.StatusCode == StatusCode.Cancelled)
			{
				Log("Operation cancelled", noWarn);
				return default;
			}

			if (rpcException.StatusCode == StatusCode.DeadlineExceeded)
			{
				Log("Operation timed out", noWarn);
				return default;
			}

			throw new Exception(message, rpcException);
		}

		private static void Log(string message, bool noWarn)
		{
			if (noWarn)
			{
				_msg.Debug(message);
			}
			else
			{
				_msg.Warn(message);
			}
		}
	}
}
