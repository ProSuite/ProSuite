using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
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
		private IDictionary<int, int> _associationTableCopyMatrix;

		private readonly AssociationDescription _associationDescription;

		// A short-term (or in the future potentially long-term) cache of the association rows.
		// As short-term cache within read operations it is used to included the bridge table
		// attributes and hence provide a unique Id from the bridge table.
		// As Long-term could it can be used in read-only scenarios to improve performance.
		// TODO: Implement long-term cache
		private AssociationTableRowCache _associationRows;

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

		public IReadOnlyTable ObjectIdSourceTable { get; set; }

		public bool IncludeMtoNAssociationRows
		{
			get => _associationRows != null;
			set
			{
				if (value)
				{
					if (_associationRows == null)
					{
						_associationRows = new AssociationTableRowCache();
					}
				}
				else
				{
					_associationRows = null;
				}
			}
		}

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
			EnsureKeyFieldNames();
			// This is ok if there is 1 left row with the provided id. For 1:many several results exist.
			// Consider using UniqueIdProvider. For many:many we should probably use the ObjectID on the
			// bridge table in the first place!
			// TODO: In case of m:n take the RID (if exists) and fill it into the OBJECTID field.
			//       In  case of 1:m take the right table's OBJECT id and fill it into the OBJECTID field.
			//       -> here, do a reverse look-up if necessary
			if (ObjectIdSource == SourceTable.Left)
			{
				IReadOnlyRow leftRow = _geometryEndClass.GetRow(id);

				return GetJoinedRows(new List<IReadOnlyRow> {leftRow}).FirstOrDefault();
			}

			if (ObjectIdSource == SourceTable.Right)
			{
				IReadOnlyRow rightRow = _otherEndClass.GetRow(id);

				string otherKeyValue = GetNonNullKeyValue(rightRow, OtherClassKeyFieldIndex);

				var otherKeyList = new List<string> {otherKeyValue};
				IList<IReadOnlyRow> resultGeoFeatures = FetchRowsByKey(
						_geometryEndClass, otherKeyList, GeometryClassKeyField, false)
					.ToList();

				Assert.True(resultGeoFeatures.Count <= 1,
				            $"Unexpected number of joined features: {resultGeoFeatures.Count}");

				IReadOnlyRow geoFeature = resultGeoFeatures[0];

				return CreateJoinedFeature(geoFeature, rightRow);
			}

			var m2nAssociation = ((ManyToManyAssociationDescription) _associationDescription);

			return GetRowManyToMany(id, m2nAssociation);
		}

		public override int GetRowCount(IQueryFilter filter)
		{
			if (filter != null && ! string.IsNullOrEmpty(filter.WhereClause))
			{
				return Search(filter, true).Count();
			}

			EnsureKeyFieldNames();

			IDictionary<string, IList<IReadOnlyRow>> otherRows = GetOtherRowsByFeatureKey(filter);

			if (_associationDescription is ManyToManyAssociationDescription)
			{
				return otherRows.Values.Sum(v => v.Count);
			}
			else
			{
				// TODO: This could be optimized if the side of the primary key was known
				return PerformFinalGeoClassRead(otherRows, filter, true).Count();
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

			EnsureKeyFieldNames();

			IDictionary<string, IList<IReadOnlyRow>> otherRowsByFeatureKey =
				GetOtherRowsByFeatureKey(filter);

			IEnumerable<IReadOnlyRow> leftRows = PerformFinalGeoClassRead(
				otherRowsByFeatureKey, filter, recycle);

			foreach (VirtualRow virtualRow in Join(leftRows, otherRowsByFeatureKey))
			{
				yield return virtualRow;
			}
		}

		/// <summary>
		/// Performs the join with the provided left rows.
		/// </summary>
		/// <param name="leftRows">The collection of left rows to join to the right rows to be retrieved
		/// from the database.</param>
		/// <returns></returns>
		private IEnumerable<VirtualRow> GetJoinedRows([NotNull] ICollection<IReadOnlyRow> leftRows)
		{
			EnsureKeyFieldNames();

			IDictionary<string, IList<IReadOnlyRow>> otherRowsByFeatureKey =
				GetOtherRowsByFeatureKey(leftRows);

			foreach (VirtualRow virtualRow in Join(leftRows, otherRowsByFeatureKey))
			{
				yield return virtualRow;
			}
		}

		private IDictionary<string, IList<IReadOnlyRow>> GetOtherRowsByFeatureKey(
			[CanBeNull] IQueryFilter filter)
		{
			Assert.NotNull(_otherEndClass);
			Assert.NotNull(OtherClassKeyField);

			string originalSubfields = filter?.SubFields;
			if (filter != null)
				filter.SubFields = GeometryClassKeyField;

			// TODO: More testing, does not seem to make a difference:
			//esriSpatialRelEnum originalSpatialRel = esriSpatialRelEnum.esriSpatialRelUndefined;
			//if (filter is ISpatialFilter spatialFilter  &&
			//    spatialFilter.SpatialRel == esriSpatialRelEnum.esriSpatialRelIntersects)
			//{
			//	originalSpatialRel = spatialFilter.SpatialRel;
			//	spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			//}

			// TODO: Deal with where-clause

			IEnumerable<IReadOnlyRow> leftFeatures = _geometryEndClass.EnumRows(filter, true);

			IDictionary<string, IList<IReadOnlyRow>> otherRows =
				GetOtherRowsByFeatureKey(leftFeatures);

			// Revert the filter change:
			if (filter != null)
				filter.SubFields = originalSubfields;

			return otherRows;
		}

		private IDictionary<string, IList<IReadOnlyRow>> GetOtherRowsByFeatureKey(
			[NotNull] IEnumerable<IReadOnlyRow> leftFeatures)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			HashSet<string> fClassKeys = new HashSet<string>();

			// TODO: If the filter is null or it's no spatial filter: compare row count to determine
			//       which table should be queried first.

			int featureCount = 0;

			foreach (IReadOnlyRow feature in leftFeatures)
			{
				featureCount++;

				string keyValue = GetKeyValue(feature, GeometryClassKeyFieldIndex);

				if (keyValue != null)
				{
					fClassKeys.Add(keyValue);
				}
			}

			_msg.DebugStopTiming(watch, "Initial search found {0} geo-keys in {1} features",
			                     fClassKeys.Count, featureCount);

			IDictionary<string, IList<IReadOnlyRow>> otherRows =
				GetOtherRowListsByFeatureKey(fClassKeys);

			return otherRows;
		}

		private IEnumerable<IReadOnlyRow> PerformFinalGeoClassRead(
			IDictionary<string, IList<IReadOnlyRow>> otherRowsByGeoKey,
			[CanBeNull] IQueryFilter filter,
			bool recycling)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			if (JoinType == JoinType.InnerJoin)
			{
				foreach (IReadOnlyRow feature in PerformFinalGeoClassReadFilteredByKeys(
					         otherRowsByGeoKey, GeometryClassKeyField, filter, recycling))
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

		private IEnumerable<VirtualRow> Join(
			[NotNull] IEnumerable<IReadOnlyRow> leftRows,
			[NotNull] IDictionary<string, IList<IReadOnlyRow>> otherRowsByFeatureKey)
		{
			foreach (IReadOnlyRow feature in leftRows)
			{
				string featureKeyValue = GetKeyValue(feature, GeometryClassKeyFieldIndex);
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

				int listIdx = 0;
				foreach (IReadOnlyRow otherRow in otherRowList)
				{
					IReadOnlyRow associationRow = null;
					if (_associationRows != null && otherRow != null)
					{
						string otherRowKey = GetKeyValue(otherRow, OtherClassKeyFieldIndex);
						associationRow =
							_associationRows.LookUp(featureKeyValue, otherRowKey, listIdx);
					}

					yield return CreateJoinedFeature(feature, otherRow, associationRow);
					listIdx++;
				}
			}
		}

		private void EnsureKeyFieldNames()
		{
			if (GeometryClassKeyField != null && OtherClassKeyField != null)
			{
				return;
			}

			if (_associationDescription is ForeignKeyAssociationDescription foreignKeyAssociation)
			{
				if (AreEqual(_geometryEndClass, foreignKeyAssociation.ReferencedTable))
				{
					GeometryClassKeyField = foreignKeyAssociation.ReferencedKeyName;
					OtherClassKeyField = foreignKeyAssociation.ReferencingKeyName;
				}
				else
				{
					GeometryClassKeyField = foreignKeyAssociation.ReferencingKeyName;
					OtherClassKeyField = foreignKeyAssociation.ReferencedKeyName;
				}
			}
			else
			{
				ManyToManyAssociationDescription manyToManyAssociation =
					(ManyToManyAssociationDescription) _associationDescription;

				if (AreEqual(_geometryEndClass, manyToManyAssociation.Table1))
				{
					GeometryClassKeyField = manyToManyAssociation.Table1KeyName;
					OtherClassKeyField = manyToManyAssociation.Table2KeyName;
				}
				else
				{
					GeometryClassKeyField = manyToManyAssociation.Table2KeyName;
					OtherClassKeyField = manyToManyAssociation.Table1KeyName;
				}
			}

			// Set up field indexes
			GeometryClassKeyFieldIndex = _geometryEndClass.FindField(GeometryClassKeyField);
			Assert.True(GeometryClassKeyFieldIndex >= 0,
			            $"Key field {GeometryClassKeyField} not found in {_geometryEndClass.Name}");

			OtherClassKeyFieldIndex = _otherEndClass.FindField(OtherClassKeyField);
			Assert.True(OtherClassKeyFieldIndex >= 0,
			            $"Key field {OtherClassKeyField} not found in {_otherEndClass.Name}");

			// Table from which the Object ID is taken: 
			ObjectIdSource = GetObjectIdTable();
		}

		private SourceTable GetObjectIdTable()
		{
			IReadOnlyTable oidSourceTable =
				TableJoinUtils.DetermineOIDTable(_associationDescription, JoinType,
				                                 _geometryEndClass);

			if (_associationDescription is ForeignKeyAssociationDescription)
			{
				return AreEqual(_geometryEndClass, oidSourceTable)
					       ? SourceTable.Left
					       : SourceTable.Right;
			}

			// The LeftTable is a non-unique fallback with a very slight performance gain due to left-out association rows. 
			return IncludeMtoNAssociationRows ? SourceTable.Association : SourceTable.Left;
		}

		private GdbRow CreateJoinedFeature([NotNull] IReadOnlyRow leftRow,
		                                   [CanBeNull] IReadOnlyRow otherRow,
		                                   [CanBeNull] IReadOnlyRow associationRow = null)
		{
			var joinedValueList = new JoinedValueList();

			joinedValueList.AddRow(leftRow, GeometryEndCopyMatrix);
			joinedValueList.AddRow(otherRow, OtherEndCopyMatrix);

			if (associationRow != null)
			{
				// At least keep the original RID (ObjectId). Potentially it could also be an attributed m:n
				joinedValueList.AddRow(associationRow, AssociationTableCopyMatrix);
			}

			OnRowCreatingAction?.Invoke(joinedValueList, leftRow, otherRow);

			IReadOnlyRow oidSourceRow;
			if (ObjectIdSource == SourceTable.Left)
			{
				oidSourceRow = leftRow;
			}
			else if (ObjectIdSource == SourceTable.Right)
			{
				oidSourceRow = Assert.NotNull(otherRow);
			}
			else if (associationRow != null)
			{
				oidSourceRow = associationRow;
			}
			else
			{
				// Uniqueness is probably irrelevant, just use the left:
				oidSourceRow = leftRow;
			}

			GdbRow result = leftRow is IReadOnlyFeature &&
			                _joinedSchema is GdbFeatureClass gdbFeatureClass
				                ? new GdbFeature(oidSourceRow.OID, gdbFeatureClass, joinedValueList)
				                : new GdbRow(oidSourceRow.OID, _joinedSchema, joinedValueList);

			return result;
		}

		private IDictionary<string, IReadOnlyRow> GetOtherRowsByKey(
			[NotNull] IEnumerable<string> keyValues)
		{
			// Get the non-feature-rows:
			IDictionary<string, IReadOnlyRow> otherRows = new Dictionary<string, IReadOnlyRow>();
			foreach (IReadOnlyRow row in GdbQueryUtils.GetRowsInList(
				         _otherEndClass, OtherClassKeyField, keyValues, false))
			{
				string otherRowKey = GetKeyValue(row, OtherClassKeyFieldIndex);

				if (otherRowKey == null)
				{
					throw new InvalidDataException(
						$"No key value in {GdbObjectUtils.ToString(row)}");
				}

				otherRows.Add(otherRowKey, row);
			}

			return otherRows;
		}

		private IDictionary<string, IList<IReadOnlyRow>> GetOtherRowListsByFeatureKey(
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
					GetOtherRowsByKey(geoKeysByOtherKey.Keys);

				_msg.DebugStopTiming(watch, "Retrieved all {0} other rows.", otherRowsByKey.Count);

				foreach (KeyValuePair<string, List<string>> keyValuePair in geoKeysByOtherKey)
				{
					string otherKey = keyValuePair.Key;

					foreach (string geoKey in keyValuePair.Value)
					{
						if (! result.TryGetValue(geoKey, out _))
						{
							IList<IReadOnlyRow> otherRowList = new List<IReadOnlyRow>(5);

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
				         fClassKeys))
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

		#region Many-to-many row access

		private Dictionary<string, List<string>> GeoKeysByOtherKeyManyToMany(
			[NotNull] HashSet<string> geoKeys,
			[NotNull] ManyToManyAssociationDescription m2nAssociation)
		{
			Dictionary<string, List<string>> geoKeysByOtherKey =
				new Dictionary<string, List<string>>();

			IReadOnlyTable bridgeTable = m2nAssociation.AssociationTable;
			GetAssociationTableKeyFields(m2nAssociation, out string bridgeTableGeoKeyField,
			                             out string bridgeTableOtherKeyField);

			string bridgeTableName = bridgeTable.Name;

			int bridgeTableOtherKeyIdx = bridgeTable.FindField(bridgeTableOtherKeyField);
			Assert.True(bridgeTableOtherKeyIdx >= 0,
			            $"Key field {bridgeTableOtherKeyField} not found in {bridgeTableName}");

			int bridgeTableGeoKeyIdx = bridgeTable.FindField(bridgeTableGeoKeyField);
			Assert.True(bridgeTableGeoKeyIdx >= 0,
			            $"Key field {bridgeTableGeoKeyField} not found in {bridgeTableName}");

			// This is the short-term cache. In a strictly read-only environment it could also be
			// used to retrieve the result in specific cases (requires caching geoKeys without match)
			_associationRows?.Clear();

			var recycle = _associationRows == null;
			foreach (IReadOnlyRow row in FetchBridgeTableRowsByKey(geoKeys, bridgeTable,
				         bridgeTableGeoKeyField, recycle))
			{
				// The primary key of the other table:
				string bridgeOtherKeyValue = GetNonNullKeyValue(row, bridgeTableOtherKeyIdx);

				// The primary key of the geo table:
				string bridgeGeoKeyValue = GetNonNullKeyValue(row, bridgeTableGeoKeyIdx);

				// NOTE: For long-term global caching if memory was no issue, the bridgeRow could be cached already here

				// Double check, the fetch might use a FTS or deliver the full cache:
				if (! geoKeys.Contains(bridgeGeoKeyValue))
				{
					continue;
				}

				// Cache the bridgeRow, if desired:
				_associationRows?.Add(bridgeGeoKeyValue, bridgeOtherKeyValue, row);

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

		private VirtualRow GetRowManyToMany(int id, ManyToManyAssociationDescription m2nAssociation)
		{
			GetAssociationTableKeyFields(m2nAssociation, out string bridgeTableGeoKeyField,
			                             out string _);

			IReadOnlyTable bridgeTable = m2nAssociation.AssociationTable;

			int bridgeTableGeoKeyIdx = bridgeTable.FindField(bridgeTableGeoKeyField);
			Assert.True(bridgeTableGeoKeyIdx >= 0,
			            $"Key field {bridgeTableGeoKeyField} not found in {bridgeTable.Name}");

			IReadOnlyRow associationRow = bridgeTable.GetRow(id);

			string geoKeyValue = GetNonNullKeyValue(associationRow, bridgeTableGeoKeyIdx);

			var geoKeyList = new List<string> {geoKeyValue};

			IList<IReadOnlyRow> geoFeatures = FetchRowsByKey(
					_geometryEndClass, geoKeyList, GeometryClassKeyField, false)
				.ToList();

			Assert.AreEqual(1, geoFeatures.Count, $"Unexpected number of left table features");

			return GetJoinedRows(new List<IReadOnlyRow> {geoFeatures[0]}).FirstOrDefault();
		}

		private void GetAssociationTableKeyFields(ManyToManyAssociationDescription m2nAssociation,
		                                          out string bridgeTableGeoKeyField,
		                                          out string bridgeTableOtherKeyField)
		{
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
		}

		#endregion

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
			[NotNull] string keyFieldName,
			bool recycle)
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

			return FetchRowsByKey(bridgeTable, keys, keyFieldName, recycle, clientSideKeyFiltering);
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

					// The primary key of the geo table:
					string rowKeyValueString = GetKeyValue(row, keyFieldIdx);

					if (rowKeyValueString == null)
					{
						continue;
					}

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

		private string GeometryClassKeyField { get; set; }
		private string OtherClassKeyField { get; set; }

		private int GeometryClassKeyFieldIndex { get; set; }
		private int OtherClassKeyFieldIndex { get; set; }

		private SourceTable ObjectIdSource { get; set; }

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

		private IDictionary<int, int> AssociationTableCopyMatrix
		{
			get
			{
				if (_associationTableCopyMatrix == null)
				{
					var associationTable =
						((ManyToManyAssociationDescription) _associationDescription)
						.AssociationTable;

					_associationTableCopyMatrix =
						GdbObjectUtils.CreateMatchingIndexMatrix(
							              _joinedSchema, associationTable, true, true, null,
							              FieldComparison.FieldName).Where(pair => pair.Value >= 0)
						              .ToDictionary(pair => pair.Value, pair => pair.Key);
				}

				return _associationTableCopyMatrix;
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

		private static string GetNonNullKeyValue(IReadOnlyRow row, int fieldIndex)
		{
			string result = GetKeyValue(row, fieldIndex);

			if (result == null)
			{
				throw new InvalidDataException(
					$"No key value in {GdbObjectUtils.ToString(row)} (field index: {fieldIndex}).");
			}

			return result;
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

		private enum SourceTable
		{
			Left,
			Right,
			Association
		}

		private class AssociationTableRowCache
		{
			private readonly IDictionary<string, IList<AssociationRow>>
				_knownAssociationRowsByGeoKey =
					new Dictionary<string, IList<AssociationRow>>();

			public AssociationTableRowCache() { }

			public void Clear()
			{
				_knownAssociationRowsByGeoKey.Clear();
			}

			public void Add([NotNull] string geoKey,
			                [NotNull] string otherKey,
			                IReadOnlyRow row)
			{
				AssociationRow associationRow = new AssociationRow()
				                                {
					                                Row = row,
					                                GeoKey = geoKey,
					                                OtherKey = otherKey
				                                };

				CollectionUtils.AddToValueList(_knownAssociationRowsByGeoKey, geoKey,
				                               associationRow);
			}

			[CanBeNull]
			public IReadOnlyRow LookUp(string geoKey, string otherKey,
			                           int assumedListIndex = -1)
			{
				IList<AssociationRow> candidates;
				if (! _knownAssociationRowsByGeoKey.TryGetValue(geoKey, out candidates))
				{
					return null;
				}

				if (assumedListIndex >= 0 && assumedListIndex < candidates.Count)
				{
					var guessedCandidate = candidates[assumedListIndex];

					if (guessedCandidate.OtherKey == otherKey)
					{
						return guessedCandidate.Row;
					}
				}

				// Iterate all candidates
				foreach (AssociationRow candidate in candidates)
				{
					if (candidate.OtherKey == otherKey)
					{
						return candidate.Row;
					}
				}

				return null;
			}

			private class AssociationRow
			{
				public IReadOnlyRow Row { get; set; }
				public string GeoKey { get; set; }
				public string OtherKey { get; set; }
			}
		}
	}
}
