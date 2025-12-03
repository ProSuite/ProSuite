using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.IssuePersistence
{
	public abstract class QualityErrorRepositoryBase
	{
		#region Fields

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IVerificationContext _verificationContext;

		private readonly IDictionary<QualityCondition, IList<ITest>> _testsByQualityCondition;

		private List<AllowedError> _allowedErrors;
		private IGeometry _perimeter;
		private readonly string _userName;
		private readonly bool _isPre10Geodatabase;

		private readonly IQualityConditionObjectDatasetResolver _datasetResolver;
		private readonly IQualityConditionRepository _qualityConditionRepository;

		private readonly List<QaError> _errorQueue = new List<QaError>();

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityErrorRepositoryBase"/> class.
		/// </summary>
		/// <param name="verificationContext">The model context.</param>
		/// <param name="testsByQualityCondition">The dictionary of tests by quality condition.
		/// It can be empty in case the <see cref="StoreError"/> method is never used and the
		/// <see cref="VerifiedQualityConditions"/> property is set.</param>
		/// <param name="datasetResolver">The dataset resolver.</param>
		/// <param name="qualityConditionRepository">The quality condition repository.</param>
		protected QualityErrorRepositoryBase(
			[NotNull] IVerificationContext verificationContext,
			[NotNull] IDictionary<QualityCondition, IList<ITest>> testsByQualityCondition,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[NotNull] IQualityConditionRepository qualityConditionRepository)
		{
			Assert.ArgumentNotNull(verificationContext, nameof(verificationContext));
			Assert.ArgumentNotNull(testsByQualityCondition, nameof(testsByQualityCondition));
			Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));
			Assert.ArgumentNotNull(qualityConditionRepository,
			                       nameof(qualityConditionRepository));

			_verificationContext = verificationContext;
			_testsByQualityCondition = testsByQualityCondition;

			IssueDatasets = new QualityErrorRepositoryDatasets(_verificationContext);

			VerifiedQualityConditions = testsByQualityCondition.Keys;

			_userName = EnvironmentUtils.UserDisplayName;

			_datasetResolver = datasetResolver;
			_qualityConditionRepository = qualityConditionRepository;

			int gdbMajorRelease = 0;

			try
			{
				gdbMajorRelease =
					((IGeodatabaseRelease)
						verificationContext.PrimaryWorkspaceContext.Workspace).MajorVersion + 7;
			}
			catch (NotImplementedException e)
			{
				_msg.Debug("GDB release version is not implemented. Assuming non-GDB/Pre-10.x.", e);
			}

			_isPre10Geodatabase = gdbMajorRelease < 10;
		}

		#endregion

		/// <summary>
		/// The list of verified conditions used to get the allowed errors list. In
		/// case the constructor parameter testsByQualityCondition has been provided
		/// it is already initialized.
		/// </summary>
		public ICollection<QualityCondition> VerifiedQualityConditions { get; set; }

		/// <summary>
		/// The test perimeter. It is important to set this property if applicable
		/// to make sure only the errors in the test perimeter are deleted.
		/// </summary>
		public IGeometry Perimeter
		{
			get { return _perimeter; }
			set
			{
				_perimeter = value;
				_allowedErrors = null;
			}
		}

		public QualityErrorRepositoryDatasets IssueDatasets { get; }

		[NotNull]
		protected string UserName => _userName;

		[NotNull]
		protected static Dictionary<QualityCondition, IList<ITest>> GetEmptyConditionList()
		{
			return new Dictionary<QualityCondition, IList<ITest>>();
		}

		#region Model information

		[NotNull]
		protected string GetFieldName([NotNull] AttributeRole attributeRole)
		{
			return IssueDatasets.GetFieldName(attributeRole);
		}

		[NotNull]
		private string FieldNameErrorType => IssueDatasets.FieldNameErrorType;

		[NotNull]
		private string FieldNameDescription => IssueDatasets.FieldNameDescription;

		[NotNull]
		private string FieldNameQualityConditionId => IssueDatasets.FieldNameQualityConditionId;

		#endregion

		#region Changing issue type

		/// <summary>
		/// Sets the error type (Soft/Hard/Allowed) for teh specified error object
		/// within the specified transaction.
		/// </summary>
		/// <param name="transaction"></param>
		/// <param name="issueObject"></param>
		/// <param name="issueType"></param>
		[Obsolete("use ChangeIssueType()")]
		public void SetErrorType([NotNull] IGdbTransaction transaction,
		                         [NotNull] IObject issueObject,
		                         ErrorType issueType)
		{
			Assert.ArgumentNotNull(transaction, nameof(transaction));
			Assert.ArgumentNotNull(issueObject, nameof(issueObject));

			ChangeIssueType(transaction, new[] { issueObject }, issueType);
		}

		/// <summary>
		/// Sets the issue type (Soft/Hard/Allowed) for the specified issue object
		/// within the specified transaction.
		/// </summary>
		/// <param name="transaction">The transaction.</param>
		/// <param name="issueObjects">The issue objects.</param>
		/// <param name="newIssueType">Type of the issue.</param>
		[NotNull]
		public ICollection<IObject> ChangeIssueType(
			[NotNull] IGdbTransaction transaction,
			[NotNull] IEnumerable<IObject> issueObjects,
			ErrorType newIssueType)
		{
			Assert.ArgumentNotNull(transaction, nameof(transaction));
			Assert.ArgumentNotNull(issueObjects, nameof(issueObjects));

			List<IObject> issueObjectList = issueObjects.ToList();
			var modifiedObjects = new List<IObject>();

			if (issueObjectList.Count == 0)
			{
				return modifiedObjects;
			}

			IDictionary<IObjectClass, IList<IObject>> objectsByClass =
				GdbObjectUtils.GroupObjectsByClass(issueObjectList);

			IWorkspace workspace = DatasetUtils.GetUniqueWorkspace(objectsByClass.Keys);
			if (workspace == null)
			{
				return modifiedObjects;
			}

			transaction.Execute(
				workspace,
				delegate
				{
					foreach (KeyValuePair<IObjectClass, IList<IObject>> pair in objectsByClass)
					{
						IObjectClass issueClass = pair.Key;
						bool isFeatureClass = issueClass is IFeatureClass;

						int fieldIndexErrorType = issueClass.Fields.FindField(FieldNameErrorType);

						foreach (IObject issueObject in pair.Value)
						{
							if (! CanChangeTo(newIssueType, issueObject, fieldIndexErrorType))
							{
								continue;
							}

							if (newIssueType == ErrorType.Allowed &&
							    isFeatureClass &&
							    IsTableIssueStoredWithReferenceGeometryFlag(issueObject))
							{
								// move to issue table without geometry
								IssueDatasetWriter issueWriter =
									IssueDatasets.GetIssueWriterNoGeometry();
								ITable issueTable = issueWriter.Table;

								IRow row = issueTable.CreateRow();
								GdbObjectUtils.CopyAttributeValues(issueObject, row);

								RemoveReferenceGeometryFlag(row);

								int fieldIndexCopiedErrorType =
									issueTable.FindField(FieldNameErrorType);
								row.set_Value(fieldIndexCopiedErrorType, (int) newIssueType);
								row.Store();

								modifiedObjects.Add((IObject) row);

								issueObject.Delete();
							}
							else
							{
								issueObject.set_Value(fieldIndexErrorType, (int) newIssueType);
								issueObject.Store();

								modifiedObjects.Add(issueObject);
							}
						}
					}
				}, "Changing issue type");

			return modifiedObjects;
		}

		private static bool CanChangeTo(ErrorType newIssueType,
		                                [NotNull] IObject issueObject,
		                                int fieldIndexErrorType)
		{
			object issueTypeValue = issueObject.Value[fieldIndexErrorType];

			if (issueTypeValue == null || issueTypeValue is DBNull)
			{
				return false;
			}

			var issueType = (ErrorType) issueTypeValue;

			switch (newIssueType)
			{
				case ErrorType.Allowed:
					return issueType == ErrorType.Soft;

				case ErrorType.Soft:
					return issueType == ErrorType.Allowed;

				case ErrorType.Hard:
					return false;

				default:
					return false;
			}
		}

		private void RemoveReferenceGeometryFlag([NotNull] IRow issueRow)
		{
			string issueDescription = GetIssueDescription(issueRow, out int fieldIndex);

			if (! issueDescription.EndsWith(ReferenceGeometryUtils.ReferencedGeometryInfo))
			{
				return;
			}

			string newIssueDescription =
				issueDescription.Substring(
					0,
					issueDescription.Length - ReferenceGeometryUtils.ReferencedGeometryInfo.Length);

			issueRow.Value[fieldIndex] = newIssueDescription;
		}

		private bool IsTableIssueStoredWithReferenceGeometryFlag([NotNull] IRow issueRow)
		{
			string issueDescription = GetIssueDescription(issueRow, out int _);

			return issueDescription.EndsWith(ReferenceGeometryUtils.ReferencedGeometryInfo);
		}

		[NotNull]
		private string GetIssueDescription([NotNull] IRow issueRow, out int fieldIndex)
		{
			fieldIndex = issueRow.Fields.FindField(FieldNameDescription);

			var value = issueRow.Value[fieldIndex] as string;

			return value ?? string.Empty;
		}

		#endregion

		#region Writing issues

		public void QueueError([NotNull] QaError qaError)
		{
			ISpatialReference spatialReference = IssueDatasets.SpatialReference;

			ICollection<esriGeometryType> supportedGeometryTypes =
				IssueDatasets.GetIssueWritersByGeometryType().Keys;

			// create valid Error geometry (geometry type, min dimensions) if possible
			IGeometry geometry = ErrorRepositoryUtils.GetGeometryToStore(
				qaError.Geometry, spatialReference, supportedGeometryTypes);

			// This geometry will not be 'reduced' to null:
			qaError.SetGeometryInModelSpatialReference(geometry);

			_errorQueue.Add(qaError);
		}

		/// <summary>
		/// Adds an error to the error repository and writes it to the database.
		/// Call <see cref="SavePendingErrors"/> to save all the pending errors added to the
		/// repository.
		/// </summary>
		/// <param name="qaError"></param>
		/// <param name="qualityCondition"></param>
		/// <param name="isAllowable"></param>
		public void StoreError([NotNull] QaError qaError,
		                       [NotNull] QualityCondition qualityCondition,
		                       bool isAllowable)
		{
			Assert.ArgumentNotNull(qaError, nameof(qaError));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			IssueDatasetWriter issueWriter = PrepareIssueWriter(qaError, qualityCondition,
			                                                    isAllowable);

			try
			{
				issueWriter.InsertRow();
			}
			catch (IssueFeatureInsertionFailedException)
			{
				_msg.Warn(
					"Unable to write issue feature (see log for details); trying again with simplified issue geometry");

				// the geometry was probably non-simple enough for the insert to fail
				// -> try again with forced simplification of issue geometry
				issueWriter = PrepareIssueWriter(qaError, qualityCondition, isAllowable,
				                                 forceSimplify: true);

				// give up if this fails also
				issueWriter.InsertRow();
			}
		}

		/// <summary>
		/// Saves the pending errors previously added to the repository.
		/// </summary>
		public void SavePendingErrors([CanBeNull] IQualityConditionLookup verificationElements)
		{
			if (_errorQueue.Count > 0)
			{
				Assert.NotNull(verificationElements,
				               "To save queued errors, provide verificationElements");

				foreach (QaError qaError in _errorQueue)
				{
					QualityConditionVerification qualityConditionVerification =
						verificationElements.GetQualityConditionVerification(qaError.Test);

					QualityCondition qualityCondition =
						Assert.NotNull(qualityConditionVerification.QualityCondition);

					bool isAllowable = qualityConditionVerification.AllowErrors;

					StoreError(qaError, qualityCondition, isAllowable);
				}
			}

			IssueDatasets.SavePendingIssues();

			_errorQueue.Clear();
		}

		[NotNull]
		private IssueDatasetWriter PrepareIssueWriter(
			[NotNull] QaError qaError,
			[NotNull] QualityCondition qualityCondition,
			bool isAllowable,
			bool forceSimplify = false)
		{
			// create valid Error geometry (geometry type, min dimensions) if possible
			IGeometry geometry = ErrorRepositoryUtils.GetGeometryToStore(
				qaError.GetGeometryInModelSpatialRef(),
				IssueDatasets.SpatialReference,
				IssueDatasets.GetIssueWritersByGeometryType().Keys,
				_isPre10Geodatabase,
				forceSimplify);

			IssueDatasetWriter issueWriter = IssueDatasets.GetIssueWriter(geometry);

			IRowBuffer rowBuffer = issueWriter.GetRowBuffer();

			// write geometry to the buffer
			if (geometry != null)
			{
				// update the geometry directly if needed (it is already a copy)
				GeometryUtils.EnsureSchemaZM(geometry, issueWriter.HasZ, issueWriter.HasM);

				WriteGeometry(geometry, (IFeatureBuffer) rowBuffer);
			}

			IList<ITest> tests = _testsByQualityCondition[qualityCondition];

			WriteAttributes(issueWriter, rowBuffer, qualityCondition, isAllowable,
			                qaError, tests);

			return issueWriter;
		}

		#region Write issue details

		/// <summary>
		/// Allows writing additional project-specific attributes such as
		/// - operator
		/// - correction status
		/// - work unit / release cycle / model id
		/// </summary>
		/// <param name="issueWriter"></param>
		/// <param name="rowBuffer"></param>
		/// <param name="qualityCondition"></param>
		/// <param name="qaError"></param>
		protected abstract void WriteAttributesCore(
			[NotNull] IssueDatasetWriter issueWriter,
			[NotNull] IRowBuffer rowBuffer,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] QaError qaError);

		private static void WriteGeometry([NotNull] IGeometry geometry,
		                                  [NotNull] IFeatureBuffer featureBuffer)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(featureBuffer, nameof(featureBuffer));

			try
			{
				featureBuffer.Shape = geometry;
			}
			catch (Exception)
			{
				try
				{
					_msg.Debug(GeometryUtils.ToString(geometry));
				}
				catch
				{
					_msg.Debug("Error writing geometry to log");
				}

				throw;
			}
		}

		private void WriteAttributes([NotNull] IssueDatasetWriter issueWriter,
		                             [NotNull] IRowBuffer rowBuffer,
		                             [NotNull] QualityCondition qualityCondition,
		                             bool isAllowable,
		                             [NotNull] QaError qaError,
		                             [NotNull] ICollection<ITest> tests)
		{
			Assert.ArgumentNotNull(issueWriter, nameof(issueWriter));
			Assert.ArgumentNotNull(rowBuffer, nameof(rowBuffer));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(qaError, nameof(qaError));
			Assert.ArgumentNotNull(tests, nameof(tests));

			issueWriter.WriteValue(rowBuffer, AttributeRole.ErrorDescription,
			                       qaError.Description);

			int involvedObjectsMaxLength =
				issueWriter.GetFieldLength(AttributeRole.ErrorObjects);

			string involvedObjects = ErrorObjectUtils.GetInvolvedObjectsString(
				qualityCondition, qaError, tests, involvedObjectsMaxLength,
				_datasetResolver);

			issueWriter.WriteValue(rowBuffer, AttributeRole.ErrorObjects, involvedObjects);

			issueWriter.WriteValue(rowBuffer, AttributeRole.ErrorErrorType,
			                       isAllowable
				                       ? ErrorType.Soft
				                       : ErrorType.Hard);

			issueWriter.WriteValue(rowBuffer, AttributeRole.ErrorConditionId,
			                       qualityCondition.Id);
			issueWriter.WriteValue(rowBuffer, AttributeRole.ErrorConditionName,
			                       qualityCondition.Name);

			issueWriter.WriteValue(rowBuffer, AttributeRole.ErrorConditionParameters,
			                       qualityCondition.GetParameterValuesString());

			if (issueWriter.HasAttribute(AttributeRole.DateOfCreation))
			{
				issueWriter.WriteValue(rowBuffer, AttributeRole.DateOfCreation,
				                       DateTime.Now);
			}

			WriteAttributesCore(issueWriter, rowBuffer, qualityCondition, qaError);
		}

		#endregion

		#endregion

		#region Delete Errors

		/// <summary>
		/// Deletes all issues of the specified quality conditions within the <see cref="Perimeter"/>.
		/// All errors except allowed errors will be deleted if the Perimeter property is not set or
		/// the provided quality conditions list is null.
		/// </summary>
		/// <param name="qualityConditions">The quality conditions.</param>
		/// <param name="objectSelection">The object selection.</param>
		public void DeleteErrors(
			[CanBeNull] IEnumerable<QualityCondition> qualityConditions,
			[CanBeNull] IObjectSelection objectSelection)
		{
			IQueryFilter filter = CreateIssueDeletionFilter(_perimeter, FieldNameErrorType);

			if (qualityConditions == null)
			{
				DeleteIssuesForAllQualityConditions(filter, objectSelection);
			}
			else
			{
				DeleteIssuesForQualityConditions(filter, objectSelection, qualityConditions);
			}
		}

		public void DeleteOrphanedErrors(bool deleteErrorsForUnreferencedQualityConditions)
		{
			var totalCount = 0;

			string message =
				deleteErrorsForUnreferencedQualityConditions
					? "Deleting orphaned issues (including issues for unreferenced quality conditions)"
					: "Deleting orphaned issues";

			using (_msg.IncrementIndentation(message))
			{
				IQueryFilter filter = CreateIssueDeletionFilter(null, FieldNameErrorType);

				var qualityConditionIds = new SimpleSet<int>();

				foreach (
					QualityCondition qualityCondition in
					GetActiveQualityConditions(_qualityConditionRepository,
					                           deleteErrorsForUnreferencedQualityConditions))
				{
					qualityConditionIds.Add(qualityCondition.Id);
				}

				// delete all errors that don't belong to a condition in the list
				foreach (IssueDatasetWriter issueWriter in IssueDatasets.GetIssueWriters())
				{
					totalCount +=
						issueWriter.DeleteOrphanedErrorObjects(filter, qualityConditionIds);
				}
			}

			_msg.InfoFormat(totalCount != 1
				                ? "{0:N0} orphaned issues have been deleted"
				                : "{0:N0} orphaned issue has been deleted",
			                totalCount);
		}

		[NotNull]
		private static IEnumerable<QualityCondition> GetActiveQualityConditions(
			[NotNull] IQualityConditionRepository repository,
			bool requireReferenceFromQualitySpecification)
		{
			Assert.ArgumentNotNull(repository, nameof(repository));

			Stopwatch watch = _msg.DebugStartTiming("Getting active quality conditions");
			var count = 0;

			IDictionary<int, int> referenceCounts;
			if (requireReferenceFromQualitySpecification)
			{
				_msg.DebugFormat(
					"Getting number of referencing quality specifications per quality condition");
				referenceCounts = repository.GetReferencingQualitySpecificationCount();
			}
			else
			{
				referenceCounts = null;
			}

			_msg.DebugFormat("Reading all quality conditions (with parameter values)");

			const bool fetchParameterValues = true;
			foreach (
				QualityCondition qualityCondition in repository.GetAll(fetchParameterValues))
			{
				if (UsesDeletedDatasets(qualityCondition))
				{
					_msg.DebugFormat("Quality condition {0} {1} references deleted datasets",
					                 qualityCondition.Name, qualityCondition.Id);

					// don't include the quality condition since it uses at least one deleted datasets
					continue;
				}

				if (requireReferenceFromQualitySpecification)
				{
					Assert.NotNull(referenceCounts, "referenceCounts");

					int referenceCount;
					bool isReferenced = referenceCounts.TryGetValue(qualityCondition.Id,
						                    out referenceCount) &&
					                    referenceCount > 0;

					if (! isReferenced)
					{
						_msg.DebugFormat(
							"Quality condition {0} {1} is not referenced by any quality specification",
							qualityCondition.Name, qualityCondition.Id);

						// don't include the quality condition since it is not referenced by any quality specification
						continue;
					}
				}

				// the quality condition is "active"
				count++;
				yield return qualityCondition;
			}

			_msg.DebugStopTiming(watch, "Returned {0} active quality conditions", count);
		}

		private static bool UsesDeletedDatasets([NotNull] QualityCondition qualityCondition)
		{
			return qualityCondition.GetDatasetParameterValues(includeReferencedProcessors: true)
			                       .Any(dataset => dataset.Deleted);
		}

		[NotNull]
		private static IQueryFilter CreateIssueDeletionFilter(
			[CanBeNull] IGeometry perimeter,
			[NotNull] string issueTypeField)
		{
			Assert.ArgumentNotNullOrEmpty(issueTypeField, nameof(issueTypeField));

			string whereClause = string.Format(
				"{0} IN ({1}, {2})",
				issueTypeField, (int) ErrorType.Hard, (int) ErrorType.Soft);

			if (perimeter == null)
			{
				return new QueryFilterClass { WhereClause = whereClause };
			}

			return new SpatialFilterClass
			       {
				       Geometry = perimeter,
				       SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects,
				       WhereClause = whereClause
			       };
		}

		/// <summary>
		/// Allows adapting the issue filter to the project-specific context such as narrowing
		/// the search down to specific work unit or release cycle. The filter is currently used
		/// to find errors to be deleted.
		/// </summary>
		/// <param name="issueWriter">The error Table.</param>
		/// <param name="queryFilter">The filter to be adapted. Note that it can already contain a
		/// spatial filter and a where clause. Additional criteria need to be appended.</param>
		protected virtual IQueryFilter AdaptFilterToContext(
			[NotNull] IssueDatasetWriter issueWriter,
			[NotNull] IQueryFilter queryFilter)
		{
			return queryFilter;
		}

		private void DeleteIssuesForAllQualityConditions(
			[NotNull] IQueryFilter queryFilter,
			[CanBeNull] IObjectSelection objectSelection)
		{
			Assert.ArgumentNotNull(queryFilter, nameof(queryFilter));

			if (objectSelection == null)
			{
				DeleteIssues(queryFilter, null, null);
			}
			else
			{
				const bool fetchParameterValues = true;
				IList<QualityCondition> qualityConditions =
					_qualityConditionRepository.GetAll(fetchParameterValues);

				DeleteIssues(queryFilter,
				             objectSelection,
				             qualityConditions.ToDictionary(qcon => qcon.Id));
			}
		}

		private void DeleteIssuesForQualityConditions(
			[NotNull] IQueryFilter filter,
			[CanBeNull] IObjectSelection objectSelection,
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			var qualityConditionsById = new Dictionary<int, QualityCondition>();

			const int maxLength = 2000; // maximum length of IN(....) in ORACLE query

			StringBuilder idListBuilder = null;
			foreach (QualityCondition condition in qualityConditions)
			{
				string idString = condition.Id.ToString(CultureInfo.InvariantCulture);

				if (idListBuilder == null)
				{
					idListBuilder = new StringBuilder(idString);
				}
				else
				{
					if (idListBuilder.Length + idString.Length + 1 > maxLength)
					{
						// the concatenated list gets too long, delete current batch
						DeleteIssues(filter, idListBuilder.ToString(),
						             qualityConditionsById,
						             objectSelection);

						idListBuilder = new StringBuilder(idString);
						qualityConditionsById.Clear();
					}
					else
					{
						idListBuilder.AppendFormat(",{0}", idString);
					}
				}

				qualityConditionsById.Add(condition.Id, condition);
			}

			if (idListBuilder != null)
			{
				// there is an open batch
				DeleteIssues(filter, idListBuilder.ToString(),
				             qualityConditionsById,
				             objectSelection);
			}
		}

		private void DeleteIssues(
			[NotNull] IQueryFilter queryFilter,
			[NotNull] string commaSeparatedQualityConditionIds,
			[NotNull] IDictionary<int, QualityCondition> qualityConditionsById,
			[CanBeNull] IObjectSelection objectSelection)
		{
			Assert.ArgumentNotNull(queryFilter, nameof(queryFilter));
			Assert.ArgumentNotNullOrEmpty(commaSeparatedQualityConditionIds,
			                              nameof(commaSeparatedQualityConditionIds));
			Assert.ArgumentNotNull(qualityConditionsById, nameof(qualityConditionsById));

			string whereClause = string.Format("{0} IN ({1})",
			                                   FieldNameQualityConditionId,
			                                   commaSeparatedQualityConditionIds);

			// To avoid downstream side effects, clone the filter:
			queryFilter = (IQueryFilter) ((IClone) queryFilter).Clone();

			queryFilter.WhereClause =
				StringUtils.IsNotEmpty(queryFilter.WhereClause)
					? string.Format("{0} AND {1}", queryFilter.WhereClause, whereClause)
					: whereClause;

			DeleteIssues(queryFilter, objectSelection, qualityConditionsById);
		}

		private void DeleteIssues(
			[NotNull] IQueryFilter queryFilter,
			[CanBeNull] IObjectSelection objectSelection,
			[CanBeNull] IDictionary<int, QualityCondition> qualityConditionsById)
		{
			Assert.ArgumentNotNull(queryFilter, nameof(queryFilter));

			foreach (IssueDatasetWriter issueWriter in IssueDatasets.GetIssueWriters())
			{
				// add project-specific filter logic
				IQueryFilter adaptedFilter = AdaptFilterToContext(issueWriter, queryFilter);

				if (objectSelection == null)
				{
					issueWriter.DeleteErrorObjects(adaptedFilter);
				}
				else
				{
					Assert.NotNull(
						qualityConditionsById,
						"The quality conditions are required when there is an object selection");

					var determineDeletableRow = new DeletableErrorRowFilter(issueWriter,
						objectSelection);

					issueWriter.DeleteErrorObjects(adaptedFilter,
					                               determineDeletableRow,
					                               qualityConditionsById);
				}
			}
		}

		#endregion

		#region Managing allowed errors

		#region Check for allowed error

		public bool IsAllowedError([NotNull] QaError qaError,
		                           [NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qaError, nameof(qaError));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			if (! IssueDatasets.GetIssueWriters().Any())
			{
				// No issue datasets, no allowed errors
				return false;
			}

			QaError comparableError = AllowedErrorUtils.GetForAllowedErrorComparison(
				qaError, IssueDatasets.SpatialReference,
				IssueDatasets.GetIssueWritersByGeometryType().Keys,
				_isPre10Geodatabase);

			var spatialFilter =
				new SpatialFilterClass
				{
					WhereClause = string.Format("{0} = {1} AND {2} = {3}",
					                            FieldNameQualityConditionId,
					                            qualityCondition.Id,
					                            FieldNameErrorType,
					                            (int) ErrorType.Allowed)
				};

			var qualityConditionsById = new Dictionary<int, QualityCondition>
			                            { { qualityCondition.Id, qualityCondition } };

			var allowedErrors =
				new List<AllowedError>(GetAllowedErrors(comparableError.Geometry,
				                                        spatialFilter,
				                                        qualityConditionsById));
			if (allowedErrors.Count == 0)
			{
				return false;
			}

			allowedErrors.Sort();

			return AllowedErrorUtils.IsAllowedError(comparableError,
			                                        qualityCondition,
			                                        allowedErrors,
			                                        _datasetResolver);
		}

		[CanBeNull]
		public AllowedError FindAllowedError([NotNull] QaError qaError,
		                                     [NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qaError, nameof(qaError));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			if (AllowedErrorList.Count == 0)
			{
				return null;
			}

			return AllowedErrorUtils.FindAllowedErrorInSortedList(
				AllowedErrorList, qaError, qualityCondition,
				IssueDatasets.SpatialReference, _datasetResolver,
				IssueDatasets.GetIssueWritersByGeometryType().Keys,
				_isPre10Geodatabase);
		}

		#endregion

		#region Delete allowed errors

		/// <summary>
		/// Deletes all allowed errors where the Used property is false. The used property is set to true
		/// in a verification if the error was found and the allowed error was used as an exception.
		/// If the error was corrected in the mean while the allowed error is not needed any more. 
		/// </summary>
		public void DeleteUnusedAllowedErrors()
		{
			AllowedErrorUtils.DeleteAllowedErrors(GetUnusedAllowedErrors());
		}

		[NotNull]
		private IEnumerable<AllowedError> GetUnusedAllowedErrors()
		{
			// TODO: if the error *can* have derived geometry, then don't delete unreported allowed errors
			//       (as it may not be reported simply because the current verification context does not contain 
			//        any related feature class as *verifiable*)
			//        https://issuetracker02.eggits.net/browse/PSM-163

			return AllowedErrorList.Where(allowedError => ! allowedError.IsUsed).ToList();
		}

		/// <summary>
		/// Deletes those allowed errors whose context have changed. In the base implementation this means
		/// where any involved row has changed since the error creation. Additional context change situations
		/// such as a change in the quality condition can be detected by the sub-repository.
		/// </summary>
		/// <param name="qualityConditions">The quality conditions.</param>
		/// <param name="invalidateAllowedErrorsIfAnyInvolvedObjectChanged">
		/// if set to <c>true</c> allowed errors are invalidated if any involved object changed.</param>
		/// <param name="invalidateAllowedErrorsIfQualityConditionWasUpdated">
		/// if set to <c>true</c> allowed errors are invalidated if the quality condition was updated.</param>
		public void DeleteInvalidAllowedErrors(
			[NotNull] IEnumerable<QualityCondition> qualityConditions,
			bool invalidateAllowedErrorsIfAnyInvolvedObjectChanged,
			bool invalidateAllowedErrorsIfQualityConditionWasUpdated)
		{
			// TODO: consider using AllowedErrorList property, remove qualityConditions parameter
			Assert.ArgumentNotNull(qualityConditions, nameof(qualityConditions));

			ICollection<QualityCondition> qualityConditionCollection =
				CollectionUtils.GetCollection(qualityConditions);

			var allowedErrors =
				new List<AllowedError>(GetAllowedErrors(qualityConditionCollection));

			AllowedErrorUtils.InvalidateAllowedErrors(
				allowedErrors,
				qualityConditionCollection,
				_verificationContext,
				invalidateAllowedErrorsIfAnyInvolvedObjectChanged,
				invalidateAllowedErrorsIfQualityConditionWasUpdated);

			List<AllowedError> invalidErrors =
				allowedErrors.Where(error => error.Invalidated).ToList();

			AllowedErrorUtils.DeleteAllowedErrors(invalidErrors);
		}

		/// <summary>
		/// Ensures that the relevant allowed errors are cached in <see cref="AllowedErrorList"/>
		/// and invalidates those allowed errors that should be deleted, such as where involved
		/// objects do not exist any more.
		/// </summary>
		/// <param name="qualityConditions"></param>
		/// <param name="invalidateAllowedErrorsIfAnyInvolvedObjectChanged"></param>
		/// <param name="invalidateAllowedErrorsIfQualityConditionWasUpdated"></param>
		public void InvalidateAllowedErrors(
			[NotNull] IEnumerable<QualityCondition> qualityConditions,
			bool invalidateAllowedErrorsIfAnyInvolvedObjectChanged,
			bool invalidateAllowedErrorsIfQualityConditionWasUpdated)
		{
			AllowedErrorUtils.InvalidateAllowedErrors(
				AllowedErrorList, qualityConditions, _verificationContext,
				invalidateAllowedErrorsIfAnyInvolvedObjectChanged,
				invalidateAllowedErrorsIfQualityConditionWasUpdated);
		}

		public IEnumerable<AllowedError> GetAllowedErrors(
			[CanBeNull] Predicate<AllowedError> predicate)
		{
			foreach (AllowedError allowedError in AllowedErrorList)
			{
				if (predicate == null || predicate(allowedError))
				{
					yield return allowedError;
				}
			}
		}

		#endregion

		#region Allowed error cache

		/// <summary>
		/// sorted list of allowed errors
		/// </summary>
		[NotNull]
		private List<AllowedError> AllowedErrorList
		{
			// Idea: Extract AllowedErrorCache/AllowedErrorRepository that knows the verified
			// conditions
			get
			{
				if (_allowedErrors == null)
				{
					if (IssueDatasets.GetIssueWriters().Any())
					{
						_allowedErrors = new List<AllowedError>(
							GetAllowedErrors(VerifiedQualityConditions));
						_allowedErrors.Sort();
					}
					else
					{
						// No issue datasets, no allowed errors:
						_allowedErrors = new List<AllowedError>(0);
					}
				}

				return _allowedErrors;
			}
		}

		[NotNull]
		private IEnumerable<AllowedError> GetAllowedErrors(
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			var filter = new SpatialFilterClass
			             {
				             WhereClause = string.Format("{0} = {1}",
				                                         FieldNameErrorType,
				                                         (int) ErrorType.Allowed)
			             };

			return GetAllowedErrors(_perimeter, filter,
			                        GetQualityConditionsById(qualityConditions));
		}

		[NotNull]
		private static IDictionary<int, QualityCondition> GetQualityConditionsById(
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			Assert.ArgumentNotNull(qualityConditions, nameof(qualityConditions));

			return qualityConditions.Where(qualityCondition => qualityCondition.Id >= 0)
			                        .ToDictionary(qualityCondition => qualityCondition.Id);
		}

		[NotNull]
		private IEnumerable<AllowedError> GetAllowedErrors(
			[CanBeNull] IGeometry areaOfInterest,
			[NotNull] ISpatialFilter spatialFilter,
			[NotNull] IDictionary<int, QualityCondition> qualityConditionsById)
		{
			// can be made static if error tables and conditions are passed in --> "repository"?
			var factory = new AllowedErrorFactory(qualityConditionsById, _datasetResolver);

			foreach (IssueDatasetWriter issueDatasetWriter in IssueDatasets.GetIssueWriters())
			{
				foreach (AllowedError allowedError in AllowedErrorUtils.GetAllowedErrors(
					         issueDatasetWriter, areaOfInterest, spatialFilter, factory))
				{
					yield return allowedError;
				}
			}
		}

		#endregion

		#endregion

		private class DeletableErrorRowFilter : IDeletableErrorRowFilter
		{
			private readonly IssueDatasetWriter _issueWriter;
			private readonly IObjectSelection _objectSelection;

			public DeletableErrorRowFilter([NotNull] IssueDatasetWriter issueWriter,
			                               [NotNull] IObjectSelection objectSelection)
			{
				Assert.ArgumentNotNull(issueWriter, nameof(issueWriter));
				Assert.ArgumentNotNull(objectSelection, nameof(objectSelection));

				_issueWriter = issueWriter;
				_objectSelection = objectSelection;
			}

			public bool IsDeletable(IRow errorRow,
			                        QualityCondition qualityCondition)
			{
				var hasUnknownTables = false;
				foreach (InvolvedRow involvedRow in _issueWriter.GetInvolvedRows(errorRow))
				{
					bool tableIsUnknown;
					if (_objectSelection.Contains(involvedRow, qualityCondition, out tableIsUnknown)
					   )
					{
						return true;
					}

					if (tableIsUnknown)
					{
						hasUnknownTables = true;
					}
				}

				if (hasUnknownTables)
				{
					// TODO delete? 
				}

				return false;
			}
		}
	}
}
