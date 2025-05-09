using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.IssueFilters
{
	public class IfInvolvedRows : IssueFilter
	{
		private readonly string _constraint;
		private Dictionary<IReadOnlyTable, TableView> _tableViews;
		private IList<IReadOnlyTable> _tables;

		[DocIf(nameof(DocIfStrings.IfInvolvedRows_0))]
		public IfInvolvedRows(
			[DocIf(nameof(DocIfStrings.IfInvolvedRows_constraint))]
			string constraint)
			: base(new IReadOnlyTable[] { })
		{
			_constraint = constraint;
		}

		[TestParameter]
		[DocIf(nameof(DocIfStrings.IfInvolvedRows_Tables))]
		public IList<IReadOnlyTable> Tables
		{
			get => _tables;
			set
			{
				_tables = value;

				if (_tables?.Count > 0)
				{
					foreach (IReadOnlyTable additionalTable in _tables)
					{
						AddInvolvedTable(additionalTable, null, false, true);
					}
				}
			}
		}

		public override bool Check(QaErrorEventArgs error)
		{
			if (! (error.TestedRows?.Count > 0))
			{
				return false;
			}

			foreach (IReadOnlyRow row in error.TestedRows)
			{
				_tableViews = _tableViews ?? new Dictionary<IReadOnlyTable, TableView>();

				IReadOnlyTable table = row.Table;

				if (! _tableViews.TryGetValue(table, out TableView tableView))
				{
					if (! (Tables?.Count > 0) || Tables.Contains(table))
					{
						int tableIndex = InvolvedTables.IndexOf(table);

						string tableConstraint = tableIndex < 0 ? null : GetConstraint(tableIndex);

						string resultConstraint;

						if (string.IsNullOrEmpty(tableConstraint))
						{
							resultConstraint = _constraint;
						}
						else
						{
							resultConstraint =
								string.IsNullOrEmpty(_constraint)
									? tableConstraint
									: $"{_constraint} AND {tableConstraint}";
						}

						tableView = TableViewFactory.Create(
							table, resultConstraint, useAsConstraint: true,
							caseSensitive: false);
					}
					// else: The involved row does not belong to any of the specified tables. Do nothing.

					_tableViews.Add(row.Table, tableView);
				}

				if (tableView?.MatchesConstraint(row) == true)
				{
					return true;
				}
			}

			return false;
		}
	}
}
