using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using Grpc.Core;
using ProSuite.Commons.Cryptography;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;

namespace ProSuite.Microservices.Server.AO
{
	public static class GrpcServerUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static async Task<ServiceCallStatus> ExecuteServiceCall(
			Func<ITrackCancel, ServiceCallStatus> func,
			ServerCallContext serverCallContext,
			TaskScheduler taskScheduler)
		{
			CancellationToken cancellationToken = serverCallContext.CancellationToken;
			string methodName = serverCallContext.Method;

			var trackCancellationToken = new TrackCancellationToken(cancellationToken);

			return await Task.Factory.StartNew(
				       () => TryExecute(
					       func, trackCancellationToken, methodName), cancellationToken,
				       TaskCreationOptions.LongRunning, taskScheduler);
		}

		public static void GracefullyStop(Grpc.Core.Server server)
		{
			Assert.ArgumentNotNull(server, nameof(server));

			_msg.Info("Starting shut down...");

			server.ShutdownAsync().Wait();

			_msg.Info("Server shut down.");
		}

		/// <summary>
		/// Creates the server credentials using either two PEM files or a certificate from the
		/// Certificate Store.
		/// </summary>
		/// <param name="certificate">The certificate store's certificate (subject or thumbprint)
		/// or the PEM file containing the certificate chain.</param>
		/// <param name="privateKeyFilePath">The PEM file containing the private key (only if the
		/// certificate was provided by a PEM file</param>
		/// <param name="enforceMutualTls">Enforce client authentication.</param>
		/// <returns></returns>
		public static ServerCredentials GetServerCredentials(
			[CanBeNull] string certificate,
			[CanBeNull] string privateKeyFilePath,
			bool enforceMutualTls = false)
		{
			if (string.IsNullOrEmpty(certificate))
			{
				_msg.InfoFormat("Certificate was not provided. Using insecure credentials.");

				return ServerCredentials.Insecure;
			}

			KeyPair certificateKeyPair =
				TryGetServerCertificateKeyPair(certificate, privateKeyFilePath);

			if (certificateKeyPair == null)
			{
				return ServerCredentials.Insecure;
			}

			List<KeyCertificatePair> keyCertificatePairs =
				new List<KeyCertificatePair>
				{
					new KeyCertificatePair(
						certificateKeyPair.PublicKey, certificateKeyPair.PrivateKey)
				};

			string rootCertificatesAsPem =
				CertificateUtils.GetUserRootCertificatesInPemFormat();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Trusted root credentials provided: {0}",
				                 rootCertificatesAsPem);
			}

			// If not required, still verify the client certificate, if presented
			var clientCertificates =
				enforceMutualTls
					? SslClientCertificateRequestType.RequestAndRequireAndVerify
					: SslClientCertificateRequestType.RequestAndVerify;

			ServerCredentials result = new SslServerCredentials(
				keyCertificatePairs, rootCertificatesAsPem,
				clientCertificates);

			return result;
		}

		private static KeyPair TryGetServerCertificateKeyPair(
			string certificate,
			[CanBeNull] string privateKeyFilePath)
		{
			KeyPair result;
			if (File.Exists(certificate))
			{
				Assert.NotNullOrEmpty(privateKeyFilePath, "Private key PEM file was not provided.");

				Assert.True(File.Exists(privateKeyFilePath),
				            $"Private key PEM file {privateKeyFilePath} was not found.");

				result = new KeyPair(File.ReadAllText(certificate),
				                     File.ReadAllText(privateKeyFilePath));

				_msg.InfoFormat("Using certificate from file {0}", certificate);
			}
			else
			{
				_msg.DebugFormat(
					"No certificate PEM file found using {0}. Getting certificate from store.",
					certificate);

				// Find server certificate from Store (Local Computer, Personal folder)
				result =
					CertificateUtils.FindKeyCertificatePairFromStore(
						certificate,
						new[]
						{
							X509FindType.FindBySubjectDistinguishedName,
							X509FindType.FindByThumbprint
						}, StoreName.My, StoreLocation.LocalMachine);

				if (result == null)
				{
					_msg.InfoFormat(
						"No certificate could be found by '{0}'. Using insecure credentials (no TLS).",
						certificate);
				}
				else
				{
					_msg.InfoFormat("Using certificate from certificate store for TLS.");
				}
			}

			if (result != null && _msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Certificate chain: {0}", result.PublicKey);
			}

			return result;
		}

		private static T TryExecute<T>(Func<ITrackCancel, T> func,
		                               TrackCancellationToken trackCancellationToken,
		                               string methodName)
		{
			T response = default;

			try
			{
				response = func(trackCancellationToken);
			}
			catch (Exception e)
			{
				_msg.Error($"Error in {methodName}", e);
				trackCancellationToken.Exception = e;
			}

			return response;
		}
	}
}
