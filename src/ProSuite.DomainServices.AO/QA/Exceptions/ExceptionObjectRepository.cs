using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Diagnostics;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.Properties;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionObjectRepository : IExceptionObjectRepository
	{
		[NotNull] private readonly IWorkspace _workspace;
		[NotNull] private readonly IIssueTableFields _issueTableFields;
		[NotNull] private readonly IDatasetContext _datasetContext;
		[NotNull] private readonly IQualityConditionObjectDatasetResolver _datasetResolver;
		[CanBeNull] private readonly IGeometry _areaOfInterest;
		[CanBeNull] private readonly IGeometry _expandedAreaOfInterest;

		[NotNull] private readonly ExceptionStatistics _exceptionStatistics;

		[CanBeNull] private ExceptionObjectEvaluator _evaluator;

		[NotNull] private readonly Dictionary<string, string> _featureClassNames;

		[NotNull] private readonly string _rowClassName;
		[NotNull] private readonly string _rowClassAliasName;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructor

		public ExceptionObjectRepository(
			[NotNull] IWorkspace workspace,
			[NotNull] IIssueTableFields issueTableFields,
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[CanBeNull] IGeometry areaOfInterest)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(issueTableFields, nameof(issueTableFields));
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));

			_workspace = workspace;
			_issueTableFields = issueTableFields;
			_datasetContext = datasetContext;
			_datasetResolver = datasetResolver;
			_areaOfInterest = areaOfInterest;

			_featureClassNames =
				new Dictionary<string, string>
				{
					{ "IssuePolygons", LocalizableStrings.ExceptionLayerName_Polygon },
					{ "IssueLines", LocalizableStrings.ExceptionLayerName_Polyline },
					{ "IssuePoints", LocalizableStrings.ExceptionLayerName_Multipoint },
					{ "IssueMultiPatches", LocalizableStrings.ExceptionLayerName_MultiPatch }
				};
			_rowClassName = "IssueRows";
			_rowClassAliasName = LocalizableStrings.ExceptionStandaloneTableName;

			_expandedAreaOfInterest = GetExpandedAreaOfInterest(areaOfInterest);

			_exceptionStatistics = new ExceptionStatistics(workspace);

			IsShapefileWorkspace = WorkspaceUtils.IsShapefileWorkspace(workspace);
		}

		#endregion

		public bool IsShapefileWorkspace { get; }

		public void ReadExceptions(
			[NotNull] ICollection<QualityCondition> qualityConditions,
			ShapeMatchCriterion defaultShapeMatchCriterion,
			ExceptionObjectStatus defaultExceptionObjectStatus,
			[CanBeNull] InvolvedObjectsMatchCriteria involvedObjectsMatchCriteria)
		{
			Assert.ArgumentNotNull(qualityConditions, nameof(qualityConditions));

			HashSet<string> qualityConditionUuids =
				GetQualityConditionUuids(qualityConditions);
			IDictionary<Guid, QualityCondition> conditionsByUuid =
				qualityConditions.ToDictionary(condition => new Guid(condition.Uuid));

			var memoryUsageInfo = new MemoryUsageInfo();

			ICollection<ExceptionObject> exceptionObjects;
			using (_msg.IncrementIndentation("Reading exceptions"))
			{
				Stopwatch readWatch = _msg.DebugStartTiming();

				exceptionObjects = ReadExceptions(qualityConditionUuids,
				                                  new AlternateKeyConverterProvider(
					                                  conditionsByUuid,
					                                  _datasetResolver,
					                                  _datasetContext),
				                                  defaultShapeMatchCriterion,
				                                  defaultExceptionObjectStatus).ToList();

				_msg.DebugStopTiming(readWatch, "{0:N0} exception(s) read",
				                     exceptionObjects.Count);
			}

			_msg.InfoFormat(
				_areaOfInterest == null
					? "{0:N0} active exception object(s) read for {1:N0} verified quality condition(s)"
					: "{0:N0} active exception object(s) read for area of interest and {1:N0} verified quality condition(s)",
				exceptionObjects.Count, qualityConditionUuids.Count);

			_msg.DebugFormat("Memory usage after reading exception objects: {0}",
			                 memoryUsageInfo.Refresh());

			Stopwatch translateWatch = _msg.DebugStartTiming();

			TranslateAlternateKeys(exceptionObjects, conditionsByUuid);

			_msg.DebugStopTiming(translateWatch, "Translation alternate keys --> object ids");

			_evaluator = new ExceptionObjectEvaluator(exceptionObjects,
			                                          conditionsByUuid,
			                                          _exceptionStatistics,
			                                          _datasetResolver,
			                                          involvedObjectsMatchCriteria,
			                                          _areaOfInterest);
		}

		[CanBeNull]
		private static IGeometry GetExpandedAreaOfInterest(
			[CanBeNull] IGeometry areaOfInterest)
		{
			if (areaOfInterest == null || areaOfInterest.IsEmpty)
			{
				return null;
			}

			double xyTolerance = GeometryUtils.GetXyTolerance(areaOfInterest);

			var envelope = areaOfInterest as IEnvelope;
			if (envelope != null)
			{
				IEnvelope expanded = GeometryFactory.Clone(envelope);
				expanded.Expand(xyTolerance, xyTolerance, asRatio: false);

				return expanded;
			}

			double densifyDeviation = xyTolerance * 5;
			double bufferDistance = xyTolerance * 10;

			IPolygon buffer = AreaOfInterestFactory.CreateBuffer(
				new[] { areaOfInterest },
				bufferDistance,
				densifyDeviation);

			GeometryUtils.AllowIndexing(buffer);

			return buffer;
		}

		public IExceptionStatistics ExceptionStatistics => _exceptionStatistics;

		public IExceptionObjectEvaluator ExceptionObjectEvaluator => _evaluator;

		[NotNull]
		public IList<IExceptionDataset> ExportExceptions(
			[NotNull] IFeatureWorkspace targetWorkspace,
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			Assert.AreEqual(IsShapefileWorkspace,
			                WorkspaceUtils.IsShapefileWorkspace((IWorkspace) targetWorkspace),
			                "source/target ws type mismatch");

			HashSet<string> qualityConditionUuids =
				GetQualityConditionUuids(qualityConditions);

			IIssueTableFieldManagement fields =
				IssueTableFieldsFactory.GetIssueTableFields(addExceptionFields: true,
				                                            useDbfFieldNames:
				                                            IsShapefileWorkspace,
				                                            addManagedExceptionFields: true);

			List<KeyValuePair<string, string>> tableNames = _featureClassNames.ToList();
			tableNames.Add(
				new KeyValuePair<string, string>(_rowClassName, _rowClassAliasName));

			var result = new List<IExceptionDataset>();
			foreach (KeyValuePair<string, string> pair in tableNames)
			{
				string tableName = pair.Key;
				string aliasName = pair.Value;

				ITable table = OpenTable(_workspace, tableName);
				if (table == null)
				{
					continue;
				}

				result.Add(ExportTable(targetWorkspace, table,
				                       qualityConditionUuids,
				                       fields, aliasName));
			}

			return result;

			// in issue map:
			// - add group layer for exceptions
			// - add exception layer per quality condition (optionally grouped by category?)
			// - if origin was specified: create unique value renderer
			// - else: single symbol (green?)
		}

		[NotNull]
		private IExceptionDataset ExportTable(
			[NotNull] IFeatureWorkspace targetWorkspace,
			[NotNull] ITable sourceTable,
			[NotNull] ICollection<string> qualityConditionUuids,
			[NotNull] IIssueTableFieldManagement fields,
			[NotNull] string aliasName,
			int autoCommitInterval = 1000)
		{
			var sourceFeatureClass = sourceTable as IFeatureClass;

			IFields exportFields = CreateExportFields(sourceTable, fields);

			string tableName = DatasetUtils.GetName(sourceTable);

			ITable exportTable = sourceFeatureClass != null
				                     ? (ITable) DatasetUtils.CreateSimpleFeatureClass(
					                     targetWorkspace, tableName, exportFields)
				                     : DatasetUtils.CreateTable(targetWorkspace,
				                                                tableName, null,
				                                                exportFields);

			DatasetUtils.TrySetAliasName(exportTable, aliasName);

			int origOidFieldIndex = fields.GetIndex(
				IssueAttribute.ExportedExceptionObjectId, exportTable);
			int usageCountFieldIndex = fields.GetIndex(
				IssueAttribute.ExportedExceptionUsageCount, exportTable);

			IRowBuffer rowBuffer = exportTable.CreateRowBuffer();

			IDictionary<int, int> indexMatrix =
				GdbObjectUtils.CreateMatchingIndexMatrix(sourceTable, exportTable,
				                                         fieldComparison:
				                                         FieldComparison.FieldName);

			var factory = new ExceptionObjectFactory(sourceTable, fields,
			                                         includeManagedExceptionAttributes: true,
			                                         areaOfInterest: _areaOfInterest);
			var rowCount = 0;
			ICursor cursor = exportTable.Insert(useBuffering: true);
			try
			{
				var pendingCount = 0;

				foreach (IRow row in GetRows(sourceTable, qualityConditionUuids))
				{
					ExceptionObject exceptionObject = factory.CreateExceptionObject(row);

					if (exceptionObject.Status != ExceptionObjectStatus.Active)
					{
						// export only active exceptions
						continue;
					}

					int usageCount = GetUsageCount(exceptionObject);

					if (usageCount == 0 && ! exceptionObject.IntersectsAreaOfInterest)
					{
						// ignore unused exception if outside the AOI (but within the tolerance buffer)
						continue;
					}

					if (sourceFeatureClass != null)
					{
						var feature = (IFeature) row;
						((IFeatureBuffer) rowBuffer).Shape = feature.ShapeCopy;
					}

					foreach (KeyValuePair<int, int> pair in
					         indexMatrix.Where(p => p.Key >= 0 && p.Value >= 0))
					{
						rowBuffer.Value[pair.Key] = row.Value[pair.Value];
					}

					rowBuffer.Value[origOidFieldIndex] = row.OID;
					rowBuffer.Value[usageCountFieldIndex] = usageCount;

					cursor.InsertRow(rowBuffer);
					pendingCount++;
					rowCount++;

					if (pendingCount >= autoCommitInterval)
					{
						cursor.Flush();
						pendingCount = 0;
					}
				}

				if (pendingCount > 0)
				{
					cursor.Flush();
				}
			}
			finally
			{
				ComUtils.ReleaseComObject(cursor);
			}

			var exportFeatureClass = exportTable as IFeatureClass;

			return exportFeatureClass == null
				       ? new ExceptionTable((IObjectClass) exportTable, fields, rowCount)
				       : (IExceptionDataset)
				       new ExceptionFeatureClass(exportFeatureClass, fields, rowCount);
		}

		private int GetUsageCount([NotNull] ExceptionObject exceptionObject)
		{
			return _evaluator?.GetUsageCount(exceptionObject) ?? 0;
		}

		[NotNull]
		private static IFields CreateExportFields([NotNull] ITable sourceTable,
		                                          [NotNull] IIssueTableFieldManagement
			                                          fields)
		{
			var result = (IFields) ((IClone) sourceTable.Fields).Clone();

			var fieldsEdit = (IFieldsEdit) result;

			fieldsEdit.AddField(fields.CreateField(IssueAttribute.ExportedExceptionObjectId));
			fieldsEdit.AddField(
				fields.CreateField(IssueAttribute.ExportedExceptionUsageCount));

			return result;
		}

		private void TranslateAlternateKeys(
			[NotNull] IEnumerable<ExceptionObject> exceptions,
			[NotNull] IDictionary<Guid, QualityCondition> conditionsByUuid)
		{
			IDictionary<TableObjectIdLookupKey, List<InvolvedTable>> involvedTablesByLookupKey;
			IDictionary<TableObjectIdLookupKey, TableObjectIdLookup> tableObjectIdLookups =
				GetTableObjectIdLookups(exceptions, conditionsByUuid,
				                        out involvedTablesByLookupKey);

			foreach (KeyValuePair<TableObjectIdLookupKey, List<InvolvedTable>> pair in
			         involvedTablesByLookupKey)
			{
				TableObjectIdLookupKey key = pair.Key;
				List<InvolvedTable> involvedTables = pair.Value;

				TableObjectIdLookup lookup = tableObjectIdLookups[key];

				_msg.DebugFormat("Translating {0:N0} key value(s) for field {1} in dataset {2}",
				                 lookup.KeyCount, key.KeyFieldName, key.ObjectDataset.Name);

				foreach (InvolvedTable involvedTable in involvedTables)
				{
					involvedTable.ReplaceRowReferences(
						LookupObjectIDRowReferences(involvedTable, lookup));
				}
			}
		}

		[NotNull]
		private static IEnumerable<OIDRowReference> LookupObjectIDRowReferences(
			[NotNull] InvolvedTable involvedTable, [NotNull] TableObjectIdLookup lookup)
		{
			var result = new List<OIDRowReference>(involvedTable.RowReferences.Count);

			foreach (RowReference rowReference in involvedTable.RowReferences)
			{
				var alternateKeyRowReference = (AlternateKeyRowReference) rowReference;

				if (lookup.TryLookupObjectId(alternateKeyRowReference.Key, out long oid))
				{
					result.Add(new OIDRowReference(oid));
				}
			}

			return result;
		}

		[NotNull]
		private IDictionary<TableObjectIdLookupKey, TableObjectIdLookup>
			GetTableObjectIdLookups(
				[NotNull] IEnumerable<ExceptionObject> exceptions,
				[NotNull] IDictionary<Guid, QualityCondition> conditionsByUuid,
				[NotNull] out IDictionary<TableObjectIdLookupKey, List<InvolvedTable>>
					involvedTablesByLookupKey)
		{
			var result = new Dictionary<TableObjectIdLookupKey, TableObjectIdLookup>();
			involvedTablesByLookupKey =
				new Dictionary<TableObjectIdLookupKey, List<InvolvedTable>>();

			foreach (ExceptionObject exception in exceptions)
			{
				QualityCondition qualityCondition = null;

				foreach (InvolvedTable involvedTable in exception.InvolvedTables)
				{
					if (involvedTable.KeyField == null)
					{
						// already uses oids for row references
						continue;
					}

					if (qualityCondition == null)
					{
						if (! conditionsByUuid.TryGetValue(exception.QualityConditionUuid,
						                                   out qualityCondition))
						{
							throw new InvalidOperationException(
								string.Format(
									"Loaded exception refers to a quality condition that was not verified: {0}",
									exception.QualityConditionUuid));
						}
					}

					// collect alternate keys by table and key field
					IObjectDataset objectDataset =
						_datasetResolver.GetDatasetByInvolvedRowTableName(
							involvedTable.TableName, qualityCondition);

					if (objectDataset == null)
					{
						// quality condition does not know about this table name 
						// - the quality condition may have been changed since the exception object
						//   was defined
						_exceptionStatistics.ReportExceptionInvolvingUnknownTable(
							exception, involvedTable.TableName, qualityCondition);
						continue;
					}

					var key = new TableObjectIdLookupKey(objectDataset, involvedTable.KeyField);

					TableObjectIdLookup tableObjectIdLookup;
					if (! result.TryGetValue(key, out tableObjectIdLookup))
					{
						tableObjectIdLookup = CreateTableObjectIdLookup(objectDataset,
							involvedTable.KeyField);

						result.Add(key, tableObjectIdLookup);
					}

					if (tableObjectIdLookup == null)
					{
						// table not found in dataset context
						// (the quality condition should already have been ignored - or failed - before)
						_exceptionStatistics.ReportExceptionInvolvingUnknownTable(
							exception, involvedTable.TableName, qualityCondition);
						continue;
					}

					foreach (RowReference rowReferences in involvedTable.RowReferences)
					{
						tableObjectIdLookup.AddKey(Assert.NotNull(rowReferences.Key));
					}

					List<InvolvedTable> involvedTablesForLookup;
					if (! involvedTablesByLookupKey.TryGetValue(key, out involvedTablesForLookup))
					{
						involvedTablesForLookup = new List<InvolvedTable>();

						involvedTablesByLookupKey.Add(key, involvedTablesForLookup);
					}

					involvedTablesForLookup.Add(involvedTable);
				}
			}

			return result;
		}

		[CanBeNull]
		private TableObjectIdLookup CreateTableObjectIdLookup(
			[NotNull] IObjectDataset objectDataset, [NotNull] string keyFieldName)
		{
			ITable table = _datasetContext.OpenTable(objectDataset);

			if (table == null)
			{
				return null;
			}

			return new TableObjectIdLookup(table,
			                               keyFieldName,
			                               _exceptionStatistics);
		}

		[NotNull]
		private static HashSet<string> GetQualityConditionUuids(
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (QualityCondition qualityCondition in qualityConditions)
			{
				if (StringUtils.IsNotEmpty(qualityCondition.Uuid))
				{
					result.Add(qualityCondition.Uuid);
				}
			}

			return result;
		}

		[NotNull]
		private IEnumerable<ExceptionObject> ReadExceptions(
			[NotNull] ICollection<string> qualityConditionUuids,
			[NotNull] IAlternateKeyConverterProvider alternateKeyConverterProvider,
			ShapeMatchCriterion defaultShapeMatchCriterion = ShapeMatchCriterion.EqualEnvelope,
			ExceptionObjectStatus defaultStatus = ExceptionObjectStatus.Active)
		{
			// read exception objects 
			// - that intersect the area of interest (if defined)
			// - for the specified quality conditions

			foreach (string featureClassName in _featureClassNames.Keys)
			{
				foreach (ExceptionObject exceptionObject in ReadExceptions(
					         featureClassName,
					         qualityConditionUuids,
					         alternateKeyConverterProvider,
					         defaultShapeMatchCriterion: defaultShapeMatchCriterion,
					         defaultStatus: defaultStatus))
				{
					yield return exceptionObject;
				}
			}

			foreach (ExceptionObject exceptionObject in
			         ReadExceptions(_rowClassName, qualityConditionUuids,
			                        alternateKeyConverterProvider))
			{
				yield return exceptionObject;
			}
		}

		[NotNull]
		private IEnumerable<ExceptionObject> ReadExceptions(
			[NotNull] string tableName,
			[NotNull] ICollection<string> qualityConditionUuids,
			[NotNull] IAlternateKeyConverterProvider alternateKeyConverterProvider,
			bool applyConditionQueryFilter = true,
			ShapeMatchCriterion defaultShapeMatchCriterion = ShapeMatchCriterion.EqualEnvelope,
			ExceptionObjectStatus defaultStatus = ExceptionObjectStatus.Active)
		{
			ITable table = OpenTable(_workspace, tableName);

			if (table == null)
			{
				yield break;
			}

			var factory = new ExceptionObjectFactory(table, _issueTableFields,
			                                         alternateKeyConverterProvider,
			                                         defaultShapeMatchCriterion,
			                                         defaultStatus,
			                                         _areaOfInterest);

			var inactiveCount = 0;
			var activeCount = 0;
			foreach (IRow row in GetRows(table, qualityConditionUuids, factory.FieldNames,
			                             applyConditionQueryFilter))
			{
				ExceptionObject exceptionObject = factory.CreateExceptionObject(row);

				if (exceptionObject.Status == ExceptionObjectStatus.Inactive)
				{
					inactiveCount++;
					_exceptionStatistics.ReportInactiveException(exceptionObject);
				}
				else
				{
					activeCount++;
					yield return exceptionObject;
				}
			}

			string inactiveMsg;
			if (inactiveCount > 0)
			{
				inactiveMsg = string.Format(
					inactiveCount == 1
						? " ({0:N0} exception object is inactive)"
						: " ({0:N0} exception objects are inactive)",
					inactiveCount);
			}
			else
			{
				inactiveMsg = string.Empty;
			}

			_msg.InfoFormat(activeCount == 1
				                ? "{0}: {1:N0} active exception object read{2}"
				                : "{0}: {1:N0} active exception objects read{2}",
			                tableName, activeCount, inactiveMsg);
		}

		[NotNull]
		private IEnumerable<IRow> GetRows(
			[NotNull] ITable table,
			[NotNull] ICollection<string> qualityConditionUuids,
			[CanBeNull] IEnumerable<string> fieldNames = null,
			bool applyConditionQueryFilter = true)
		{
			IQueryFilter queryFilter = GetQueryFilter(table, fieldNames,
			                                          _expandedAreaOfInterest);

			string uuidFieldName = _issueTableFields.GetName(
				IssueAttribute.QualityConditionUuid);
			int uuidFieldIndex = _issueTableFields.GetIndex(
				IssueAttribute.QualityConditionUuid, table);

			const bool recycle = true;
			IEnumerable<IRow> rows = applyConditionQueryFilter
				                         ? GdbQueryUtils.GetRowsInList(table,
					                         uuidFieldName,
					                         qualityConditionUuids,
					                         recycle, queryFilter)
				                         : GdbQueryUtils.GetRows(table, queryFilter, recycle);

			foreach (IRow row in rows)
			{
				if (! applyConditionQueryFilter)
				{
					object uuid = row.Value[uuidFieldIndex];

					var uuidString = uuid as string;

					if (uuidString == null || ! qualityConditionUuids.Contains(uuidString))
					{
						continue;
					}
				}

				yield return row;
			}
		}

		[CanBeNull]
		private static ITable OpenTable([NotNull] IWorkspace workspace,
		                                [NotNull] string tableName)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(tableName, nameof(tableName));

			var featureWorkspace = (IFeatureWorkspace) workspace;

			try
			{
				return featureWorkspace.OpenTable(tableName);
			}
			catch (COMException e)
			{
				if (e.ErrorCode == (int) fdoError.FDO_E_TABLE_NOT_FOUND)
				{
					return null;
				}

				throw;
			}
		}

		[CanBeNull]
		private static IQueryFilter GetQueryFilter(
			[NotNull] ITable table,
			[CanBeNull] IEnumerable<string> fieldNames,
			[CanBeNull] IGeometry areaOfInterest = null)
		{
			var featureClass = table as IFeatureClass;
			IQueryFilter queryFilter;
			if (featureClass == null || areaOfInterest == null || areaOfInterest.IsEmpty)
			{
				queryFilter = new QueryFilterClass();
			}
			else
			{
				ISpatialFilter spatialFilter = new SpatialFilterClass();

				spatialFilter.set_GeometryEx(GeometryFactory.Clone(areaOfInterest), true);
				spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

				spatialFilter.OutputSpatialReference[featureClass.ShapeFieldName] =
					areaOfInterest.SpatialReference;

				queryFilter = spatialFilter;
			}

			if (fieldNames != null)
			{
				GdbQueryUtils.SetSubFields(queryFilter, fieldNames);
			}
			else
			{
				queryFilter.SubFields = "*";
			}

			return queryFilter;
		}

		#region Nested types

		private class TableObjectIdLookup
		{
			[NotNull] private readonly Dictionary<object, long> _oidMap =
				new Dictionary<object, long>();

			[NotNull] private readonly HashSet<object> _keysRequiringLookup =
				new HashSet<object>();

			[NotNull] private readonly ITable _table;
			[NotNull] private readonly string _keyFieldName;
			[NotNull] private readonly ExceptionStatistics _statistics;
			private readonly int _keyFieldIndex;

			public TableObjectIdLookup([NotNull] ITable table,
			                           [NotNull] string keyFieldName,
			                           [NotNull] ExceptionStatistics statistics)
			{
				Assert.ArgumentNotNull(table, nameof(table));
				Assert.ArgumentNotNullOrEmpty(keyFieldName, nameof(keyFieldName));

				_table = table;
				_keyFieldName = keyFieldName;
				_statistics = statistics;
				_keyFieldIndex = table.FindField(keyFieldName);

				Assert.ArgumentCondition(_keyFieldIndex >= 0,
				                         string.Format("Key field not found: {0}", keyFieldName),
				                         nameof(keyFieldName));
			}

			public void AddKey([NotNull] object key)
			{
				_keysRequiringLookup.Add(key);
			}

			public int KeyCount => _keysRequiringLookup.Count;

			public bool TryLookupObjectId([NotNull] object key, out long oid)
			{
				if (_keysRequiringLookup.Count > 0)
				{
					LookupObjectIds();

					_keysRequiringLookup.Clear();
				}

				return _oidMap.TryGetValue(key, out oid);
			}

			private void LookupObjectIds()
			{
				foreach (KeyValuePair<object, long> pair in LookupObjectIds(_keysRequiringLookup))
				{
					if (_oidMap.ContainsKey(pair.Key))
					{
						_statistics.ReportNonUniqueKey(_table, pair.Key);
					}
					else
					{
						_oidMap.Add(pair.Key, pair.Value);
					}
				}
			}

			[NotNull]
			private IEnumerable<KeyValuePair<object, long>> LookupObjectIds(
				[NotNull] IEnumerable<object> keys)
			{
				string oidFieldName = _table.OIDFieldName;

				var queryFilter = new QueryFilterClass();
				GdbQueryUtils.SetSubFields(queryFilter, oidFieldName, _keyFieldName);

				// NOTE: this assumes that the key values are of a compatible type for the key field
				const bool recycle = true;
				foreach (IRow row in GdbQueryUtils.GetRowsInList(
					         _table, _keyFieldName, keys, recycle, queryFilter))
				{
					yield return new KeyValuePair<object, long>(row.Value[_keyFieldIndex], row.OID);
				}
			}
		}

		private class TableObjectIdLookupKey : IEquatable<TableObjectIdLookupKey>
		{
			public TableObjectIdLookupKey([NotNull] IObjectDataset objectDataset,
			                              [NotNull] string keyFieldName)
			{
				ObjectDataset = objectDataset;
				KeyFieldName = keyFieldName.Trim().ToUpper();
			}

			[NotNull]
			public IObjectDataset ObjectDataset { get; }

			[NotNull]
			public string KeyFieldName { get; }

			public bool Equals(TableObjectIdLookupKey other)
			{
				if (ReferenceEquals(null, other))
				{
					return false;
				}

				if (ReferenceEquals(this, other))
				{
					return true;
				}

				return ObjectDataset.Equals(other.ObjectDataset) &&
				       string.Equals(KeyFieldName, other.KeyFieldName);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
				{
					return false;
				}

				if (ReferenceEquals(this, obj))
				{
					return true;
				}

				if (obj.GetType() != GetType())
				{
					return false;
				}

				return Equals((TableObjectIdLookupKey) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (ObjectDataset.GetHashCode() * 397) ^ KeyFieldName.GetHashCode();
				}
			}

			public override string ToString()
			{
				return $"ObjectDataset: {ObjectDataset}, KeyFieldName: {KeyFieldName}";
			}
		}

		#endregion
	}
}
