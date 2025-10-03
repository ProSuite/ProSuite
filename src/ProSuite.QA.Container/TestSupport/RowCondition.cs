using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Container.TestSupport
{
	public class RowCondition
	{
		[NotNull] private readonly IReadOnlyTable _table;
		private readonly bool _undefinedConstraintIsFulfilled;
		[CanBeNull] private readonly TableView _tableView;

		public RowCondition([NotNull] IReadOnlyTable table,
		                    [CanBeNull] string condition,
		                    bool undefinedConstraintIsFulfilled = false,
		                    bool caseSensitive = false)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
			_undefinedConstraintIsFulfilled = undefinedConstraintIsFulfilled;

			if (StringUtils.IsNotEmpty(condition))
			{
				const bool useAsConstraint = true;
				_tableView = TableViewFactory.Create(table, condition, useAsConstraint,
				                                     caseSensitive);
			}
		}

		public bool IsFulfilled([NotNull] IReadOnlyRow row)
		{
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentCondition(row.Table.Equals(_table), "table does not match");

			return _tableView?.MatchesConstraint(row) ?? _undefinedConstraintIsFulfilled;
		}
	}
}
