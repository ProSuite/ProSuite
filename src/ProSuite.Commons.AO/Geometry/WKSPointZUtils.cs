using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Array = System.Array;

namespace ProSuite.Commons.AO.Geometry
{
	public static class WKSPointZUtils
	{
		public static WKSPointZ CreatePoint(double x, double y, double z)
		{
			WKSPointZ result;

			result.X = x;
			result.Y = y;
			result.Z = z;

			return result;
		}

		public static WKSPointZ CreatePoint([NotNull] IPoint point)
		{
			return CreatePoint(point.X, point.Y, point.Z);
		}

		public static WKSPointZ GetNormed(WKSPointZ v)
		{
			double f = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);

			WKSPointZ result;

			result.X = v.X / f;
			result.Y = v.Y / f;
			result.Z = v.Z / f;

			return result;
		}

		/// <summary>
		/// Calculates the cross product of two vectors.
		/// </summary>
		/// <param name="u">The first vector.</param>
		/// <param name="v">The second vector.</param>
		/// <returns></returns>
		public static WKSPointZ GetVectorProduct(WKSPointZ u, WKSPointZ v)
		{
			WKSPointZ result;

			result.X = u.Y * v.Z - u.Z * v.Y;

			result.Y = u.Z * v.X - u.X * v.Z;

			// result.Z = u.Y * v.Y - u.Y * v.X;
			result.Z = u.X * v.Y - u.Y * v.X;

			return result;
		}

		/// <summary>
		/// Determines whether the coordinate values at the specified indexes are within
		/// the appropriate tolerance. To compare ony in XY, provide NaN as zTolerance.
		/// </summary>
		/// <param name="points"></param>
		/// <param name="index1"></param>
		/// <param name="index2"></param>
		/// <param name="xyTolerance"></param>
		/// <param name="zTolerance"></param>
		/// <returns></returns>
		public static bool ArePointsEqual(
			[NotNull] IList<WKSPointZ> points,
			int index1, int index2,
			double xyTolerance, double zTolerance)
		{
			if (index1 < 0 || index1 >= points.Count)
			{
				return false;
			}

			if (index2 < 0 || index2 >= points.Count)
			{
				return false;
			}

			WKSPointZ a = points[index1];
			WKSPointZ b = points[index2];

			return GeometryUtils.IsSamePoint(a, b, xyTolerance, zTolerance);
		}

		[NotNull]
		public static IList<KeyValuePair<WKSPointZ, List<WKSPointZ>>> GroupPoints(
			[NotNull] WKSPointZ[] coords,
			double xyTolerance,
			double zTolerance)
		{
			Assert.ArgumentNotNull(coords, nameof(coords));

			IComparer<WKSPointZ> comparer = new WKSPointZComparer();
			Array.Sort(coords, comparer);

			var toleranceGroups = new List<List<WKSPointZ>>();
			var currentGroup = new List<WKSPointZ> {coords[0]};

			for (var i = 1; i < coords.Length; i++)
			{
				if (! ArePointsEqual(coords, i - 1, i, xyTolerance, zTolerance))
				{
					toleranceGroups.Add(currentGroup);
					currentGroup = new List<WKSPointZ> {coords[i]};

					continue;
				}

				currentGroup.Add(coords[i]);
			}

			if (currentGroup.Count > 0)
			{
				toleranceGroups.Add(currentGroup);
			}

			var result =
				new List<KeyValuePair<WKSPointZ, List<WKSPointZ>>>();

			// For strict interpretation of tolerance: Consider splitting clusters that are too large (Divisive Hierarchical clustering)
			foreach (List<WKSPointZ> groupedPoints in toleranceGroups)
			{
				if (groupedPoints.Count == 1)
				{
					result.Add(new KeyValuePair<WKSPointZ, List<WKSPointZ>>(groupedPoints[0],
					                                                        groupedPoints));
				}
				else
				{
					var center = new WKSPointZ
					             {
						             X = groupedPoints.Average(p => p.X),
						             Y = groupedPoints.Average(p => p.Y),
						             Z = groupedPoints.Average(p => p.Z)
					             };

					result.Add(new KeyValuePair<WKSPointZ, List<WKSPointZ>>(center, groupedPoints));
				}
			}

			return result;
		}

		public static bool HaveSameVertices(
			WKSPointZ[] coords1, WKSPointZ[] coords2,
			double xyTolerance, double zTolerance,
			bool ignoreDuplicateVertices = true)
		{
			Assert.ArgumentNotNull(coords1, nameof(coords1));
			Assert.ArgumentNotNull(coords2, nameof(coords2));

			IComparer<WKSPointZ> comparer = new WKSPointZComparer();
			Array.Sort(coords1, comparer);
			Array.Sort(coords2, comparer);

			var i1 = 0;
			var i2 = 0;
			while (i1 < coords1.Length && i2 < coords2.Length)
			{
				WKSPointZ a = coords1[i1];
				WKSPointZ b = coords2[i2];

				if (! GeometryUtils.IsSamePoint(a, b, xyTolerance, zTolerance))
				{
					return false;
				}

				// Advance to next set of coordinates:
				++i1;
				++i2;

				// Skip identical points in sequence!
				while (ignoreDuplicateVertices && ArePointsEqual(coords1, i1, i1 - 1,
				                                                 xyTolerance,
				                                                 zTolerance))
				{
					++i1;
				}

				while (ignoreDuplicateVertices && ArePointsEqual(coords2, i2, i2 - 1,
				                                                 xyTolerance,
				                                                 zTolerance))
				{
					++i2;
				}
			}

			return (i1 == coords1.Length) && (i2 == coords2.Length);
		}
	}
}
