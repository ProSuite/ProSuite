using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class DoubleFieldExpression : FieldExpressionBase
	{
		[CLSCompliant(false)]
		public DoubleFieldExpression([NotNull] ITable table,
		                             [NotNull] string expression,
		                             bool evaluateImmediately = false,
		                             bool caseSensitive = false)
			: base(table, expression, evaluateImmediately, caseSensitive) { }

		[CLSCompliant(false)]
		[CanBeNull]
		public double? GetDouble([NotNull] IRow row)
		{
			object value = GetValue(row);

			return value == null || value is DBNull
				       ? (double?) null
				       : Convert.ToDouble(value);
		}

		protected override Type GetColumnType()
		{
			return typeof(double);
		}
	}
}