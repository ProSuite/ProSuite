using System.Data;
using System.IO;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using ProSuite.Microservices.Server.AO.QA;

namespace ProSuite.DomainServices.AO.Test.QA
{
	[TestFixture]
	public class VerificationReporterTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanCreateXmlReport()
		{
			string tempDir = TestUtils.GetTempDirPath();
			Directory.CreateDirectory(tempDir);

			IVerificationParameters parameters =
				new VerificationServiceParameters(
					"testContextType", "testContext", 12345)
				{
					HtmlReportPath = $@"{tempDir}\bla.html",
					IssueFgdbPath = $@"{tempDir}\issues.gdb",
					VerificationReportPath = $@"{tempDir}\verification.xml",
					WriteDetailedVerificationReport = true
				};

			var reporter = new VerificationReporter(parameters);

			Assert.IsTrue(reporter.WriteDetailedVerificationReport);

			Assert.IsNotNull(reporter.HtmlReportDir);
			FileAttributes attr = File.GetAttributes(reporter.HtmlReportDir);
			Assert.IsTrue(FileAttributes.Directory == (attr & FileAttributes.Directory));

			const string specificationName = "TestSpec";
			string gdbPath = TestData.GetGdb1Path();
			const string featureClassName = "lines";

			QualitySpecification qualitySpecification =
				VerificationTestUtils.CreateQualitySpecification(
					featureClassName, specificationName, gdbPath);

			IVerificationReportBuilder reportBuilder = reporter.CreateReportBuilders();
			Assert.NotNull(reportBuilder);

			reportBuilder.BeginVerification(null);

			reportBuilder.EndVerification(false);

			XmlVerificationReport xmlVerificationReport =
				reporter.WriteReports(qualitySpecification);

			Assert.IsNotNull(xmlVerificationReport);
		}

		[Test]
		public void CanCreateGeodatabases()
		{
			string tempDir = TestUtils.GetTempDirPath();
			Directory.CreateDirectory(tempDir);

			IVerificationParameters parameters =
				new VerificationServiceParameters(
					"testContextType", "testContext", 12345)
				{
					HtmlReportPath = $@"{tempDir}\bla.html",
					IssueFgdbPath = $@"{tempDir}\issues.gdb",
					VerificationReportPath = $@"{tempDir}\verification.xml",
					WriteDetailedVerificationReport = true
				};

			var reporter = new VerificationReporter(parameters);

			Assert.IsTrue(reporter.CanCreateIssueRepository);

			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IIssueRepository issueRepository =
				reporter.CreateIssueRepository(IssueRepositoryType.FileGdb, sr);

			Assert.IsNotNull(issueRepository);
			Assert.IsTrue(Directory.Exists($@"{tempDir}\issues.gdb"));

			ISubverificationObserver subVerificationObserver =
				reporter.CreateSubVerificationObserver(IssueRepositoryType.FileGdb, sr);

			Assert.IsNotNull(subVerificationObserver);

			Assert.IsTrue(Directory.Exists($@"{tempDir}\progress.gdb"));

			// The second time the FC creation fails because it already exists
			Assert.Throws<DataException>(
				() => subVerificationObserver =
					      reporter.CreateSubVerificationObserver(IssueRepositoryType.FileGdb, sr));
		}
	}
}
