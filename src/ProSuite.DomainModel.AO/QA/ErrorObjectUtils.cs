using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Container;

namespace ProSuite.DomainModel.AO.QA
{
	public static class ErrorObjectUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static IErrorObject CreateErrorObject(
			[NotNull] IObject errorObject, [NotNull] IErrorDataset errorDataset,
			[CanBeNull] IFieldIndexCache fieldIndexCache = null)
		{
			Assert.ArgumentNotNull(errorObject, nameof(errorObject));
			Assert.ArgumentNotNull(errorDataset, nameof(errorDataset));

			switch (errorDataset)
			{
				case ErrorTableDataset dataset:
					return new ErrorTableObject(errorObject, dataset, fieldIndexCache);
				case ErrorLineDataset dataset:
					return new ErrorLineObject((IFeature) errorObject, dataset, fieldIndexCache);
				case ErrorMultiPatchDataset dataset:
					return new ErrorMultiPatchObject((IFeature) errorObject, dataset,
					                                 fieldIndexCache);
				case ErrorMultipointDataset dataset:
					return new ErrorMultipointObject((IFeature) errorObject, dataset,
					                                 fieldIndexCache);
				case ErrorPolygonDataset dataset:
					return new ErrorPolygonObject((IFeature) errorObject, dataset, fieldIndexCache);
				default:
					throw new ArgumentOutOfRangeException(
						$"Unknown IErrorDataset: {errorDataset.GetType()}");
			}
		}

		[NotNull]
		public static IErrorDataset GetErrorDataset([NotNull] IObject errorObject,
		                                            [NotNull] IDatasetLookup datasetLookup)
		{
			Assert.ArgumentNotNull(errorObject, nameof(errorObject));
			Assert.ArgumentNotNull(datasetLookup, nameof(datasetLookup));

			var dataset = (IErrorDataset) datasetLookup.GetDataset(errorObject);

			Assert.NotNull(dataset, "error dataset not found for {0}",
			               GdbObjectUtils.ToString(errorObject));
			return dataset;
		}

		// NOTE must be called in a domain transaction
		[NotNull]
		public static IEnumerable<IRow> GetInvolvedRows(
			[NotNull] IErrorObject errorObject,
			[NotNull] IModelContext modelContext,
			[NotNull] IQualityConditionRepository qualityConditionRepository)
		{
			Assert.ArgumentNotNull(errorObject, nameof(errorObject));
			Assert.ArgumentNotNull(modelContext, nameof(modelContext));

			var datasetResolver = new QualityConditionObjectDatasetResolver(modelContext);

			return GetInvolvedRows(errorObject, modelContext, qualityConditionRepository,
			                       datasetResolver);
		}

		[NotNull]
		public static IEnumerable<IFeature> GetDerivedTableGeometryFeatures(
			[NotNull] IObject obj,
			[NotNull] ObjectDataset dataset,
			[NotNull] IModelContext modelContext)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));
			Assert.ArgumentNotNull(dataset, nameof(dataset));
			Assert.ArgumentNotNull(modelContext, nameof(modelContext));

			// TODO move elsewhere - domain? IObjectDataset? use objectrepository (moved from AE)?

			var relationList = new List<IRelationshipClass>();

			IWorkspaceContext workspaceContext = null;
			foreach (AssociationEnd associationEnd in dataset.GetAssociationEnds())
			{
				if (! (associationEnd.OppositeDataset is VectorDataset) ||
				    associationEnd.Association.NotUsedForDerivedTableGeometry)
				{
					continue;
				}

				// make sure to open the relationship class from the same workspace context as the dataset
				if (workspaceContext == null)
				{
					workspaceContext = modelContext.GetWorkspaceContext(dataset);
					Assert.NotNull(workspaceContext,
					               "Unable to determine workspace context for dataset {0}",
					               dataset);
				}

				IRelationshipClass relationshipClass =
					workspaceContext.OpenRelationshipClass(associationEnd.Association);

				if (relationshipClass != null)
				{
					relationList.Add(relationshipClass);
				}
			}

			return GdbQueryUtils.GetRelatedObjectList(obj, relationList).Cast<IFeature>();
		}

		/// <summary>
		/// Gets the involved or related features.
		/// </summary>
		/// <param name="errorObject">The error object.</param>
		/// <param name="datasetLookup">The dataset lookup.</param>
		/// <param name="modelContext">The model context.</param>
		/// <param name="qualityConditionRepository">The quality condition repository.</param>
		/// <returns></returns>
		/// <remarks>
		/// Must be called within a domain transaction
		/// </remarks>
		[NotNull]
		public static IList<IFeature> GetInvolvedOrRelatedFeatures(
			[NotNull] IObject errorObject,
			[NotNull] IDatasetLookup datasetLookup,
			[NotNull] IModelContext modelContext,
			[NotNull] IQualityConditionRepository qualityConditionRepository)
		{
			Assert.ArgumentNotNull(errorObject, nameof(errorObject));
			Assert.ArgumentNotNull(datasetLookup, nameof(datasetLookup));
			Assert.ArgumentNotNull(modelContext, nameof(modelContext));

			var list = new List<IFeature>();

			IErrorDataset errorTable = GetErrorDataset(errorObject, datasetLookup);

			IErrorObject errorRow = CreateErrorObject(errorObject, errorTable, null);

			foreach (IRow involvedRow in
			         GetInvolvedRows(errorRow, modelContext, qualityConditionRepository))
			{
				var feature = involvedRow as IFeature;

				if (feature != null)
				{
					list.Add(feature);
				}
				else
				{
					list.AddRange(GetDerivedTableGeometryFeatures((IObject) involvedRow,
						              datasetLookup,
						              modelContext));
				}
			}

			return list;
		}

		[NotNull]
		public static string GetInvolvedObjectsString(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] QaError qaError,
			[NotNull] ICollection<ITest> tests,
			int maxLength,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver)
		{
			IEnumerable<InvolvedRow> involvedRowsWithDatasetNames =
				GetInvolvedRows(qaError,
				                qualityCondition,
				                datasetResolver);

			if (tests.Count == 1)
			{
				return RowParser.Format(involvedRowsWithDatasetNames, maxLength);
			}

			var testIndex = 0;
			foreach (ITest test in tests)
			{
				if (test == qaError.Test)
				{
					return RowParser.Format(test, testIndex, involvedRowsWithDatasetNames,
					                        maxLength);
				}

				testIndex++;
			}

			throw new InvalidProgramException(
				string.Format("Test {0} not found in QualityCondition {1}",
				              qaError.Test.GetType(), qualityCondition.Name));
		}

		[NotNull]
		private static IEnumerable<IRow> GetInvolvedRows(
			[NotNull] IErrorObject errorObject,
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IQualityConditionRepository qualityConditionRepository,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver)
		{
			IList<InvolvedRow> involved = RowParser.Parse(errorObject.RawInvolvedObjects);

			QualityCondition qualityCondition = GetQualityCondition(errorObject,
				qualityConditionRepository);

			if (qualityCondition == null)
			{
				yield break;
			}

			foreach (KeyValuePair<string, IList<long>> pair in
			         GetInvolvedObjectIDsByTableName(involved))
			{
				string tableName = pair.Key;
				IList<long> objectIDs = pair.Value;

				IObjectDataset dataset =
					datasetResolver.GetDatasetByInvolvedRowTableName(tableName, qualityCondition);
				if (dataset == null)
				{
					continue;
				}

				ITable table;
				try
				{
					table = datasetContext.OpenTable(dataset);
				}
				catch (Exception e)
				{
					_msg.WarnFormat("Error getting involved rows for table {0}: {1}", tableName,
					                e.Message);
					continue;
				}

				if (table == null)
				{
					continue;
				}

				IEnumerable<IRow> rows = TryGetRows(table, objectIDs);

				if (rows == null)
				{
					// error already logged in TryGetRows()
					continue;
				}

				foreach (IRow row in rows)
				{
					if (GdbObjectUtils.IsDeleted(row))
					{
						// don't return deleted rows
						continue;
					}

					yield return row;
				}
			}
		}

		[NotNull]
		private static IEnumerable<KeyValuePair<string, IList<long>>>
			GetInvolvedObjectIDsByTableName(
				[NotNull] IEnumerable<InvolvedRow> involvedRows)
		{
			Assert.ArgumentNotNull(involvedRows, nameof(involvedRows));

			var result = new Dictionary<string, IList<long>>(
				StringComparer.InvariantCultureIgnoreCase);

			foreach (InvolvedRow involvedRow in involvedRows)
			{
				if (involvedRow.RepresentsEntireTable)
				{
					continue;
				}

				IList<long> list;
				if (! result.TryGetValue(involvedRow.TableName, out list))
				{
					list = new List<long>();
					result.Add(involvedRow.TableName, list);
				}

				if (! list.Contains(involvedRow.OID))
				{
					list.Add(involvedRow.OID);
				}
			}

			return result;
		}

		[CanBeNull]
		private static QualityCondition GetQualityCondition(
			[NotNull] IErrorObject errorObject,
			[NotNull] IRepository<QualityCondition> qualityConditionRepository)
		{
			return errorObject.QualityConditionId.HasValue
				       ? qualityConditionRepository.Get(
					       errorObject.QualityConditionId.Value)
				       : null;
		}

		/// <summary>
		/// Gets the derived table geometry features.
		/// </summary>
		/// <param name="obj">The object in a table without geometry.</param>
		/// <param name="datasetLookup">The dataset lookup.</param>
		/// <param name="modelContext">The model context.</param>
		/// <returns></returns>
		/// <remarks>Must be called within a domain transaction</remarks>
		[NotNull]
		private static IEnumerable<IFeature> GetDerivedTableGeometryFeatures(
			[NotNull] IObject obj,
			[NotNull] IDatasetLookup datasetLookup,
			[NotNull] IModelContext modelContext)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));
			Assert.ArgumentNotNull(datasetLookup, nameof(datasetLookup));

			ObjectDataset dataset = datasetLookup.GetDataset(obj);
			if (dataset == null)
			{
				yield break;
			}

			foreach (IFeature feature in GetDerivedTableGeometryFeatures(
				         obj, dataset, modelContext))
			{
				yield return feature;
			}
		}

		[CanBeNull]
		private static IEnumerable<IRow> TryGetRows(
			[NotNull] ITable table,
			[NotNull] ICollection<long> objectIDs)
		{
			if (objectIDs.Count == 0)
			{
				return null;
			}

			const bool recycle = false;

			try
			{
				return GdbQueryUtils.GetRowsByObjectIds(table, objectIDs, recycle);
			}
			catch (Exception e)
			{
				_msg.WarnFormat(
					"Error getting involved rows for table {0}: {1} (see log for details)",
					DatasetUtils.GetName(table),
					e.Message);
				using (_msg.IncrementIndentation())
				{
					_msg.DebugFormat("Object IDs of involved rows: {0}",
					                 StringUtils.Concatenate(objectIDs, ","));
					LogTableDebugInfo(table);
				}

				_msg.WarnFormat("Trying again using alternative query method");

				// try again using different fetch method (where clause)
				try
				{
					return GdbQueryUtils.GetRowsInList(table, table.OIDFieldName,
					                                   objectIDs, recycle);
				}
				catch (Exception e2)
				{
					_msg.WarnFormat("Error getting involved rows for table {0}: {1}",
					                DatasetUtils.GetName(table),
					                e2.Message);
					using (_msg.IncrementIndentation())
					{
						_msg.WarnFormat("Object IDs of involved rows: {0}",
						                StringUtils.Concatenate(objectIDs, ","));
					}

					return null;
				}
			}
		}

		private static void LogTableDebugInfo([NotNull] ITable table)
		{
			try
			{
				_msg.DebugFormat("Table name: {0}", DatasetUtils.GetName(table));
				_msg.DebugFormat("Workspace: {0}",
				                 WorkspaceUtils.GetConnectionString(
					                 DatasetUtils.GetWorkspace(table), true));
				_msg.DebugFormat("OID field name: {0}", table.OIDFieldName);
				_msg.DebugFormat("Registered with geodatabase: {0}",
				                 DatasetUtils.IsRegisteredAsObjectClass(table));
				_msg.DebugFormat("Registered as versioned: {0}",
				                 DatasetUtils.IsVersioned((IDataset) table));
			}
			catch (Exception)
			{
				_msg.DebugFormat("Error logging debug info about table");
			}
		}

		[NotNull]
		private static IEnumerable<InvolvedRow> GetInvolvedRows(
			[NotNull] QaError qaError,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver)
		{
			foreach (InvolvedRow involvedRow in qaError.InvolvedRows)
			{
				IObjectDataset dataset =
					datasetResolver.GetDatasetByInvolvedRowTableName(
						involvedRow.TableName, qualityCondition);

				if (dataset == null)
				{
					// TOP-5874: Some transformers cannot provide the correct table, such as TrMakeTable
					_msg.InfoFormat("Unable to resolve dataset {0} for quality condition {1}",
					                involvedRow.TableName, qualityCondition.Name);
					yield break;
				}

				//Assert.NotNull(dataset,
				//               "Unable to resolve dataset {0} for quality condition {1}",
				//               involvedRow.TableName, qualityCondition.Name);

				yield return new InvolvedRow(dataset.Name, involvedRow.OID);
			}
		}
	}
}
