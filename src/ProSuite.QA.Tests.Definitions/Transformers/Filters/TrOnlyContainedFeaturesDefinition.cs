using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	[FilterTransformer]
	public class TrOnlyContainedFeaturesDefinition : TrSpatiallyFilteredDefinition
	{
		public IFeatureClassSchemaDef FeatureClassToFilter { get; }

		public IFeatureClassSchemaDef Containing { get; }

		[DocTr(nameof(DocTrStrings.TrOnlyContainedFeatures_0))]
		public TrOnlyContainedFeaturesDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyContainedFeatures_featureClassToFilter))]
			IFeatureClassSchemaDef featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyContainedFeatures_containing))]
			IFeatureClassSchemaDef containing)
			: base(featureClassToFilter, containing)
		{
			FeatureClassToFilter = featureClassToFilter;
			Containing = containing;
		}
	}
}
