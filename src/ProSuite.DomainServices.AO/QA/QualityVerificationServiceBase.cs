using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Globalization;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone;
using ProSuite.DomainServices.AO.QA.VerificationReports;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	public abstract class QualityVerificationServiceBase
	{
		#region Field declarations

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IDatasetLookup _datasetLookup;

		[CanBeNull] private ILocationBasedQualitySpecification
			_locationBasedQualitySpecification;

		private int _errorCount;
		private int _warningCount;
		private QualityErrorRepositoryBase _verificationContextIssueRepository;
		private IQualityConditionObjectDatasetResolver _datasetResolver;

		private IIssueRepository _externalIssueRepository;
		private IVerificationReportBuilder _verificationReportBuilder;
		private IVerificationContext _verificationContext;
		private IGeometry _testPerimeter;
		protected IVerificationParameters Parameters { get; private set; }
		private IList<IObject> _selection;
		private IObjectSelection _objectSelection;

		// TODO define default based on some other criterion (also valid for lat/long)
		private double _tileSize = 10000;

		#endregion

		#region Constructors

		protected QualityVerificationServiceBase([NotNull] IDatasetLookup datasetLookup)
		{
			_datasetLookup = datasetLookup;
		}

		#endregion

		public void SetParameters([NotNull] IVerificationParameters parameters)
		{
			TileSize = parameters.TileSize;

			UpdateErrorsInVerifiedModelContext =
				parameters.UpdateIssuesInVerifiedModelContext;
			ErrorDeletionInPerimeter = parameters.IssueDeletionInPerimeter;
			KeepUnusedAllowedErrors = ! parameters.DeleteUnusedAllowedErrors;
			KeepInvalidAllowedErrors = ! parameters.DeleteObsoleteAllowedErrors;
			InvalidateAllowedErrorsIfQualityConditionWasUpdated =
				parameters.InvalidateAllowedErrorsIfQualityConditionWasUpdated;
			InvalidateAllowedErrorsIfAnyInvolvedObjectChanged =
				parameters.InvalidateAllowedErrorsIfAnyInvolvedObjectChanged;

			StoreErrorsOutsidePerimeter = parameters.StoreIssuesOutsidePerimeter;

			StoreRelatedGeometryForTableRowErrors =
				parameters.StoreRelatedGeometryForTableRowIssues;
			FilterTableRowsUsingRelatedGeometry =
				parameters.FilterTableRowsUsingRelatedGeometry;
			OverrideAllowedErrors = parameters.OverrideAllowedErrors;
			ForceFullScanForNonContainerTests = parameters.ForceFullScanForNonContainerTests;

			// TODO other params

			Parameters = parameters;
		}

		public double TileSize
		{
			get { return _tileSize; }
			set { _tileSize = value; }
		}

		/// <summary>
		/// The interface that allows sending progress information during a lengthy operation.
		/// </summary>
		[CanBeNull]
		public IVerificationProgressStreamer ProgressStreamer { get; set; }

		public bool ForceFullScanForNonContainerTests { get; set; }

		// [Obsolete("set individual properties, use parameters class")]
		public ErrorCreation ErrorCreation
		{
			set
			{
				UpdateErrorsInVerifiedModelContext = (value & ErrorCreation.Create) != 0;
				StoreErrorsOutsidePerimeter = (value & ErrorCreation.AllowDisjointPerimeter) != 0;
				StoreRelatedGeometryForTableRowErrors =
					(value & ErrorCreation.NoStoreReferenceGeometries) == 0;
				FilterTableRowsUsingRelatedGeometry =
					(value & ErrorCreation.UseReferenceGeometries) != 0;
				OverrideAllowedErrors = (value & ErrorCreation.IgnoreAllowedErrors) == 0;
			}
		}

		// [Obsolete("set individual properties, use parameters class")]
		public ErrorDeletion ErrorDeletion
		{
			set
			{
				KeepUnusedAllowedErrors = (value & ErrorDeletion.KeepUnusedAllowedErrors) != 0;
				KeepInvalidAllowedErrors = (value & ErrorDeletion.KeepChangedAllowedErrors) != 0;

				ErrorDeletionInPerimeter = (value & ErrorDeletion.VerifiedConditions) != 0
					                           ? ErrorDeletionInPerimeter.VerifiedQualityConditions
					                           : ErrorDeletionInPerimeter.AllQualityConditions;
			}
		}

		[NotNull]
		protected abstract QualityErrorRepositoryBase CreateQualityErrorRepository(
			[NotNull] IVerificationContext verificationContext,
			[NotNull] IDictionary<QualityCondition, IList<ITest>> qualityConditionTests,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver);

		[NotNull]
		protected abstract IGdbTransaction CreateGdbTransaction();

		protected void SetTestPerimeter([CanBeNull] AreaOfInterest areaOfInterest,
		                                [NotNull] DdxModel model)
		{
			SetTestPerimeter(areaOfInterest,
			                 model.SpatialReferenceDescriptor.GetSpatialReference());
		}

		protected void SetTestPerimeter([CanBeNull] AreaOfInterest areaOfInterest,
		                                [NotNull] ISpatialReference spatialReference)
		{
			IPolygon perimeter;
			if (areaOfInterest == null || areaOfInterest.IsEmpty)
			{
				perimeter = null;
			}
			else
			{
				perimeter = areaOfInterest.CreatePolygon();

				GeometryUtils.EnsureSpatialReference(perimeter, spatialReference);
			}

			// TODO redirect calls, make setter private
			TestPerimeter = perimeter;
		}

		protected void InitializeTestParameterValuesTx(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IDomainTransactionManager domainTransactions)
		{
			var involvedModels = new HashSet<DdxModel>();

			foreach (var qcon in qualitySpecification.Elements
			                                         .Select(e => e.QualityCondition)
			                                         .Where(qcon => qcon != null))
			{
				// TODO: Limit number of round trips, use QualitySpecificationUtils.InitializeAssociatedEntitiesTx() ?
				foreach (Dataset dataset in qcon
				                            .GetDatasetParameterValues(true, true)
				                            .Where(ds => ds.Model != null))
				{
					ObjectDataset objectDataset = dataset as ObjectDataset;

					if (objectDataset != null)
					{
						// Make sure all datasets are attached and fully loaded
						domainTransactions.Reattach(objectDataset);

						domainTransactions.Initialize(objectDataset.Attributes);
						domainTransactions.Initialize(objectDataset.AssociationEnds);
					}

					involvedModels.Add((DdxModel) dataset.Model);
				}
			}

			foreach (var model in involvedModels.Where(m => m.IsPersistent))
			{
				// TOP-5202 (disconnected session) when a different model is involved 
				domainTransactions.Reattach(model);
				domainTransactions.Initialize(model.Associations);
				domainTransactions.Initialize(model.Datasets);
			}
		}

		protected static void DisableUninvolvedConditions(
			[NotNull] QualitySpecification specification,
			[NotNull] IEnumerable<Dataset> verifiedDatasets)
		{
			var datasets = new HashSet<Dataset>(verifiedDatasets);

			if (datasets.Count == 0)
			{
				return;
			}

			foreach (QualitySpecificationElement element in
			         specification.Elements
			                      .Where(e => e.Enabled))
			{
				QualityCondition condition = element.QualityCondition;

				bool involvesAnyVerifiedDataset =
					condition != null &&
					condition.ParameterValues
					         .OfType<DatasetTestParameterValue>()
					         .Where(pv => pv.DatasetValue != null &&
					                      ! pv.DatasetValue.Deleted &&
					                      ! pv.UsedAsReferenceData)
					         .Any(pv => datasets.Contains(pv.DatasetValue));

				if (! involvesAnyVerifiedDataset)
				{
					element.Enabled = false;
				}
			}
		}

		protected IGeometry TestPerimeter
		{
			get { return _testPerimeter; }
			set
			{
				_testPerimeter = value;
				if (_testPerimeter != null)
				{
					GeometryUtils.AllowIndexing(_testPerimeter);
				}
			}
		}

		protected IVerificationContext VerificationContext
		{
			get { return _verificationContext; }
			set { _verificationContext = value; }
		}

		[PublicAPI]
		protected void SetLocationBasedQualitySpecification(
			[CanBeNull] ILocationBasedQualitySpecification value)
		{
			_locationBasedQualitySpecification = value;
		}

		protected bool AllowEditing { get; set; }

		private bool UpdateErrorsInVerifiedModelContext { get; set; }

		private bool StoreErrorsOutsidePerimeter { get; set; }

		private bool StoreRelatedGeometryForTableRowErrors { get; set; }

		private bool FilterTableRowsUsingRelatedGeometry { get; set; }

		protected bool OverrideAllowedErrors { get; set; }

		private bool KeepUnusedAllowedErrors { get; set; }

		private bool KeepInvalidAllowedErrors { get; set; }

		public bool InvalidateAllowedErrorsIfAnyInvolvedObjectChanged { get; set; }

		public bool InvalidateAllowedErrorsIfQualityConditionWasUpdated { get; set; }

		private ErrorDeletionInPerimeter ErrorDeletionInPerimeter { get; set; }

		#region Selected features

		protected void ClearSelection()
		{
			_selection = null;
		}

		[CanBeNull]
		protected IEnvelope InitSelection([NotNull] ICollection<IObject> origSelection,
		                                  [CanBeNull] IEnvelope minimumExtent,
		                                  [CanBeNull] IGeometry perimeter)
		{
			Assert.ArgumentNotNull(origSelection, nameof(origSelection));
			Assert.ArgumentCondition(origSelection.Count > 0, "no features in selection");

			bool ensureIntersectingPerimeter = perimeter != null;
			_selection = VerificationUtils.GetFilteredObjects(
				origSelection, perimeter, ensureIntersectingPerimeter, out IEnvelope testExtent);

			if (testExtent == null)
			{
				return null;
			}

			if (minimumExtent != null && ! minimumExtent.IsEmpty)
			{
				GeometryUtils.EnsureSpatialReference(minimumExtent, testExtent.SpatialReference);
				testExtent.Union(minimumExtent);
			}

			double tolerance = GeometryUtils.GetXyTolerance(testExtent);

			// enlarge the envelope of the selection by the xy tolerance x 100
			double expandDistance = tolerance * 100;
			const bool asRatio = false;
			testExtent.Expand(expandDistance, expandDistance, asRatio);

			return testExtent;
		}

		[NotNull]
		private ICollection<Dataset> GetDatasetsInvolvedInSelection(
			[NotNull] ICollection<Dataset> candidates)
		{
			if (_selection == null)
			{
				return candidates;
			}

			var datasetsInvolvedInSelection = new HashSet<IObjectDataset>();
			foreach (IObjectDataset objectDataset in
			         VerificationUtils.GetDatasetsByObjectClass(_selection, _datasetLookup)
			                          .Values)
			{
				if (objectDataset != null)
				{
					datasetsInvolvedInSelection.Add(objectDataset);
				}
			}

			var result = new List<Dataset>(candidates.Count);

			foreach (Dataset dataset in candidates)
			{
				var objectDataset = dataset as IObjectDataset;

				if (objectDataset != null)
				{
					if (datasetsInvolvedInSelection.Contains(objectDataset))
					{
						result.Add(dataset);
					}
				}

				// TODO always keep topologies, geometric networks, terrains?
			}

			return result;
		}

		#endregion

		#region Preparing/managing tests

		private IList<QualityCondition> _qualityConditions;
		private VerificationElements _verificationElements;

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual IList<ITest> GetTests(
			[NotNull] QualitySpecification specification,
			[NotNull] out QualityVerification qualityVerification)
		{
			// TODO revise, don't make the OUTER virtual
			return GetTestsCore(specification, out qualityVerification);
		}

		[NotNull]
		protected IList<ITest> GetTestsCore(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] out QualityVerification qualityVerification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			IOpenDataset datasetOpener = CreateDatasetOpener(_verificationContext);

			IList<ITest> testList = QualityVerificationUtils.GetTestsAndDictionaries(
				qualitySpecification, datasetOpener, out qualityVerification,
				out _qualityConditions, out _verificationElements, ReportPreProcessing);

			return testList;
		}

		protected abstract IOpenDataset CreateDatasetOpener(
			[NotNull] IVerificationContext verificationContext);

		#endregion

		#region Verification

		public void Cancel()
		{
			CancellationTokenSource.Cancel();
		}

		/// <summary>
		/// Special verification steps if the workspace is editable.
		/// </summary>
		/// <param name="qualitySpecification">The specification.</param>
		/// <returns></returns>
		[NotNull]
		protected QualityVerification VerifyEditableDatasets(
			[NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			IList<QualitySpecificationElement> newDisabledElements =
				qualitySpecification.DisableNonApplicableElements(GetVerifiedDatasets());

			QualityVerification qualityVerification;

			LogVerificationParameters();

			try
			{
				if (AllowEditing)
				{
					_msg.Debug("Validating topologies");

					ValidateTopologies(qualitySpecification);
				}

				qualityVerification = CultureInfoUtils.ExecuteUsing(
					CultureInfo.InvariantCulture,
					CultureInfo.InvariantCulture,
					() => VerifyQuality(qualitySpecification));
			}
			finally
			{
				foreach (QualitySpecificationElement element in newDisabledElements)
				{
					element.Enabled = true;
				}
			}

			return qualityVerification;
		}

		[NotNull]
		protected QualityVerification VerifyQuality(
			[NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			QualitySpecificationUtils.LogSpecification(qualitySpecification);

			IList<ITest> tests = GetTests(qualitySpecification,
			                              out QualityVerification qualityVerification);

			if (tests.Count == 0)
			{
				// TODO: report warning via progress to client
				_msg.Warn("The provided quality specification contains no quality conditions. " +
				          "The specification is fulfilled but no reports will be generated.");

				// Sets the fulfilled flag to true:
				qualityVerification.CalculateStatistics();
				return qualityVerification;
			}

			_datasetResolver = new QualityConditionObjectDatasetResolver(_verificationContext);

			_objectSelection = _selection == null
				                   ? null
				                   : new ObjectSelection(_selection,
				                                         _datasetLookup,
				                                         _datasetResolver);

			_verificationContextIssueRepository =
				CreateQualityErrorRepository(_verificationContext,
				                             _verificationElements.TestsByCondition,
				                             _datasetResolver);

			_verificationContextIssueRepository.Perimeter = _testPerimeter;

			// TODO perimeter name?
			AreaOfInterest areaOfInterest = _testPerimeter == null
				                                ? null
				                                : new AreaOfInterest(_testPerimeter);

			ITestRunner testRunner = CreateTestRunner(qualityVerification);

			var testAssembler =
				new TestAssembler(t => _verificationElements.GetQualityCondition(t));

			testAssembler.DetectTestsWithRelatedGeometry(VerificationContext, _datasetResolver,
			                                             IsRelevantVectorDataset);

			testRunner.TestAssembler = testAssembler;
			testRunner.QualityVerification = qualityVerification;

			if (UpdateErrorsInVerifiedModelContext)
			{
				var gdbTransaction = CreateGdbTransaction();

				gdbTransaction.Execute(
					_verificationContext.PrimaryWorkspaceContext.Workspace,
					delegate
					{
						DeleteErrors(_objectSelection);

						Verify(tests, qualitySpecification, qualityVerification,
						       areaOfInterest, testRunner);

						if (! CancellationTokenSource.IsCancellationRequested &&
						    ! KeepUnusedAllowedErrors &&
						    ! OverrideAllowedErrors &&
						    ! qualitySpecification.IsCustom)
						{
							_verificationContextIssueRepository.DeleteUnusedAllowedErrors();
						}
					}, "Quality Verification"
				);
			}
			else
			{
				// no need to run within an edit operation
				if (! OverrideAllowedErrors)
				{
					// but still honour invalidated allowed errors (they might be deleted remotely)
					_verificationContextIssueRepository.InvalidateAllowedErrors(
						_qualityConditions,
						InvalidateAllowedErrorsIfAnyInvolvedObjectChanged,
						InvalidateAllowedErrorsIfQualityConditionWasUpdated);
				}

				Verify(tests, qualitySpecification, qualityVerification,
				       areaOfInterest, testRunner);
			}

			QualitySpecificationUtils.LogQualityVerification(qualityVerification);

			GC.Collect();

			return qualityVerification;
		}

		protected void ReportPreProcessing([NotNull] string message,
		                                   int currentStep = 0,
		                                   int stepCount = 0)
		{
			_msg.VerboseDebug(() => $"Preprocessing - {message}");

			var args = new VerificationProgressEventArgs(
				           VerificationProgressType.PreProcess, currentStep, stepCount)
			           {
				           Tag = message
			           };

			OnProgress(args);
		}

		private void DeleteErrors([CanBeNull] IObjectSelection objectSelection)
		{
			ReportPreProcessing("Deleting existing issues in verification perimeter...");

			_verificationContextIssueRepository.DeleteErrors(
				GetQualityConditionsForErrorDeletion(ErrorDeletionInPerimeter),
				objectSelection);

			if (! KeepInvalidAllowedErrors)
			{
				ReportPreProcessing("Deleting obsolete allowed issues...");

				_verificationContextIssueRepository.DeleteInvalidAllowedErrors(
					_qualityConditions,
					InvalidateAllowedErrorsIfAnyInvolvedObjectChanged,
					InvalidateAllowedErrorsIfQualityConditionWasUpdated);
			}
		}

		[CanBeNull]
		private IEnumerable<QualityCondition> GetQualityConditionsForErrorDeletion(
			ErrorDeletionInPerimeter errorDeletionInPerimeter)
		{
			switch (errorDeletionInPerimeter)
			{
				case ErrorDeletionInPerimeter.AllQualityConditions:
					return null;

				case ErrorDeletionInPerimeter.VerifiedQualityConditions:
					return _qualityConditions;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(errorDeletionInPerimeter),
						errorDeletionInPerimeter,
						$@"Invalid value: {errorDeletionInPerimeter}");
			}
		}

		private void Verify([NotNull] IEnumerable<ITest> tests,
		                    [NotNull] QualitySpecification qualitySpecification,
		                    [NotNull] QualityVerification qualityVerification,
		                    [CanBeNull] AreaOfInterest areaOfInterest,
		                    [NotNull] ITestRunner testRunner)
		{
			Assert.ArgumentNotNull(tests, nameof(tests));
			Assert.ArgumentNotNull(qualityVerification, nameof(qualityVerification));

			var verificationReporter =
				new VerificationReporter(Parameters)
				{
					ReportInvolvedTableForSchemaIssues = true,
					ProgressStreamer = ProgressStreamer
				};

			ISubVerificationObserver subVerificationObserver = null;
			if (verificationReporter.CanCreateIssueRepository)
			{
				ISpatialReference spatialReference =
					Parameters.IssueFgdbSpatialReference ??
					GetSpatialReferenceForIssueDatasets();

				_externalIssueRepository = verificationReporter.CreateIssueRepository(
					IssueRepositoryType.FileGdb, spatialReference);

				subVerificationObserver =
					testRunner.AddObserver(verificationReporter, spatialReference);
			}

			_warningCount = 0;
			_errorCount = 0;

			try
			{
				_verificationReportBuilder =
					InitializeReporting(verificationReporter, qualityVerification, areaOfInterest);

				testRunner.QaError += container_QaError;
				testRunner.Progress += TestRunner_Progress;

				testRunner.Execute(tests, areaOfInterest, CancellationTokenSource);

				CancellationMessage = testRunner.CancellationMessage;

				if (UpdateErrorsInVerifiedModelContext)
				{
					_verificationContextIssueRepository.SavePendingErrors();
				}

				_verificationReportBuilder.AddRowsWithStopConditions(
					testRunner.RowsWithStopConditions.GetRowsWithStopConditions());

				_verificationReportBuilder.EndVerification(testRunner.Cancelled);

				XmlVerificationReport verificationReport =
					verificationReporter.WriteReports(qualitySpecification);

				if (_externalIssueRepository != null)
				{
					verificationReporter.CreateIssueRepositoryIndexes(_externalIssueRepository);

					string issueTableName =
						verificationReporter.WriteIssueStatistics(_externalIssueRepository);

					string aoiTableName = verificationReporter.WriteAreaOfInterest(
						_externalIssueRepository, areaOfInterest,
						_verificationContext.SpatialReferenceDescriptor.GetSpatialReference());

					#region Write MXD - TO BE DEPRECATED once AO 10.x support is dropped

					if (verificationReport != null &&
					    StringUtils.IsNotEmpty(Parameters?.MxdDocumentPath))
					{
						// gather the verified object classes
						IList<IObjectClass> verifiedObjectClasses =
							VerificationUtils.GetObjectClasses(
								Assert.NotNull(verificationReporter.DatasetsCollector)
								      .InvolvedDatasets,
								_verificationContext).ToList();

						WriteIssueMapDocument(
							qualitySpecification, _externalIssueRepository,
							issueTableName, verifiedObjectClasses,
							verificationReport, aoiTableName);
					}

					#endregion
				}
			}
			finally
			{
				testRunner.QaError -= container_QaError;
				testRunner.Progress -= TestRunner_Progress;

				_externalIssueRepository?.Dispose();
				_externalIssueRepository = null;

				subVerificationObserver?.Dispose();

				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		private IVerificationReportBuilder InitializeReporting(
			[NotNull] VerificationReporter verificationReporter,
			[NotNull] QualityVerification qualityVerification,
			[CanBeNull] AreaOfInterest areaOfInterest)
		{
			IVerificationReportBuilder reportBuilder = verificationReporter.CreateReportBuilders();

			// TODO indicate if polygon, perimeter name, perimeter type etc.
			reportBuilder.BeginVerification(areaOfInterest);

			foreach (var verificationDataset in qualityVerification.VerificationDatasets)
			{
				Dataset dataset = verificationDataset.Dataset;
				double loadTime = verificationDataset.LoadTime;

				IWorkspaceContext workspaceContext =
					VerificationContext.GetWorkspaceContext(dataset);

				verificationReporter.AddVerifiedDataset(verificationDataset, workspaceContext);
			}

			verificationReporter.AddVerifiedConditions(
				qualityVerification.ConditionVerifications.Select(
					cv => _verificationElements.GetSpecificationElement(cv)));

			return reportBuilder;
		}

		private ISpatialReference GetSpatialReferenceForIssueDatasets()
		{
			QualityErrorRepositoryDatasets errorRepoDatasets =
				_verificationContextIssueRepository.IssueDatasets;

			if (errorRepoDatasets.HasAnyIssueDatasets)
			{
				return errorRepoDatasets.SpatialReference;
			}

			// Take from the model (as in the standalone/XML service)
			ISpatialReference spatialReference =
				_verificationContext.SpatialReferenceDescriptor.GetSpatialReference();

			return spatialReference;
		}

		protected abstract void Compress(IEnumerable<string> compressibleDatasetPaths);

		protected abstract bool WriteIssueMapDocument(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IIssueRepository issueRepository,
			[NotNull] string issueStatisticsTableName,
			[NotNull] IList<IObjectClass> verifiedObjectClasses,
			[NotNull] XmlVerificationReport verificationReport,
			string aoiFeatureClassName);

		protected virtual ITestRunner CreateTestRunner(
			[NotNull] QualityVerification qualityVerification)
		{
			SingleThreadedTestRunner testRunner =
				new SingleThreadedTestRunner(_verificationElements, TileSize)
				{
					AllowEditing = AllowEditing,
					FilterTableRowsUsingRelatedGeometry = FilterTableRowsUsingRelatedGeometry,
					ForceFullScanForNonContainerTests = ForceFullScanForNonContainerTests,
					ObjectSelection = _objectSelection,
					LocationBasedQualitySpecification = _locationBasedQualitySpecification,
					QualityVerification = qualityVerification,
					DatasetLookup = _datasetLookup
				};

			return testRunner;
		}

		private CancellationTokenSource _cancellationTokenSource;

		private CancellationTokenSource CancellationTokenSource =>
			_cancellationTokenSource ??
			(_cancellationTokenSource = new CancellationTokenSource());

		public string CancellationMessage { get; set; }

		private void LogVerificationParameters()
		{
			if (! _msg.IsVerboseDebugEnabled)
			{
				return;
			}

			_msg.Debug("Verification parameters:");

			using (_msg.IncrementIndentation())
			{
				_msg.DebugFormat(
					"Test perimeter: {0}",
					_testPerimeter == null
						? "<null>"
						: GeometryUtils.GetGeometrySize(_testPerimeter)
						               .ToString(CultureInfo.InvariantCulture));

				_msg.DebugFormat("Verification context: {0}", VerificationContext);

				_msg.DebugFormat("Tile size: {0}", TileSize);

				_msg.DebugFormat("Filter table rows using related geometry: {0}",
				                 FilterTableRowsUsingRelatedGeometry);

				_msg.DebugFormat("Force full scan for non-container tests: {0}",
				                 ForceFullScanForNonContainerTests);

				_msg.DebugFormat("Allow editing: {0}", AllowEditing);

				_msg.DebugFormat("Update errors in verified model context: {0}",
				                 UpdateErrorsInVerifiedModelContext);
				_msg.DebugFormat("Error deletion in perimeter: {0}", ErrorDeletionInPerimeter);
				_msg.DebugFormat("Store errors outside perimeter: {0}",
				                 StoreErrorsOutsidePerimeter);

				_msg.DebugFormat("Store related geometry for table row errors: {0}",
				                 StoreRelatedGeometryForTableRowErrors);

				_msg.DebugFormat("Override allowed errors: {0}", OverrideAllowedErrors);
				_msg.DebugFormat("Keep unused allowed errors: {0}", KeepUnusedAllowedErrors);
				_msg.DebugFormat("Keep invalid allowed errors: {0}", KeepInvalidAllowedErrors);
				_msg.DebugFormat("Invalidate allowed errors if any involved object changed: {0}",
				                 InvalidateAllowedErrorsIfAnyInvolvedObjectChanged);
				_msg.DebugFormat("Invalidate allowed errors if quality condition was updated: {0}",
				                 InvalidateAllowedErrorsIfQualityConditionWasUpdated);
			}
		}

		#endregion

		#region Verify by related geometry

		private int _maximumReferenceGeometryPointCount = 20000;
		private IList<InvolvedRow> _lastInvolvedRowsReferenceGeometry;
		private IGeometry _lastReferenceGeometry;
		private SimpleSet<VectorDataset> _verifiedVectorDatasets;

		public ICustomErrorFilter CustomErrorFilter { get; set; }

		// TODO make configurable (project properties)
		public int MaximumReferenceGeometryPointCount
		{
			get { return _maximumReferenceGeometryPointCount; }
			set { _maximumReferenceGeometryPointCount = value; }
		}

		// TODO make configurable (project properties)
		public bool DeriveGeometryOnlyFromVerifiedVectorDatasets { get; set; }

		protected bool IsRelevantVectorDataset([NotNull] VectorDataset vectorDataset)
		{
			if (! DeriveGeometryOnlyFromVerifiedVectorDatasets)
			{
				return true;
			}

			if (_verifiedVectorDatasets == null)
			{
				_verifiedVectorDatasets = GetVerifiedVectorDatasets(
					_verificationContext.GetVerifiedDatasets());
			}

			return _verifiedVectorDatasets.Contains(vectorDataset);
		}

		[NotNull]
		private static SimpleSet<VectorDataset> GetVerifiedVectorDatasets(
			[NotNull] ICollection<Dataset> verifiedDatasets)
		{
			var result = new SimpleSet<VectorDataset>(verifiedDatasets.Count);

			foreach (Dataset dataset in verifiedDatasets)
			{
				var vd = dataset as VectorDataset;
				if (vd != null)
				{
					result.TryAdd(vd);
				}
			}

			return result;
		}

		[CanBeNull]
		protected IGeometry GetReferenceGeometry(
			[NotNull] IList<InvolvedRow> involvedRows,
			[NotNull] QualityCondition qualityCondition)
		{
			if (involvedRows == _lastInvolvedRowsReferenceGeometry)
			{
				return _lastReferenceGeometry;
			}

			_lastInvolvedRowsReferenceGeometry = involvedRows;

			_lastReferenceGeometry = ReferenceGeometryUtils.CreateReferenceGeometry(
				involvedRows, _verificationContext, qualityCondition,
				_maximumReferenceGeometryPointCount,
				_datasetResolver, IsRelevantVectorDataset);

			return _lastReferenceGeometry;
		}

		#endregion

		#region Progress

		public event EventHandler<IssueFoundEventArgs> IssueFound;

		public event EventHandler<VerificationProgressEventArgs> Progress;

		private void TestRunner_Progress(object sender, VerificationProgressEventArgs e)
		{
			OnProgress(e);
		}

		protected void OnProgress([NotNull] VerificationProgressEventArgs args)
		{
			Progress?.Invoke(this, args);
		}

		private void OnIssueFound(
			[NotNull] QualityConditionVerification conditionVerification,
			[NotNull] QaError qaError,
			bool isAllowable)
		{
			if (IssueFound == null)
			{
				return;
			}

			QualitySpecificationElement qSpecElement =
				_verificationElements.GetSpecificationElement(conditionVerification);

			QualityCondition condition = qSpecElement.QualityCondition;

			// This would look a lot more elegant when using Issue and its involved table
			const int involvedObjectsMaxLength = 2000;
			string involvedObjectsString = ErrorObjectUtils.GetInvolvedObjectsString(
				condition, qaError, _verificationElements.GetTests(condition),
				involvedObjectsMaxLength, _datasetResolver);

			IssueFound.Invoke(
				this, new IssueFoundEventArgs(qSpecElement, qaError, isAllowable,
				                              involvedObjectsString));
		}

		#endregion

		#region Error processing

		/// <summary>
		/// Determines whether the error is relevant for its location.
		/// </summary>
		/// <param name="errorGeometry">The error geometry.</param>
		/// <param name="qualityCondition">The quality condition.</param>
		/// <param name="involvedRows">The involved rows.</param>
		/// <returns>
		///   <c>true</c> if [is error relevant for location] [the specified error geometry]; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>Must be called within dom tx!</remarks>
		protected bool IsErrorRelevantForLocation(
			[NotNull] IGeometry errorGeometry,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] ICollection<InvolvedRow> involvedRows)
		{
			return _locationBasedQualitySpecification == null ||
			       _locationBasedQualitySpecification.IsErrorRelevant(errorGeometry,
				       qualityCondition,
				       involvedRows);
		}

		private void container_QaError(object sender, QaErrorEventArgs e)
		{
			bool report = ProcessQaError(e.QaError);

			e.Cancel = ! report;
		}

		private bool ProcessQaError([NotNull] QaError qaError)
		{
			Assert.ArgumentNotNull(qaError, nameof(qaError));

			SetErrorGeometryInModelSpatialRef(qaError);

			ITest test = qaError.Test;
			QualityConditionVerification conditionVerification =
				_verificationElements.GetQualityConditionVerification(test);

			QualityCondition qualityCondition = conditionVerification.QualityCondition;
			Assert.NotNull(qualityCondition, "no quality condition for verification");

			try
			{
				// Consider extracting separate ErrorFilter class / interface
				if (! IsErrorRelevant(qaError, qualityCondition,
				                      conditionVerification.AllowErrors))
				{
					_msg.VerboseDebug(
						() => "Issue is not relevant for current verification context");

					return false;
				}
			}
			catch (Exception e)
			{
				_msg.Warn($"Error checking error for relevance: {qaError}", e);
				throw;
			}

			conditionVerification.AddError();

			bool isAllowable = conditionVerification.AllowErrors;

			if (isAllowable)
			{
				_warningCount++;
			}
			else
			{
				_errorCount++;
			}

			// Once the error filter is applied inside the test runner, this can be 
			// moved to the test runner too:
			if (qaError.Geometry == null &&
			    StoreRelatedGeometryForTableRowErrors &&
			    ! qualityCondition.NeverStoreRelatedGeometryForTableRowIssues)
			{
				// set reference geometry
				IGeometry referenceGeometry = GetReferenceGeometry(qaError.InvolvedRows,
					qualityCondition);

				if (referenceGeometry != null)
				{
					qaError = ReferenceGeometryUtils.CreateReferenceGeometryError(qaError,
						referenceGeometry);
				}
			}

			// report the error count that was changed (soft or hard)
			int changedErrorCount = isAllowable
				                        ? _warningCount
				                        : _errorCount;

			if (UpdateErrorsInVerifiedModelContext)
			{
				_verificationContextIssueRepository.AddError(qaError, qualityCondition,
				                                             isAllowable);
			}

			if (_externalIssueRepository != null || _verificationReportBuilder != null)
			{
				var issue = new Issue(
					_verificationElements.GetSpecificationElement(conditionVerification), qaError);

				_externalIssueRepository?.AddIssue(issue, qaError.Geometry);
				_verificationReportBuilder?.AddIssue(issue, qaError.Geometry);
			}

			OnIssueFound(conditionVerification, qaError, isAllowable);

			OnProgress(new VerificationProgressEventArgs(VerificationProgressType.Error,
			                                             changedErrorCount,
			                                             qaError.Geometry,
			                                             isAllowable));

			return true;
		}

		private void SetErrorGeometryInModelSpatialRef(QaError qaError)
		{
			IGeometry errorGeometry = qaError.Geometry;

			ISpatialReference contextSpatialReference = GetSpatialReferenceForIssueDatasets();

			if (errorGeometry != null && contextSpatialReference != null &&
			    GeometryUtils.EnsureSpatialReference(errorGeometry, contextSpatialReference, false,
			                                         out IGeometry projected))
			{
				_msg.DebugFormat(
					"Error spatial reference different from model context. Projected!");
				qaError.SetGeometryInModelSpatialReference(projected);
			}
		}

		private bool HasAnyRowsToBeTested(
			[NotNull] IEnumerable<InvolvedRow> involvedRows,
			[NotNull] QualityCondition qualityCondition)
		{
			if (_objectSelection == null)
			{
				// no selection --> none excluded
				return true;
			}

			return involvedRows.Any(
				involvedRow => _objectSelection.Contains(
					involvedRow, qualityCondition));
		}

		/// <summary>
		/// Verify that an error found by the tests belongs to current context of
		/// test perimeter and other specifications.
		/// </summary>
		private bool IsErrorRelevant([NotNull] QaError qaError,
		                             [NotNull] QualityCondition qualityCondition,
		                             bool isSoftCondition)
		{
			if (! HasAnyRowsToBeTested(qaError.InvolvedRows, qualityCondition))
			{
				AllowedError allowedError = _verificationContextIssueRepository.FindAllowedError(
					qaError, qualityCondition);

				if (allowedError != null && ! allowedError.Invalidated)
				{
					// flag the cached allowed error as "used" by a reported error
					allowedError.IsUsed = true;
				}

				// not in selection
				return false;
			}

			var relation = new ErrorPerimeterRelation(qaError, qualityCondition,
			                                          IsErrorGeometryOutsidePerimeter);

			if (! StoreErrorsOutsidePerimeter && relation.ErrorIsOutsidePerimeter)
			{
				// filtered out due to error location outside perimeter
				return false;

				// TODO: area specification check should probably be done regardless of StoreErrorsOutsidePerimeter!
			}

			// TODO: revise for errors without geometry - when to start using reference geometry?
			if (isSoftCondition && ! OverrideAllowedErrors)
			{
				// check for allowed errors
				if (! relation.ErrorIsOutsidePerimeter)
				{
					// the error intersects the perimeter -> search in the cached allowed errors
					AllowedError allowedError =
						_verificationContextIssueRepository.FindAllowedError(qaError,
							qualityCondition);

					// Traditionally invalidated allowed errors have been deleted before the
					// verification start. However, in background verification no updates can be
					// made and therefore the invalidated allowed errors are kept in the list and
					// must be excluded here.
					if (allowedError != null && ! allowedError.Invalidated)
					{
						// flag the cached allowed error as "used" by a reported error
						allowedError.IsUsed = true;

						// filtered out by allowed error
						return false;
					}
				}
				else
				{
					// the error is outside the perimeter -> search in *all* allowed errors
					if (_verificationContextIssueRepository.IsAllowedError(
						    qaError, qualityCondition))
					{
						// filtered out by allowed error
						return false;
					}
				}
			}

			// error is relevant
			if (CustomErrorFilter == null)
			{
				return true;
			}

			if (CustomErrorFilter.IsRelevantDueToErrorGeometry(qaError.Geometry, TestPerimeter))
			{
				return true;
			}

			IGeometry referenceGeometry = GetReferenceGeometry(qaError.InvolvedRows,
			                                                   qualityCondition);

			return CustomErrorFilter.IsRelevantDueToReferenceGeometry(
				qaError, qualityCondition, _datasetResolver, referenceGeometry, TestPerimeter,
				VerificationContext, _locationBasedQualitySpecification);
		}

		private bool IsErrorGeometryOutsidePerimeter(
			[NotNull] QaError qaError,
			[NotNull] QualityCondition qualityCondition)
		{
			IGeometry errorGeometry = qaError.GetGeometryInModelSpatialRef();

			if (errorGeometry == null || errorGeometry.IsEmpty)
			{
				return false;
			}

			GeometryUtils.AllowIndexing(errorGeometry);

			if (! IsErrorRelevantForLocation(errorGeometry,
			                                 qualityCondition,
			                                 qaError.InvolvedRows))
			{
				return true;
			}

			if (_testPerimeter == null)
			{
				return false;
			}

			try
			{
				return IsDisjointFromTestPerimeter(errorGeometry);
			}
			catch (Exception e)
			{
				_msg.ErrorFormat(
					"Error in disjoint check for issue geometry ({0}). Writing issue details to log...",
					e.Message);

				foreach (InvolvedRow row in qaError.InvolvedRows)
				{
					_msg.DebugFormat("Involved row: {0} table: {1}", row.OID, row.TableName);
				}

				_msg.DebugFormat("test perimeter: {0}", GeometryUtils.ToString(_testPerimeter));
				_msg.DebugFormat("error geometry: {0}", GeometryUtils.ToString(errorGeometry));

				throw;
			}
		}

		private bool IsDisjointFromTestPerimeter([NotNull] IGeometry errorGeometry)
		{
			// Note: disjoint fails when testing small envelope that is fully within a huge multi-part polygon

			var testPolygon = _testPerimeter as IPolygon;
			if (testPolygon == null)
			{
				IEnvelope envelope = _testPerimeter as IEnvelope ?? _testPerimeter.Envelope;

				testPolygon = GeometryFactory.CreatePolygon(envelope);
			}

			GeometryUtils.AllowIndexing(testPolygon);

			return ((IRelationalOperator) testPolygon).Disjoint(errorGeometry);
		}

		#endregion

		#region Topology validation

		protected abstract void ValidateTopologies(
			[NotNull] QualitySpecification qualitySpecification);

		#endregion

		/// <summary>
		/// Gets the list of verifiable datasets in the current context.
		/// </summary>
		/// <returns></returns>
		/// <remarks>Expects to be called in a domain transaction</remarks>
		[NotNull]
		private ICollection<Dataset> GetVerifiedDatasets()
		{
			ICollection<Dataset> verifiedDatasets = VerificationContext.GetVerifiedDatasets();

			return GetDatasetsInvolvedInSelection(verifiedDatasets);
		}

		private class ErrorPerimeterRelation
		{
			private readonly QaError _qaError;
			private readonly QualityCondition _qualityCondition;
			private readonly Func<QaError, QualityCondition, bool> _evalFunction;
			private bool? _isOutsidePerimeter;

			public ErrorPerimeterRelation(
				[NotNull] QaError qaError,
				[NotNull] QualityCondition qualityCondition,
				[NotNull] Func<QaError, QualityCondition, bool> evalFunction)
			{
				_qaError = qaError;
				_qualityCondition = qualityCondition;
				_evalFunction = evalFunction;
			}

			public bool ErrorIsOutsidePerimeter
			{
				get
				{
					if (! _isOutsidePerimeter.HasValue)
					{
						_isOutsidePerimeter = _evalFunction(_qaError, _qualityCondition);
					}

					return _isOutsidePerimeter.Value;
				}
			}
		}
	}
}
