using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Cryptography
{
	public static class CertificateUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static IEnumerable<X509Certificate2> FindCertificates(
			StoreName storeName,
			StoreLocation storeLocation,
			[NotNull] string searchString,
			X509FindType findType = X509FindType.FindBySubjectDistinguishedName,
			bool validOnly = true)
		{
			X509Certificate2Collection certificates;

			X509Store store = new X509Store(storeName, storeLocation);
			try
			{
				store.Open(OpenFlags.ReadOnly);

				X509Certificate2Collection certCollection = store.Certificates;

				certificates = certCollection.Find(findType, searchString, validOnly);
			}
			catch (Exception e)
			{
				_msg.Debug($"Error finding using {findType} ({searchString})", e);
				yield break;
			}
			finally
			{
				store.Close();
			}

			foreach (X509Certificate2 certificate in certificates)
			{
				yield return certificate;
			}
		}

		public static IEnumerable<X509Certificate2> FindValidCertificates(
			StoreName storeName,
			StoreLocation storeLocation,
			[NotNull] string searchString,
			[NotNull] IEnumerable<X509FindType> findTypes)
		{
			foreach (X509FindType findType in findTypes)
			{
				_msg.DebugFormat("Searching certificate store ({0}/{1}) trying {2} ({3})",
				                 storeName, storeLocation, findType, searchString);

				foreach (X509Certificate2 certificate in FindCertificates(
					         storeName, storeLocation, searchString, findType))
				{
					yield return certificate;
				}
			}
		}

		public static IEnumerable<X509Certificate2> FindValidCertificatesWithPrivateKey(
			StoreName storeName,
			StoreLocation storeLocation,
			[NotNull] string searchString,
			X509FindType findType = X509FindType.FindBySubjectDistinguishedName)
		{
			IEnumerable<X509Certificate2> certificates = FindCertificates(
				storeName, storeLocation, searchString, findType);

			foreach (X509Certificate2 certificate in certificates)
			{
				if (certificate.HasPrivateKey)
				{
					yield return certificate;
				}
			}
		}

		public static IEnumerable<X509Certificate2> GetCertificates(
			StoreName storeName,
			StoreLocation storeLocation = StoreLocation.CurrentUser,
			[CanBeNull] Predicate<X509Certificate2> predicate = null)
		{
			X509Store store = new X509Store(storeName, storeLocation);

			try
			{
				store.Open(OpenFlags.ReadOnly);
				foreach (X509Certificate2 certificate in store.Certificates)
				{
					if (predicate != null && ! predicate(certificate))
					{
						continue;
					}

					yield return certificate;
				}
			}
			finally
			{
				store.Close();
			}
		}

		public static IEnumerable<X509Certificate2> GetUserRootCertificates()
		{
			return GetCertificates(StoreName.Root);
		}

		[CanBeNull]
		public static KeyPair FindKeyCertificatePairFromStore(
			[NotNull] string searchString,
			[NotNull] IEnumerable<X509FindType> findTypes,
			StoreName storeName, StoreLocation storeLocation)
		{
			foreach (X509FindType findType in findTypes)
			{
				_msg.DebugFormat("Searching certificate store ({0}/{1}) trying {2} ({3})",
				                 storeName, storeLocation, findType, searchString);

				List<X509Certificate2> foundCertificates =
					FindValidCertificatesWithPrivateKey(
						storeName, storeLocation, searchString, findType).ToList();

				KeyPair keyPair = GetCertificatePair(foundCertificates);

				if (keyPair != null)
				{
					return keyPair;
				}
			}

			return null;
		}

		public static string GetUserRootCertificatesInPemFormat()
		{
			return ExportCertificatesToPem(GetUserRootCertificates());
		}

		/// <summary>
		/// Export a certificate (the public key) to a PEM format string.
		/// </summary>
		/// <param name="certificate">The certificate to export</param>
		/// <param name="fullChain"></param>
		/// <returns>A PEM encoded string</returns>
		public static string ExportToPem([NotNull] X509Certificate2 certificate,
		                                 bool fullChain = false)
		{
			if (fullChain)
			{
				IEnumerable<X509Certificate2> certificatesInChain =
					GetCertificatesInChain(certificate);

				return ExportCertificatesToPem(certificatesInChain);
			}

			StringBuilder stringBuilder = new StringBuilder();

			AddAsPem(certificate, stringBuilder);

			return stringBuilder.ToString();
		}

		public static string ExportCertificatesToPem(
			StoreName storeName,
			StoreLocation storeLocation = StoreLocation.CurrentUser,
			[CanBeNull] Predicate<X509Certificate2> predicate = null)
		{
			return ExportCertificatesToPem(
				GetCertificates(storeName, storeLocation, predicate));
		}

		public static string ExportCertificatesToPem(
			[NotNull] IEnumerable<X509Certificate2> certificates)
		{
			if (certificates == null)
			{
				throw new ArgumentNullException(nameof(certificates));
			}

			StringBuilder stringBuilder = new StringBuilder();

			foreach (X509Certificate2 certificate in certificates)
			{
				AddAsPem(certificate, stringBuilder);

				certificate.Dispose();
			}

			return stringBuilder.ToString();
		}

		public static IEnumerable<X509Certificate2> GetCertificatesInChain(
			[NotNull] X509Certificate2 certificate)
		{
			using (X509Chain chain = new X509Chain())
			{
				chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
				chain.Build(certificate);

				foreach (X509ChainElement chainElement in chain.ChainElements)
				{
					yield return chainElement.Certificate;
				}
			}
		}

		[CanBeNull]
		private static KeyPair GetCertificatePair(
			[NotNull] IReadOnlyList<X509Certificate2> foundCertificates)
		{
			if (foundCertificates == null)
			{
				throw new ArgumentNullException(nameof(foundCertificates));
			}

			if (foundCertificates.Count == 0)
			{
				_msg.Debug("No certificate found.");
				return null;
			}

			// If several were found, use the first that works:
			foreach (X509Certificate2 certificate in foundCertificates)
			{
				KeyPair certificateKeyPair = TryExtractKeyPair(certificate);

				if (certificateKeyPair != null)
				{
					return certificateKeyPair;
				}
			}

			return null;
		}

		[CanBeNull]
		private static KeyPair TryExtractKeyPair([NotNull] X509Certificate2 certificate)
		{
			if (! certificate.HasPrivateKey)
			{
				_msg.DebugFormat(
					"Certificate {0} has no private key. It cannot be used as server credentials.",
					certificate);

				return null;
			}

			if (! certificate.Verify())
			{
				_msg.DebugFormat(
					"Certificate {0} is not valid. It cannot be used as server credentials.",
					certificate);

				return null;
			}

			_msg.DebugFormat("Trying to extract private key from certificate {0}...",
			                 certificate);

			string publicCertificateChain = ExportToPem(certificate, true);

			string privateKeyValue;
			string notificationMsg;
			if (! TryExportPrivateKey(certificate, out privateKeyValue,
			                          out notificationMsg))
			{
				_msg.Debug(notificationMsg);

				return null;
			}

			return new KeyPair(privateKeyValue, publicCertificateChain);
		}

		private static void AddAsPem(X509Certificate2 certificate,
		                             StringBuilder toStringBuilder)
		{
			toStringBuilder.AppendLine(
				"# Issuer: " + certificate.Issuer + "\n" +
				"# Subject: " + certificate.Subject + "\n" +
				"# Label: " + certificate.FriendlyName + "\n" +
				"# Serial: " + certificate.SerialNumber + "\n" +
				"# SHA1 Fingerprint: " + certificate.GetCertHashString() + "\n" +
				AddPublicKeyAsPem(certificate) + "\n");
		}

		private static string AddPublicKeyAsPem([NotNull] X509Certificate cert)
		{
			StringBuilder toStringBuilder = new StringBuilder();

			toStringBuilder.AppendLine("-----BEGIN CERTIFICATE-----");
			toStringBuilder.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert),
			                                                  Base64FormattingOptions
				                                                  .InsertLineBreaks));
			toStringBuilder.AppendLine("-----END CERTIFICATE-----");

			return toStringBuilder.ToString();
		}

		#region Private key export

		/// <summary>
		/// Extracts the private key of a certificate to a string value.
		/// </summary>
		/// <param name="certificate"></param>
		/// <param name="privateKeyValue"></param>
		/// <param name="notifications">The possible failure notifications.</param>
		/// <returns></returns>
		public static bool TryExportPrivateKey([NotNull] X509Certificate2 certificate,
		                                       out string privateKeyValue,
		                                       out string notifications)
		{
			privateKeyValue = null;
			notifications = null;

			if (! certificate.HasPrivateKey)
			{
				notifications = "The provided certificate has no private key.";
				return false;
			}

			RSAParameters parameters;

			try
			{
				// CryptographicException: "Keyset does not exist" -> This works when running as administrator.
				RSA rsaPrivateKey = certificate.GetRSAPrivateKey();

				// CryptographicException: "The requested operation is not supported" (e.g. the key is not exportable)
				parameters = rsaPrivateKey.ExportParameters(true);
			}
			catch (CryptographicException e)
			{
				_msg.Debug("Error getting private key from certificate.", e);

				notifications =
					$"Cannot get private key from certificate {certificate}, possibly due to access restriction ({e.Message}).";

				return false;
			}
			catch (Exception e)
			{
				_msg.Debug("Error getting private key from certificate.", e);

				notifications =
					$"Cannot get private key from certificate {certificate}, possibly due to access restriction ({e.Message}).";

				return false;
			}

			// To PEM format 
			privateKeyValue = ExportParameters(parameters);

			return true;
		}

		/// <summary>
		/// Exports the RSA to the PEM format (PKCS#1)
		/// Adapted version of https://stackoverflow.com/a/23739932/2860309
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		private static string ExportParameters(RSAParameters parameters)
		{
			TextWriter resultStream = new StringWriter();

			using (var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream);

				// Sequence
				writer.Write((byte) 0x30);
				using (var innerStream = new MemoryStream())
				{
					var innerWriter = new BinaryWriter(innerStream);

					// Version
					EncodeIntegerBigEndian(innerWriter, new byte[] {0x00});
					EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
					EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
					EncodeIntegerBigEndian(innerWriter, parameters.D);
					EncodeIntegerBigEndian(innerWriter, parameters.P);
					EncodeIntegerBigEndian(innerWriter, parameters.Q);
					EncodeIntegerBigEndian(innerWriter, parameters.DP);
					EncodeIntegerBigEndian(innerWriter, parameters.DQ);
					EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
					var length = (int) innerStream.Length;
					EncodeLength(writer, length);
					writer.Write(innerStream.GetBuffer(), 0, length);
				}

				char[] base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int) stream.Length)
				                       .ToCharArray();

				// NOTE: In https://gist.github.com/therightstuff/aa65356e95f8d0aae888e9f61aa29414
				// .Write with \n rather than WriteLine (which results in \r\n) is used. But not in
				// https://github.com/Azure/azure-powershell/blob/91ece8f6138350a8fd5a9db93710766aa498a1ac/src/KeyVault/KeyVault/Helpers/JwkHelper.cs#L29
				// However, the new .net 6 method PemEncoding.Write only writes \n
				// -> For consistent behaviour with the future implementation, use \n
				// See unit test in Quaestor.Utilities.Tests.CertificateUtilsTest
				// END NOTE

				resultStream.Write("-----BEGIN RSA PRIVATE KEY-----\n");

				// Base64 which means a new line after 64 characters
				for (var i = 0; i < base64.Length; i += 64)
				{
					resultStream.Write(base64, i, Math.Min(64, base64.Length - i));
					resultStream.Write("\n");
				}

				resultStream.Write("-----END RSA PRIVATE KEY-----");
			}

			return resultStream.ToString();
		}

		private static void EncodeLength(BinaryWriter stream, int length)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length),
				                                      "Length must be non-negative");

			if (length < 0x80)
			{
				// Short form
				stream.Write((byte) length);
			}
			else
			{
				// Long form
				var temp = length;
				var bytesRequired = 0;
				while (temp > 0)
				{
					temp >>= 8;
					bytesRequired++;
				}

				stream.Write((byte) (bytesRequired | 0x80));
				for (var i = bytesRequired - 1; i >= 0; i--)
				{
					stream.Write((byte) (length >> (8 * i) & 0xff));
				}
			}
		}

		private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value,
		                                           bool forceUnsigned = true)
		{
			stream.Write((byte) 0x02); // INTEGER
			var prefixZeros = 0;
			for (var i = 0; i < value.Length; i++)
			{
				if (value[i] != 0) break;
				prefixZeros++;
			}

			if (value.Length - prefixZeros == 0)
			{
				EncodeLength(stream, 1);
				stream.Write((byte) 0);
			}
			else
			{
				if (forceUnsigned && value[prefixZeros] > 0x7f)
				{
					// Add a prefix zero to force unsigned if the MSB is 1
					EncodeLength(stream, value.Length - prefixZeros + 1);
					stream.Write((byte) 0);
				}
				else
				{
					EncodeLength(stream, value.Length - prefixZeros);
				}

				for (var i = prefixZeros; i < value.Length; i++)
				{
					stream.Write(value[i]);
				}
			}
		}

		#endregion
	}
}
