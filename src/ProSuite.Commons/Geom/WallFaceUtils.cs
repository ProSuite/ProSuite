using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// How two adjacent wall faces are joined where the sketch bends in XY.
	/// </summary>
	public enum WallCornerJoin
	{
		/// <summary>
		/// Both sides are extended to the mitered corner point, so the two faces butt against
		/// each other along the full cross-section. The footprint is the mitered buffer. Where
		/// the slope changes as well, the resulting Z step is closed by a vertical filler ring
		/// per side.
		/// </summary>
		Miter = 0,

		/// <summary>
		/// The outside of the bend is cut back to the perpendicular offset point of either
		/// segment and the remaining wedge is closed by a single sloped filler triangle (which
		/// absorbs the Z step, so no vertical filler is needed there). The inside of the bend
		/// is mitered as in <see cref="Miter"/>.
		/// </summary>
		Bevel = 1,

		/// <summary>
		/// Like <see cref="Bevel"/>, but the wedge on the outside of the bend is closed by a fan
		/// of sloped filler triangles (roughly one per 45 degrees of turn), which avoids a single
		/// long chord at very sharp corners. This is the default: it keeps the vertical filler
		/// rings to the inside of the bends, where they are least conspicuous, and needs no
		/// miter limit on the outside.
		/// </summary>
		Round = 2
	}

	/// <summary>
	/// Builds the planar faces of a "wall": the 3D band that results from offsetting a 3D
	/// sketch line to the left and/or the right.
	/// <para>
	/// A wall cannot be modelled as the single buffer outline: as soon as consecutive sketch
	/// vertices do not share one plane in Z, that outline is warped (non-coplanar) and renders
	/// as an arbitrarily triangulated, non-planar multipatch face. Instead the band is cut into
	/// faces that are each exactly planar - one face per maximal run of coplanar segments - and
	/// the faces are stitched with filler rings at the corners.
	/// </para>
	/// <para>
	/// The hard guarantee is that there is no gap - neither in XY nor in Z - between the
	/// resulting rings: every ring shares a full 3D line segment with each of its neighbours.
	/// Where the two adjacent planes meet at a corner point at different heights, that step
	/// cannot be closed by a face that lies in either plane, so a filler ring is emitted; see
	/// <see cref="WallCornerJoin"/> for the available flavours.
	/// </para>
	/// </summary>
	public static class WallFaceUtils
	{
		private const double _defaultMiterLimit = 10.0;
		private const double _defaultTolerance = 1e-8;

		/// <summary>
		/// Builds the planar wall faces for the given line parts.
		/// </summary>
		/// <param name="pathsToBuffer">The line parts (open or closed) to build the wall from.</param>
		/// <param name="offsetDistances">The offset distance per part, applied to each buffered
		/// side. Must have the same count as <paramref name="pathsToBuffer"/>.</param>
		/// <param name="bufferSide">The side(s) to offset, seen in digitizing direction. The side
		/// that is not offset has its face boundary on the sketch line itself.</param>
		/// <param name="cornerJoin">How adjacent faces are joined where the sketch bends. The
		/// default keeps the vertical filler rings to the inside of the bends.</param>
		/// <param name="miterLimit">The ratio of the miter length to the offset distance beyond
		/// which a mitered corner point is clamped, to avoid long spikes at sharp corners.</param>
		/// <param name="tolerance">Distances (and height differences) at or below this value are
		/// considered zero.</param>
		/// <returns>A polyhedron whose ring groups are the individual planar faces, or null if
		/// no valid face could be built.</returns>
		[CanBeNull]
		public static Polyhedron GetWallFaces(
			[NotNull] IList<Linestring> pathsToBuffer,
			[NotNull] IList<double> offsetDistances,
			BufferSide bufferSide,
			WallCornerJoin cornerJoin = WallCornerJoin.Round,
			double miterLimit = _defaultMiterLimit,
			double tolerance = _defaultTolerance)
		{
			Assert.ArgumentNotNull(pathsToBuffer, nameof(pathsToBuffer));
			Assert.ArgumentNotNull(offsetDistances, nameof(offsetDistances));
			Assert.ArgumentCondition(pathsToBuffer.Count == offsetDistances.Count,
			                         "Path count and offset distance count must match");

			var faces = new List<RingGroup>();

			for (var i = 0; i < pathsToBuffer.Count; i++)
			{
				Linestring path = pathsToBuffer[i];
				double distance = offsetDistances[i];

				if (path == null || path.SegmentCount == 0 || distance <= 0)
				{
					continue;
				}

				// The side that is not buffered gets a zero offset, so that its face boundary
				// lies on the sketch line (matching the one-sided buffer, whose one boundary is
				// the line itself).
				double leftDistance = bufferSide == BufferSide.Right ? 0 : distance;
				double rightDistance = bufferSide == BufferSide.Left ? 0 : distance;

				var builder = new WallBuilder(path, leftDistance, rightDistance, cornerJoin,
				                              miterLimit, tolerance);

				builder.AddFaces(faces);
			}

			return faces.Count == 0 ? null : new Polyhedron(faces);
		}

		/// <summary>
		/// The plane a face lies in, expressed as the height gradient (the steepest ascent per
		/// horizontal unit) through a reference point. A face plane is never vertical, so the
		/// height is a function of X and Y.
		/// </summary>
		private struct FacePlane
		{
			private readonly double _refX;
			private readonly double _refY;
			private readonly double _refZ;

			public FacePlane(Pnt3D reference, double gradientX, double gradientY)
			{
				_refX = reference.X;
				_refY = reference.Y;
				_refZ = reference.Z;

				GradientX = gradientX;
				GradientY = gradientY;
			}

			public double GradientX { get; }

			public double GradientY { get; }

			public double GetZ(double x, double y)
			{
				return _refZ + GradientX * (x - _refX) + GradientY * (y - _refY);
			}

			public Pnt3D Project(double x, double y)
			{
				return new Pnt3D(x, y, GetZ(x, y));
			}
		}

		/// <summary>
		/// One side (left or right) of the joint at a sketch vertex: where the boundary of the
		/// face before the vertex ends, where the boundary of the face after it starts, and the
		/// intermediate points of the fan that bridges the two (empty unless the corner is
		/// rounded).
		/// </summary>
		private sealed class SideJoint
		{
			public SideJoint(double prevX, double prevY, double nextX, double nextY,
			                 [CanBeNull] IList<double[]> fan = null)
			{
				PrevX = prevX;
				PrevY = prevY;
				NextX = nextX;
				NextY = nextY;
				Fan = fan ?? new List<double[]>(0);
			}

			public double PrevX { get; }
			public double PrevY { get; }
			public double NextX { get; }
			public double NextY { get; }

			[NotNull]
			public IList<double[]> Fan { get; }
		}

		private sealed class Joint
		{
			public Joint([NotNull] SideJoint left, [NotNull] SideJoint right)
			{
				Left = left;
				Right = right;
			}

			[NotNull]
			public SideJoint Left { get; }

			[NotNull]
			public SideJoint Right { get; }
		}

		/// <summary>
		/// Builds the faces of a single line part. All arrays are indexed by segment index
		/// (0 .. SegmentCount-1) or by vertex index (0 .. SegmentCount), as noted.
		/// </summary>
		private sealed class WallBuilder
		{
			private readonly double _leftDistance;
			private readonly double _rightDistance;
			private readonly WallCornerJoin _cornerJoin;
			private readonly double _miterLimit;
			private readonly double _tolerance;

			// Vertex index 0 .. _segmentCount (the last one repeats the first if closed).
			private readonly List<Pnt3D> _vertices;
			private readonly bool _closed;
			private readonly int _segmentCount;

			// Segment index 0 .. _segmentCount - 1.
			private readonly double[] _dirX;
			private readonly double[] _dirY;
			private readonly double[] _length;
			private readonly double[] _gradientX;
			private readonly double[] _gradientY;

			// Vertex index 0 .. _segmentCount. For a closed part the entries at 0 and at
			// _segmentCount describe the same (seam) joint.
			private readonly Joint[] _joints;

			public WallBuilder([NotNull] Linestring path, double leftDistance,
			                   double rightDistance, WallCornerJoin cornerJoin,
			                   double miterLimit, double tolerance)
			{
				_leftDistance = leftDistance;
				_rightDistance = rightDistance;
				_cornerJoin = cornerJoin;
				_miterLimit = miterLimit;
				_tolerance = tolerance;

				_vertices = GetSignificantVertices(path, tolerance, out _closed);
				_segmentCount = _vertices.Count - 1;

				if (_segmentCount < 1)
				{
					return;
				}

				_dirX = new double[_segmentCount];
				_dirY = new double[_segmentCount];
				_length = new double[_segmentCount];
				_gradientX = new double[_segmentCount];
				_gradientY = new double[_segmentCount];

				InitializeSegments();

				// A closed part must start at a plane change, otherwise the first and the last
				// face would be two halves of one plane that are not joined at the seam.
				RotateClosedPartToPlaneChange();

				_joints = new Joint[_segmentCount + 1];
			}

			public void AddFaces([NotNull] List<RingGroup> faces)
			{
				if (_segmentCount < 1)
				{
					return;
				}

				InitializeJoints();

				IList<int> runEnds = GetRunEnds();

				var runStart = 0;
				foreach (int runEnd in runEnds)
				{
					AddFace(faces, runStart, runEnd);
					runStart = runEnd + 1;
				}

				// The corners between the runs: close the Z step (and, for a bevelled or rounded
				// join, the XY wedge) between the face before and the face after the corner.
				foreach (int runEnd in runEnds)
				{
					if (runEnd == _segmentCount - 1 && ! _closed)
					{
						continue; // the far end of an open part: no face follows
					}

					// Past the last segment of a closed part the wall continues at its seam.
					int nextSegment = (runEnd + 1) % _segmentCount;

					AddFillers(faces, nextSegment, runEnd, nextSegment);
				}
			}

			#region Set-up

			/// <summary>
			/// The path vertices with the ones that do not span a horizontal distance removed:
			/// a segment without a horizontal extent has neither a plane nor an offset
			/// direction. Of such a group of vertices the first one is kept, so a purely
			/// vertical step in the sketch keeps the height it starts at.
			/// </summary>
			[NotNull]
			private static List<Pnt3D> GetSignificantVertices([NotNull] Linestring path,
			                                                  double tolerance, out bool closed)
			{
				var result = new List<Pnt3D>();

				foreach (Pnt3D point in path.GetPoints(clone: true))
				{
					if (result.Count > 0 && IsSamePointXY(result[result.Count - 1], point,
					                                      tolerance))
					{
						continue;
					}

					result.Add(point);
				}

				closed = false;

				if (path.IsClosed && result.Count > 1 &&
				    IsSamePointXY(result[0], result[result.Count - 1], tolerance))
				{
					// Keep the repeated start point as the last vertex, it delimits the last
					// segment.
					closed = result.Count >= 4;
				}

				return result;
			}

			private void InitializeSegments()
			{
				for (var s = 0; s < _segmentCount; s++)
				{
					Pnt3D start = _vertices[s];
					Pnt3D end = _vertices[s + 1];

					double dx = end.X - start.X;
					double dy = end.Y - start.Y;
					double length = Math.Sqrt(dx * dx + dy * dy);

					_length[s] = length;
					_dirX[s] = dx / length;
					_dirY[s] = dy / length;

					// The height gradient of the segment's plane: the slope, in the horizontal
					// direction of the segment. Perpendicular to the segment the plane is level,
					// because the cross-section of the wall is horizontal.
					double slope = (end.Z - start.Z) / length;

					_gradientX[s] = slope * _dirX[s];
					_gradientY[s] = slope * _dirY[s];
				}
			}

			private void RotateClosedPartToPlaneChange()
			{
				if (! _closed)
				{
					return;
				}

				for (var s = 0; s < _segmentCount; s++)
				{
					int previous = (s - 1 + _segmentCount) % _segmentCount;

					if (! IsSamePlane(previous, s))
					{
						Rotate(s);
						return;
					}
				}

				// A completely coplanar loop has no plane change to start at; any seam will do
				// and GetRunEnds cuts the loop in two so that neither face becomes an annulus.
			}

			private void Rotate(int by)
			{
				if (by == 0)
				{
					return;
				}

				List<Pnt3D> rotated = _vertices.Skip(by).Take(_segmentCount - by)
				                               .Concat(_vertices.Take(by + 1)).ToList();

				_vertices.Clear();
				_vertices.AddRange(rotated);

				RotateInPlace(_dirX, by);
				RotateInPlace(_dirY, by);
				RotateInPlace(_length, by);
				RotateInPlace(_gradientX, by);
				RotateInPlace(_gradientY, by);
			}

			private static void RotateInPlace([NotNull] double[] values, int by)
			{
				double[] rotated = values.Skip(by).Concat(values.Take(by)).ToArray();

				Array.Copy(rotated, values, values.Length);
			}

			/// <summary>
			/// The (inclusive) last segment index of each maximal run of coplanar segments.
			/// </summary>
			[NotNull]
			private IList<int> GetRunEnds()
			{
				var result = new List<int>();

				for (var s = 0; s < _segmentCount - 1; s++)
				{
					if (! IsSamePlane(s, s + 1))
					{
						result.Add(s);
					}
				}

				result.Add(_segmentCount - 1);

				if (_closed && result.Count == 1)
				{
					// A fully coplanar loop: cut it in half so that neither face is an annulus.
					result.Insert(0, (_segmentCount - 1) / 2);
				}

				return result;
			}

			/// <summary>
			/// Whether two adjacent segments lie in one plane. Because they share a vertex, equal
			/// height gradients imply the same plane. The gradients are compared by the height
			/// difference they produce over the length of the longer segment.
			/// </summary>
			private bool IsSamePlane(int segment1, int segment2)
			{
				double referenceLength = Math.Max(_length[segment1], _length[segment2]);

				double dx = _gradientX[segment1] - _gradientX[segment2];
				double dy = _gradientY[segment1] - _gradientY[segment2];

				return Math.Sqrt(dx * dx + dy * dy) * referenceLength <= _tolerance;
			}

			private FacePlane GetPlane(int segment)
			{
				return new FacePlane(_vertices[segment], _gradientX[segment],
				                     _gradientY[segment]);
			}

			#endregion

			#region Joints

			private void InitializeJoints()
			{
				for (var k = 0; k <= _segmentCount; k++)
				{
					if (k == 0 || k == _segmentCount)
					{
						_joints[k] = _closed
							             ? GetCornerJoint(_segmentCount - 1, 0)
							             : GetEndCapJoint(k, k == 0 ? 0 : _segmentCount - 1);
					}
					else
					{
						_joints[k] = GetCornerJoint(k - 1, k);
					}
				}
			}

			/// <summary>
			/// The flat end of an open part: the boundary points are the perpendicular offsets of
			/// the single adjacent segment.
			/// </summary>
			[NotNull]
			private Joint GetEndCapJoint(int vertexIndex, int segment)
			{
				Pnt3D vertex = _vertices[vertexIndex];

				double[] left = GetPerpendicular(vertex, segment, +1, _leftDistance);
				double[] right = GetPerpendicular(vertex, segment, -1, _rightDistance);

				return new Joint(new SideJoint(left[0], left[1], left[0], left[1]),
				                 new SideJoint(right[0], right[1], right[0], right[1]));
			}

			/// <summary>
			/// The joint between two consecutive segments.
			/// </summary>
			/// <param name="previousSegment">The segment before the vertex.</param>
			/// <param name="nextSegment">The segment after the vertex.</param>
			[NotNull]
			private Joint GetCornerJoint(int previousSegment, int nextSegment)
			{
				// Within one plane the corner is always mitered: there is no Z step to absorb,
				// so cutting the corner back would only open an XY gap.
				bool planeChange = ! IsSamePlane(previousSegment, nextSegment);

				int vertexIndex = (nextSegment == 0 && _closed) ? 0 : nextSegment;
				Pnt3D vertex = _vertices[vertexIndex];

				double cross = _dirX[previousSegment] * _dirY[nextSegment] -
				               _dirY[previousSegment] * _dirX[nextSegment];

				// Turning left (cross > 0) puts the outside of the bend on the right, and the
				// other way round.
				bool leftIsOutside = cross < 0;

				bool cutBackOutside = planeChange && _cornerJoin != WallCornerJoin.Miter;

				SideJoint left = GetSideJoint(vertex, previousSegment, nextSegment, +1,
				                              _leftDistance,
				                              cutBackOutside && leftIsOutside);

				SideJoint right = GetSideJoint(vertex, previousSegment, nextSegment, -1,
				                               _rightDistance,
				                               cutBackOutside && ! leftIsOutside);

				return new Joint(left, right);
			}

			/// <param name="sideSign">+1 for the left side, -1 for the right side.</param>
			/// <param name="cutBack">Whether to cut the corner back to the two perpendicular
			/// offset points instead of extending both faces to the mitered corner point.</param>
			[NotNull]
			private SideJoint GetSideJoint([NotNull] Pnt3D vertex, int previousSegment,
			                               int nextSegment, int sideSign, double distance,
			                               bool cutBack)
			{
				if (distance <= 0)
				{
					// The un-offset side: both faces end on the sketch line itself.
					return new SideJoint(vertex.X, vertex.Y, vertex.X, vertex.Y);
				}

				double[] fromPrevious =
					GetPerpendicular(vertex, previousSegment, sideSign, distance);
				double[] fromNext = GetPerpendicular(vertex, nextSegment, sideSign, distance);

				if (cutBack)
				{
					IList<double[]> fan =
						_cornerJoin == WallCornerJoin.Round
							? GetFan(vertex, fromPrevious, previousSegment, nextSegment)
							: null;

					return new SideJoint(fromPrevious[0], fromPrevious[1],
					                     fromNext[0], fromNext[1], fan);
				}

				double[] miter = GetMiterPoint(vertex, fromPrevious, fromNext, previousSegment,
				                               nextSegment, distance);

				return new SideJoint(miter[0], miter[1], miter[0], miter[1]);
			}

			[NotNull]
			private double[] GetPerpendicular([NotNull] Pnt3D vertex, int segment, int sideSign,
			                                  double distance)
			{
				// The left normal of the segment direction, i.e. the direction rotated by +90°.
				double normalX = -_dirY[segment];
				double normalY = _dirX[segment];

				return new[]
				       {
					       vertex.X + sideSign * normalX * distance,
					       vertex.Y + sideSign * normalY * distance
				       };
			}

			/// <summary>
			/// The point where the two offset lines meet, clamped so that it neither forms a long
			/// spike (miter limit) nor reaches beyond either of the two adjacent segments (which
			/// would fold the face over itself). Falls back to the perpendicular offset point of
			/// the previous segment if the offset lines are parallel.
			/// </summary>
			[NotNull]
			private double[] GetMiterPoint([NotNull] Pnt3D vertex, [NotNull] double[] fromPrevious,
			                               [NotNull] double[] fromNext, int previousSegment,
			                               int nextSegment, double distance)
			{
				double denominator = _dirX[previousSegment] * _dirY[nextSegment] -
				                     _dirY[previousSegment] * _dirX[nextSegment];

				if (Math.Abs(denominator) < 1e-12)
				{
					// Collinear (the two offset points coincide) or a 180° reversal (no miter
					// point exists).
					return fromPrevious;
				}

				double along =
					((fromNext[0] - fromPrevious[0]) * _dirY[nextSegment] -
					 (fromNext[1] - fromPrevious[1]) * _dirX[nextSegment]) / denominator;

				double offsetX = fromPrevious[0] + along * _dirX[previousSegment] - vertex.X;
				double offsetY = fromPrevious[1] + along * _dirY[previousSegment] - vertex.Y;

				double scale = 1.0;

				// The miter point always lies beyond the end of the previous segment and before
				// the start of the next one; do not let it reach past either of them.
				double beyondPrevious = offsetX * _dirX[previousSegment] +
				                        offsetY * _dirY[previousSegment];
				double beforeNext = -(offsetX * _dirX[nextSegment] +
				                      offsetY * _dirY[nextSegment]);

				if (beyondPrevious > _length[previousSegment])
				{
					scale = Math.Min(scale, _length[previousSegment] / beyondPrevious);
				}

				if (beforeNext > _length[nextSegment])
				{
					scale = Math.Min(scale, _length[nextSegment] / beforeNext);
				}

				double miterLength = Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
				double maxLength = _miterLimit * distance;

				if (miterLength > maxLength && miterLength > 0)
				{
					scale = Math.Min(scale, maxLength / miterLength);
				}

				return new[]
				       {
					       vertex.X + offsetX * scale,
					       vertex.Y + offsetY * scale
				       };
			}

			/// <summary>
			/// The intermediate points of a rounded corner: the perpendicular offset point of the
			/// previous segment, rotated about the vertex towards the one of the next segment.
			/// </summary>
			[NotNull]
			private IList<double[]> GetFan([NotNull] Pnt3D vertex, [NotNull] double[] fromPrevious,
			                               int previousSegment, int nextSegment)
			{
				double cross = _dirX[previousSegment] * _dirY[nextSegment] -
				               _dirY[previousSegment] * _dirX[nextSegment];
				double dot = _dirX[previousSegment] * _dirX[nextSegment] +
				             _dirY[previousSegment] * _dirY[nextSegment];

				// The turn angle of the sketch, which is also the angle between the two
				// perpendicular offset points.
				double turnAngle = Math.Atan2(cross, dot);

				var stepCount = (int) Math.Ceiling(Math.Abs(turnAngle) / (Math.PI / 4));

				var result = new List<double[]>(Math.Max(0, stepCount - 1));

				double radiusX = fromPrevious[0] - vertex.X;
				double radiusY = fromPrevious[1] - vertex.Y;

				for (var step = 1; step < stepCount; step++)
				{
					double angle = turnAngle * step / stepCount;
					double cos = Math.Cos(angle);
					double sin = Math.Sin(angle);

					result.Add(new[]
					           {
						           vertex.X + radiusX * cos - radiusY * sin,
						           vertex.Y + radiusX * sin + radiusY * cos
					           });
				}

				return result;
			}

			#endregion

			#region Faces and fillers

			/// <summary>
			/// The face of one run of coplanar segments: the left boundary from the start to the
			/// end of the run, then back along the right boundary. The sketch vertex is an
			/// explicit ring vertex on either cross-section, so that the filler rings at the
			/// corners share complete edges with the face rather than parts of one.
			/// </summary>
			private void AddFace([NotNull] List<RingGroup> faces, int firstSegment,
			                     int lastSegment)
			{
				FacePlane plane = GetPlane(firstSegment);

				var ring = new List<Pnt3D>();

				for (int k = firstSegment; k <= lastSegment + 1; k++)
				{
					SideJoint left = _joints[k].Left;

					ring.Add(k == firstSegment
						         ? plane.Project(left.NextX, left.NextY)
						         : plane.Project(left.PrevX, left.PrevY));
				}

				ring.Add(_vertices[lastSegment + 1].ClonePnt3D());

				for (int k = lastSegment + 1; k >= firstSegment; k--)
				{
					SideJoint right = _joints[k].Right;

					ring.Add(k == firstSegment
						         ? plane.Project(right.NextX, right.NextY)
						         : plane.Project(right.PrevX, right.PrevY));
				}

				ring.Add(_vertices[firstSegment].ClonePnt3D());

				AddRing(faces, ring, _tolerance);
			}

			/// <summary>
			/// The filler rings that close the corner at the given vertex, per side: a fan of
			/// triangles from the sketch vertex to the chain of boundary points that leads from
			/// where the previous face ends to where the next face starts. Each triangle shares a
			/// complete edge with its neighbour, so the surface stays closed in 3D.
			/// </summary>
			private void AddFillers([NotNull] List<RingGroup> faces, int vertexIndex,
			                        int previousSegment, int nextSegment)
			{
				Pnt3D vertex = _vertices[vertexIndex];

				FacePlane previousPlane = GetPlane(previousSegment);
				FacePlane nextPlane = GetPlane(nextSegment);

				Joint joint = _joints[vertexIndex];

				IList<Pnt3D> leftChain =
					GetJointChain(joint.Left, previousPlane, nextPlane);

				for (var i = 0; i < leftChain.Count - 1; i++)
				{
					AddRing(faces,
					        new List<Pnt3D>
					        {
						        vertex.ClonePnt3D(), leftChain[i], leftChain[i + 1],
						        vertex.ClonePnt3D()
					        }, _tolerance);
				}

				// The right side is traversed the other way round, so that its rings are wound
				// like the faces they are attached to.
				IList<Pnt3D> rightChain =
					GetJointChain(joint.Right, previousPlane, nextPlane);

				for (int i = rightChain.Count - 1; i > 0; i--)
				{
					AddRing(faces,
					        new List<Pnt3D>
					        {
						        vertex.ClonePnt3D(), rightChain[i], rightChain[i - 1],
						        vertex.ClonePnt3D()
					        }, _tolerance);
				}
			}

			/// <summary>
			/// The boundary points to bridge on one side of a joint, from where the previous face
			/// ends (at its own plane's height) to where the next face starts (at the next
			/// plane's height). The intermediate points of a rounded corner get the height
			/// interpolated between the two.
			/// </summary>
			[NotNull]
			private static IList<Pnt3D> GetJointChain([NotNull] SideJoint side,
			                                          FacePlane previousPlane,
			                                          FacePlane nextPlane)
			{
				var result = new List<Pnt3D>(side.Fan.Count + 2);

				double startZ = previousPlane.GetZ(side.PrevX, side.PrevY);
				double endZ = nextPlane.GetZ(side.NextX, side.NextY);

				result.Add(new Pnt3D(side.PrevX, side.PrevY, startZ));

				for (var i = 0; i < side.Fan.Count; i++)
				{
					double[] point = side.Fan[i];
					double ratio = (i + 1.0) / (side.Fan.Count + 1.0);

					result.Add(new Pnt3D(point[0], point[1],
					                     startZ + ratio * (endZ - startZ)));
				}

				result.Add(new Pnt3D(side.NextX, side.NextY, endZ));

				return result;
			}

			#endregion
		}

		/// <summary>
		/// Adds the points as a closed ring, dropping consecutive duplicates. Rings that have no
		/// area in 3D (fewer than three distinct points, or all points on one line) are skipped.
		/// </summary>
		private static void AddRing([NotNull] List<RingGroup> faces,
		                            [NotNull] IList<Pnt3D> points, double tolerance)
		{
			var ring = new List<Pnt3D>(points.Count);

			foreach (Pnt3D point in points)
			{
				if (ring.Count > 0 && IsSamePoint(ring[ring.Count - 1], point, tolerance))
				{
					continue;
				}

				ring.Add(point);
			}

			while (ring.Count > 1 && IsSamePoint(ring[0], ring[ring.Count - 1], tolerance))
			{
				ring.RemoveAt(ring.Count - 1);
			}

			if (ring.Count < 3 || GetArea3D(ring) <= tolerance * tolerance)
			{
				return;
			}

			ring.Add(ring[0].ClonePnt3D());

			faces.Add(new RingGroup(new Linestring(ring)));
		}

		/// <summary>
		/// Twice the area of the ring in 3D, i.e. the length of the summed cross products. Unlike
		/// the 2D area this is non-zero for a vertical ring.
		/// </summary>
		private static double GetArea3D([NotNull] IList<Pnt3D> ring)
		{
			double sumX = 0, sumY = 0, sumZ = 0;

			Pnt3D origin = ring[0];

			for (var i = 1; i < ring.Count - 1; i++)
			{
				double ax = ring[i].X - origin.X;
				double ay = ring[i].Y - origin.Y;
				double az = ring[i].Z - origin.Z;

				double bx = ring[i + 1].X - origin.X;
				double by = ring[i + 1].Y - origin.Y;
				double bz = ring[i + 1].Z - origin.Z;

				sumX += ay * bz - az * by;
				sumY += az * bx - ax * bz;
				sumZ += ax * by - ay * bx;
			}

			return Math.Sqrt(sumX * sumX + sumY * sumY + sumZ * sumZ);
		}

		private static bool IsSamePointXY([NotNull] Pnt3D point1, [NotNull] Pnt3D point2,
		                                  double tolerance)
		{
			return Math.Abs(point1.X - point2.X) <= tolerance &&
			       Math.Abs(point1.Y - point2.Y) <= tolerance;
		}

		private static bool IsSamePoint([NotNull] Pnt3D point1, [NotNull] Pnt3D point2,
		                                double tolerance)
		{
			return IsSamePointXY(point1, point2, tolerance) &&
			       Math.Abs(point1.Z - point2.Z) <= tolerance;
		}
	}
}
