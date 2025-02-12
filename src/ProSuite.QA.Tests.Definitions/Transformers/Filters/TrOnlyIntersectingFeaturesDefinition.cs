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
	public class TrOnlyIntersectingFeaturesDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClassToFilter { get; }

		public IFeatureClassSchemaDef Intersecting { get; }

		private const SearchOption _defaultSearchOption = SearchOption.Tile;

		[DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_0))]
		public TrOnlyIntersectingFeaturesDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_featureClassToFilter))]
			IFeatureClassSchemaDef featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_intersecting))]
			IFeatureClassSchemaDef intersecting)
			: base(new[] { featureClassToFilter, intersecting })
		{
			FeatureClassToFilter = featureClassToFilter;
			Intersecting = intersecting;
			FilteringSearchOption = _defaultSearchOption;
		}

		[TestParameter(_defaultSearchOption)]
		[DocTr(nameof(DocTrStrings.TrSpatiallyFiltered_FilteringSearchOption))]
		public SearchOption FilteringSearchOption { get; set; }
	}
}
