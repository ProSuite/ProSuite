using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public class SpatiallyFilteredBackingDataset : FilteredBackingDataset
	{
		private readonly IReadOnlyFeatureClass _intersecting;

		public SpatiallyFilteredBackingDataset(
			[NotNull] FilteredFeatureClass resultFeatureClass,
			[NotNull] IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] IReadOnlyFeatureClass intersecting)
			: base(resultFeatureClass, featureClassToFilter,
			       new List<IReadOnlyTable> {featureClassToFilter, intersecting})
		{
			_intersecting = intersecting;
		}

		public ISpatialFilter IntersectingFeatureFilter { get; set; }

		public Func<IReadOnlyFeature, IReadOnlyFeature, bool> PassCriterion { get; set; }

		#region Overrides of BackingDataset

		public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
		{
			QueryFilterHelper resultFilter = QueryHelpers[0];
			Assert.NotNull(filter);

			// If the features are not in the container, a different approach would be more suitable:
			// Get all intersecting, search by unioned envelopes, etc.
			foreach (IReadOnlyRow resultRow in DataSearchContainer.Search(
				         FeatureClassToFilter, filter, resultFilter))
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

		public override bool PassesFilter(IReadOnlyFeature resultFeature)
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
