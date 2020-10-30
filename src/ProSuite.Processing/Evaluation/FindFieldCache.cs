using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Evaluation
{
	public class FindFieldCache
	{
		// TODO Pro SDK: unclear if caching yields any benefits
		// TODO Pro SDK: unclear how to cache (what is a table's identity? Handle may be reused...)
		// Note Pro SDK: other than in ArcObjects, Row is NOT derived from RowBuffer

		private readonly IDictionary<FieldKey, int> _cache = new Dictionary<FieldKey, int>();

		public int GetFieldIndex(RowBuffer row, string fieldName)
		{
			if (row == null) return -1;
			if (string.IsNullOrEmpty(fieldName)) return -1;

			// cannot cache field on a row buffer
			return row.FindField(fieldName);
		}

		public int GetFieldIndex(Row row, string fieldName)
		{
			if (row == null) return -1;
			if (string.IsNullOrEmpty(fieldName)) return -1;

			// TODO Pro SDK: what is a stable row schema identity? row.Table.Handle may be reused?!?
			var key = new FieldKey(row.GetTable(), fieldName);

			int index;
			if (! _cache.TryGetValue(key, out index))
			{
				index = row.FindField(fieldName);
				_cache.Add(key, index);
			}

			return index;
		}

		public int GetFieldIndex(IRowValues row, string fieldName)
		{
			if (row == null) return -1;
			if (string.IsNullOrEmpty(fieldName)) return -1;

			if (row is RowBufferValues rowBuffer)
				return GetFieldIndex(rowBuffer.Row, fieldName);

			if (row is Utils.RowValues realRow)
				return GetFieldIndex(realRow.Row, fieldName);

			return row.FindField(fieldName);
		}

		public int GetFieldIndex(Table table, string fieldName)
		{
			if (table == null) return -1;
			if (fieldName == null) return -1;

			throw new NotImplementedException();
		}

		public void ClearAll()
		{
			_cache.Clear();
		}

		#region Nested type: FieldKey

		private readonly struct FieldKey : IEquatable<FieldKey>
		{
			private readonly IntPtr _tableHandle;
			[NotNull] private readonly string _fieldName;

			public FieldKey(Table table, string fieldName)
			{
				_tableHandle = table.Handle;
				_fieldName = (fieldName ?? string.Empty).ToUpperInvariant();
			}

			public bool Equals(FieldKey other)
			{
				return _tableHandle == other._tableHandle &&
				       string.Equals(_fieldName, other._fieldName);
			}

			public override bool Equals(object obj)
			{
				return obj is FieldKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (_tableHandle.GetHashCode() * 397) ^ _fieldName.GetHashCode();
				}
			}

			public override string ToString()
			{
				return string.Format("Hash(Table) = {0}, FieldName = {1}",
				                     _tableHandle.GetHashCode(), _fieldName);
			}
		}

		#endregion
	}
}
