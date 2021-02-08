using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.QA.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using Path = System.IO.Path;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased
{
	/// <summary>
	/// Standalone (i.e. non-DDX dependent) verification service based on a xml specification.
	/// This should probably be driven by a separate gRPC service that uses a simpler request/response.
	/// </summary>
	public class XmlBasedVerificationService
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private readonly string _htmlReportTemplatePath;
		[CanBeNull] private readonly string _qualitySpecificationTemplatePath;

		public XmlBasedVerificationService(
			[CanBeNull] string htmlReportTemplatePath = null,
			[CanBeNull] string qualitySpecificationTemplatePath = null)
		{
			_htmlReportTemplatePath = htmlReportTemplatePath;
			_qualitySpecificationTemplatePath = qualitySpecificationTemplatePath;
		}

		[CLSCompliant(false)]
		public void ExecuteVerification(
			[NotNull] string dataQualityXml,
			[CanBeNull] string specificationName,
			[NotNull] IList<string> dataSourceReplacements,
			[CanBeNull] AreaOfInterest areaOfInterest,
			[CanBeNull] string optionsXml,
			double tileSize,
			string outputDirectoryPath,
			IssueRepositoryType issueRepositoryType = IssueRepositoryType.FileGdb,
			bool ignoreConditionsForUnknownDatasets = true,
			ITrackCancel cancelTracker = null)
		{
			IList<XmlQualitySpecification> qualitySpecifications;
			XmlDataQualityDocument document = ReadXmlDocument(dataQualityXml,
			                                                  out qualitySpecifications);

			XmlQualitySpecification xmlQualitySpecification =
				qualitySpecifications.FirstOrDefault(
					s =>
						specificationName == null ||
						specificationName.Equals(
							s.Name, StringComparison.CurrentCultureIgnoreCase));

			Assert.NotNull(xmlQualitySpecification, "qualitySpecification");

			IList<DataSource> dataSources =
				StandaloneVerificationUtils.GetDataSources(document, xmlQualitySpecification);

			ApplyDataSourceChanges(dataSourceReplacements, dataSources);

			XmlVerificationOptions verificationOptions =
				StringUtils.IsNotEmpty(optionsXml)
					? VerificationOptionUtils.ReadOptionsXml(optionsXml)
					: null;

			IList<KeyValuePair<string, string>> properties =
				new List<KeyValuePair<string, string>>();

			Directory.CreateDirectory(outputDirectoryPath);

			try
			{
				int errorCount;
				int warningCount;
				int exceptionCount;
				int unusedExceptionObjectCount;
				int rowCountWithStopConditions;
				bool fulfilled = Verify(document, dataSources,
				                        tileSize, outputDirectoryPath,
				                        issueRepositoryType, properties,
				                        verificationOptions,
				                        areaOfInterest,
				                        cancelTracker,
				                        ignoreConditionsForUnknownDatasets,
				                        out errorCount,
				                        out warningCount,
				                        out exceptionCount,
				                        out unusedExceptionObjectCount,
				                        out rowCountWithStopConditions);
			}
			catch (Exception)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				StandaloneVerificationUtils.TryDeleteOutputDirectory(outputDirectoryPath);
				throw;
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		[NotNull]
		private static XmlDataQualityDocument ReadXmlDocument(
			[NotNull] string xml,
			[NotNull] out IList<XmlQualitySpecification> qualitySpecifications)
		{
			Assert.ArgumentNotNullOrEmpty(xml, nameof(xml));

			XmlDataQualityDocument document = XmlDataQualityUtils.DeserializeXml(xml);

			Assert.True(document.GetAllQualitySpecifications().Any(),
			            "The document does not contain any quality specifications");
			XmlDataQualityUtils.AssertUniqueQualitySpecificationNames(document);
			XmlDataQualityUtils.AssertUniqueQualityConditionNames(document);
			XmlDataQualityUtils.AssertUniqueTestDescriptorNames(document);

			qualitySpecifications = document.GetAllQualitySpecifications()
			                                .Select(p => p.Key)
			                                .Where(qs => qs.Elements.Count > 0)
			                                .ToList();

			return document;
		}

		private static void ApplyDataSourceChanges(
			[NotNull] IList<string> dataSourceReplacements,
			[NotNull] IList<DataSource> dataSources)
		{
			Assert.ArgumentNotNull(dataSourceReplacements, nameof(dataSourceReplacements));
			Assert.ArgumentNotNull(dataSources, nameof(dataSources));

			if (dataSourceReplacements.Count == 0)
			{
				return;
			}

			Assert.AreEqual(dataSources.Count, dataSourceReplacements.Count,
			                "The number of data source replacement workspaces does not match number of data sources in XML specification.");

			for (var i = 0; i < dataSources.Count; i++)
			{
				DataSource dataSource = dataSources[i];
				string replacement = dataSourceReplacements[i];

				_msg.DebugFormat("Replacing data source {0} with {1}...", dataSource.DisplayName,
				                 replacement);

				dataSource.WorkspaceAsText = replacement;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool Verify([NotNull] XmlDataQualityDocument document,
		                    [NotNull] IEnumerable<DataSource> dataSources,
		                    double tileSize,
		                    [NotNull] string directoryPath,
		                    IssueRepositoryType issureRepositoryType,
		                    [NotNull] IEnumerable<KeyValuePair<string, string>> properties,
		                    [CanBeNull] XmlVerificationOptions verificationOptions,
		                    [CanBeNull] AreaOfInterest areaOfInterest,
		                    [CanBeNull] ITrackCancel trackCancel,
		                    bool ignoreConditionsForUnknownDatasets,
		                    out int errorCount,
		                    out int warningCount,
		                    out int exceptionCount,
		                    out int unusedExceptionObjectCount,
		                    out int rowCountWithStopConditions)
		{
			try
			{
				QualitySpecification qualitySpecification;
				using (_msg.IncrementIndentation("Setting up quality specification"))
				{
					// TODO report errors 
					var modelFactory = new VerifiedModelFactory(
						CreateSimpleWorkspaceContext, new SimpleVerifiedDatasetHarvester());

					var datasetOpener = new SimpleDatasetOpener(new MasterDatabaseDatasetContext());

					var factory =
						new XmlBasedQualitySpecificationFactory(modelFactory, datasetOpener);

					// Assumption: the document contains *exactly* 1 specification.
					qualitySpecification = factory.CreateQualitySpecification(
						document, dataSources, ignoreConditionsForUnknownDatasets);
				}

				return Verify(qualitySpecification, tileSize, directoryPath,
				              issureRepositoryType, properties, verificationOptions,
				              areaOfInterest, trackCancel,
				              out errorCount,
				              out warningCount,
				              out exceptionCount,
				              out unusedExceptionObjectCount,
				              out rowCountWithStopConditions);
			}
			finally
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		private bool Verify([NotNull] QualitySpecification qualitySpecification,
		                    double tileSize,
		                    [NotNull] string directory,
		                    IssueRepositoryType issueRepositoryType,
		                    [NotNull] IEnumerable<KeyValuePair<string, string>> properties,
		                    [CanBeNull] XmlVerificationOptions verificationOptions,
		                    [CanBeNull] AreaOfInterest areaOfInterest,
		                    [CanBeNull] ITrackCancel trackCancel,
		                    out int errorCount,
		                    out int warningCount,
		                    out int exceptionCount,
		                    out int unusedExceptionObjectCount,
		                    out int rowCountWithStopConditions)
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

			// This context excludes geometric networks, terrains, topology, etc.:
			var datasetContext = new MasterDatabaseDatasetContext();
			var datasetResolver =
				new QualityConditionObjectDatasetResolver(
					new MasterDatabaseWorkspaceContextLookup());

			string issueWorkspaceName =
				VerificationOptionUtils.GetIssueWorkspaceName(verificationOptions);
			string verificationReportFileName =
				VerificationOptionUtils.GetXmlReportFileName(verificationOptions);

			ISpatialReference spatialReference =
				primaryModel.SpatialReferenceDescriptor?.SpatialReference;

			var issueGdbWritten = false;
			bool fulfilled;

			List<string> htmlReportFilePaths;
			List<string> specificationReportFilePaths;
			string gdbPath = null;

			Func<IObjectDataset, string> getKeyField =
				StandaloneVerificationUtils.GetKeyFieldLookupFunction(verificationOptions);

			ExceptionObjectRepository exceptionObjectRepository =
				StandaloneVerificationUtils.PrepareExceptionRepository(
					qualitySpecification, datasetContext, datasetResolver, areaOfInterest,
					verificationOptions);

			using (IIssueRepository issueRepository =
				ExternalIssueRepositoryUtils.GetIssueRepository(
					directory, issueWorkspaceName, spatialReference, issueRepositoryType,
					addExceptionFields: true))
			{
				fulfilled = service.Verify(qualitySpecification, datasetContext, datasetResolver,
				                           issueRepository, exceptionObjectRepository, tileSize,
				                           getKeyField,
				                           areaOfInterest, trackCancel,
				                           out errorCount,
				                           out warningCount,
				                           out rowCountWithStopConditions);

				if (issueRepository != null)
				{
					issueGdbWritten = true;

					gdbPath = ((IWorkspace) issueRepository.FeatureWorkspace).PathName;

					_msg.InfoFormat("Issues written to {0}", gdbPath);

					issueRepository.CreateIndexes(GetForSubProcess(trackCancel),
					                              ignoreErrors: true);
				}

				using (_msg.IncrementIndentation("Documenting verification results..."))
				{
					XmlVerificationReport verificationReport = GetVerificationReport(
						xmlReportBuilder, qualitySpecification, properties);

					string verificationReportPath = Path.Combine(directory,
					                                             verificationReportFileName);
					XmlUtils.Serialize(verificationReport, verificationReportPath);

					_msg.InfoFormat("Verification report written to {0}", verificationReportPath);

					IssueStatistics issueStatistics = statisticsBuilder.IssueStatistics;

					if (issueRepository != null)
					{
						var issueStatisticsWriter =
							new IssueStatisticsWriter(issueRepository.FeatureWorkspace);

						issueStatisticsWriter.WriteStatistics(issueStatistics);

						if (spatialReference != null &&
						    areaOfInterest != null &&
						    ! areaOfInterest.IsEmpty)
						{
							var aoiWriter =
								new AreaOfInterestWriter(issueRepository.FeatureWorkspace);
							aoiWriter.WriteAreaOfInterest(areaOfInterest, spatialReference);
						}
					}

					specificationReportFilePaths =
						StandaloneVerificationUtils.WriteQualitySpecificationReport(
							qualitySpecification, directory, _qualitySpecificationTemplatePath,
							verificationOptions);

					htmlReportFilePaths = StandaloneVerificationUtils.WriteHtmlReports(
						qualitySpecification, directory, issueStatistics, verificationReport,
						verificationReportFileName, _htmlReportTemplatePath, verificationOptions,
						issueGdbWritten ? gdbPath : null,
						null, specificationReportFilePaths);
				}
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();

			if (htmlReportFilePaths.Count > 0)
			{
				using (_msg.IncrementIndentation(htmlReportFilePaths.Count == 1
					                                 ? "Html report:"
					                                 : "Html reports:"))
				{
					foreach (string path in htmlReportFilePaths)
					{
						_msg.Info(path);
					}
				}
			}

			if (specificationReportFilePaths.Count > 0)
			{
				using (_msg.IncrementIndentation(specificationReportFilePaths.Count == 1
					                                 ? "Quality specification report:"
					                                 : "Quality specification reports:"))
				{
					foreach (string path in specificationReportFilePaths)
					{
						_msg.Info(path);
					}
				}
			}

			if (exceptionObjectRepository != null)
			{
				IExceptionStatistics stats = exceptionObjectRepository.ExceptionStatistics;
				exceptionCount = stats.ExceptionCount;
				unusedExceptionObjectCount = stats.UnusedExceptionObjectCount;
			}
			else
			{
				exceptionCount = 0;
				unusedExceptionObjectCount = 0;
			}

			return fulfilled;
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

		private static IWorkspaceContext CreateSimpleWorkspaceContext(
			[NotNull] Model model,
			[NotNull] IFeatureWorkspace workspace)
		{
			return new MasterDatabaseWorkspaceContext(workspace, model);
		}
	}
}
