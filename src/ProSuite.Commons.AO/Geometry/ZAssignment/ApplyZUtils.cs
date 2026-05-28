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
