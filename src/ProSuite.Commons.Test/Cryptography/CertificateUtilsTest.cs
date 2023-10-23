using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using ProSuite.Commons.Cryptography;

namespace ProSuite.Commons.Test.Cryptography
{
	[TestFixture]
	public class CertificateUtilsTest
	{
		[Test]
		public void CanGetRootCertificates()
		{
			var rootCertificates = CertificateUtils.GetUserRootCertificates().ToList();

			Assert.IsTrue(rootCertificates.Count > 0);

			foreach (X509Certificate2 certificate in rootCertificates)
			{
				Console.WriteLine(certificate);
			}
		}

		[Test]
		public void CanFindCertificateByCommonName()
		{
			var found = CertificateUtils.FindCertificates(
				                            StoreName.Root, StoreLocation.CurrentUser,
				                            "CN=Microsoft Root Certificate Authority, DC=microsoft, DC=com",
				                            X509FindType.FindBySubjectDistinguishedName, false)
			                            .ToList();

			Assert.IsTrue(found.Count > 0);
		}

		[Test]
		public void CanFindCertificateByThumbprint()
		{
			var certificate = CertificateUtils.GetCertificates(StoreName.My).First();

			Assert.NotNull(certificate.Thumbprint);

			string certificateThumbprint = certificate.Thumbprint;

			var found =
				CertificateUtils.FindCertificates(
					StoreName.My, StoreLocation.CurrentUser, certificateThumbprint,
					X509FindType.FindByThumbprint, false).ToList();

			Assert.AreEqual(1, found.Count);

			Assert.AreEqual(certificate, found.First());
		}

		[Test]
		public void CanGetMyCertificates()
		{
			var found = CertificateUtils.GetCertificates(StoreName.My).ToList();

			Assert.IsTrue(found.Count > 0);

			foreach (X509Certificate2 certificate in found)
			{
				Console.WriteLine(certificate);
			}
		}

		[Test]
		public void CanGetPrivateKeyFromCertificate()
		{
			// Finds the localhost developer certificate that is used for ASP.NET:
			var found = CertificateUtils.FindCertificates(
				                            StoreName.Root, StoreLocation.CurrentUser,
				                            "CN=Microsoft Root Certificate Authority, DC=microsoft, DC=com",
				                            X509FindType.FindBySubjectDistinguishedName, false)
			                            .ToList();

			Assert.IsTrue(found.Count > 0);

			var certificate = found[0];

			// No private key:
			Assert.IsFalse(
				CertificateUtils.TryExportPrivateKey(certificate, out string _, out string _));

			foreach (X509Certificate2 certWithPrivateKey in CertificateUtils.GetCertificates(
				         StoreName.My, StoreLocation.LocalMachine, c => c.HasPrivateKey))
			{
				// No access (must run as admin):
				bool canExport =
					CertificateUtils.TryExportPrivateKey(certWithPrivateKey, out string _,
					                                     out string _);

				string msg = canExport ? "Successfully exported " : "Export failed for ";
				Console.WriteLine("{0} {1}", msg, certWithPrivateKey);
			}
		}

		[Test]
		[Ignore("In some cases this opens a UI requesting a smart card.")]
		public void CanGetPrivateKeyFromPersonalCertificate()
		{
			foreach (X509Certificate2 certWithPrivateKey in CertificateUtils.GetCertificates(
				         StoreName.My, StoreLocation.CurrentUser, c => c.HasPrivateKey))
			{
				// The key might not be exportable...

				bool canExport =
					CertificateUtils.TryExportPrivateKey(certWithPrivateKey, out string _,
					                                     out string _);

				string msg = canExport ? "Successfully exported " : "Export failed for ";
				Console.WriteLine("{0} {1}", msg, certWithPrivateKey);
			}
		}

		[Test]
		public void CanGetCertificatesInChain()
		{
			var found = CertificateUtils.GetCertificates(StoreName.My).ToList();

			Assert.IsTrue(found.Count > 0);

			foreach (var cert in CertificateUtils.GetCertificatesInChain(found[0]))
			{
				Console.WriteLine(cert);
			}
		}

		[Test]
		public void CanGetCertificatesInChainDiraCodeSigning()
		{
			var found = CertificateUtils.FindCertificates(
				                            StoreName.My, StoreLocation.CurrentUser,
				                            @"CN=Dira GeoSystems AG, O=Dira GeoSystems AG, S=Zürich, C=CH",
				                            X509FindType.FindBySubjectDistinguishedName, false)
			                            .ToList();

			if (found.Count == 0)
			{
				// The certificate is not installed.
				return;
			}

			X509Certificate2 diraCodeSigningCert = found[0];

			int count = 0;
			foreach (var cert in CertificateUtils.GetCertificatesInChain(diraCodeSigningCert))
			{
				Console.WriteLine(cert);
				count++;
			}

			Assert.IsTrue(count > 2);
		}

		[Test]
		[Ignore(
			"Requires the code signing file on the local machine and in case of the pfx the password")]
		public void CanGetCertificatesInChainFromFile()
		{
			// Replace with the path to your certificate file (e.g., .cer or .p7b).
			string certificateFilePath =
				@"C:\Users\ema\Documents\Administrative\Certificates\DiraCodeSigning.pfx";

			try
			{
				// Load the certificate file.
				X509Certificate2 certificate =
					new X509Certificate2(certificateFilePath, "ThisIsNotThePassword");

				// Create an X509Chain object.
				X509Chain chain = new X509Chain();

				// Set X509ChainPolicy options (optional).
				chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
				chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid;

				// Build the chain.
				bool chainBuilt = chain.Build(certificate);

				if (chainBuilt)
				{
					// Get the full certificate chain.
					X509Certificate2Collection fullChain =
						new X509Certificate2Collection(
							chain.ChainElements.Cast<X509ChainElement>().Select(x => x.Certificate)
							     .ToArray());

					Assert.IsTrue(fullChain.Count > 2);
					// Display the full certificate chain.
					Console.WriteLine("Full Certificate Chain:");
					foreach (X509Certificate2 cert in fullChain)
					{
						Console.WriteLine("Subject: " + cert.Subject);
					}
				}
				else
				{
					Console.WriteLine("Chain building failed.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
			}
		}
	}
}
