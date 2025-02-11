using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	[FilterTransformer]
	public class TrOnlyContainedFeaturesDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClassToFilter { get; }

		public IFeatureClassSchemaDef Containing { get; }

		private const SearchOption _defaultSearchOption = SearchOption.Tile;

		[DocTr(nameof(DocTrStrings.TrOnlyContainedFeatures_0))]
		public TrOnlyContainedFeaturesDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyContainedFeatures_featureClassToFilter))]
			IFeatureClassSchemaDef featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyContainedFeatures_containing))]
			IFeatureClassSchemaDef containing)
			: base(new[] { featureClassToFilter, containing })
		{
			FeatureClassToFilter = featureClassToFilter;
			Containing = containing;
			FilteringSearchOption = _defaultSearchOption;
		}

		[TestParameter(_defaultSearchOption)]
		[DocTr(nameof(DocTrStrings.TrSpatiallyFiltered_FilteringSearchOption))]
		public SearchOption FilteringSearchOption { get; set; }
	}
}
