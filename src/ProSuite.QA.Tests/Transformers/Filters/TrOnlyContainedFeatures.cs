using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	[FilterTransformer]
	public class TrOnlyContainedFeatures : TrSpatiallyFiltered
	{
		[DocTr(nameof(DocTrStrings.TrOnlyContainedFeatures_0))]
		public TrOnlyContainedFeatures(
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyContainedFeatures_featureClassToFilter))]
			IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyContainedFeatures_containing))]
			IReadOnlyFeatureClass containing)
			: base(featureClassToFilter, containing) { }

		[InternallyUsedTest]
		public TrOnlyContainedFeatures(TrOnlyContainedFeaturesDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClassToFilter,
			       (IReadOnlyFeatureClass) definition.Containing)
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
				       PassCriterion = IsContained
			       };
		}

		private static bool IsContained(IReadOnlyFeature feature, IReadOnlyFeature containing)
		{
			IRelationalOperator containingShape = (IRelationalOperator) containing.Shape;

			return containingShape.Contains(feature.Shape);
		}

		#endregion
	}
}
