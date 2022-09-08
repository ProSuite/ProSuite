using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	[FilterTransformer]
	public class TrFilterIntersecting : TrSpatiallyFiltered
	{
		[DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_0))]
		public TrFilterIntersecting(
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_featureClassToFilter))]
			IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_intersecting))]
			IReadOnlyFeatureClass intersecting)
			: base(featureClassToFilter, intersecting) { }

		#region Overrides of TrSpatiallyFiltered

		protected override SpatiallyFilteredBackingDataset CreateFilteredDataset(
			FilteredFeatureClass resultClass)
		{
			return new SpatiallyFilteredBackingDataset(resultClass, _featureClassToFilter,
			                                           _intersecting)
			       {
				       PassCriterion = IsDisjoint
			       };
		}

		private static bool IsDisjoint(IReadOnlyFeature feature, IReadOnlyFeature containing)
		{
			IRelationalOperator containingShape = (IRelationalOperator) containing.Shape;

			return containingShape.Disjoint(feature.Shape);
		}

		#endregion
	}
}
