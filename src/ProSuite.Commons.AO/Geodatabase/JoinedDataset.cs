using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class JoinedDataset : BackingDataset
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly GdbTable _joinedSchema;

		private readonly AssociationDescription _associationDescription;

		// A short-term (or in the future potentially long-term) cache of the association rows.
		// As short-term cache within read operations it is used to included the bridge table
		// attributes and hence provide a unique Id from the bridge table.
		// As Long-term could it can be used in read-only scenarios to improve performance.
		// TODO: Implement long-term cache
		private AssociationTableRowCache _associationRows;

		private readonly string
			_joinStrategy = Environment.GetEnvironmentVariable("PROSUITE_MEMORY_JOIN_STRATEGY");

		private readonly Dictionary<IReadOnlyTable, long> _tableRowStatistics =
			new Dictionary<IReadOnlyTable, long>(3);

		private string GeometryClassKeyField { get; set; }
		private string OtherClassKeyField { get; set; }

		private int GeometryClassKeyFieldIndex { get; set; }
		private int OtherClassKeyFieldIndex { get; set; }

		public JoinedDataset([NotNull] AssociationDescription associationDescription,
		                     [NotNull] IReadOnlyTable geometryTable,
		                     [NotNull] IReadOnlyTable otherTable,
		                     [NotNull] GdbTable joinedSchema)
		{
			GeometryEndClass = geometryTable;
			OtherEndClass = otherTable;

			_associationDescription = associationDescription;

			_joinedSchema = joinedSchema;
		}

		/// <summary>
		/// The 'left' table which, if it has a geometry field, will be used to define the
		/// resulting FeatureClass. If this table has no geometry field, the result will be a
		/// GdbTable without geometry field.
		/// </summary>
		[NotNull]
		public IReadOnlyTable GeometryEndClass { get; }

		/// <summary>
		/// The 'right' table, typically the one without geometry or whose geometry field is not
		/// used.
		/// </summary>
		[NotNull]
		public IReadOnlyTable OtherEndClass { get; }

		[CanBeNull]
		public IReadOnlyTable AssociationTable
		{
			get
			{
				var manyToManyAssociation =
					_associationDescription as ManyToManyAssociationDescription;

				return manyToManyAssociation?.AssociationTable;
			}
		}

		public JoinType JoinType { get; set; } = JoinType.InnerJoin;

		private JoinSourceTable _objectIdSource;

		public JoinSourceTable ObjectIdSource
		{
			get
			{
				if (_objectIdSource == JoinSourceTable.None)
				{
					_objectIdSource = GetObjectIdTable();
				}

				return _objectIdSource;
			}
		}

		public IReadOnlyTable ObjectIdSourceTable
		{
			get
			{
				switch (ObjectIdSource)
				{
					case JoinSourceTable.Left:
						return GeometryEndClass;
					case JoinSourceTable.Right:
						return OtherEndClass;
					case JoinSourceTable.Association:
						return AssociationTable;
					default:
						return null;
				}
			}
		}

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
		/// The factory that creates the final row from the rows to be joined (left row, other row,
		/// optionally the association table row). Extra values can be injected by subclasses of
		/// the default row factory.
		/// </summary>
		public JoinedRowFactory JoinedRowFactory { get; set; }

		/// <summary>
		/// Whether the features from the left table should be assumed to be cached and therefore
		/// the filtering can take place in client code rather than the database.
		/// </summary>
		public bool AssumeLeftTableCached { get; set; }

		#region BackingDataset implementation

		public override IEnvelope Extent => (GeometryEndClass as IReadOnlyFeatureClass)?.Extent;

		public override VirtualRow GetRow(long id)
		{
			EnsureKeyFieldNames();
			// This is ok if there is 1 left row with the provided id. For 1:many several results exist.
			// Consider using UniqueIdProvider. For many:many we should probably use the ObjectID on the
			// bridge table in the first place!
			// TODO: In case of m:n take the RID (if exists) and fill it into the OBJECTID field.
			//       In  case of 1:m take the right table's OBJECT id and fill it into the OBJECTID field.
			//       -> here, do a reverse look-up if necessary
			if (ObjectIdSource == JoinSourceTable.Left)
			{
				IReadOnlyRow leftRow = GeometryEndClass.GetRow(id);

				return GetJoinedRows(new List<IReadOnlyRow> { leftRow }).FirstOrDefault();
			}

			if (ObjectIdSource == JoinSourceTable.Right)
			{
				IReadOnlyRow rightRow = OtherEndClass.GetRow(id);

				string otherKeyValue = GetKeyValue(rightRow, OtherClassKeyFieldIndex);

				if (otherKeyValue == null)
				{
					if (JoinType != JoinType.RightJoin)
					{
						throw new InvalidDataException(
							$"No key value in {GdbObjectUtils.ToString(rightRow)} (field: {OtherClassKeyField})");
					}
					// TODO: Implement right join: Return the right row without a left row
				}

				var otherKeyList = new List<string> { otherKeyValue };
				IList<IReadOnlyRow> resultGeoFeatures = FetchRowsByKey(
						GeometryEndClass, otherKeyList, GeometryClassKeyField, false)
					.ToList();

				Assert.True(resultGeoFeatures.Count <= 1,
				            $"Unexpected number of joined features: {resultGeoFeatures.Count}");

				IReadOnlyRow geoFeature = resultGeoFeatures[0];

				return CreateJoinedFeature(geoFeature, rightRow);
			}

			var m2nAssociation = ((ManyToManyAssociationDescription) _associationDescription);

			return GetRowManyToMany(id, m2nAssociation);
		}

		public override long GetRowCount(ITableFilter filter)
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

		public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
		{
			var filterHelper = FilterHelper.Create(_joinedSchema, filter?.WhereClause);

			if (_msg.IsVerboseDebugEnabled)
			{
				LogQueryProperties(filter);
			}

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

		#endregion

		/// <summary>
		/// Performs the join using the filter only by applying the spatial constraint.
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="recycle"></param>
		/// <returns></returns>
		private IEnumerable<VirtualRow> GetJoinedRows([CanBeNull] ITableFilter filter,
		                                              bool recycle)
		{
			EnsureKeyFieldNames();

			IDictionary<string, IList<IReadOnlyRow>> otherRowsByFeatureKey = null;

			bool filterHasWhereClause =
				filter != null && ! string.IsNullOrEmpty(filter.WhereClause);

			if (filterHasWhereClause)
			{
				try
				{
					// TOP-5597: Try using the where clause on the left table: It could improve performance
					// tremendously in case there is no spatial filter and the where-clause is restrictive.
					// However, it could fail if the where clause references fields from the right table.
					// The where clause will be re-applied to the resulting joined rows.
					otherRowsByFeatureKey = GetOtherRowsByFeatureKey(filter);
				}
				catch (Exception e)
				{
					_msg.Debug("Exception tentative executing the where-clause on the left table " +
					           "only. Falling back to default.", e);
				}
			}

			// Do not apply the where clause on the remaining queries (it will be applied applied to the resulting joined rows)
			if (filterHasWhereClause)
			{
				filter = filter.Clone();
				filter.WhereClause = null;
			}

			if (otherRowsByFeatureKey == null)
			{
				otherRowsByFeatureKey = GetOtherRowsByFeatureKey(filter);
			}

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

		/// <summary>
		/// Gets the 'other' class' records by respective key value from the geometry class.
		/// First, the features from the <see cref="GeometryEndClass"/> are searched using the
		/// provided filter to get the left features. Using the keys from these features, the
		/// relevant records from the <see cref="OtherEndClass"/> are fetched and returned as
		/// values in the result dictionary.
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		public IDictionary<string, IList<IReadOnlyRow>> GetOtherRowsByFeatureKey(
			[CanBeNull] ITableFilter filter)
		{
			EnsureKeyFieldNames();

			string originalSubfields = filter?.SubFields;

			if (filter == null)
			{
				filter = new AoTableFilter();
			}

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

			IEnumerable<IReadOnlyRow> leftFeatures = GeometryEndClass.EnumRows(filter, true);

			IDictionary<string, IList<IReadOnlyRow>> otherRows =
				GetOtherRowsByFeatureKey(leftFeatures);

			// Revert the filter change:
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
			[CanBeNull] ITableFilter filter,
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
				foreach (IReadOnlyRow feature in GeometryEndClass.EnumRows(filter, recycling))
				{
					yield return feature;
				}
			}

			_msg.DebugStopTiming(watch, "Final read of geo-features with result processing");
		}

		private IEnumerable<IReadOnlyRow> PerformFinalGeoClassReadFilteredByKeys(
			[NotNull] IDictionary<string, IList<IReadOnlyRow>> otherRowsByGeoKey,
			[NotNull] string featureClassKeyField,
			[CanBeNull] ITableFilter filter,
			bool recycling)
		{
			int featureClassKeyIdx = GeometryEndClass.FindField(featureClassKeyField);
			Assert.True(featureClassKeyIdx >= 0, $"Key field not found: {featureClassKeyIdx}");

			bool clientSideKeyFiltering;

			if (! FilterHasGeometry(filter))
			{
				// The container is of no help, leverage at least the WhereClause directly in the DB
				clientSideKeyFiltering = false;
			}
			else if (AssumeLeftTableCached)
			{
				clientSideKeyFiltering = true;
			}
			else if (_joinStrategy == "INDEX")
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
				GeometryEndClass, otherRowsByGeoKey.Keys, featureClassKeyField, recycling,
				clientSideKeyFiltering, filter);

			foreach (IReadOnlyRow row in resultGeoFeatures)
			{
				yield return row;
			}
		}

		private static bool FilterHasGeometry(ITableFilter filter)
		{
			if (filter is IFeatureClassFilter spatialFilter)
			{
				return spatialFilter.FilterGeometry != null;
			}

			return false;
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
					    ! otherRowsByFeatureKey.TryGetValue(featureKeyValue, out otherRowList) ||
					    otherRowList.Count == 0)
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
				if (GeometryEndClass.Equals(foreignKeyAssociation.ReferencedTable))
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

				if (GeometryEndClass.Equals(manyToManyAssociation.Table1))
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
			GeometryClassKeyFieldIndex = GeometryEndClass.FindField(GeometryClassKeyField);
			Assert.True(GeometryClassKeyFieldIndex >= 0,
			            $"Key field {GeometryClassKeyField} not found in {GeometryEndClass.Name}");

			OtherClassKeyFieldIndex = OtherEndClass.FindField(OtherClassKeyField);
			Assert.True(OtherClassKeyFieldIndex >= 0,
			            $"Key field {OtherClassKeyField} not found in {OtherEndClass.Name}");
		}

		private JoinSourceTable GetObjectIdTable()
		{
			string oidFieldName = _joinedSchema.OIDFieldName;

			if (oidFieldName == null)
			{
				return JoinSourceTable.None;
			}

			int splitPosition = oidFieldName.LastIndexOf('.');

			Assert.False(splitPosition < 0, "OBJECTID field is not fully qualified.");

			string tableName = oidFieldName.Substring(0, splitPosition);

			if (tableName.Equals(GeometryEndClass.Name,
			                     StringComparison.InvariantCultureIgnoreCase))
			{
				return JoinSourceTable.Left;
			}

			if (tableName.Equals(OtherEndClass.Name, StringComparison.InvariantCultureIgnoreCase))
			{
				return JoinSourceTable.Right;
			}

			Assert.True(_associationDescription is ManyToManyAssociationDescription,
			            $"Unexpected association type for OID field {oidFieldName}");

			Assert.True(
				tableName.Equals(AssociationTable?.Name,
				                 StringComparison.InvariantCultureIgnoreCase),
				$"OID field {oidFieldName} not found in join tables");

			return JoinSourceTable.Association;
		}

		private GdbRow CreateJoinedFeature([NotNull] IReadOnlyRow leftRow,
		                                   [CanBeNull] IReadOnlyRow otherRow,
		                                   [CanBeNull] IReadOnlyRow associationRow = null)
		{
			if (JoinedRowFactory == null)
			{
				JoinedRowFactory = new JoinedRowFactory(
					                   _joinedSchema, GeometryEndClass, OtherEndClass)
				                   {
					                   AssociationTable = AssociationTable
				                   };
			}

			return JoinedRowFactory.CreateRow(leftRow, otherRow, ObjectIdSource, associationRow);
		}

		private IDictionary<string, IReadOnlyRow> GetOtherRowsByKey(
			[NotNull] IEnumerable<string> keyValues)
		{
			// Get the non-feature-rows:
			IDictionary<string, IReadOnlyRow> otherRows = new Dictionary<string, IReadOnlyRow>();
			foreach (IReadOnlyRow row in TableFilterUtils.GetRowsInList(
				         OtherEndClass, OtherClassKeyField, keyValues, false))
			{
				string otherRowKey = GetKeyValue(row, OtherClassKeyFieldIndex);

				if (otherRowKey == null)
				{
					throw new InvalidDataException(
						$"No key value in {GdbObjectUtils.ToString(row)}");
				}

				try
				{
					otherRows.Add(otherRowKey, row);
				}
				catch (ArgumentException e)
				{
					throw new ArgumentException(
						$"The the key value {otherRowKey} in row {GdbObjectUtils.ToString(row)} is not unique. " +
						$"The row {GdbObjectUtils.ToString(otherRows[otherRowKey])} has the same value",
						e);
				}
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
			foreach (IReadOnlyRow row in TableFilterUtils.GetRowsInList(
				         OtherEndClass, OtherClassKeyField, fClassKeys, false))
			{
				string otherRowKey = GetKeyValue(row, OtherClassKeyFieldIndex);

				if (otherRowKey == null)
				{
					throw new InvalidDataException(
						$"No key value in {GdbObjectUtils.ToString(row)}");
				}

				if (! result.TryGetValue(otherRowKey, out IList<IReadOnlyRow> otherRowList))
				{
					otherRowList = new List<IReadOnlyRow>(3);

					result.Add(otherRowKey, otherRowList);
				}
				else
				{
					_msg.VerboseDebug(
						() =>
							$"The the key value {otherRowKey} in row {GdbObjectUtils.ToString(row)} is not " +
							$"unique. The rows {StringUtils.Concatenate(otherRowList, GdbObjectUtils.ToString, ", ")} " +
							"have the same value.");

					// TODO: Set property that declares the OID field not unique and use virtual composite key.
					_msg.WarnFormat(
						"{0}: Multiple right-table-rows reference the same left-table-row. The OID will be non-unique!",
						_joinedSchema.Name);
				}

				otherRowList.Add(row);
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
				// The primary key of the other table. This can be null, for example if the bridge
				// table is not a relationship table but a regular table with m:1 and 1:m
				// relationship class (TOP-5877). One side of the relationship might be missing.
				string bridgeOtherKeyValue = GetKeyValue(row, bridgeTableOtherKeyIdx);

				if (bridgeOtherKeyValue == null)
				{
					_msg.DebugFormat("No key value in {0} (field: {1}).",
					                 GdbObjectUtils.ToString(row), bridgeTableOtherKeyField);
					continue;
				}

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

		private VirtualRow GetRowManyToMany(
			long id, ManyToManyAssociationDescription m2nAssociation)
		{
			GetAssociationTableKeyFields(m2nAssociation, out string bridgeTableGeoKeyField,
			                             out string bridgeTableOtherKeyField);

			IReadOnlyTable bridgeTable = m2nAssociation.AssociationTable;

			int bridgeTableOtherKeyIdx = bridgeTable.FindField(bridgeTableOtherKeyField);
			Assert.True(bridgeTableOtherKeyIdx >= 0,
			            $"Key field {bridgeTableOtherKeyField} not found in {bridgeTable.Name}");

			int bridgeTableGeoKeyIdx = bridgeTable.FindField(bridgeTableGeoKeyField);
			Assert.True(bridgeTableGeoKeyIdx >= 0,
			            $"Key field {bridgeTableGeoKeyField} not found in {bridgeTable.Name}");

			IReadOnlyRow associationRow = bridgeTable.GetRow(id);

			// The primary key of the other table (can be null in left join)
			string bridgeOtherKeyValue = GetKeyValue(associationRow, bridgeTableOtherKeyIdx);

			// The primary key of the geo table:
			string bridgeGeoKeyValue = GetKeyValue(associationRow, bridgeTableGeoKeyIdx);

			if (bridgeGeoKeyValue == null)
			{
				if (JoinType == JoinType.RightJoin)
				{
					// TODO: Should be handled differently once RightJoin is supported:
				}

				throw new InvalidOperationException(
					$"Association record {GdbObjectUtils.ToString(associationRow)} has null foreign key in {bridgeTableGeoKeyField}. " +
					"This is no valid row for this kine of join");
			}

			var geoKeyList = new List<string> { bridgeGeoKeyValue };

			IList<IReadOnlyRow> geoFeatures = FetchRowsByKey(
					GeometryEndClass, geoKeyList, GeometryClassKeyField, false)
				.ToList();

			Assert.AreEqual(1, geoFeatures.Count,
			                $"Unexpected number of features found in {GeometryEndClass.Name} with {GeometryClassKeyField} {bridgeGeoKeyValue}. It might not exist or it was filtered or (if more than one were found, the assumed primary key is not unique).");

			IReadOnlyRow rightRow = null;

			if (bridgeOtherKeyValue != null)
			{
				var otherKeyList = new List<string> { bridgeOtherKeyValue };

				IList<IReadOnlyRow> otherFeatures = FetchRowsByKey(
					OtherEndClass, otherKeyList, OtherClassKeyField, false).ToList();

				Assert.AreEqual(1, otherFeatures.Count,
				                "Unexpected number of right table features");

				rightRow = otherFeatures[0];
			}

			return CreateJoinedFeature(geoFeatures[0], rightRow, associationRow);
		}

		private void GetAssociationTableKeyFields(ManyToManyAssociationDescription m2nAssociation,
		                                          out string bridgeTableGeoKeyField,
		                                          out string bridgeTableOtherKeyField)
		{
			if (GeometryEndClass.Equals(m2nAssociation.Table1))
			{
				bridgeTableGeoKeyField = m2nAssociation.AssociationTableKey1;
				bridgeTableOtherKeyField = m2nAssociation.AssociationTableKey2;
			}
			else
			{
				// Test for correct equals implementation:
				Assert.True(OtherEndClass.Equals(m2nAssociation.Table1),
				            "Table equality implementation: unexpected results");

				bridgeTableGeoKeyField = m2nAssociation.AssociationTableKey2;
				bridgeTableOtherKeyField = m2nAssociation.AssociationTableKey1;
			}
		}

		#endregion

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
				long ftsCount = GetTableRowCount(bridgeTable);

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
			[CanBeNull] ITableFilter filter = null)
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
				foreach (IReadOnlyRow row in TableFilterUtils.GetRowsInList(
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

		private long GetTableRowCount(IReadOnlyTable table)
		{
			if (! _tableRowStatistics.TryGetValue(table, out long rowCount))
			{
				Stopwatch watch = _msg.DebugStartTiming();

				ITableFilter filter = new AoTableFilter
				                      {
					                      SubFields = table.OIDFieldName
				                      };

				rowCount = table.RowCount(filter);

				_tableRowStatistics.Add(table, rowCount);

				_msg.DebugStopTiming(watch, "Determined row count of {0}", table.Name);
			}

			return rowCount;
		}

		[NotNull]
		private static string GetNonNullKeyValue([NotNull] IReadOnlyRow row,
		                                         int fieldIndex)
		{
			string result = GetKeyValue(row, fieldIndex);

			if (result == null)
			{
				throw new InvalidDataException(
					$"No key value in {GdbObjectUtils.ToString(row)} (field index: {fieldIndex}).");
			}

			return result;
		}

		[CanBeNull]
		private static string GetKeyValue([NotNull] IReadOnlyRow row, int fieldIndex)
		{
			object value = row.get_Value(fieldIndex);

			if (value != null && DBNull.Value != value)
			{
				return value.ToString();
			}

			return null;
		}

		private void LogQueryProperties([CanBeNull] ITableFilter filter)
		{
			_msg.DebugFormat("Querying joined table {0} using the following filter:",
			                 _joinedSchema.Name);

			if (filter == null)
			{
				_msg.Debug("NULL");
				return;
			}

			using (_msg.IncrementIndentation())
			{
				GdbQueryUtils.LogFilterProperties(TableFilterUtils.GetQueryFilter(filter));
			}
		}

		private class AssociationTableRowCache
		{
			private readonly IDictionary<string, IList<AssociationRow>>
				_knownAssociationRowsByGeoKey =
					new Dictionary<string, IList<AssociationRow>>();

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
