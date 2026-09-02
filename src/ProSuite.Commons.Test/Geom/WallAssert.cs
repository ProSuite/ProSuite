using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	/// <summary>
	/// Verifies that a set of wall faces forms a surface that is closed in 3D. This is the
	/// property the wall construction has to guarantee, so the check is deliberately independent
	/// of how the faces were built.
	/// </summary>
	public static class WallAssert
	{
		private const double _tolerance = 1e-6;

		// How far beyond an unshared edge to look for a face that continues the surface. Must be
		// well above the distance at which a point still counts as being on a face's boundary.
		private const double _probeDistance = 1e-4;
		private const double _onBoundaryTolerance = 1e-6;

		/// <summary>
		/// Asserts that there is no gap - neither in XY nor in Z - between the faces:
		/// <list type="number">
		/// <item>every face edge is either shared, in the opposite direction, with exactly one
		/// other face, or it lies on the outline of the wall;</item>
		/// <item>no outline edge has another face on its far side, i.e. the surface never simply
		/// stops where it continues in another face at another height;</item>
		/// <item>the faces are linearly connected: every face shares a whole edge - not just a
		/// point - with a neighbour, and the whole set hangs together.</item>
		/// </list>
		/// </summary>
		/// <param name="polyhedron">The wall faces.</param>
		/// <param name="requireNoOverlap">Whether faces that overlap in XY at the same height
		/// (which a clamped corner point can produce) are reported as well.</param>
		/// <param name="partCount">The number of line parts the wall was built from: the faces
		/// of one part must all hang together, but the parts need not touch each other.</param>
		public static void AssertGapFree([NotNull] Polyhedron polyhedron,
		                                 bool requireNoOverlap = true, int partCount = 1)
		{
			IList<Face> faces = GetFaces(polyhedron);

			Assert.Greater(faces.Count, 0, "No faces");

			// All vertices of all faces must be known before the ring segments can be cut at
			// them, so register them first.
			var vertices = new VertexSet(_tolerance);

			foreach (Face face in faces)
			{
				face.Register(vertices);
			}

			foreach (Face face in faces)
			{
				face.Split(vertices);
			}

			// 1: edge pairing.
			var edgeOwners = new Dictionary<Edge, List<Face>>();

			foreach (Face face in faces)
			{
				foreach (Edge edge in face.Edges)
				{
					List<Face> owners;
					if (! edgeOwners.TryGetValue(edge, out owners))
					{
						owners = new List<Face>();
						edgeOwners.Add(edge, owners);
					}

					owners.Add(face);
				}
			}

			foreach (KeyValuePair<Edge, List<Face>> pair in edgeOwners)
			{
				if (pair.Value.Count > 1)
				{
					Assert.Fail(
						"The edge {0} is used {1} times in the same direction (by the faces {2})",
						Format(pair.Key, vertices), pair.Value.Count,
						string.Join(", ", pair.Value.Select(f => f.Index.ToString())));
				}
			}

			// 2: an unshared edge must be on the outline.
			var adjacency = new Dictionary<int, HashSet<int>>();

			foreach (KeyValuePair<Edge, List<Face>> pair in edgeOwners)
			{
				Face face = pair.Value[0];

				List<Face> reverseOwners;
				if (edgeOwners.TryGetValue(pair.Key.Reversed(), out reverseOwners))
				{
					Connect(adjacency, face.Index, reverseOwners[0].Index);
					continue;
				}

				AssertIsOutlineEdge(face, pair.Key, vertices, faces, requireNoOverlap);
			}

			// 3: linear connectivity (a single face has nothing to be connected to).
			if (faces.Count == 1)
			{
				return;
			}

			foreach (Face face in faces)
			{
				Assert.IsTrue(adjacency.ContainsKey(face.Index),
				              "The face {0} ({1}) does not share a single edge with any other " +
				              "face: it is at best connected point-wise", face.Index,
				              Format(face.Ring));
			}

			AssertConnected(adjacency, faces.Count, partCount);
		}

		/// <summary>
		/// Asserts that each face is planar within the given tolerance.
		/// </summary>
		public static void AssertFacesArePlanar([NotNull] Polyhedron polyhedron,
		                                        double tolerance = 0.0001)
		{
			foreach (RingGroup ringGroup in polyhedron.RingGroups)
			{
				IList<Pnt3D> points = ringGroup.ExteriorRing.GetPoints().ToList();

				Plane3D plane = Plane3D.TryFitPlane(points, isRing: true);

				Assert.NotNull(plane, "No plane could be fitted to {0}",
				               Format(ringGroup.ExteriorRing));
				Assert.IsTrue(plane.IsDefined, "The face {0} is degenerate",
				              Format(ringGroup.ExteriorRing));

				foreach (Pnt3D point in points)
				{
					Assert.LessOrEqual(plane.GetDistanceAbs(point.X, point.Y, point.Z), tolerance,
					                   "The face {0} is not planar",
					                   Format(ringGroup.ExteriorRing));
				}
			}
		}

		/// <summary>
		/// Asserts that each face is wound clockwise in XY, the Esri convention for the exterior
		/// ring of a multipatch face that is seen from above. A vertical face has no XY
		/// orientation and is skipped.
		/// </summary>
		public static void AssertFacesAreClockwise([NotNull] Polyhedron polyhedron)
		{
			foreach (RingGroup ringGroup in polyhedron.RingGroups)
			{
				Linestring ring = ringGroup.ExteriorRing;

				if (ring.IsVerticalRing(_tolerance))
				{
					continue;
				}

				Assert.IsTrue(ring.ClockwiseOriented == true,
				              "The face {0} is not wound clockwise", Format(ring));
			}
		}

		/// <summary>
		/// Asserts that the faces tile their footprint without overlapping in XY: the summed
		/// face areas then equal the area of the union. Vertical faces contribute no area, so
		/// they do not affect the comparison.
		/// </summary>
		public static void AssertNoOverlap([NotNull] Polyhedron polyhedron)
		{
			MultiLinestring footprint = polyhedron.GetXYFootprint(0.001, 0.001, out _);

			Assert.AreEqual(footprint.GetArea2D(), polyhedron.GetArea2D(), 0.01,
			                "The faces overlap in XY");
		}

		/// <summary>
		/// Asserts that one of the faces is the given ring, in any rotation but not reversed.
		/// </summary>
		public static void AssertHasFace([NotNull] Polyhedron polyhedron,
		                                 [NotNull] string expectedPoints)
		{
			IList<Pnt3D> expected = ParsePoints(expectedPoints);

			foreach (RingGroup ringGroup in polyhedron.RingGroups)
			{
				if (IsSameRing(ringGroup.ExteriorRing, expected))
				{
					return;
				}
			}

			Assert.Fail("No face equals {0}. The faces are:{1}{2}", expectedPoints,
			            Environment.NewLine,
			            string.Join(Environment.NewLine,
			                        polyhedron.RingGroups.Select(g => Format(g.ExteriorRing))));
		}

		public static int GetVerticalFaceCount([NotNull] Polyhedron polyhedron)
		{
			return polyhedron.RingGroups.Count(g => g.IsVertical(_tolerance));
		}

		#region Formatting and parsing

		[NotNull]
		public static IList<Pnt3D> ParsePoints([NotNull] string points)
		{
			var result = new List<Pnt3D>();

			foreach (string token in points.Split(new[] { ' ' },
			                                      StringSplitOptions.RemoveEmptyEntries))
			{
				string[] parts = token.Split('/');

				Assert.AreEqual(3, parts.Length, "Not an x/y/z point: {0}", token);

				result.Add(new Pnt3D(ParseDouble(parts[0]), ParseDouble(parts[1]),
				                     ParseDouble(parts[2])));
			}

			return result;
		}

		private static double ParseDouble([NotNull] string value)
		{
			return double.Parse(value, CultureInfo.InvariantCulture);
		}

		[NotNull]
		public static string Format([NotNull] Linestring linestring)
		{
			var sb = new StringBuilder();

			foreach (Pnt3D point in linestring.GetPoints())
			{
				if (sb.Length > 0)
				{
					sb.Append(' ');
				}

				sb.Append(Format(point));
			}

			return sb.ToString();
		}

		[NotNull]
		private static string Format([NotNull] Pnt3D point)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0:0.####}/{1:0.####}/{2:0.####}",
			                     point.X, point.Y, point.Z);
		}

		[NotNull]
		private static string Format(Edge edge, [NotNull] VertexSet vertices)
		{
			return Format(vertices[edge.From]) + " -> " + Format(vertices[edge.To]);
		}

		#endregion

		#region Implementation

		private static void AssertIsOutlineEdge([NotNull] Face face, Edge edge,
		                                        [NotNull] VertexSet vertices,
		                                        [NotNull] IList<Face> faces,
		                                        bool requireNoOverlap)
		{
			Pnt3D from = vertices[edge.From];
			Pnt3D to = vertices[edge.To];

			double midX = (from.X + to.X) / 2;
			double midY = (from.Y + to.Y) / 2;
			double midZ = (from.Z + to.Z) / 2;

			double dx = to.X - from.X;
			double dy = to.Y - from.Y;
			double lengthXY = Math.Sqrt(dx * dx + dy * dy);

			// The point just beyond the edge, outside the face. For a clockwise ring the
			// interior is to the right of the traversal direction, so the outside is to the
			// left. A vertical edge (or a face without an XY extent) has no such direction: the
			// edge point itself must then not be inside another face.
			double probeX = midX;
			double probeY = midY;

			if (lengthXY > _tolerance && ! face.IsVerticalXY)
			{
				double probeDistance = _probeDistance * Math.Max(1, lengthXY);

				probeX -= dy / lengthXY * probeDistance;
				probeY += dx / lengthXY * probeDistance;
			}

			foreach (Face other in faces)
			{
				if (other.Index == face.Index || other.IsVerticalXY)
				{
					continue;
				}

				// Only a face that touches this one can be its missing neighbour. A sketch that
				// crosses over itself has unrelated faces above or below each other, which is
				// not a gap.
				if (! face.Touches(other))
				{
					continue;
				}

				if (! other.ContainsInteriorXY(probeX, probeY))
				{
					continue;
				}

				double otherZ = other.GetZ(midX, midY);

				if (Math.Abs(otherZ - midZ) > _tolerance)
				{
					Assert.Fail(
						"Gap in Z: the face {0} ({1}) ends at the edge {2} while the face {3} " +
						"({4}) covers it at {5:0.####} instead of {6:0.####}",
						face.Index, Format(face.Ring), Format(edge, vertices), other.Index,
						Format(other.Ring), otherZ, midZ);
				}

				if (requireNoOverlap)
				{
					Assert.Fail(
						"The face {0} ({1}) ends at the edge {2}, but the face {3} ({4}) " +
						"continues across it", face.Index, Format(face.Ring),
						Format(edge, vertices), other.Index, Format(other.Ring));
				}
			}
		}

		private static void Connect([NotNull] Dictionary<int, HashSet<int>> adjacency, int face1,
		                            int face2)
		{
			Add(adjacency, face1, face2);
			Add(adjacency, face2, face1);
		}

		private static void Add([NotNull] Dictionary<int, HashSet<int>> adjacency, int from,
		                        int to)
		{
			HashSet<int> neighbours;
			if (! adjacency.TryGetValue(from, out neighbours))
			{
				neighbours = new HashSet<int>();
				adjacency.Add(from, neighbours);
			}

			neighbours.Add(to);
		}

		private static void AssertConnected([NotNull] Dictionary<int, HashSet<int>> adjacency,
		                                    int faceCount, int expectedComponentCount)
		{
			var visited = new HashSet<int>();
			var componentCount = 0;

			for (var seed = 0; seed < faceCount; seed++)
			{
				if (! visited.Add(seed))
				{
					continue;
				}

				componentCount++;

				var pending = new Stack<int>();
				pending.Push(seed);

				while (pending.Count > 0)
				{
					int current = pending.Pop();

					HashSet<int> neighbours;
					if (! adjacency.TryGetValue(current, out neighbours))
					{
						continue;
					}

					foreach (int neighbour in neighbours.Where(n => visited.Add(n)))
					{
						pending.Push(neighbour);
					}
				}
			}

			Assert.AreEqual(expectedComponentCount, componentCount,
			                "The faces fall apart into {0} groups that are not connected to " +
			                "each other", componentCount);
		}

		private static bool IsSameRing([NotNull] Linestring ring, [NotNull] IList<Pnt3D> expected)
		{
			IList<Pnt3D> actual = ring.GetPoints().ToList();

			// Both are closed rings, so drop the repeated last point.
			actual.RemoveAt(actual.Count - 1);

			IList<Pnt3D> wanted = expected;
			if (wanted.Count > 1 && IsSamePoint(wanted[0], wanted[wanted.Count - 1]))
			{
				wanted = wanted.Take(wanted.Count - 1).ToList();
			}

			if (actual.Count != wanted.Count)
			{
				return false;
			}

			for (var offset = 0; offset < actual.Count; offset++)
			{
				var same = true;

				for (var i = 0; i < wanted.Count; i++)
				{
					if (! IsSamePoint(actual[(offset + i) % actual.Count], wanted[i]))
					{
						same = false;
						break;
					}
				}

				if (same)
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsSamePoint([NotNull] Pnt3D point1, [NotNull] Pnt3D point2)
		{
			return Math.Abs(point1.X - point2.X) <= _tolerance &&
			       Math.Abs(point1.Y - point2.Y) <= _tolerance &&
			       Math.Abs(point1.Z - point2.Z) <= _tolerance;
		}

		[NotNull]
		private static IList<Face> GetFaces([NotNull] Polyhedron polyhedron)
		{
			var result = new List<Face>();

			foreach (RingGroup ringGroup in polyhedron.RingGroups)
			{
				Assert.AreEqual(0, ringGroup.InteriorRingCount,
				                "A wall face is expected to be a simple ring");

				result.Add(new Face(result.Count, ringGroup.ExteriorRing, _tolerance));
			}

			return result;
		}

		#endregion

		#region Nested types

		/// <summary>
		/// The distinct 3D points of all faces. Points closer to each other than the tolerance
		/// are one and the same vertex, so that the edge comparison does not depend on how the
		/// coordinates were arrived at.
		/// </summary>
		private class VertexSet
		{
			private readonly double _snapTolerance;
			private readonly List<Pnt3D> _points = new List<Pnt3D>();

			public VertexSet(double snapTolerance)
			{
				_snapTolerance = snapTolerance;
			}

			public Pnt3D this[int index] => _points[index];

			public int Count => _points.Count;

			public int GetIndex([NotNull] Pnt3D point)
			{
				for (var i = 0; i < _points.Count; i++)
				{
					if (Math.Abs(_points[i].X - point.X) <= _snapTolerance &&
					    Math.Abs(_points[i].Y - point.Y) <= _snapTolerance &&
					    Math.Abs(_points[i].Z - point.Z) <= _snapTolerance)
					{
						return i;
					}
				}

				_points.Add(point);

				return _points.Count - 1;
			}
		}

		private struct Edge : IEquatable<Edge>
		{
			public Edge(int from, int to)
			{
				From = from;
				To = to;
			}

			public int From { get; }
			public int To { get; }

			public Edge Reversed()
			{
				return new Edge(To, From);
			}

			public bool Equals(Edge other)
			{
				return From == other.From && To == other.To;
			}

			public override bool Equals(object obj)
			{
				return obj is Edge && Equals((Edge) obj);
			}

			public override int GetHashCode()
			{
				return (From * 397) ^ To;
			}
		}

		private class Face
		{
			private readonly double _tolerance;
			private readonly IList<Pnt3D> _points;
			private readonly List<Edge> _edges = new List<Edge>();
			private readonly HashSet<int> _vertexIndexes = new HashSet<int>();

			private readonly double _gradientX;
			private readonly double _gradientY;
			private readonly Pnt3D _reference;

			public Face(int index, [NotNull] Linestring ring, double tolerance)
			{
				Index = index;
				Ring = ring;
				_tolerance = tolerance;

				_points = ring.GetPoints().ToList();
				_points.RemoveAt(_points.Count - 1); // the repeated last point

				double area2D = Math.Abs(ring.GetArea2D());
				IsVerticalXY = area2D <= tolerance;

				_reference = _points[0];

				if (! IsVerticalXY)
				{
					GetGradient(_points, out _gradientX, out _gradientY);
				}
			}

			public int Index { get; }

			[NotNull]
			public Linestring Ring { get; }

			public bool IsVerticalXY { get; }

			/// <summary>
			/// Whether the two faces have a vertex in common, i.e. whether they meet at all.
			/// </summary>
			public bool Touches([NotNull] Face other)
			{
				return _vertexIndexes.Overlaps(other._vertexIndexes);
			}

			[NotNull]
			public IEnumerable<Edge> Edges => _edges;

			public void Register([NotNull] VertexSet vertices)
			{
				foreach (Pnt3D point in _points)
				{
					_vertexIndexes.Add(vertices.GetIndex(point));
				}
			}

			/// <summary>
			/// Turns the ring into edges between the shared vertices, cutting each ring segment
			/// wherever a vertex of another face lies on it. Without that, a filler that is
			/// attached to the middle of a longer face edge would look unshared.
			/// </summary>
			public void Split([NotNull] VertexSet vertices)
			{
				var indexes = new List<int>();

				foreach (Pnt3D point in _points)
				{
					indexes.Add(vertices.GetIndex(point));
				}

				for (var i = 0; i < indexes.Count; i++)
				{
					int from = indexes[i];
					int to = indexes[(i + 1) % indexes.Count];

					foreach (int between in GetVerticesBetween(vertices, from, to))
					{
						_edges.Add(new Edge(from, between));
						from = between;
					}

					_edges.Add(new Edge(from, to));
				}
			}

			/// <summary>
			/// Whether the point is strictly inside the XY projection of the ring. A point on
			/// (or very close to) the boundary is not: that is where the neighbouring faces
			/// legitimately touch.
			/// </summary>
			public bool ContainsInteriorXY(double x, double y)
			{
				// Crossing number over the XY projection of the ring.
				var inside = false;

				for (var i = 0; i < _points.Count; i++)
				{
					Pnt3D current = _points[i];
					Pnt3D next = _points[(i + 1) % _points.Count];

					if (GetDistanceXY(current, next, x, y) <= _onBoundaryTolerance)
					{
						return false;
					}

					if (current.Y > y != next.Y > y &&
					    x < (next.X - current.X) * (y - current.Y) / (next.Y - current.Y) +
					    current.X)
					{
						inside = ! inside;
					}
				}

				return inside;
			}

			private static double GetDistanceXY([NotNull] Pnt3D from, [NotNull] Pnt3D to,
			                                    double x, double y)
			{
				double dx = to.X - from.X;
				double dy = to.Y - from.Y;

				double lengthSquared = dx * dx + dy * dy;

				double ratio = lengthSquared <= 0
					               ? 0
					               : ((x - from.X) * dx + (y - from.Y) * dy) / lengthSquared;

				ratio = Math.Max(0, Math.Min(1, ratio));

				double distanceX = from.X + ratio * dx - x;
				double distanceY = from.Y + ratio * dy - y;

				return Math.Sqrt(distanceX * distanceX + distanceY * distanceY);
			}

			public double GetZ(double x, double y)
			{
				return _reference.Z + _gradientX * (x - _reference.X) +
				       _gradientY * (y - _reference.Y);
			}

			[NotNull]
			private IEnumerable<int> GetVerticesBetween([NotNull] VertexSet vertices, int fromIndex,
			                                            int toIndex)
			{
				Pnt3D from = vertices[fromIndex];
				Pnt3D to = vertices[toIndex];

				double dx = to.X - from.X;
				double dy = to.Y - from.Y;
				double dz = to.Z - from.Z;

				double lengthSquared = dx * dx + dy * dy + dz * dz;

				if (lengthSquared <= 0)
				{
					yield break;
				}

				var found = new List<KeyValuePair<double, int>>();

				for (var i = 0; i < vertices.Count; i++)
				{
					if (i == fromIndex || i == toIndex)
					{
						continue;
					}

					Pnt3D point = vertices[i];

					double ratio = ((point.X - from.X) * dx + (point.Y - from.Y) * dy +
					                (point.Z - from.Z) * dz) / lengthSquared;

					if (ratio <= 0 || ratio >= 1)
					{
						continue;
					}

					double distanceX = from.X + ratio * dx - point.X;
					double distanceY = from.Y + ratio * dy - point.Y;
					double distanceZ = from.Z + ratio * dz - point.Z;

					if (distanceX * distanceX + distanceY * distanceY + distanceZ * distanceZ >
					    _tolerance * _tolerance)
					{
						continue;
					}

					found.Add(new KeyValuePair<double, int>(ratio, i));
				}

				foreach (KeyValuePair<double, int> pair in found.OrderBy(p => p.Key))
				{
					yield return pair.Value;
				}
			}

			private static void GetGradient([NotNull] IList<Pnt3D> points, out double gradientX,
			                                out double gradientY)
			{
				Plane3D plane = Plane3D.FitPlane(points);

				// The height gradient of the (non-vertical) plane.
				gradientX = -plane.A / plane.C;
				gradientY = -plane.B / plane.C;
			}
		}

		#endregion
	}
}
