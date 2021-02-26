using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class IntFieldExpression : FieldExpressionBase
	{
		public IntFieldExpression([NotNull] ITable table,
		                          [NotNull] string expression,
		                          bool evaluateImmediately = false,
		                          bool caseSensitive = false)
			: base(table, expression, evaluateImmediately, caseSensitive) { }

		[CanBeNull]
		public int? GetInt([NotNull] IRow row)
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
