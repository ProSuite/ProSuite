using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		[NotNull] private readonly IRelationshipClass _relationshipClass;
		[NotNull] private readonly IFeatureClass _geometryEndClass;
		[NotNull] private readonly IFeatureClass _joinedSchema;
		[CanBeNull] private readonly string _primaryKeyFieldName;
		private readonly RelationshipClassJoinDefinition _joinDefinition;
		private IDictionary<int, int> _geometryEndCopyMatrix;
		private IDictionary<int, int> _otherEndCopyMatrix;
		private readonly IObjectClass _otherEndClass;

		private readonly AssociationDescription _associationDescription;

		public JoinedDataset([NotNull] IRelationshipClass relationshipClass,
		                     [NotNull] IFeatureClass geometryEndClass,
		                     [NotNull] IFeatureClass joinedSchema)
		{
			_relationshipClass = relationshipClass;
			_geometryEndClass = geometryEndClass;
			_otherEndClass =
				RelationshipClassUtils.GetOtherEndObjectClass(relationshipClass, geometryEndClass);

			_joinedSchema = joinedSchema;

			_associationDescription =
				AssociationDescriptionUtils.CreateAssociationDescription(_relationshipClass);
		}

		public override IEnvelope Extent => ((IGeoDataset) _geometryEndClass).Extent;

		public override IRow GetRow(int id)
		{
			throw new NotImplementedException();
		}

		public override int GetRowCount(IQueryFilter queryFilter)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<IRow> Search(IQueryFilter filter, bool recycling)
		{
			HashSet<string> fClassKeys = new HashSet<string>();

			GetKeyFieldNames(out string featureClassKeyField, out string otherClassKeyField);

			Assert.NotNull(_otherEndClass);
			Assert.NotNull(otherClassKeyField);

			string originalSubfields = filter.SubFields;
			filter.SubFields = featureClassKeyField;

			int featureClassKeyIdx = _geometryEndClass.FindField(featureClassKeyField);
			Assert.True(featureClassKeyIdx >= 0, $"Key field not found: {featureClassKeyIdx}");

			Stopwatch watch = _msg.DebugStartTiming();

			foreach (IFeature feature in GdbQueryUtils.GetFeatures(_geometryEndClass, filter, true))
			{
				string keyValue = GetKeyValue(feature, featureClassKeyIdx);

				if (keyValue != null)
					fClassKeys.Add(keyValue);
			}

			_msg.DebugStopTiming(watch, "Initial search found {0} geo-keys", fClassKeys.Count);

			IDictionary<string, IList<IRow>> otherRows =
				GetOtherRowListsByFeatureKey(otherClassKeyField, fClassKeys);

			// Now get the actual features again:
			filter.SubFields = originalSubfields;

			watch = _msg.DebugStartTiming();
			foreach (IFeature feature in GdbQueryUtils.GetRowsInList<IFeature>(
				         (ITable) _geometryEndClass, featureClassKeyField, otherRows.Keys, true,
				         filter))
			{
				string keyValue = Assert.NotNull(GetKeyValue(feature, featureClassKeyIdx));

				foreach (IRow otherRow in otherRows[keyValue])
				{
					yield return CreateJoinedFeature(feature, otherRow);
				}
			}

			_msg.DebugStopTiming(watch, "Final read of geo-features with result processing");
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

		private IRow CreateJoinedFeature(IFeature feature, IRow otherRow)
		{
			IFeature resultFeature = new GdbFeature(feature.OID, _joinedSchema);

			for (int i = 0; i < feature.Fields.FieldCount; i++)
			{
				int targetIndex = GeometryEndCopyMatrix[i];
				resultFeature.set_Value(targetIndex, feature.Value[i]);
			}

			for (int i = 0; i < otherRow.Fields.FieldCount; i++)
			{
				int targetIndex = OtherEndCopyMatrix[i];
				resultFeature.set_Value(targetIndex, otherRow.Value[i]);
			}

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

		//private void AddOtherRows(
		//	[NotNull] string otherClassField,
		//	[NotNull] HashSet<string> keyValues)
		//{
		//	// Get the non-feature-rows:
		//	int otherClassKeyIdx = _otherEndClass.FindField(otherClassField);
		//	string otherTableName = DatasetUtils.GetName(_otherEndClass);
		//	Assert.True(otherClassKeyIdx >= 0,
		//	            $"Key field {otherClassField} not found in {otherTableName}");

		//	IDictionary<string, IRow> otherRows = new Dictionary<string, IRow>();
		//	foreach (IRow row in GdbQueryUtils.GetRowsInList(
		//		         (ITable)_otherEndClass, otherClassField, keyValues, true))
		//	{
		//		object otherRowKey = row.get_Value(otherClassKeyIdx);
		//		Assert.True(otherRowKey != null && DBNull.Value != otherRowKey,
		//		            $"No key value in {otherTableName}");

		//		otherRows.Add(otherRowKey.ToString(), row);
		//	}

		//	return otherRows;
		//}

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
					fClassKeys.Count, geoKeysByOtherKey);

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
				result.Add(keyValuePair.Key, new List<IRow> {keyValuePair.Value});
			}

			return result;
		}

		private Dictionary<string, List<string>> GeoKeysByOtherKeyManyToMany(
			[NotNull] IEnumerable<string> geoKeys,
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

			foreach (IRow row in GdbQueryUtils.GetRowsInList(
				         bridgeTable, bridgeTableGeoKeyField, geoKeys, false))
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

		private IDictionary<int, int> GeometryEndCopyMatrix
		{
			get
			{
				if (_geometryEndCopyMatrix == null)
				{
					_geometryEndCopyMatrix = GdbObjectUtils.CreateMatchingIndexMatrix(
						_joinedSchema, _geometryEndClass, true, true, null,
						FieldComparison.FieldName);
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
					_otherEndCopyMatrix = GdbObjectUtils.CreateMatchingIndexMatrix(
						_joinedSchema, _otherEndClass, true, true, null, FieldComparison.FieldName);
				}

				return _otherEndCopyMatrix;
			}
		}

		private static string GetKeyValue(IRow row, int fieldIndex)
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
