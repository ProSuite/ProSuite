using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public abstract class IssueFilter : InvolvesTablesBase, IIssueFilter
	{
		public string Name { get; set; }

		protected IssueFilter([NotNull] IEnumerable<IReadOnlyTable> tables)
			: base(tables) { }

		public abstract bool Check(QaErrorEventArgs error);
	}
}
