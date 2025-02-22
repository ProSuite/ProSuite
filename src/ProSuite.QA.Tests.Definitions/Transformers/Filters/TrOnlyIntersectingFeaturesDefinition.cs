using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	[FilterTransformer]
	public class TrOnlyIntersectingFeaturesDefinition : TrSpatiallyFilteredDefinition
	{
		public IFeatureClassSchemaDef FeatureClassToFilter { get; }

		public IFeatureClassSchemaDef Intersecting { get; }

		[DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_0))]
		public TrOnlyIntersectingFeaturesDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_featureClassToFilter))]
			IFeatureClassSchemaDef featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_intersecting))]
			IFeatureClassSchemaDef intersecting)
			: base(featureClassToFilter, intersecting)
		{
			FeatureClassToFilter = featureClassToFilter;
			Intersecting = intersecting;
		}
	}
}
