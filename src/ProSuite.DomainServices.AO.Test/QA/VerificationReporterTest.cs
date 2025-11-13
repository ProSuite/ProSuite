using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using ProSuite.Microservices.Server.AO.QA;
using ProSuite.QA.Tests;

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

			ISubVerificationObserver subVerificationObserver =
				reporter.CreateSubVerificationObserver(IssueRepositoryType.FileGdb, sr);

			Assert.IsNotNull(subVerificationObserver);

			Assert.IsTrue(Directory.Exists($@"{tempDir}\progress.gdb"));

			// The second time the FeatureClass creation fails because it already exists
			Assert.Throws<DataException>(
				() => subVerificationObserver =
					      reporter.CreateSubVerificationObserver(IssueRepositoryType.FileGdb, sr));
		}

		[Test]
		public void CanDeleteGeodatabasesAfterUse()
		{
			string tempDir = TestUtils.GetTempDirPath();
			Directory.CreateDirectory(tempDir);

			var issueFgdbPath = $@"{tempDir}\issues.gdb";

			IVerificationParameters parameters =
				new VerificationServiceParameters(
					"testContextType", "testContext", 12345)
				{
					HtmlReportPath = $@"{tempDir}\bla.html",
					IssueFgdbPath = issueFgdbPath,
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
			Assert.IsTrue(Directory.Exists(issueFgdbPath));

			var element = new QualitySpecificationElement(
				new QualityCondition(
					"test",
					new TestDescriptor("testDescriptor", new ClassDescriptor(typeof(Qa3dConstantZ)),
					                   0)));
			IEnumerable<InvolvedTable> involvedRows = new List<InvolvedTable>();
			issueRepository.AddIssue(new Issue(element, "testError", involvedRows), null);

			ISubVerificationObserver subVerificationObserver =
				reporter.CreateSubVerificationObserver(IssueRepositoryType.FileGdb, sr);

			Assert.IsNotNull(subVerificationObserver);

			var progressGdbPath = $@"{tempDir}\progress.gdb";

			Assert.IsTrue(Directory.Exists(progressGdbPath));

			subVerificationObserver.CreatedSubverification(
				42,
				new EnvelopeXY(2600000, 1200000, 2601000, 1201000));
			subVerificationObserver.Finished(42, ServiceCallStatus.Finished);

			subVerificationObserver.Dispose();
			issueRepository.Dispose();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			// No lock, can delete!
			Directory.Delete(issueFgdbPath, true);
			Directory.Delete(progressGdbPath, true);
		}
	}
}
