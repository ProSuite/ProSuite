using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	[FilterTransformer]
	public class TrOnlyDisjointFeaturesDefinition : TrSpatiallyFilteredDefinition
	{
		public IFeatureClassSchemaDef FeatureClassToFilter { get; }

		public IFeatureClassSchemaDef Disjoint { get; }

		[DocTr(nameof(DocTrStrings.TrOnlyDisjointFeatures_0))]
		public TrOnlyDisjointFeaturesDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyDisjointFeatures_featureClassToFilter))]
			IFeatureClassSchemaDef featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyDisjointFeatures_disjoint))]
			IFeatureClassSchemaDef disjoint)
			: base(featureClassToFilter, disjoint)

		{
			FeatureClassToFilter = featureClassToFilter;
			Disjoint = disjoint;
		}
	}
}
