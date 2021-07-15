using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb
{
	// TODO Should go to ProSuite.Commons.AGP.Core (but then we have to wrap CancelableProgressor)
	public static class GdbQueryUtils
	{
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

		public static IEnumerable<T> GetRows<T>([NotNull] Table table,
		                                        [CanBeNull] QueryFilter filter = null,
		                                        bool recycle = true)
			where T : Row
		{
			Assert.ArgumentNotNull(table, nameof(table));

			using (RowCursor cursor = table.Search(filter, recycle))
			{
				while (cursor.MoveNext())
				{
					yield return (T) cursor.Current;
				}
			}
		}
	}
}
