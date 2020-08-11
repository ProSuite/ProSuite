using System.Collections.Generic;
using ArcGIS.Core.Data;
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
