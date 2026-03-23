using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container
{
	public class QueryFilterHelper
	{
		public QueryFilterHelper([NotNull] IReadOnlyTable table,
		                         [CanBeNull] string constraint,
		                         bool caseSensitive)
		{
			constraint = ExpressionUtils.AdaptSdeFieldExpression(table, constraint);

			const bool useAsConstraint = true;
			TableView = TableViewFactory.Create(table, constraint, useAsConstraint,
			                                    caseSensitive);

			SubFields = TableView.SubFields;
		}

		public ContainerTest ContainerTest { get; set; }
		public TableView TableView { get; }

		public bool ForNetwork { get; set; }

		public bool FullGeometrySearch { get; set; }

		public string SubFields { get; }

		public long MinimumOID { get; set; }

		public bool AttributeFirst { get; set; } = true;

		public bool MatchesConstraint([NotNull] IReadOnlyRow row)
		{
			bool match = TableView.MatchesConstraint(row);
			return match;
		}
	}
}
