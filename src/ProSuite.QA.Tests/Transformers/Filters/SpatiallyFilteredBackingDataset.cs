using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Transformers.Filters
{
	public class SpatiallyFilteredBackingDataset : FilteredBackingDataset
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IReadOnlyFeatureClass _filtering;
		private readonly SearchOption _neighborSearchOption;

		public SpatiallyFilteredBackingDataset(
			[NotNull] FilteredFeatureClass resultFeatureClass,
			[NotNull] IReadOnlyFeatureClass featureClassToFilter,
			[NotNull] IReadOnlyFeatureClass filtering,
			SearchOption neighborSearchOption)
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

			// Cannot search the container without geometry! (InvalidCastException)
			bool canUseDataContainer = DataSearchContainer != null &&
			                           filter is IFeatureClassFilter;

			if (canUseDataContainer)
			{
				if (filter is AoFeatureClassFilter aoFeatureClassFilter)
				{
					((AoFeatureClassFilter) IntersectingFeatureFilter).TileExtent =
						aoFeatureClassFilter.TileExtent;
				}

				IFeatureClassFilter featureClassFilter = (IFeatureClassFilter) filter;

				// Search in the container
				foreach (IReadOnlyRow resultRow in DataSearchContainer.Search(
					         FeatureClassToFilter, featureClassFilter, resultFilter))
				{
					IReadOnlyFeature resultFeature = (IReadOnlyFeature) resultRow;

					if (PassesFilter(resultFeature))
					{
						// No caching, just wrap it:
						yield return CreateFeature(resultFeature);
					}
				}
			}
			else
			{
				// For the moment, use the brute-force approach and circumvent the container:
				// Consider a different approach (if not DisjointIsPass):
				// Get all intersecting, search by unioned envelopes, etc.:
				_msg.DebugFormat(
					"Cannot search data container. Using feature class search with where clause {0}",
					filter?.WhereClause);

				foreach (IReadOnlyRow resultRow in FeatureClassToFilter.EnumRows(filter, recycling))
				{
					IReadOnlyFeature resultFeature = (IReadOnlyFeature) resultRow;

					if (PassesFilter(resultFeature))
					{
						// No caching, just wrap it:
						yield return CreateFeature(resultFeature);
					}
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
				_neighborSearchOption == SearchOption.All;

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
