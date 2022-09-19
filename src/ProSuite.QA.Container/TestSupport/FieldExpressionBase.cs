using System;
using System.Data;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public abstract class FieldExpressionBase
	{
		private const string _columnName = "__expression";
		private readonly IReadOnlyTable _table;
		private readonly string _expression;
		private readonly bool _caseSensitive;

		private TableView _tableView;

		protected FieldExpressionBase([NotNull] IReadOnlyTable table,
		                              [NotNull] string expression,
		                              bool evaluateImmediately = false,
		                              bool caseSensitive = false)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(expression, nameof(expression));

			_table = table;

			// parse/remove/use case sensitivity hint
			bool? caseSensitivityOverride;
			_expression = ExpressionUtils.ParseCaseSensitivityHint(expression,
			                                                       out caseSensitivityOverride);
			if (caseSensitivityOverride != null)
			{
				caseSensitive = caseSensitivityOverride.Value;
			}

			_caseSensitive = caseSensitive;

			if (evaluateImmediately)
			{
				_tableView = CreateTableView(_table, _expression, _caseSensitive);
			}
		}

		[CanBeNull]
		protected object GetValue([NotNull] IReadOnlyRow row)
		{
			if (_tableView == null)
			{
				_tableView = CreateTableView(_table, _expression, _caseSensitive);
			}

			try
			{
				DataRow dataRow = Assert.NotNull(_tableView.Add(row), "no row added");

				return dataRow[_columnName];
			}
			finally
			{
				_tableView.ClearRows();
			}
		}

		[NotNull]
		private TableView CreateTableView([NotNull] IReadOnlyTable table,
		                                  [NotNull] string expression,
		                                  bool caseSensitive)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(expression, nameof(expression));

			const bool useAsConstraint = false;
			TableView result = TableViewFactory.Create(table, expression,
			                                           useAsConstraint,
			                                           caseSensitive);
			result.Constraint = string.Empty;
			DataColumn column = result.AddColumn(_columnName, GetColumnType());
			column.Expression = expression;

			return result;
		}

		[NotNull]
		protected abstract Type GetColumnType();
	}
}
