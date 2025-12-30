using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.Gdb;

namespace ProSuite.Commons.AGP.Core.Geodatabase;

public static class RowValuesAdapter
{
	public static IRowValues RowValues(this Row row)
	{
		return row is null ? null : new RowAdapter(row);
	}

	public static IRowValues RowValues(this RowBuffer row)
	{
		return row is null ? null : new RowBufferAdapter(row);
	}

	#region Nested type: RowAdapter

	private class RowAdapter : IRowValues
	{
		// Note: the old Evaluation.RowValues has support for a pseudo fields "_DATASET" and "_DATASET_" that return the dataset's name

		public RowAdapter(Row row)
		{
			Row = row ?? throw new ArgumentNullException(nameof(row));
			FieldNames = row.GetFields().Select(f => f.Name).ToArray();
		}

		private Row Row { get; }

		public IReadOnlyList<string> FieldNames { get; }

		public object this[int index]
		{
			get => Row[index];
			set => Row[index] = value;
		}

		public int FindField(string fieldName)
		{
			if (fieldName is null) return -1;
			return Row.FindField(fieldName);
		}

		public bool Exists(string name)
		{
			if (name is null) return false;
			return Row.FindField(name) >= 0;
		}

		public object GetValue(string name)
		{
			int index = Row.FindField(name);
			return index < 0 ? null : Row[index];
		}
	}

	#endregion

	#region Nested type: RowBufferAdapter

	private class RowBufferAdapter : IRowValues
	{
		public RowBufferAdapter(RowBuffer row)
		{
			Row = row ?? throw new ArgumentNullException(nameof(row));
			FieldNames = row.GetFields().Select(f => f.Name).ToArray();
		}

		private RowBuffer Row { get; }

		public IReadOnlyList<string> FieldNames { get; }

		public object this[int index]
		{
			get => Row[index];
			set => Row[index] = value;
		}

		public int FindField(string fieldName)
		{
			if (fieldName is null) return -1;
			return Row.FindField(fieldName);
		}

		public bool Exists(string name)
		{
			if (name is null) return false;
			return Row.FindField(name) >= 0;
		}

		public object GetValue(string name)
		{
			int index = Row.FindField(name);
			return index < 0 ? null : Row[index];
		}
	}

	#endregion
}
