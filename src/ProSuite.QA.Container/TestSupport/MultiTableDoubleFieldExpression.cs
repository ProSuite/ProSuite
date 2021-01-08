using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class MultiTableDoubleFieldExpression : MultiTableFieldExpressionBase
	{
		[CLSCompliant(false)]
		public MultiTableDoubleFieldExpression([NotNull] string expression,
		                                       [NotNull] string row1Alias,
		                                       [NotNull] string row2Alias,
		                                       bool caseSensitive = false)
			: base(expression, row1Alias, row2Alias, caseSensitive) { }

		[CLSCompliant(false)]
		[CanBeNull]
		public double? GetDouble([NotNull] IRow row1, int tableIndex1,
		                         [NotNull] IRow row2, int tableIndex2)
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
