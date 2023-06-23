using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Gdb
{
	// TODO Should go to ProSuite.Commons.AGP.Core (but then we have to wrap CancelableProgressor)
	// Or better just provide the CancellationToken from the CancelableProgressor in the signatures
	public static class GdbQueryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static QueryFilter CreateFilter([NotNull] IReadOnlyList<long> oids)
		{
			Assert.ArgumentNotNull(oids, nameof(oids));

			return new QueryFilter {ObjectIDs = oids};
		}

		[NotNull]
		public static SpatialQueryFilter CreateSpatialFilter(
			[NotNull] Geometry filterGeometry,
			SpatialRelationship spatialRelationship = SpatialRelationship.Intersects,
			SearchOrder searchOrder = SearchOrder.Spatial)
		{
			Assert.ArgumentNotNull(filterGeometry, nameof(filterGeometry));

			return new SpatialQueryFilter
			       {
				       FilterGeometry = filterGeometry,
				       SpatialRelationship = spatialRelationship,
				       SearchOrder = searchOrder
			       };
		}

		public static Feature GetFeature([NotNull] FeatureClass featureClass, long oid)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return GetRow<Feature>(featureClass, oid);
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] Table featureClass,
			[NotNull] IEnumerable<long> objectIds,
			[CanBeNull] SpatialReference outputSpatialReference,
			bool recycle,
			[CanBeNull] CancelableProgressor cancelableProgressor = null)
		{
			IReadOnlyList<long> oidList = objectIds.ToList();

			try
			{
				QueryFilter filter = CreateFilter(oidList);

				filter.OutputSpatialReference = outputSpatialReference;

				return GetFeatures(featureClass, filter, recycle, cancelableProgressor);
			}
			catch (Exception e)
			{
				// TODO: Specifically catch FDO_E_SE_LOG_NOEXIST
				_msg.Debug("Error getting rows by OID-list", e);

				const int maxRowCount = 1000;
				return GetRowsByObjectIdsBatched<Feature>(
					featureClass, oidList, outputSpatialReference, recycle, maxRowCount,
					cancelableProgressor);
			}
		}

		public static IEnumerable<Feature> GetFeatures(
			[NotNull] Table featureClass,
			[CanBeNull] QueryFilter filter,
			bool recycling,
			[CanBeNull] CancelableProgressor cancelableProgressor = null)
		{
			var cursor = featureClass.Search(filter, recycling);

			try
			{
				while (cursor.MoveNext())
				{
					if (cancelableProgressor != null &&
					    cancelableProgressor.CancellationToken.IsCancellationRequested)
					{
						yield break;
					}

					var feature = (Feature) cursor.Current;

					yield return feature;
				}
			}
			finally
			{
				cursor.Dispose();
			}
		}

		public static Row GetRow([NotNull] Table table, long oid)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return GetRow<Row>(table, oid);
		}

		public static T GetRow<T>([NotNull] Table table, long oid)
			where T : Row
		{
			Assert.ArgumentNotNull(table, nameof(table));

			QueryFilter filter = CreateFilter(new[] {oid});

			using (RowCursor cursor = table.Search(filter, false))
			{
				if (! cursor.MoveNext())
				{
					return null;
				}

				var result = (T) cursor.Current;

				// todo daro: remove later when GetRow is used intensively throughout the solution
				Assert.False(cursor.MoveNext(), "more than one row found");

				return result;
			}
		}

		public static IEnumerable<T> GetRows<T>(
			[NotNull] Table table,
			[CanBeNull] QueryFilter filter = null,
			bool recycle = true,
			[CanBeNull] CancelableProgressor cancelableProgressor = null)
			where T : Row
		{
			Assert.ArgumentNotNull(table, nameof(table));

			using (RowCursor cursor = table.Search(filter, recycle))
			{
				while (cursor.MoveNext())
				{
					if (cancelableProgressor != null &&
					    cancelableProgressor.CancellationToken.IsCancellationRequested)
					{
						yield break;
					}

					yield return (T) cursor.Current;
				}
			}
		}

		/// <summary>
		/// Returns the rows from ITable.GetRows method called in batches with the specified maximum number of rows.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="objectIds"></param>
		/// <param name="outputSpatialReference"></param>
		/// <param name="recycle"></param>
		/// <param name="maxRowCount"></param>
		/// <param name="cancelableProgressor"></param>
		/// <returns></returns>
		private static IEnumerable<T> GetRowsByObjectIdsBatched<T>(
			[NotNull] Table table,
			[NotNull] IEnumerable<long> objectIds,
			SpatialReference outputSpatialReference,
			bool recycle,
			int maxRowCount,
			[CanBeNull] CancelableProgressor cancelableProgressor = null) where T : Row
		{
			foreach (IList<long> oidBatch in CollectionUtils.Split(objectIds, maxRowCount))
			{
				if (oidBatch.Count == 0)
				{
					continue;
				}

				QueryFilter filter = GetOidListFilter(table, oidBatch, outputSpatialReference);

				foreach (var row in GetRows<T>(table, filter, recycle, cancelableProgressor))
				{
					yield return row;
				}
			}
		}

		private static QueryFilter GetOidListFilter(
			[NotNull] Table table,
			IEnumerable<long> objectIds,
			SpatialReference outputSpatialReference)
		{
			var filter = new QueryFilter
			             {
				             WhereClause =
					             $"{table.GetDefinition().GetObjectIDField()} IN ({StringUtils.Concatenate(objectIds, ", ")})"
			             };

			filter.OutputSpatialReference = outputSpatialReference;

			return filter;
		}
	}
}
