using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using Path = System.IO.Path;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased
{
	/// <summary>
	/// Standalone (i.e. non-DDX dependent) verification service based on a xml specification or
	/// based on a list of conditions (defined via xml). This service supports the exception-Gdb
	/// mechanism. This class is currently named after the (legacy) XML-based GP Tool because it
	/// provides the its functionality.
	/// TODO: Rename to Standalone/UnRegisteredModelVerification or something like this
	/// TODO: Report Errors, probably progress messages back via events to allow for error-streaming
	/// TODO: Report final (probably simplified statistics) to be delivered to client.
	/// </summary>
	public class XmlBasedVerificationService
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private readonly string _htmlReportTemplatePath;
		[CanBeNull] private readonly string _qualitySpecificationTemplatePath;

		private string _issueRepositoryDir;
		private string _issueRepositoryName;
		private string _xmlVerificationReportPath;
		private string _htmlReportDir;

		public XmlBasedVerificationService(
			[CanBeNull] string htmlReportTemplatePath = null,
			[CanBeNull] string qualitySpecificationTemplatePath = null)
		{
			_htmlReportTemplatePath = htmlReportTemplatePath;
			_qualitySpecificationTemplatePath = qualitySpecificationTemplatePath;
		}

		/// <summary>
		/// The interface that allows streaming progress information during a lengthy operation.
		/// </summary>
		public IVerificationProgressStreamer ProgressStreamer { get; set; }

		public ITestRunner DistributedTestRunner { get; set; }

		public IssueRepositoryType IssueRepositoryType { get; set; } = IssueRepositoryType.FileGdb;

		public QualityVerification Verification { get; set; }

		public event EventHandler<IssueFoundEventArgs> IssueFound;

		public void SetupOutputPaths(string outputDirectory)
		{
			Directory.CreateDirectory(outputDirectory);

			_issueRepositoryDir = outputDirectory;

			// These options might return somehow in the future.
			XmlVerificationOptions verificationOptions = null;
			_issueRepositoryName =
				VerificationOptionUtils.GetIssueWorkspaceName(verificationOptions);

			string verificationReportFileName =
				VerificationOptionUtils.GetXmlReportFileName(verificationOptions);
			_xmlVerificationReportPath = Path.Combine(outputDirectory, verificationReportFileName);

			_htmlReportDir = outputDirectory;
		}

		public void SetupOutputPaths(string issueRepositoryPath,
		                             string xmlVerificationReportPath,
		                             string htmlReportPath)
		{
			if (! string.IsNullOrEmpty(issueRepositoryPath))
			{
				try
				{
					_issueRepositoryDir = Path.GetDirectoryName(issueRepositoryPath);
					_issueRepositoryName = Path.GetFileNameWithoutExtension(issueRepositoryPath);
				}
				catch (ArgumentException argumentException)
				{
					// Include more details in the error message to be more useful to the client
					// Typically: ArgumentException: The path is not of a legal form.
					throw new ArgumentException(
						$"Issue Repository path {issueRepositoryPath}: {argumentException.Message}",
						argumentException);
				}
			}
			else
			{
				_msg.Info(
					"No issue repository path was provided and no issue workspace will be written.");
			}

			_xmlVerificationReportPath = xmlVerificationReportPath;

			if (string.IsNullOrEmpty(_xmlVerificationReportPath))
			{
				_msg.Info(
					"No XML verification report path was provided and no xml report will be written.");
			}

			// NOTE: Currently the file names are hard-coded
			if (! string.IsNullOrEmpty(htmlReportPath))
			{
				try
				{
					_htmlReportDir = Path.GetDirectoryName(htmlReportPath);
				}
				catch (ArgumentException argumentException)
				{
					// Include more details in the error message to be more useful to the client
					// Typically: ArgumentException: The path is not of a legal form.
					throw new ArgumentException(
						$"HTML Report path {htmlReportPath}: {argumentException.Message}",
						argumentException);
				}
			}
			else
			{
				_msg.Info("No HTML report path was provided and no HTML will be written.");
			}
		}

		private string XmlVerificationReportFileName =>
			Path.GetFileName(_xmlVerificationReportPath);

		public void ExecuteVerification(
			[NotNull] QualitySpecification specification,
			[CanBeNull] AreaOfInterest areaOfInterest,
			double tileSize,
			[NotNull] string outputDirectoryPath,
			IssueRepositoryType issueRepositoryType = IssueRepositoryType.FileGdb,
			ITrackCancel cancelTracker = null)
		{
			Assert.NotNullOrEmpty(outputDirectoryPath, "Output directory path is null or empty.");

			string issueWorkspaceName = "issues";

			if (ExternalIssueRepositoryUtils.IssueRepositoryExists(
				    outputDirectoryPath, issueWorkspaceName, issueRepositoryType))
			{
				_msg.WarnFormat("The {0} workspace '{1}' in {2} already exists",
				                issueRepositoryType, issueWorkspaceName, outputDirectoryPath);
				return;
			}

			IssueRepositoryType = issueRepositoryType;
			SetupOutputPaths(outputDirectoryPath);

			try
			{
				bool fulfilled = ExecuteVerification(specification,
				                                     areaOfInterest,
				                                     tileSize, cancelTracker);
			}
			catch (Exception)
			{
				StandaloneVerificationUtils.TryDeleteOutputDirectory(outputDirectoryPath);
				throw;
			}
		}

		public bool ExecuteVerification(
			[NotNull] QualitySpecification qualitySpecification,
			[CanBeNull] AreaOfInterest areaOfInterest,
			double tileSize,
			[CanBeNull] ITrackCancel trackCancel)
		{
			Model primaryModel = StandaloneVerificationUtils.GetPrimaryModel(qualitySpecification);
			Assert.NotNull(primaryModel, "no primary model found for quality specification");

			// TODO disable quality conditions based on primaryModel and DatasetTestParameterValue.UsedAsReferenceData?
			// TODO this would probably require an explicit identification of the primary data source
			XmlVerificationReportBuilder xmlReportBuilder = GetReportBuilder();
			var statisticsBuilder = new IssueStatisticsBuilder();

			var datasetsCollector = new InvolvedDatasetsCollector();

			var service = new StandaloneQualityVerificationService(
				new MultiReportBuilder(xmlReportBuilder,
				                       statisticsBuilder,
				                       datasetsCollector),
				(context) => new SimpleDatasetOpener(context));

			service.IssueFound += (sender, args) => IssueFound?.Invoke(this, args);

			// This context excludes geometric networks, terrains, topology, etc.:
			var datasetContext = new MasterDatabaseDatasetContext();
			var datasetResolver =
				new QualityConditionObjectDatasetResolver(
					new MasterDatabaseWorkspaceContextLookup());

			ISpatialReference spatialReference =
				primaryModel.SpatialReferenceDescriptor?.SpatialReference;

			var issueGdbWritten = false;
			bool fulfilled;

			List<string> htmlReportFilePaths = null;
			List<string> specificationReportFilePaths = null;
			string gdbPath = null;

			service.DistributedTestRunner = DistributedTestRunner;
			service.ProgressStreamer = ProgressStreamer;

			StringBuilder sb = new StringBuilder();

			using (IIssueRepository issueRepository =
			       ExternalIssueRepositoryUtils.GetIssueRepository(
				       _issueRepositoryDir, _issueRepositoryName, spatialReference,
				       IssueRepositoryType,
				       addExceptionFields: true))
			{
				fulfilled = service.Verify(qualitySpecification, datasetContext, datasetResolver,
				                           issueRepository, tileSize,
				                           areaOfInterest, trackCancel,
				                           out int _,
				                           out int _,
				                           out int _);

				Verification = service.Verification;

				if (issueRepository != null)
				{
					issueGdbWritten = true;

					gdbPath = ((IWorkspace) issueRepository.FeatureWorkspace).PathName;

					InfoFormat("Issues written to {0}", sb, gdbPath);

					issueRepository.CreateIndexes(GetForSubProcess(trackCancel),
					                              ignoreErrors: true);
				}

				using (_msg.IncrementIndentation("Documenting verification results..."))
				{
					var properties = new List<KeyValuePair<string, string>>();

					XmlVerificationReport verificationReport = GetVerificationReport(
						xmlReportBuilder, qualitySpecification, properties);

					if (! string.IsNullOrWhiteSpace(_xmlVerificationReportPath))
					{
						XmlUtils.Serialize(verificationReport, _xmlVerificationReportPath);
						InfoFormat("Verification report written to {0}", sb,
						           _xmlVerificationReportPath);
					}

					IssueStatistics issueStatistics = statisticsBuilder.IssueStatistics;

					if (issueRepository != null)
					{
						var issueStatisticsWriter =
							new IssueStatisticsWriter(issueRepository.FeatureWorkspace);

						var statisticsTable =
							issueStatisticsWriter.WriteStatistics(issueStatistics);

						statisticsTable.Dispose();

						if (spatialReference != null &&
						    areaOfInterest != null &&
						    ! areaOfInterest.IsEmpty)
						{
							var aoiWriter =
								new AreaOfInterestWriter(issueRepository.FeatureWorkspace);

							IFeatureClass aoiFeatureClass =
								aoiWriter.WriteAreaOfInterest(areaOfInterest, spatialReference);

							Marshal.ReleaseComObject(aoiFeatureClass);
						}
					}

					if (! string.IsNullOrWhiteSpace(_htmlReportDir))
					{
						XmlVerificationOptions verificationOptions = null;
						specificationReportFilePaths =
							StandaloneVerificationUtils.WriteQualitySpecificationReport(
								qualitySpecification, _htmlReportDir,
								_qualitySpecificationTemplatePath,
								verificationOptions);

						htmlReportFilePaths = StandaloneVerificationUtils.WriteHtmlReports(
							qualitySpecification, _htmlReportDir, issueStatistics,
							verificationReport,
							XmlVerificationReportFileName, _htmlReportTemplatePath,
							verificationOptions,
							issueGdbWritten ? gdbPath : null,
							null, specificationReportFilePaths);
					}
				}
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();

			if (htmlReportFilePaths?.Count > 0)
			{
				string htmlReports = htmlReportFilePaths.Count == 1
					                     ? "Html report:"
					                     : "Html reports:";

				using (_msg.IncrementIndentation(htmlReports))
				{
					sb.AppendLine(htmlReports);

					foreach (string path in htmlReportFilePaths)
					{
						InfoFormat(path, sb);
					}
				}
			}

			if (specificationReportFilePaths?.Count > 0)
			{
				string specReports = specificationReportFilePaths.Count == 1
					                     ? "Quality specification report:"
					                     : "Quality specification reports:";

				using (_msg.IncrementIndentation(specReports))
				{
					sb.AppendLine(specReports);
					foreach (string path in specificationReportFilePaths)
					{
						InfoFormat(path, sb);
					}
				}
			}

			if (service.ExceptionObjectRepository != null)
			{
				IExceptionStatistics stats = service.ExceptionObjectRepository.ExceptionStatistics;
			}

			ProgressStreamer?.Info(sb.ToString());

			return fulfilled;
		}

		private static void InfoFormat([StructuredMessageTemplate] string format,
		                               [CanBeNull] StringBuilder fullMessage,
		                               params object[] args)
		{
			string message = string.Format(format, args);

			_msg.Info(message);
			fullMessage?.AppendLine(message);
		}

		private static XmlVerificationReportBuilder GetReportBuilder()
		{
			return new XmlVerificationReportBuilder(
				IssueReportingContexts.QualityConditionWithIssues,
				VerifiedConditionContexts.Summary,
				reportInvolvedTableForSchemaIssues: false);
		}

		[CanBeNull]
		private static ITrackCancel GetForSubProcess([CanBeNull] ITrackCancel trackCancel)
		{
			if (trackCancel == null)
			{
				return null;
			}

			// if cancelled, pass null to the subprocess - it should not be cancelled
			// (it not cancelled: pass the real thing to allow cancelling the subprocess)
			return ! trackCancel.Continue()
				       ? null
				       : trackCancel;
		}

		[NotNull]
		private static XmlVerificationReport GetVerificationReport(
			[NotNull] XmlVerificationReportBuilder xmlReportBuilder,
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IEnumerable<KeyValuePair<string, string>> properties)
		{
			return xmlReportBuilder.CreateReport(qualitySpecification.Name, properties);
		}
	}
}
