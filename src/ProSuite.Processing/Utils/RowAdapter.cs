using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Processing.Evaluation;

namespace ProSuite.Processing.Utils
{
	public class RowValues : IRowValues, INamedValues
	{
		// TODO the old Evaluation.RowValues has support for a pseudo fields "_DATASET" and "_DATASET_" that return the dataset's name
		public RowValues(Row row)
		{
			Row = row ?? throw new ArgumentNullException();
			FieldNames = row.GetFields().Select(f => f.Name).ToArray();
		}

		public Row Row { get; }

		public IReadOnlyList<string> FieldNames { get; }

		public object this[int index]
		{
			get => Row[index];
			set => Row[index] = value;
		}

		public int FindField(string fieldName)
		{
			if (fieldName == null) return -1;
			return Row.FindField(fieldName);
		}

		public bool Exists(string name)
		{
			if (name == null) return false;
			return Row.FindField(name) >= 0;
		}

		public object GetValue(string name)
		{
			int index = Row.FindField(name);
			return index < 0 ? null : Row[index];
		}
	}

	public class RowBufferValues : IRowValues, INamedValues
	{
		public RowBufferValues(RowBuffer row)
		{
			Row = row ?? throw new ArgumentNullException();
			FieldNames = row.GetFields().Select(f => f.Name).ToArray();
		}

		public RowBuffer Row { get; }

		public IReadOnlyList<string> FieldNames { get; }

		public object this[int index]
		{
			get => Row[index];
			set => Row[index] = value;
		}

		public int FindField(string fieldName)
		{
			if (fieldName == null) return -1;
			return Row.FindField(fieldName);
		}

		public bool Exists(string name)
		{
			if (name == null) return false;
			return Row.FindField(name) >= 0;
		}

		public object GetValue(string name)
		{
			int index = Row.FindField(name);
			return index < 0 ? null : Row[index];
		}
	}
}
