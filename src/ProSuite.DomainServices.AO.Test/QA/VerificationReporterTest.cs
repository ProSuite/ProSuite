using System.IO;
using NUnit.Framework;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using ProSuite.Microservices.Server.AO.QA;

namespace ProSuite.DomainServices.AO.Test.QA
{
	[TestFixture]
	public class VerificationReporterTest
	{
		[Test]
		public void CanCreateXmlReport()
		{
			IVerificationParameters parameters =
				new VerificationServiceParameters(
					"testContextType", "testContext", 12345)
				{
					HtmlReportPath = @"C:\temp\bla.html",
					IssueFgdbPath = @"C:\temp\issues.gdb",
					VerificationReportPath = @"C:\Temp\verification.xml",
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
	}
}
