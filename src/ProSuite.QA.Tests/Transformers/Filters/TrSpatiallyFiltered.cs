using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public abstract class TrSpatiallyFiltered : TableTransformer<FilteredFeatureClass>
	{
		private const SearchOption _defaultSearchOption = SearchOption.Tile;

		[NotNull] protected readonly IReadOnlyFeatureClass _featureClassToFilter;
		[NotNull] protected readonly IReadOnlyFeatureClass _filtering;

		private FilteredFeatureClass _resultingClass;

		protected TrSpatiallyFiltered(
			[NotNull] IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] IReadOnlyFeatureClass filtering)
			: base(new[] { featureClassToFilter, filtering })
		{
			_featureClassToFilter = featureClassToFilter;
			_filtering = filtering;
			FilteringSearchOption = _defaultSearchOption;
		}

		[TestParameter(_defaultSearchOption)]
		[DocTr(nameof(DocTrStrings.TrSpatiallyFiltered_FilteringSearchOption))]
		public SearchOption FilteringSearchOption { get; set; }


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
					createBackingDataset: gdbTable =>
						CreateFilteredDataset((FilteredFeatureClass) gdbTable));

				SpatiallyFilteredBackingDataset filterBackingData =
					(SpatiallyFilteredBackingDataset) _resultingClass.BackingData;

				IFeatureClassFilter filterIntersecting = new AoFeatureClassFilter();

				// TODO: Is this the way to do this?
				int intersectingTableIndex = 1;
				ConfigureQueryFilter(intersectingTableIndex, filterIntersecting);

				filterIntersecting.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects;

				filterBackingData.IntersectingFeatureFilter = filterIntersecting;
			}

			return _resultingClass;
		}

		protected abstract SpatiallyFilteredBackingDataset CreateFilteredDataset(
			[NotNull] FilteredFeatureClass resultClass);
	}
}
