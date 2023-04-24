using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests.IssueFilters
{
	[UsedImplicitly]
	public class IfAll : IssueFilter
	{
		private const bool _defaultFilter = true;

		public IfAll()
			: base(new IReadOnlyFeatureClass[] { })
		{
			Filter = _defaultFilter;
		}

		[TestParameter(_defaultFilter)]
		public bool Filter { get; set; }

		public override bool Check(QaErrorEventArgs error)
		{
			return Filter;
		}
	}
}
