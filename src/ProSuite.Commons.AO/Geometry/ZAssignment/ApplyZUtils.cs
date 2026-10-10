using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Properties;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.ZAssignment
{
	public static class ApplyZUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static readonly ThreadLocal<IPoint> _queryPoint =
			new ThreadLocal<IPoint>(() => new PointClass());

		[NotNull]
		public static T Dtm<T>([NotNull] T source,
		                       [NotNull] ISimpleSurface surface,
		                       [CanBeNull] IGeometry areaOfInterest = null,
		                       double drapeTolerance = double.NaN,
		                       double zOffset = double.NaN)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(surface, LocalizableStrings.ApplyZ_NoSurface);

			IGeometry newGeometry;
			if (! double.IsNaN(drapeTolerance))
			{
				newGeometry = Drape(source, surface, areaOfInterest, drapeTolerance);
			}
			else
			{
				newGeometry = GeometryFactory.Clone(source);
				if (! TryUpdateShapeVerticesZ(newGeometry, surface, areaOfInterest))
				{
					throw new InvalidDataException("Z value could not be assigned from surface.");
				}
			}

			if (! double.IsNaN(zOffset) && Math.Abs(zOffset) > double.Epsilon)
			{
				newGeometry = GeometryUtils.OffsetZ(newGeometry, zOffset);
			}

			return (T) newGeometry;
		}

		[CanBeNull]
		public static IGeometry Interpolate([NotNull] IGeometry sourceGeometry,
		                                    [CanBeNull] IGeometry areaOfInterest,
		                                    [CanBeNull] NotificationCollection notifications)
		{
			if (! (sourceGeometry is IPolycurve polycurve))
			{
				NotificationUtils.Add(notifications,
				                      $"Interpolate is only supported for polylines and polygons " +
				                      $"that are cut by the area of interest. The provided shape " +
				                      $"type is {sourceGeometry.GeometryType}");

				return null;
			}

			if (areaOfInterest == null || GeometryUtils.Contains(areaOfInterest, polycurve))
			{
				return Interpolate(polycurve, notifications);
			}

			double tolerance = GeometryUtils.GetXyTolerance(polycurve);

			IBoundedXY aoi = GetAoiBounds(areaOfInterest);

			MultiPolycurve multiPolycurve =
				GeometryConversionUtils.CreateMultiPolycurve(polycurve);

			foreach (Pnt3D point in multiPolycurve.GetPoints())
			{
				if (! GeomRelationUtils.BoundsContainXY(aoi, point, tolerance))
				{
					continue;
				}

				if (aoi is ISegmentList polygon &&
				    ! GeomRelationUtils.PolycurveContainsXY(polygon, point, tolerance))
				{
					continue;
				}

				point.Z = double.NaN;
			}

			multiPolycurve.InterpolateUndefinedZs();

			return CreateAoGeometry(multiPolycurve.GetLinestrings(), polycurve);
		}

		/// <summary>
		/// Interpolates the Z values of the vertices within the area of interest. For each
		/// sequence of consecutive vertices within the area of interest, the outermost vertices
		/// keep their Z values and the Z values of the vertices in between are interpolated.
		/// Rings that are completely within the area of interest are not changed.
		/// </summary>
		[CanBeNull]
		public static IGeometry InterpolateWithin([NotNull] IGeometry sourceGeometry,
		                                          [NotNull] IGeometry areaOfInterest,
		                                          [CanBeNull] NotificationCollection notifications)
		{
			if (! (sourceGeometry is IPolycurve polycurve))
			{
				NotificationUtils.Add(notifications,
				                      $"Interpolate is only supported for polylines and polygons. " +
				                      $"The provided shape type is {sourceGeometry.GeometryType}");

				return null;
			}

			double tolerance = GeometryUtils.GetXyTolerance(polycurve);

			IBoundedXY aoi = GetAoiBounds(areaOfInterest);

			MultiPolycurve multiPolycurve =
				GeometryConversionUtils.CreateMultiPolycurve(polycurve);

			foreach (Linestring linestring in multiPolycurve.GetLinestrings())
			{
				List<Pnt3D> points = linestring.GetPoints().ToList();

				bool closed = linestring.IsClosed;

				// For rings, the last point is the same as the first point
				int count = closed ? points.Count - 1 : points.Count;

				bool[] within = points.Take(count)
				                      .Select(p => IsWithin(p, aoi, tolerance))
				                      .ToArray();

				if (closed && within.All(w => w))
				{
					NotificationUtils.Add(
						notifications,
						"A ring completely within the area of interest cannot be interpolated. " +
						"Select a part of the boundary to be interpolated.");
					continue;
				}

				bool[] interpolate = new bool[count];

				for (int i = 0; i < count; i++)
				{
					int? previous = i > 0 ? i - 1 : closed ? count - 1 : (int?) null;
					int? next = i < count - 1 ? i + 1 : closed ? 0 : (int?) null;

					// Only the vertices with both neighbours within are interpolated. The
					// outermost vertices within the area of interest keep their Z value.
					interpolate[i] = within[i] &&
					                 previous != null && within[previous.Value] &&
					                 next != null && within[next.Value];
				}

				for (int i = 0; i < count; i++)
				{
					if (interpolate[i])
					{
						points[i].Z = double.NaN;
					}
				}

				if (closed && interpolate[0])
				{
					points[count].Z = double.NaN;
				}

				linestring.TryInterpolateUndefinedZs();
			}

			return CreateAoGeometry(multiPolycurve.GetLinestrings(), polycurve);
		}

		private static bool IsWithin([NotNull] Pnt3D point,
		                             [NotNull] IBoundedXY aoi,
		                             double tolerance)
		{
			if (! GeomRelationUtils.BoundsContainXY(aoi, point, tolerance))
			{
				return false;
			}

			return ! (aoi is ISegmentList polygon) ||
			       GeomRelationUtils.PolycurveContainsXY(polygon, point, tolerance);
		}

		public static IGeometry ConstantZ([NotNull] IGeometry sourceGeometry,
		                                  double z,
		                                  [CanBeNull] IGeometry areaOfInterest)
		{
			if (areaOfInterest == null || GeometryUtils.Contains(areaOfInterest, sourceGeometry))
			{
				return GeometryUtils.ConstantZ(sourceGeometry, z);
			}

			double tolerance = GeometryUtils.GetXyTolerance(sourceGeometry);

			IBoundedXY aoi = GetAoiBounds(areaOfInterest);

			IEnumerable<Pnt3D> points;
			if (sourceGeometry is IPolycurve polycurve)
			{
				MultiPolycurve multiPolycurve =
					GeometryConversionUtils.CreateMultiPolycurve(polycurve);

				points = multiPolycurve.GetPoints();

				AssignZsWithin(points, z, aoi, tolerance);

				return CreateAoGeometry(multiPolycurve.GetLinestrings(), polycurve);
			}

			if (sourceGeometry is IMultipoint sourceMultipoint)
			{
				Multipoint<IPnt> multipoint =
					GeometryConversionUtils.CreateMultipoint(sourceMultipoint);

				points = multipoint.GetPoints().Cast<Pnt3D>();

				AssignZsWithin(points, z, aoi, tolerance);

				return CreateAoGeometry(multipoint, sourceMultipoint);
			}

			if (GeometryUtils.Disjoint(areaOfInterest, sourceGeometry))
			{
				return sourceGeometry;
			}

			throw new NotImplementedException(
				$"Applying constant Z to parts of a {sourceGeometry.GeometryType} is currently not supported");
		}

		public static IGeometry Offset([NotNull] IGeometry sourceGeometry,
		                               double zOffset,
		                               [CanBeNull] IGeometry areaOfInterest)
		{
			if (areaOfInterest == null || GeometryUtils.Contains(areaOfInterest, sourceGeometry))
			{
				return GeometryUtils.OffsetZ(sourceGeometry, zOffset);
			}

			double tolerance = GeometryUtils.GetXyTolerance(sourceGeometry);

			IBoundedXY aoi = GetAoiBounds(areaOfInterest);

			IEnumerable<Pnt3D> points;
			if (sourceGeometry is IPolycurve polycurve)
			{
				MultiPolycurve multiPolycurve =
					GeometryConversionUtils.CreateMultiPolycurve(polycurve);

				points = multiPolycurve.GetPoints();

				OffsetZsWithin(points, zOffset, aoi, tolerance);

				return CreateAoGeometry(multiPolycurve.GetLinestrings(), polycurve);
			}

			if (sourceGeometry is IMultipoint sourceMultipoint)
			{
				Multipoint<IPnt> multipoint =
					GeometryConversionUtils.CreateMultipoint(sourceMultipoint);

				points = multipoint.GetPoints().Cast<Pnt3D>();

				OffsetZsWithin(points, zOffset, aoi, tolerance);

				return CreateAoGeometry(multipoint, sourceMultipoint);
			}

			if (GeometryUtils.Disjoint(areaOfInterest, sourceGeometry))
			{
				return sourceGeometry;
			}

			throw new NotImplementedException(
				$"Applying Z offset to parts of a {sourceGeometry.GeometryType} is currently not supported");
		}

		/// <summary>
		/// Gets the minimal Z tolerance to be used to detect Z changes. It is half the Z
		/// resolution of the geometry's spatial reference, i.e. any Z change that survives
		/// storing the geometry is detected.
		/// </summary>
		public static double GetMinimumZTolerance([NotNull] IGeometry geometry)
		{
			ISpatialReference spatialReference = geometry.SpatialReference;

			double zResolution = spatialReference != null
				                     ? GeometryUtils.GetZResolution(spatialReference)
				                     : double.NaN;

			return double.IsNaN(zResolution) || zResolution <= 0
				       ? double.Epsilon
				       : zResolution / 2;
		}

		/// <summary>
		/// Calculates the parts of the updated geometry whose Z values differ from the source
		/// geometry by more than the specified Z tolerance. The result has the Z values of the
		/// updated geometry:
		/// - Polylines and polygons: A polyline containing the connected changed segments.
		/// - Multipoints: A multipoint containing the changed points.
		/// - Points: The updated point, if it has changed.
		/// </summary>
		/// <param name="sourceGeometry">The original geometry.</param>
		/// <param name="updatedGeometry">The geometry with the updated Z values. Its vertices
		/// must be a super-set (in XY) of the source geometry's vertices.</param>
		/// <param name="zTolerance">The Z tolerance.</param>
		/// <returns>The changed parts or null, if no Z value has changed.</returns>
		[CanBeNull]
		public static IGeometry GetZChanges([NotNull] IGeometry sourceGeometry,
		                                    [NotNull] IGeometry updatedGeometry,
		                                    double zTolerance)
		{
			if (updatedGeometry is IPoint updatedPoint)
			{
				var sourcePoint = (IPoint) sourceGeometry;

				return GeomTopoOpUtils.IsDifferentZ(sourcePoint.Z, updatedPoint.Z, zTolerance)
					       ? updatedPoint
					       : null;
			}

			double xyTolerance = GeometryUtils.GetXyTolerance(updatedGeometry);

			if (updatedGeometry is IMultipoint)
			{
				var sourcePoints =
					new Multipoint<Pnt3D>(GeometryConversionUtils.GetPnts3D(sourceGeometry));

				List<IPnt> changedPoints =
					GeometryConversionUtils.GetPnts3D(updatedGeometry)
					                       .Where(p => IsZChanged(p, sourcePoints, xyTolerance,
					                                              zTolerance))
					                       .Cast<IPnt>()
					                       .ToList();

				return changedPoints.Count == 0
					       ? null
					       : (IGeometry) GeometryConversionUtils.CreatePointCollection(
						       updatedGeometry, changedPoints);
			}

			if (updatedGeometry is IPolycurve updatedPolycurve)
			{
				IPolyline result = IntersectionUtils.GetZOnlyDifferenceLines(
					updatedPolycurve, (IPolycurve) sourceGeometry, xyTolerance, zTolerance);

				return result.IsEmpty ? null : result;
			}

			throw new NotImplementedException(
				$"Z changes are not supported for geometry type {updatedGeometry.GeometryType}");
		}

		/// <summary>
		/// Creates a geometry that has the Z values of the updated geometry only at the vertices
		/// that are part of the specified changes (as calculated by <see cref="GetZChanges"/>).
		/// All other vertices keep the Z values of the source geometry. Vertices of the updated
		/// geometry that do not exist in the source geometry (e.g. added by draping) are only
		/// kept if they are part of the specified changes.
		/// </summary>
		/// <param name="sourceGeometry">The original geometry.</param>
		/// <param name="updatedGeometry">The geometry with all Z values updated.</param>
		/// <param name="changesToApply">The subset of changes to apply.</param>
		/// <returns></returns>
		[NotNull]
		public static IGeometry ApplyZChanges([NotNull] IGeometry sourceGeometry,
		                                      [NotNull] IGeometry updatedGeometry,
		                                      [NotNull] IGeometry changesToApply)
		{
			double xyTolerance = GeometryUtils.GetXyTolerance(updatedGeometry);

			// The changes could have been snapped to the resolution in the meanwhile. Use the
			// normal tolerance to identify the changed vertices:
			double zTolerance = GeometryUtils.GetZTolerance(updatedGeometry);

			if (double.IsNaN(zTolerance))
			{
				zTolerance = xyTolerance;
			}

			var changedPoints = new Multipoint<Pnt3D>(GetPnts3D(changesToApply));

			if (updatedGeometry is IPoint updatedPoint)
			{
				Pnt3D updatedPnt = GetPnts3D(updatedPoint).Single();

				return FindClosestPoint(changedPoints, updatedPnt, xyTolerance) == null
					       ? sourceGeometry
					       : updatedGeometry;
			}

			var sourcePoints = new Multipoint<Pnt3D>(GetPnts3D(sourceGeometry));

			if (updatedGeometry is IMultipoint)
			{
				List<IPnt> resultPoints =
					GetResultVertices(GeometryConversionUtils.GetPnts3D(updatedGeometry),
					                  changedPoints, sourcePoints, xyTolerance, zTolerance)
						.Cast<IPnt>().ToList();

				return (IGeometry) GeometryConversionUtils.CreatePointCollection(
					updatedGeometry, resultPoints);
			}

			if (updatedGeometry is IPolycurve updatedPolycurve)
			{
				MultiPolycurve updatedLinestrings =
					GeometryConversionUtils.CreateMultiPolycurve(updatedPolycurve);

				var resultLinestrings = new List<Linestring>();

				foreach (Linestring linestring in updatedLinestrings.GetLinestrings())
				{
					List<Pnt3D> resultVertices =
						GetResultVertices(linestring.GetPoints(), changedPoints, sourcePoints,
						                  xyTolerance, zTolerance).ToList();

					resultLinestrings.Add(new Linestring(resultVertices));
				}

				return CreateAoGeometry(resultLinestrings, updatedPolycurve);
			}

			throw new NotImplementedException(
				$"Z changes are not supported for geometry type {updatedGeometry.GeometryType}");
		}

		private static IEnumerable<Pnt3D> GetPnts3D([NotNull] IGeometry geometry)
		{
			if (geometry is IPoint point)
			{
				return new[] { new Pnt3D(point.X, point.Y, point.Z) };
			}

			return GeometryConversionUtils.GetPnts3D(geometry);
		}

		private static IEnumerable<Pnt3D> GetResultVertices(
			[NotNull] IEnumerable<Pnt3D> updatedVertices,
			[NotNull] Multipoint<Pnt3D> changedPoints,
			[NotNull] Multipoint<Pnt3D> sourcePoints,
			double xyTolerance,
			double zTolerance)
		{
			foreach (Pnt3D updatedVertex in updatedVertices)
			{
				if (FindClosestPoint(changedPoints, updatedVertex, xyTolerance,
				                     p => ! GeomTopoOpUtils.IsDifferentZ(
					                          p.Z, updatedVertex.Z, zTolerance)) != null)
				{
					yield return updatedVertex;
					continue;
				}

				Pnt3D sourceVertex = FindClosestPoint(sourcePoints, updatedVertex, xyTolerance);

				if (sourceVertex == null)
				{
					// Additional vertex that is not part of the applied changes
					continue;
				}

				yield return new Pnt3D(updatedVertex.X, updatedVertex.Y, sourceVertex.Z);
			}
		}

		private static bool IsZChanged([NotNull] Pnt3D updatedPoint,
		                               [NotNull] Multipoint<Pnt3D> sourcePoints,
		                               double xyTolerance,
		                               double zTolerance)
		{
			Pnt3D sourcePoint = FindClosestPoint(sourcePoints, updatedPoint, xyTolerance);

			return sourcePoint == null ||
			       GeomTopoOpUtils.IsDifferentZ(sourcePoint.Z, updatedPoint.Z, zTolerance);
		}

		[CanBeNull]
		private static Pnt3D FindClosestPoint([NotNull] Multipoint<Pnt3D> inPoints,
		                                      [NotNull] Pnt3D searchPoint,
		                                      double xyTolerance,
		                                      [CanBeNull] Predicate<Pnt3D> predicate = null)
		{
			Pnt3D result = null;
			double minDistance = double.MaxValue;

			foreach (int index in inPoints.FindPointIndexes(searchPoint, xyTolerance))
			{
				var candidate = (Pnt3D) inPoints.GetPoint(index);

				if (predicate != null && ! predicate(candidate))
				{
					continue;
				}

				double distance = candidate.GetDistance(searchPoint, true);

				if (distance < minDistance)
				{
					minDistance = distance;
					result = candidate;
				}
			}

			return result;
		}

		private static void
			AssignZsWithin(IEnumerable<Pnt3D> toPoints, double z, IBoundedXY aoi, double tolerance)
		{
			foreach (Pnt3D point in toPoints)
			{
				if (! GeomRelationUtils.BoundsContainXY(aoi, point, tolerance))
				{
					continue;
				}

				if (aoi is ISegmentList polygon &&
				    ! GeomRelationUtils.PolycurveContainsXY(polygon, point, tolerance))
				{
					continue;
				}

				point.Z = z;
			}
		}

		private static void OffsetZsWithin(IEnumerable<Pnt3D> toPoints, double zOffset,
		                                   IBoundedXY aoi, double tolerance)
		{
			foreach (Pnt3D point in toPoints)
			{
				if (! GeomRelationUtils.BoundsContainXY(aoi, point, tolerance))
				{
					continue;
				}

				if (aoi is ISegmentList polygon &&
				    ! GeomRelationUtils.PolycurveContainsXY(polygon, point, tolerance))
				{
					continue;
				}

				point.Z += zOffset;
			}
		}

		[CanBeNull]
		private static T Interpolate<T>([NotNull] T geometry,
		                                [CanBeNull] NotificationCollection notifications)
			where T : IGeometry
		{
			var polyline = geometry as IPolyline;
			if (polyline == null)
			{
				NotificationUtils.Add(
					notifications,
					"Interpolate is supported for polygons but only for sub-sections of the boundary. " +
					"Select a part of the boundary to be interpolated.");

				return default;
			}

			return (T) GeometryUtils.InterpolateZ(polyline);
		}

		private static IGeometry Drape([NotNull] IGeometry shape,
		                               [NotNull] ISimpleSurface surface,
		                               [CanBeNull] IGeometry areaOfInterest,
		                               double drapeTolerance)
		{
			if (shape is IPoint || shape is IMultipoint)
			{
				throw new InvalidOperationException(
					$"Draping is not supported for shape type {shape.GeometryType}");
			}

			if (! TryUpdateShapeVerticesZ(shape, surface, areaOfInterest))
			{
				return null;
			}

			// Intermediate vertices:

			IBoundedXY aoiEnvelope = GetAoiBounds(areaOfInterest);

			if (shape is IPolycurve polycurve)
			{
				return DrapePolycurve(polycurve, surface, drapeTolerance, aoiEnvelope);
			}

			throw new InvalidOperationException(
				$"Draping is not supported for shape type {shape.GeometryType}");
		}

		private static IBoundedXY GetAoiBounds(IGeometry areaOfInterest)
		{
			MultiPolycurve aoiPoly = null;
			if (areaOfInterest is IPolygon polygon)
			{
				aoiPoly = GeometryConversionUtils.CreateMultiPolycurve(polygon);
			}

			IBoundedXY aoiEnvelope = null;

			if (aoiPoly != null)
			{
				aoiEnvelope = aoiPoly;
			}
			else if (areaOfInterest is IEnvelope envelope)
			{
				aoiEnvelope = GeometryConversionUtils.CreateEnvelopeXY(envelope);
			}

			return aoiEnvelope;
		}

		private static IGeometry DrapePolycurve(IPolycurve polycurve, ISimpleSurface ontoSurface,
		                                        double drapeTolerance, IBoundedXY aoi)
		{
			List<Linestring> drapedLinestrings = new List<Linestring>();

			double tolerance = GeometryUtils.GetXyTolerance(polycurve);

			foreach (IPath path in GeometryUtils.GetPaths(polycurve))
			{
				Linestring linestring = GeometryConversionUtils.GetLinestring(path);

				if (aoi != null &&
				    GeomRelationUtils.AreBoundsDisjoint(linestring, aoi, 0))
				{
					// Keep it as is
					drapedLinestrings.Add(linestring);
					continue;
				}

				var densifiedCoordinates = new List<Pnt3D>();
				foreach (Line3D segment in linestring)
				{
					double length2D = segment.Length2D;
					double count = Math.Floor(length2D / drapeTolerance);

					densifiedCoordinates.Add(segment.StartPoint);

					if (aoi == null || IsInAreaOfInterest(segment, aoi, tolerance))
					{
						Pnt3D previousPoint = null;
						double pieceLength = length2D / count;

						for (int i = 1; i < count; i++)
						{
							Pnt3D pointAlong = segment.GetPointAlong(pieceLength * i, false);

							double originalZ = pointAlong.Z;

							if (! TryUpdatePointZ(pointAlong, ontoSurface))
							{
								throw new ArgumentException(
									$"No Z value available at {pointAlong.X}|{pointAlong.Y}");
							}

							double actualZ = pointAlong.Z;

							if (MathUtils.AreEqual(originalZ, actualZ, drapeTolerance))
							{
								continue;
							}

							if (previousPoint != null)
							{
								Line3D currentSegment = new Line3D(previousPoint, segment.EndPoint);

								Pnt3D thisPointOnCurrentSegment =
									currentSegment.GetPointAlong(pieceLength, false);

								if (MathUtils.AreEqual(thisPointOnCurrentSegment.Z, actualZ,
								                       drapeTolerance))
								{
									continue;
								}

								densifiedCoordinates.Add(pointAlong);
							}

							previousPoint = pointAlong;
						}
					}

					densifiedCoordinates.Add(segment.EndPoint);
				}

				drapedLinestrings.Add(new Linestring(densifiedCoordinates));
			}

			return CreateAoGeometry(drapedLinestrings, polycurve);
		}

		private static IGeometry CreateAoGeometry(IEnumerable<Linestring> linestrings,
		                                          IPolycurve templateGeometry)
		{
			if (templateGeometry is IPolygon templatePoly)
			{
				return GeometryConversionUtils.CreatePolygon(templatePoly, linestrings);
			}

			if (templateGeometry is IPolyline)
			{
				return GeometryConversionUtils.CreatePolyline(
					linestrings, templateGeometry.SpatialReference);
			}

			throw new AssertionException("Unexpected polycurve type");
		}

		private static IGeometry CreateAoGeometry(Multipoint<IPnt> pnts,
		                                          IMultipoint templateGeometry)
		{
			return (IGeometry) GeometryConversionUtils.CreatePointCollection(
				templateGeometry, pnts.GetPoints());
		}

		private static bool IsInAreaOfInterest([NotNull] Line3D segment,
		                                       [NotNull] IBoundedXY aoi,
		                                       double tolerance)
		{
			if (GeomRelationUtils.AreBoundsDisjoint(segment, aoi, tolerance))
			{
				return false;
			}

			if (aoi is ISegmentList polyAoi)
			{
				if (GeomRelationUtils.PolycurveContainsXY(polyAoi, segment.StartPoint, tolerance))
				{
					return true;
				}

				if (GeomRelationUtils.PolycurveContainsXY(polyAoi, segment.EndPoint, tolerance))
				{
					return true;
				}
			}

			return false;
		}

		// TODO: Use some of these methods also from the SimpleRasterSurface
		private static bool TryUpdateShapeVerticesZ([NotNull] IGeometry shape,
		                                            [NotNull] ISimpleSurface surface,
		                                            [CanBeNull] IGeometry areaOfInterest)
		{
			GeometryUtils.MakeZAware(shape);

			if (shape is IPoint point)
			{
				return TryUpdatePointZ(point, surface);
			}

			return TryUpdateExistingVerticesPreserveAttributes(shape, surface, areaOfInterest);
		}

		private static bool TryUpdateExistingVerticesPreserveAttributes([NotNull] IGeometry shape,
			[NotNull] ISimpleSurface surface,
			[CanBeNull] IGeometry areaOfInterest)
		{
			IPointCollection pointCollection = (IPointCollection) shape;

			int pointCount = pointCollection.PointCount;

			for (int i = 0; i < pointCount; i++)
			{
				IPoint queryPoint = _queryPoint.Value;

				pointCollection.QueryPoint(i, queryPoint);

				if (areaOfInterest != null && ! GeometryUtils.Contains(areaOfInterest, queryPoint))
				{
					continue;
				}

				if (! TryUpdatePointZ(queryPoint, surface))
				{
					return false;
				}

				pointCollection.UpdatePoint(i, queryPoint);
			}

			return true;
		}

		private static bool TryGetZ(double x, double y, [NotNull] ISimpleSurface surface,
		                            out double z)
		{
			z = surface.GetZ(x, y);

			if (double.IsNaN(z))
			{
				_msg.DebugFormat("No valid Z value at {0}|{1}", x, y);

				return false;
			}

			return true;
		}

		private static bool TryUpdatePointZ([NotNull] IPoint point,
		                                    [NotNull] ISimpleSurface surface)
		{
			if (! TryGetZ(point.X, point.Y, surface, out double z))
			{
				return false;
			}

			point.Z = z;

			return true;
		}

		private static bool TryUpdatePointZ([NotNull] Pnt3D point,
		                                    [NotNull] ISimpleSurface surface)
		{
			if (! TryGetZ(point.X, point.Y, surface, out double z))
			{
				return false;
			}

			point.Z = z;

			return true;
		}
	}
}
