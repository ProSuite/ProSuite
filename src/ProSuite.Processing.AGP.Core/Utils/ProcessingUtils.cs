using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.AGP.Core.Utils
{
	public static class ProcessingUtils
	{
		/// <summary>
		/// Create a string from the given object. Format:
		/// &quot;OID=123 Class=AliasNameOrDatasetName&quot;
		/// </summary>
		public static string Format([NotNull] Feature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			var oid = feature.GetObjectID();
			string className;

			using (var table = feature.GetTable())
			{
				className = table?.GetName() ?? "UnknownTable";
			}

			return FormattableString.Invariant($"OID={oid} Class={className}");
		}

		/// <returns>True iff the two features are the same</returns>
		/// <remarks>
		/// This is a cheap test, but it assumes that both features are from
		/// the <b>same workspace</b>.  If the two features are from different workspaces,
		/// this method <em>may</em> return true even though the features are different!
		/// </remarks>
		public static bool IsSameFeature(Feature feature1, Feature feature2)
		{
			if (ReferenceEquals(feature1, feature2)) return true;
			if (Equals(feature1.Handle, feature2.Handle)) return true;

			var oid1 = feature1.GetObjectID();
			var oid2 = feature2.GetObjectID();
			if (oid1 != oid2) return false;

			using (var table1 = feature1.GetTable())
			using (var table2 = feature2.GetTable())
			{
				return IsSameTable(table1, table2);
			}
		}

		public static bool IsSameTable(Table fc1, Table fc2)
		{
			if (ReferenceEquals(fc1, fc2)) return true;
			if (Equals(fc1.Handle, fc2.Handle)) return true;

			var id1 = fc1.GetID();
			var id2 = fc2.GetID();
			if (id1 != id2) return false;
			if (id1 >= 0) return true;

			// table id is negative for tables not registered with the Geodatabase
			// compare table name and workspace -- for now, give up and assume not same

			return false;
		}

		[NotNull]
		public static QueryFilter CreateFilter(string whereClause, Geometry extent)
		{
			QueryFilter filter;

			if (extent != null)
			{
				filter = new SpatialQueryFilter
				         {
					         FilterGeometry = extent,
					         SpatialRelationship = SpatialRelationship.Intersects
				         };
			}
			else
			{
				filter = new QueryFilter();
			}

			if (! string.IsNullOrEmpty(whereClause))
			{
				filter.WhereClause = whereClause;
			}

			return filter;
		}

		/// <summary>
		/// Return true iff <paramref name="shape"/> is within
		/// <paramref name="perimeter"/>; if <paramref name="perimeter"/> is
		/// <c>null</c> the <paramref name="shape"/> is considered within.
		/// </summary>
		// TODO Shouldn't this be on IProcessingContext?
		public static bool WithinPerimeter(Geometry shape, [CanBeNull] Geometry perimeter)
		{
			if (shape == null)
			{
				return false;
			}

			if (perimeter == null)
			{
				return true;
			}

			return GeometryEngine.Instance.Contains(perimeter, shape);
		}
	}
}
