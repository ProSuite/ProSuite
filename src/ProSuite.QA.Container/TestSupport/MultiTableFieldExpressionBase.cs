using System;
using System.Collections.Generic;
using System.Data;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public abstract class MultiTableFieldExpressionBase
	{
		private const string _columnName = "__expression";
		[NotNull] private readonly string _expression;
		private readonly bool _caseSensitive;

		[NotNull] private readonly IDictionary<TableIndexPair, MultiTableView> _constraintViews =
			new Dictionary<TableIndexPair, MultiTableView>();

		[CLSCompliant(false)]
		protected MultiTableFieldExpressionBase(
			[NotNull] string expression,
			[NotNull] string row1Alias,
			[NotNull] string row2Alias,
			bool caseSensitive = false)
		{
			Assert.ArgumentNotNullOrEmpty(expression, nameof(expression));

			// parse/remove/use case sensitivity hint
			bool? caseSensitivityOverride;
			_expression = ExpressionUtils.ParseCaseSensitivityHint(
				expression,
				out caseSensitivityOverride);

			if (caseSensitivityOverride != null)
			{
				caseSensitive = caseSensitivityOverride.Value;
			}

			Row1Alias = row1Alias;
			Row2Alias = row2Alias;

			_caseSensitive = caseSensitive;
		}

		[NotNull]
		public string Row1Alias { get; }

		[NotNull]
		public string Row2Alias { get; }

		[CLSCompliant(false)]
		[CanBeNull]
		protected object GetValue([NotNull] IRow row1, int tableIndex1,
		                          [NotNull] IRow row2, int tableIndex2)
		{
			var view = GetTableView(row1, tableIndex1, row2, tableIndex2);

			try
			{
				DataRow dataRow = Assert.NotNull(view.Add(row1, row2),
				                                 "no row added");

				return dataRow[_columnName];
			}
			finally
			{
				view.ClearRows();
			}
		}

		[NotNull]
		private MultiTableView GetTableView([NotNull] IRow row1, int tableIndex1,
		                                    [NotNull] IRow row2, int tableIndex2)
		{
			var tableIndexPair = new TableIndexPair(tableIndex1, tableIndex2);

			MultiTableView view;
			if (! _constraintViews.TryGetValue(tableIndexPair, out view))
			{
				view = CreateTableView(row1, row2);

				_constraintViews.Add(tableIndexPair, view);
			}

			return view;
		}

		[NotNull]
		private MultiTableView CreateTableView([NotNull] IRow row1,
		                                       [NotNull] IRow row2)
		{
			var table1 = row1.Table;
			var table2 = row2.Table;

			var result = TableViewFactory.Create(new[] {table1, table2},
			                                     new[] {Row1Alias, Row2Alias},
			                                     _expression,
			                                     _caseSensitive,
			                                     useAsConstraint: false);
			result.Constraint = string.Empty;
			DataColumn column = result.AddColumn(_columnName, GetColumnType());
			column.Expression = _expression;

			return result;
		}

		[NotNull]
		protected abstract Type GetColumnType();
	}
}
