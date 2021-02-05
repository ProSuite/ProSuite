using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Processing.Evaluation;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.AGP.Core
{
	public static class Extensions
	{
		public static IRowValues RowValues(this Row row)
		{
			return row == null ? null : new RowAdapter(row);
		}

		public static IRowValues RowValues(this RowBuffer row)
		{
			return row == null ? null : new RowBufferAdapter(row);
		}

		public static FieldSetter DefineFields(
			this FieldSetter instance, Row row, string qualifier = null)
		{
			if (instance == null) return null;
			return instance.DefineFields(row.RowValues(), qualifier);
		}

		public static FieldSetter DefineFields(
			this FieldSetter instance, RowBuffer row, string qualifier = null)
		{
			if (instance == null) return null;
			return instance.DefineFields(row.RowValues(), qualifier);
		}

		public static void Execute(
			this FieldSetter instance, Row row, IEvaluationEnvironment env = null)
		{
			if (instance == null)
				throw new ArgumentNullException(nameof(instance));
			instance.Execute(row.RowValues(), env);
		}

		public static void Execute(
			this FieldSetter instance, RowBuffer row, IEvaluationEnvironment env = null)
		{
			if (instance == null)
				throw new ArgumentNullException(nameof(instance));
			instance.Execute(row.RowValues(), env);
		}

		#region Nested type: RowAdapter

		private class RowAdapter : IRowValues
		{
			// TODO the old Evaluation.RowValues has support for a pseudo fields "_DATASET" and "_DATASET_" that return the dataset's name

			public RowAdapter(Row row)
			{
				Row = row ?? throw new ArgumentNullException();
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

		#endregion

		#region Nested type: RowBufferAdapter

		private class RowBufferAdapter : IRowValues
		{
			public RowBufferAdapter(RowBuffer row)
			{
				Row = row ?? throw new ArgumentNullException();
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

		#endregion
	}
}
