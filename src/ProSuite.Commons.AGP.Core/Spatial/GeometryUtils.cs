using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;
using esriGeometryType = ArcGIS.Core.CIM.esriGeometryType;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	public static class GeometryUtils
	{
		public static Coordinate2D Shifted(this Coordinate2D point, double dx, double dy)
		{
			return new Coordinate2D(point.X + dx, point.Y + dy);
		}

		public static MapPoint Shifted(this MapPoint point, double dx, double dy)
		{
			var builder = new MapPointBuilderEx(point);
			builder.X += dx;
			builder.Y += dy;
			return builder.ToGeometry();
		}

		public static T Shifted<T>(this T segment, double dx, double dy) where T : Segment
		{
			if (segment is LineSegment line)
			{
				var movedStart = line.StartCoordinate.Shifted(dx, dy);
				var movedEnd = line.EndCoordinate.Shifted(dx, dy);
				var moved = LineBuilderEx.CreateLineSegment(movedStart, movedEnd);
				return (T) (Segment) moved;
			}

			if (segment is EllipticArcSegment arc)
			{
				throw new NotImplementedException($"{arc.GetType().Name} is not yet implemented");
			}

			if (segment is CubicBezierSegment cubic)
			{
				var builder = new CubicBezierBuilderEx(cubic);
				builder.StartPoint = builder.StartPoint.Shifted(dx, dy);
				builder.ControlPoint1 = builder.ControlPoint1.Shifted(dx, dy);
				builder.ControlPoint2 = builder.ControlPoint2.Shifted(dx, dy);
				builder.EndPoint = builder.EndPoint.Shifted(dx, dy);
				return (T) (Segment) builder.ToSegment();
			}

			throw new NotSupportedException($"Unknown segment type: {segment.GetType().FullName}");
		}

		public static double GetXyTolerance(Geometry geometry)
		{
			return geometry?.SpatialReference?.XYTolerance ?? double.NaN;
		}

		/// <returns>The total number of points (vertices) across all parts
		/// of the given <paramref name="geometry"/>; 0 if empty or null</returns>
		public static int GetPointCount([CanBeNull] Geometry geometry)
		{
			return geometry?.PointCount ?? 0;
		}

		/// <returns>The number of points (vertices) in the indicated part
		/// or 0 if the given geometry is null or empty (and always
		/// 1 for points and multipoints)</returns>
		/// <remarks>An error occurs if the given part index is negative
		/// or beyond the number of parts in the given geometry.</remarks>
		public static int GetPointCount([CanBeNull] Geometry geometry, int partIndex)
		{
			if (geometry is null) return 0;

			if (partIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(partIndex));

			if (geometry is Multipart multipart)
			{
				if (partIndex >= multipart.PartCount)
					throw new ArgumentOutOfRangeException(nameof(partIndex));
				var part = multipart.Parts[partIndex];
				return part.Count + 1; // part.Count is segments, +1 gives vertices
			}

			if (geometry is Multipatch multipatch)
			{
				if (partIndex >= multipatch.PartCount)
					throw new ArgumentOutOfRangeException(nameof(partIndex));
				return multipatch.GetPatchPointCount(partIndex);
			}

			if (geometry is Multipoint multipoint)
			{
				if (partIndex >= multipoint.PointCount)
					throw new ArgumentOutOfRangeException(nameof(partIndex));
				return 1;
			}

			if (geometry is MapPoint)
			{
				if (partIndex != 0)
					throw new ArgumentOutOfRangeException(nameof(partIndex));
				return 1;
			}

			throw new NotSupportedException($"Geometry of type {geometry.GetType().Name} is not supported");
		}

		/// <summary>
		/// Get the part of the given <paramref name="geometry"/> at the
		/// given <paramref name="partIndex"/>. Throw an exception if the
		/// <paramref name="partIndex"/> is out of range for the geometry.
		/// </summary>
		/// <returns>High-level geometry: a ring of a Polygon is returned
		/// as a single-part Polygon, and similarly for Polylines</returns>
		public static Geometry GetPart(Geometry geometry, int partIndex)
		{
			if (geometry is null) return null;
			if (partIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(partIndex), partIndex,
				                                      "Must not be negative");

			if (geometry is MapPoint mapPoint)
			{
				if (partIndex != 0)
					throw new ArgumentOutOfRangeException(nameof(partIndex), partIndex,
					                                      "A point only has part 0");
				return mapPoint;
			}

			if (geometry is Multipoint multipoint)
			{
				if (partIndex >= multipoint.PointCount)
				{
					throw new ArgumentOutOfRangeException(nameof(partIndex), partIndex,
					                                      "Part index beyond number of parts");
				}

				return multipoint.Points[partIndex];
			}

			if (geometry is Multipart multipart)
			{
				if (partIndex >= multipart.PartCount)
				{
					throw new ArgumentOutOfRangeException(nameof(partIndex), partIndex,
					                                      "Part index beyond number of parts");
				}

				var path = multipart.Parts[partIndex];
				var flags = multipart.GetAttributeFlags();
				var sref = multipart.SpatialReference;
				return geometry is Polyline
					       ? PolylineBuilderEx.CreatePolyline(path, flags, sref)
					       : PolygonBuilderEx.CreatePolygon(path, flags, sref);
			}

			throw new NotSupportedException(
				$"Geometry of type {geometry.GetType().Name} is not supported");
		}

		/// <returns>The number of parts in the given geometry, or zero
		/// if it is null or empty</returns>
		/// <remarks>Each point of a multipoint is considered a part</remarks>
		public static int GetPartCount([CanBeNull] Geometry geometry)
		{
			if (geometry is null) return 0;
			if (geometry is Multipart multipart) return multipart.PartCount;
			if (geometry is Multipatch multipatch) return multipatch.PartCount;
			// Convention: a multipoint's ith point is part i and point i (this
			// is consistent with, e.g., info=NearestVertex(multipoint, hitPoint),
			// which here would report info.PartIndex == info.PointIndex
			if (geometry is Multipoint multipoint) return multipoint.PointCount;
			return 1;
		}

		public static MapPoint GetStartPoint([CanBeNull] Multipart multipart)
		{
			var points = multipart?.Points;
			if (points is null) return null;
			int count = points.Count;
			return count > 0 ? points[0] : null;
		}

		public static MapPoint GetStartPoint(ReadOnlySegmentCollection segments)
		{
			if (segments is null) return null;
			if (segments.Count < 1) return null;
			var first = segments[0];
			return first.StartPoint;
		}

		[CanBeNull]
		public static MapPoint GetEndPoint([CanBeNull] Multipart multipart)
		{
			var points = multipart?.Points;
			if (points is null) return null;
			int count = points.Count;
			return count > 0 ? points[count - 1] : null;
		}

		public static MapPoint GetEndPoint(ReadOnlySegmentCollection segments)
		{
			if (segments is null) return null;
			if (segments.Count < 1) return null;
			var last = segments[segments.Count - 1];
			return last.EndPoint;
		}

		public static MapPoint GetLabelPoint([CanBeNull] Geometry geometry)
		{
			// Note: GeometryEngine's LabelPoint is only implemented for Polygon and Envelope

			if (geometry is null) return null;

			if (geometry is MapPoint point)
			{
				return point;
			}

			if (geometry is Polyline polyline)
			{
				var segmentExtension = SegmentExtensionType.NoExtension;

				return Engine.QueryPoint(polyline, segmentExtension,
				                         0.5, AsRatioOrLength.AsRatio);
			}

			return Engine.LabelPoint(geometry);
		}

		public static MapPoint GetLowerLeft([CanBeNull] Envelope envelope)
		{
			// AO had .LowerLeft and similar properties on IEnvelope

			if (envelope is null) return null;

			double x = envelope.XMin;
			double y = envelope.YMin;

			bool hasZ = envelope.HasZ;
			double z = hasZ ? envelope.ZMin : 0.0;

			bool hasM = envelope.HasM;
			double m = hasM ? envelope.MMin : double.NaN;

			bool hasId = envelope.HasID;
			int id = hasId ? envelope.IDMin : 0;

			var sref = envelope.SpatialReference;

			return MapPointBuilderEx.CreateMapPoint(x, y, hasZ, z, hasM, m, hasId, id, sref);
		}

		[NotNull]
		public static MapPoint GetUpperRight([NotNull] Envelope envelope)
		{
			double x = envelope.XMax;
			double y = envelope.YMax;

			bool hasZ = envelope.HasZ;
			double z = hasZ ? envelope.ZMin : 0.0;

			bool hasM = envelope.HasM;
			double m = hasM ? envelope.MMin : double.NaN;

			bool hasId = envelope.HasID;
			int id = hasId ? envelope.IDMin : 0;

			var sref = envelope.SpatialReference;

			return MapPointBuilderEx.CreateMapPoint(x, y, hasZ, z, hasM, m, hasId, id, sref);
		}

		[NotNull]
		public static MapPoint GetLowerRight([NotNull] Envelope envelope)
		{
			double x = envelope.XMax;
			double y = envelope.YMin;

			bool hasZ = envelope.HasZ;
			double z = hasZ ? envelope.ZMin : 0.0;

			bool hasM = envelope.HasM;
			double m = hasM ? envelope.MMin : double.NaN;

			bool hasId = envelope.HasID;
			int id = hasId ? envelope.IDMin : 0;

			var sref = envelope.SpatialReference;

			return MapPointBuilderEx.CreateMapPoint(x, y, hasZ, z, hasM, m, hasId, id, sref);
		}

		public static double GetArea([CanBeNull] Geometry geometry)
		{
			if (geometry is Polygon polygon) return polygon.Area;
			if (geometry is Envelope envelope) return envelope.Area;
			return 0.0;
		}

		public static double GetLength([CanBeNull] Geometry geometry)
		{
			if (geometry is Multipart multipart) return multipart.Length;
			if (geometry is Envelope envelope) return envelope.Length;
			return 0.0;
		}

		public static double GetLength([CanBeNull] Multipart multipart, int partIndex)
		{
			if (multipart is null) return 0.0;
			if (multipart.IsEmpty) return 0.0;
			if (partIndex < 0 || partIndex >= multipart.PartCount)
				throw new ArgumentOutOfRangeException(nameof(partIndex), partIndex, "no such part");
			var path = multipart.Parts[partIndex];
			return path.Sum(seg => seg.Length);
		}

		public static AttributeFlags GetAttributeFlags(this Geometry geometry)
		{
			var flags = AttributeFlags.None;

			if (geometry != null)
			{
				if (geometry.HasZ)
					flags |= AttributeFlags.HasZ;
				if (geometry.HasM)
					flags |= AttributeFlags.HasM;
				if (geometry.HasID)
					flags |= AttributeFlags.HasID;
			}

			return flags;
		}

		public static Envelope Union([CanBeNull] Envelope a, [CanBeNull] Envelope b)
		{
			if (a is null || a.IsEmpty) return b;
			if (b is null || b.IsEmpty) return a;
			return a.Union(b);
		}

		public static Geometry Union<T>(ICollection<T> geometries) where T : Geometry
		{
			if (geometries is null) return null;
			int count = geometries.Count;
			if (count < 1) return null;

			if (count == 1)
			{
				return geometries.Single();
			}
			//Fails in ArcGIS 3.0 when merging a polygon that is congruent with the island of the first polygon. See issue #168
			//The list overload: Engine.Union(geometries) works
			//if (count == 2)
			//{
			//	using (var enumerator = geometries.GetEnumerator())
			//	{
			//		enumerator.MoveNext();
			//		var one = enumerator.Current;
			//		enumerator.MoveNext();
			//		var two = enumerator.Current;
			//		return Engine.Union(one, two);
			//	}
			//}

			return Engine.Union(geometries);
		}

		public static Polyline Boundary(Polygon polygon)
		{
			if (polygon == null) return null;

			var boundary = Engine.Boundary(polygon);
			if (boundary is Polyline polyline) return polyline;
			throw UnexpectedResultFrom("GeometryEngine.Boundary()", typeof(Polyline), boundary);
		}

		public static Polygon Intersection(Envelope extent, Polygon perimeter)
		{
			if (extent == null) return perimeter;
			if (perimeter == null) return GeometryFactory.CreatePolygon(extent);
			return GetClippedPolygon(perimeter, extent);
		}

		public static Geometry Intersection(
			[CanBeNull] Geometry a, [CanBeNull] Geometry b)
		{
			if (a is null) return null;
			if (b is null) return null;
			return Engine.Intersection(a, b);
		}

		public static Geometry Difference(Geometry minuend, Geometry subtrahend)
		{
			Geometry difference = Engine.Difference(minuend, subtrahend);
			// Note: difference may have another geometry type than minuend
			return difference;
		}

		public static Geometry Buffer(Geometry geometry, double distance)
		{
			if (geometry is null) return null;
			if (geometry is Envelope extent)
			{
				// Note: GeometryEngine's Buffer() does not support Envelope
				return Buffer(extent, distance);
			}

			var buffer = Engine.Buffer(geometry, distance);
			// Note: buffer may NOT be a Polygon if distance is almost zero!
			return buffer;
		}

		public static Envelope Buffer(Envelope envelope, double distance)
		{
			if (envelope is null) return null;
			if (envelope.IsEmpty) return envelope;
			return envelope.Expand(distance, distance, false);
		}

		public static Geometry ConvexHull(Geometry geometry)
		{
			if (geometry is null) return null;
			if (geometry.IsEmpty) return geometry;
			if (geometry is Envelope) return geometry;
			return Engine.ConvexHull(geometry);
		}

		public static T Move<T>(T geometry, double dx, double dy) where T : Geometry
		{
			if (geometry is null) return null;
			var moved = Engine.Move(geometry, dx, dy);
			if (moved is T result) return result;
			throw UnexpectedResultFrom("GeometryEngine.Move()", typeof(T), moved);
		}

		public static T Rotate<T>(T geometry, MapPoint origin, double angleRadians)
			where T : Geometry
		{
			if (geometry is null) return null;
			if (Math.Abs(angleRadians) < double.Epsilon) return geometry;
			if (origin is null) throw new ArgumentNullException(nameof(origin));
			var rotated = Engine.Rotate(geometry, origin, angleRadians);
			if (rotated is T result) return result;
			throw UnexpectedResultFrom(nameof(Engine.Rotate), typeof(T), rotated);
		}

		public static T Scale<T>(T geometry, MapPoint origin, double sx, double sy)
			where T : Geometry
		{
			if (geometry is null) return null;
			if (origin is null) throw new ArgumentNullException(nameof(origin));
			var scaled = Engine.Scale(geometry, origin, sx, sy);
			if (scaled is T result) return result;
			throw UnexpectedResultFrom(nameof(Engine.Scale), typeof(T), scaled);
		}

		public static T Generalize<T>(T geometry, double maxDeviation,
		                              bool removeDegenerateParts = false,
		                              bool preserveCurves = false)
			where T : Geometry
		{
			if (geometry is null) return null;
			if (geometry is MapPoint) return geometry;
			if (geometry is Multipoint) return geometry;
			if (geometry is Envelope) return geometry;

			if (maxDeviation < double.Epsilon)
			{
				return geometry;
			}

			var generalized =
				Engine.Generalize(geometry, maxDeviation, removeDegenerateParts, preserveCurves);
			if (generalized is T result) return result;
			throw UnexpectedResultFrom("GeometryEngine.Generalize()", typeof(T), generalized);
		}

		public static Polyline Simplify(Polyline polyline, SimplifyType simplifyType,
		                                bool forceSimplify = false)
		{
			if (polyline == null) return null;

			return Engine.SimplifyPolyline(polyline, simplifyType, forceSimplify);
		}

		public static T Simplify<T>(T geometry, bool forceSimplify = false)
			where T : Geometry
		{
			if (geometry == null) return null;
			var simplified = Engine.SimplifyAsFeature(geometry, forceSimplify);
			if (simplified is T result) return result;
			throw UnexpectedResultFrom("GeometryEngine.Simplify()", typeof(T), simplified);
		}

		public static T SimplifyZ<T>(T geometry, double defaultZ = 0d) where T : Geometry
		{
			// TODO: Unittests for SimplifyZ
			if (geometry == null) return null;

			if (! geometry.HasZ)
			{
				// TODO: DropZs?! (Engine.DropMs exists, what about DropZs?)
				return geometry;
			}

			if (geometry is MapPoint mapPoint)
			{
				return SimplifyZ<T>(mapPoint, defaultZ);
			}

			if (geometry is Multipart multipart)
			{
				return (T) (Geometry) Engine.CalculateNonSimpleZs(multipart, defaultZ);
			}

			if (geometry is Multipoint multipoint)
			{
				var mapPoints = new List<MapPoint>();

				foreach (MapPoint point in multipoint.Points)
				{
					MapPoint simplePoint = SimplifyZ<MapPoint>(point, defaultZ);
					mapPoints.Add(simplePoint);
				}

				return (T) (Geometry) MultipointBuilderEx.CreateMultipoint(
					mapPoints, multipoint.GetAttributeFlags());
			}

			throw new NotImplementedException("The provided geometry type is not yet supported");
		}

		public static T SimplifyZ<T>(MapPoint mapPoint, double defaultZ = 0d) where T : Geometry
		{
			if (double.IsNaN(mapPoint.Z))
			{
				MapPointBuilder pointBuilder = new MapPointBuilder(mapPoint);
				pointBuilder.Z = defaultZ;

				return (T) (Geometry) pointBuilder.ToGeometry();
			}

			return (T) (Geometry) mapPoint;
		}

		/// <summary>
		/// Return a polygon that consists of all exterior rings
		/// of the given <paramref name="polygon"/> that are not
		/// contained within another exterior ring. All interior
		/// rings (holes) are discarded.
		/// </summary>
		public static Polygon RemoveHoles(Polygon polygon)
		{
			// Simple cases: empty or just one ring:

			if (polygon is null || polygon.IsEmpty || polygon.PartCount <= 1)
			{
				return polygon;
			}

			// Let result = new List of rings
			// For each part (i.e., ring):
			//   - discard if interior (negative area)
			//   - if exterior (positive area):
			//      - if contained in any of result: discard
			//      - if contains any of result: add to result, discard previous result
			//      - else: add to result list

			var result = new List<Polygon>();

			var flags = polygon.GetAttributeFlags();
			var sref = polygon.SpatialReference;

			int partCount = polygon.Parts.Count;
			for (int i = 0; i < partCount; i++)
			{
				var segments = polygon.Parts[i];
				var ring = PolygonBuilderEx.CreatePolygon(segments, flags, sref);

				if (ring.Area > 0) // exterior ring
				{
					bool handled = false;

					for (int j = 0; j < result.Count; j++)
					{
						if (Contains(result[j], ring))
						{
							handled = true;
							break;
						}

						if (Contains(ring, result[j]))
						{
							result[j] = ring;
							handled = true;
							break;
						}
					}

					if (! handled)
					{
						result.Add(ring);
					}
				}
				// else: discard interior ring or degenerate part
			}

			return PolygonBuilderEx.CreatePolygon(result, flags, sref);
		}

		/// <summary>
		/// Get the connected components, that is, the collection
		/// of single polygons that make up the given (multi) polygon.
		/// </summary>
		/// <remarks>
		/// Time is O(N**2) where N is the number of rings; any better ideas around?
		/// </remarks>
		public static IList<Polygon> ConnectedComponents(Polygon polygon)
		{
			if (polygon is null || polygon.IsEmpty)
			{
				return Array.Empty<Polygon>();
			}

			// Algorithm: (1) separate all rings into exterior and interior
			// rings; (2) for each exterior ring, build a polygon consisting
			// of this exterior ring and all its holes, i.e., interior rings
			// contained in this exterior ring.

			var flags = polygon.GetAttributeFlags();
			var sref = polygon.SpatialReference;

			var shells = new List<Polygon>();
			var holes = new List<Polygon>();

			int partCount = polygon.Parts.Count;
			for (int i = 0; i < partCount; i++)
			{
				var segments = polygon.Parts[i];
				var part = PolygonBuilderEx.CreatePolygon(segments, flags, sref);

				if (part.Area > 0)
				{
					shells.Add(part);
				}
				else if (part.Area < 0)
				{
					holes.Add(part);
				}
				// else: skip degenerate part
			}

			// Note: ExteriorRingCount is expensive (the first time called)
			//Assert.AreEqual(polygon.ExteriorRingCount, shells.Count, "Oops");

			var parts = new List<Polygon>();

			for (var i = 0; i < shells.Count; i++)
			{
				parts.Clear();
				parts.Add(shells[i]);

				int j = 0;
				int holeCount = holes.Count;
				while (j < holeCount)
				{
					if (Contains(shells[i], holes[j]))
					{
						parts.Add(holes[j]);
						holes[j] = holes[--holeCount];
					}
					else
					{
						j++;
					}
				}

				if (parts.Count > 1)
				{
					holes.RemoveRange(holeCount, holes.Count - holeCount);

					shells[i] = PolygonBuilderEx.CreatePolygon(parts, flags, sref);
				}
			}

			return shells;
		}

		/// <summary>
		/// Reverse the orientation of the given part <paramref name="partIndex"/>
		/// (or all parts if <paramref name="partIndex"/> is negative) of the given
		/// <paramref name="polycurve"/> (Polyline or Polygon). Notice that reversing
		/// a polygon ring changes its meaning from interior to exterior or vice versa!
		/// </summary>
		public static T ReverseOrientation<T>(T polycurve, int partIndex = -1) where T : Multipart
		{
			if (polycurve is null)
				return null;

			if (partIndex < 0)
			{
				return (T) GeometryEngine.Instance.ReverseOrientation(polycurve);
			}

			Multipart result;

			switch (polycurve)
			{
				case Polyline polyline:
					result = ReverseOrientation(polyline, partIndex);
					break;

				case Polygon polygon:
					result = ReverseOrientation(polygon, partIndex);
					break;

				default:
					throw new NotSupportedException(
						$"Unknown {nameof(Multipart)} subtype: {polycurve.GetType().Name}");
			}

			return (T) result;
		}

		private static Polyline ReverseOrientation(Polyline polyline, int partIndex = -1)
		{
			if (polyline is null)
				throw new ArgumentNullException(nameof(polyline));
			if (partIndex < 0 || partIndex >= polyline.PartCount)
				throw new ArgumentOutOfRangeException(nameof(partIndex));
			var builder = new PolylineBuilderEx(polyline);
			ReverseOrientation(builder.Parts[partIndex]);
			return builder.ToGeometry();
		}

		private static Polygon ReverseOrientation(Polygon polygon, int partIndex = -1)
		{
			if (polygon is null)
				throw new ArgumentNullException(nameof(polygon));
			if (partIndex < 0 || partIndex > polygon.PartCount)
				throw new ArgumentOutOfRangeException(nameof(partIndex));
			var builder = new PolygonBuilderEx(polygon);
			ReverseOrientation(builder.Parts[partIndex]);
			return builder.ToGeometry();
		}

		private static void ReverseOrientation(List<Segment> path)
		{
			// Reverse the list, then each segment in the list:

			path.Reverse();

			int count = path.Count;
			for (int i = 0; i < count; i++)
			{
				var segment = path[i];

				switch (segment)
				{
					case LineSegment line:
						path[i] = LineBuilderEx.CreateLineSegment(line.EndPoint, line.StartPoint);
						break;

					case CubicBezierSegment bezier:
						path[i] = CubicBezierBuilderEx.CreateCubicBezierSegment(
							bezier.EndPoint, bezier.ControlPoint2, bezier.ControlPoint1, bezier.StartPoint);
						break;

					case EllipticArcSegment arc:
						var builder = new EllipticArcBuilderEx(arc);
						builder.Orientation = arc.IsCounterClockwise
							                      ? ArcOrientation.ArcClockwise
							                      : ArcOrientation.ArcCounterClockwise;
						path[i] = builder.ToSegment();
						break;
				}
			}
		}

		/// <summary>
		/// Return a copy of the input geometry with index structures
		/// added that may accelerate the relational operations.
		/// </summary>
		public static T Accelerate<T>(T geometry) where T : Geometry
		{
			if (geometry is null) return null;
			return (T) Engine.AccelerateForRelationalOperations(geometry);
		}

		public static bool Contains(Geometry containing, Geometry contained)
		{
			if (containing == null) return false;
			if (contained == null) return true;

			return Engine.Contains(containing, contained);
		}

		public static double GetDistanceAlongCurve(Multipart curve, MapPoint point)
		{
			const SegmentExtensionType extension = SegmentExtensionType.NoExtension;

			Engine.QueryPointAndDistance(
				curve, extension, point, AsRatioOrLength.AsLength,
				out double distanceAlong, out _, out _);
			return distanceAlong;
		}

		public static bool Disjoint(Geometry geometry1, Geometry geometry2)
		{
			if (geometry1 is null) return true;
			if (geometry2 is null) return true;
			return Engine.Disjoint(geometry1, geometry2);
		}

		public static Geometry GetClippedGeometry([NotNull] Geometry polygon,
		                                          [NotNull] Envelope clipExtent,
		                                          double clipExtentRotationDeg = 0)
		{
			if (clipExtentRotationDeg == 0)
			{
				Envelope clipExtentSref =
					EnsureSpatialReference(clipExtent, polygon.SpatialReference);

				return Engine.Clip(polygon, clipExtentSref);
			}

			// It's a polygon:
			Polygon envelopeAsPoly =
				GeometryFactory.CreatePolygon(clipExtent, polygon.SpatialReference);

			double rotationInRadians = MathUtils.ToRadians(clipExtentRotationDeg);

			Geometry rotated = Engine.Rotate(envelopeAsPoly, clipExtent.Center, rotationInRadians);

			return Engine.Intersection(polygon, rotated);
		}

		/// <summary>
		/// Clips the polygon using the provided envelope. The envelope can be rotated
		/// by the specified degrees before the clip is applied. This is useful for rotated
		/// map extents.
		/// </summary>
		/// <param name="polygon">The polygon to be clipped.</param>
		/// <param name="clipExtent">The clip extent.</param>
		/// <param name="clipExtentRotationDeg">The rotation to be applied to the clip extent before
		/// clipping. The unit is degrees.</param>
		/// <returns></returns>
		public static Polygon GetClippedPolygon([NotNull] Geometry polygon,
		                                        [NotNull] Envelope clipExtent,
		                                        double clipExtentRotationDeg = 0)
		{
			if (clipExtentRotationDeg == 0)
			{
				Envelope clipExtentSref =
					EnsureSpatialReference(clipExtent, polygon.SpatialReference);

				return (Polygon) Engine.Clip(polygon, clipExtentSref);
			}

			// It's a polygon:
			Polygon envelopeAsPoly =
				GeometryFactory.CreatePolygon(clipExtent, polygon.SpatialReference);

			double rotationInRadians = MathUtils.ToRadians(clipExtentRotationDeg);

			Geometry rotated = Engine.Rotate(envelopeAsPoly, clipExtent.Center, rotationInRadians);

			return (Polygon) Engine.Intersection(polygon, rotated);
		}

		public static Polyline GetClippedPolyline(Polyline polyline, Envelope clipExtent)
		{
			return (Polyline) Engine.Clip(polyline, clipExtent);
		}

		public static Envelope Project(Envelope envelope, SpatialReference sref)
		{
			var projected = Engine.Project(envelope, sref);
			return projected as Envelope ??
			       throw UnexpectedResultFrom("Project", typeof(Envelope), projected);
		}

		/// <summary>
		/// Return the given <paramref name="geometry"/> in the given
		/// spatial reference. If <paramref name="sref"/> is null,
		/// return <paramref name="geometry"/> as-is. If the given
		/// <paramref name="geometry"/> has no spatial reference, assume
		/// it's in the given spatial reference and just set it. Otherwise,
		/// project <paramref name="geometry"/> (if it has a different
		/// spatial reference).
		/// </summary>
		public static T EnsureSpatialReference<T>(T geometry, SpatialReference sref)
			where T : Geometry
		{
			if (geometry is null) return null;
			if (sref is null) return geometry; // TODO unsure: set geom's sref to null instead?

			if (geometry.SpatialReference is null)
			{
				return (T) GeometryBuilderEx.ReplaceSpatialReference(geometry, sref);
			}

			if (SpatialReference.AreEqual(geometry.SpatialReference, sref))
			{
				return geometry;
			}

			return (T) Engine.Project(geometry, sref);
		}

		public static bool HasCurves(this Geometry geometry)
		{
			return geometry is Multipart { HasCurves: true };
		}

		public static bool Intersects(Envelope a, Envelope b)
		{
			if (a is null || b is null) return false;

			// There's a quick'n'simple way for envelopes:
			return a.XMin <= b.XMax && b.XMin <= a.XMax &&
			       a.YMin <= b.YMax && b.YMin <= a.YMax;
		}

		public static bool Intersects(Geometry a, Geometry b)
		{
			if (a is null || b is null) return false;

			return Engine.Intersects(a, b);
		}

		[NotNull]
		public static MapPoint Centroid([NotNull] Geometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			Assert.False(geometry.IsEmpty, "geometry is empty");

			return Engine.Centroid(geometry);
		}

		/// <summary>
		/// Returns a reference to the smallest (area for IArea objects, length for ICurve objects) 
		/// geometry of the given geometries. If several geometries have the smallest size, the first 
		/// in the list will be returned.
		/// </summary>
		/// <param name="geometries">The geometries which must all be of the same geometry type.</param>
		/// <returns></returns>
		public static T GetSmallestGeometry<T>([NotNull] IEnumerable<T> geometries)
			where T : Geometry
		{
			Geometry smallestPart = null;
			double smallestSize = double.PositiveInfinity;

			foreach (T candidate in geometries)
			{
				double candidateSize = GetGeometrySize(candidate);

				if (candidateSize < smallestSize)
				{
					smallestPart = candidate;
					smallestSize = candidateSize;
				}
			}

			return (T) smallestPart;
		}

		[CanBeNull]
		public static Segment GetLargestSegment([NotNull] IEnumerable<Segment> segments,
		                                        Envelope areaOfInterest = null)
		{
			Segment shortesSegment = null;
			double longestLength = double.Epsilon;

			bool considerAreaOfInterest = areaOfInterest != null && ! areaOfInterest.IsEmpty;

			foreach (Segment candidate in segments)
			{
				if (considerAreaOfInterest)
				{
					Polyline lineSegment =
						GeometryFactory.CreatePolyline(candidate.StartPoint,
						                               candidate.EndPoint,
						                               candidate.SpatialReference);

					if (Disjoint(areaOfInterest, lineSegment))
					{
						continue;
					}
				}

				double length = candidate.Length;

				if (length > longestLength)
				{
					shortesSegment = candidate;
					longestLength = length;
				}
			}

			return shortesSegment;
		}

		/// <summary>
		/// Returns a value that indicates the size of the specified geometry:
		/// - Multipatch, Polygon, Ring: 2D area
		/// - Polyline, Path, Segment: 2D length
		/// - Multipoint: Point count
		/// - Point: 0
		/// </summary>
		/// <param name="geometry"></param>
		/// <returns></returns>
		public static double GetGeometrySize([NotNull] Geometry geometry)
		{
			var multipart = geometry as Multipart;

			if (multipart != null && multipart.Area > 0)
			{
				return Math.Abs(multipart.Area);
			}

			if (multipart != null)
			{
				return multipart.Length;
			}

			return 0;
		}

		public static Geometry EnsureGeometrySchema([NotNull] Geometry inputGeometry,
		                                            bool? hasZ,
		                                            bool? hasM = null,
		                                            bool? hasID = null)
		{
			bool changeHasZ = hasZ.HasValue && inputGeometry.HasZ != hasZ;
			bool changeHasM = hasM.HasValue && inputGeometry.HasM != hasM;
			bool changeHasID = hasID.HasValue && inputGeometry.HasID != hasID;

			if (! changeHasZ &&
			    ! changeHasM &&
			    ! changeHasID)
			{
				return SimplifyZ(inputGeometry);
			}

			var builder = inputGeometry.ToBuilder();

			builder.HasZ = hasZ ?? inputGeometry.HasZ;
			builder.HasM = hasM ?? inputGeometry.HasM;
			builder.HasID = hasID ?? inputGeometry.HasID;

			return SimplifyZ(builder.ToGeometry());
		}

		public static IGeometryEngine Engine
		{
			get => _engine ??= GeometryEngine.Instance;
			set => _engine = value;
		}

		private static IGeometryEngine _engine;

		#region Access points of a multipart geometry builder

		public static int GetPointCount(this MultipartBuilderEx builder,
		                                int partIndex)
		{
			// There is always one more vertex than there are segments,
			// even for rings, because the last vertex duplicates the first:
			return builder.GetSegmentCount(partIndex) + 1;
		}

		public static MapPoint GetPoint(this MultipartBuilderEx builder,
		                                int partIndex,
		                                int pointIndex)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));
			if (partIndex < 0 || partIndex >= builder.PartCount)
				throw new ArgumentOutOfRangeException(nameof(partIndex));
			var segmentCount = builder.GetSegmentCount(partIndex);
			if (pointIndex < 0 || pointIndex > segmentCount)
				throw new ArgumentOutOfRangeException(nameof(pointIndex));
			bool isEndPoint = pointIndex == segmentCount;
			var segmentIndex = isEndPoint ? segmentCount - 1 : pointIndex;
			var segment = builder.GetSegment(partIndex, segmentIndex);
			return isEndPoint ? segment.EndPoint : segment.StartPoint;
		}

		public static void SetPoint(this MultipartBuilderEx builder,
		                            int partIndex,
		                            int pointIndex,
		                            MapPoint point)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));
			if (partIndex < 0 || partIndex >= builder.PartCount)
				throw new ArgumentOutOfRangeException(nameof(partIndex));
			var segmentCount = builder.GetSegmentCount(partIndex);
			if (pointIndex < 0 || pointIndex > segmentCount)
				throw new ArgumentOutOfRangeException(nameof(pointIndex));

			// TODO beware of closed rings!
			Segment pre, post;
			switch (builder)
			{
				case PolylineBuilderEx:
					// update StartPoint on segment i (if exists)
					// update EndPoint on segment i-1 (if exists)
					if (pointIndex < segmentCount)
					{
						post = builder.GetSegment(partIndex, pointIndex);
						post = SetPoints(post, point, null);
						builder.ReplaceSegment(partIndex, pointIndex, post);
					}

					if (pointIndex > 0)
					{
						pre = builder.GetSegment(partIndex, pointIndex - 1);
						pre = SetPoints(pre, null, point);
						builder.ReplaceSegment(partIndex, pointIndex - 1, pre);
					}

					break;

				case PolygonBuilderEx:
					// update StartPoint of segment i (mod N)
					// update EndPoint of segment (i-1) (mod N)

					int segmentIndex = pointIndex % segmentCount;
					post = builder.GetSegment(partIndex, segmentIndex);
					post = SetPoints(post, point, null);
					builder.ReplaceSegment(partIndex, segmentIndex, post);

					segmentIndex = pointIndex > 0 ? pointIndex - 1 : segmentCount - 1;
					pre = builder.GetSegment(partIndex, segmentIndex);
					pre = SetPoints(pre, null, point);
					builder.ReplaceSegment(partIndex, segmentIndex, pre);
					break;

				default:
					throw new InvalidOperationException(
						"multipart builder is neither polygon nor polyline builder");
			}
		}

		#endregion

		public static T SetPoints<T>(T segment, [CanBeNull] MapPoint startPoint,
		                             [CanBeNull] MapPoint endPoint)
			where T : Segment
		{
			if (segment is null)
				throw new ArgumentNullException(nameof(segment));
			if (startPoint is null && endPoint is null) return segment;

			var builder = SegmentBuilderEx.ConstructSegmentBuilder(segment);

			if (startPoint is not null)
			{
				builder.StartPoint = startPoint;
			}

			if (endPoint is not null)
			{
				builder.EndPoint = endPoint;
			}

			return (T) builder.ToSegment();
		}

		/// <summary>
		/// Given a global (shape-wide) vertex index, get the
		/// part index and the part-local vertex index.
		/// </summary>
		public static int GetLocalVertexIndex(Geometry shape, int globalVertexIndex, out int partIndex)
		{
			if (shape is Multipoint multipoint)
			{
				// by convention (and consistent with the SDK's hit test),
				// multipoints have partIndex == vertexIndex for all points

				var pointCount = multipoint.PointCount;
				if (globalVertexIndex < 0 || globalVertexIndex >= pointCount)
					throw new ArgumentOutOfRangeException(nameof(globalVertexIndex));

				partIndex = globalVertexIndex;
				return globalVertexIndex;
			}

			if (shape is Multipart multipart)
			{
				return GetLocalVertexIndex(multipart, globalVertexIndex, out partIndex);
			}

			if (shape is Multipatch)
			{
				throw new NotImplementedException("Multipatches are not yet implemented");
			}

			partIndex = 0;
			return globalVertexIndex;
		}

		/// <summary>
		/// Given a global (shape-wide) vertex index, get the part-local
		/// vertex index and the part index
		/// </summary>
		public static int GetLocalVertexIndex(Multipart multipart, int globalVertexIndex, out int partIndex)
		{
			if (multipart is null)
				throw new ArgumentNullException(nameof(multipart));
			if (globalVertexIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(globalVertexIndex));

			int vertexIndex = globalVertexIndex;
			int partCount = multipart.PartCount;
			for (int k = 0; k < partCount; k++)
			{
				int segmentCount = multipart.Parts[k].Count;
				int vertexCount = segmentCount + 1; // even for rings!

				if (vertexIndex < vertexCount)
				{
					partIndex = k;
					return vertexIndex;
				}

				vertexIndex -= vertexCount;
			}

			throw new ArgumentOutOfRangeException(nameof(globalVertexIndex));
		}

		/// <summary>
		/// Given a part index and a part-local vertex index,
		/// get the global (shape-wide) vertex index.
		/// </summary>
		public static int GetGlobalVertexIndex(Geometry shape, int partIndex, int localVertexIndex)
		{
			if (shape is Multipoint multipoint)
			{
				// by convention (and consistent with the SDK's hit test),
				// multipoints have partIndex == vertexIndex for all points

				var pointCount = multipoint.PointCount;
				if (localVertexIndex < 0 || localVertexIndex >= pointCount)
					throw new ArgumentOutOfRangeException(nameof(localVertexIndex));

				return localVertexIndex;
			}

			if (shape is Multipart multipart)
			{
				if (multipart.IsEmpty)
					throw new InvalidOperationException("empty geometry");

				int partCount = multipart.PartCount;
				if (partIndex < 0 || partIndex >= partCount)
					throw new ArgumentOutOfRangeException(nameof(partIndex));

				int k = 0;
				int segmentCount;
				int vertexCount;
				int vertexTally = 0;

				for (; k < partIndex; k++)
				{
					segmentCount = multipart.Parts[k].Count;
					vertexCount = segmentCount + 1; // even for rings!
					vertexTally += vertexCount;
				}

				segmentCount = multipart.Parts[k].Count;
				vertexCount = segmentCount + 1;

				if (localVertexIndex < 0 || localVertexIndex >= vertexCount)
					throw new ArgumentOutOfRangeException(nameof(localVertexIndex));

				return vertexTally + localVertexIndex;
			}

			if (shape is Multipatch)
			{
				throw new NotImplementedException("Multipatches are not yet implemented");
			}

			return localVertexIndex;
		}

		/// <summary>
		/// This method ensures that either <paramref name="partIndex"/>
		/// equals <paramref name="pointIndex"/>, or that either
		/// <paramref name="partIndex"/> or <paramref name="pointIndex"/>
		/// is zero or unspecified (negative).
		/// </summary>
		/// <remarks>By convention, a multipoint geometry reports its
		/// constituent points as its points (addressed by point index)
		/// and as its parts (addressed by part index).</remarks>
		public static int GetMultipointIndex(int partIndex, int pointIndex)
		{
			// p == v | p > 0 and v <= 0 | v > 0 and p <= 0

			if (partIndex == pointIndex)
			{
				return partIndex;
			}

			if (partIndex <= 0)
			{
				return pointIndex;
			}

			if (pointIndex <= 0)
			{
				return partIndex;
			}

			throw new ArgumentException("For a multipoint, part and vertex index must not be different");
		}

		public static Geometry AddVertex(Geometry shape, MapPoint point)
		{
			return AddVertex(shape, point, out _, out _);
		}

		/// <summary>
		/// Add a vertex to the given <paramref name="shape"/>
		/// (a Polyline, Polygon, or a Multipoint) at or near the
		/// given <paramref name="point"/> (it will be projected
		/// onto the Polyline or Polygon boundary). If the given
		/// <paramref name="point"/> has a non-zero ID, the new
		/// vertex will be a control point, otherwise a regular vertex.
		/// </summary>
		/// <returns>A new geometry with the vertex added</returns>
		/// <remarks>For multipoints, just append the point, for polylines
		/// and polygons, use <see cref="IGeometryEngine.SplitAtPoint"/></remarks>
		public static Geometry AddVertex(Geometry shape, MapPoint point, out int partIndex, out int vertexIndex)
		{
			if (shape is null)
				throw new ArgumentNullException(nameof(shape));
			if (point is null)
				throw new ArgumentNullException(nameof(point));

			// Multipoint: add point at clickPoint
			// Multipart: split line at clickPoint
			// MultiPatch: not implemented
			// otherwise: error

			if (shape is Multipoint multipoint)
			{
				point = EnsureSpatialReference(point, multipoint.SpatialReference);
				var builder = new MultipointBuilderEx(multipoint);
				if (point.HasID) builder.HasID = true;
				// else: don't modify HasID
				builder.AddPoint(point);
				partIndex = vertexIndex = builder.PointCount - 1;
				return builder.ToGeometry();
			}

			if (shape is Multipart multipart)
			{
				if (multipart.IsEmpty)
				{
					// cannot add a (single) vertex to an empty polyline/polygon
					partIndex = -1;
					vertexIndex = -1;
					return multipart;
				}

				const bool projectOnto = true;
				point = EnsureSpatialReference(point, multipart.SpatialReference);
				Geometry newShape = GeometryEngine.Instance.SplitAtPoint(
					multipart, point, projectOnto, false,
					out bool splitOccurred, out partIndex, out int segmentIndex);
				if (!splitOccurred)
					throw new Exception($"Could not add vertex to {multipart.GeometryType}: " +
					                    $"{nameof(GeometryEngine.Instance.SplitAtPoint)} says no split occurred");
				// Returned partIndex and segmentIndex are for the segment *after* the
				// split point, thus segmentIndex is the vertexIndex of the inserted vertex:
				vertexIndex = segmentIndex;
				// SplitAtPoint interpolates Z and M attributes (good), but also seems to
				// inherit the ID from neighbouring vertices: take ID from split point
				newShape = ControlPointUtils.SetPointID(point.ID, newShape, partIndex, segmentIndex);
				return newShape;
			}

			if (shape is Multipatch)
			{
				throw new NotImplementedException("Add Vertex is not implemented for MultiPatch geometries");
			}

			throw new NotSupportedException(
				$"Cannot Add Vertex on a geometry of type {shape.GetType().Name}");
		}

		/// <summary>
		/// Remove the addressed vertex from the given geometry.
		/// For a point geometry, both indices must be zero.
		/// For a multipoint geometry, both indices should be the same.
		/// </summary>
		/// <returns>A new geometry instance, which may be an empty geometry
		/// (if last vertex or segment was removed)</returns>
		public static Geometry RemoveVertex(Geometry shape, int partIndex, int vertexIndex)
		{
			if (shape is null)
				throw new ArgumentNullException(nameof(shape));

			if (shape.IsEmpty)
				throw new InvalidOperationException("Cannot remove vertex on an empty geometry");

			if (shape is MapPoint mapPoint)
			{
				if (partIndex != 0)
					throw new ArgumentOutOfRangeException(nameof(partIndex));
				if (vertexIndex != 0)
					throw new ArgumentOutOfRangeException(nameof(vertexIndex));

				var builder = new MapPointBuilderEx(mapPoint);
				builder.SetEmpty();
				return builder.ToGeometry();
			}

			if (shape is Multipoint multipoint)
			{
				int pointIndex = GetMultipointIndex(partIndex, vertexIndex);
				if (pointIndex < 0 || pointIndex >= multipoint.PointCount)
					throw new ArgumentOutOfRangeException(
						"point index out of range for multipoint shape", (Exception) null);

				var builder = new MultipointBuilderEx(multipoint);
				builder.RemovePoint(pointIndex);
				return builder.ToGeometry();
			}

			if (shape is Polyline polyline)
			{
				var builder = new PolylineBuilderEx(polyline);
				RemoveVertices(builder, partIndex, vertexIndex);
				return builder.ToGeometry();
			}

			if (shape is Polygon polygon)
			{
				var builder = polygon.ToBuilder();
				RemoveVertices(builder, partIndex, vertexIndex);
				return builder.ToGeometry();
			}

			if (shape is Multipatch)
			{
				throw new NotImplementedException();
			}

			if (shape is Envelope)
			{
				throw new NotSupportedException("Cannot remove vertex on an Envelope");
			}

			throw new NotSupportedException($"Geometry type {shape.GetType().Name} is not supported");
		}

		public static void RemoveVertices(/*this*/ MultipartBuilderEx builder, int partIndex,
		                                           int firstVertex, int lastVertex = -1)
		{
			switch (builder)
			{
				case null:
					throw new ArgumentNullException(nameof(builder));
				case PolylineBuilderEx polylineBuilder:
					RemoveVertices(polylineBuilder, partIndex, firstVertex, lastVertex);
					break;
				case PolygonBuilderEx polygonBuilder:
					RemoveVertices(polygonBuilder, partIndex, firstVertex, lastVertex);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(builder),
					                                      "neither Polyline nor Polygon builder");
			}
		}

		/// <summary>
		/// Remove the vertices from first to last (both inclusive) in the
		/// given part from the given Polyline builder. The resulting gap
		/// is filled with a straight line segment. All indices are validated
		/// against the given <paramref name="builder"/>. If no last vertex
		/// is given, it defaults to the given first vertex.
		/// </summary>
		public static void RemoveVertices(/*this*/ PolylineBuilderEx builder, int partIndex,
		                                  int firstVertex, int lastVertex = -1)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));

			if (builder.PartCount <= 0)
				throw new InvalidOperationException("no parts at all (empty builder)");
			if (partIndex < 0 || partIndex >= builder.PartCount)
				throw new ArgumentOutOfRangeException(nameof(partIndex));

			if (lastVertex < 0) lastVertex = firstVertex; // convenience
			if (lastVertex < firstVertex) return; // nothing to remove
			var verticesInPart = builder.GetSegmentCount(partIndex) + 1;

			if (firstVertex < 0 || firstVertex >= verticesInPart)
				throw new ArgumentOutOfRangeException(nameof(firstVertex));
			if (lastVertex >= verticesInPart)
				throw new ArgumentOutOfRangeException(nameof(lastVertex));

			// If less than two vertices remain, remove entire part:
			int verticesToRemove = lastVertex - firstVertex + 1;
			if (verticesInPart - verticesToRemove < 2)
			{
				builder.RemovePart(partIndex);
				return;
			}

			if (firstVertex == 0)
			{
				// Remove first k segments:
				for (int i = 0; i <= lastVertex; i++)
				{
					builder.RemoveSegment(partIndex, i, false);
				}
			}
			else if (lastVertex == verticesInPart - 1)
			{
				// Remove last k segments (from end to avoid memory moving):
				for (int i = lastVertex; i >= firstVertex; i--)
				{
					var segmentIndex = i - 1;
					builder.RemoveSegment(partIndex, segmentIndex, false);
				}
			}
			else
			{
				// Remove segments last through first, then
				// Replace segment first-1 with line segment to close the gap
				var startPoint = builder.Parts[partIndex][firstVertex - 1].StartPoint;
				var endPoint = builder.Parts[partIndex][lastVertex].EndPoint;
				for (int i = lastVertex; i >= firstVertex; i--)
				{
					builder.RemoveSegment(partIndex, i, false);
				}
				var straight = LineBuilderEx.CreateLineSegment(startPoint, endPoint);
				builder.ReplaceSegment(partIndex, firstVertex - 1, straight);
			}
		}

		/// <summary>
		/// Remove vertices from a Polygon builder.
		/// Presently, only a single vertex can be removed at a time (expect
		/// <paramref name="firstVertex"/> == <paramref name="lastVertex"/>).
		/// The resulting gap is filled with a straight line segment.
		/// The vertex indices are taken modulo the part's segment count,
		/// that is, they are cyclic.
		/// </summary>
		public static void RemoveVertices(/*this*/ PolygonBuilderEx builder, int partIndex,
		                                  int firstVertex, int lastVertex = -1)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));

			if (builder.PartCount <= 0)
				throw new InvalidOperationException("no parts at all (empty builder)");
			if (partIndex < 0 || partIndex >= builder.PartCount)
				throw new ArgumentOutOfRangeException(nameof(partIndex));

			if (lastVertex < 0) lastVertex = firstVertex; // convenience;
			else if (lastVertex != firstVertex)
				throw new NotImplementedException("Can only remove one vertex for now");

			var segmentCount = builder.GetSegmentCount(partIndex);

			firstVertex %= segmentCount;

			if (segmentCount <= 2)
			{
				builder.RemovePart(partIndex);
				return;
			}

			if (firstVertex == 0) // vertex is start/end-point
			{
				// remove last segment, replace first segment:
				var startPoint = builder.Parts[partIndex][segmentCount - 1].StartPoint;
				var endPoint = builder.Parts[partIndex][0].EndPoint;
				var straight = LineBuilderEx.CreateLineSegment(startPoint, endPoint);
				builder.RemoveSegment(partIndex, segmentCount - 1, false); // leave gap
				builder.ReplaceSegment(partIndex, 0, straight); // new segment closes gap
			}
			else // any other vertex
			{
				var startPoint = builder.Parts[partIndex][firstVertex - 1].StartPoint;
				var endPoint = builder.Parts[partIndex][firstVertex].EndPoint;
				var straight = LineBuilderEx.CreateLineSegment(startPoint, endPoint);
				builder.RemoveSegment(partIndex, firstVertex, false); // leave gap
				builder.ReplaceSegment(partIndex, firstVertex - 1, straight);
			}
		}

		#region Moving vertices of a multipart geometry builder

		public static void MovePart(this MultipartBuilderEx builder, int partIndex, double dx,
		                            double dy)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));

			int segmentCount = builder.GetSegmentCount(partIndex);

			for (int k = 0; k < segmentCount; k++)
			{
				var segment = builder.GetSegment(partIndex, k);
				var moved = segment.Shifted(dx, dy);
				builder.ReplaceSegment(partIndex, k, moved);
			}
		}

		public static void MoveVertex(this MultipartBuilderEx builder, int partIndex,
		                              int vertexIndex, double dx, double dy)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));

			int segmentCount = builder.GetSegmentCount(partIndex);

			switch (builder)
			{
				case PolylineBuilderEx:
					// update StartPoint on segment i (if exists)
					// update EndPoint on segment i-1 (if exists)
					if (vertexIndex < segmentCount)
						MoveStartPoint(builder, partIndex, vertexIndex, dx, dy);
					vertexIndex -= 1;
					if (vertexIndex >= 0)
						MoveEndPoint(builder, partIndex, vertexIndex, dx, dy);
					break;

				case PolygonBuilderEx:
					// update StartPoint of segment i (mod N)
					// update EndPoint of segment (i-1) (mod N)
					vertexIndex %= segmentCount;
					MoveStartPoint(builder, partIndex, vertexIndex, dx, dy);
					vertexIndex -= 1;
					if (vertexIndex < 0) vertexIndex += segmentCount;
					MoveEndPoint(builder, partIndex, vertexIndex, dx, dy);
					break;

				default:
					throw new NotSupportedException(
						"Multipart builder is neither polygon nor polyline builder");
			}
		}

		private static void MoveStartPoint(
			MultipartBuilderEx builder, int partIndex, int segmentIndex, double dx, double dy)
		{
			var segment = builder.GetSegment(partIndex, segmentIndex);

			var startPoint = segment.StartPoint.Shifted(dx, dy);
			Segment moved = UpdateEndpoints(segment, startPoint, null);

			builder.ReplaceSegment(partIndex, segmentIndex, moved);
		}

		private static void MoveEndPoint(
			MultipartBuilderEx builder, int partIndex, int segmentIndex, double dx, double dy)
		{
			var segment = builder.GetSegment(partIndex, segmentIndex);

			var endPoint = segment.EndPoint.Shifted(dx, dy);
			Segment moved = UpdateEndpoints(segment, null, endPoint);

			builder.ReplaceSegment(partIndex, segmentIndex, moved);
		}

		/// <summary>
		/// Update either or both endpoints of the given segment.
		/// For a line segment, this is trivial. For a cubic Bézier
		/// segment, move the control point(s) accordingly. For a
		/// circular or elliptic arc, move the center decently.
		/// </summary>
		public static T UpdateEndpoints<T>([NotNull] T segment, MapPoint startPoint, MapPoint endPoint)
			where T : Segment
		{
			if (segment is null)
				throw new ArgumentNullException(nameof(segment));

			if (segment is CubicBezierSegment bezier)
			{
				return (T) (Segment) UpdateEndpoints(bezier, startPoint, endPoint);
			}

			if (segment is EllipticArcSegment arc)
			{
				return (T) (Segment) UpdateEndpoints(arc, startPoint, endPoint);
			}

			var builder = segment.ToBuilder();
			builder.StartPoint = startPoint ?? segment.StartPoint;
			builder.EndPoint = endPoint ?? builder.EndPoint;
			return (T) builder.ToSegment();
		}

		private static CubicBezierSegment UpdateEndpoints(
			CubicBezierSegment bezier, MapPoint startPoint, MapPoint endPoint)
		{
			if (startPoint is null && endPoint is null)
			{
				// no endpoint changed: nothing to do
				return bezier;
			}

			// Move CP1 same as startPoint, and CP2 same as endPoint:

			var controlPoint1 = bezier.ControlPoint1;
			var controlPoint2 = bezier.ControlPoint2;

			if (startPoint is not null)
			{
				controlPoint1.Move(startPoint.X - bezier.StartPoint.X,
				         startPoint.Y - bezier.StartPoint.Y);
			}

			if (endPoint is not null)
			{
				controlPoint2.Move(endPoint.X - bezier.EndPoint.X,
				         endPoint.Y - bezier.EndPoint.Y);
			}

			var sref = bezier.SpatialReference;

			return CubicBezierBuilderEx.CreateCubicBezierSegment(
				startPoint ?? bezier.StartPoint, controlPoint1,
				controlPoint2, endPoint ?? bezier.EndPoint, sref);
		}

		private static EllipticArcSegment UpdateEndpoints(
			EllipticArcSegment arc, MapPoint startPoint, MapPoint endPoint)
		{
			if (startPoint is null && endPoint is null)
			{
				// no endpoint changed: nothing to do
				return arc;
			}

			// Cannot just update arc.StartPoint and arc.EndPoint:
			// must recreate the circular/elliptic arc segment:

			EllipticArcSegment updated;

			var orientation = arc.IsCounterClockwise
				                  ? ArcOrientation.ArcCounterClockwise
				                  : ArcOrientation.ArcClockwise;

			var sref = arc.SpatialReference;

			if (arc.IsCircular)
			{

				var ds = startPoint is null
					         ? new Coordinate2D(0, 0)
					         : new Coordinate2D(startPoint) - new Coordinate2D(arc.StartPoint);

				var de = endPoint is null
					         ? new Coordinate2D(0, 0)
					         : new Coordinate2D(endPoint) - new Coordinate2D(arc.EndPoint);

				var dc = 0.5 * (ds + de);
				var centerPt = arc.CenterPoint.Shifted(dc.X, dc.Y);

				updated = EllipticArcBuilderEx.CreateCircularArc(
					startPoint ?? arc.StartPoint, endPoint ?? arc.EndPoint, centerPt, orientation, sref);
			}
			else
			{
				var minor = arc.IsMinor
					            ? MinorOrMajor.Minor
					            : MinorOrMajor.Major;

				updated = EllipticArcBuilderEx.CreateEllipticArcSegment(
					startPoint ?? arc.StartPoint, endPoint ?? arc.EndPoint,
					arc.SemiMajorAxis, arc.MinorMajorRatio,
					arc.RotationAngle, minor, orientation, sref);
			}

			return updated;
		}

		#endregion

		public static GeometryType TranslateEsriGeometryType(esriGeometryType esriGeometryType)
		{
			switch (esriGeometryType)
			{
				case esriGeometryType.esriGeometryPoint:
					return GeometryType.Point;
				case esriGeometryType.esriGeometryMultipoint:
					return GeometryType.Multipoint;
				case esriGeometryType.esriGeometryPolyline:
					return GeometryType.Polyline;
				case esriGeometryType.esriGeometryPolygon:
					return GeometryType.Polygon;
				case esriGeometryType.esriGeometryMultiPatch:
					return GeometryType.Multipatch;
				case esriGeometryType.esriGeometryEnvelope:
					return GeometryType.Envelope;
				case esriGeometryType.esriGeometryBag:
					return GeometryType.GeometryBag;
				case esriGeometryType.esriGeometryAny:
				case esriGeometryType.esriGeometryNull:
					return GeometryType.Unknown;
				default: // TODO Why not translate the remaining ones to Unknown as well?
					throw new ArgumentOutOfRangeException($"Cannot translate {esriGeometryType}");
			}
		}

		public static ProSuiteGeometryType TranslateToProSuiteGeometryType(
			GeometryType esriGeometryType)
		{
			switch (esriGeometryType)
			{
				case GeometryType.Unknown:
					return ProSuiteGeometryType.Unknown;
				case GeometryType.Point:
					return ProSuiteGeometryType.Point;
				case GeometryType.Envelope:
					return ProSuiteGeometryType.Envelope;
				case GeometryType.Multipoint:
					return ProSuiteGeometryType.Multipoint;
				case GeometryType.Polyline:
					return ProSuiteGeometryType.Polyline;
				case GeometryType.Polygon:
					return ProSuiteGeometryType.Polygon;
				case GeometryType.Multipatch:
					return ProSuiteGeometryType.MultiPatch;
				case GeometryType.GeometryBag:
					return ProSuiteGeometryType.Bag;
				default:
					throw new ArgumentOutOfRangeException(nameof(esriGeometryType),
					                                      esriGeometryType, null);
			}
		}

		private static Exception UnexpectedResultFrom(string action,
		                                              Type expectedType,
		                                              object actualValue)
		{
			return new AssertionException(
				$"Unexpected result from {action}: " +
				$"expected type {expectedType.Name}, " +
				$"actual type {actualValue?.GetType().Name ?? "(null)"}");
		}

		public static int GetShapeDimension(GeometryType geometryType)
		{
			switch (geometryType)
			{
				case GeometryType.Point:
				case GeometryType.Multipoint:
					return 0;
				case GeometryType.Polyline:
					return 1;
				case GeometryType.Polygon:
				case GeometryType.Multipatch:
				case GeometryType.Envelope:
					return 2;

				default:
					throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType,
					                                      $"Unexpected geometry type: {geometryType}");
			}
		}

		public static string Format([NotNull] MapPoint point, int digits = 0)
		{
			return $"{Math.Round(point.X, digits)}/{Math.Round(point.Y, digits)}";
		}

		[NotNull]
		public static string Format([NotNull] Envelope extent, int digits = 0)
		{
			MapPoint lowerLeft = GetLowerLeft(extent);
			MapPoint upperRight = GetUpperRight(extent);

			return $"{Format(lowerLeft, digits)}, {Format(upperRight, digits)}";
		}

		public static IEnumerable<MapPoint> GetVertices([CanBeNull] Geometry geometry)
		{
			// Check the type of geometry
			if (geometry is Polyline polyline)
			{
				// Access vertices of a polyline
				foreach (var point in polyline.Points)
				{
					yield return point;
				}
			}
			else if (geometry is Polygon polygon)
			{
				// Access vertices of a polygon
				foreach (var point in polygon.Points)
				{
					yield return point;
				}
			}
			else if (geometry is Multipoint multipoint)
			{
				// Access vertices of a multipoint
				foreach (var point in multipoint.Points)
				{
					yield return point;
				}
			}
			else if (geometry is MapPoint mapPoint)
			{
				// Single point geometry
				yield return mapPoint;
			}
		}
	}
}
