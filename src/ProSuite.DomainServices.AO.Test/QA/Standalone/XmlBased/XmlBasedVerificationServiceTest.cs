using System.IO;
using NUnit.Framework;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;

namespace ProSuite.DomainServices.AO.Test.QA.Standalone.XmlBased
{
	public class XmlBasedVerificationServiceTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanExecuteConditionBasedSpecification()
		{
			const string specificationName = "TestSpec";
			const string condition1Name = "Str_Simple";
			string gdbPath = TestData.GetGdb1Path();
			const string featureClassName = "lines";

			QualitySpecification qualitySpecification =
				XmlBasedQualitySpecificationFactoryTest.CreateConditionBasedQualitySpecification(
					condition1Name, featureClassName, specificationName, gdbPath);

			XmlBasedVerificationService service = new XmlBasedVerificationService();

			string tempDirPath = TestUtils.GetTempDirPath(null);

			service.ExecuteVerification(qualitySpecification, null, 1000,
			                            tempDirPath);

			Assert.IsTrue(Directory.Exists(Path.Combine(tempDirPath, "issues.gdb")));
			Assert.IsTrue(File.Exists(Path.Combine(tempDirPath, "verification.xml")));
		}
	}
}
