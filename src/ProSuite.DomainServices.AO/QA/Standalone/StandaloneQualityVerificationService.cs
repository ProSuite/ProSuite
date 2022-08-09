using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

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

		public event EventHandler<IssueFoundEventArgs> IssueFound;

		/// <summary>
		/// Verifies the specified object classes.
		/// </summary>
		/// <param name="qualitySpecification">The quality specification to verify.</param>
		/// <param name="datasetContext">The model context.</param>
		/// <param name="datasetResolver">The resolver for getting the object dataset based on a table name, in the context of a quality condition</param>
		/// <param name="issueRepository">The issue repository.</param>
		/// <param name="exceptionObjectRepository">The exception object repository</param>
		/// <param name="tileSize">Tile size for the quality verification.</param>
		/// <param name="getKeyFieldName">Function for getting the key field name for an object dataset</param>
		/// <param name="areaOfInterest">The area of interest for the verification (optional).</param>
		/// <param name="trackCancel">The cancel tracker.</param>
		/// <returns></returns>
		public bool Verify(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[CanBeNull] IIssueRepository issueRepository,
			[CanBeNull] IExceptionObjectRepository exceptionObjectRepository,
			double tileSize,
			[CanBeNull] Func<IObjectDataset, string> getKeyFieldName,
			[CanBeNull] AreaOfInterest areaOfInterest,
			[CanBeNull] ITrackCancel trackCancel)
		{
			return Verify(qualitySpecification, datasetContext, datasetResolver,
			              issueRepository, exceptionObjectRepository, tileSize,
			              getKeyFieldName, areaOfInterest, trackCancel,
			              out int _, out int _, out int _);
		}

		/// <summary>
		/// Verifies the specified object classes.
		/// </summary>
		/// <param name="qualitySpecification">The quality specification to verify.</param>
		/// <param name="datasetContext">The model context.</param>
		/// <param name="datasetResolver">The resolver for getting the object dataset based on a table name, in the context of a quality condition</param>
		/// <param name="issueRepository">The issue repository.</param>
		/// <param name="exceptionObjectRepository">The exception object repository</param>
		/// <param name="tileSize">Tile size for the quality verification.</param>
		/// <param name="getKeyFieldName">Function for getting the key field name for an object dataset</param>
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
		                   [CanBeNull] IExceptionObjectRepository exceptionObjectRepository,
		                   double tileSize,
		                   [CanBeNull] Func<IObjectDataset, string> getKeyFieldName,
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

			IEnumerable<QualitySpecificationElement> elements =
				GetOrderedElements(qualitySpecification).ToList();

			IDictionary<ITest, QualitySpecificationElement> elementsByTest;
			IEnumerable<ITest> tests =
				CreateTests(elements, datasetContext, out elementsByTest).ToList();

			var qualityConditionCount = 0;
			foreach (QualitySpecificationElement element in elements)
			{
				qualityConditionCount++;
				_verificationReportBuilder.AddVerifiedQualityCondition(element);
			}

			var datasetCount = 0;
			foreach (Dataset dataset in
			         GetVerifiedDatasets(qualitySpecification, datasetContext))
			{
				datasetCount++;
				_verificationReportBuilder.AddVerifiedDataset(dataset);
			}

			Stopwatch watch = _msg.DebugStartTiming();

			LogTests(tests, elementsByTest);

			TestContainer testContainer = CreateTestContainer(tests, tileSize);

			LogBeginVerification(qualitySpecification, tileSize, areaOfInterest);

			IssueProcessor issueProcessor;
			ProgressProcessor progressProcessor;
			using (var issueWriter = new BufferedIssueWriter(_verificationReportBuilder,
			                                                 datasetContext, datasetResolver,
			                                                 issueRepository,
			                                                 getKeyFieldName))
			{
				issueProcessor = CreateIssueProcessor(testContainer, issueWriter,
				                                      areaOfInterest, exceptionObjectRepository,
				                                      elementsByTest);

				progressProcessor = new ProgressProcessor(testContainer, elementsByTest,
				                                          trackCancel);

				testContainer.QaError += (sender, args) => issueProcessor.Process(args);
				issueProcessor.IssueFound += (sender, args) => IssueFound?.Invoke(this, args);

				testContainer.TestingRow +=
					delegate(object o, RowEventArgs args)
					{
						if (issueProcessor.HasStopCondition(args.Row))
						{
							args.Cancel = true;
						}
					};

				testContainer.ProgressChanged +=
					(sender, args) => progressProcessor.Process(args);

				// run the tests
				TestExecutionUtils.Execute(testContainer, areaOfInterest);
			}

			_verificationReportBuilder.AddRowsWithStopConditions(
				issueProcessor.GetRowsWithStopConditions());

			if (exceptionObjectRepository != null)
			{
				_verificationReportBuilder.AddExceptionStatistics(
					exceptionObjectRepository.ExceptionStatistics);
			}

			_verificationReportBuilder.EndVerification(progressProcessor.Cancelled);

			_msg.DebugStopTiming(watch, "Verification");

			errorCount = issueProcessor.ErrorCount;
			warningCount = issueProcessor.WarningCount;
			rowCountWithStopConditions = issueProcessor.RowsWithStopConditionsCount;

			bool fulfilled = errorCount == 0 && ! progressProcessor.Cancelled;

			LogResults(elements, issueProcessor,
			           qualityConditionCount, datasetCount,
			           fulfilled, progressProcessor.Cancelled,
			           exceptionObjectRepository?.ExceptionStatistics);

			return fulfilled;
		}

		[CanBeNull]
		private static IGeometry GetTestPerimeter([CanBeNull] AreaOfInterest areaOfInterest,
		                                          [NotNull] TestContainer testContainer)
		{
			if (areaOfInterest == null)
			{
				return null;
			}

			IGeometry result;
			GeometryUtils.EnsureSpatialReference(areaOfInterest.Geometry,
			                                     testContainer.GetSpatialReference(), false,
			                                     out result);
			return result;
		}

		[NotNull]
		private static IssueProcessor CreateIssueProcessor(
			[NotNull] TestContainer testContainer,
			[NotNull] IIssueWriter issueWriter,
			[CanBeNull] AreaOfInterest areaOfInterest,
			[CanBeNull] IExceptionObjectRepository exceptionObjectRepository,
			[NotNull] IDictionary<ITest, QualitySpecificationElement> elementsByTest)
		{
			IGeometry testPerimeter = GetTestPerimeter(areaOfInterest, testContainer);

			return new IssueProcessor(
				issueWriter,
				elementsByTest,
				testPerimeter,
				exceptionObjectRepository?.ExceptionObjectEvaluator);
		}

		[NotNull]
		private static IEnumerable<Dataset> GetVerifiedDatasets(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IDatasetContext datasetContext)
		{
			var datasets = new SimpleSet<Dataset>();

			foreach (
				QualitySpecificationElement qualitySpecificationElement in
				qualitySpecification.Elements)
			{
				QualityCondition qualityCondition = qualitySpecificationElement.QualityCondition;

				if (qualityCondition == null)
				{
					continue;
				}

				foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues())
				{
					if (! datasets.Contains(dataset) && datasetContext.CanOpen(dataset))
					{
						datasets.Add(dataset);
					}
				}
			}

			return datasets;
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

		private static void LogBeginVerification(
			[NotNull] QualitySpecification qualitySpecification,
			double tileSize,
			[CanBeNull] AreaOfInterest areaOfInterest)
		{
			using (_msg.IncrementIndentation("Begin quality verification"))
			{
				_msg.InfoFormat("Quality specification: {0}", qualitySpecification.Name);
				_msg.InfoFormat("Verification tile size: {0}", tileSize);

				if (areaOfInterest != null)
				{
					IGeometry testPerimeter = areaOfInterest.Geometry;

					if (testPerimeter.IsEmpty)
					{
						_msg.Warn("Test perimeter is empty");
					}
					else
					{
						var envelope = testPerimeter as IEnvelope;

						string message;
						if (envelope == null)
						{
							Assert.ArgumentCondition(testPerimeter is IPolygon,
							                         "Unexpected test perimeter type: {0}; must be polygon or envelope",
							                         testPerimeter.GeometryType);

							envelope = testPerimeter.Envelope;
							message = string.Format("Polygon extent: {0} x {1}",
							                        envelope.Width, envelope.Height);
						}
						else
						{
							message = string.Format("Extent: {0} x {1}",
							                        envelope.Width, envelope.Height);
						}

						using (_msg.IncrementIndentation(message))
						{
							_msg.InfoFormat("X-Min: {0}", envelope.XMin);
							_msg.InfoFormat("Y-Min: {0}", envelope.YMin);
							_msg.InfoFormat("X-Max: {0}", envelope.XMax);
							_msg.InfoFormat("Y-Max: {0}", envelope.YMax);
						}
					}
				}
			}
		}

		private static void LogResults(
			[NotNull] IEnumerable<QualitySpecificationElement> qualitySpecificationElements,
			[NotNull] IssueProcessor issueProcessor,
			int qualityConditionCount, int datasetCount,
			bool fulfilled, bool cancelled,
			[CanBeNull] IExceptionStatistics exceptionStatistics)
		{
			using (_msg.IncrementIndentation("Quality verification finished"))
			{
				_msg.InfoFormat("Number of verified datasets: {0:N0}", datasetCount);
				using (_msg.IncrementIndentation("Number of verified quality conditions: {0:N0}",
				                                 qualityConditionCount))
				{
					LogVerifiedConditions(qualitySpecificationElements,
					                      issueProcessor,
					                      exceptionStatistics);
				}

				_msg.InfoFormat("Warning count: {0:N0}", issueProcessor.WarningCount);
				_msg.InfoFormat("Error count: {0:N0}", issueProcessor.ErrorCount);

				if (issueProcessor.RowsWithStopConditionsCount > 0)
				{
					_msg.WarnFormat("Number of features with stop errors: {0:N0}",
					                issueProcessor.RowsWithStopConditionsCount);
				}

				if (exceptionStatistics != null &&
				    exceptionStatistics.TablesWithNonUniqueKeys.Count > 0)
				{
					_msg.WarnFormat(
						"Number of tables with non-unique keys referenced by exception objects: {0}",
						exceptionStatistics.TablesWithNonUniqueKeys.Count);
				}

				if (cancelled)
				{
					_msg.Warn("The quality verification was cancelled");
				}
				else if (fulfilled)
				{
					_msg.Info("The quality specification is fulfilled");
				}
				else
				{
					_msg.Warn("The quality specification is not fulfilled");
				}
			}
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
			[CanBeNull] IExceptionStatistics exceptionStatistics)
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
				using (_msg.IncrementIndentation("Category '{0}':", category.Name))
				{
					List<QualitySpecificationElement> elementsForCategory =
						elementsByCategory[category];

					elementsForCategory.Sort(CompareElements);

					LogElements(elementsForCategory, issueProcessor, exceptionStatistics);
				}
			}

			if (elementsWithNoCategory.Count > 0)
			{
				using (_msg.IncrementIndentation("No category:"))
				{
					elementsWithNoCategory.Sort(CompareElements);

					LogElements(elementsWithNoCategory, issueProcessor, exceptionStatistics);
				}
			}
		}

		private static void LogElements(
			[NotNull] IEnumerable<QualitySpecificationElement> qualitySpecificationElements,
			[NotNull] IssueProcessor issueProcessor,
			[CanBeNull] IExceptionStatistics exceptionStatistics)
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

				if (exceptionStatistics != null)
				{
					IQualityConditionExceptionStatistics conditionStatistics =
						exceptionStatistics.GetQualityConditionStatistics(qualityCondition);

					if (conditionStatistics != null)
					{
						if (conditionStatistics.UnknownTableNames.Count > 0)
						{
							using (_msg.IncrementIndentation())
							{
								_msg.Warn(
									"Exception objects were ignored for this condition due to unknown table names:");
								foreach (string tableName in conditionStatistics.UnknownTableNames)
								{
									_msg.WarnFormat(
										"- {0}: used in {1} exception object(s)",
										tableName,
										conditionStatistics
											.GetExceptionObjectsInvolvingUnknownTableName(
												tableName).Count);
								}
							}
						}
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
		private static TestContainer CreateTestContainer([NotNull] IEnumerable<ITest> tests,
		                                                 double tileSize)
		{
			var result = new TestContainer
			             {
				             AllowEditing = false,
				             TileSize = tileSize
			             };

			foreach (ITest test in tests)
			{
				result.AddTest(test);
			}

			return result;
		}

		[NotNull]
		private IEnumerable<ITest> CreateTests(
			[NotNull] IEnumerable<QualitySpecificationElement> qualitySpecificationElements,
			[CanBeNull] IDatasetContext datasetContext,
			out IDictionary<ITest, QualitySpecificationElement> qualityConditionsByTest)
		{
			var result = new List<ITest>();
			qualityConditionsByTest = new Dictionary<ITest, QualitySpecificationElement>();

			if (datasetContext == null)
			{
				return result;
			}

			IOpenDataset datasetOpener = _openDatasetFactory(datasetContext);
			foreach (QualitySpecificationElement element in qualitySpecificationElements)
			{
				QualityCondition condition = element.QualityCondition;

				TestFactory factory = Assert.NotNull(
					TestFactoryUtils.CreateTestFactory(condition),
					$"Cannot create test factory for condition {condition.Name}");

				foreach (ITest test in factory.CreateTests(datasetOpener))
				{
					result.Add(test);
					qualityConditionsByTest.Add(test, element);
				}
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<QualitySpecificationElement> GetOrderedElements(
			[NotNull] QualitySpecification qualitySpecification)
		{
			var list = new List<OrderedQualitySpecificationElement>();

			var listIndex = 0;
			foreach (QualitySpecificationElement element in
			         qualitySpecification.Elements)
			{
				if (! element.Enabled)
				{
					continue;
				}

				list.Add(new OrderedQualitySpecificationElement(
					         element,
					         listIndex));
				listIndex++;
			}

			list.Sort();

			return list.Select(ordered => ordered.QualitySpecificationElement);
		}

		#endregion
	}
}
