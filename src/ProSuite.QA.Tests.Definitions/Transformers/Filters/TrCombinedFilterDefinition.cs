using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	[FilterTransformer]
	public class TrCombinedFilterDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClassToFilter { get; }
		public IList<IFeatureClassSchemaDef> InputFilters { get; }
		public string Expression { get; }

		[DocTr(nameof(DocTrStrings.TrCombinedFilter_0))]
		public TrCombinedFilterDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrCombinedFilter_featureClassToFilter))]
			IFeatureClassSchemaDef featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrCombinedFilter_inputFilters))]
			IList<IFeatureClassSchemaDef> inputFilters,
			[CanBeNull] [DocTr(nameof(DocTrStrings.TrCombinedFilter_expression))]
			string expression)
			: base(inputFilters.Prepend(featureClassToFilter))
		{
			FeatureClassToFilter = featureClassToFilter;
			InputFilters = inputFilters;
			Expression = expression;
		}
	}
}
