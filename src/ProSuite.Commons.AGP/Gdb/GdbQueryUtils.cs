using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb
{
	public static class GdbQueryUtils
	{
		[NotNull]
		public static SpatialQueryFilter CreateSpatialFilter(
			[NotNull] ArcGIS.Core.Geometry.Geometry filterGeometry,
			SpatialRelationship spatialRelationship = SpatialRelationship.Intersects,
			SearchOrder searchOrder = SearchOrder.Spatial)
		{
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

			string oidField = table.GetDefinition().GetObjectIDField();

			using (RowCursor cursor =
				table.Search(new QueryFilter {WhereClause = $"{oidField} = {oid}"}, false))
			{
				if (! cursor.MoveNext())
				{
					return null;
				}

				// todo daro: remove later when GetRow is used intensively throughout the solution
				Assert.False(cursor.MoveNext(), "more than one row found");

				return cursor.Current;
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
