using System;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class DateFieldExpression : FieldExpressionBase
	{
		public DateFieldExpression([NotNull] IReadOnlyTable table,
		                           [NotNull] string expression,
		                           bool evaluateImmediately = false,
		                           bool caseSensitive = false)
			: base(table, expression, evaluateImmediately, caseSensitive) { }

		[CanBeNull]
		public DateTime? GetDateTime([NotNull] IReadOnlyRow row)
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
