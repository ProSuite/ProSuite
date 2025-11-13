using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.IssueFilters
{
	[UsedImplicitly]
	public class IfAll : IssueFilter
	{
		private const bool _defaultFilter = true;

		[DocIf(nameof(DocIfStrings.IfAll_0))]
		public IfAll()
			: base(new IReadOnlyFeatureClass[] { })
		{
			Filter = _defaultFilter;
		}

		[InternallyUsedTest]
		public IfAll([NotNull] IfAllDefinition definition)
			: this()
		{
			Filter = definition.Filter;
		}

		[DocIf(nameof(DocIfStrings.IfAll_Filter))]
		[TestParameter(_defaultFilter)]
		public bool Filter { get; set; }

		public override bool Check(QaErrorEventArgs error)
		{
			return Filter;
		}
	}
}
