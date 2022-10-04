using System;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class IntFieldExpression : FieldExpressionBase
	{
		public IntFieldExpression([NotNull] IReadOnlyTable table,
		                          [NotNull] string expression,
		                          bool evaluateImmediately = false,
		                          bool caseSensitive = false)
			: base(table, expression, evaluateImmediately, caseSensitive) { }

		[CanBeNull]
		public int? GetInt([NotNull] IReadOnlyRow row)
		{
			object value = GetValue(row);

			return value == null || value is DBNull
				       ? (int?) null
				       : Convert.ToInt32(value);
		}

		protected override Type GetColumnType()
		{
			return typeof(int);
		}
	}
}
