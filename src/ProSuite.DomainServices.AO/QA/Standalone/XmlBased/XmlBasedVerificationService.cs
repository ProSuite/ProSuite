using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
using XmlTestDescriptor = ProSuite.DomainModel.AO.QA.Xml.XmlTestDescriptor;

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

		public XmlBasedVerificationService(
			[CanBeNull] string htmlReportTemplatePath = null,
			[CanBeNull] string qualitySpecificationTemplatePath = null)
		{
			_htmlReportTemplatePath = htmlReportTemplatePath;
			_qualitySpecificationTemplatePath = qualitySpecificationTemplatePath;
		}

		public event EventHandler<IssueFoundEventArgs> IssueFound;

		public QualitySpecification SetupQualitySpecification(
			[NotNull] string dataQualityXml,
			[NotNull] string specificationName,
			[NotNull] IList<DataSource> dataSourceReplacements,
			bool ignoreConditionsForUnknownDatasets = true)
		{
			IList<XmlQualitySpecification> qualitySpecifications;
			XmlDataQualityDocument document;

			using (Stream baseStream = new MemoryStream(Encoding.UTF8.GetBytes(dataQualityXml)))
			using (StreamReader xmlReader = new StreamReader(baseStream))
			{
				document = XmlDataQualityUtils.ReadXmlDocument(xmlReader,
				                                               out qualitySpecifications);
			}

			_msg.DebugFormat("Available specifications: {0}",
			                 StringUtils.Concatenate(qualitySpecifications.Select(s => s.Name),
			                                         ", "));

			return SetupQualitySpecification(document, specificationName, dataSourceReplacements,
			                                 ignoreConditionsForUnknownDatasets);
		}

		public QualitySpecification SetupQualitySpecification(
			[NotNull] string specificationName,
			IList<XmlTestDescriptor> supportedDescriptors,
			[NotNull] IList<SpecificationElement> specificationElements,
			[NotNull] IEnumerable<DataSource> dataSources,
			bool ignoreConditionsForUnknownDatasets)
		{
			XmlBasedQualitySpecificationFactory factory = CreateSpecificationFactory();

			QualitySpecification result = factory.CreateQualitySpecification(
				specificationName, supportedDescriptors, specificationElements, dataSources,
				ignoreConditionsForUnknownDatasets);

			result.Name = specificationName;

			return result;
		}

		public void ExecuteVerification(
			[NotNull] QualitySpecification specification,
			[CanBeNull] AreaOfInterest areaOfInterest,
			[CanBeNull] string optionsXml,
			double tileSize,
			[NotNull] string outputDirectoryPath,
			IssueRepositoryType issueRepositoryType = IssueRepositoryType.FileGdb,
			ITrackCancel cancelTracker = null)
		{
			Assert.NotNullOrEmpty(outputDirectoryPath, "Output directory path is null or empty.");

			XmlVerificationOptions verificationOptions =
				StringUtils.IsNotEmpty(optionsXml)
					? VerificationOptionUtils.ReadOptionsXml(optionsXml)
					: null;

			string issueWorkspaceName =
				VerificationOptionUtils.GetIssueWorkspaceName(verificationOptions);

			if (ExternalIssueRepositoryUtils.IssueRepositoryExists(
				    outputDirectoryPath, issueWorkspaceName, issueRepositoryType))
			{
				_msg.WarnFormat("The {0} workspace '{1}' in {2} already exists",
				                issueRepositoryType, issueWorkspaceName, outputDirectoryPath);
				return;
			}

			var properties = new List<KeyValuePair<string, string>>();

			Directory.CreateDirectory(outputDirectoryPath);

			try
			{
				bool fulfilled = Verify(specification, tileSize, outputDirectoryPath,
				                        issueRepositoryType, properties,
				                        verificationOptions,
				                        areaOfInterest,
				                        cancelTracker,
				                        out int _, out int _, out int _, out int _, out int _);
			}
			catch (Exception)
			{
				StandaloneVerificationUtils.TryDeleteOutputDirectory(outputDirectoryPath);
				throw;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool Verify([NotNull] XmlDataQualityDocument document,
		                    [NotNull] string specificationName,
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
				QualitySpecification qualitySpecification =
					SetupQualitySpecification(document, specificationName, dataSources,
					                          ignoreConditionsForUnknownDatasets);

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
			}
		}

		private static QualitySpecification SetupQualitySpecification(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] string specificationName,
			[NotNull] IEnumerable<DataSource> dataSources,
			bool ignoreConditionsForUnknownDatasets)
		{
			QualitySpecification qualitySpecification;
			using (_msg.IncrementIndentation("Setting up quality specification"))
			{
				XmlBasedQualitySpecificationFactory factory = CreateSpecificationFactory();

				qualitySpecification = factory.CreateQualitySpecification(
					document, specificationName, dataSources,
					ignoreConditionsForUnknownDatasets);
			}

			return qualitySpecification;
		}

		private static XmlBasedQualitySpecificationFactory CreateSpecificationFactory()
		{
			var modelFactory = new VerifiedModelFactory(
				new MasterDatabaseWorkspaceContextFactory(), new SimpleVerifiedDatasetHarvester());

			var datasetOpener = new SimpleDatasetOpener(new MasterDatabaseDatasetContext());

			var factory =
				new XmlBasedQualitySpecificationFactory(modelFactory, datasetOpener);
			return factory;
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

			service.IssueFound += (sender, args) => IssueFound?.Invoke(this, args);

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
