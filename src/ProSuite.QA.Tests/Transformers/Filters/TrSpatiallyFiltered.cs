using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public abstract class TrSpatiallyFiltered : TableTransformer<FilteredFeatureClass>
	{
		[NotNull] protected readonly IReadOnlyFeatureClass _featureClassToFilter;
		[NotNull] protected readonly IReadOnlyFeatureClass _intersecting;

		private FilteredFeatureClass _resultingClass;

		protected TrSpatiallyFiltered(
			[NotNull] IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] IReadOnlyFeatureClass intersecting)
			: base(new[] {featureClassToFilter, intersecting})
		{
			_featureClassToFilter = featureClassToFilter;
			_intersecting = intersecting;
		}

		protected override FilteredFeatureClass GetTransformedCore(string name)
		{
			if (_resultingClass == null)
			{
				string filteredTableName = ((ITableTransformer) this).TransformerName;

				// Un-transformed, uncached identical schema as the _featureClassToFilter
				// If the evaluation of the filter criterion is slow, re-consider caching.
				// But an efficient cache could also be implemented locally, e.g. by
				// remembering the OIDs that were filtered out previously.
				_resultingClass = new FilteredFeatureClass(
					_featureClassToFilter, filteredTableName,
					createBackingDataset: CreateFilteredDataset);

				FilteredBackingDataset filterBackingData = _resultingClass.BackingData;

				ISpatialFilter filterIntersecting = new SpatialFilterClass();

				// TODO: Is this the way to do this?
				int intersectingTableIndex = 1;
				ConfigureQueryFilter(intersectingTableIndex, filterIntersecting);

				filterIntersecting.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

				filterBackingData.IntersectingFeatureFilter = filterIntersecting;
			}

			return _resultingClass;
		}

		protected abstract FilteredBackingDataset CreateFilteredDataset(GdbTable gdbTable);
	}
}
