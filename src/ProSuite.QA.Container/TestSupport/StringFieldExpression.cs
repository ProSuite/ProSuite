using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class StringFieldExpression : FieldExpressionBase
	{
		[CLSCompliant(false)]
		public StringFieldExpression([NotNull] ITable table,
		                             [NotNull] string expression,
		                             bool evaluateImmediately = false,
		                             bool caseSensitive = false)
			: base(table, expression, evaluateImmediately, caseSensitive) { }

		[CanBeNull]
		[CLSCompliant(false)]
		public string GetString([NotNull] IRow row)
		{
			object value = GetValue(row);

			return value == null || value is DBNull
				       ? null
				       : Convert.ToString(value);
		}

		protected override Type GetColumnType()
		{
			return typeof(string);
		}
	}
}
