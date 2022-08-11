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

		[NotNull] private readonly IReadOnlyTable _geometryEndClass;
		[NotNull] private readonly IReadOnlyTable _otherEndClass;
		[NotNull] private readonly GdbTable _joinedSchema;

		private IDictionary<int, int> _geometryEndCopyMatrix;
		private IDictionary<int, int> _otherEndCopyMatrix;

		private readonly AssociationDescription _associationDescription;

		private readonly string
			_joinStrategy = Environment.GetEnvironmentVariable("PROSUITE_MEMORY_JOIN_STRATEGY");

		private readonly Dictionary<IReadOnlyTable, int> _tableRowStatistics =
			new Dictionary<IReadOnlyTable, int>(3);

		public JoinedDataset([NotNull] AssociationDescription associationDescription,
		                     IReadOnlyTable geometryTable,
		                     IReadOnlyTable otherTable,
		                     [NotNull] GdbTable joinedSchema)
		{
			_geometryEndClass = geometryTable;
			_otherEndClass = otherTable;

			_associationDescription = associationDescription;

			_joinedSchema = joinedSchema;
		}

		public JoinType JoinType { get; set; } = JoinType.InnerJoin;

		/// <summary>
		/// The action to be performed on newly joined rows to allow client code to react to or
		/// adapt new instantiated joined rows. The arguments are:
		/// 1. JoinedValueList: The new virtual joined row list. It can be used to add extra rows.
		/// 2. IReadOnlyRow: The left table row
		/// 3. IReadOnlyRow: The right table row
		/// </summary>
		[CanBeNull]
		public Action<JoinedValueList, IReadOnlyRow, IReadOnlyRow> OnRowCreatingAction { get; set; }

		public override IEnvelope Extent => (_geometryEndClass as IReadOnlyFeatureClass)?.Extent;

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

			IDictionary<string, IList<IReadOnlyRow>> otherRows =
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
					otherRows, featureClassKeyField, filter, true).Count();
			}
		}

		public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
		{
			var filterHelper = FilterHelper.Create(_joinedSchema, filter?.WhereClause);

			// The filter's where clause must use the fully qualified rows. But the spatial
			// constraint is applied during the creation of the join.
			foreach (VirtualRow virtualRow in GetJoinedRows(filter, recycling))
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
		/// <param name="recycle"></param>
		/// <returns></returns>
		private IEnumerable<VirtualRow> GetJoinedRows([CanBeNull] IQueryFilter filter,
		                                              bool recycle)
		{
			if (filter != null && (! string.IsNullOrEmpty(filter.WhereClause)))
			{
				filter = (IQueryFilter) ((IClone) filter).Clone();
				filter.WhereClause = null;
			}

			GetKeyFieldNames(out string featureClassKeyField, out string otherClassKeyField);

			IDictionary<string, IList<IReadOnlyRow>> otherRowsByFeatureKey =
				GetOtherRowsByFeatureKey(filter, featureClassKeyField, otherClassKeyField,
				                         out int featureClassKeyIdx);

			foreach (IReadOnlyRow feature in PerformFinalGeoClassRead(
				         otherRowsByFeatureKey, featureClassKeyField, filter, recycle))
			{
				string featureKeyValue = GetKeyValue(feature, featureClassKeyIdx);
				IList<IReadOnlyRow> otherRowList;
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

				foreach (IReadOnlyRow otherRow in otherRowList)
				{
					yield return CreateJoinedFeature(feature, otherRow);
				}
			}
		}

		private IDictionary<string, IList<IReadOnlyRow>> GetOtherRowsByFeatureKey(
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
			foreach (IReadOnlyRow feature in _geometryEndClass.EnumRows(filter, true))
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

			IDictionary<string, IList<IReadOnlyRow>> otherRows =
				GetOtherRowListsByFeatureKey(otherClassKeyField, fClassKeys);

			// Revert the filter change:
			if (filter != null)
				filter.SubFields = originalSubfields;

			return otherRows;
		}

		private IEnumerable<IReadOnlyRow> PerformFinalGeoClassRead(
			IDictionary<string, IList<IReadOnlyRow>> otherRowsByGeoKey,
			string featureClassKeyField,
			[CanBeNull] IQueryFilter filter,
			bool recycling)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			if (JoinType == JoinType.InnerJoin)
			{
				foreach (IReadOnlyRow feature in PerformFinalGeoClassReadFilteredByKeys(
					         otherRowsByGeoKey, featureClassKeyField, filter, recycling))
				{
					yield return feature;
				}
			}
			else if (JoinType == JoinType.LeftJoin)
			{
				// All records from the left table:
				foreach (IReadOnlyRow feature in _geometryEndClass.EnumRows(filter, recycling))
				{
					yield return feature;
				}
			}

			_msg.DebugStopTiming(watch, "Final read of geo-features with result processing");
		}

		private IEnumerable<IReadOnlyRow> PerformFinalGeoClassReadFilteredByKeys(
			[NotNull] IDictionary<string, IList<IReadOnlyRow>> otherRowsByGeoKey,
			[NotNull] string featureClassKeyField,
			[CanBeNull] IQueryFilter filter,
			bool recycling)
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

			IEnumerable<IReadOnlyRow> resultGeoFeatures = FetchRowsByKey(
				_geometryEndClass, otherRowsByGeoKey.Keys, featureClassKeyField, recycling,
				clientSideKeyFiltering, filter);

			foreach (IReadOnlyRow row in resultGeoFeatures)
			{
				yield return row;
			}
		}

		private void GetKeyFieldNames(out string featureClassKeyField,
		                              out string otherClassKeyField)
		{
			if (_associationDescription is ForeignKeyAssociationDescription foreignKeyAssociation)
			{
				if (AreEqual(_geometryEndClass, foreignKeyAssociation.ReferencedTable))
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

				if (AreEqual(_geometryEndClass, manyToManyAssociation.Table1))
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

		private GdbRow CreateJoinedFeature(IReadOnlyRow leftRow, [CanBeNull] IReadOnlyRow otherRow)
		{
			var joinedValueList = new JoinedValueList();

			joinedValueList.AddRow(leftRow, GeometryEndCopyMatrix);
			joinedValueList.AddRow(otherRow, OtherEndCopyMatrix);

			OnRowCreatingAction?.Invoke(joinedValueList, leftRow, otherRow);

			GdbRow result = leftRow is IReadOnlyFeature &&
			                _joinedSchema is GdbFeatureClass gdbFeatureClass
				                ? new GdbFeature(leftRow.OID, gdbFeatureClass, joinedValueList)
				                : new GdbRow(leftRow.OID, _joinedSchema, joinedValueList);

			return result;
		}

		private IDictionary<string, IReadOnlyRow> GetOtherRowsByKey(
			[NotNull] string otherClassField,
			[NotNull] IEnumerable<string> keyValues)
		{
			// Get the non-feature-rows:
			int otherClassKeyIdx = _otherEndClass.FindField(otherClassField);
			string otherTableName = _otherEndClass.Name;
			Assert.True(otherClassKeyIdx >= 0,
			            $"Key field {otherClassField} not found in {otherTableName}");

			IDictionary<string, IReadOnlyRow> otherRows = new Dictionary<string, IReadOnlyRow>();
			foreach (IReadOnlyRow row in GdbQueryUtils.GetRowsInList(
				         _otherEndClass, otherClassField, keyValues, false))
			{
				object otherRowKey = row.get_Value(otherClassKeyIdx);
				Assert.True(otherRowKey != null && DBNull.Value != otherRowKey,
				            $"No key value in {otherTableName}");

				otherRows.Add(otherRowKey.ToString(), row);
			}

			return otherRows;
		}

		private IDictionary<string, IList<IReadOnlyRow>> GetOtherRowListsByFeatureKey(
			string otherClassKeyField,
			HashSet<string> fClassKeys)
		{
			IDictionary<string, IList<IReadOnlyRow>> result =
				new Dictionary<string, IList<IReadOnlyRow>>();

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

				IDictionary<string, IReadOnlyRow> otherRowsByKey =
					GetOtherRowsByKey(otherClassKeyField, geoKeysByOtherKey.Keys);

				_msg.DebugStopTiming(watch, "Retrieved all {0} other rows.", otherRowsByKey.Count);

				foreach (KeyValuePair<string, List<string>> keyValuePair in geoKeysByOtherKey)
				{
					string otherKey = keyValuePair.Key;

					foreach (string geoKey in keyValuePair.Value)
					{
						if (! result.TryGetValue(geoKey, out IList<IReadOnlyRow> otherRowList))
						{
							otherRowList = new List<IReadOnlyRow>(5);

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
			foreach (KeyValuePair<string, IReadOnlyRow> keyValuePair in GetOtherRowsByKey(
				         otherClassKeyField, fClassKeys))
			{
				string otherRowKey = keyValuePair.Key;

				if (! result.TryGetValue(otherRowKey, out IList<IReadOnlyRow> otherRowList))
				{
					otherRowList = new List<IReadOnlyRow>(3);

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

			IReadOnlyTable bridgeTable = m2nAssociation.AssociationTable;
			string bridgeTableGeoKeyField;
			string bridgeTableOtherKeyField;
			if (AreEqual(_geometryEndClass, m2nAssociation.Table1))
			{
				bridgeTableGeoKeyField = m2nAssociation.AssociationTableKey1;
				bridgeTableOtherKeyField = m2nAssociation.AssociationTableKey2;
			}
			else
			{
				bridgeTableGeoKeyField = m2nAssociation.AssociationTableKey2;
				bridgeTableOtherKeyField = m2nAssociation.AssociationTableKey1;
			}

			string bridgeTableName = bridgeTable.Name;

			int bridgeTableOtherKeyIdx = bridgeTable.FindField(bridgeTableOtherKeyField);
			Assert.True(bridgeTableOtherKeyIdx >= 0,
			            $"Key field {bridgeTableOtherKeyField} not found in {bridgeTable}");

			int bridgeTableGeoKeyIdx = bridgeTable.FindField(bridgeTableGeoKeyField);
			Assert.True(bridgeTableGeoKeyIdx >= 0,
			            $"Key field {bridgeTableGeoKeyField} not found in {bridgeTable}");

			foreach (IReadOnlyRow row in FetchBridgeTableRowsByKey(geoKeys, bridgeTable,
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

		private static bool AreEqual(IReadOnlyTable table1, IReadOnlyTable table2)
		{
			if (table1.Equals(table2))
			{
				return true;
			}

			if (table1 is ReadOnlyTable roTable1 && table2 is ReadOnlyTable roTable2)
			{
				// TODO: Move to Equals implementation of ReadOnlyTable? Add Equals to IReadOnlyTable?
				return DatasetUtils.IsSameObjectClass((IObjectClass) roTable1.BaseTable,
				                                      (IObjectClass) roTable2.BaseTable);
			}

			return false;
		}

		private IEnumerable<IReadOnlyRow> FetchBridgeTableRowsByKey(
			[NotNull] HashSet<string> keys,
			[NotNull] IReadOnlyTable bridgeTable,
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

		private static IEnumerable<IReadOnlyRow> FetchRowsByKey(
			[NotNull] IReadOnlyTable table,
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
					table.Name);

				int keyFieldIdx = table.FindField(keyFieldName);
				Assert.True(keyFieldIdx >= 0,
				            $"Key field {keyFieldName} not found in {table.Name}");

				int totalCount = 0, yieldCount = 0;

				foreach (IReadOnlyRow row in table.EnumRows(filter, recycle))
				{
					totalCount++;
					object rowKeyValue = row.get_Value(keyFieldIdx);

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
					table.Name);

				int count = 0;
				foreach (IReadOnlyRow row in GdbQueryUtils.GetRowsInList(
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

		private int GetTableRowCount(IReadOnlyTable table)
		{
			if (! _tableRowStatistics.TryGetValue(table, out int rowCount))
			{
				Stopwatch watch = _msg.DebugStartTiming();

				IQueryFilter filter = new QueryFilterClass
				                      {
					                      SubFields = table.OIDFieldName
				                      };

				rowCount = table.RowCount(filter);

				_tableRowStatistics.Add(table, rowCount);

				_msg.DebugStopTiming(watch, "Determined row count of {0}", table.Name);
			}

			return rowCount;
		}

		private static string GetKeyValue(IReadOnlyRow row, int fieldIndex)
		{
			object value = row.get_Value(fieldIndex);

			if (value != null && DBNull.Value != value)
			{
				return value.ToString();
			}

			return null;
		}
	}
}
