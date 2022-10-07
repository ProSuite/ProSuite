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
		public IList<IReadOnlyTable> Tables { get; set; }

		public override bool Check(QaErrorEventArgs error)
		{
			if (! (error.TestedRows?.Count > 0))
			{
				return false;
			}

			foreach (IReadOnlyRow row in error.TestedRows)
			{
				_tableViews = _tableViews ?? new Dictionary<IReadOnlyTable, TableView>();
				if (! _tableViews.TryGetValue(row.Table, out TableView helper))
				{
					IReadOnlyTable table = row.Table;
					if (! (Tables?.Count > 0) || Tables.Contains(table))
					{
						helper = TableViewFactory.Create(
							row.Table, _constraint, useAsConstraint: true,
							caseSensitive: false); // TODO;
					}

					_tableViews.Add(row.Table, helper);
				}

				if (helper?.MatchesConstraint(row) == true)
				{
					return true;
				}
			}

			return false;
		}
	}
}
