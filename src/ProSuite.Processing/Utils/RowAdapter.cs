using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Processing.Evaluation;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// An abstraction of Row and RowBuffer; other than
	/// in ArcObjects, with Pro SDK Row is not derived
	/// from RowBuffer.
	/// </summary>
	public interface IRowValues
	{
		IReadOnlyList<Field> Fields { get; }

		object this[int index] { get; set; }

		int FindField(string fieldName);
	}

	public class RowValues : IRowValues, INamedValues
	{
		// TODO the old Evaluation.RowValues has support for a pseudo fields "_DATASET" and "_DATASET_" that return the dataset's name
		public RowValues(Row row)
		{
			Row = row ?? throw new ArgumentNullException();
		}

		public Row Row { get; }

		public IReadOnlyList<Field> Fields => Row.GetFields();

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
		}

		public RowBuffer Row { get; }

		public IReadOnlyList<Field> Fields => Row.GetFields();

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
