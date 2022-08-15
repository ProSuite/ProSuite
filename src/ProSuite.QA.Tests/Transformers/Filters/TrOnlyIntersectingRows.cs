using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public class TrOnlyIntersectingFeatures : TableTransformer<FilteredFeatureClass>
	{
		[NotNull] private readonly IReadOnlyFeatureClass _featureClassToFilter;
		[NotNull] private readonly IReadOnlyFeatureClass _intersecting;
		private FilteredFeatureClass _resultingTable;

		[DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_0))]
		public TrOnlyIntersectingFeatures(
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_featureClassToFilter))]
			IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] [DocTr(nameof(DocTrStrings.TrOnlyIntersectingFeatures_intersecting))]
			IReadOnlyFeatureClass intersecting)
			: base(new[] {featureClassToFilter, intersecting})
		{
			_featureClassToFilter = featureClassToFilter;
			_intersecting = intersecting;
		}

		protected override FilteredFeatureClass GetTransformedCore(string name)
		{
			if (_resultingTable == null)
			{
				string filteredTableName = ((ITableTransformer) this).TransformerName;

				// Un-transformed, uncached identical schema as the _featureClassToFilter
				// If the evaluation of the filter criterion is slow, re-consider caching.
				// But an efficient cache could also be implemented locally, e.g. by
				// remembering the OIDs that were filtered out previously.
				_resultingTable = new FilteredFeatureClass(
					_featureClassToFilter, filteredTableName,
					createBackingDataset: CreateFilteredDataset);

				FilteredBackingDataset filterBackingData = _resultingTable.BackingData;

				ISpatialFilter filterIntersecting = new SpatialFilterClass();

				// TODO: Is this the way to do this?
				int intersectingTableIndex = 1;
				ConfigureQueryFilter(intersectingTableIndex, filterIntersecting);

				filterIntersecting.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

				filterBackingData.IntersectingFeatureFilter = filterIntersecting;
			}

			return _resultingTable;
		}

		private FilteredBackingDataset CreateFilteredDataset(GdbTable gdbTable)
		{
			return new FilteredBackingDataset(gdbTable, _featureClassToFilter, _intersecting);
		}
	}

	//public interface IFilterTransformer
	//{
	//	bool IsFiltered(IReadOnlyRow row);
	//}
}
