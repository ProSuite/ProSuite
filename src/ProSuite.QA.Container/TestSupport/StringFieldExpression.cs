using System;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class StringFieldExpression : FieldExpressionBase
	{
		public StringFieldExpression([NotNull] IReadOnlyTable table,
		                             [NotNull] string expression,
		                             bool evaluateImmediately = false,
		                             bool caseSensitive = false)
			: base(table, expression, evaluateImmediately, caseSensitive) { }

		[CanBeNull]
		public string GetString([NotNull] IReadOnlyRow row)
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
