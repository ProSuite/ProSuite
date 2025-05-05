using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Standalone
{
	public class StandaloneQualityVerificationService
	{
		[NotNull] private readonly IVerificationReportBuilder _verificationReportBuilder;
		private readonly Func<IDatasetContext, IOpenDataset> _openDatasetFactory;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="StandaloneQualityVerificationService"/> class.
		/// </summary>
		/// <param name="verificationReportBuilder">The verification report builder.</param>
		/// <param name="openDatasetFactory">Factory method that creates the appropriate
		/// IOpenDataset implementation.</param>
		public StandaloneQualityVerificationService(
			[NotNull] IVerificationReportBuilder verificationReportBuilder,
			Func<IDatasetContext, IOpenDataset> openDatasetFactory)
		{
			Assert.ArgumentNotNull(verificationReportBuilder,
			                       nameof(verificationReportBuilder));

			_verificationReportBuilder = verificationReportBuilder;
			_openDatasetFactory = openDatasetFactory;
		}

		#endregion

		/// <summary>
		/// The optional exception object repository.
		/// </summary>
		[CanBeNull]
		public IExceptionObjectRepository ExceptionObjectRepository { get; set; }

		/// <summary>
		/// The interface that allows streaming progress information during a lengthy operation.
		/// </summary>
		[CanBeNull]
		public IVerificationProgressStreamer ProgressStreamer { get; set; }

		/// <summary>
		/// Function for getting the key field name for an object dataset.
		/// </summary>
		[CanBeNull]
		public Func<IObjectDataset, string> GetKeyFieldNameFunc { get; set; }

		/// <summary>
		/// The test runner to be used if parallel processing has been requested. No parallel
		/// processing will take place if this property has not been set.
		/// </summary>
		[CanBeNull]
		public ITestRunner DistributedTestRunner { get; set; }

		private CancellationTokenSource _cancellationTokenSource;

		private CancellationTokenSource CancellationTokenSource =>
			_cancellationTokenSource ??
			(_cancellationTokenSource = new CancellationTokenSource());

		public event EventHandler<VerificationProgressEventArgs> Progress;

		public event EventHandler<IssueFoundEventArgs> IssueFound;

		public QualityVerification Verification { get; set; }

		/// <summary>
		/// Verifies the specified object classes.
		/// </summary>
		/// <param name="qualitySpecification">The quality specification to verify.</param>
		/// <param name="datasetContext">The model context.</param>
		/// <param name="datasetResolver">The resolver for getting the object dataset based on a table name, in the context of a quality condition</param>
		/// <param name="issueRepository">The issue repository.</param>
		/// <param name="tileSize">Tile size for the quality verification.</param>
		/// <param name="areaOfInterest">The test run perimeter (optional).</param>
		/// <param name="trackCancel">The cancel tracker.</param>
		/// <param name="errorCount">The number of (hard) errors.</param>
		/// <param name="warningCount">The number of warnings.</param>
		/// <param name="rowCountWithStopConditions">The number of rows for which a stop condition was violated - those rows may not be completely tested.</param>
		/// <returns></returns>
		public bool Verify([NotNull] QualitySpecification qualitySpecification,
		                   [NotNull] IDatasetContext datasetContext,
		                   [NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
		                   [CanBeNull] IIssueRepository issueRepository,
		                   double tileSize,
		                   [CanBeNull] AreaOfInterest areaOfInterest,
		                   [CanBeNull] ITrackCancel trackCancel,
		                   out int errorCount,
		                   out int warningCount,
		                   out int rowCountWithStopConditions)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));
			Assert.ArgumentCondition(tileSize > 0, "Invalid tile size: {0}", tileSize);

			_verificationReportBuilder.BeginVerification(areaOfInterest);

			IList<ITest> tests =
				CreateTests(qualitySpecification, datasetContext,
				            out VerificationElements verificationElements,
				            out QualityVerification verification);

			Verification = verification;

			var qualityConditionCount = 0;
			foreach (QualitySpecificationElement element in verificationElements.Elements)
			{
				qualityConditionCount++;
				_verificationReportBuilder.AddVerifiedQualityCondition(element);
			}

			var datasetCount = 0;
			foreach (QualityVerificationDataset verificationDataset in GetVerifiedDatasets(
				         verification, datasetContext))
			{
				datasetCount++;

				AddDatasetToReportBuilder(verificationDataset);
			}

			Stopwatch watch = _msg.DebugStartTiming();

			LogTests(tests, verificationElements.ElementsByTest);

			ITestRunner testRunner = DistributedTestRunner ??
			                         CreateSingleThreadedTestRunner(
				                         verificationElements, tileSize);

			testRunner.TestAssembler =
				new TestAssembler(t => verificationElements.GetQualityCondition(t));

			testRunner.QualityVerification = verification;

			LogBeginVerification(qualitySpecification, tileSize, areaOfInterest);

			ProgressProcessor progressProcessor =
				new ProgressProcessor(
					CancellationTokenSource, verificationElements.ElementsByTest, trackCancel)
				{
					ProgressStreamer = ProgressStreamer
				};

			IssueProcessor issueProcessor;

			using (var issueWriter = new BufferedIssueWriter(_verificationReportBuilder,
			                                                 datasetContext, datasetResolver,
			                                                 issueRepository,
			                                                 GetKeyFieldNameFunc))
			{
				issueProcessor = CreateIssueProcessor(
					tests, issueWriter, areaOfInterest, ExceptionObjectRepository,
					verificationElements.ElementsByTest);

				testRunner.Progress += (sender, args) => progressProcessor.Process(args);
				testRunner.Progress += (sender, args) => Progress?.Invoke(sender, args);

				testRunner.QaError += (sender, args) => issueProcessor.Process(args);
				issueProcessor.IssueFound += (sender, args) => IssueFound?.Invoke(this, args);

				// run the tests
				testRunner.Execute(tests, areaOfInterest, CancellationTokenSource);
			}

			_verificationReportBuilder.AddRowsWithStopConditions(
				issueProcessor.GetRowsWithStopConditions());

			if (ExceptionObjectRepository != null)
			{
				_verificationReportBuilder.AddExceptionStatistics(
					ExceptionObjectRepository.ExceptionStatistics);
			}

			_verificationReportBuilder.EndVerification(progressProcessor.Cancelled);

			_msg.DebugStopTiming(watch, "Verification");

			errorCount = issueProcessor.ErrorCount;
			warningCount = issueProcessor.WarningCount;
			rowCountWithStopConditions = issueProcessor.RowsWithStopConditionsCount;

			// The verification could be un-fulfilled due to an exception
			bool fulfilled = errorCount == 0 && verification.Fulfilled &&
			                 ! progressProcessor.Cancelled;

			LogResults(verificationElements.Elements, issueProcessor,
			           qualityConditionCount, datasetCount,
			           fulfilled, progressProcessor.Cancelled,
			           ExceptionObjectRepository?.ExceptionStatistics);

			return fulfilled;
		}

		private void AddDatasetToReportBuilder(QualityVerificationDataset verificationDataset)
		{
			try
			{
				Dataset dataset = verificationDataset.Dataset;

				Model model = dataset.Model as Model;

				// TODO: only if WriteDetailedReport == true (use VerificationReporter from other service?)
				IWorkspaceContext workspaceContext = model?.GetMasterDatabaseWorkspaceContext();

				IWorkspace workspace = workspaceContext?.Workspace;

				string workspaceDisplayText =
					workspace != null
						? WorkspaceUtils.GetWorkspaceDisplayText(workspace)
						: "<N.A.>";

				ISpatialReference spatialReference = null;

				if (dataset is IVectorDataset vectorDataset)
				{
					IFeatureClass featureClass =
						workspaceContext?.OpenFeatureClass(vectorDataset);

					if (featureClass != null)
					{
						spatialReference = DatasetUtils.GetSpatialReference(featureClass);
					}
				}

				_verificationReportBuilder.AddVerifiedDataset(
					verificationDataset, workspaceDisplayText, spatialReference);
			}
			catch (Exception e)
			{
				_msg.Warn(
					$"Failed to add verified dataset to report. {ExceptionUtils.FormatMessage(e)}",
					e);
			}
		}

		private static ITestRunner CreateSingleThreadedTestRunner(
			VerificationElements verificationElements,
			double tileSize)
		{
			SingleThreadedTestRunner testRunner =
				new SingleThreadedTestRunner(verificationElements, tileSize)
				{
					AllowEditing = false,
					FilterTableRowsUsingRelatedGeometry = false,
					ForceFullScanForNonContainerTests = false, //parameters.ForceFullScan...?,
					ObjectSelection = null,
					LocationBasedQualitySpecification = null,
					QualityVerification = null,
					DatasetLookup = null
				};

			return testRunner;
		}

		[CanBeNull]
		private static IGeometry GetTestPerimeter([CanBeNull] AreaOfInterest areaOfInterest,
		                                          [NotNull] IEnumerable<ITest> tests)
		{
			if (areaOfInterest == null)
			{
				return null;
			}

			ISpatialReference spatialReference = TestUtils.GetUniqueSpatialReference(tests);

			GeometryUtils.EnsureSpatialReference(areaOfInterest.Geometry, spatialReference, false,
			                                     out IGeometry result);
			return result;
		}

		[NotNull]
		private static IssueProcessor CreateIssueProcessor(
			[NotNull] IEnumerable<ITest> tests,
			[NotNull] IIssueWriter issueWriter,
			[CanBeNull] AreaOfInterest areaOfInterest,
			[CanBeNull] IExceptionObjectRepository exceptionObjectRepository,
			[NotNull] IDictionary<ITest, QualitySpecificationElement> elementsByTest)
		{
			IGeometry testPerimeter = GetTestPerimeter(areaOfInterest, tests);

			return new IssueProcessor(
				issueWriter,
				elementsByTest,
				testPerimeter,
				exceptionObjectRepository?.ExceptionObjectEvaluator);
		}

		[NotNull]
		private static IEnumerable<QualityVerificationDataset> GetVerifiedDatasets(
			[NotNull] QualityVerification qualityVerification,
			[NotNull] IDatasetContext datasetContext)
		{
			var datasets = new SimpleSet<Dataset>();

			foreach (QualityVerificationDataset qvds in qualityVerification.VerificationDatasets)
			{
				Dataset dataset = qvds.Dataset;

				if (datasetContext.CanOpen(dataset))
				{
					yield return qvds;
				}
			}

			//foreach (
			//	QualitySpecificationElement qualitySpecificationElement in
			//	qualityVerification.Elements)
			//{
			//	QualityCondition qualityCondition = qualitySpecificationElement.QualityCondition;

			//	if (qualityCondition == null)
			//	{
			//		continue;
			//	}

			//	foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues(
			//		         includeSourceDatasets: true))
			//	{
			//		if (! datasets.Contains(dataset) && datasetContext.CanOpen(dataset))
			//		{
			//			datasets.Add(dataset);
			//		}
			//	}
			//}

			//return datasets;
		}

		#region Non-public

		private static void LogTests([NotNull] IEnumerable<ITest> tests,
		                             [NotNull] IDictionary<ITest, QualitySpecificationElement>
			                             qualitySpecificationElementsByTest)
		{
			foreach (ITest test in tests)
			{
				QualityCondition qualityCondition =
					qualitySpecificationElementsByTest[test].QualityCondition;

				_msg.DebugFormat("Adding quality condition '{0}' based on test '{1}' with {2}",
				                 qualityCondition.Name,
				                 test.GetType().Name,
				                 GetParametersText(qualityCondition));
			}
		}

		private void LogBeginVerification(
			[NotNull] QualitySpecification qualitySpecification,
			double tileSize,
			[CanBeNull] AreaOfInterest areaOfInterest)
		{
			using (_msg.IncrementIndentation("Begin quality verification"))
			{
				StringBuilder stringBuilder = new StringBuilder(
					$"Starting quality verification using quality specification {qualitySpecification.Name}" +
					$" with verification tile size {tileSize}");

				_msg.InfoFormat("Quality specification: {0}", qualitySpecification.Name);
				_msg.InfoFormat("Verification tile size: {0}", tileSize);

				if (areaOfInterest != null)
				{
					IGeometry testPerimeter = areaOfInterest.Geometry;

					if (testPerimeter.IsEmpty)
					{
						const string testPerimeterIsEmpty = "Test perimeter is empty";
						_msg.Warn(testPerimeterIsEmpty);
						stringBuilder.Append(testPerimeterIsEmpty);
					}
					else
					{
						string envelopeMsg;
						var envelope = testPerimeter as IEnvelope;

						if (envelope == null)
						{
							Assert.ArgumentCondition(testPerimeter is IPolygon,
							                         "Unexpected test perimeter type: {0}; must be polygon or envelope",
							                         testPerimeter.GeometryType);

							envelope = testPerimeter.Envelope;
							envelopeMsg = $"Polygon extent: {envelope.Width} x {envelope.Height}";
						}
						else
						{
							envelopeMsg = $"Extent: {envelope.Width} x {envelope.Height}";
						}

						using (_msg.IncrementIndentation(envelopeMsg))
						{
							_msg.InfoFormat("X-Min: {0}", envelope.XMin);
							_msg.InfoFormat("Y-Min: {0}", envelope.YMin);
							_msg.InfoFormat("X-Max: {0}", envelope.XMax);
							_msg.InfoFormat("Y-Max: {0}", envelope.YMax);
						}

						if (ProgressStreamer != null)
						{
							stringBuilder.AppendLine(envelopeMsg);
							stringBuilder.AppendLine($"  X-Min: {envelope.XMin}");
							stringBuilder.AppendLine($"  Y-Min: {envelope.YMin}");
							stringBuilder.AppendLine($"  X-Max: {envelope.XMax}");
							stringBuilder.AppendLine($"  Y-Max: {envelope.YMax}");
						}
					}

					ProgressStreamer?.Info(stringBuilder.ToString());
				}
			}
		}

		private void LogResults(
			[NotNull] IEnumerable<QualitySpecificationElement> qualitySpecificationElements,
			[NotNull] IssueProcessor issueProcessor,
			int qualityConditionCount, int datasetCount,
			bool fulfilled, bool cancelled,
			[CanBeNull] IExceptionStatistics exceptionStatistics)
		{
			StringBuilder streamMessage = new StringBuilder("Quality verification finished");
			streamMessage.AppendLine();
			streamMessage.AppendLine($"Number of verified datasets: {datasetCount:N0}.");
			streamMessage.AppendLine($"Number of verified conditions: {qualityConditionCount}");

			using (_msg.IncrementIndentation("Quality verification finished"))
			{
				_msg.Info($"Number of verified datasets: {datasetCount:N0}.");

				using (_msg.IncrementIndentation("Number of verified quality conditions: {0:N0}",
				                                 qualityConditionCount))
				{
					LogVerifiedConditions(qualitySpecificationElements, issueProcessor,
					                      exceptionStatistics, streamMessage);
				}

				InfoFormat("Warning count: {0:N0}", streamMessage, issueProcessor.WarningCount);
				InfoFormat("Error count: {0:N0}", streamMessage, issueProcessor.ErrorCount);

				if (issueProcessor.RowsWithStopConditionsCount > 0)
				{
					WarnFormat("Number of features with stop errors: {0:N0}",
					           streamMessage, issueProcessor.RowsWithStopConditionsCount);
				}

				if (exceptionStatistics != null &&
				    exceptionStatistics.TablesWithNonUniqueKeys.Count > 0)
				{
					WarnFormat(
						"Number of tables with non-unique keys referenced by exception objects: {0}",
						streamMessage, exceptionStatistics.TablesWithNonUniqueKeys.Count);
				}

				if (cancelled)
				{
					WarnFormat("The quality verification was cancelled", streamMessage);
				}
				else if (fulfilled)
				{
					InfoFormat("The quality specification is fulfilled", streamMessage);
				}
				else
				{
					WarnFormat("The quality specification is not fulfilled", streamMessage);
				}
			}

			ProgressStreamer?.Info(streamMessage.ToString());
		}

		private static void InfoFormat([StructuredMessageTemplate] string format,
		                               [CanBeNull] StringBuilder fullMessage,
		                               params object[] args)
		{
			string message = string.Format(format, args);

			_msg.Info(message);
			fullMessage?.AppendLine(message);
		}

		private static void WarnFormat([StructuredMessageTemplate] string format,
		                               [CanBeNull] StringBuilder fullMessage,
		                               params object[] args)
		{
			string message = string.Format(format, args);

			_msg.Warn(message);
			fullMessage?.AppendLine(message);
		}

		private static int CompareElements(QualitySpecificationElement e1,
		                                   QualitySpecificationElement e2)
		{
			return string.Compare(e1.QualityCondition.Name,
			                      e2.QualityCondition.Name,
			                      StringComparison.CurrentCulture);
		}

		private static void LogVerifiedConditions(
			[NotNull] IEnumerable<QualitySpecificationElement> qualitySpecificationElements,
			[NotNull] IssueProcessor issueProcessor,
			[CanBeNull] IExceptionStatistics exceptionStatistics,
			[CanBeNull] StringBuilder fullMsg)
		{
			List<QualitySpecificationElement> elementsWithNoCategory;
			Dictionary<DataQualityCategory, List<QualitySpecificationElement>>
				elementsByCategory =
					GetElementsByCategory(qualitySpecificationElements,
					                      out elementsWithNoCategory);

			var categories = new List<DataQualityCategory>(elementsByCategory.Keys);

			categories.Sort(new DataQualityCategoryComparer());

			foreach (DataQualityCategory category in categories)
			{
				string categoryMsg = $"Category '{category.Name}':";
				fullMsg?.AppendLine(categoryMsg);

				using (_msg.IncrementIndentation(categoryMsg))
				{
					List<QualitySpecificationElement> elementsForCategory =
						elementsByCategory[category];

					elementsForCategory.Sort(CompareElements);

					LogElements(elementsForCategory, issueProcessor, exceptionStatistics, fullMsg);
				}
			}

			if (elementsWithNoCategory.Count > 0)
			{
				fullMsg?.AppendLine("No category");

				using (_msg.IncrementIndentation("No category:"))
				{
					elementsWithNoCategory.Sort(CompareElements);

					LogElements(elementsWithNoCategory, issueProcessor, exceptionStatistics,
					            fullMsg);
				}
			}
		}

		private static void LogElements(
			[NotNull] IEnumerable<QualitySpecificationElement> qualitySpecificationElements,
			[NotNull] IssueProcessor issueProcessor,
			[CanBeNull] IExceptionStatistics exceptionStatistics,
			[CanBeNull] StringBuilder fullMessage)
		{
			foreach (QualitySpecificationElement element in qualitySpecificationElements)
			{
				QualityCondition qualityCondition = element.QualityCondition;

				int exceptionCount;
				int issueCount = issueProcessor.GetIssueCount(qualityCondition,
				                                              out exceptionCount);

				var sb = new StringBuilder(qualityCondition.Name);

				if (issueCount > 0)
				{
					sb.AppendFormat(element.AllowErrors
						                ? " - warnings: {0}"
						                : " - errors: {0}",
					                issueCount);
				}

				if (exceptionCount > 0)
				{
					sb.AppendFormat(" - exceptions: {0}", exceptionCount);
				}

				if (issueCount > 0)
				{
					_msg.Warn(sb.ToString());
				}
				else
				{
					_msg.Info(sb.ToString());
				}

				fullMessage?.AppendLine($"  {sb}");

				IQualityConditionExceptionStatistics conditionStatistics =
					exceptionStatistics?.GetQualityConditionStatistics(qualityCondition);

				if (conditionStatistics == null)
				{
					continue;
				}

				if (conditionStatistics.UnknownTableNames.Count == 0)
				{
					continue;
				}

				using (_msg.IncrementIndentation())
				{
					const string ignoreMsg =
						"Exception objects were ignored for this condition due to unknown table names:";

					_msg.Warn(ignoreMsg);
					fullMessage?.AppendLine($"    {ignoreMsg}");

					foreach (string tableName in conditionStatistics.UnknownTableNames)
					{
						string exceptionMsg =
							$"- {tableName}: used in " +
							$"{conditionStatistics.GetExceptionObjectsInvolvingUnknownTableName(tableName).Count} exception object(s)";

						_msg.WarnFormat(exceptionMsg);
						fullMessage?.AppendLine($"    {exceptionMsg}");
					}
				}
			}
		}

		[NotNull]
		private static
			Dictionary<DataQualityCategory, List<QualitySpecificationElement>>
			GetElementsByCategory(
				[NotNull] IEnumerable<QualitySpecificationElement> qualitySpecificationElements,
				[NotNull] out List<QualitySpecificationElement> elementsWithNoCategory)
		{
			elementsWithNoCategory = new List<QualitySpecificationElement>();
			var result =
				new Dictionary<DataQualityCategory, List<QualitySpecificationElement>>();

			foreach (QualitySpecificationElement element in qualitySpecificationElements)
			{
				QualityCondition condition = element.QualityCondition;

				DataQualityCategory category = condition.Category;
				if (category == null)
				{
					elementsWithNoCategory.Add(element);
				}
				else
				{
					List<QualitySpecificationElement> elements;
					if (! result.TryGetValue(category, out elements))
					{
						elements = new List<QualitySpecificationElement>();
						result.Add(category, elements);
					}

					elements.Add(element);
				}
			}

			return result;
		}

		[NotNull]
		private static string GetParametersText([NotNull] QualityCondition qualityCondition)
		{
			var sb = new StringBuilder();

			if (qualityCondition.ParameterValues.Count == 1)
			{
				TestParameterValue parameterValue = qualityCondition.ParameterValues[0];
				sb.AppendFormat("parameter {0}", Format(parameterValue));
			}
			else
			{
				sb.Append("parameters ");
				foreach (TestParameterValue parameterValue in qualityCondition.ParameterValues)
				{
					if (sb.Length > 0)
					{
						sb.Append(", ");
					}

					sb.Append(Format(parameterValue));
				}
			}

			return sb.ToString();
		}

		[NotNull]
		private static string Format([NotNull] TestParameterValue parameterValue)
		{
			return string.Format("{0}={1}",
			                     parameterValue.TestParameterName,
			                     parameterValue.StringValue);
		}

		[NotNull]
		private IList<ITest> CreateTests(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IDatasetContext datasetContext,
			out VerificationElements verificationElements,
			out QualityVerification qualityVerification)
		{
			IOpenDataset datasetOpener = _openDatasetFactory(datasetContext);

			IList<ITest> testList = QualityVerificationUtils.GetTestsAndDictionaries(
				qualitySpecification, datasetOpener,
				out qualityVerification, out IList<QualityCondition> _, out verificationElements,
				ReportPreProcessing);

			return testList;
		}

		private void ReportPreProcessing([NotNull] string message,
		                                 int currentStep = 0,
		                                 int stepCount = 0)
		{
			var args = new VerificationProgressEventArgs(
				           VerificationProgressType.PreProcess, currentStep, stepCount)
			           {
				           Tag = message
			           };

			Progress?.Invoke(this, args);
		}

		#endregion
	}
}
