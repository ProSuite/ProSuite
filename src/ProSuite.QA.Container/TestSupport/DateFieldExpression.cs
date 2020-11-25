using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class DateFieldExpression : FieldExpressionBase
	{
		[CLSCompliant(false)]
		public DateFieldExpression([NotNull] ITable table,
		                           [NotNull] string expression,
		                           bool evaluateImmediately = false,
		                           bool caseSensitive = false)
			: base(table, expression, evaluateImmediately, caseSensitive) { }

		[CLSCompliant(false)]
		[CanBeNull]
		public DateTime? GetDateTime([NotNull] IRow row)
		{
			object value = GetValue(row);

			return value == null || value is DBNull
				       ? (DateTime?) null
				       : Convert.ToDateTime(value);
		}

		protected override Type GetColumnType()
		{
			return typeof(DateTime);
		}
	}
}
