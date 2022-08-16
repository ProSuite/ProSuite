using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	[UsedImplicitly]
	public class TrOnlyIntersectingFeatures : TrSpatiallyFiltered
	{
		[DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_0))]
		public TrOnlyIntersectingFeatures(
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_featureClassToFilter))]
			IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_intersecting))]
			IReadOnlyFeatureClass intersecting)
			: base(featureClassToFilter, intersecting) { }

		protected override SpatiallyFilteredBackingDataset CreateFilteredDataset(
			FilteredFeatureClass resultClass)
		{
			return new SpatiallyFilteredBackingDataset(resultClass, _featureClassToFilter,
			                                           _intersecting);
		}
	}
}
