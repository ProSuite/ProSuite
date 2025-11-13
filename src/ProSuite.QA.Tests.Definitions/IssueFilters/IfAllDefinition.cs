using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.IssueFilters
{
	[UsedImplicitly]
	public class IfAllDefinition : AlgorithmDefinition
	{
		private const bool _defaultFilter = true;

		[DocIf(nameof(DocIfStrings.IfAll_0))]
		public IfAllDefinition()
			: base(new IFeatureClassSchemaDef[] { })
		{
			Filter = _defaultFilter;
		}

		[DocIf(nameof(DocIfStrings.IfAll_Filter))]
		[TestParameter(_defaultFilter)]
		public bool Filter { get; set; }
	}
}
