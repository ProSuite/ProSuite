using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container
{
	public class QueryFilterHelper
	{
		private readonly TableView _tableView;

		public QueryFilterHelper([NotNull] IReadOnlyTable table,
		                         [CanBeNull] string constraint,
		                         bool caseSensitive)
		{
			const bool useAsConstraint = true;
			_tableView = TableViewFactory.Create(table, constraint, useAsConstraint,
			                                     caseSensitive);

			SubFields = _tableView.SubFields;
		}

		public ContainerTest ContainerTest { get; set; }
		public TableView TableView => _tableView;

		public bool ForNetwork { get; set; }
		public bool? RepeatCachedRows { get; set; }

		public string SubFields { get; }

		public int MinimumOID { get; set; }

		public bool AttributeFirst { get; set; } = true;

		public bool MatchesConstraint([NotNull] IReadOnlyRow row)
		{
			bool match = _tableView.MatchesConstraint(row);
			return match;
		}
	}
}
