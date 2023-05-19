using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Db;
using ProSuite.Commons.Diagnostics;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Tests.KeySets
{
	/// <summary>
	/// Utility methods for working with KeySets
	/// </summary>
	internal static class KeySetUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static IKeySet ReadKeySet([NotNull] IReadOnlyTable table,
		                                 [NotNull] string keyField,
		                                 [CanBeNull] string whereClause)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(keyField, nameof(keyField));

			int keyFieldIndex = table.FindField(keyField);
			Assert.ArgumentCondition(keyFieldIndex >= 0, "Field {0} not found in table {1}",
			                         keyField, table.Name);

			esriFieldType keyFieldType = GetFieldValueType(table, keyFieldIndex);

			return ReadKeySet(table, keyField, whereClause, keyFieldType, keyFieldIndex);
		}

		public static esriFieldType GetFieldValueType([NotNull] IReadOnlyTable table,
		                                              int fieldIndex)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentCondition(fieldIndex >= 0, "Invalid field index: {0}", fieldIndex);

			esriFieldType fieldType = table.Fields.Field[fieldIndex].Type;

			return fieldType == esriFieldType.esriFieldTypeOID
				       ? esriFieldType.esriFieldTypeInteger
				       : fieldType;
		}

		public static bool IsSupportedTypeCombination(esriFieldType foreignKeyFieldType,
		                                              esriFieldType referencedKeyFieldType)
		{
			if (foreignKeyFieldType == referencedKeyFieldType)
			{
				return true;
			}

			return IsSupportedKeyType(foreignKeyFieldType) &&
			       IsSupportedKeyType(referencedKeyFieldType);
		}

		[NotNull]
		internal static IKeySet ReadKeySet([NotNull] IReadOnlyTable table,
		                                   [NotNull] string keyField,
		                                   [CanBeNull] string whereClause,
		                                   esriFieldType keyFieldType,
		                                   int keyFieldIndex)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(keyField, nameof(keyField));

			Stopwatch watch = null;
			MemoryUsageInfo memoryUsage = null;
			if (_msg.IsVerboseDebugEnabled)
			{
				watch = _msg.DebugStartTiming();
				memoryUsage = new MemoryUsageInfo();
				memoryUsage.Refresh();
			}

			IKeySet result = CreateKeySet(keyFieldType);

			var queryFilter = new AoTableFilter()
			                  {
				                  SubFields = keyField,
				                  WhereClause = whereClause
			                  };
			string tableName = table.Name;

			const bool recycle = true;
			foreach (IReadOnlyRow row in table.EnumRows(queryFilter, recycle))
			{
				object key = row.get_Value(keyFieldIndex);

				if (key == DBNull.Value || key == null)
				{
					continue;
				}

				// TODO handle errors (e.g. invalid guid strings)
				bool added = result.Add(key);

				if (! added)
				{
					_msg.VerboseDebug(
						() =>
							$"Ignored duplicate key found in field '{keyField}' in table '{tableName}': {key}");
				}
			}

			if (watch != null)
			{
				_msg.DebugStopTiming(watch,
				                     "Reading {0:N0} {1} keys from field '{2}' in table '{3}'",
				                     result.Count, keyFieldType, keyField,
				                     table.Name);
				_msg.DebugFormat("Memory usage of keys: {0}", memoryUsage);
			}

			return result;
		}

		[NotNull]
		public static IKeySet CreateKeySet(esriFieldType referencedKeyFieldType)
		{
			switch (referencedKeyFieldType)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
					return new KeySet<short>();

				case esriFieldType.esriFieldTypeInteger:
				case esriFieldType.esriFieldTypeOID:
					return new KeySet<int>();

				case esriFieldType.esriFieldTypeSingle:
					return new KeySet<float>();

				case esriFieldType.esriFieldTypeDouble:
					return new KeySet<double>();

				case esriFieldType.esriFieldTypeString:
					return new KeySet<string>(); // TODO: case sensitivity?

				case esriFieldType.esriFieldTypeDate:
					return new KeySet<DateTime>();

				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeGlobalID:
					return new GuidKeySet();

				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
				case esriFieldType.esriFieldTypeXML:
					throw new ArgumentOutOfRangeException(
						nameof(referencedKeyFieldType),
						$@"Unsupported key field type: {referencedKeyFieldType}");

				default:
					throw new ArgumentOutOfRangeException(nameof(referencedKeyFieldType),
					                                      @"Unknown field type");
			}
		}

		[NotNull]
		internal static ITupleKeySet ReadTupleKeySet(
			[NotNull] IReadOnlyTable table,
			[NotNull] ICollection<string> keyFields,
			[CanBeNull] string whereClause,
			[NotNull] IList<esriFieldType> keyFieldTypes,
			[NotNull] IList<int> keyFieldIndices)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(keyFields, nameof(keyFields));
			Assert.ArgumentNotNull(keyFieldTypes, nameof(keyFieldTypes));
			Assert.ArgumentNotNull(keyFieldIndices, nameof(keyFieldIndices));

			Stopwatch watch = null;
			MemoryUsageInfo memoryUsage = null;
			if (_msg.IsVerboseDebugEnabled)
			{
				watch = _msg.DebugStartTiming();
				memoryUsage = new MemoryUsageInfo();
				memoryUsage.Refresh();
			}

			var fieldIndices = new List<int>();

			foreach (string keyField in keyFields)
			{
				int fieldIndex = table.FindField(keyField);
				Assert.True(fieldIndex >= 0, "field '{0}' not found in table {1}",
				            keyField, table.Name);
				fieldIndices.Add(fieldIndex);
			}

			ITupleKeySet result = new TupleKeySet();

			ITableFilter queryFilter = GetQueryFilter(keyFields, whereClause);

			string keyFieldsString = StringUtils.Concatenate(keyFields, ",");
			string tableName = table.Name;

			const bool recycle = true;
			foreach (IReadOnlyRow row in table.EnumRows(queryFilter, recycle))
			{
				Tuple tuple = ReadTuple(row, fieldIndices);

				bool added = result.Add(tuple);

				if (! added)
				{
					_msg.DebugFormat(
						"Ignored duplicate value combination found in fields '{0}' in table '{1}': {2}",
						keyFieldsString, tableName, tuple);
				}
			}

			if (watch != null)
			{
				_msg.DebugStopTiming(watch,
				                     "Reading {0:N0} keys from fields '{1}' in table '{2}'",
				                     result.Count, queryFilter.SubFields,
				                     table.Name);
				_msg.DebugFormat("Memory usage of keys: {0}", memoryUsage);
			}

			return result;
		}

		private static bool IsSupportedKeyType(esriFieldType fieldType)
		{
			switch (fieldType)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
				case esriFieldType.esriFieldTypeInteger:
				case esriFieldType.esriFieldTypeSingle:
				case esriFieldType.esriFieldTypeDouble:
				case esriFieldType.esriFieldTypeString:
				case esriFieldType.esriFieldTypeDate:
				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeGlobalID:
					return true;

				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
				case esriFieldType.esriFieldTypeXML:
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(fieldType));
			}
		}

		[NotNull]
		private static ITableFilter GetQueryFilter([NotNull] IEnumerable<string> keyFields,
		                                           [CanBeNull] string whereClause)
		{
			var result = new AoTableFilter {WhereClause = whereClause};

			TableFilterUtils.SetSubFields(result, keyFields);

			return result;
		}

		[NotNull]
		private static Tuple ReadTuple([NotNull] IReadOnlyRow row,
		                               [NotNull] ICollection<int> fieldIndices)
		{
			var values = new List<object>(fieldIndices.Count);

			values.AddRange(fieldIndices.Select(fieldIndex => row.get_Value(fieldIndex)));

			return new Tuple(values);
		}
	}
}
