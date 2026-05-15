using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// Provides the spike-free point insertion algorithm as a reusable utility.
	/// </summary>
	public static class SpikeFreePointInserter
	{
		private const int TagValue = 0;
		private const int FrozenTag = 42;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Adds points to a TIN using the spike-free algorithm: points are globally sorted by Z
		/// descending and only inserted if they do not represent a spike relative to already-inserted
		/// triangles. Triangles surrounding a detected spike are frozen to prevent further insertion.
		/// </summary>
		/// <param name="tin">The TIN to insert points into.</param>
		/// <param name="points">All candidate points (source SR, already transformed if needed).</param>
		/// <param name="freezeDistance">Maximum triangle edge length (map units) for a triangle to
		/// be considered a spike candidate.</param>
		/// <param name="insertionBuffer">Z-value offset added to a candidate point's Z when testing
		/// whether existing triangle vertices form a spike.</param>
		public static void AddPointsToTin(
			[NotNull] ITinEdit tin,
			[NotNull] IEnumerable<(double x, double y, double z)> points,
			double freezeDistance,
			double insertionBuffer)
		{
			var advancedTin = tin as ITinAdvanced;
			Assert.ArgumentNotNull(advancedTin, nameof(advancedTin));

			var point = new PointClass();
			var adjacentTriangle1 = new TinTriangleClass();
			var adjacentTriangle2 = new TinTriangleClass();
			var adjacentTriangle3 = new TinTriangleClass();

			int addedPoints = 0;
			int ignoredPoints = 0;

			foreach ((double x, double y, double z) in points.OrderByDescending(p => p.z))
			{
				point.PutCoords(x, y);
				point.Z = z;

				ITinTriangle triangle = advancedTin.FindTriangle(point);

				if (IsFrozen(triangle))
				{
					ignoredPoints++;
					continue;
				}

				if (IsPointSpike(triangle, point, freezeDistance, insertionBuffer))
				{
					Freeze(tin, triangle);
					ignoredPoints++;
					continue;
				}

				triangle.QueryAdjacentTriangles(adjacentTriangle1, adjacentTriangle2,
				                                adjacentTriangle3);

				if (! IsFrozen(adjacentTriangle1) &&
				    IsPointSpike(adjacentTriangle1, point, freezeDistance, insertionBuffer))
				{
					Freeze(tin, adjacentTriangle1);
				}

				if (! IsFrozen(adjacentTriangle2) &&
				    IsPointSpike(adjacentTriangle2, point, freezeDistance, insertionBuffer))
				{
					Freeze(tin, adjacentTriangle2);
				}

				if (! IsFrozen(adjacentTriangle3) &&
				    IsPointSpike(adjacentTriangle3, point, freezeDistance, insertionBuffer))
				{
					Freeze(tin, adjacentTriangle3);
				}

				addedPoints++;
				tin.AddPointZ(point, TagValue);
			}

			_msg.InfoFormat(
				"Added {0} points to the TIN. {1} points were identified as spikes and ignored.",
				addedPoints, ignoredPoints);
		}

		private static bool IsPointSpike(ITinTriangle triangle, IPoint point,
		                                  double freezeDistance, double insertionBuffer)
		{
			if (triangle.IsEmpty)
			{
				return false;
			}

			double freezeSq = freezeDistance * freezeDistance;
			double thresholdZ = point.Z + insertionBuffer;

			WKSPointZ v0, v1, v2;
			triangle.QueryVertices(out v0, out v1, out v2);

			if (SquaredDist2D(v0, v1) >= freezeSq) return false;
			if (SquaredDist2D(v1, v2) >= freezeSq) return false;
			if (SquaredDist2D(v2, v0) >= freezeSq) return false;

			if (v0.Z < thresholdZ) return false;
			if (v1.Z < thresholdZ) return false;
			if (v2.Z < thresholdZ) return false;

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static double SquaredDist2D(WKSPointZ a, WKSPointZ b)
		{
			double dx = a.X - b.X;
			double dy = a.Y - b.Y;
			return dx * dx + dy * dy;
		}

		private static bool IsFrozen(ITinTriangle triangle)
		{
			return ! triangle.IsEmpty && triangle.TagValue == FrozenTag;
		}

		private static void Freeze(ITinEdit tin, ITinTriangle triangle)
		{
			tin.SetTriangleTagValue(triangle.Index, FrozenTag);

			for (int i = 0; i < 3; i++)
			{
				tin.SetEdgeType(triangle.Edge[i].Index, esriTinEdgeType.esriTinHardEdge);
			}
		}
	}
}
