using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests.IssueFilters
{
	public class IfInvolvedRows : IssueFilter
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly string _constraint;
		private Dictionary<ITable, TableView> _tableViews;

		public IfInvolvedRows(string constraint)
			: base(new ITable[] { })
		{
			_constraint = constraint;
		}

		[TestParameter]
		public IList<string> TableNames { get; set; }

		public override bool Check(QaErrorEventArgs error)
		{
			if (! (error.TestedRows?.Count > 0))
			{
				return false;
			}

			foreach (IRow row in error.TestedRows)
			{
				_tableViews = _tableViews ?? new Dictionary<ITable, TableView>();
				if (! _tableViews.TryGetValue(row.Table, out TableView helper))
				{
					string tableName = DatasetUtils.GetName(row.Table);
					if (! (TableNames?.Count > 0) || TableNames.Contains(tableName))
					{
						bool caseSensitivity = false; // TODO;
						helper = TableViewFactory.Create(
							row.Table, _constraint, useAsConstraint: true,
							caseSensitive: caseSensitivity);
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
