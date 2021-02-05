using System;
using ArcGIS.Core.Data;
using ProSuite.Processing.Evaluation;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// Provide additional overloads to <see cref="FieldSetter"/>
	/// suited for the Pro SDK types RowBuffer and Row, where
	/// the latter no longer derives from the former.
	/// </summary>
	// TODO Must go to ProSuite.Processing.AGP.Core (cf EsriDE.ProSuite.Processing.Utils.FieldSetterExtensions)
	public static class FieldSetterExtensions
	{
		public static IRowValues RowValues(this Row row)
		{
			return row == null ? null : new RowValues(row);
		}

		public static IRowValues RowValues(this RowBuffer row)
		{
			return row == null ? null : new RowBufferValues(row);
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
	}
}
