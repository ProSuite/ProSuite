using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using Path = System.IO.Path;

namespace ProSuite.DomainServices.AO.QA
{
	/// <summary>
	/// Encapsulates the report creation during a quality verification.
	/// </summary>
	public class VerificationReporter
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private readonly string _issueGdbPath;
		[CanBeNull] private readonly string _xmlReportPath;

		private const string _progressWorkspaceName = "progress";

		private IVerificationReportBuilder _verificationReportBuilder;

		private XmlVerificationReportBuilder _xmlVerificationReportBuilder;
		private IssueStatisticsBuilder _statisticsBuilder;

		private string _xmlVerificationReportPath;

		public VerificationReporter([CanBeNull] IVerificationParameters verificationParameters)
			: this(verificationParameters?.IssueFgdbPath,
			       verificationParameters?.VerificationReportPath,
			       TryGetDirectory(verificationParameters?.VerificationReportPath))
		{
			WriteDetailedVerificationReport =
				verificationParameters?.WriteDetailedVerificationReport ?? false;

			HtmlQualitySpecificationTemplatePath =
				verificationParameters?.HtmlSpecificationTemplatePath;

			HtmlReportTemplatePath = verificationParameters?.HtmlReportTemplatePath;

			if (verificationParameters?.ReportProperties != null)
			{
				ReportProperties = verificationParameters.ReportProperties.ToList();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VerificationReporter"/> class.
		/// </summary>
		/// <param name="issueGdbPath">The full path to the issue geodatabase.</param>
		/// <param name="xmlReportPath">The full path to the xml report.</param>
		/// <param name="htmlReportDir">The report directory for the HTML report.</param>
		public VerificationReporter([CanBeNull] string issueGdbPath,
		                            [CanBeNull] string xmlReportPath,
		                            [CanBeNull] string htmlReportDir)
		{
			_issueGdbPath = string.IsNullOrEmpty(issueGdbPath) ? null : issueGdbPath;
			_xmlReportPath = string.IsNullOrEmpty(xmlReportPath) ? null : xmlReportPath;
			HtmlReportDir = string.IsNullOrEmpty(htmlReportDir) ? null : htmlReportDir;
		}

		[CanBeNull]
		private string XmlReportFileName =>
			_xmlReportPath != null
				? Path.GetFileName(_xmlReportPath)
				: VerificationOptionUtils.GetXmlReportFileName(XmlVerificationOptions);

		[CanBeNull]
		private string XmlReportDirectory =>
			_xmlReportPath != null
				? Path.GetDirectoryName(_xmlReportPath)
				: HtmlReportDir;

		/// <summary>
		/// The directory of the HTML report.
		/// </summary>
		[CanBeNull]
		public string HtmlReportDir { get; }

		/// <summary>
		/// The specification template path for the output HTML specification.
		/// </summary>
		public string HtmlQualitySpecificationTemplatePath { get; set; }

		/// <summary>
		/// The report template path for HTML reports.
		/// </summary>
		[CanBeNull]
		public string HtmlReportTemplatePath { get; set; }

		public bool WriteDetailedVerificationReport { get; set; }

		// False for standalone verification, true for standard service
		public bool ReportInvolvedTableForSchemaIssues { get; set; }

		/// <summary>
		/// The interface that allows streaming progress information during a lengthy operation.
		/// </summary>
		public IVerificationProgressStreamer ProgressStreamer { get; set; }

		/// <summary>
		/// These options could return some day.
		/// </summary>
		public XmlVerificationOptions XmlVerificationOptions { get; set; }

		public bool CanCreateIssueRepository => ! string.IsNullOrEmpty(_issueGdbPath);

		public InvolvedDatasetsCollector DatasetsCollector { get; private set; }

		public IList<KeyValuePair<string, string>> ReportProperties { get; } =
			new List<KeyValuePair<string, string>>();

		public IVerificationReportBuilder CreateReportBuilders()
		{
			var reportBuilders = new List<IVerificationReportBuilder>();

			_statisticsBuilder = new IssueStatisticsBuilder();
			DatasetsCollector = new InvolvedDatasetsCollector();

			reportBuilders.Add(_statisticsBuilder);
			reportBuilders.Add(DatasetsCollector);

			if (! string.IsNullOrEmpty(XmlReportDirectory))
			{
				_xmlVerificationReportBuilder = new XmlVerificationReportBuilder(
					WriteDetailedVerificationReport
						? IssueReportingContexts.QualityConditionWithIssues
						: IssueReportingContexts.None,
					VerifiedConditionContexts.Summary | VerifiedConditionContexts.Dataset,
					ReportInvolvedTableForSchemaIssues);

				reportBuilders.Add(_xmlVerificationReportBuilder);
			}

			_verificationReportBuilder = new MultiReportBuilder(reportBuilders);

			return _verificationReportBuilder;
		}

		public void AddVerifiedDataset([NotNull] QualityVerificationDataset verificationDataset,
		                               [CanBeNull] IWorkspaceContext workspaceContext)
		{
			string workspaceDisplayText = null;
			ISpatialReference spatialReference = null;

			if (WriteDetailedVerificationReport)
			{
				try
				{
					IWorkspace workspace = workspaceContext?.Workspace;

					workspaceDisplayText = workspace != null
						                       ? WorkspaceUtils.GetWorkspaceDisplayText(workspace)
						                       : "<N.A.>";

					Dataset dataset = verificationDataset.Dataset;

					if (dataset is IVectorDataset vectorDataset)
					{
						IFeatureClass featureClass =
							workspaceContext?.OpenFeatureClass(vectorDataset);

						if (featureClass != null)
						{
							spatialReference = DatasetUtils.GetSpatialReference(featureClass);
						}
					}
				}
				catch (Exception e)
				{
					_msg.Warn("Unable to get detailed dataset properties " +
					          $"from {verificationDataset.Dataset}", e);
				}
			}

			_verificationReportBuilder.AddVerifiedDataset(verificationDataset,
			                                              workspaceDisplayText, spatialReference);
		}

		public void AddVerifiedConditions(IEnumerable<QualitySpecificationElement> elements)
		{
			foreach (QualitySpecificationElement element in elements)
			{
				_verificationReportBuilder.AddVerifiedQualityCondition(element);
			}
		}

		[CanBeNull]
		public IIssueRepository CreateIssueRepository(
			IssueRepositoryType issueRepositoryType,
			[CanBeNull] ISpatialReference spatialReference,
			bool addExceptionFields = false)
		{
			if (_issueGdbPath == null)
			{
				return null;
			}

			ProgressStreamer?.Info("Creating external issue file geodatabase");

			var watch = _msg.DebugStartTiming("Creating issue repository {0}...", _issueGdbPath);

			string directoryPath = Path.GetDirectoryName(_issueGdbPath);
			Assert.NotNull(directoryPath,
			               "Invalid full path to gdb (undefined directory): {0}", _issueGdbPath);

			string gdbName = Path.GetFileName(_issueGdbPath);
			Assert.NotNull(gdbName, "Invalid full path to gdb (undefined file name): {0}",
			               _issueGdbPath);

			IIssueRepository result = ExternalIssueRepositoryUtils.GetIssueRepository(
				directoryPath, gdbName, spatialReference, issueRepositoryType,
				addExceptionFields: addExceptionFields);

			_msg.DebugStopTiming(watch, "Created issue repository");

			return result;
		}

		[CanBeNull]
		public ISubVerificationObserver CreateSubVerificationObserver(
			IssueRepositoryType issueRepositoryType,
			[CanBeNull] ISpatialReference spatialReference)
		{
			if (string.IsNullOrEmpty(_issueGdbPath))
			{
				return null;
			}

			string directoryPath = Path.GetDirectoryName(_issueGdbPath);
			Assert.NotNull(directoryPath,
			               "Invalid full path to gdb (undefined directory): {0}", _issueGdbPath);

			var watch = _msg.DebugStartTiming(
				"Creating sub-verification progress file geodatabase {0} in {1}...",
				_progressWorkspaceName, directoryPath);

			ISubVerificationObserver result = SubVerificationObserverUtils.GetProgressRepository(
				directoryPath, _progressWorkspaceName, spatialReference, issueRepositoryType);

			_msg.DebugStopTiming(watch, "Created sub-verification progress repository");

			return result;
		}

		public XmlVerificationReport WriteReports(
			[NotNull] QualitySpecification qualitySpecification)
		{
			StringBuilder sb = new StringBuilder();

			XmlVerificationReport verificationReport;

			using (_msg.IncrementIndentation("Documenting verification results..."))
			{
				verificationReport =
					_xmlVerificationReportBuilder?.CreateReport(
						qualitySpecification.Name, ReportProperties);

				if (verificationReport != null &
				    ! string.IsNullOrWhiteSpace(XmlReportDirectory) &&
				    ! string.IsNullOrWhiteSpace(XmlReportFileName))
				{
					string name = XmlReportFileName.Trim();

					if (FileSystemUtils.HasInvalidFileNameChars(name))
					{
						throw new InvalidConfigurationException(
							$"Xml report name is not a valid file name: {name}");
					}

					// TODO: Move to separate method SetOutputPaths()
					_xmlVerificationReportPath = Path.Combine(XmlReportDirectory, name);

					XmlUtils.Serialize(verificationReport, _xmlVerificationReportPath);

					InfoFormat("Verification report written to {0}", sb,
					           _xmlVerificationReportPath);
				}

				List<string> htmlReportFilePaths = null;
				List<string> specificationReportFilePaths = null;

				if (verificationReport != null &&
				    ! string.IsNullOrWhiteSpace(HtmlReportDir) &&
				    ! string.IsNullOrWhiteSpace(_xmlVerificationReportPath))
				{
					specificationReportFilePaths =
						StandaloneVerificationUtils.WriteQualitySpecificationReport(
							qualitySpecification, HtmlReportDir,
							HtmlQualitySpecificationTemplatePath, XmlVerificationOptions);

					string xmlVerificationReportFileName =
						Path.GetFileName(_xmlVerificationReportPath);

					htmlReportFilePaths = StandaloneVerificationUtils.WriteHtmlReports(
						qualitySpecification, HtmlReportDir, _statisticsBuilder.IssueStatistics,
						verificationReport, xmlVerificationReportFileName, HtmlReportTemplatePath,
						XmlVerificationOptions, _issueGdbPath, null, specificationReportFilePaths);
				}

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

				ProgressStreamer?.Info(sb.ToString());
			}

			return verificationReport;
		}

		public void CreateIssueRepositoryIndexes([CanBeNull] IIssueRepository issueRepository,
		                                         [CanBeNull] ITrackCancel trackCancel = null)
		{
			if (issueRepository == null)
			{
				return;
			}

			string gdbPath = ((IWorkspace) issueRepository.FeatureWorkspace).PathName;

			StringBuilder sb = new StringBuilder();

			InfoFormat("Issues written to {0}", sb, gdbPath);

			issueRepository.CreateIndexes(trackCancel, ignoreErrors: true);

			ProgressStreamer?.Info(sb.ToString());
		}

		public string WriteIssueStatistics(IIssueRepository issueRepository)
		{
			if (issueRepository == null)
			{
				return null;
			}

			_msg.InfoFormat("Writing issue statistics table");

			IssueStatistics issueStatistics = _statisticsBuilder.IssueStatistics;

			var issueStatisticsWriter =
				new IssueStatisticsWriter(issueRepository.FeatureWorkspace);

			var statisticsTable =
				issueStatisticsWriter.WriteStatistics(issueStatistics);

			string tableName = DatasetUtils.GetName(statisticsTable.Table);

			statisticsTable.Dispose();

			return tableName;
		}

		public string WriteAreaOfInterest([CanBeNull] IIssueRepository issueRepository,
		                                  [CanBeNull] AreaOfInterest areaOfInterest,
		                                  [CanBeNull] ISpatialReference spatialReference)
		{
			if (issueRepository == null)
			{
				return null;
			}

			string resultTableName = null;

			if (spatialReference != null &&
			    areaOfInterest != null &&
			    ! areaOfInterest.IsEmpty)
			{
				IFeatureWorkspace issueWorkspace = issueRepository.FeatureWorkspace;
				var aoiWriter = new AreaOfInterestWriter(issueWorkspace);

				IFeatureClass aoiFeatureClass =
					aoiWriter.WriteAreaOfInterest(areaOfInterest, spatialReference);

				resultTableName = DatasetUtils.GetName(aoiFeatureClass);

				Marshal.ReleaseComObject(aoiFeatureClass);
			}

			return resultTableName;
		}

		private static void InfoFormat([StructuredMessageTemplate] string format,
		                               [CanBeNull] StringBuilder fullMessage,
		                               params object[] args)
		{
			string message = string.Format(format, args);

			_msg.Info(message);
			fullMessage?.AppendLine(message);
		}

		private static string TryGetDirectory([CanBeNull] string verificationReportFilePath)
		{
			return string.IsNullOrEmpty(verificationReportFilePath)
				       ? null
				       : Path.GetDirectoryName(verificationReportFilePath);
		}
	}
}
