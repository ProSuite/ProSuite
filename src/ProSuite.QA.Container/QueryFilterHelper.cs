using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container
{
	public class QueryFilterHelper
	{
		private readonly TableView _tableView;

		[CLSCompliant(false)]
		public QueryFilterHelper([NotNull] ITable table,
		                         [CanBeNull] string constraint,
		                         bool caseSensitive)
		{
			const bool useAsConstraint = true;
			_tableView = TableViewFactory.Create(table, constraint, useAsConstraint,
			                                     caseSensitive);

			SubFields = _tableView.SubFields;
		}

		[CLSCompliant(false)]
		public ContainerTest ContainerTest { get; set; }

		public bool ForNetwork { get; set; }

		public string SubFields { get; }

		public int MinimumOID { get; set; }

		public bool AttributeFirst { get; set; } = true;

		[CLSCompliant(false)]
		public bool MatchesConstraint([NotNull] IRow row)
		{
			bool match = _tableView.MatchesConstraint(row);
			return match;
		}
	}
}
