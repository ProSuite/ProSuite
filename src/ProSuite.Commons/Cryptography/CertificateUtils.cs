using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Cryptography
{
	public static class CertificateUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static IEnumerable<X509Certificate2> FindValidCertificates(
			StoreName storeName,
			StoreLocation storeLocation,
			[NotNull] string searchString,
			X509FindType findType = X509FindType.FindBySubjectDistinguishedName)
		{
			X509Certificate2Collection certificates;

			X509Store store = new X509Store(storeName, storeLocation);
			try
			{
				store.Open(OpenFlags.ReadOnly);

				X509Certificate2Collection certCollection = store.Certificates;

				certificates = certCollection.Find(findType, searchString, true);
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

		public static IEnumerable<X509Certificate2> FindValidCertificatesWithPrivateKey(
			StoreName storeName,
			StoreLocation storeLocation,
			[NotNull] string searchString,
			X509FindType findType = X509FindType.FindBySubjectDistinguishedName)
		{
			var certificates = FindValidCertificates(
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

				var foundCertificates =
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
		/// <returns>A PEM encoded string</returns>
		public static string ExportToPem([NotNull] X509Certificate2 certificate)
		{
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

			// If several were find, use the first that works:
			foreach (X509Certificate2 certificate in foundCertificates)
			{
				if (! certificate.HasPrivateKey)
				{
					_msg.DebugFormat(
						"Certificate {0} has no private key. It cannot be used as server credentials.",
						certificate);

					continue;
				}

				if (! certificate.Verify())
				{
					_msg.DebugFormat(
						"Certificate {0} is not valid. It cannot be used as server credentials.",
						certificate);

					continue;
				}

				_msg.DebugFormat("Trying to extract private key from certificate {0}...",
				                 certificate);

				string publicCertificateChain = ExportToPem(certificate);

				string privateKeyValue;
				string notificationMsg;
				if (! TryExportPrivateKey(certificate, out privateKeyValue,
				                          out notificationMsg))
				{
					_msg.Debug(notificationMsg);

					continue;
				}

				return new KeyPair(privateKeyValue, publicCertificateChain);
			}

			return null;
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
			StringBuilder privateKey = new StringBuilder();
			TextWriter writer = new StringWriter(privateKey);

			ExportPrivateKey(parameters, writer);

			privateKeyValue = privateKey.ToString();

			return true;
		}

		// https://stackoverflow.com/questions/23734792/c-sharp-export-private-public-rsa-key-from-rsacryptoserviceprovider-to-pem-strin

		private static void ExportPrivateKey(RSAParameters parameters, TextWriter outputStream)
		{
			using (var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream);
				writer.Write((byte) 0x30); // SEQUENCE
				using (var innerStream = new MemoryStream())
				{
					var innerWriter = new BinaryWriter(innerStream);
					EncodeIntegerBigEndian(innerWriter, new byte[] {0x00}); // Version
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

				var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int) stream.Length)
				                    .ToCharArray();

				outputStream.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
				// Output as Base64 with lines chopped at 64 characters
				for (var i = 0; i < base64.Length; i += 64)
				{
					outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
				}

				outputStream.WriteLine("-----END RSA PRIVATE KEY-----");
			}
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
