using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	public static class AllowedErrorUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static void DeleteAllowedErrors(
			[NotNull] IEnumerable<AllowedError> allowedErrors)
		{
			Assert.ArgumentNotNull(allowedErrors, nameof(allowedErrors));

			IEnumerable<KeyValuePair<ITable, IList<int>>> allowedObjectIDsByErrorTable =
				GetObjectIDsByAllowedErrorTable(allowedErrors);

			DeleteAllowedErrors(allowedObjectIDsByErrorTable);
		}

		public static void DeleteAllowedErrors(
			[NotNull] IEnumerable<GdbObjectReference> allowedErrorObjRefs,
			[NotNull] IEnumerable<ITable> allowedErrorTables)
		{
			IEnumerable<KeyValuePair<ITable, IList<int>>> allowedObjectIDsByErrorTable =
				GetObjectIDsByAllowedErrorTable(allowedErrorObjRefs, allowedErrorTables);

			DeleteAllowedErrors(allowedObjectIDsByErrorTable);
		}

		public static void DeleteAllowedErrors(
			[NotNull] IEnumerable<KeyValuePair<ITable, IList<int>>> allowedObjectIDsByErrorTable)
		{
			foreach (KeyValuePair<ITable, IList<int>> pair in allowedObjectIDsByErrorTable)
			{
				IList<int> oids = pair.Value;

				if (oids.Count == 0)
				{
					continue;
				}

				Stopwatch watch = _msg.DebugStartTiming();

				ITable table = pair.Key;
				DatasetUtils.DeleteRows(table, oids);

				_msg.VerboseDebug(
					() =>
						$"Deleted from {DatasetUtils.GetName(table)}: {StringUtils.Concatenate(oids, ", ")}");

				_msg.DebugStopTiming(watch, "Deleted {0} allowed errors in {1}",
				                     oids.Count, DatasetUtils.GetName(table));
			}
		}

		/// <summary>
		/// Assigns allowedError.Invalidated = true for all errors, where the context changed.
		/// In the base implementation, Invalidated is assigned to errors, where involved rows changed.
		/// </summary>
		/// <param name="allowedErrors">The allowed errors.</param>
		/// <param name="qualityConditions">The quality conditions.</param>
		/// <param name="datasetContext">The model context.</param>
		/// <param name="invalidateIfAnyInvolvedObjectChanged">if set to <c>true</c> [invalidate if involved object changed].</param>
		/// <param name="invalidateIfQualityConditionWasUpdated">if set to <c>true</c> [invalidate if quality condition was updated].</param>
		public static void InvalidateAllowedErrors(
			[NotNull] IEnumerable<AllowedError> allowedErrors,
			[NotNull] IEnumerable<QualityCondition> qualityConditions,
			[NotNull] IDatasetContext datasetContext,
			bool invalidateIfAnyInvolvedObjectChanged,
			bool invalidateIfQualityConditionWasUpdated)
		{
			Assert.ArgumentNotNull(allowedErrors, nameof(allowedErrors));
			Assert.ArgumentNotNull(qualityConditions, nameof(qualityConditions));
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));

			ICollection<AllowedError> allowedErrorsCollection =
				CollectionUtils.GetCollection(allowedErrors);

			IDictionary<string, IDictionary<int, List<AllowedError>>>
				errorsByMissingTableNameAndInvolvedObjectID;
			IEnumerable<KeyValuePair<IObjectDataset, IDictionary<int, List<AllowedError>>>>
				errorsByObjectDatasetAndInvolvedObjectID =
					GetAllowedErrorsByObjectDatasetAndInvolvedObjectID(
						allowedErrorsCollection,
						out errorsByMissingTableNameAndInvolvedObjectID);

			foreach (string tableName in errorsByMissingTableNameAndInvolvedObjectID.Keys)
			{
				_msg.WarnFormat("Table '{0}' referenced by allowed errors not registered",
				                tableName);
			}

			foreach (KeyValuePair<IObjectDataset, IDictionary<int, List<AllowedError>>> pair
			         in errorsByObjectDatasetAndInvolvedObjectID)
			{
				InvalidateAllowedErrorsByInvolvedObjectState(
					pair.Key,
					datasetContext,
					pair.Value,
					invalidateIfAnyInvolvedObjectChanged);
			}

			if (invalidateIfQualityConditionWasUpdated)
			{
				InvalidateAllowedErrorsWithUpdatedQualityCondition(allowedErrorsCollection,
					qualityConditions);
			}
		}

		[NotNull]
		public static IEnumerable<AllowedError> GetAllowedErrors(
			[NotNull] IssueDatasetWriter issueWriter,
			[CanBeNull] IGeometry areaOfInterest,
			[NotNull] ISpatialFilter spatialFilter,
			[NotNull] AllowedErrorFactory allowedErrorFactory)
		{
			Assert.ArgumentNotNull(issueWriter, nameof(issueWriter));
			Assert.ArgumentNotNull(spatialFilter, nameof(spatialFilter));
			Assert.ArgumentNotNull(allowedErrorFactory, nameof(allowedErrorFactory));

			Stopwatch watch = _msg.DebugStartTiming();

			ITable errorTable = issueWriter.Table;
			IQueryFilter filter;

			var errorFeatureClass = errorTable as IFeatureClass;
			if (errorFeatureClass != null)
			{
				if (areaOfInterest != null)
				{
					spatialFilter.Geometry = areaOfInterest;
					spatialFilter.GeometryField = errorFeatureClass.ShapeFieldName;
					spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
				}

				filter = spatialFilter;
			}
			else
			{
				filter = new QueryFilterClass
				         {
					         WhereClause = spatialFilter.WhereClause,
					         SubFields = spatialFilter.SubFields
				         };
			}

			var addedAllowedErrorsCount = 0;
			var readRowCount = 0;

			const bool recycling = true;

			foreach (IRow row in GdbQueryUtils.GetRows(errorTable, filter, recycling))
			{
				readRowCount++;
				AllowedError allowedError =
					allowedErrorFactory.CreateAllowedError(issueWriter, row);

				if (allowedError == null)
				{
					continue;
				}

				addedAllowedErrorsCount++;

				yield return allowedError;
			}

			_msg.DebugStopTiming(watch,
			                     "Collected {0} allowed error(s) based on {1} row(s) read from {2}",
			                     addedAllowedErrorsCount, readRowCount,
			                     issueWriter.DatasetName);
		}

		[CanBeNull]
		public static AllowedError FindAllowedErrorInSortedList(
			[NotNull] List<AllowedError> allowedErrors,
			[NotNull] QaError qaError,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] ISpatialReference spatialReference,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[NotNull] ICollection<esriGeometryType> storedGeometryTypes)
		{
			const bool forPre10Geodatabase = false;
			return FindAllowedErrorInSortedList(allowedErrors, qaError, qualityCondition,
			                                    spatialReference, datasetResolver,
			                                    storedGeometryTypes, forPre10Geodatabase);
		}

		[CanBeNull]
		public static AllowedError FindAllowedErrorInSortedList(
			[NotNull] List<AllowedError> allowedErrors,
			[NotNull] QaError qaError,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] ISpatialReference spatialReference,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[NotNull] ICollection<esriGeometryType> storedGeometryTypes,
			bool forPre10Geodatabase)
		{
			Assert.ArgumentNotNull(allowedErrors, nameof(allowedErrors));
			Assert.ArgumentNotNull(qaError, nameof(qaError));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));
			Assert.ArgumentNotNull(storedGeometryTypes, nameof(storedGeometryTypes));

			if (allowedErrors.Count <= 0)
			{
				return null;
			}

			QaError asStoredError = GetForAllowedErrorComparison(
				qaError, spatialReference, storedGeometryTypes, forPre10Geodatabase);

			int index = FindAllowedErrorIndex(asStoredError,
			                                  qualityCondition,
			                                  allowedErrors,
			                                  datasetResolver);

			return index < 0
				       ? null
				       : allowedErrors[index];
		}

		public static bool IsAllowedError(
			[NotNull] QaError comparableError,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] List<AllowedError> allowedErrors,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver)
		{
			int index = FindAllowedErrorIndex(comparableError,
			                                  qualityCondition,
			                                  allowedErrors,
			                                  datasetResolver);

			return index >= 0;
		}

		[NotNull]
		public static QaError GetForAllowedErrorComparison(
			[NotNull] QaError qaError,
			[NotNull] ISpatialReference spatialReference,
			[NotNull] ICollection<esriGeometryType> storedGeometryTypes)
		{
			const bool forPre10Geodatabase = false;
			return GetForAllowedErrorComparison(qaError, spatialReference, storedGeometryTypes,
			                                    forPre10Geodatabase);
		}

		[NotNull]
		public static QaError GetForAllowedErrorComparison(
			[NotNull] QaError qaError,
			[NotNull] ISpatialReference spatialReference,
			[NotNull] ICollection<esriGeometryType> storedGeometryTypes,
			bool forPre10Geodatabase)
		{
			Assert.ArgumentNotNull(qaError, nameof(qaError));
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			if (qaError.Geometry == null)
			{
				return qaError;
			}

			return new QaError(qaError.Test,
			                   qaError.Description,
			                   qaError.InvolvedRows,
			                   ErrorRepositoryUtils.GetGeometryToStore(qaError.Geometry,
				                   spatialReference,
				                   storedGeometryTypes,
				                   forPre10Geodatabase),
			                   qaError.IssueCode,
			                   qaError.AffectedComponent);
		}

		private static int FindAllowedErrorIndex(
			[NotNull] QaError comparableError,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] List<AllowedError> allowedErrors,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver)
		{
			const bool usesGdbDatasetNames = true;

			// TODO resolve involved rows to datasets - compare by datasets instead of table names
			var searchInstance = new AllowedError(qualityCondition,
			                                      comparableError,
			                                      datasetResolver,
			                                      usesGdbDatasetNames);

			return allowedErrors.BinarySearch(searchInstance);
		}

		private static void InvalidateAllowedErrorsByInvolvedObjectState(
			[NotNull] IObjectDataset involvedObjectDataset,
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IDictionary<int, List<AllowedError>> allowedErrorsByInvolvedObjectID,
			bool invalidateIfAnyInvolvedObjectChanged)
		{
			ICollection<int> existingInvolvedObjectIds;
			try
			{
				existingInvolvedObjectIds = GetExistingInvolvedObjectIds(
					involvedObjectDataset, datasetContext,
					allowedErrorsByInvolvedObjectID,
					invalidateIfAnyInvolvedObjectChanged);
			}
			catch (InvolvedTableOpenException e)
			{
				_msg.WarnFormat(e.Message);

				// don't invalidate the allowed errors that involve a table which (currently?) can't be opened
				return;
			}

			// invalidate the allowed errors if any of the involved objects no longer exists
			foreach (
				KeyValuePair<int, List<AllowedError>> involvedObjectId in
				allowedErrorsByInvolvedObjectID)
			{
				int objectId = involvedObjectId.Key;

				if (objectId <= 0)
				{
					// QUICK FIX for https://issuetracker02.eggits.net/browse/COM-171
					// this seems to be the case for allowed errors without any involved objects 
					// TODO find out why
					continue;
				}

				if (existingInvolvedObjectIds.Contains(objectId))
				{
					// the involved object still exists
					continue;
				}

				// object with rowId does not exist any more in table,
				// so it is deleted and an implicit change
				foreach (AllowedError error in
				         allowedErrorsByInvolvedObjectID[objectId])
				{
					error.Invalidated = true;
				}
			}
		}

		private static void InvalidateIfAnyInvolvedObjectChanged(
			[NotNull] IEnumerable<AllowedError> allowedErrors,
			[NotNull] IRow involvedRow,
			int dateOfChangeFieldIndex)
		{
			DateTime? dateOfInvolvedRowChange =
				GdbObjectUtils.ReadRowValue<DateTime>(
					involvedRow, dateOfChangeFieldIndex);

			foreach (AllowedError allowedError in allowedErrors)
			{
				if (allowedError.DateOfCreation < dateOfInvolvedRowChange)
				{
					allowedError.Invalidated = true;
				}
			}
		}

		/// <summary>
		/// Sets the Invalidated flag on the allowed errors for which the version
		/// of the quality condition has changed since the error was stored.
		/// </summary>
		/// <param name="allowedErrors">The allowed errors to be evalutated.</param>
		/// <param name="qualityConditions">The list quality conditions to be tested.</param>
		private static void InvalidateAllowedErrorsWithUpdatedQualityCondition(
			[NotNull] IEnumerable<AllowedError> allowedErrors,
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			Dictionary<int, QualityCondition> conditionsById =
				qualityConditions.ToDictionary(condition => condition.Id);

			foreach (AllowedError allowedError in allowedErrors)
			{
				if (allowedError.Invalidated)
				{
					continue;
				}

				QualityCondition condition =
					conditionsById[allowedError.QualityConditionId];

				if (WasQualityConditionUpdatedSinceAllowedErrorCreation(allowedError, condition))
				{
					allowedError.Invalidated = true;
				}
			}
		}

		[NotNull]
		private static ICollection<int> GetExistingInvolvedObjectIds(
			[NotNull] IObjectDataset involvedObjectDataset,
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IDictionary<int, List<AllowedError>> allowedErrorsByInvolvedObjectID,
			bool invalidateIfAnyInvolvedObjectChanged)
		{
			var result = new SimpleSet<int>(allowedErrorsByInvolvedObjectID.Count);

			IQueryFilter queryFilter;
			int dateOfChangeFieldIndex;
			ITable involvedTable = GetInvolvedTableAndQueryFilter(
				involvedObjectDataset, datasetContext,
				invalidateIfAnyInvolvedObjectChanged,
				out queryFilter, out dateOfChangeFieldIndex);

			const bool recycle = true;
			foreach (IRow involvedRow in GdbQueryUtils.GetRowsInList(
				         involvedTable,
				         involvedTable.OIDFieldName,
				         allowedErrorsByInvolvedObjectID.Keys,
				         recycle, queryFilter))
			{
				// these are all involved rows in all the allowed errors. 
				int oid = involvedRow.OID;

				result.TryAdd(oid);

				if (dateOfChangeFieldIndex >= 0)
				{
					InvalidateIfAnyInvolvedObjectChanged(allowedErrorsByInvolvedObjectID[oid],
					                                     involvedRow,
					                                     dateOfChangeFieldIndex);
				}
			}

			return result;
		}

		[NotNull]
		private static ITable GetInvolvedTableAndQueryFilter(
			[NotNull] IObjectDataset involvedObjectDataset,
			[NotNull] IDatasetContext datasetContext,
			bool includeDateOfChangeField,
			[NotNull] out IQueryFilter queryFilter,
			out int dateOfChangeFieldIndex)
		{
			ITable result;
			try
			{
				result = datasetContext.OpenTable(involvedObjectDataset);
				Assert.NotNull(result, "Dataset not found in current context: {0}",
				               involvedObjectDataset.Name);
			}
			catch (Exception e)
			{
				string message;
				if (involvedObjectDataset.Deleted)
				{
					message = string.Format(
						"The dataset '{0}' referenced in allowed errors is registered as deleted, unable to open",
						involvedObjectDataset.Name);
				}
				else
				{
					message = string.Format(
						"Error opening dataset '{0}' referenced in allowed errors: {1}",
						involvedObjectDataset.Name,
						e.Message);
				}

				throw new InvolvedTableOpenException(message, e);
			}

			queryFilter = new QueryFilterClass();

			var subfields = new List<string> {result.OIDFieldName};

			if (includeDateOfChangeField)
			{
				ObjectAttribute dateOfChangeAttribute =
					involvedObjectDataset.GetAttribute(AttributeRole.DateOfChange);

				dateOfChangeFieldIndex =
					dateOfChangeAttribute != null
						? AttributeUtils.GetFieldIndex(result, dateOfChangeAttribute)
						: -1;

				if (dateOfChangeAttribute != null && dateOfChangeFieldIndex >= 0)
				{
					subfields.Add(dateOfChangeAttribute.Name);
				}
			}
			else
			{
				dateOfChangeFieldIndex = -1;
			}

			GdbQueryUtils.SetSubFields(queryFilter, subfields);

			return result;
		}

		[NotNull]
		private static IEnumerable<KeyValuePair<ITable, IList<int>>>
			GetObjectIDsByAllowedErrorTable(
				[NotNull] IEnumerable<AllowedError> allowedErrors)
		{
			Assert.ArgumentNotNull(allowedErrors, nameof(allowedErrors));

			var result = new Dictionary<ITable, IList<int>>();

			foreach (AllowedError allowedError in allowedErrors)
			{
				IList<int> oids;
				if (! result.TryGetValue(allowedError.Table, out oids))
				{
					oids = new List<int>();
					result.Add(allowedError.Table, oids);
				}

				oids.Add(allowedError.ObjectId);
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<KeyValuePair<ITable, IList<int>>>
			GetObjectIDsByAllowedErrorTable(
				[NotNull] IEnumerable<GdbObjectReference> allowedErrorObjRefs,
				[NotNull] IEnumerable<ITable> allowedErrorTables)
		{
			Assert.ArgumentNotNull(allowedErrorObjRefs, nameof(allowedErrorObjRefs));
			Assert.ArgumentNotNull(allowedErrorTables, nameof(allowedErrorTables));

			var result = new Dictionary<ITable, IList<int>>();

			var allowedErrorObjRefCollection = CollectionUtils.GetCollection(allowedErrorObjRefs);

			foreach (ITable allowedErrorTable in allowedErrorTables)
			{
				int objectClassID = ((IObjectClass) allowedErrorTable).ObjectClassID;

				List<int> idsForTable =
					(from gdbObjectReference in allowedErrorObjRefCollection
					 where gdbObjectReference.ClassId == objectClassID
					 select gdbObjectReference.ObjectId).ToList();

				result.Add(allowedErrorTable, idsForTable);
			}

			return result;
		}

		[NotNull]
		private static
			IEnumerable<KeyValuePair<IObjectDataset, IDictionary<int, List<AllowedError>>>>
			GetAllowedErrorsByObjectDatasetAndInvolvedObjectID(
				[NotNull] IEnumerable<AllowedError> allowedErrors,
				[NotNull] out IDictionary<string, IDictionary<int, List<AllowedError>>>
					allowedErrorsByMissingTableNameAndInvolvedObjectID)
		{
			Stopwatch stopWatch = _msg.DebugStartTiming();

			var result =
				new Dictionary<IObjectDataset, IDictionary<int, List<AllowedError>>>();

			allowedErrorsByMissingTableNameAndInvolvedObjectID =
				new Dictionary<string, IDictionary<int, List<AllowedError>>>(
					StringComparer.OrdinalIgnoreCase);

			// TODO for allowed errors with no involved objects (e.g. NoGaps), there seems to be one involvedRow with an OID=0
			// currently these can't be correctly used as allowed errors (no match -> deleted)
			// https://issuetracker02.eggits.net/browse/COM-171

			var count = 0;
			foreach (AllowedError allowedError in allowedErrors)
			{
				count++;

				AddResolvedInvolvedObjects(allowedError, result);
				AddUnesolvedInvolvedObjects(allowedError,
				                            allowedErrorsByMissingTableNameAndInvolvedObjectID);
			}

			_msg.DebugStopTiming(stopWatch,
			                     "Returned {0} allowed error(s) by involved objects",
			                     count);

			return result;
		}

		private static void AddUnesolvedInvolvedObjects(
			[NotNull] AllowedError allowedError,
			[NotNull] IDictionary<string, IDictionary<int, List<AllowedError>>>
				allowedErrorsByUnresolvedTableNameAndObjectId)
		{
			foreach (
				KeyValuePair<string, ICollection<int>> pair in
				allowedError.InvolvedRowsByUnresolvedTableName)
			{
				string tableName = pair.Key;
				ICollection<int> oids = pair.Value;

				IDictionary<int, List<AllowedError>> errorsByInvolvedOID;
				if (! allowedErrorsByUnresolvedTableNameAndObjectId.TryGetValue(
					    tableName, out errorsByInvolvedOID))
				{
					errorsByInvolvedOID = new Dictionary<int, List<AllowedError>>();
					allowedErrorsByUnresolvedTableNameAndObjectId.Add(tableName,
						errorsByInvolvedOID);
				}

				foreach (int oid in oids)
				{
					List<AllowedError> errors;
					if (! errorsByInvolvedOID.TryGetValue(oid, out errors))
					{
						errors = new List<AllowedError>();
						errorsByInvolvedOID.Add(oid, errors);
					}

					errors.Add(allowedError);
				}
			}
		}

		private static void AddResolvedInvolvedObjects(
			[NotNull] AllowedError allowedError,
			[NotNull] IDictionary<IObjectDataset, IDictionary<int, List<AllowedError>>>
				allowedErrorsByDatasetAndObjectId)
		{
			foreach (InvolvedDatasetRow involvedDatasetRow in allowedError.InvolvedDatasetRows)
			{
				IObjectDataset objectDataset = involvedDatasetRow.Dataset;
				int oid = involvedDatasetRow.ObjectId;

				IDictionary<int, List<AllowedError>> errorsByInvolvedOID;
				if (! allowedErrorsByDatasetAndObjectId.TryGetValue(
					    objectDataset,
					    out errorsByInvolvedOID))
				{
					errorsByInvolvedOID = new Dictionary<int, List<AllowedError>>();
					allowedErrorsByDatasetAndObjectId.Add(objectDataset,
					                                      errorsByInvolvedOID);
				}

				List<AllowedError> errors;
				if (! errorsByInvolvedOID.TryGetValue(oid, out errors))
				{
					errors = new List<AllowedError>();
					errorsByInvolvedOID.Add(oid, errors);
				}

				errors.Add(allowedError);
			}
		}

		private static bool WasQualityConditionUpdatedSinceAllowedErrorCreation(
			[NotNull] AllowedError allowedError,
			[NotNull] QualityCondition qualityCondition)
		{
			// TODO compare hash of RELEVANT quality condition properties

			// TODO revise
			if (allowedError.QualityConditionVersion == null ||
			    allowedError.QualityConditionVersion < qualityCondition.Version)
			{
				return true;
			}

			return false;
		}

		private class InvolvedTableOpenException : Exception
		{
			public InvolvedTableOpenException(string message, Exception innerException)
				: base(message, innerException) { }
		}
	}
}
