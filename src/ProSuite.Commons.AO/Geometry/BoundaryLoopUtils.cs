using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.AO.Geometry
{
	public static class BoundaryLoopUtils
	{
		[NotNull]
		public static IEnumerable<IPolygon> GetBoundaryLoops([NotNull] IPolygon polygon,
		                                                     double tolerance)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));
			Assert.ArgumentCondition(tolerance >= 0, "tolerance must be >= 0");

			foreach (IRing ring in GeometryUtils.GetRings(polygon))
			{
				if (ring.IsExterior)
				{
					foreach (IPolygon boundaryLoop in GetBoundaryLoops(ring, tolerance))
					{
						yield return boundaryLoop;
					}
				}

				Marshal.ReleaseComObject(ring);
			}
		}

		[NotNull]
		private static IEnumerable<IPolygon> GetBoundaryLoops([NotNull] IRing ring,
		                                                      double tolerance)
		{
			Assert.ArgumentNotNull(ring, nameof(ring));

			var searcher = new VertexSearcher(ring.Envelope, tolerance);

			WKSPoint[] points = GeometryUtils.GetWKSPoints(ring);
			int lastVertexIndex = points.Length - 1;

			for (var i = 0; i < points.Length; i++)
			{
				var vertex = new Vertex(points[i].X, points[i].Y, i);

				// search other vertex matching this vertex
				Vertex nearestVertex = searcher.GetNearestVertex(vertex);

				if (nearestVertex != null)
				{
					// a nearest vertex was found

					// ignore expected match of last point to first point
					if (nearestVertex.Index != 0 || vertex.Index != lastVertexIndex)
					{
						// ignore matches of consecutive vertices: these should be caught by QaSimpleGeometry
						if (vertex.Index - nearestVertex.Index > 1)
						{
							string noPolygonReason;
							IPolygon loopPolygon = TryCreateLoopPolygon(ring, nearestVertex.Index,
								vertex.Index,
								out noPolygonReason);

							if (loopPolygon != null)
							{
								yield return loopPolygon;
							}
						}
					}
				}

				// add vertex to tree
				searcher.Add(vertex);
			}
		}

		/// <summary>
		/// Tries to create the loop polygon between two vertex indexes on a geometry
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="startVertexIndex">Start vertex index of the loop.</param>
		/// <param name="endVertexIndex">End vertex index of the loop.</param>
		/// <param name="noPolygonReason">In case no loop polygon can be created, the displayable reason for it.</param>
		/// <returns></returns>
		[CanBeNull]
		public static IPolygon TryCreateLoopPolygon([NotNull] IGeometry geometry,
		                                            int startVertexIndex,
		                                            int endVertexIndex,
		                                            [NotNull] out string noPolygonReason)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var segments = (ISegmentCollection) geometry;
			var points = (IPointCollection) geometry;

			int expectedSegmentCount = points.PointCount - 1;

			int totalSegmentCount = segments.SegmentCount;

			if (expectedSegmentCount != totalSegmentCount)
			{
				// Unexpected segment count. This can be the case for bezier segments, where the 
				// control points apparently are part of the point collection also (without representing segment endpoints)
				// TODO: provide alternative implementation based on segment enumeration
				noPolygonReason = "unexpected segment count";
				return null;
			}

			int startSegmentIndex = startVertexIndex;
			int endSegmentIndex = endVertexIndex - 1;

			int loopSegmentCount = endSegmentIndex - startSegmentIndex + 1;

			if (loopSegmentCount <= 2)
			{
				noPolygonReason = string.Format("insufficient segment count: {0}",
				                                loopSegmentCount);
				return null;
			}

			int remainingSegmentCount = totalSegmentCount - loopSegmentCount;
			bool assumeStartPointInLoop = remainingSegmentCount < loopSegmentCount;

			IPolygon polygon = CreateLoopPolygonUnSimplified(startSegmentIndex, endSegmentIndex,
			                                                 geometry,
			                                                 assumeStartPointInLoop);

			if (! polygon.IsClosed)
			{
				// no longer needed, release early to avoid VM impact
				Marshal.ReleaseComObject(polygon);

				noPolygonReason = "unclosed segments";
				return null;
			}

			// Check the orientation of the unsimplified loop polygon. Loops should be "empty" -> inner rings,
			// hence have negative area.
			double loopArea = ((IArea) polygon).Area;

			if (loopArea > 0)
			{
				// the loop is not an inner ring 
				// --> the assumption was wrong, the loop has the larger number of vertices
				// --> create the ring from the inverse segment set

				polygon = CreateLoopPolygonUnSimplified(startSegmentIndex, endSegmentIndex,
				                                        geometry,
				                                        ! assumeStartPointInLoop);

				if (! polygon.IsClosed)
				{
					// no longer needed, release early to avoid VM impact
					Marshal.ReleaseComObject(polygon);

					noPolygonReason = "unclosed segments";
					return null;
				}
			}

			const bool allowReorder = true;
			GeometryUtils.Simplify(polygon, allowReorder);

			if (polygon.IsEmpty)
			{
				// no longer needed, release early to avoid VM impact
				Marshal.ReleaseComObject(polygon);

				noPolygonReason = "simplified loop polygon is empty";
				return null;
			}

			noPolygonReason = string.Empty;
			return polygon;
		}

		/// <summary>
		/// Creates the un-simplified loop polygon.
		/// </summary>
		/// <param name="startSegmentIndex">Start index of the segment.</param>
		/// <param name="endSegmentIndex">End index of the segment.</param>
		/// <param name="geometry">The geometry.</param>
		/// <param name="assumeStartPointInLoop">if set to <c>true</c> [assume start point in loop].</param>
		/// <returns></returns>
		[NotNull]
		private static IPolygon CreateLoopPolygonUnSimplified(int startSegmentIndex,
		                                                      int endSegmentIndex,
		                                                      [NotNull] IGeometry geometry,
		                                                      bool assumeStartPointInLoop)
		{
			var segments = (ISegmentCollection) geometry;

			int totalSegmentCount = segments.SegmentCount;

			object missing = Type.Missing;

			IPolygon loopPolygon = CreateEmptyLoopPolygon(geometry);
			var loopSegments = (ISegmentCollection) loopPolygon;

			if (! assumeStartPointInLoop)
			{
				for (int i = startSegmentIndex; i <= endSegmentIndex; i++)
				{
					loopSegments.AddSegment(segments.Segment[i], ref missing, ref missing);
				}
			}
			else
			{
				// Add the inverse segment set: from the segment after the end segment to the last one:
				for (int i = endSegmentIndex + 1; i < totalSegmentCount; i++)
				{
					loopSegments.AddSegment(segments.Segment[i], ref missing, ref missing);
				}

				// ... plus from the first segment to before the start segment index
				for (var i = 0; i < startSegmentIndex; i++)
				{
					loopSegments.AddSegment(segments.Segment[i], ref missing, ref missing);
				}
			}

			// important: don't simplify here, the raw segment orientation is needed
			return loopPolygon;
		}

		[NotNull]
		private static IPolygon CreateEmptyLoopPolygon([NotNull] IGeometry geometry)
		{
			var polygon = new PolygonClass
			              {
				              SpatialReference = geometry.SpatialReference
			              };

			((IZAware) polygon).ZAware = ((IZAware) geometry).ZAware;

			return polygon;
		}

		private class VertexSearcher
		{
			private readonly double _tolerance;
			private readonly double _toleranceSquared;
			[NotNull] private readonly BoxTree<Vertex> _boxTree;

			public VertexSearcher([NotNull] IEnvelope envelope, double tolerance)
			{
				Assert.ArgumentNotNull(envelope, nameof(envelope));

				_tolerance = tolerance;
				_toleranceSquared = tolerance * tolerance;

				_boxTree = BoxTreeUtils.CreateBoxTree<Vertex>(envelope.XMin, envelope.YMin,
				                                              envelope.XMax, envelope.YMax,
				                                              maxElementsPerTile: 16);
			}

			public void Add([NotNull] Vertex vertex)
			{
				_boxTree.Add(new Pnt2D(vertex.X, vertex.Y), vertex);
			}

			[CanBeNull]
			public Vertex GetNearestVertex([NotNull] Vertex searchVertex)
			{
				var searchBox =
					new Box(new Pnt2D(searchVertex.X - _tolerance, searchVertex.Y - _tolerance),
					        new Pnt2D(searchVertex.X + _tolerance, searchVertex.X + _tolerance));

				double minDistanceSqr = double.MaxValue;
				Vertex nearestVertex = null;

				foreach (BoxTree<Vertex>.TileEntry tileEntry in _boxTree.Search(searchBox))
				{
					Vertex candidate = tileEntry.Value;
					double distanceSqr = GetDistanceSqr(searchVertex, candidate);

					if (distanceSqr > _toleranceSquared)
					{
						// outside tolerance, ignore
						continue;
					}

					if (distanceSqr < minDistanceSqr)
					{
						// nearest so far
						nearestVertex = candidate;
						minDistanceSqr = distanceSqr;
					}
				}

				return nearestVertex;
			}

			private static double GetDistanceSqr([NotNull] Vertex v0, [NotNull] Vertex v1)
			{
				double dx = v1.X - v0.X;
				double dy = v1.Y - v0.Y;

				return dx * dx + dy * dy;
			}
		}

		private class Vertex
		{
			public Vertex(double x, double y, int index)
			{
				X = x;
				Y = y;
				Index = index;
			}

			public double X { get; }

			public double Y { get; }

			public int Index { get; }
		}
	}
}
