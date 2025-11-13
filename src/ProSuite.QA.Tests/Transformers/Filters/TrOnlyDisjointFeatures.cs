using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	[FilterTransformer]
	public class TrOnlyDisjointFeatures : TrSpatiallyFiltered
	{
		[DocTr(nameof(DocTrStrings.TrOnlyDisjointFeatures_0))]
		public TrOnlyDisjointFeatures(
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyDisjointFeatures_featureClassToFilter))]
			IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyDisjointFeatures_disjoint))]
			IReadOnlyFeatureClass disjoint)
			: base(featureClassToFilter, disjoint) { }

		[InternallyUsedTest]
		public TrOnlyDisjointFeatures(TrOnlyDisjointFeaturesDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClassToFilter,
			       (IReadOnlyFeatureClass)definition.Disjoint)
		{
			FilteringSearchOption = definition.FilteringSearchOption;
		}

		#region Overrides of TrSpatiallyFiltered

		protected override SpatiallyFilteredBackingDataset CreateFilteredDataset(
			FilteredFeatureClass resultClass)
		{
			return new SpatiallyFilteredBackingDataset(resultClass, _featureClassToFilter,
			                                           _filtering, FilteringSearchOption)
			       {
				       PassCriterion = IsDisjoint,
				       DisjointIsPass = true
			       };
		}

		private static bool IsDisjoint(IReadOnlyFeature feature, IReadOnlyFeature disjoint)
		{
			IRelationalOperator disjointShape = (IRelationalOperator) disjoint.Shape;

			return disjointShape.Disjoint(feature.Shape);
		}

		#endregion
	}
}
