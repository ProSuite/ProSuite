using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ProSuite.Commons.DotLiquid;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.DomainServices.AO.QA.DatasetReports.Xml;
using ProSuite.DomainServices.AO.QA.HtmlReports;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;

namespace ProSuite.DomainServices.AO.QA.Standalone.RuleBased
{
	public class HtmlReportWriter
	{
		private readonly string _htmlReportTemplatePath;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public HtmlReportWriter([NotNull] string htmlReportTemplatePath)
		{
			Assert.ArgumentNotNullOrEmpty(htmlReportTemplatePath,
			                              nameof(htmlReportTemplatePath));
			Assert.ArgumentCondition(File.Exists(htmlReportTemplatePath),
			                         "template file does not exist: {0}",
			                         htmlReportTemplatePath);

			_htmlReportTemplatePath = htmlReportTemplatePath;
		}

		public void WriteHtmlReport([NotNull] string htmlReportFilePath,
		                            [NotNull] IssueStatistics issueStatistics,
		                            [NotNull] ObjectClassReport objectClassReport,
		                            [NotNull] XmlVerificationReport verificationReport,
		                            [NotNull] string outputDirectoryPath,
		                            [NotNull] string objectClassReportName,
		                            [NotNull] string verificationReportName,
		                            [CanBeNull] string issueGeodatabasePath,
		                            [CanBeNull] string mapDocumentName)
		{
			Assert.ArgumentNotNullOrEmpty(htmlReportFilePath, nameof(htmlReportFilePath));
			Assert.ArgumentCondition(! File.Exists(htmlReportFilePath),
			                         "output file already exists: {0}", htmlReportFilePath);
			Assert.ArgumentNotNull(issueStatistics, nameof(issueStatistics));
			Assert.ArgumentNotNull(objectClassReport, nameof(objectClassReport));
			Assert.ArgumentNotNull(verificationReport, nameof(verificationReport));

			_msg.DebugFormat("Preparing html report model");
			var reportModel = new HtmlReportModel(issueStatistics,
			                                      objectClassReport,
			                                      verificationReport,
			                                      outputDirectoryPath,
			                                      objectClassReportName,
			                                      verificationReportName,
			                                      issueGeodatabasePath,
			                                      mapDocumentName);

			_msg.DebugFormat("Rendering html report based on template {0}",
			                 _htmlReportTemplatePath);

			LiquidUtils.RegisterSafeType<HtmlReportModel>();
			LiquidUtils.RegisterSafeType<HtmlTexts>();

			string output = LiquidUtils.Render(
				_htmlReportTemplatePath,
				new KeyValuePair<string, object>("report", reportModel),
				new KeyValuePair<string, object>("text", new HtmlTexts()));

			_msg.DebugFormat("Writing html report to {0}", htmlReportFilePath);
			FileSystemUtils.WriteTextFile(output, htmlReportFilePath);

			_msg.InfoFormat("Html report written to {0}", htmlReportFilePath);
		}
	}
}