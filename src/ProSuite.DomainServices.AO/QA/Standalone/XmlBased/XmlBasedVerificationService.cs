using System;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using Path = System.IO.Path;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased
{
	/// <summary>
	/// Standalone (i.e. non-DDX dependent) verification service based on a xml specification or
	/// based on a list of conditions (defined via xml). This service supports the exception-Gdb
	/// mechanism. This class is currently named after the (legacy) XML-based GP Tool because it
	/// provides the same functionality.
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

		public ISpatialReference IssueRepositorySpatialReference { get; set; }

		/// <summary>
		/// Optional XML verification options that provide additional configurations.
		/// </summary>
		public XmlVerificationOptions XmlVerificationOptions { get; set; }

		public QualityVerification Verification { get; set; }

		public event EventHandler<IssueFoundEventArgs> IssueFound;

		public void SetupOutputPaths(string outputDirectory)
		{
			Assert.True(FileSystemUtils.EnsureDirectoryExists(outputDirectory),
			            $"Invalid directory: {outputDirectory}");

			_issueRepositoryDir = outputDirectory;

			_issueRepositoryName =
				VerificationOptionUtils.GetIssueWorkspaceName(XmlVerificationOptions);

			string verificationReportFileName =
				VerificationOptionUtils.GetXmlReportFileName(XmlVerificationOptions);
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

		public bool ExecuteVerification(
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
				return false;
			}

			IssueRepositoryType = issueRepositoryType;
			SetupOutputPaths(outputDirectoryPath);

			bool fulfilled;
			try
			{
				fulfilled = ExecuteVerification(specification,
				                                areaOfInterest,
				                                tileSize, cancelTracker);
			}
			catch (Exception)
			{
				StandaloneVerificationUtils.TryDeleteOutputDirectory(outputDirectoryPath);
				throw;
			}

			return fulfilled;
		}

		public bool ExecuteVerification(
			[NotNull] QualitySpecification qualitySpecification,
			[CanBeNull] AreaOfInterest areaOfInterest,
			double tileSize,
			[CanBeNull] ITrackCancel trackCancel)
		{
			if (qualitySpecification.Elements.Count == 0)
			{
				// TODO: report warning via progress to client
				_msg.Warn("The provided quality specification contains no quality conditions. " +
				          "The specification is fulfilled but no reports will be generated.");

				return true;
			}

			DdxModel primaryModel =
				StandaloneVerificationUtils.GetPrimaryModel(qualitySpecification);
			Assert.NotNull(primaryModel, "no primary model found for quality specification");

			string issueGdbPath = null;

			if (! string.IsNullOrEmpty(_issueRepositoryDir) &&
			    ! string.IsNullOrEmpty(_issueRepositoryName))
			{
				issueGdbPath = Path.Combine(_issueRepositoryDir, _issueRepositoryName);
			}

			VerificationReporter verificationReporter =
				new VerificationReporter(issueGdbPath, _xmlVerificationReportPath, _htmlReportDir)
				{
					WriteDetailedVerificationReport = true,
					HtmlReportTemplatePath = _htmlReportTemplatePath,
					HtmlQualitySpecificationTemplatePath = _qualitySpecificationTemplatePath,
					ProgressStreamer = ProgressStreamer,
					XmlVerificationOptions = XmlVerificationOptions
				};

			IVerificationReportBuilder reportBuilder = verificationReporter.CreateReportBuilders();

			// TODO disable quality conditions based on primaryModel and DatasetTestParameterValue.UsedAsReferenceData?
			// TODO this would probably require an explicit identification of the primary data source

			var service = new StandaloneQualityVerificationService(
				reportBuilder, context => new SimpleDatasetOpener(context));

			service.IssueFound += (sender, args) => IssueFound?.Invoke(this, args);

			// This context excludes geometric networks, terrains, topology, etc.:
			var datasetContext = new MasterDatabaseDatasetContext();
			var datasetResolver =
				new QualityConditionObjectDatasetResolver(
					new MasterDatabaseWorkspaceContextLookup());

			ISpatialReference spatialReference =
				primaryModel.SpatialReferenceDescriptor?.GetSpatialReference();

			ISpatialReference issuesSpatialReference =
				IssueRepositorySpatialReference ?? spatialReference;

			bool fulfilled;

			service.DistributedTestRunner = DistributedTestRunner;
			service.ProgressStreamer = ProgressStreamer;

			ISubVerificationObserver subVerificationObserver =
				DistributedTestRunner?.AddObserver(verificationReporter, issuesSpatialReference);

			using (IIssueRepository issueRepository =
			       verificationReporter.CreateIssueRepository(
				       IssueRepositoryType, issuesSpatialReference, addExceptionFields: true))
			{
				fulfilled = service.Verify(qualitySpecification, datasetContext, datasetResolver,
				                           issueRepository, tileSize, areaOfInterest, trackCancel,
				                           out int _,
				                           out int _,
				                           out int _);

				Verification = service.Verification;

				verificationReporter.CreateIssueRepositoryIndexes(issueRepository, trackCancel);

				verificationReporter.WriteIssueStatistics(issueRepository);
				verificationReporter.WriteAreaOfInterest(issueRepository, areaOfInterest,
				                                         spatialReference);

				verificationReporter.WriteReports(qualitySpecification);
			}

			subVerificationObserver?.Dispose();

			GC.Collect();
			GC.WaitForPendingFinalizers();

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
	}
}
