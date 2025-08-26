using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.QA
{
	/// <summary>
	/// A lazy IValueList implementation that extracts values from columnar data only when requested.
	/// </summary>
	public class ColumnarValueList : IValueList
	{
		private readonly ColumnarGdbObjects _columnarData;
		private readonly int _rowIndex;
		private readonly ColumnarFieldMapping _fieldMapping;
		[CanBeNull] private readonly Func<ShapeMsg, object> _fromShapeMsgFunc;
		private readonly object[] _cachedValues;
		private readonly bool[] _valuesCached;

		public ColumnarValueList(
			[NotNull] ColumnarGdbObjects columnarData,
			int rowIndex,
			[NotNull] ColumnarFieldMapping fieldMapping,
			[CanBeNull] Func<ShapeMsg, object> fromShapeMsgFunc)
		{
			_columnarData = columnarData ?? throw new ArgumentNullException(nameof(columnarData));
			_rowIndex = rowIndex;
			_fieldMapping = fieldMapping ?? throw new ArgumentNullException(nameof(fieldMapping));

			_fromShapeMsgFunc = fromShapeMsgFunc;

			// TODO: Lazier caching strategy?
			int fieldCount = _fieldMapping.FieldCount;
			_cachedValues = new object[fieldCount];
			_valuesCached = new bool[fieldCount];
		}

		public object GetValue(int index, bool increaseRcwRefCount = false)
		{
			if (index < 0 || index >= _cachedValues.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			if (! _valuesCached[index])
			{
				_cachedValues[index] = ExtractValue(index);
				_valuesCached[index] = true;
			}

			return _cachedValues[index];
		}

		public void SetValue(int index, object value)
		{
			if (index < 0 || index >= _cachedValues.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			_cachedValues[index] = value;
			_valuesCached[index] = true;
		}

		public bool HasValue(int index)
		{
			if (index < 0 || index >= _cachedValues.Length)
			{
				return false;
			}

			return _valuesCached[index] || _fieldMapping.HasColumnMapping(index);
		}

		private object ExtractValue(int fieldIndex)
		{
			if (! _fieldMapping.TryGetColumnIndex(fieldIndex, out int columnIndex))
			{
				// Field not present in columnar data
				return DBNull.Value;
			}

			var column = _columnarData.Columns[columnIndex];
			ITableField field = _fieldMapping.GetField(fieldIndex);

			return ExtractValueFromColumn(column, _rowIndex, field.FieldType, field.Name);
		}

		private object ExtractValueFromColumn(
			[NotNull] Column column,
			int rowIndex,
			FieldType fieldType,
			string fieldName)
		{
			try
			{
				// Check null bitmap first (now on column level)
				if (IsNull(column.Nulls.ToByteArray(), rowIndex))
				{
					return DBNull.Value;
				}

				switch (fieldType)
				{
					case FieldType.ShortInteger:
						return column.ShortIntValues?.Values[rowIndex];

					case FieldType.Integer:
					case FieldType.ObjectID:
						return column.IntValues?.Values[rowIndex];

					case FieldType.BigInteger:
						return column.BigIntValues?.Values[rowIndex];

					case FieldType.Float:
						return column.FloatValues?.Values[rowIndex];

					case FieldType.Double:
						return column.DoubleValues?.Values[rowIndex];

					case FieldType.Text:
						return column.StringValues?.Values[rowIndex];

					case FieldType.Date:
						if (column.DateTimeTicksValues?.Values[rowIndex] is long ticks)
						{
							return new DateTime(ticks);
						}

						return DBNull.Value;

					case FieldType.Guid:
					case FieldType.GlobalID:
						var uuidColumn = column.UuidValues;
						if (uuidColumn?.Values[rowIndex] != null)
						{
							var uuid = uuidColumn.Values[rowIndex];
							if (uuid?.Value != null)
							{
								var guid = new Guid(uuid.Value.ToByteArray());
								return guid.ToString("B").ToUpper();
							}
						}

						return DBNull.Value;

					case FieldType.Blob:
						return column.ByteValues?.Values[rowIndex]?.ToByteArray();

					case FieldType.Xml:
						return column.StringValues?.Values[rowIndex];

					case FieldType.Geometry:
						ShapeMsg shapeMsg = column.Geometries?.Shapes[rowIndex];

						return _fromShapeMsgFunc == null ? shapeMsg : _fromShapeMsgFunc(shapeMsg);

					case FieldType.Raster:
						// Not supported
						return null;

					default:
						return DBNull.Value;
				}
			}
			catch (Exception)
			{
				return DBNull.Value;
			}
		}

		private static bool IsNull(byte[] nullBitmap, int rowIndex)
		{
			int byteIndex = rowIndex / 8;
			if (byteIndex >= nullBitmap.Length)
				return false;

			int bitPosition = rowIndex % 8;
			return (nullBitmap[byteIndex] & (1 << bitPosition)) != 0;
		}
	}

	/// <summary>
	/// Helper class that manages the mapping between table field indexes and columnar data column indexes.
	/// </summary>
	public class ColumnarFieldMapping
	{
		private readonly IReadOnlyList<ITableField> _tableFields;
		private readonly Dictionary<int, int> _fieldToColumnMapping;
		private readonly Dictionary<string, int> _columnNameToIndex;

		public ColumnarFieldMapping(
			[NotNull] IReadOnlyList<ITableField> tableFields,
			[NotNull] ColumnarGdbObjects columnarData)
		{
			_tableFields = tableFields ?? throw new ArgumentNullException(nameof(tableFields));
			_fieldToColumnMapping = new Dictionary<int, int>();
			_columnNameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			// Build column name to index mapping
			for (int i = 0; i < columnarData.Columns.Count; i++)
			{
				_columnNameToIndex[columnarData.Columns[i].Name] = i;
			}

			// Build field to column mapping
			for (int fieldIndex = 0; fieldIndex < _tableFields.Count; fieldIndex++)
			{
				string fieldName = _tableFields[fieldIndex].Name;
				if (_columnNameToIndex.TryGetValue(fieldName, out int columnIndex))
				{
					_fieldToColumnMapping[fieldIndex] = columnIndex;
				}
			}
		}

		public int FieldCount => _tableFields.Count;

		public bool TryGetColumnIndex(int fieldIndex, out int columnIndex)
		{
			return _fieldToColumnMapping.TryGetValue(fieldIndex, out columnIndex);
		}

		public bool HasColumnMapping(int fieldIndex)
		{
			return _fieldToColumnMapping.ContainsKey(fieldIndex);
		}

		public ITableField GetField(int fieldIndex)
		{
			if (fieldIndex < 0 || fieldIndex >= _tableFields.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(fieldIndex));
			}

			return _tableFields[fieldIndex];
		}

		public long GetOidForRow(ColumnarGdbObjects columnarData, int rowIndex, string oidFieldName)
		{
			if (string.IsNullOrEmpty(oidFieldName))
			{
				return -1;
			}

			if (! _columnNameToIndex.TryGetValue(oidFieldName, out int oidColumnIndex))
			{
				return -1;
			}

			var column = columnarData.Columns[oidColumnIndex];
			if (IsNull(column.Nulls.ToByteArray(), rowIndex))
			{
				return -1;
			}

			if (column.IntValues != null)
			{
				return column.IntValues.Values[rowIndex];
			}

			if (column.BigIntValues != null)
			{
				return column.BigIntValues.Values[rowIndex];
			}

			return -1;
		}

		private static bool IsNull(byte[] nullBitmap, int rowIndex)
		{
			int byteIndex = rowIndex / 8;
			if (byteIndex >= nullBitmap.Length)
				return false;

			int bitPosition = rowIndex % 8;
			return (nullBitmap[byteIndex] & (1 << bitPosition)) != 0;
		}
	}
}
