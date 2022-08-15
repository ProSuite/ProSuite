using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public class FilteredBackingDataset : TransformedBackingData //, IFilterTransformer
	{
		private readonly IReadOnlyFeatureClass _filtered;
		private readonly IReadOnlyFeatureClass _intersecting;
		[NotNull] private readonly FilteredFeatureClass _resultTable;

		public FilteredBackingDataset([NotNull] GdbTable resultTable,
		                              IReadOnlyFeatureClass filtered,
		                              IReadOnlyFeatureClass intersecting)
			: base(new List<IReadOnlyTable> {filtered, intersecting})
		{
			_filtered = filtered;
			_intersecting = intersecting;

			_resultTable = (FilteredFeatureClass) resultTable;
		}

		#region Overrides of BackingDataset

		public override IEnvelope Extent => _filtered.Extent;

		public ISpatialFilter IntersectingFeatureFilter { get; set; }

		public override VirtualRow GetRow(int id)
		{
			IReadOnlyFeature feature = (IReadOnlyFeature) _filtered.GetRow(id);

			if (PassesFilter(feature))
			{
				return CreateFeature(feature);
			}

			// Or throw?
			return null;
		}

		private GdbFeature CreateFeature(IReadOnlyFeature feature)
		{
			var wrappedAttributeValues = new WrappedRowValues(feature, true);

			return new GdbFeature(feature.OID, _resultTable, wrappedAttributeValues);
		}

		public override int GetRowCount(IQueryFilter queryFilter)
		{
			return Search(queryFilter, true).Count();
		}

		public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
		{
			QueryFilterHelper resultFilter = QueryHelpers[0];
			Assert.NotNull(filter);

			// If the features are not in the container, a different approach would be more suitable:
			// Get all intersecting, search by unioned envelopes, etc.
			foreach (IReadOnlyRow resultRow in DataSearchContainer.Search(
				         _filtered, filter, resultFilter))
			{
				IReadOnlyFeature resultFeature = (IReadOnlyFeature) resultRow;

				if (PassesFilter(resultFeature))
				{
					// No caching, just wrap it:
					yield return CreateFeature(resultFeature);
				}
			}
		}

		#endregion

		public Func<IReadOnlyFeature, IReadOnlyFeature, bool> PassCriterion { get; set; }

		private bool PassesFilter(IReadOnlyFeature resultFeature)
		{
			IGeometry testGeometry = resultFeature.Shape;

			ISpatialFilter spatialFilter = IntersectingFeatureFilter;

			spatialFilter.Geometry = testGeometry;
			QueryFilterHelper queryFilterHelper = QueryHelpers[1];

			foreach (var testRow in DataSearchContainer.Search(
				         _intersecting, spatialFilter, queryFilterHelper))
			{
				IReadOnlyFeature intersectingFeature = ((IReadOnlyFeature) testRow);

				if (((IRelationalOperator) testGeometry).Disjoint(intersectingFeature.Shape))
				{
					continue;
				}

				if (PassCriterion == null)
				{
					return true;
				}

				return PassCriterion.Invoke(resultFeature, intersectingFeature);
			}

			return false;
		}
	}
}
