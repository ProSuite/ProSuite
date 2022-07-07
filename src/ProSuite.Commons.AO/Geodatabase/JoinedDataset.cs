using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class JoinedDataset : BackingDataset
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IFeatureClass _geometryEndClass;
		[NotNull] private readonly IObjectClass _otherEndClass;
		[NotNull] private readonly GdbFeatureClass _joinedSchema;

		private IDictionary<int, int> _geometryEndCopyMatrix;
		private IDictionary<int, int> _otherEndCopyMatrix;

		private readonly AssociationDescription _associationDescription;

		private readonly string
			_joinStrategy = Environment.GetEnvironmentVariable("PROSUITE_MEMORY_JOIN_STRATEGY");

		private readonly Dictionary<ITable, int> _tableRowStatistics =
			new Dictionary<ITable, int>(3);

		public JoinedDataset([NotNull] IRelationshipClass relationshipClass,
		                     [NotNull] IFeatureClass geometryEndClass,
		                     [NotNull] GdbFeatureClass joinedSchema)
			: this(AssociationDescriptionUtils.CreateAssociationDescription(relationshipClass),
			       geometryEndClass,
			       RelationshipClassUtils.GetOtherEndObjectClass(
				       relationshipClass, geometryEndClass),
			       joinedSchema) { }

		public JoinedDataset([NotNull] AssociationDescription associationDescription,
		                     [NotNull] IFeatureClass geometryEndClass,
		                     [NotNull] IObjectClass otherEndClass,
		                     [NotNull] GdbFeatureClass joinedSchema)
		{
			_geometryEndClass = geometryEndClass;
			_otherEndClass = otherEndClass;

			_associationDescription = associationDescription;

			_joinedSchema = joinedSchema;
		}

		public JoinType JoinType { get; set; } = JoinType.InnerJoin;

		public override IEnvelope Extent => ((IGeoDataset) _geometryEndClass).Extent;

		public override VirtualRow GetRow(int id)
		{
			throw new NotImplementedException();
		}

		public override int GetRowCount(IQueryFilter filter)
		{
			if (filter != null && ! string.IsNullOrEmpty(filter.WhereClause))
			{
				return Search(filter, true).Count();
			}

			GetKeyFieldNames(out string featureClassKeyField, out string otherClassKeyField);

			IDictionary<string, IList<IRow>> otherRows =
				GetOtherRowsByFeatureKey(filter, featureClassKeyField, otherClassKeyField,
				                         out int _);

			if (_associationDescription is ManyToManyAssociationDescription)
			{
				return otherRows.Values.Sum(v => v.Count);
			}
			else
			{
				// TODO: This could be optimized if the side of the primary key was known
				return PerformFinalGeoClassRead(
					otherRows, featureClassKeyField, filter).Count();
			}
		}

		public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
		{
			var filterHelper = FilterHelper.Create(_joinedSchema, filter?.WhereClause);

			// The filter's where clause must use the fully qualified rows. But the spatial
			// constraint is applied during the creation of the join.
			foreach (VirtualRow virtualRow in GetJoinedRows(filter))
			{
				if (filterHelper.Check(virtualRow))
				{
					yield return virtualRow;
				}
			}
		}

		/// <summary>
		/// Performs the join using the filter only by applying the spatial constraint.
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		private IEnumerable<VirtualRow> GetJoinedRows([CanBeNull] IQueryFilter filter)
		{
			if (filter != null && (! string.IsNullOrEmpty(filter.WhereClause)))
			{
				filter = (IQueryFilter) ((IClone) filter).Clone();
				filter.WhereClause = null;
			}

			GetKeyFieldNames(out string featureClassKeyField, out string otherClassKeyField);

			IDictionary<string, IList<IRow>> otherRowsByFeatureKey =
				GetOtherRowsByFeatureKey(filter, featureClassKeyField, otherClassKeyField,
				                         out int featureClassKeyIdx);

			foreach (IFeature feature in PerformFinalGeoClassRead(
				         otherRowsByFeatureKey, featureClassKeyField, filter))
			{
				string featureKeyValue = GetKeyValue(feature, featureClassKeyIdx);
				IList<IRow> otherRowList;
				if (JoinType == JoinType.InnerJoin)
				{
					Assert.NotNull(featureKeyValue);
					otherRowList = otherRowsByFeatureKey[featureKeyValue];
				}
				else
				{
					if (featureKeyValue == null ||
					    ! otherRowsByFeatureKey.TryGetValue(featureKeyValue, out otherRowList))
					{
						// No relationship or no relational integrity
						yield return CreateJoinedFeature(feature, null);
						continue;
					}
				}

				foreach (IRow otherRow in otherRowList)
				{
					yield return CreateJoinedFeature(feature, otherRow);
				}
			}
		}

		private IDictionary<string, IList<IRow>> GetOtherRowsByFeatureKey(
			[CanBeNull] IQueryFilter filter,
			[NotNull] string featureClassKeyField,
			[NotNull] string otherClassKeyField,
			out int featureClassKeyIdx)
		{
			Assert.NotNull(_otherEndClass);
			Assert.NotNull(otherClassKeyField);

			string originalSubfields = filter?.SubFields;
			if (filter != null)
				filter.SubFields = featureClassKeyField;

			// TODO: More testing, does not seem to make a difference:
			//esriSpatialRelEnum originalSpatialRel = esriSpatialRelEnum.esriSpatialRelUndefined;
			//if (filter is ISpatialFilter spatialFilter  &&
			//    spatialFilter.SpatialRel == esriSpatialRelEnum.esriSpatialRelIntersects)
			//{
			//	originalSpatialRel = spatialFilter.SpatialRel;
			//	spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			//}

			// TODO: Deal with where-clause

			featureClassKeyIdx = _geometryEndClass.FindField(featureClassKeyField);
			Assert.True(featureClassKeyIdx >= 0, $"Key field not found: {featureClassKeyIdx}");

			Stopwatch watch = _msg.DebugStartTiming();

			HashSet<string> fClassKeys = new HashSet<string>();

			// TODO: If the filter is null or it's no spatial filter: compare row count to determine
			//       which table should be queried first.

			int featureCount = 0;
			foreach (IFeature feature in GdbQueryUtils.GetFeatures(_geometryEndClass, filter, true))
			{
				featureCount++;

				string keyValue = GetKeyValue(feature, featureClassKeyIdx);

				if (keyValue != null)
				{
					fClassKeys.Add(keyValue);
				}
			}

			_msg.DebugStopTiming(watch, "Initial search found {0} geo-keys in {1} features",
			                     fClassKeys.Count, featureCount);

			IDictionary<string, IList<IRow>> otherRows =
				GetOtherRowListsByFeatureKey(otherClassKeyField, fClassKeys);

			// Revert the filter change:
			if (filter != null)
				filter.SubFields = originalSubfields;

			return otherRows;
		}

		private IEnumerable<IFeature> PerformFinalGeoClassRead(
			IDictionary<string, IList<IRow>> otherRowsByGeoKey,
			string featureClassKeyField,
			[CanBeNull] IQueryFilter filter)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			if (JoinType == JoinType.InnerJoin)
			{
				foreach (IFeature feature in PerformFinalGeoClassReadFilteredByKeys(
					         otherRowsByGeoKey, featureClassKeyField, filter))
				{
					yield return feature;
				}
			}
			else if (JoinType == JoinType.LeftJoin)
			{
				// All records from the left table:
				foreach (IFeature feature in GdbQueryUtils.GetFeatures(
					         _geometryEndClass, filter, true))
				{
					yield return feature;
				}
			}

			_msg.DebugStopTiming(watch, "Final read of geo-features with result processing");
		}

		private IEnumerable<IFeature> PerformFinalGeoClassReadFilteredByKeys(
			[NotNull] IDictionary<string, IList<IRow>> otherRowsByGeoKey,
			[NotNull] string featureClassKeyField,
			[CanBeNull] IQueryFilter filter)
		{
			int featureClassKeyIdx = _geometryEndClass.FindField(featureClassKeyField);
			Assert.True(featureClassKeyIdx >= 0, $"Key field not found: {featureClassKeyIdx}");

			bool clientSideKeyFiltering;
			if (_joinStrategy == "INDEX")
			{
				clientSideKeyFiltering = true;
			}
			else if (_joinStrategy == "FTS")
			{
				clientSideKeyFiltering = false;
			}
			else
			{
				// TODO: if number of keys is large-ish, compare with
				// initial read's feature count
				clientSideKeyFiltering = false;
			}

			IEnumerable<IRow> resultGeoFeatures = FetchRowsByKey(
				(ITable) _geometryEndClass, otherRowsByGeoKey.Keys, featureClassKeyField, true,
				clientSideKeyFiltering, filter);

			foreach (IRow row in resultGeoFeatures)
			{
				IFeature feature = (IFeature) row;

				yield return feature;
			}
		}

		private void GetKeyFieldNames(out string featureClassKeyField,
		                              out string otherClassKeyField)
		{
			if (_associationDescription is ForeignKeyAssociationDescription foreignKeyAssociation)
			{
				if (DatasetUtils.IsSameObjectClass(
					    _geometryEndClass, (IObjectClass) foreignKeyAssociation.ReferencedTable))
				{
					featureClassKeyField = foreignKeyAssociation.ReferencedKeyName;
					otherClassKeyField = foreignKeyAssociation.ReferencingKeyName;
				}
				else
				{
					featureClassKeyField = foreignKeyAssociation.ReferencingKeyName;
					otherClassKeyField = foreignKeyAssociation.ReferencedKeyName;
				}
			}
			else
			{
				ManyToManyAssociationDescription manyToManyAssociation =
					(ManyToManyAssociationDescription) _associationDescription;

				if (DatasetUtils.IsSameObjectClass(
					    _geometryEndClass, (IObjectClass) manyToManyAssociation.Table1))
				{
					featureClassKeyField = manyToManyAssociation.Table1KeyName;
					otherClassKeyField = manyToManyAssociation.Table2KeyName;
				}
				else
				{
					featureClassKeyField = manyToManyAssociation.Table2KeyName;
					otherClassKeyField = manyToManyAssociation.Table1KeyName;
				}
			}
		}

		private GdbFeature CreateJoinedFeature(IFeature feature, [CanBeNull] IRow otherRow)
		{
			var joinedValueList = new JoinedValueList {Readonly = true};

			joinedValueList.AddRow(feature, GeometryEndCopyMatrix);
			joinedValueList.AddRow(otherRow, OtherEndCopyMatrix);

			GdbFeature resultFeature = new GdbFeature(feature.OID, _joinedSchema,
			                                          joinedValueList);

			return resultFeature;
		}

		private IDictionary<string, IRow> GetOtherRowsByKey(
			[NotNull] string otherClassField,
			[NotNull] IEnumerable<string> keyValues)
		{
			// Get the non-feature-rows:
			int otherClassKeyIdx = _otherEndClass.FindField(otherClassField);
			string otherTableName = DatasetUtils.GetName(_otherEndClass);
			Assert.True(otherClassKeyIdx >= 0,
			            $"Key field {otherClassField} not found in {otherTableName}");

			IDictionary<string, IRow> otherRows = new Dictionary<string, IRow>();
			foreach (IRow row in GdbQueryUtils.GetRowsInList(
				         (ITable) _otherEndClass, otherClassField, keyValues, false))
			{
				object otherRowKey = row.get_Value(otherClassKeyIdx);
				Assert.True(otherRowKey != null && DBNull.Value != otherRowKey,
				            $"No key value in {otherTableName}");

				otherRows.Add(otherRowKey.ToString(), row);
			}

			return otherRows;
		}

		private IDictionary<string, IList<IRow>> GetOtherRowListsByFeatureKey(
			string otherClassKeyField,
			HashSet<string> fClassKeys)
		{
			IDictionary<string, IList<IRow>> result = new Dictionary<string, IList<IRow>>();

			// M:N - Get the bridge table rows:
			if (_associationDescription is ManyToManyAssociationDescription m2n)
			{
				Stopwatch watch = _msg.DebugStartTiming();

				Dictionary<string, List<string>> geoKeysByOtherKey =
					GeoKeysByOtherKeyManyToMany(fClassKeys, m2n);

				_msg.DebugStopTiming(
					watch,
					"Searched with {0} geo-keys, found {1} other keys.",
					fClassKeys.Count, geoKeysByOtherKey.Count);

				watch = _msg.DebugStartTiming();

				IDictionary<string, IRow> otherRowsByKey =
					GetOtherRowsByKey(otherClassKeyField, geoKeysByOtherKey.Keys);

				_msg.DebugStopTiming(watch, "Retrieved all {0} other rows.", otherRowsByKey.Count);

				foreach (KeyValuePair<string, List<string>> keyValuePair in geoKeysByOtherKey)
				{
					string otherKey = keyValuePair.Key;

					foreach (string geoKey in keyValuePair.Value)
					{
						if (! result.TryGetValue(geoKey, out IList<IRow> otherRowList))
						{
							otherRowList = new List<IRow>(5);

							result.Add(geoKey, otherRowList);
						}

						if (otherRowsByKey.ContainsKey(otherKey))
						{
							result[geoKey].Add(otherRowsByKey[otherKey]);
						}
					}
				}

				return result;
			}

			// Get the non-feature-rows:
			foreach (KeyValuePair<string, IRow> keyValuePair in GetOtherRowsByKey(
				         otherClassKeyField, fClassKeys))
			{
				string otherRowKey = keyValuePair.Key;

				if (! result.TryGetValue(otherRowKey, out IList<IRow> otherRowList))
				{
					otherRowList = new List<IRow>(3);

					result.Add(otherRowKey, otherRowList);
				}

				otherRowList.Add(keyValuePair.Value);
			}

			return result;
		}

		private Dictionary<string, List<string>> GeoKeysByOtherKeyManyToMany(
			[NotNull] HashSet<string> geoKeys,
			[NotNull] ManyToManyAssociationDescription m2nAssociation)
		{
			Dictionary<string, List<string>> geoKeysByOtherKey =
				new Dictionary<string, List<string>>();

			ITable bridgeTable = m2nAssociation.AssociationTable;
			string bridgeTableGeoKeyField;
			string bridgeTableOtherKeyField;
			if (DatasetUtils.IsSameObjectClass(_geometryEndClass,
			                                   (IObjectClass) m2nAssociation.Table1))
			{
				bridgeTableGeoKeyField = m2nAssociation.AssociationTableKey1;
				bridgeTableOtherKeyField = m2nAssociation.AssociationTableKey2;
			}
			else
			{
				bridgeTableGeoKeyField = m2nAssociation.AssociationTableKey2;
				bridgeTableOtherKeyField = m2nAssociation.AssociationTableKey1;
			}

			string bridgeTableName = DatasetUtils.GetName(bridgeTable);

			int bridgeTableOtherKeyIdx = bridgeTable.FindField(bridgeTableOtherKeyField);
			Assert.True(bridgeTableOtherKeyIdx >= 0,
			            $"Key field {bridgeTableOtherKeyField} not found in {bridgeTable}");

			int bridgeTableGeoKeyIdx = bridgeTable.FindField(bridgeTableGeoKeyField);
			Assert.True(bridgeTableGeoKeyIdx >= 0,
			            $"Key field {bridgeTableGeoKeyField} not found in {bridgeTable}");

			foreach (IRow row in FetchBridgeTableRowsByKey(geoKeys, bridgeTable,
			                                               bridgeTableGeoKeyField))
			{
				// The primary key of the other table:
				object bridgeOtherKey = row.get_Value(bridgeTableOtherKeyIdx);
				Assert.True(bridgeOtherKey != null && DBNull.Value != bridgeOtherKey,
				            $"Missing other key value in {bridgeTableName}");
				string bridgeOtherKeyValue = Convert.ToString(bridgeOtherKey);

				object bridgeGeoKey = row.get_Value(bridgeTableGeoKeyIdx);
				Assert.True(bridgeGeoKey != null && DBNull.Value != bridgeGeoKey,
				            $"Missing feature key value in {bridgeTableName}");

				// The primary key of the geo table:
				string bridgeGeoKeyValue = Convert.ToString(bridgeGeoKey);

				// Double check, the fetch might use a FTS or deliver the full cache:
				if (! geoKeys.Contains(bridgeGeoKeyValue))
				{
					continue;
				}

				// Collect the list of geo-end keys per other-table key
				if (! geoKeysByOtherKey.TryGetValue(bridgeOtherKeyValue, out List<string> other))
				{
					other = new List<string>(5);
					geoKeysByOtherKey.Add(bridgeOtherKeyValue, other);
				}

				geoKeysByOtherKey[bridgeOtherKeyValue].Add(bridgeGeoKeyValue);
			}

			return geoKeysByOtherKey;
		}

		private IEnumerable<IRow> FetchBridgeTableRowsByKey(
			[NotNull] HashSet<string> keys,
			[NotNull] ITable bridgeTable,
			[NotNull] string keyFieldName)
		{
			bool clientSideKeyFiltering;

			if (_joinStrategy == "INDEX")
			{
				clientSideKeyFiltering = true;
			}
			else if (_joinStrategy == "FTS")
			{
				clientSideKeyFiltering = false;
			}
			else if (keys.Count < 1000)
			{
				clientSideKeyFiltering = false;
			}
			else
			{
				int ftsCount = GetTableRowCount(bridgeTable);

				clientSideKeyFiltering = (double) keys.Count / ftsCount > 0.025;
			}

			return FetchRowsByKey(bridgeTable, keys, keyFieldName, true, clientSideKeyFiltering);
		}

		private static IEnumerable<IRow> FetchRowsByKey(
			[NotNull] ITable table,
			[NotNull] ICollection<string> keys,
			[NotNull] string keyFieldName,
			bool recycle,
			bool clientSideKeyFiltering = false,
			[CanBeNull] IQueryFilter filter = null)
		{
			// TODO: Switch depending on previous input-output count ratio with count
			// Empirical values:
			// Dev machine with local docker:
			// 155K rows in bridge table: FTS: ~1s
			// 4K rows in key list: Select-in: ~1s
			// --> Use 3% as threshold to to switch to FTS?
			//
			// Production DB:
			// 155K rows in bridge table: FTS: 1.3s - 1.5s
			// 3.5K rows in key list: ~1.2s
			// -> Use 2% of key-count / all rows count

			string originalSubfields = filter?.SubFields;
			if (filter != null)
			{
				filter.SubFields = null;
			}

			if (clientSideKeyFiltering)
			{
				_msg.DebugFormat(
					"Fetching rows from {0} and filtering for key list on the client...",
					DatasetUtils.GetName(table));

				int keyFieldIdx = table.FindField(keyFieldName);
				Assert.True(keyFieldIdx >= 0,
				            $"Key field {keyFieldName} not found in {DatasetUtils.GetName(table)}");

				int totalCount = 0, yieldCount = 0;
				foreach (IRow row in GdbQueryUtils.GetRows(table, filter, recycle))
				{
					totalCount++;
					object rowKeyValue = row.Value[keyFieldIdx];

					if (rowKeyValue == null || rowKeyValue == DBNull.Value)
					{
						continue;
					}

					// The primary key of the geo table:
					string rowKeyValueString = Convert.ToString(rowKeyValue);

					if (! keys.Contains(rowKeyValueString))
					{
						continue;
					}

					yield return row;
					yieldCount++;
				}

				_msg.DebugFormat("Yielded {0} rows of a total of {1}", yieldCount, totalCount);
			}
			else
			{
				_msg.DebugFormat(
					"Fetching rows from {0} using select-in-list strategy (filtering on the server)",
					DatasetUtils.GetName(table));

				int count = 0;
				foreach (IRow row in GdbQueryUtils.GetRowsInList(
					         table, keyFieldName, keys, recycle, filter))
				{
					yield return row;
					count++;
				}

				_msg.DebugFormat("Yielded {0} rows", count);
			}

			if (filter != null)
			{
				filter.SubFields = originalSubfields;
			}
		}

		private IDictionary<int, int> GeometryEndCopyMatrix
		{
			get
			{
				if (_geometryEndCopyMatrix == null)
				{
					_geometryEndCopyMatrix =
						GdbObjectUtils.CreateMatchingIndexMatrix(
							              _joinedSchema, _geometryEndClass, true, true, null,
							              FieldComparison.FieldName).Where(pair => pair.Value >= 0)
						              .ToDictionary(pair => pair.Value, pair => pair.Key);
				}

				return _geometryEndCopyMatrix;
			}
		}

		private IDictionary<int, int> OtherEndCopyMatrix
		{
			get
			{
				if (_otherEndCopyMatrix == null)
				{
					_otherEndCopyMatrix =
						GdbObjectUtils.CreateMatchingIndexMatrix(
							              _joinedSchema, _otherEndClass, true, true, null,
							              FieldComparison.FieldName).Where(pair => pair.Value >= 0)
						              .ToDictionary(pair => pair.Value, pair => pair.Key);
				}

				return _otherEndCopyMatrix;
			}
		}

		private int GetTableRowCount(ITable table)
		{
			if (! _tableRowStatistics.TryGetValue(table, out int rowCount))
			{
				Stopwatch watch = _msg.DebugStartTiming();

				rowCount = GdbQueryUtils.Count((IObjectClass) table);
				_tableRowStatistics.Add(table, rowCount);

				_msg.DebugStopTiming(watch, "Determined row count of {0}",
				                     DatasetUtils.GetName(table));
			}

			return rowCount;
		}

		private static string GetKeyValue(IRow row, int fieldIndex)
		{
			object value = row.Value[fieldIndex];

			if (value != null && DBNull.Value != value)
			{
				return value.ToString();
			}

			return null;
		}
	}
}
