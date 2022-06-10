using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Geom
{
	public class Linestring : ISegmentList, IPointList, IEquatable<Linestring>
	{
		private Box _boundingBox;
		private readonly List<Line3D> _segments;

		private bool? _isKnownClosed;
		private double? _knownArea;

		public static Linestring CreateEmpty()
		{
			var result = new Linestring(new List<Line3D>(0));

			result.SetEmpty();

			return result;
		}

		private Linestring([NotNull] List<Line3D> segments,
		                   double xMin,
		                   double yMin,
		                   double xMax,
		                   double yMax,
		                   int rightMostBottomIndex)
		{
			// for clone

			Assert.ArgumentNotNull(segments, nameof(segments));

			_segments = segments;

			XMin = xMin;
			YMin = yMin;
			XMax = xMax;
			YMax = yMax;

			RightMostBottomIndex = rightMostBottomIndex;
		}

		public Linestring([NotNull] IEnumerable<Pnt3D> vertices,
		                  bool ensureClockwise = false)
		{
			Assert.ArgumentNotNull(vertices, nameof(vertices));

			_segments = FromPoints(vertices);

			if (ensureClockwise && ClockwiseOriented == false)
			{
				ReverseOrientation();
			}
		}

		public Linestring([NotNull] IEnumerable<Line3D> lines)
		{
			Assert.ArgumentNotNull(lines, nameof(lines));

			var result = new List<Line3D>();

			var idx = 0;
			Line3D previousLine = null;
			foreach (Line3D line in lines)
			{
				UpdateBounds(line.StartPoint, idx, result, previousLine?.StartPoint);

				result.Add(line);

				previousLine = line;

				idx++;
			}

			if (previousLine != null)
				UpdateBounds(previousLine.EndPoint, idx, result, null);

			_segments = result;
		}

		public IList<Line3D> Segments => _segments.AsReadOnly();

		public int GetSegmentStartPointIndex(int segmentIndex)
		{
			return segmentIndex;
		}

		public Line3D this[int index] => _segments[index];

		public int PartCount => 1;

		public int SegmentCount => _segments.Count;

		public int PointCount => SegmentCount == 0 ? 0 : SegmentCount + 1;

		public double XMin { get; private set; } = double.MaxValue;
		public double YMin { get; private set; } = double.MaxValue;

		public double XMax { get; private set; } = double.MinValue;
		public double YMax { get; private set; } = double.MinValue;

		public int RightMostBottomIndex { get; private set; } = -1;

		public int AllowIndexingThreshold { get; set; } = 200;

		public ISpatialSearcher<int> SpatialIndex { get; set; }

		public IBox Extent2D
		{
			get
			{
				if (_boundingBox == null)
				{
					_boundingBox = new Box(new Pnt2D(XMin, YMin), new Pnt2D(XMax, YMax));
				}

				return _boundingBox;
			}
		}

		public bool IsClosed
		{
			get
			{
				if (_isKnownClosed == null)
				{
					_isKnownClosed = SegmentCount > 1 &&
					                 StartPoint.Equals(EndPoint);
				}

				return _isKnownClosed.Value;
			}
		}

		public bool? ClockwiseOriented
		{
			get
			{
				if (! IsClosed)
				{
					return null;
				}

				Assert.True(RightMostBottomIndex >= 0,
				            "Right-most-lowest point has not been defined.");

				// test orientation at the rightMostLowestIndex vertex, using segments that have an XY extent
				// ccw <=> the edge leaving V[rmin] is left of the entering edge
				Line3D leaving =
					_segments[RightMostBottomIndex == SegmentCount ? 0 : RightMostBottomIndex];

				if (leaving.Length2DSquared < double.Epsilon)
				{
					leaving = NextSegmentInRing(RightMostBottomIndex, true);
				}

				Line3D entering = PreviousSegmentInRing(RightMostBottomIndex, true);

				double isLeft = entering.IsLeftXY(leaving.EndPoint);

				if (MathUtils.AreEqual(0, isLeft))
				{
					return null;
				}

				bool ccw = isLeft > 0;

				return ! ccw;
			}
		}

		public Pnt3D StartPoint => _segments.Count == 0 ? null : _segments[0].StartPoint;

		public Pnt3D EndPoint =>
			_segments.Count == 0 ? null : _segments[SegmentCount - 1].EndPoint;

		public bool IsEmpty => _segments.Count == 0;

		public IEnumerator<Line3D> GetEnumerator()
		{
			return _segments.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool Equals(Linestring other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (_segments.Count != other._segments.Count)
			{
				return false;
			}

			for (int i = 0; i < _segments.Count; i++)
			{
				if (! _segments[i].Equals(other._segments[i]))
				{
					return false;
				}
			}

			return true;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((Linestring) obj);
		}

		public override int GetHashCode()
		{
			return _segments.GetHashCode();
		}

		public override string ToString()
		{
			return $"Segment count: {SegmentCount}, length: {_segments.Sum(l => l.Length2D)}";
		}

		public Linestring Clone()
		{
			List<Line3D> clonedSegments = FromPoints(GetPoints(0, null, true), false);

			var result = new Linestring(clonedSegments, XMin, YMin, XMax, YMax,
			                            RightMostBottomIndex);

			return result;
		}

		public void Close(double tolerance = 0)
		{
			// double.Epsilon is not enough!
			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(StartPoint.X, StartPoint.Y);

			if (StartPoint.Equals(EndPoint, epsilon))
			{
				// Absolutely equal
				return;
			}

			if (StartPoint.EqualsXY(EndPoint, epsilon))
			{
				// Absolutely equal in XY, difference is only in Z
				UpdatePoint(PointCount - 1, EndPoint.X, EndPoint.Y, StartPoint.Z);
				return;
			}

			if (tolerance > double.Epsilon &&
			    StartPoint.EqualsXY(EndPoint, tolerance))
			{
				// Equal within the geometry's tolerance. Do not add an extra segment,
				// just update the current end point:
				UpdatePoint(PointCount - 1, StartPoint.X, StartPoint.Y, StartPoint.Z);
				return;
			}

			// Add a new segment
			Pnt3D previousEnd = EndPoint.ClonePnt3D();
			Pnt3D startPointClone = StartPoint.ClonePnt3D();
			_segments.Add(new Line3D(previousEnd, startPointClone));

			SpatialIndex = null;
			_isKnownClosed = null;
			_knownArea = null;
		}

		public void SetEmpty()
		{
			_segments.Clear();

			XMin = double.NaN;
			YMin = double.NaN;
			XMax = double.NaN;
			YMax = double.NaN;

			_boundingBox = null;
			RightMostBottomIndex = -1;

			_isKnownClosed = null;
			_knownArea = null;

			SpatialIndex = null;
		}

		public bool IsVerticalRing(double tolerance)
		{
			// NOTE: Just returning ClockwiseOriented == null reports incorrect results if the bottom
			// right has a cut-back.

			if (! IsClosed)
			{
				return false;
			}

			// Optimization using 2D area
			double area2D = Math.Abs(GetArea2D());

			double areaTolerance = GetLength2D() * tolerance;

			if (area2D > areaTolerance)
			{
				return false;
			}

			if (Math.Abs(area2D) < double.Epsilon)
			{
				// It's perfectly snapped and cracked
				return true;
			}

			// Each segment must be vertical or completely covered by at least one segment.
			for (var i = 0; i < _segments.Count; i++)
			{
				Line3D segment = _segments[i];

				if (segment.StartPoint.EqualsXY(segment.EndPoint, tolerance))
				{
					// vertical
					continue;
				}

				if (! GeomTopoOpUtils.IsSegmentCoveredWithSelfIntersectionsXY(
					    this, i, tolerance))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Determines whether this linestring has linear self-intersections, e.g. because it is
		/// partially vertical.
		/// </summary>
		/// <param name="tolerance"></param>
		/// <returns></returns>
		public bool HasLinearSelfIntersections(double tolerance)
		{
			// Some segment must be covered by other segments.
			for (var i = 0; i < _segments.Count; i++)
			{
				Line3D segment = _segments[i];

				if (segment.StartPoint.EqualsXY(segment.EndPoint, tolerance))
				{
					// vertical
					continue;
				}

				var segmentIntersectingLines =
					GeomTopoOpUtils.GetLinearSelfIntersectionsXY(this, i, tolerance);

				if (segmentIntersectingLines.Count == 0)
				{
					continue;
				}

				return true;
			}

			return false;
		}

		public bool IsFirstPointInPart(int vertexIndex)
		{
			return vertexIndex == 0;
		}

		public bool IsLastPointInPart(int vertexIndex)
		{
			return vertexIndex == PointCount - 1;
		}

		public bool IsFirstSegmentInPart(int segmentIndex)
		{
			return segmentIndex == 0;
		}

		public bool IsLastSegmentInPart(int segmentIndex)
		{
			return segmentIndex == SegmentCount - 1;
		}

		/// <summary>
		/// Segment access with better performance than using the <see cref="Segments"/> readonly list.
		/// </summary>
		/// <param name="segmentIndex"></param>
		/// <returns></returns>
		public Line3D GetSegment(int segmentIndex)
		{
			return _segments[segmentIndex];
		}

		public Linestring GetPart(int partIndex)
		{
			return this;
		}

		public IEnumerable<Pnt3D> GetPoints(int startPointIndex = 0,
		                                    int? pointCount = null,
		                                    bool clone = false)
		{
			// Line count + 1 - 1:
			int lastPoint = SegmentCount;

			if (startPointIndex == lastPoint)
			{
				if (lastPoint == 0)
				{
					// no point
					yield break;
				}

				// last point is start:
				Assert.ArgumentCondition(pointCount == null || pointCount <= 1,
				                         "Requested point count out of range");

				yield return _segments[SegmentCount - 1].EndPoint;

				yield break;
			}

			bool includeLastPoint = false;
			int endSegmentIndex;
			if (pointCount == null)
			{
				endSegmentIndex = SegmentCount - 1;
				includeLastPoint = true;
			}
			else
			{
				endSegmentIndex = startPointIndex + pointCount.Value - 1;

				if (endSegmentIndex > SegmentCount - 1)
				{
					endSegmentIndex -= 1;
					includeLastPoint = true;
				}
			}

			Line3D lastSegment = null;
			for (int i = startPointIndex; i <= endSegmentIndex; i++)
			{
				lastSegment = _segments[i];
				yield return clone ? lastSegment.StartPointCopy : lastSegment.StartPoint;
			}

			if (includeLastPoint)
			{
				Assert.NotNull(lastSegment);
				yield return clone ? lastSegment.EndPointCopy : lastSegment.EndPoint;
			}
		}

		public IPnt GetPoint(int pointIndex, bool clone = false)
		{
			return GetPoint3D(pointIndex, clone);
		}

		public void GetCoordinates(int pointIndex, out double x, out double y, out double z)
		{
			IPnt point = GetPoint(pointIndex);

			x = point.X;
			y = point.Y;
			z = point[2];
		}

		public void SnapToResolution(double resolution,
		                             double xOrigin,
		                             double yOrigin,
		                             double zOrigin = double.NaN)
		{
			InitializeBounds();

			int pointIdx = 0;
			Pnt3D previous = null;
			foreach (Pnt3D point in GetPoints())
			{
				point.X = xOrigin + Math.Round((point.X - xOrigin) / resolution) * resolution;
				point.Y = yOrigin + Math.Round((point.Y - yOrigin) / resolution) * resolution;

				if (! double.IsNaN(zOrigin) && ! double.IsNaN(point.Z))
				{
					point.Z = zOrigin + Math.Round((point.Z - zOrigin) / resolution) * resolution;
				}

				UpdateBounds(point, pointIdx, _segments, previous);

				previous = point;
				pointIdx++;
			}

			SpatialIndex = null;
		}

		public IPnt GetPointAlong(double distanceAlong, bool asRatio)
		{
			distanceAlong = asRatio ? GetLength2D() * distanceAlong : distanceAlong;

			double currentDistance = 0;
			foreach (Line3D segment in _segments)
			{
				double nextDistance = currentDistance + segment.Length2D;

				if (nextDistance < distanceAlong)
				{
					currentDistance = nextDistance;
					continue;
				}

				// It's along the current segment
				double distanceAlongSegment = distanceAlong - currentDistance;

				return segment.GetPointAlong(distanceAlongSegment, false);
			}

			throw new ArgumentOutOfRangeException(nameof(distanceAlong),
			                                      "The provided distance is outside the linestring");
		}

		public double GetDistanceAlong2D(int vertexIndex,
		                                 int startVertexIndex = 0)
		{
			double result = 0;

			int? segmentIndex = startVertexIndex;
			Line3D segment = GetSegment(startVertexIndex);

			while (segmentIndex != null && segmentIndex != vertexIndex)
			{
				segment = GetSegment(segmentIndex.Value);

				result += segment.Length2D;

				segmentIndex = NextSegmentIndex(segmentIndex.Value);
			}

			return result;
		}

		public IEnumerable<IPnt> AsEnumerablePoints(bool clone = false)
		{
			return GetPoints(0, null, clone);
		}

		public IEnumerable<int> FindPointIndexes(IPnt searchPoint,
		                                         double xyTolerance = double.Epsilon,
		                                         bool useSearchCircle = false,
		                                         bool allowIndexing = true)
		{
			if (SpatialIndex == null && allowIndexing &&
			    SegmentCount > AllowIndexingThreshold)
			{
				SpatialIndex = SpatialHashSearcher<int>.CreateSpatialSearcher(this);
			}

			if (SpatialIndex != null)
			{
				foreach (int foundSegmentIdx in SpatialIndex.Search(searchPoint.X, searchPoint.Y,
					         searchPoint.X, searchPoint.Y, xyTolerance))
				{
					Line3D segment = this[foundSegmentIdx];

					bool withinTolerance =
						GeomRelationUtils.IsWithinTolerance(segment.StartPoint, searchPoint,
						                                    xyTolerance, useSearchCircle);

					if (withinTolerance)
					{
						yield return foundSegmentIdx;
					}
				}
			}
			else
			{
				for (var i = 0; i < SegmentCount; i++)
				{
					bool withinTolerance =
						GeomRelationUtils.IsWithinTolerance(_segments[i].StartPoint, searchPoint,
						                                    xyTolerance, useSearchCircle);

					if (withinTolerance)
					{
						yield return i;
					}
				}
			}

			bool isEndPoint = EndPoint?.EqualsXY(searchPoint, xyTolerance) ?? false;

			if (isEndPoint)
			{
				yield return PointCount - 1;
			}
		}

		public Pnt3D GetPoint3D(int pointIndex, bool clone = false)
		{
			// Line count + 1 - 1:
			int lastPoint = SegmentCount;

			Pnt3D result;
			if (pointIndex == lastPoint)
			{
				// last point
				result = _segments[SegmentCount - 1].EndPoint;
			}
			else
			{
				result = _segments[pointIndex].StartPoint;
			}

			return clone ? result.ClonePnt3D() : result;
		}

		/// <summary>
		/// Returns the index of a vertex that is within the tolerance of the specified point. The tolerance is
		/// applied separately in X, Y and, if required, Z.
		/// </summary>
		/// <param name="point">The search point.</param>
		/// <param name="inXY">Whether or not only the XY coordinates should be compared.</param>
		/// <param name="tolerance">The search tolerance.</param>
		/// <param name="allowIndexing"></param>
		/// <returns></returns>
		[CanBeNull]
		public int? FindPointIdx([NotNull] Pnt3D point, bool inXY,
		                         double tolerance = double.Epsilon,
		                         bool allowIndexing = true)
		{
			foreach (int foundXY in FindPointIndexes(point, tolerance, false, allowIndexing))
			{
				if (inXY)
				{
					return foundXY;
				}

				if (! (GetPoint(foundXY) is Pnt3D foundPoint))
				{
					throw new InvalidOperationException(
						"Cannot determine 3D location for 2D point");
				}

				if (MathUtils.AreEqual(foundPoint.Z, point.Z, tolerance))
				{
					return foundXY;
				}
			}

			return null;
		}

		public IEnumerable<KeyValuePair<int, Line3D>> FindSegments(
			IBoundedXY searchGeometry,
			double tolerance,
			bool allowIndexing = true,
			Predicate<int> predicate = null)
		{
			return FindSegments(searchGeometry.XMin, searchGeometry.YMin,
			                    searchGeometry.XMax, searchGeometry.YMax, tolerance, allowIndexing,
			                    predicate);
		}

		public IEnumerable<KeyValuePair<int, Line3D>> FindSegments(
			double xMin, double yMin, double xMax, double yMax,
			double tolerance, bool allowIndexing = true, Predicate<int> predicate = null)
		{
			if (SpatialIndex == null && allowIndexing &&
			    SegmentCount > AllowIndexingThreshold)
			{
				SpatialIndex = SpatialHashSearcher<int>.CreateSpatialSearcher(this);
			}

			if (SpatialIndex != null)
			{
				foreach (int index in SpatialIndex.Search(
					         xMin, yMin, xMax, yMax, this, tolerance, predicate))
				{
					Line3D segment = _segments[index];
					if (segment.ExtentIntersectsXY(xMin, yMin, xMax, yMax, tolerance))
					{
						yield return new KeyValuePair<int, Line3D>(index, segment);
					}
				}
			}
			else
			{
				for (int i = 0; i < SegmentCount; i++)
				{
					if (predicate != null && ! predicate(i))
					{
						continue;
					}

					Line3D segment = this[i];
					if (segment.ExtentIntersectsXY(xMin, yMin, xMax, yMax, tolerance))
					{
						yield return new KeyValuePair<int, Line3D>(i, segment);
					}
				}
			}
		}

		public int GetLocalSegmentIndex(int globalSegmentIndex, out int linestringIndex)
		{
			linestringIndex = 0;
			return globalSegmentIndex;
		}

		public int GetGlobalSegmentIndex(int linestringIndex, int localSegmentIndex)
		{
			return localSegmentIndex;
		}

		public bool ExtentsIntersectXY(Linestring other, double tolerance)
		{
			return ExtentsIntersectXY(other.XMin, other.YMin, other.XMax, other.YMax,
			                          tolerance);
		}

		public bool ExtentsIntersectXY(double otherXMin, double otherYMin,
		                               double otherXMax, double otherYMax,
		                               double tolerance)
		{
			return ! GeomRelationUtils.AreBoundsDisjoint(XMin, YMin, XMax, YMax,
			                                             otherXMin, otherYMin, otherXMax, otherYMax,
			                                             tolerance);
		}

		public bool ExtentIntersectsXY(double x, double y, double tolerance)
		{
			if (x + tolerance < XMin)
			{
				return false;
			}

			if (x - tolerance > XMax)
			{
				return false;
			}

			if (y + tolerance < YMin)
			{
				return false;
			}

			if (y - tolerance > YMax)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// The next vertex index or null if the linestring is not closed and the
		/// specified segment is already the last one. For closed rings, the last and
		/// the first vertex are considered the same.
		/// </summary>
		/// <param name="currentIndex"></param>
		/// <returns></returns>
		public int? NextVertexIndex(int currentIndex)
		{
			int result = currentIndex + 1;

			if (result < PointCount)
			{
				return result;
			}

			if (IsClosed)
			{
				// Last point and 0 are considered the same vertex
				return result == PointCount ? 1 : result % PointCount;
			}

			return null;
		}

		/// <summary>
		/// The next segment index or null if the linestring is not closed and the
		/// specified segment is already the last one.
		/// </summary>
		/// <param name="currentSegmentIndex"></param>
		/// <returns></returns>
		public int? NextSegmentIndex(int currentSegmentIndex)
		{
			int result = currentSegmentIndex + 1;

			if (result < SegmentCount)
			{
				return result;
			}

			if (IsClosed)
			{
				return result % SegmentCount;
			}

			return null;
		}

		public int NextIndexInRing(int currentSegmentIndex)
		{
			int? result = NextSegmentIndex(currentSegmentIndex);

			if (result == null)
			{
				Assert.True(IsClosed,
				            "Must be closed ring to get next index after last.");
				Assert.CantReach("Unexpected null index despite closed ring.");
			}

			return result.Value;
		}

		[NotNull]
		public Line3D NextSegmentInRing(int currentIndex,
		                                bool skipVerticalSegments = false,
		                                double tolerance = 0)
		{
			Line3D result = NextSegment(currentIndex, skipVerticalSegments, tolerance);

			if (result == null)
			{
				throw new InvalidOperationException(
					$"Cannot get next segment after {currentIndex} because linestring ({this}) is no ring or has only vertical segments.");
			}

			return result;
		}

		public Line3D NextSegment(int currentSegmentIdx,
		                          bool skipVerticalSegments = false,
		                          double tolerance = 0)
		{
			int? nextIdx;

			var count = 0;
			while ((nextIdx = NextSegmentIndex(currentSegmentIdx)) != null)
			{
				Line3D nextSegment = this[nextIdx.Value];

				currentSegmentIdx = nextIdx.Value;

				if (skipVerticalSegments &&
				    MathUtils.AreEqual(nextSegment.Length2DSquared,
				                       tolerance * tolerance))
				{
					if (count++ > SegmentCount)
					{
						// Cannot get non-vertical segment. There are only vertical segments or none at all.
						return null;
					}

					continue;
				}

				return nextSegment;
			}

			return null;
		}

		public int? PreviousVertexIndex(int currentIndex)
		{
			if (currentIndex == 0)
			{
				// The last point is equal to 0, therefore jump to second-but-last:
				return IsClosed ? (int?) (PointCount - 2) : null;
			}

			return currentIndex - 1;
		}

		/// <summary>
		/// The previous segment index or null, if the linestring is not closed and
		/// the specified index is already 0.
		/// </summary>
		/// <param name="currentIndex"></param>
		/// <returns></returns>
		public int? PreviousSegmentIndex(int currentIndex)
		{
			if (currentIndex == 0)
			{
				return IsClosed ? (int?) (SegmentCount - 1) : null;
			}

			return currentIndex - 1;
		}

		public int PreviousIndexInRing(int currentIndex)
		{
			int? result = PreviousSegmentIndex(currentIndex);

			if (result == null)
			{
				Assert.True(IsClosed,
				            "Must be closed ring to get next index after last.");
				Assert.CantReach("Unexpected null index despite closed ring.");
			}

			return result.Value;
		}

		[NotNull]
		public Line3D PreviousSegmentInRing(int currentSegment,
		                                    bool skipVerticalSegments = false,
		                                    double tolerance = 0)
		{
			Line3D result =
				PreviousSegment(currentSegment, skipVerticalSegments, tolerance);

			if (result == null)
			{
				throw new InvalidOperationException(
					$"Cannot get previous segment before {currentSegment} because linestring ({this}) is no ring or has only vertical segments.");
			}

			return result;
		}

		public Line3D PreviousSegment(int currentSegmentIdx,
		                              bool skipVerticalSegments = false,
		                              double tolerance = 0)
		{
			int? previousIdx;

			int count = 0;
			while ((previousIdx = PreviousSegmentIndex(currentSegmentIdx)) != null)
			{
				Line3D previousSegment = this[previousIdx.Value];
				currentSegmentIdx = previousIdx.Value;

				if (skipVerticalSegments &&
				    MathUtils.AreEqual(previousSegment.Length2DSquared,
				                       tolerance * tolerance))
				{
					if (count++ > SegmentCount)
					{
						// Cannot get non-vertical segment. There are only vertical segments or none at all.
						return null;
					}

					continue;
				}

				return previousSegment;
			}

			return null;
		}

		public void ReplacePoint(int pointIndex, Pnt3D newPoint)
		{
			Line3D segment1 = null;
			Line3D segment2 = null;

			if (pointIndex == SegmentCount && IsClosed)
			{
				// Last point index in closed ring
				pointIndex = 0;
			}

			// Update EndPoint of previous segment and StartPoint of this index' segment
			int? previousIdx = PreviousSegmentIndex(pointIndex);

			if (previousIdx != null)
			{
				segment1 = PreviousSegmentInRing(pointIndex);
			}

			if (pointIndex < SegmentCount)
			{
				segment2 = _segments[pointIndex];
			}

			segment1?.SetEndPoint(newPoint);
			segment2?.SetStartPoint(newPoint);

			// TODO: Separate method 'CoordinatesUpdated(bool zOnly)' similar to Line3D
			// TODO: Set spatial index to null, consider making bounds lazy, deal with spatial index on Multilinestring
			// TODO: To avoid grow-only bounds changes, make sure the replaced point was not an extreme point
			//       ... if replacedPoint.X == XMax -> re-create envelope
			UpdateBounds(newPoint, pointIndex, Segments, null);

			_isKnownClosed = null;
			_knownArea = null;
		}

		public void UpdatePoint(int pointIndex, double x, double y, double z)
		{
			var newPoint = new Pnt3D(x, y, z);

			// Do not update the existing point to avoid (even temporary) gaps between segments (IsClosed is called everywhere all the time...)
			ReplacePoint(pointIndex, newPoint);
		}

		public void SetConstantZ(int startVertexIndex, int endVertexIndex, double z)
		{
			for (int i = startVertexIndex; i <= endVertexIndex; i++)
			{
				Pnt3D point = GetPoint3D(i);

				UpdatePoint(i, point.X, point.Y, z);
			}
		}

		public void AssignUndefinedZs(Plane3D fromPlane)
		{
			for (int i = 0; i < PointCount; i++)
			{
				Pnt3D point = GetPoint3D(i);

				if (double.IsNaN(point.Z))
				{
					point.Z = fromPlane.GetZ(point.X, point.Y);
					ReplacePoint(i, point);
				}
			}
		}

		public bool TryInterpolateUndefinedZs()
		{
			IDictionary<int, int> nanZSequences = GetNanZSequences(this);

			bool result = true;

			foreach (var sequence in nanZSequences)
			{
				int firstNan = sequence.Key;
				int lastNan = sequence.Value;

				result &= TryInterpolateZ(firstNan, lastNan);
			}

			return result;
		}

		public void ReverseOrientation()
		{
			foreach (Line3D segment in _segments)
			{
				segment.ReverseOrientation();
			}

			_segments.Reverse();

			if (RightMostBottomIndex != 0)
				RightMostBottomIndex = SegmentCount - RightMostBottomIndex;

			_knownArea = null;
		}

		/// <summary>
		/// Returns the subcurve consisting of the points between the specified locations along the
		/// linestring.
		/// </summary>
		/// <param name="fromSegment"></param>
		/// <param name="fromSegmentRatio"></param>
		/// <param name="toSegment"></param>
		/// <param name="toSegmentRatio"></param>
		/// <param name="clonePoints"></param>
		/// <param name="againstRingOrientation">If true, the resulting path will travel against
		/// the segment orientation of this linestring. If the </param>
		/// <param name="preferFullRingToZeroLength">In case the from-point equals the to-point:
		/// Whether a the full ring should be returned or a zero-length segment.</param>
		/// <returns></returns>
		public Linestring GetSubcurve(int fromSegment, double fromSegmentRatio,
		                              int toSegment, double toSegmentRatio,
		                              bool clonePoints,
		                              bool againstRingOrientation,
		                              bool preferFullRingToZeroLength)
		{
			Assert.ArgumentCondition(fromSegmentRatio >= 0 && fromSegmentRatio <= 1.0,
			                         "fromSegmentDistance must be >= 0 and <= 1");
			Assert.ArgumentCondition(toSegmentRatio >= 0 && toSegmentRatio <= 1.0,
			                         "toSegmentDistance must be >= 0 and <= 1");

			if (againstRingOrientation)
			{
				int segTemp = fromSegment;
				fromSegment = toSegment;
				toSegment = segTemp;

				double ratioTemp = fromSegmentRatio;
				fromSegmentRatio = toSegmentRatio;
				toSegmentRatio = ratioTemp;
			}

			var result = GetSubcurve(fromSegment, fromSegmentRatio,
			                         toSegment, toSegmentRatio,
			                         clonePoints, preferFullRingToZeroLength);

			if (againstRingOrientation)
			{
				result.ReverseOrientation();
			}

			return new Linestring(result);
		}

		/// <summary>
		/// Returns the subcurve consisting of the points between the specified locations along the
		/// linestring. The from location must be before the end location, unless the linestring is
		/// closed in which case the new subcurve will cross the original's from/to point.
		/// </summary>
		/// <param name="fromSegment"></param>
		/// <param name="fromSegmentRatio"></param>
		/// <param name="toSegment"></param>
		/// <param name="toSegmentRatio"></param>
		/// <param name="clonePoints"></param>
		/// <returns></returns>
		public Linestring GetSubcurve(int fromSegment, double fromSegmentRatio,
		                              int toSegment, double toSegmentRatio,
		                              bool clonePoints,
		                              bool preferFullRingToZeroLength)
		{
			Line3D startSegment = _segments[fromSegment];

			Pnt3D startPoint = fromSegmentRatio > 0
				                   ? startSegment.GetPointAlong(
					                   fromSegmentRatio, asRatio: true)
				                   : clonePoints
					                   ? startSegment.StartPointCopy
					                   : startSegment.StartPoint;

			Line3D endSegment = _segments[toSegment];
			Pnt3D endPoint = toSegmentRatio > 0
				                 ? endSegment.GetPointAlong(
					                 toSegmentRatio, asRatio: true)
				                 : clonePoints
					                 ? endSegment.StartPointCopy
					                 : endSegment.StartPoint;

			var resultPoints = new List<Pnt3D>();

			resultPoints.Add(startPoint);

			double epsilon =
				MathUtils.GetDoubleSignificanceEpsilon(startPoint.X, startPoint.Y);

			if (preferFullRingToZeroLength)
			{
				epsilon = -epsilon;
			}

			bool fromBeforeTo = fromSegmentRatio - toSegmentRatio < epsilon;

			if (fromSegment == toSegment && fromBeforeTo)
			{
				resultPoints.Add(endPoint);
			}
			else
			{
				int currentIndex = fromSegment;

				if (toSegment <= fromSegment)
				{
					// must go across the From/To point
					Assert.True(IsClosed,
					            "Cannot get subcurve from non-closed linestring with from point after to point.");
				}

				while ((currentIndex = NextIndexInRing(currentIndex)) != toSegment)
				{
					if (fromSegmentRatio < 1)
					{
						resultPoints.Add(_segments[currentIndex].StartPoint);
					}
				}

				if (toSegmentRatio > 0)
				{
					resultPoints.Add(endSegment.StartPoint);
				}

				resultPoints.Add(endPoint);
			}

			return new Linestring(resultPoints);
		}

		/// <summary>
		/// Returns the subcurve consisting of the points between the specified start location
		/// along the linestring and the end. If this linestring is closed and
		/// <paramref name="fullRing"/> is true, the resulting linestring will be a closed
		/// linestring that crosses the null point and ends at the start location.
		/// </summary>
		/// <param name="startSegmentIdx"></param>
		/// <param name="startRatioAlong"></param>
		/// <param name="clonePoints"></param>
		/// <param name="fullRing"></param>
		/// <returns></returns>
		public Linestring GetSubcurve(int startSegmentIdx, double startRatioAlong,
		                              bool clonePoints,
		                              bool fullRing = false)
		{
			if (startSegmentIdx < SegmentCount - 1 || startRatioAlong < 1)
			{
				Linestring toEndSubcurve =
					GetSubcurve(startSegmentIdx, startRatioAlong, SegmentCount - 1, 1,
					            clonePoints, false);

				if (IsClosed && fullRing)
				{
					double epsilon = MathUtils.GetDoubleSignificanceEpsilon(XMax, YMax);

					Linestring fromStartSubcurve =
						GetSubcurve(0, 0, startSegmentIdx, startRatioAlong,
						            clonePoints, false);

					return GeomTopoOpUtils.MergeConnectedLinestrings(
						new List<Linestring> {toEndSubcurve, fromStartSubcurve}, null, epsilon);
				}

				return toEndSubcurve;
			}

			// The specified start is at the end point
			if (IsClosed)
			{
				return new Linestring(GetPoints(0, null, clonePoints));
			}

			throw new ArgumentOutOfRangeException(nameof(startSegmentIdx),
			                                      "Start must be before the last point for un-closed linestring.");
		}

		public double GetArea2D()
		{
			if (! IsClosed)
			{
				throw new InvalidOperationException(
					"Area can only be calculated for closed linestrings.");
			}

			if (_knownArea == null)
			{
				_knownArea = GeomUtils.GetArea2D(GetPoints().ToList());
			}

			return GeomUtils.GetArea2D(GetPoints().ToList());
		}

		public double GetLength2D()
		{
			return _segments.Sum(s => s.Length2D);
		}

		public IEnumerable<Line3D> GetSegmentsBetween(int startPointIndex,
		                                              int endPointIndex)
		{
			// Get the segment that starts at start point:
			int? currentSegmentIdx =
				startPointIndex == PointCount && IsClosed ? 0 : startPointIndex;

			// ... and the segment index after the end point
			int? stopSegmentIdx = endPointIndex == SegmentCount
				                      ? IsClosed ? 0 : (int?) null
				                      : endPointIndex;

			do
			{
				yield return this[Assert.NotNull(currentSegmentIdx).Value];

				currentSegmentIdx = NextSegmentIndex(currentSegmentIdx.Value);
			} while (currentSegmentIdx != stopSegmentIdx);
		}

		private List<Line3D> FromPoints(IEnumerable<Pnt3D> points,
		                                bool updateBounds = true)
		{
			var result = new List<Line3D>();

			Pnt3D p1 = null;

			var idx = 0;
			foreach (Pnt3D point in points)
			{
				if (updateBounds)
					UpdateBounds(point, idx, result, p1);

				if (p1 != null)
				{
					result.Add(new Line3D(p1, point));
				}

				p1 = point;
				idx++;
			}

			return result;
		}

		private void UpdateBounds([NotNull] IPnt point,
		                          int pointIndex,
		                          [NotNull] IList<Line3D> currentSegments,
		                          [CanBeNull] Pnt3D previouslyAdded)
		{
			if (point.X < XMin)
			{
				XMin = point.X;
			}

			if (point.X > XMax)
			{
				XMax = point.X;
			}

			if (point.Y < YMin)
			{
				YMin = point.Y;
				RightMostBottomIndex = pointIndex;
			}
			else if (! (point.Y > YMin))
			{
				Pnt3D currentRightMostLowestPnt;
				if (currentSegments.Count == RightMostBottomIndex)
				{
					if (previouslyAdded != null)
					{
						currentRightMostLowestPnt = previouslyAdded;
					}
					else
					{
						currentRightMostLowestPnt =
							currentSegments[currentSegments.Count - 1].EndPoint;
					}
				}
				else
				{
					currentRightMostLowestPnt =
						currentSegments[RightMostBottomIndex].StartPoint;
				}

				if (point.X > currentRightMostLowestPnt.X)
				{
					// equally low, but more to the right
					RightMostBottomIndex = pointIndex;
				}
			}

			if (point.Y > YMax)
			{
				YMax = point.Y;
			}

			_boundingBox = null;
		}

		private void InitializeBounds()
		{
			XMin = double.MaxValue;
			YMin = double.MaxValue;

			XMax = double.MinValue;
			YMax = double.MinValue;

			RightMostBottomIndex = -1;

			_boundingBox = null;
			_isKnownClosed = null;
			_knownArea = null;
		}

		public void TryOrientClockwise()
		{
			if (ClockwiseOriented == false)
			{
				ReverseOrientation();
			}
		}

		public void TryOrientAnticlockwise()
		{
			if (ClockwiseOriented == true)
			{
				ReverseOrientation();
			}
		}

		/// <summary>
		/// Interpolates the linestring's Z values in the range (firstPointIndex..lastPointIndex)
		/// using the Z values of the adjacent vertices.
		/// </summary>
		/// <param name="firstPointIndex"></param>
		/// <param name="lastPointIndex"></param>
		/// <param name="extrapolateDanglesHorizontally"></param>
		/// <returns></returns>
		private bool TryInterpolateZ(int firstPointIndex,
		                             int lastPointIndex,
		                             bool extrapolateDanglesHorizontally = true)
		{
			int? beforeIndex = PreviousSegmentIndex(firstPointIndex);

			if (beforeIndex == null && ! extrapolateDanglesHorizontally)
			{
				return false;
			}

			int? afterIndex = NextVertexIndex(lastPointIndex);

			if (afterIndex == null && ! extrapolateDanglesHorizontally)
			{
				return false;
			}

			if (beforeIndex == null && afterIndex == null)
			{
				return false;
			}

			if (beforeIndex == null)
			{
				// first index == 0, use z value of afterIndex
				double z = GetPoint3D(afterIndex.Value).Z;

				SetConstantZ(0, lastPointIndex, z);
				return true;
			}

			if (afterIndex == null)
			{
				double z = GetPoint3D(beforeIndex.Value).Z;
				int lastVertexIndex = SegmentCount;
				SetConstantZ(firstPointIndex, lastVertexIndex, z);
				return true;
			}

			return TryInterpolateZBetween(beforeIndex.Value, afterIndex.Value);
		}

		private bool TryInterpolateZBetween(int beforeIndex,
		                                    int afterIndex)
		{
			if (beforeIndex > afterIndex && ! IsClosed)
			{
				throw new ArgumentException(
					"The firstIndex is greater than lastIndex and linestring is not closed. Cannot interpolate across start/end.");
			}

			double z1 = GetPoint3D(beforeIndex).Z;
			double z2 = GetPoint3D(afterIndex).Z;

			double sequenceLength =
				GetSegmentsBetween(beforeIndex, afterIndex)
					.Sum(s => s.Length2D);

			double zFactor = (z2 - z1) / sequenceLength;

			if (double.IsNaN(zFactor))
			{
				return false;
			}

			// Now update each segment's end point
			double currentLength = 0;

			int lastUpdatePointIdx =
				Assert.NotNull(PreviousVertexIndex(afterIndex)).Value;

			int currentIndex = beforeIndex;
			do
			{
				Line3D segment = this[currentIndex];

				currentLength += segment.Length2D;

				double newZ = z1 + zFactor * currentLength;

				Pnt3D previousPoint = segment.EndPoint;
				UpdatePoint(currentIndex + 1, previousPoint.X, previousPoint.Y,
				            newZ);

				currentIndex = Assert.NotNull(NextSegmentIndex(currentIndex)).Value;
			} while (currentIndex != lastUpdatePointIdx);

			return true;
		}

		private static IDictionary<int, int> GetNanZSequences(Linestring linestring)
		{
			var result = new Dictionary<int, int>();

			int index = 0;

			int? firstNanIndex = null;
			foreach (Pnt3D vertex in linestring.GetPoints())
			{
				if (double.IsNaN(vertex.Z))
				{
					if (firstNanIndex == null)
					{
						firstNanIndex = index;
					}
				}
				else if (firstNanIndex != null)
				{
					result.Add(firstNanIndex.Value, index - 1);
					firstNanIndex = null;
				}

				index++;
			}

			if (firstNanIndex != null)
			{
				// last point is also NaN. Connect to first stretch if linestring also started with NaN
				if (linestring.IsClosed &&
				    result.ContainsKey(0) &&
				    result.Count > 0)
				{
					var last = result[0];
					result.Remove(0);
					result.Add(firstNanIndex.Value, last);
				}
				else
				{
					result.Add(firstNanIndex.Value, linestring.SegmentCount);
				}
			}

			return result;
		}
	}
}
