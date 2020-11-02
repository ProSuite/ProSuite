using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Evaluation
{
	/// <summary>
	/// Provide additional overloads to <see cref="FieldSetter"/>
	/// suited for the Pro SDK types RowBuffer and Row, where
	/// the latter no longer derives from the former.
	/// </summary>
	public static class FieldSetterExtensions
	{
		public static IRowValues RowValues(this Row row)
		{
			return new RowValues(row);
		}

		public static IRowValues RowValues(this RowBuffer row)
		{
			return new RowBufferValues(row);
		}

		public static FieldSetter DefineFields(
			this FieldSetter instance, [NotNull] Row row, string qualifier = null)
		{
			return instance.DefineFields(row.RowValues(), qualifier);
		}

		public static FieldSetter DefineFields(
			this FieldSetter instance, [NotNull] RowBuffer row, string qualifier = null)
		{
			return instance.DefineFields(row.RowValues(), qualifier);
		}

		public static void Execute(
			this FieldSetter instance, Row row, IEvaluationEnvironment env = null)
		{
			instance.Execute(row.RowValues(), env);
		}

		public static void Execute(
			this FieldSetter instance, RowBuffer row, IEvaluationEnvironment env = null)
		{
			instance.Execute(row.RowValues(), env);
		}
	}
}
