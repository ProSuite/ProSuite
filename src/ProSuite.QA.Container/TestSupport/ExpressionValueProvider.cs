using System;
using System.Data;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ExpressionValueProvider<T> : IValueProvider<T> where T : struct
	{
		private const string _expressionColumnName = "__valueExpression";

		private readonly IReadOnlyTable _table;
		private readonly string _expression;

		private readonly TableView _expressionView;

		public ExpressionValueProvider([NotNull] IReadOnlyTable table,
		                               [NotNull] string expression,
		                               bool caseSensitive = false)
		{
			_table = table;

			// parse/remove/use case sensitivty hint
			bool? caseSensitivityOverride;
			_expression = ExpressionUtils.ParseCaseSensitivityHint(expression,
			                                                       out caseSensitivityOverride);
			if (caseSensitivityOverride != null)
			{
				caseSensitive = caseSensitivityOverride.Value;
			}

			_expressionView = GetValueView(caseSensitive);
		}

		[NotNull]
		private TableView GetValueView(bool caseSensitive)
		{
			const bool useAsConstraint = false;

			TableView valueView = TableViewFactory.Create(_table, _expression,
			                                              useAsConstraint, caseSensitive);
			valueView.Constraint = string.Empty;
			DataColumn column = valueView.AddColumn(_expressionColumnName, typeof(T));
			column.Expression = _expression;

			return valueView;
		}

		T? IValueProvider<T>.GetValue(IReadOnlyRow row)
		{
			_expressionView.ClearRows();

			DataRow dataRow = Assert.NotNull(_expressionView.Add(row));

			object value = dataRow[_expressionColumnName];
			if (value == null || value == DBNull.Value)
			{
				return null;
			}

			var typeValue = (T) value;
			return typeValue;
		}
	}
}
