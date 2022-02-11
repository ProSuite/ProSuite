using System;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class MultiTableDoubleFieldExpression : MultiTableFieldExpressionBase
	{
		public MultiTableDoubleFieldExpression([NotNull] string expression,
		                                       [NotNull] string row1Alias,
		                                       [NotNull] string row2Alias,
		                                       bool caseSensitive = false)
			: base(expression, row1Alias, row2Alias, caseSensitive) { }

		[CanBeNull]
		public double? GetDouble([NotNull] IReadOnlyRow row1, int tableIndex1,
		                         [NotNull] IReadOnlyRow row2, int tableIndex2)
		{
			object value = GetValue(row1, tableIndex1, row2, tableIndex2);

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
