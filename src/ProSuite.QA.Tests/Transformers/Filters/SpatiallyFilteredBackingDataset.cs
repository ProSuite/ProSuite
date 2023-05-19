using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Db;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public class SpatiallyFilteredBackingDataset : FilteredBackingDataset
	{
		private readonly IReadOnlyFeatureClass _filtering;
		private readonly TrSpatiallyFiltered.SearchOption _neighborSearchOption;

		public SpatiallyFilteredBackingDataset(
			[NotNull] FilteredFeatureClass resultFeatureClass,
			[NotNull] IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] IReadOnlyFeatureClass filtering,
			TrSpatiallyFiltered.SearchOption neighborSearchOption)
			: base(resultFeatureClass, featureClassToFilter,
			       new List<IReadOnlyTable> { featureClassToFilter, filtering })
		{
			_filtering = filtering;
			_neighborSearchOption = neighborSearchOption;
		}

		public IFeatureClassFilter IntersectingFeatureFilter { get; set; }

		/// <summary>
		/// Whether a feature that is disjoint to any filtering feature passes the filter.
		/// </summary>
		public bool DisjointIsPass { get; set; }

		public Func<IReadOnlyFeature, IReadOnlyFeature, bool> PassCriterion { get; set; }

		#region Overrides of BackingDataset

		public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
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

			IFeatureClassFilter spatialFilter = IntersectingFeatureFilter;

			spatialFilter.FilterGeometry = testGeometry;
			QueryFilterHelper queryFilterHelper = QueryHelpers[1];
			queryFilterHelper.FullGeometrySearch =
				_neighborSearchOption == TrSpatiallyFiltered.SearchOption.All;

			foreach (var testRow in DataSearchContainer.Search(
				         _filtering, spatialFilter, queryFilterHelper))
			{
				IReadOnlyFeature intersectingFeature = (IReadOnlyFeature) testRow;

				bool isDisjoint =
					((IRelationalOperator) testGeometry).Disjoint(intersectingFeature.Shape);

				if (isDisjoint)
				{
					continue;
				}

				if (DisjointIsPass)
				{
					// Not disjoint -> filtered
					return false;
				}

				if (PassCriterion == null)
				{
					return true;
				}

				bool passesFilter = PassCriterion.Invoke(resultFeature, intersectingFeature);

				// if Disjoint is pass we have to check every feature
				if (! DisjointIsPass)
				{
					return passesFilter;
				}
			}

			return DisjointIsPass;
		}
	}
}
