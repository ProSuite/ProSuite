using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.Xml;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	// TODO sort datasets
	// TODO sort issues
	// TODO rows with stop conditions
	public class XmlVerificationReportBuilder : IVerificationReportBuilder
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private bool _verificationOngoing;

		[NotNull] private readonly HashSet<QualityVerificationDataset> _verifiedDatasets =
			new HashSet<QualityVerificationDataset>();

		[NotNull] private readonly HashSet<QualitySpecificationElement>
			_verifiedQualityConditions =
				new HashSet<QualitySpecificationElement>();

		[NotNull] private readonly Dictionary<Dataset, List<QualitySpecificationElement>>
			_qualitySpecificationElementsByDataset =
				new Dictionary<Dataset, List<QualitySpecificationElement>>();

		[NotNull] private readonly Dictionary<QualitySpecificationElement, IssueStats>
			_issuesByQualityCondition =
				new Dictionary<QualitySpecificationElement, IssueStats>();

		[NotNull] private readonly Dictionary<DdxModel, string>
			_dataSourceDescriptionsByModel = new Dictionary<DdxModel, string>();

		private DateTime _verificationStartTime;
		private DateTime _verificationEndTime;
		private Stopwatch _stopWatch;
		private bool _cancelled;
		[CanBeNull] private AreaOfInterest _areaOfInterest;
		private readonly IssueReportingContexts _issueContexts;
		private readonly int _maxExplicitIssuesPerQualityCondition;
		private readonly VerifiedConditionContexts _verifiedConditionContexts;
		private readonly bool _reportInvolvedTableForSchemaIssues;
		[CanBeNull] private IExceptionStatistics _exceptionStatistics;

		[CanBeNull] private IEnvelope _envelopeTemplate;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlVerificationReportBuilder"/> class.
		/// </summary>
		/// <param name="verifiedConditionContexts"></param>
		/// <param name="reportInvolvedTableForSchemaIssues">if set to <c>true</c>, the involved table 
		/// is reported for schema issues (i.e. issues not involving a row id). Otherwise, these 
		/// involved tables are not reported, making the xml report easier to read.</param>
		/// <param name="issueContexts"></param>
		public XmlVerificationReportBuilder(IssueReportingContexts issueContexts,
		                                    VerifiedConditionContexts verifiedConditionContexts,
		                                    bool reportInvolvedTableForSchemaIssues,
		                                    int maxExplicitIssuesPerQualityCondition = 256)
		{
			_issueContexts = issueContexts;
			_verifiedConditionContexts = verifiedConditionContexts;
			_reportInvolvedTableForSchemaIssues = reportInvolvedTableForSchemaIssues;

			_maxExplicitIssuesPerQualityCondition = maxExplicitIssuesPerQualityCondition;
		}

		#endregion

		#region Implementation of IVerificationReportBuilder

		public void BeginVerification(AreaOfInterest areaOfInterest)
		{
			Assert.False(_verificationOngoing, "verification already begun");

			_verificationOngoing = true;
			_verificationStartTime = DateTime.Now;
			_stopWatch = Stopwatch.StartNew();
			_areaOfInterest = areaOfInterest;

			_issuesByQualityCondition.Clear();
			_qualitySpecificationElementsByDataset.Clear();
			_verifiedDatasets.Clear();
			_dataSourceDescriptionsByModel.Clear();
			_verifiedQualityConditions.Clear();
		}

		public void AddVerifiedDataset(QualityVerificationDataset verificationDataset,
		                               string workspaceDisplayText,
		                               ISpatialReference spatialReference)
		{
			if (spatialReference != null)
			{
				verificationDataset.CoordinateSystem = spatialReference.Name;
				verificationDataset.Tolerance =
					SpatialReferenceUtils.GetXyTolerance(spatialReference);
				verificationDataset.Resolution =
					SpatialReferenceUtils.GetXyResolution(spatialReference);
			}

			_verifiedDatasets.Add(verificationDataset);

			if (! string.IsNullOrEmpty(workspaceDisplayText))
			{
				_dataSourceDescriptionsByModel[verificationDataset.Dataset.Model] =
					workspaceDisplayText;
			}
		}

		public void AddVerifiedQualityCondition(
			QualitySpecificationElement qualitySpecificationElement)
		{
			if (_verifiedQualityConditions.Contains(qualitySpecificationElement))
			{
				return;
			}

			_verifiedQualityConditions.Add(qualitySpecificationElement);

			QualityCondition qualityCondition = qualitySpecificationElement.QualityCondition;

			foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues(
				         excludeReferenceDatasets: true))
			{
				List<QualitySpecificationElement> list;
				if (! _qualitySpecificationElementsByDataset.TryGetValue(dataset, out list))
				{
					list = new List<QualitySpecificationElement>();
					_qualitySpecificationElementsByDataset.Add(dataset, list);
				}

				list.Add(qualitySpecificationElement);
			}
		}

		public void AddIssue(Issue issue, IGeometry errorGeometry)
		{
			AssertVerifying();

			IssueStats issueStats;
			if (! _issuesByQualityCondition.TryGetValue(
				    issue.QualitySpecificationElement,
				    out issueStats))
			{
				issueStats = new IssueStats();
				_issuesByQualityCondition.Add(issue.QualitySpecificationElement,
				                              issueStats);
			}

			issueStats.IssueCount = issueStats.IssueCount + 1;

			if (_issueContexts != IssueReportingContexts.None &&
			    (_maxExplicitIssuesPerQualityCondition <= 0 ||
			     issueStats.IssueCount < _maxExplicitIssuesPerQualityCondition))
			{
				issueStats.AddExplicitIssue(CreateXmlIssue(issue, errorGeometry));
			}
		}

		public void AddRowsWithStopConditions(
			IEnumerable<RowWithStopCondition> rowsWithStopCondition)
		{
			AssertVerifying();
		}

		public void AddExceptionStatistics(IExceptionStatistics statistics)
		{
			_exceptionStatistics = statistics;
		}

		public void EndVerification(bool cancelled)
		{
			AssertVerifying();

			_cancelled = cancelled;
			_verificationEndTime = DateTime.Now;
			_stopWatch.Stop();
			_verificationOngoing = false;
		}

		#endregion

		[NotNull]
		public XmlVerificationReport CreateReport(
			[CanBeNull] string qualitySpecificationName,
			[CanBeNull] IEnumerable<KeyValuePair<string, string>> properties = null)
		{
			Assert.NotNull(_stopWatch, "BeginVerification has not been called");
			Assert.False(_verificationOngoing, "verification not finished");

			// evaluate options

			// where issues are reported:
			bool reportIssuesPerQualityCondition =
				(_issueContexts & IssueReportingContexts.VerifiedQualityCondition) != 0;
			bool reportIssuesPerDataset =
				(_issueContexts & IssueReportingContexts.Dataset) != 0;
			bool reportQualityConditionsWithIssues =
				(_issueContexts & IssueReportingContexts.QualityConditionWithIssues) != 0;

			// where conditions are reported:
			bool reportConditionsInSummary =
				(_verifiedConditionContexts & VerifiedConditionContexts.Summary) != 0;
			bool reportConditionsPerDataset =
				reportIssuesPerDataset ||
				(_verifiedConditionContexts & VerifiedConditionContexts.Dataset) != 0;

			var result = new XmlVerificationReport
			             {
				             QualitySpecification = Escape(qualitySpecificationName),
				             StartTime = _verificationStartTime,
				             EndTime = _verificationEndTime,
				             ProcessingTimeSeconds =
					             (double) _stopWatch.ElapsedMilliseconds / 1000,
				             Version = ReflectionUtils.GetAssemblyVersionString(
					             Assembly.GetExecutingAssembly())
			             };

			if (properties != null)
			{
				result.Properties = properties.Select(p => new XmlNameValuePair
				                                           {
					                                           Name = p.Key,
					                                           Value = p.Value
				                                           })
				                              .ToList();
			}

			if (_areaOfInterest != null)
			{
				if (! _areaOfInterest.IsEmpty)
				{
					result.TestExtent = new Xml2DEnvelope(_areaOfInterest.Extent);
				}

				result.AreaOfInterest = new XmlAreaOfInterest(_areaOfInterest);
			}

			if (reportConditionsInSummary)
			{
				foreach (XmlVerifiedQualityCondition verifiedCondition in
				         GetVerifiedQualityConditions(_verifiedQualityConditions,
				                                      reportIssuesPerQualityCondition,
				                                      reportParameters: true,
				                                      reportDescription: true)
					         .OrderBy(qc => qc.Name))
				{
					result.AddVerifiedCondition(verifiedCondition);
				}
			}

			foreach (QualityVerificationDataset verifiedDataset in _verifiedDatasets.OrderBy(
				         d => d.Dataset.Name))
			{
				bool reportParameters = ! reportConditionsInSummary;
				bool reportDescription = ! reportConditionsInSummary;

				XmlVerifiedDataset xmlVerifiedDataset =
					CreateXmlVerifiedDataset(verifiedDataset,
					                         reportConditionsPerDataset,
					                         reportIssuesPerDataset,
					                         reportParameters,
					                         reportDescription);

				result.AddVerifiedDataset(xmlVerifiedDataset);
			}

			foreach (KeyValuePair<DdxModel, string> dataSource in _dataSourceDescriptionsByModel)
			{
				string modelName = Escape(dataSource.Key.Name);
				result.AddDataSourceDescription(modelName, dataSource.Value);
			}

			if (reportQualityConditionsWithIssues)
			{
				foreach (XmlVerifiedQualityCondition verifiedCondition in
				         GetVerifiedQualityConditions(_verifiedQualityConditions,
				                                      reportIssues: true,
				                                      reportParameters: false,
				                                      reportDescription: true)
					         .OrderBy(qc => qc.Name))
				{
					if (verifiedCondition.IssueCount > 0)
					{
						result.AddConditionWithIssues(verifiedCondition);
					}
				}
			}

			if (_exceptionStatistics != null)
			{
				result.ExceptionCount = _exceptionStatistics.ExceptionCount;

				result.ExceptionStatistics = CreateExceptionStatistics(
					_exceptionStatistics, _verifiedQualityConditions);
			}

			int warningCount;
			int errorCount;
			int stopErrorCount;
			GetIssueCounts(_verifiedQualityConditions,
			               out warningCount, out errorCount, out stopErrorCount);

			result.WarningCount = warningCount;
			result.ErrorCount = errorCount;
			result.StopErrorCount = stopErrorCount;
			result.Cancelled = _cancelled;

			return result;
		}

		#region Non-public

		private void GetIssueCounts(
			[NotNull] IEnumerable<QualitySpecificationElement> qualitySpecificationElements,
			out int warningCount,
			out int errorCount,
			out int stopErrorCount)
		{
			warningCount = 0;
			errorCount = 0;
			stopErrorCount = 0;

			foreach (QualitySpecificationElement element in qualitySpecificationElements)
			{
				IssueStats issueStats;
				if (! _issuesByQualityCondition.TryGetValue(element, out issueStats))
				{
					continue;
				}

				if (element.AllowErrors)
				{
					warningCount += issueStats.IssueCount;
				}
				else
				{
					errorCount += issueStats.IssueCount;

					if (element.StopOnError)
					{
						stopErrorCount += issueStats.IssueCount;
					}
				}
			}
		}

		[NotNull]
		private XmlVerifiedDataset CreateXmlVerifiedDataset(
			[NotNull] QualityVerificationDataset verifiedDataset,
			bool reportVerifiedConditions,
			bool reportIssues,
			bool reportParameters,
			bool reportDescription)
		{
			var dataset = verifiedDataset.Dataset;

			List<QualitySpecificationElement> qualitySpecificationElements;
			if (! _qualitySpecificationElementsByDataset.TryGetValue(
				    dataset, out qualitySpecificationElements))
			{
				qualitySpecificationElements = new List<QualitySpecificationElement>();
			}

			var result = new XmlVerifiedDataset(dataset.Name, Escape(dataset.Model.Name));

			if (reportVerifiedConditions)
			{
				foreach (XmlVerifiedQualityCondition verifiedCondition in
				         GetVerifiedQualityConditions(qualitySpecificationElements,
				                                      reportIssues,
				                                      reportParameters,
				                                      reportDescription))
				{
					result.AddVerifiedCondition(verifiedCondition);
				}
			}

			int warningCount;
			int errorCount;
			int stopErrorCount;
			GetIssueCounts(qualitySpecificationElements,
			               out warningCount,
			               out errorCount,
			               out stopErrorCount);

			result.ErrorCount = errorCount;
			result.WarningCount = warningCount;
			result.StopErrorCount = stopErrorCount;

			result.GeometryType = dataset.GeometryType?.Name;

			result.CoordinateSystem = verifiedDataset.CoordinateSystem;
			result.Tolerance = verifiedDataset.Tolerance;
			result.Resolution = verifiedDataset.Resolution;

			return result;
		}

		[NotNull]
		private IEnumerable<XmlVerifiedQualityCondition> GetVerifiedQualityConditions(
			[NotNull] IEnumerable<QualitySpecificationElement> elements,
			bool reportIssues,
			bool reportParameters,
			bool reportDescription)
		{
			foreach (QualitySpecificationElement element in elements)
			{
				List<XmlIssue> explicitIssues;
				IssueStats stats;
				if (_issuesByQualityCondition.TryGetValue(element, out stats)
				    && stats.ExplicitIssues != null)
				{
					explicitIssues = stats.ExplicitIssues;
				}
				else
				{
					explicitIssues = new List<XmlIssue>();
				}

				yield return CreateVerifiedQualityCondition(
					element, stats?.IssueCount ?? 0, explicitIssues,
					GetQualityConditionExceptionStatistics(element),
					reportIssues, reportParameters, reportDescription);
			}
		}

		[CanBeNull]
		private IQualityConditionExceptionStatistics GetQualityConditionExceptionStatistics(
			[NotNull] QualitySpecificationElement element)
		{
			return _exceptionStatistics?.GetQualityConditionStatistics(
				element.QualityCondition);
		}

		[NotNull]
		private static XmlVerifiedQualityCondition CreateVerifiedQualityCondition(
			[NotNull] QualitySpecificationElement element,
			int issueCount,
			[NotNull] ICollection<XmlIssue> explicitIssues,
			[CanBeNull] IQualityConditionExceptionStatistics exceptionStatistics,
			bool reportIssues,
			bool reportParameters,
			bool reportDescription)
		{
			QualityCondition qualityCondition = element.QualityCondition;

			try
			{
				var result = new XmlVerifiedQualityCondition
				             {
					             Name = Escape(qualityCondition.Name),
					             Guid = qualityCondition.Uuid,
					             VersionGuid = qualityCondition.VersionUuid,
					             Type = element.AllowErrors
						                    ? XmlQualityConditionType.Soft
						                    : XmlQualityConditionType.Hard,
					             StopCondition = element.StopOnError,
					             Category = qualityCondition.Category
				             };

				if (explicitIssues.Count > 0 && reportIssues)
				{
					result.AddIssues(explicitIssues, element.ReportIndividualErrors);
				}

				result.IssueCount = issueCount;

				if (reportParameters)
				{
					result.TestDescriptor = GetTestDescriptor(qualityCondition.TestDescriptor);
					result.AddParameters(GetParameters(qualityCondition));
				}

				if (reportDescription)
				{
					if (StringUtils.IsNotEmpty(qualityCondition.Description))
					{
						result.Description = Escape(qualityCondition.Description);
					}

					if (StringUtils.IsNotEmpty(qualityCondition.Url))
					{
						result.Url = Escape(qualityCondition.Url);
					}
				}

				if (exceptionStatistics != null)
				{
					result.ExceptionCount = exceptionStatistics.ExceptionCount;
				}

				return result;
			}
			catch (Exception e)
			{
				_msg.Debug($"Error creating XML quality condition from {qualityCondition.Name}", e);
				throw;
			}
		}

		[NotNull]
		private static XmlQualityConditionExceptions
			CreateQualityConditionExceptionStatistics(
				[NotNull] IQualityConditionExceptionStatistics statistics)
		{
			var result =
				new XmlQualityConditionExceptions
				{
					QualityConditionName = Escape(statistics.QualityCondition.Name),
					ExceptionCount = statistics.ExceptionCount,
					ExceptionObjectCount = statistics.ExceptionObjectCount,
					UnusedExceptionObjectCount = statistics.UnusedExceptionObjectCount,
					ExceptionObjectsUsedMultipleTimesCount =
						statistics.ExceptionObjectUsedMultipleTimesCount
				};

			foreach (ExceptionObject exceptionObject in statistics.UnusedExceptionObjects)
			{
				result.AddUnusedExceptionObject(
					CreateException(exceptionObject, usageCount: 0));
			}

			foreach (ExceptionUsage usage in statistics.ExceptionObjectsUsedMultipleTimes)
			{
				result.AddExceptionObjectUsedMultipleTimes(
					CreateException(usage.ExceptionObject, usage.UsageCount));
			}

			foreach (string tableName in statistics.UnknownTableNames)
			{
				ICollection<ExceptionObject> ignored = statistics
					.GetExceptionObjectsInvolvingUnknownTableName(
						tableName);

				result.ExceptionObjectsIgnoredDueToUnknownTableNameCount = ignored.Count;
				result.AddUnknownTableName(
					CreateUnknownTableName(tableName, ignored));
			}

			return result;
		}

		[NotNull]
		private static XmlUnknownTableName CreateUnknownTableName(
			[NotNull] string tableName,
			[NotNull] IEnumerable<ExceptionObject> exceptionObjects)
		{
			var result = new XmlUnknownTableName { TableName = tableName };

			foreach (ExceptionObject exceptionObject in exceptionObjects)
			{
				result.AddExceptionObjects(CreateException(exceptionObject, 0));
			}

			return result;
		}

		[NotNull]
		private static XmlExceptionObject CreateException(
			[NotNull] ExceptionObject exceptionObject,
			int usageCount)
		{
			return new
			       XmlExceptionObject
			       {
				       Id = exceptionObject.Id,
				       ShapeType = Escape(ExceptionObjectUtils.GetShapeTypeText(
					                          exceptionObject.ShapeType)),
				       IssueCode = Escape(exceptionObject.IssueCode),
				       InvolvedObjects = Escape(exceptionObject.InvolvedTablesText),
				       UsageCount = usageCount
			       };
		}

		[NotNull]
		private static XmlExceptionStatistics CreateExceptionStatistics(
			[NotNull] IExceptionStatistics statistics,
			[NotNull] IEnumerable<QualitySpecificationElement> verifiedQualityConditions)
		{
			var result =
				new XmlExceptionStatistics
				{
					DataSource = WorkspaceUtils.GetWorkspaceDisplayText(
						statistics.Workspace),
					ExceptionCount = statistics.ExceptionCount,
					ExceptionObjectCount = statistics.ExceptionObjectCount,
					InactiveExceptionObjectCount = statistics.InactiveExceptionObjectCount,
					UnusedExceptionObjectCount = statistics.UnusedExceptionObjectCount,
					ExceptionObjectsUsedMultipleTimesCount =
						statistics.ExceptionObjectsUsedMultipleTimesCount
				};

			foreach (QualitySpecificationElement element in verifiedQualityConditions)
			{
				IQualityConditionExceptionStatistics conditionStatistics =
					statistics.GetQualityConditionStatistics(
						element.QualityCondition);

				if (conditionStatistics != null)
				{
					result.AddQualityConditionExceptions(
						CreateQualityConditionExceptionStatistics(conditionStatistics));
				}
			}

			foreach (ITable table in statistics.TablesWithNonUniqueKeys)
			{
				result.AddTableWithNonUniqueKeys(
					CreateTableWithNonUniqueKeys(table,
					                             statistics.GetNonUniqueKeys(table)));
			}

			return result;
		}

		[NotNull]
		private static XmlTableWithNonUniqueKeys CreateTableWithNonUniqueKeys(
			[NotNull] ITable table,
			[NotNull] IEnumerable<object> nonUniqueKeys)
		{
			var result =
				new XmlTableWithNonUniqueKeys
				{
					TableName = DatasetUtils.GetName(table),
					Workspace = WorkspaceUtils.GetWorkspaceDisplayText
						(DatasetUtils.GetWorkspace(table))
				};

			foreach (object key in nonUniqueKeys)
			{
				result.AddNonUniqueKey(key);
			}

			return result;
		}

		[NotNull]
		private static XmlTestDescriptor GetTestDescriptor(
			[NotNull] TestDescriptor testDescriptor)
		{
			var result = new XmlTestDescriptor
			             {
				             Name = Escape(testDescriptor.Name),
				             Description = Escape(testDescriptor.Description),
				             Assembly = testDescriptor.AssemblyName
			             };

			if (testDescriptor.TestClass != null)
			{
				result.TestClass = testDescriptor.TestClass.TypeName;
				result.TestConstructorIndex =
					testDescriptor.TestConstructorId.ToString(CultureInfo.InvariantCulture);
			}
			else if (testDescriptor.TestFactoryDescriptor != null)
			{
				result.FactoryClass = testDescriptor.TestFactoryDescriptor.TypeName;
			}

			return result;
		}

		[NotNull]
		private static XmlTestDescriptor GetInstanceDescriptor(
			[NotNull] InstanceDescriptor instanceDescriptor)
		{
			var result = new XmlTestDescriptor
			             {
				             Name = Escape(instanceDescriptor.Name),
				             Description = Escape(instanceDescriptor.Description),
				             Assembly = instanceDescriptor.AssemblyName
			             };

			if (instanceDescriptor.Class != null)
			{
				result.TestClass = instanceDescriptor.Class.TypeName;
				result.TestConstructorIndex = $"{instanceDescriptor.ConstructorId}";
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<XmlTestParameterValue> GetParameters(
			[NotNull] InstanceConfiguration instanceConfiguration)
		{
			foreach (TestParameterValue parameterValue in instanceConfiguration.ParameterValues)
			{
				XmlTestParameterValue xmlValue =
					new XmlTestParameterValue
					{
						Name = parameterValue.TestParameterName,
						ValueSource = GetValueSource(parameterValue.ValueSource)
					};

				var datasetParameter = parameterValue as DatasetTestParameterValue;
				if (datasetParameter != null)
				{
					Dataset dataset = datasetParameter.DatasetValue;

					xmlValue.WhereClause = Escape(datasetParameter.FilterExpression);
					xmlValue.UsedAsReferenceData = datasetParameter.UsedAsReferenceData;

					if (parameterValue.ValueSource == null)
					{
						if (dataset == null)
						{
							_msg.DebugFormat(
								"Dataset parameter {0} of condition {1} has no dataset value.",
								datasetParameter.TestParameterName, instanceConfiguration.Name);

							continue;
						}

						Assert.False(dataset.Deleted, "dataset is deleted");

						xmlValue.Dataset = dataset.Name;
					}
				}
				else
				{
					var scalarParameter = parameterValue as ScalarTestParameterValue;
					Assert.NotNull(scalarParameter, "scalarParameter");

					xmlValue.Value = scalarParameter.StringValue;
				}

				yield return xmlValue;
			}
		}

		private static XmlInstanceConfiguration GetValueSource(
			[CanBeNull] TransformerConfiguration transformerConfiguration)
		{
			if (transformerConfiguration == null)
			{
				return null;
			}

			var result =
				new XmlInstanceConfiguration
				{
					Name = Escape(transformerConfiguration.Name)
				};

			result.TestDescriptor =
				GetInstanceDescriptor(transformerConfiguration.InstanceDescriptor);
			result.AddParameters(GetParameters(transformerConfiguration));

			return result;
		}

		private void AssertVerifying()
		{
			Assert.True(_verificationOngoing, "verification not begun");
		}

		[CanBeNull]
		private static string Escape([CanBeNull] string text)
		{
			return XmlUtils.EscapeInvalidCharacters(text);
		}

		[NotNull]
		private XmlIssue CreateXmlIssue([NotNull] Issue issue,
		                                [CanBeNull] IGeometry geometry)
		{
			var result = new XmlIssue
			             {
				             Description = Escape(issue.Description),
				             IssueCode = issue.IssueCode?.ID,
				             AffectedComponent = Escape(issue.AffectedComponent)
			             };

			if (geometry != null && ! geometry.IsEmpty)
			{
				result.Extent = CreateXmlEnvelope(geometry);
			}

			foreach (InvolvedTable involvedTable in issue.InvolvedTables)
			{
				if (involvedTable.RowReferences.Count == 0 &&
				    ! _reportInvolvedTableForSchemaIssues)
				{
					continue;
				}

				result.AddInvolvedTable(new XmlInvolvedTable(involvedTable.TableName,
				                                             involvedTable.KeyField,
				                                             involvedTable.RowReferences));
			}

			return result;
		}

		[NotNull]
		private XmlEnvelope CreateXmlEnvelope([NotNull] IGeometry geometry)
		{
			if (_envelopeTemplate == null)
			{
				_envelopeTemplate = new EnvelopeClass();
			}

			geometry.QueryEnvelope(_envelopeTemplate);

			return new XmlEnvelope(_envelopeTemplate);
		}

		#endregion

		private class IssueStats
		{
			public int IssueCount { get; set; }

			[CanBeNull]
			public List<XmlIssue> ExplicitIssues { get; private set; }

			public void AddExplicitIssue([NotNull] XmlIssue issue)
			{
				Assert.ArgumentNotNull(issue, nameof(issue));

				if (ExplicitIssues == null)
				{
					ExplicitIssues = new List<XmlIssue>();
				}

				ExplicitIssues.Add(issue);
			}
		}
	}
}
