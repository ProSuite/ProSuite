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
	}
}
