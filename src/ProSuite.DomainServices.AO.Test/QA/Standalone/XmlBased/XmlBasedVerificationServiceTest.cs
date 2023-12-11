using System.IO;
using NUnit.Framework;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using TestUtils = ProSuite.Commons.Test.Testing.TestUtils;

namespace ProSuite.DomainServices.AO.Test.QA.Standalone.XmlBased
{
	public class XmlBasedVerificationServiceTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnitTestLogging();
			Commons.AO.Test.TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			Commons.AO.Test.TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanExecuteConditionBasedSpecification()
		{
			const string specificationName = "TestSpec";
			string gdbPath = TestData.GetGdb1Path();
			const string featureClassName = "lines";

			QualitySpecification qualitySpecification =
				VerificationTestUtils.CreateQualitySpecification(
					featureClassName, specificationName, gdbPath);

			XmlBasedVerificationService service = new XmlBasedVerificationService();

			string tempDirPath = Commons.AO.Test.TestUtils.GetTempDirPath(null);

			service.ExecuteVerification(qualitySpecification, null, 1000,
			                            tempDirPath);

			Assert.IsTrue(Directory.Exists(Path.Combine(tempDirPath, "issues.gdb")));
			Assert.IsTrue(File.Exists(Path.Combine(tempDirPath, "verification.xml")));
		}
	}
}
