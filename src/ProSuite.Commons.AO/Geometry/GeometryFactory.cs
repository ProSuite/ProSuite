using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry
{
	[CLSCompliant(false)]
	public static class GeometryFactory
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private static object _emptyRef = Type.Missing;

		[NotNull]
		public static IMultipoint CreateMultipoint([NotNull] params IPoint[] points)
		{
			return CreateMultipoint((IEnumerable<IPoint>) points);
		}

		/// <summary>
		/// Creates a multipoint from a point collection.
		/// </summary>
		/// <param name="points">The point collection. Must also implement <see cref="IGeometry"></see></param>
		/// <returns></returns>
		[NotNull]
		public static IMultipoint CreateMultipoint([NotNull] IPointCollection points)
		{
			Assert.ArgumentNotNull(points, nameof(points));

			var geometry = (IGeometry) points;

			IMultipoint multipoint = new MultipointClass();
			var multiPoints = (IPointCollection) multipoint;

			multipoint.SpatialReference = geometry.SpatialReference;
			((IZAware) multipoint).ZAware = ((IZAware) geometry).ZAware;
			((IMAware) multipoint).MAware = ((IMAware) geometry).MAware;

			multiPoints.AddPointCollection(points);

			return multipoint;
		}

		[NotNull]
		public static IMultipoint CreateMultipoint([NotNull] IEnumerable<IPoint> points)
		{
			object missing = Type.Missing;

			IMultipoint result = new MultipointClass();
			var multiPoints = (IPointCollection) result;

			var firstPoint = true;
			foreach (IPoint point in points)
			{
				if (firstPoint)
				{
					result.SpatialReference = point.SpatialReference;

					((IZAware) result).ZAware = ((IZAware) point).ZAware;
					((IMAware) result).MAware = ((IMAware) point).MAware;

					firstPoint = false;
				}

				multiPoints.AddPoint(point, ref missing, ref missing);
			}

			return result;
		}

		/// <summary>
		/// Creates a multipoint with the properties (spatial reference, m-aware, z-aware)
		/// as defined in the provided geometryDef.
		/// </summary>
		/// <param name="pointZs">The list of points.</param>
		/// <param name="geometryDef">The geometry definition.</param>
		/// <returns></returns>
		[NotNull]
		public static IMultipoint CreateMultipoint([NotNull] IList<WKSPointZ> pointZs,
		                                           [CanBeNull] IGeometryDef geometryDef)
		{
			var pointZsArray = new WKSPointZ[pointZs.Count];
			pointZs.CopyTo(pointZsArray, 0);

			return CreateMultipoint(pointZsArray, geometryDef);
		}

		/// <summary>
		/// Creates a multipoint with the properties (spatial reference, m-aware, z-aware)
		/// as defined in the provided geometryDef.
		/// </summary>
		/// <param name="pointZs">The list of points.</param>
		/// <param name="geometryDef">The geometry definition.</param>
		/// <returns></returns>
		[NotNull]
		public static IMultipoint CreateMultipoint([NotNull] WKSPointZ[] pointZs,
		                                           [CanBeNull] IGeometryDef geometryDef)
		{
			Assert.ArgumentNotNull(pointZs, nameof(pointZs));

			IGeometry multipoint = new MultipointClass();

			IGeometry targetMultipoint = null;

			if (geometryDef != null)
			{
				GeometryUtils.EnsureSchemaZM(multipoint, geometryDef,
				                             out targetMultipoint);

				targetMultipoint.SpatialReference = geometryDef.SpatialReference;
			}

			if (targetMultipoint == null)
			{
				targetMultipoint = multipoint;
			}

			GeometryUtils.AddWKSPointZs((IPointCollection4) targetMultipoint, pointZs);

			return (IMultipoint) targetMultipoint;
		}

		/// <summary>
		/// Creates a Z-aware multipoint based on a x,y,z WKS array.
		/// </summary>
		/// <param name="pointZs">The list of points.</param>
		/// <param name="spatialReference">The spatial reference (optional).</param>
		/// <returns></returns>
		[NotNull]
		public static IMultipoint CreateMultipoint(
			[NotNull] WKSPointZ[] pointZs,
			[CanBeNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(pointZs, nameof(pointZs));

			IMultipoint multipoint = new MultipointClass();

			if (spatialReference != null)
			{
				multipoint.SpatialReference = spatialReference;
			}

			GeometryUtils.AddWKSPointZs((IPointCollection4) multipoint, pointZs);

			GeometryUtils.MakeZAware(multipoint);

			return multipoint;
		}

		[NotNull]
		public static IPolygon CreatePolygon(
			WKSEnvelope wksEnvelope,
			[CanBeNull] ISpatialReference spatialReference = null)
		{
			return CreatePolygon(wksEnvelope.XMin,
			                     wksEnvelope.YMin,
			                     wksEnvelope.XMax,
			                     wksEnvelope.YMax);
		}

		[NotNull]
		public static IPolygon CreatePolygon(
			[NotNull] EnvelopeXY e,
			[CanBeNull] ISpatialReference spatialReference = null)
		{
			return CreatePolygon(e.XMin, e.YMin, e.XMax, e.YMax, spatialReference);
		}

		[NotNull]
		public static IPolygon CreatePolygon(
			double xmin, double ymin,
			double xmax, double ymax,
			[CanBeNull] ISpatialReference spatialReference = null)
		{
			return CreatePolygon(CreateEnvelope(xmin, ymin, xmax, ymax, spatialReference));
		}

		[NotNull]
		public static IPolygon CreatePolygon(double xmin, double ymin, double xmax,
		                                     double ymax, double constantZ)
		{
			IEnvelope envelope = CreateEnvelope(xmin, ymin, xmax, ymax);
			IPolygon polygon = CreatePolygon(envelope);

			MakeZAware(polygon);

			((IZ) polygon).SetConstantZ(constantZ);

			return polygon;
		}

		[NotNull]
		public static IPolygon CreatePolygon([NotNull] IPoint minCorner,
		                                     [NotNull] IPoint maxCorner)
		{
			Assert.ArgumentNotNull(minCorner, nameof(minCorner));
			Assert.ArgumentNotNull(maxCorner, nameof(maxCorner));

			IEnvelope envelope = new EnvelopeClass();
			envelope.PutCoords(minCorner.X, minCorner.Y, maxCorner.X, maxCorner.Y);

			return CreatePolygon(envelope);
		}

		[NotNull]
		public static IPolygon CreatePolygon([NotNull] IEnvelope envelope)
		{
			Assert.ArgumentNotNull(envelope, nameof(envelope));

			IPolygon polygon = new PolygonClass();

			SetRectangle(polygon, envelope);

			return polygon;
		}

		/// <summary>
		/// Creates a polygon based on a collection of paths.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="zAware">if set to <c>true</c> the result will be z-aware.</param>
		/// <param name="mAware">if set to <c>true</c> the result will be m-aware.</param>
		/// <param name="paths">The paths.</param>
		/// <returns></returns>
		/// <remarks>
		///   <list>
		///   <item>The spatial reference of the polygon is defined based on the first path in the collection.</item>
		///   <item>The result is not simplified.</item>
		///   </list>
		/// </remarks>
		[NotNull]
		public static IPolygon CreatePolygon<T>(bool zAware, bool mAware,
		                                        params T[] paths) where T : IPath
		{
			var result = new PolygonClass();
			var parts = (IGeometryCollection) result;

			if (paths.Length > 0)
			{
				result.SpatialReference = paths[0].SpatialReference;
			}

			object missing = Type.Missing;
			foreach (T path in paths)
			{
				parts.AddGeometry(Clone(path), ref missing, ref missing);
			}

			((IZAware) result).ZAware = zAware;
			((IMAware) result).MAware = mAware;

			return result;
		}

		/// <summary>
		/// Creates a polygon from a collection of rings.
		/// </summary>
		/// <param name="ringCollection"></param>
		/// <param name="spatialReference"></param>
		/// <param name="makeZAware"></param>
		/// <param name="makeMAware"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolygon CreatePolygon(
			[NotNull] ICollection<IRing> ringCollection,
			[CanBeNull] ISpatialReference spatialReference = null,
			[CanBeNull] bool? makeZAware = null,
			[CanBeNull] bool? makeMAware = null)
		{
			Assert.ArgumentNotNull(ringCollection, nameof(ringCollection));
			Assert.ArgumentCondition(ringCollection.Count > 0,
			                         "pathCollection must contain at least 1 element");

			IPolygon result = null;

			object missing = Type.Missing;

			foreach (IRing ring in ringCollection)
			{
				if (spatialReference == null)
				{
					spatialReference = ring.SpatialReference;
				}

				if (makeZAware == null)
				{
					makeZAware = GeometryUtils.IsZAware(ring);
				}

				if (makeMAware == null)
				{
					makeMAware = GeometryUtils.IsMAware(ring);
				}

				IPath ringToAdd = Clone(ring);

				if (result == null)
				{
					result = CreatePolygon((ISegmentCollection) ringToAdd, spatialReference,
					                       makeZAware, makeMAware);
				}
				else
				{
					((IGeometryCollection) result).AddGeometry(ringToAdd, ref missing, ref missing);
				}
			}

			return Assert.NotNull(result);
		}

		/// <summary>
		/// Creates a polygon from a collection of paths.
		/// </summary>
		/// <param name="pathCollection"></param>
		/// <param name="spatialReference"></param>
		/// <param name="makeZAware"></param>
		/// <param name="makeMAware"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolygon CreatePolygon(
			[NotNull] ICollection<IPath> pathCollection,
			[CanBeNull] ISpatialReference spatialReference = null,
			[CanBeNull] bool? makeZAware = null,
			[CanBeNull] bool? makeMAware = null)
		{
			Assert.ArgumentNotNull(pathCollection, nameof(pathCollection));
			Assert.ArgumentCondition(pathCollection.Count > 0,
			                         "pathCollection must contain at least 1 element");

			IPolygon result = null;

			foreach (IPath path in pathCollection)
			{
				if (spatialReference == null)
				{
					spatialReference = path.SpatialReference;
				}

				if (makeZAware == null)
				{
					makeZAware = GeometryUtils.IsZAware(path);
				}

				if (makeMAware == null)
				{
					makeMAware = GeometryUtils.IsMAware(path);
				}

				IPath pathToAdd = Clone(path);

				// Consider parameter to close each path individually. Currently the paths must aggregate into
				// closed rings to build an actual polygon.
				if (result == null)
				{
					result = CreatePolygon(spatialReference, makeZAware, makeMAware);
				}

				((ISegmentCollection) result).AddSegmentCollection((ISegmentCollection) pathToAdd);
			}

			GeometryUtils.Simplify(Assert.NotNull(result));

			return result;
		}

		[NotNull]
		public static IPolyline CreatePolyline(
			[NotNull] EnvelopeXY e,
			[CanBeNull] ISpatialReference spatialReference = null)
		{
			IEnvelope envelope = CreateEnvelope(e, spatialReference);

			return CreatePolyline(envelope);
		}

		[NotNull]
		public static IPolyline CreatePolyline([NotNull] IEnvelope envelope)
		{
			Assert.ArgumentNotNull(envelope, nameof(envelope));

			IPolyline polyline = new PolylineClass();

			SetRectangle(polyline, envelope);

			return polyline;
		}

		public static void SetRectangle([NotNull] IPolyline polyline,
		                                [NotNull] IEnvelope envelope)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));
			Assert.ArgumentNotNull(envelope, nameof(envelope));

			polyline.SpatialReference = envelope.SpatialReference;

			if (IsZAware(envelope))
			{
				MakeZAware(polyline);
			}

			if (envelope.IsEmpty)
			{
				polyline.SetEmpty();
			}
			else
			{
				((ISegmentCollection) polyline).SetRectangle(envelope);
				((ITopologicalOperator) polyline).Simplify();
			}
		}

		public static void SetRectangle([NotNull] IPolygon polygon,
		                                [NotNull] IEnvelope envelope)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));
			Assert.ArgumentNotNull(envelope, nameof(envelope));

			polygon.SpatialReference = envelope.SpatialReference;

			if (IsZAware(envelope))
			{
				MakeZAware(polygon);
			}

			if (envelope.IsEmpty)
			{
				polygon.SetEmpty();
			}
			else
			{
				((ISegmentCollection) polygon).SetRectangle(envelope);
				((ITopologicalOperator) polygon).Simplify();
			}
		}

		/// <summary>
		/// Creates a (simple!) polygon from the specified multipatch geometry
		/// </summary>
		/// <param name="multipatch"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolygon CreatePolygon([NotNull] IMultiPatch multipatch)
		{
			Assert.ArgumentNotNull(multipatch, nameof(multipatch));

			return CreatePolygon(multipatch, multipatch.SpatialReference, null, null);
		}

		/// <summary>
		/// Creates a (simple!) polygon from the specified multipatch geometry
		/// </summary>
		/// <param name="multipatch"></param>
		/// <param name="spatialReference"></param>
		/// <param name="zAware"></param>
		/// <param name="mAware"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolygon CreatePolygon([NotNull] IMultiPatch multipatch,
		                                     [CanBeNull] ISpatialReference spatialReference,
		                                     bool? zAware,
		                                     bool? mAware)
		{
			Assert.ArgumentNotNull(multipatch, nameof(multipatch));

			var boundary = (IPolyline) GeometryUtils.GetBoundary(multipatch);

			if (spatialReference == null)
			{
				spatialReference = multipatch.SpatialReference;
			}

			if (zAware == null)
			{
				zAware = GeometryUtils.IsZAware(multipatch);
			}

			if (mAware == null)
			{
				mAware = GeometryUtils.IsMAware(multipatch);
			}

			IPolygon footprint = CreatePolygon((ISegmentCollection) boundary, spatialReference,
			                                   zAware, mAware);

			// NOTE: in some situations the boundary is not quite simple (despite ITopoOp.get_IsSimpleEx being true)
			const bool allowReorder = true;
			GeometryUtils.Simplify(footprint, allowReorder);

			// NOTE: boundary has Z = 0
			if (GeometryUtils.IsZAware(footprint))
			{
				((IZ) footprint).SetConstantZ(multipatch.Envelope.ZMin);
			}

			return footprint;
		}

		[NotNull]
		public static IPolygon CreateCircularPolygon([NotNull] IPoint center,
		                                             double radius,
		                                             double segmentLength)
		{
			Assert.ArgumentNotNull(center, nameof(center));

			ICircularArc circle = new CircularArcClass();
			circle.PutCoordsByAngle(center, 0, 2 * Math.PI, radius);

			object emptyRef = Type.Missing;

			IPolyline polylline = new PolylineClass();
			((ISegmentCollection) polylline).AddSegment(((ISegment) circle), ref emptyRef,
			                                            ref emptyRef);
			polylline.Densify(segmentLength, 0);
			if (! polylline.IsClosed)
			{
				((IPointCollection) polylline).AddPoint(polylline.FromPoint, ref emptyRef,
				                                        ref emptyRef);
			}

			IPolygon polygon = new PolygonClass();
			((ISegmentCollection) polygon).AddSegmentCollection(
				(ISegmentCollection) polylline);

			return polygon;
		}

		[NotNull]
		private static ICircularArc CreateCircleArc([NotNull] IPoint point, double radius,
		                                            bool isCcw = false)
		{
			IConstructCircularArc constructCircularArc = new CircularArcClass();

			constructCircularArc.ConstructCircle(point, radius, isCcw);

			return (ICircularArc) constructCircularArc;
		}

		[NotNull]
		public static ICircularArc CreateCircularArc([NotNull] IPoint point1,
		                                             [NotNull] IPoint point2,
		                                             [NotNull] IPoint point3)
		{
			var constructionArc = new CircularArc() as IConstructCircularArc;
			Assert.NotNull(constructionArc, "ConstructCircularArc null");

			constructionArc.ConstructThreePoints(point1, point2, point3, false);

			var circularArc = constructionArc as ICircularArc;
			Assert.NotNull(circularArc, "ICircularArc null");

			return circularArc;
		}

		/// <summary>
		/// Creates a polygon in the shape of a circle with one circular arc from the
		/// specified 3 distinct points. Throws an error if some points are not disjoint
		/// to one another.
		/// </summary>
		/// <param name="point1"></param>
		/// <param name="point2"></param>
		/// <param name="point3"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolygon CreateCircleArcPolygon([NotNull] IPoint point1,
		                                              [NotNull] IPoint point2,
		                                              [NotNull] IPoint point3)
		{
			Assert.ArgumentNotNull(point1, nameof(point1));
			Assert.ArgumentNotNull(point2, nameof(point2));
			Assert.ArgumentNotNull(point3, nameof(point3));

			ICircularArc circularArc = CreateCircularArc(point1, point2, point3);

			var circleSegment =
				(ISegment) CreateCircleArc(circularArc.CenterPoint, circularArc.Radius, false);

			ISegmentCollection polygonSegments = new PolygonClass();

			((IPolygon) polygonSegments).SpatialReference = point1.SpatialReference;

			object missing = Type.Missing;
			polygonSegments.AddSegment(circleSegment, ref missing, ref missing);

			return (IPolygon) polygonSegments;
		}

		[NotNull]
		public static IPolygon CreateCircleArcPolygon([NotNull] IPoint point, double radius,
		                                              bool isCcw = false)
		{
			Assert.ArgumentNotNull(point, nameof(point));

			var circularSegment = (ISegment) CreateCircleArc(point, radius, isCcw);

			ISegmentCollection polygonSegments = new PolygonClass();

			((IPolygon) polygonSegments).SpatialReference = point.SpatialReference;

			object missing = Type.Missing;
			polygonSegments.AddSegment(circularSegment, ref missing, ref missing);

			return (IPolygon) polygonSegments;
		}

		/// <summary>
		/// Creates a polygon with outer ring outside of model, inner ring inside of model
		/// </summary>
		/// <param name="geometry">The model.</param>
		/// <param name="outerDistance">The outer distance.</param>
		/// <param name="innerDistance">The inner distance.</param>
		/// <returns></returns>
		[NotNull]
		public static IGeometry CreatePolygonWithHole([NotNull] IGeometry geometry,
		                                              double outerDistance,
		                                              double innerDistance)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			IGeometry resultPolygon = new PolygonClass();
			var resultCollection = (IGeometryCollection) resultPolygon;

			var modelTopoOp = (ITopologicalOperator) geometry;

			IGeometry outerBuffer = modelTopoOp.Buffer(outerDistance);
			IGeometry outerRing = ((IGeometryCollection) outerBuffer).Geometry[0];

			IGeometry innerBuffer = modelTopoOp.Buffer(innerDistance);
			IGeometry innerRing = ((IGeometryCollection) innerBuffer).Geometry[0];

			((IRing) innerRing).ReverseOrientation();

			object missing = Type.Missing;

			resultCollection.AddGeometry(outerRing, ref missing, ref missing);
			resultCollection.AddGeometry(innerRing, ref missing, ref missing);

			resultPolygon.SpatialReference = geometry.SpatialReference;

			return resultPolygon;
		}

		[NotNull]
		public static IEnvelope CreateEnvelope(
			double xmin, double ymin,
			double xmax, double ymax,
			[CanBeNull] ISpatialReference spatialReference = null)
		{
			var envelope = new EnvelopeClass
			               {
				               XMin = xmin,
				               YMin = ymin,
				               XMax = xmax,
				               YMax = ymax
			               };

			if (spatialReference != null)
			{
				envelope.SpatialReference = spatialReference;

				envelope.SnapToSpatialReference();
			}

			return envelope;
		}

		/// <summary>
		/// Creates an envelope centered on a given 3d coordinate, with a given width and height.
		/// The depth of the envelope is 0 (zmin=zmax).
		/// </summary>
		/// <param name="centerX">The center X.</param>
		/// <param name="centerY">The center Y.</param>
		/// <param name="centerZ">The center Z.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="spatialReference">The spatial reference (optional).</param>
		/// <returns></returns>
		[NotNull]
		public static IEnvelope CreateEnvelope(
			double centerX, double centerY, double centerZ,
			double width, double height,
			[CanBeNull] ISpatialReference spatialReference = null)
		{
			double halfWidth = width / 2;
			double halfHeight = height / 2;

			return CreateEnvelope(centerX - halfWidth,
			                      centerY - halfHeight,
			                      centerX + halfWidth,
			                      centerY + halfHeight,
			                      centerZ, centerZ,
			                      spatialReference);
		}

		[NotNull]
		public static IEnvelope CreateEnvelope(
			double xmin, double ymin,
			double xmax, double ymax,
			double zmin, double zmax,
			[CanBeNull] ISpatialReference spatialReference = null)
		{
			IEnvelope envelope = CreateEnvelope(xmin, ymin, xmax, ymax, spatialReference);

			if (! double.IsNaN(zmin) &&
			    ! double.IsNaN(zmax))
			{
				MakeZAware(envelope);

				envelope.ZMin = zmin;
				envelope.ZMax = zmax;
			}

			return envelope;
		}

		[NotNull]
		public static IEnvelope CreateEnvelope(
			[NotNull] EnvelopeXY e,
			[CanBeNull] ISpatialReference spatialReference = null)
		{
			return CreateEnvelope(e.XMin, e.YMin, e.XMax, e.YMax, spatialReference);
		}

		/// <summary>
		/// Creates an envelope as an expanded copy of a prototype envelope.
		/// </summary>
		/// <param name="prototype">The prototype envelope (must not be empty).</param>
		/// <param name="expansionFactor">The expansion factor (1=no expansion, 2=double size).</param>
		/// <returns></returns>
		[NotNull]
		public static IEnvelope CreateEnvelope([NotNull] IEnvelope prototype,
		                                       double expansionFactor)
		{
			Assert.ArgumentNotNull(prototype, nameof(prototype));
			Assert.ArgumentCondition(! prototype.IsEmpty, "prototype must not be empty");

			IEnvelope copy = Clone(prototype);

			const bool asRatio = true;
			copy.Expand(expansionFactor, expansionFactor, asRatio);

			return copy;
		}

		/// <summary>
		/// Creates a z-aware envelope based on a prototype and a zmin and zmax value.
		/// </summary>
		/// <param name="prototype">The prototype envelope (must not be empty).</param>
		/// <param name="zmin">The z min value for the new envelope.</param>
		/// <param name="zmax">The z max value for the new envelope.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnvelope CreateEnvelope([NotNull] IEnvelope prototype,
		                                       double zmin, double zmax)
		{
			Assert.ArgumentNotNull(prototype, nameof(prototype));
			Assert.ArgumentCondition(! prototype.IsEmpty, "prototype must not be empty");

			return CreateEnvelope(prototype.XMin, prototype.YMin,
			                      prototype.XMax, prototype.YMax,
			                      zmin, zmax,
			                      prototype.SpatialReference);
		}

		[NotNull]
		public static IEnvelope CreateEnvelope([NotNull] IPoint center,
		                                       double width, double height)
		{
			Assert.ArgumentNotNull(center, nameof(center));

			if (((IZAware) center).ZAware)
			{
				const double depth = 0;
				return CreateEnvelope(center, width, height, depth);
			}

			double x;
			double y;
			center.QueryCoords(out x, out y);

			double halfWidth = width / 2;
			double halfHeight = height / 2;

			return CreateEnvelope(x - halfWidth, y - halfHeight,
			                      x + halfWidth, y + halfHeight,
			                      center.SpatialReference);
		}

		[NotNull]
		public static IEnvelope CreateEnvelope([NotNull] IPoint center,
		                                       double width, double height,
		                                       double depth)
		{
			Assert.ArgumentNotNull(center, nameof(center));
			Assert.True(((IZAware) center).ZAware, "Point must be Z aware");

			double halfWidth = width / 2;
			double halfHeight = height / 2;
			double halfDepth = depth / 2;

			double x;
			double y;
			center.QueryCoords(out x, out y);
			double z = center.Z;

			return CreateEnvelope(x - halfWidth, y - halfHeight,
			                      x + halfWidth, y + halfHeight,
			                      z - halfDepth, z + halfDepth,
			                      center.SpatialReference);
		}

		[NotNull]
		public static IEnvelope CreateEnvelope(
			WKSEnvelope wksEnvelope,
			[CanBeNull] ISpatialReference spatialReference = null)
		{
			return CreateEnvelope(wksEnvelope.XMin,
			                      wksEnvelope.YMin,
			                      wksEnvelope.XMax,
			                      wksEnvelope.YMax, spatialReference);
		}

		[NotNull]
		public static IGeometryBag CreateBag([NotNull] IEnumerable<IFeature> features)
		{
			Assert.ArgumentNotNull(features, nameof(features));

			var geometries = new List<IGeometry>();
			foreach (IFeature feature in features)
			{
				geometries.Add(feature.ShapeCopy);
			}

			return CreateBag(geometries, CloneGeometry.Never, true);
		}

		[NotNull]
		public static IGeometryBag CreateBag(params IGeometry[] geometries)
		{
			return CreateBag(geometries, CloneGeometry.Always);
		}

		[NotNull]
		public static IGeometryBag CreateBag(
			[NotNull] IEnumerable<IGeometry> geometries,
			CloneGeometry cloneOption = CloneGeometry.Always,
			ISpatialReference spatialReference = null)
		{
			return CreateBag(geometries, cloneOption, spatialReference, false);
		}

		[NotNull]
		public static IGeometryBag CreateBag(
			[NotNull] IEnumerable<IGeometry> geometries,
			CloneGeometry cloneOption,
			bool allowProjectingInput)
		{
			return CreateBag(geometries, cloneOption, null, allowProjectingInput);
		}

		[NotNull]
		public static IGeometryBag CreateBag(
			[NotNull] IEnumerable<IGeometry> geometries,
			CloneGeometry cloneOption,
			[CanBeNull] ISpatialReference spatialReference,
			bool allowProjectingInput)
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));
			// spatialReference may be null

			IGeometryBag bag = new GeometryBagClass();
			if (spatialReference != null)
			{
				bag.SpatialReference = spatialReference;
			}

			var collection = (IGeometryCollection) bag;

			object emptyRef = Type.Missing;

			foreach (IGeometry geometry in geometries)
			{
				if (spatialReference == null)
				{
					spatialReference = geometry.SpatialReference;
					bag.SpatialReference = geometry.SpatialReference;
				}

				bool sameSref = SpatialReferenceUtils.AreEqual(
					bag.SpatialReference, geometry.SpatialReference, true, true);

				IGeometry clone = null;

				if (cloneOption == CloneGeometry.Always)
				{
					clone = Clone(geometry);
				}

				if (! sameSref)
				{
					if (clone != null)
					{
						GeometryUtils.EnsureSpatialReference(clone,
						                                     spatialReference);
					}
					else
					{
						if (cloneOption == CloneGeometry.Never)
						{
							if (allowProjectingInput)
							{
								GeometryUtils.EnsureSpatialReference(geometry,
								                                     spatialReference);
							}
							else
							{
								throw new ArgumentException(
									"All geometries must have the same spatial reference");
							}
						}
						else
						{
							GeometryUtils.EnsureSpatialReference(geometry,
							                                     spatialReference,
							                                     out clone);
						}
					}
				}

				IGeometry itemToAdd = (clone ?? geometry);

				collection.AddGeometry(itemToAdd, ref emptyRef, ref emptyRef);
			}

			return bag;
		}

		/// <summary>
		/// TODO: Method is most likely obsolete
		/// </summary>
		/// <param name="firstPolygon"></param>
		/// <param name="secondPolygon"></param>
		/// <param name="prohibitMultipleExteriorRings"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IPolygon MergePolygons(
			[NotNull] IPolygon firstPolygon,
			[NotNull] IPolygon secondPolygon,
			bool prohibitMultipleExteriorRings = false,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(firstPolygon, nameof(firstPolygon));
			Assert.ArgumentNotNull(secondPolygon, nameof(secondPolygon));

			var relationalOperator = (IRelationalOperator) firstPolygon;

			if (prohibitMultipleExteriorRings && relationalOperator.Disjoint(secondPolygon))
			{
				NotificationUtils.Add(notifications,
				                      "Polygons are disjoint and merge result would have more than one exterior ring");
				return null;
			}

			if (prohibitMultipleExteriorRings &&
			    (firstPolygon.ExteriorRingCount > 1 || secondPolygon.ExteriorRingCount > 1))
			{
				NotificationUtils.Add(notifications,
				                      "One or both input polygons have more than one exterior ring");
				return null;
			}

			GeometryUtils.Simplify(firstPolygon);
			GeometryUtils.Simplify(secondPolygon);

			var topologicalOperator = (ITopologicalOperator) firstPolygon;

			IGeometry mergedGeometry = topologicalOperator.Union(secondPolygon);

			var mergedPolygon = mergedGeometry as IPolygon;
			if (mergedPolygon == null)
				// does this ever happen? probably if the input is empty?
			{
				NotificationUtils.Add(notifications, "The merged output is not a polygon");
				return null;
			}

			if (! GeometryUtils.IsGeometryValid(mergedPolygon, false, notifications))
			{
				return null;
			}

			if (prohibitMultipleExteriorRings && mergedPolygon.ExteriorRingCount > 1)
			{
				NotificationUtils.Add(notifications,
				                      "The merged output polygon has more than one exterior rings");
				return null;
			}

			return mergedPolygon;
		}

		[NotNull]
		public static IPolyline CreatePolyline(
			[CanBeNull] ISpatialReference spatialReference,
			[NotNull] IPoint fromPoint,
			[NotNull] IPoint secondPoint,
			params IPoint[] additionalPoints)
		{
			Assert.ArgumentNotNull(fromPoint, nameof(fromPoint));
			Assert.ArgumentNotNull(secondPoint, nameof(secondPoint));

			IPolyline result = new PolylineClass
			                   {
				                   SpatialReference = spatialReference
			                   };

			var points = (IPointCollection) result;

			object missing = Type.Missing;

			points.AddPoint(fromPoint, ref missing, ref missing);
			points.AddPoint(secondPoint, ref missing, ref missing);

			foreach (IPoint point in additionalPoints)
			{
				points.AddPoint(point, ref missing, ref missing);
			}

			return result;
		}

		[NotNull]
		public static IPolyline CreatePolyline(
			[CanBeNull] ISpatialReference spatialReference,
			[NotNull] ICollection<IPoint> points)
		{
			Assert.ArgumentNotNull(points, nameof(points));
			Assert.ArgumentCondition(points.Count > 1, "Invalid point count: {0}", points.Count);

			IPolyline result = new PolylineClass
			                   {
				                   SpatialReference = spatialReference
			                   };

			var pointCollection = (IPointCollection) result;

			object missing = Type.Missing;

			foreach (IPoint point in points)
			{
				pointCollection.AddPoint(point, ref missing, ref missing);
			}

			return result;
		}

		[NotNull]
		public static IPolyline CreateLine([NotNull] IPoint fromPoint,
		                                   [NotNull] IPoint secondPoint,
		                                   params IPoint[] additionalPoints)
		{
			Assert.ArgumentNotNull(fromPoint, nameof(fromPoint));
			Assert.ArgumentNotNull(secondPoint, nameof(secondPoint));

			IPolyline result = new PolylineClass();

			var points = (IPointCollection) result;

			object missing = Type.Missing;

			points.AddPoint(fromPoint, ref missing, ref missing);
			points.AddPoint(secondPoint, ref missing, ref missing);

			foreach (IPoint point in additionalPoints)
			{
				points.AddPoint(point, ref missing, ref missing);
			}

			result.SpatialReference = fromPoint.SpatialReference;

			return result;
		}

		[NotNull]
		public static T Clone<T>([NotNull] T prototype) where T : IGeometry
		{
			return (T) ((IClone) prototype).Clone();
		}

		[NotNull]
		public static IGeometry CreateEmptyGeometry(esriGeometryType geometryType)
		{
			var factory = (IGeometryFactory3) GeometryUtils.GeometryBridge;

			IGeometry geometry;

			factory.CreateEmptyGeometryByType(geometryType, out geometry);

			return geometry;
		}

		[NotNull]
		public static IGeometry CreateEmptyGeometry([NotNull] IGeometry templateGeometry)
		{
			IGeometry result = CreateEmptyGeometry(templateGeometry.GeometryType);

			result.SpatialReference = templateGeometry.SpatialReference;

			EnsureZM(result, templateGeometry, null, null);

			if (GeometryUtils.IsPointIDAware(templateGeometry))
			{
				GeometryUtils.MakePointIDAware(result);
			}

			return result;
		}

		/// <summary>
		/// Creates a polyline.
		/// </summary>
		/// <param name="baseGeometry">The base geometry.</param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline([CanBeNull] IGeometry baseGeometry)
		{
			ISpatialReference spatialReference = baseGeometry?.SpatialReference;

			return CreatePolyline(baseGeometry, spatialReference);
		}

		/// <summary>
		/// Creates a polyline.
		/// </summary>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline(
			[CanBeNull] ISpatialReference spatialReference)
		{
			return CreatePolyline((IGeometry) null, spatialReference);
		}

		/// <summary>
		/// Creates a polyline from a polygon.
		/// </summary>
		/// <param name="fromPolygon"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline([NotNull] IPolygon fromPolygon)
		{
			return CreatePolyline(fromPolygon, fromPolygon.SpatialReference,
			                      null, null);
		}

		/// <summary>
		/// Creates a polyline from a polygon. Make sure the input polygon is simple.
		/// </summary>
		/// <param name="fromPolygon"></param>
		/// <param name="spatialReference"></param>
		/// <param name="makeZAware"></param>
		/// <param name="makeMAware"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline(
			[NotNull] IPolygon fromPolygon,
			[CanBeNull] ISpatialReference spatialReference,
			[CanBeNull] bool? makeZAware,
			[CanBeNull] bool? makeMAware)
		{
			Assert.ArgumentNotNull(fromPolygon, nameof(fromPolygon));

			// TODO: test with non-simple geometries! Catch block with simple test?
			IGeometry boundary = GeometryUtils.GetTopoOperator(fromPolygon).Boundary;

			GeometryUtils.Simplify(boundary);

			GeometryUtils.EnsureSpatialReference(boundary, spatialReference);

			EnsureZM(boundary, fromPolygon, makeZAware, makeMAware);

			return (IPolyline) boundary;
		}

		/// <summary>
		/// Creates a polyline from a polyline applying the specified spatial reference
		/// and Z/M attributes.
		/// </summary>
		/// <param name="fromPolyline"></param>
		/// <param name="spatialReference"></param>
		/// <param name="makeZAware"></param>
		/// <param name="makeMAware"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline(
			[NotNull] IPolyline fromPolyline,
			[CanBeNull] ISpatialReference spatialReference,
			[CanBeNull] bool? makeZAware,
			[CanBeNull] bool? makeMAware)
		{
			IGeometry clonedPolyline = Clone(fromPolyline);

			GeometryUtils.EnsureSpatialReference(clonedPolyline, spatialReference);

			EnsureZM(clonedPolyline, fromPolyline, makeZAware, makeMAware);

			return (IPolyline) clonedPolyline;
		}

		/// <summary>
		/// Creates an empty polyline geometry.
		/// </summary>
		/// <param name="spatialReference"></param>
		/// <param name="makeZAware"></param>
		/// <param name="makeMAware"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline(
			[CanBeNull] ISpatialReference spatialReference,
			[CanBeNull] bool? makeZAware,
			[CanBeNull] bool? makeMAware)
		{
			IPolyline polyline = new PolylineClass
			                     {
				                     SpatialReference = spatialReference
			                     };

			((IZAware) polyline).ZAware = makeZAware ?? false;
			((IMAware) polyline).MAware = makeMAware ?? false;

			return polyline;
		}

		/// <summary>
		/// Creates a polyline.
		/// </summary>
		/// <param name="baseGeometry">The base geometry.</param>
		/// <param name="spatialReference">The spatial ref.</param>
		/// <param name="makeZAware">The make Z aware.</param>
		/// <param name="makeMAware">The make M aware.</param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline(
			[CanBeNull] IGeometry baseGeometry,
			[CanBeNull] ISpatialReference spatialReference,
			[CanBeNull] bool? makeZAware = null,
			[CanBeNull] bool? makeMAware = null)
		{
			// Allow also high-level geometries as baseGeometry:
			if (baseGeometry is IPolygon)
			{
				// TODO: respect spatial ref and ZM-awareness
				return CreatePolyline((IPolygon) baseGeometry);
			}

			if (baseGeometry is IPolyline)
			{
				return CreatePolyline((IPolyline) baseGeometry,
				                      spatialReference, makeZAware, makeMAware);
			}

			if (baseGeometry is ISegment)
			{
				return CreatePolyline((ISegment) baseGeometry, spatialReference, makeZAware,
				                      makeMAware);
			}

			if (baseGeometry != null)
			{
				var segmentCollection = baseGeometry as ISegmentCollection;

				if (segmentCollection != null)
				{
					return CreatePolyline((ISegmentCollection) baseGeometry, spatialReference,
					                      makeZAware, makeMAware, false);
				}

				throw new ArgumentException(
					@"Geometry is not valid to create polyline.", nameof(baseGeometry));

				// TEST: try without this. It results in exception with single-point lines
				//		 and it is not clear what situations this would be needed for
				//if (baseGeometry is ICurve && ((ICurve) baseGeometry).IsClosed)
				//{
				//    // close the polyline
				//    // TODO REVISE. Isn't it already closed?
				//    ((IPointCollection) polyline).AddPoint(polyline.FromPoint,
				//                                           ref _emptyRef,
				//                                           ref _emptyRef);
				//}
			}

			return CreatePolyline(spatialReference, makeZAware, makeMAware);
		}

		/// <summary>
		/// Creates a polyline.
		/// </summary>
		/// <param name="segmentCollection"></param>
		/// <param name="spatialReference"></param>
		/// <param name="makeZAware"></param>
		/// <param name="makeMAware"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline(
			[NotNull] ISegmentCollection segmentCollection,
			[CanBeNull] ISpatialReference spatialReference,
			bool? makeZAware, bool? makeMAware)
		{
			const bool doNotCloneInput = false;
			return CreatePolyline(segmentCollection, spatialReference, makeZAware, makeMAware,
			                      doNotCloneInput);
		}

		[NotNull]
		public static IPolyline CreatePolyline(
			[NotNull] ISegmentCollection segmentCollection,
			bool doNotCloneInput)
		{
			return CreatePolyline(segmentCollection, null, null, null, doNotCloneInput);
		}

		/// <summary>
		/// Creates a polyline.
		/// </summary>
		/// <param name="segmentCollection"></param>
		/// <param name="spatialReference"></param>
		/// <param name="makeZAware"></param>
		/// <param name="makeMAware"></param>
		/// <param name="doNotCloneInput"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline(
			[NotNull] ISegmentCollection segmentCollection,
			[CanBeNull] ISpatialReference spatialReference,
			bool? makeZAware, bool? makeMAware,
			bool doNotCloneInput)
		{
			if (spatialReference == null)
			{
				spatialReference = ((IGeometry) segmentCollection).SpatialReference;
			}

			IPolyline polyline = new PolylineClass
			                     {
				                     SpatialReference = spatialReference
			                     };

			EnsureZM(polyline, (IGeometry) segmentCollection, makeZAware, makeMAware);

			IGeometry baseGeometry = ! doNotCloneInput
				                         ? Clone((IGeometry) segmentCollection)
				                         : (IGeometry) segmentCollection;

			try
			{
				((ISegmentCollection) polyline).AddSegmentCollection(
					(ISegmentCollection) baseGeometry);
			}
			finally
			{
				if (baseGeometry != segmentCollection)
				{
					Marshal.ReleaseComObject(baseGeometry);
				}
			}

			return polyline;
		}

		/// <summary>
		/// Creates a polyline from a given segment.
		/// </summary>
		/// <param name="segment">The segment</param>
		/// <param name="spatialReference">The spatial reference</param>
		/// <param name="makeZAware">Whether the result should be Z aware or not. 
		/// If null, the segment's data will be evaluated. If both from- and to-points have Z values, it will be Z aware.</param>
		/// <param name="makeMAware">Whether the result should be M aware or not. If null, the segment's data will be evaluated.</param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline([NotNull] ISegment segment,
		                                       [CanBeNull] ISpatialReference
			                                       spatialReference,
		                                       [CanBeNull] bool? makeZAware,
		                                       [CanBeNull] bool? makeMAware)
		{
			// Segments do not implement IZAware - infer from data
			if (makeZAware == null)
			{
				makeZAware = ! (double.IsNaN(segment.FromPoint.Z) ||
				                double.IsNaN(segment.ToPoint.Z));
			}

			if (makeMAware == null)
			{
				makeMAware = ! (double.IsNaN(segment.FromPoint.M) ||
				                double.IsNaN(segment.ToPoint.M));
			}

			IPolyline polyline = CreatePolyline(spatialReference, makeZAware, makeMAware);

			ISegment segmentCopy = Clone(segment);

			try
			{
				((ISegmentCollection) polyline).AddSegment(segmentCopy,
				                                           ref _emptyRef,
				                                           ref _emptyRef);
			}
			finally
			{
				Marshal.ReleaseComObject(segmentCopy);
			}

			return polyline;
		}

		/// <summary>
		/// Creates a polyline from a multipatch consisting of rings. The result is a salad
		/// of all segments of the rings which, in most cases, will be highly un-simple.
		/// </summary>
		/// <param name="multipatch"></param>
		/// <returns></returns>
		[NotNull]
		public static IGeometry CreatePolyline([NotNull] IMultiPatch multipatch)
		{
			Assert.ArgumentNotNull(multipatch, nameof(multipatch));

			IPolyline result = CreatePolyline(multipatch.SpatialReference,
			                                  GeometryUtils.IsZAware(multipatch),
			                                  GeometryUtils.IsMAware(multipatch));

			if (GeometryUtils.IsPointIDAware(multipatch))
			{
				GeometryUtils.MakePointIDAware(result);
			}

			var resultLineCollection = (IGeometryCollection) result;
			var sourceCollection = (IGeometryCollection) multipatch;

			object missing = Type.Missing;

			int partCount = sourceCollection.GeometryCount;
			for (var i = 0; i < partCount; i++)
			{
				var ring = sourceCollection.Geometry[i] as IRing;

				if (ring == null)
				{
					throw new ArgumentException(
						@"The multipatch contains non-ring geometry parts. Cannot create polyline.",
						nameof(multipatch));
				}

				IPath path = CreatePath(ring);

				resultLineCollection.AddGeometry(path, ref missing, ref missing);
			}

			return result;
		}

		/// <summary>
		/// Creates a polyline from a collection of paths or rings.
		/// </summary>
		/// <param name="pathCollection"></param>
		/// <param name="spatialReference"></param>
		/// <param name="makeZAware"></param>
		/// <param name="makeMAware"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline(
			[NotNull] ICollection<IPath> pathCollection,
			[CanBeNull] ISpatialReference spatialReference = null,
			[CanBeNull] bool? makeZAware = null,
			[CanBeNull] bool? makeMAware = null)
		{
			Assert.ArgumentNotNull(pathCollection, nameof(pathCollection));
			Assert.ArgumentCondition(pathCollection.Count > 0,
			                         "pathCollection must contain at least 1 element");

			IPolyline result = null;

			object missing = Type.Missing;

			foreach (IPath path in pathCollection)
			{
				if (spatialReference == null)
				{
					spatialReference = path.SpatialReference;
				}

				if (makeZAware == null)
				{
					makeZAware = GeometryUtils.IsZAware(path);
				}

				if (makeMAware == null)
				{
					makeMAware = GeometryUtils.IsMAware(path);
				}

				IPath pathToAdd =
					path.GeometryType == esriGeometryType.esriGeometryRing
						? CreatePath((IRing) path)
						: Clone(path);

				if (result == null)
				{
					result = CreatePolyline(pathToAdd, spatialReference, makeZAware, makeMAware);
				}
				else
				{
					((IGeometryCollection) result).AddGeometry(pathToAdd, ref missing, ref missing);
				}
			}

			return Assert.NotNull(result);
		}

		[NotNull]
		public static IPolyline CreatePolyline(
			[NotNull] WKSPointZ[] pointZs,
			[CanBeNull] ISpatialReference spatialReference)
		{
			IPolyline polyline = new PolylineClass();
			if (spatialReference != null)
			{
				polyline.SpatialReference = spatialReference;
			}

			GeometryUtils.AddWKSPointZs((IPointCollection4) polyline, pointZs);

			GeometryUtils.MakeZAware(polyline);

			return polyline;
		}

		private static void EnsureZM([NotNull] IGeometry newGeometry,
		                             [NotNull] IGeometry baseGeometry,
		                             bool? makeZAware,
		                             bool? makeMAware)
		{
			if (baseGeometry is IZAware)
			{
				((IZAware) newGeometry).ZAware = makeZAware ?? ((IZAware) baseGeometry).ZAware;
			}

			if (baseGeometry is IMAware)
			{
				((IMAware) newGeometry).MAware = makeMAware ?? ((IMAware) baseGeometry).MAware;
			}
		}

		[NotNull]
		public static IPolyline CreatePolyline([NotNull] IPoint fromPoint,
		                                       [NotNull] IPoint toPoint)
		{
			Assert.ArgumentNotNull(fromPoint, nameof(fromPoint));
			Assert.ArgumentNotNull(toPoint, nameof(toPoint));
			Assert.ArgumentCondition(! fromPoint.IsEmpty, "from point is empty");
			Assert.ArgumentCondition(! toPoint.IsEmpty, "to point is empty");

			var result = new PolylineClass {SpatialReference = fromPoint.SpatialReference};

			if (GeometryUtils.IsZAware(fromPoint) && GeometryUtils.IsZAware(toPoint))
			{
				GeometryUtils.MakeZAware(result);
			}

			if (GeometryUtils.IsMAware(fromPoint) && GeometryUtils.IsMAware(toPoint))
			{
				GeometryUtils.MakeMAware(result);
			}

			object o = Type.Missing;

			result.AddPoint(Clone(fromPoint), ref o, ref o);
			result.AddPoint(Clone(toPoint), ref o, ref o);

			return result;
		}

		/// <summary>
		/// Creates a ring from the specified path that allows specifying the orientation.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="interior"></param>
		/// <returns></returns>
		[NotNull]
		public static IRing CreateRing([NotNull] IPath path, bool interior)
		{
			IRing result = CreateRing(path);

			if (result.IsExterior == interior)
			{
				result.ReverseOrientation();
			}

			return result;
		}

		/// <summary>
		/// Creates a ring from the specified path. The ring has the same orientation as the path.
		/// The path can be non-simple (e.g. consisting of a single point).
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[NotNull]
		public static IRing CreateRing([NotNull] IPath path)
		{
			IRing ring = new RingClass();

			ConfigureGeometry(ring, path);

			IPath pathClone = Clone(path);

			var pathSegments = (ISegmentCollection) pathClone;

			if (pathSegments.SegmentCount > 0)
			{
				((ISegmentCollection) ring).AddSegmentCollection(pathSegments);
			}
			else if (((IPointCollection) pathClone).PointCount > 0)
			{
				// Single-point path
				((IPointCollection) ring).AddPointCollection((IPointCollection) pathClone);
			}

			if (! ring.IsClosed)
			{
				ring.Close();
			}

			return ring;
		}

		[NotNull]
		public static IRing CreateRing([NotNull] WKSPointZ[] wksPoints,
		                               [CanBeNull] ISpatialReference spatialReference = null)
		{
			Assert.ArgumentNotNull(wksPoints, nameof(wksPoints));

			IRing ring = new RingClass();
			((IZAware) ring).ZAware = true;

			GeometryUtils.SetWKSPointZs(ring, wksPoints);

			ring.SpatialReference = spatialReference;
			return ring;
		}

		[NotNull]
		public static IPath CreatePath([NotNull] IRing ring)
		{
			IPath path = new PathClass();

			ConfigureGeometry(path, ring);

			IRing ringClone = Clone(ring);

			((ISegmentCollection) path).AddSegmentCollection((ISegmentCollection) ringClone);

			return path;
		}

		[NotNull]
		public static IPath CreatePath([NotNull] IPoint fromPoint, [NotNull] IPoint toPoint)
		{
			IPath path = new PathClass
			             {
				             FromPoint = fromPoint,
				             ToPoint = toPoint,
				             SpatialReference = fromPoint.SpatialReference
			             };

			if (GeometryUtils.IsZAware(fromPoint) && GeometryUtils.IsZAware(toPoint))
			{
				GeometryUtils.MakeZAware(path);
			}

			if (GeometryUtils.IsMAware(fromPoint) && GeometryUtils.IsMAware(toPoint))
			{
				GeometryUtils.MakeMAware(path);
			}

			return path;
		}

		[NotNull]
		public static IPath CreatePath([NotNull] params IPoint[] vertices)
		{
			return CreatePath(vertices.AsEnumerable());
		}

		public static IPath CreatePath(IEnumerable<IPoint> vertices)
		{
			IPath result = new PathClass();

			var points = (IPointCollection) result;

			object missing = Type.Missing;

			var allPointsZAware = true;
			var allPointsMAware = true;
			var allPointsPointIdAware = true;

			ISpatialReference spatialReference = null;

			foreach (IPoint point in vertices)
			{
				if (! GeometryUtils.IsZAware(point))
				{
					allPointsZAware = false;
				}

				if (! GeometryUtils.IsMAware(point))
				{
					allPointsMAware = false;
				}

				if (! GeometryUtils.IsPointIDAware(point))
				{
					allPointsPointIdAware = false;
				}

				if (spatialReference == null)
				{
					spatialReference = point.SpatialReference;
				}

				points.AddPoint(point, ref missing, ref missing);
			}

			result.SpatialReference = spatialReference;

			if (allPointsZAware)
			{
				GeometryUtils.MakeZAware(result);
			}

			if (allPointsMAware)
			{
				GeometryUtils.MakeMAware(result);
			}

			if (allPointsPointIdAware)
			{
				GeometryUtils.MakePointIDAware(result);
			}

			return result;
		}

		/// <summary>
		/// Creates a polyline.
		/// </summary>
		/// <param name="baseGeometry">The base geometry.</param>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <param name="zAware">if set to <c>true</c> the resulting geometry is created as z aware</param>
		/// <returns></returns>
		[NotNull]
		public static IPolyline CreatePolyline(IGeometry baseGeometry,
		                                       ISpatialReference spatialReference,
		                                       bool zAware)
		{
			return CreatePolyline(baseGeometry, spatialReference, zAware, null);
		}

		[NotNull]
		public static IPolyline CreatePolyline(double x1, double y1,
		                                       double x2, double y2)
		{
			IPolyline polyline = new PolylineClass();

			var pointCollection = (IPointCollection4) polyline;

			var pointArray = new WKSPoint[2];
			PutPoint(pointArray, 0, x1, y1);
			PutPoint(pointArray, 1, x2, y2);

			GeometryUtils.SetWKSPoints(pointCollection, pointArray);

			((ITopologicalOperator) polyline).Simplify();

			return polyline;
		}

		[NotNull]
		public static IPolyline CreatePolyline(double x1, double y1, double z1,
		                                       double x2, double y2, double z2,
		                                       bool dontSimplify = false)
		{
			IPolyline polyline = new PolylineClass();
			MakeZAware(polyline);

			var pointCollection = (IPointCollection4) polyline;

			var pointArray = new WKSPointZ[2];
			PutPoint(pointArray, 0, x1, y1, z1);
			PutPoint(pointArray, 1, x2, y2, z2);

			GeometryUtils.SetWKSPointZs(pointCollection, pointArray);

			if (! dontSimplify)
			{
				((ITopologicalOperator) polyline).Simplify();
			}

			return polyline;
		}

		/// <summary>
		/// Creates a polygon.
		/// </summary>
		/// <param name="baseGeometry">The base geometry (optional).</param>
		/// <returns></returns>
		[NotNull]
		public static IPolygon CreatePolygon([CanBeNull] IGeometry baseGeometry)
		{
			return CreatePolygon(baseGeometry, baseGeometry?.SpatialReference);
		}

		/// <summary>
		/// Creates an empty polygon.
		/// </summary>
		/// <param name="spatialReference">The spatial reference for the polygon</param>
		/// <returns></returns>
		[NotNull]
		public static IPolygon CreatePolygon([CanBeNull] ISpatialReference spatialReference)
		{
			return CreatePolygon((IGeometry) null, spatialReference);
		}

		/// <summary>
		/// Creates a polygon.
		/// </summary>
		/// <param name="baseGeometry">The base geometry (optional).</param>
		/// <param name="spatialReference">The spatial ref.</param>
		/// <returns></returns>
		[NotNull]
		public static IPolygon CreatePolygon(
			[CanBeNull] IGeometry baseGeometry,
			[CanBeNull] ISpatialReference spatialReference)
		{
			IPolygon polygon;
			if (baseGeometry == null)
			{
				polygon = new PolygonClass {SpatialReference = spatialReference};

				// TODO always????
				((IZAware) polygon).ZAware = true;

				return polygon;
			}

			var basePolygon = baseGeometry as IPolygon;
			if (basePolygon != null)
			{
				polygon = Clone(basePolygon);
			}
			else
			{
				var baseEnvelope = baseGeometry as IEnvelope;
				if (baseEnvelope != null)
				{
					polygon = CreatePolygon(baseEnvelope);
				}
				else
				{
					var segmentCollection = baseGeometry as ISegmentCollection;

					if (segmentCollection != null)
					{
						polygon = CreatePolygon(segmentCollection, spatialReference, null, null);
					}
					else if (baseGeometry is IMultiPatch)
					{
						polygon = CreatePolygon((IMultiPatch) baseGeometry, spatialReference, true,
						                        GeometryUtils.IsMAware(baseGeometry));
					}
					else
					{
						throw new NotSupportedException(
							string.Format(
								"Unsupported geometry type: {0}. Unable to create a polygon.",
								baseGeometry.GeometryType));
					}
				}
			}

			if (spatialReference != null)
			{
				GeometryUtils.EnsureSpatialReference(polygon, spatialReference);
			}

			return polygon;
		}

		/// <summary>
		/// Creates an empty polygon geometry.
		/// </summary>
		/// <param name="spatialReference"></param>
		/// <param name="makeZAware"></param>
		/// <param name="makeMAware"></param>
		/// <returns></returns>
		[NotNull]
		public static IPolygon CreatePolygon(
			[CanBeNull] ISpatialReference spatialReference,
			[CanBeNull] bool? makeZAware,
			[CanBeNull] bool? makeMAware)
		{
			IPolygon polygon = new PolygonClass
			                   {
				                   SpatialReference = spatialReference
			                   };

			((IZAware) polygon).ZAware = makeZAware ?? false;

			((IMAware) polygon).MAware = makeMAware ?? false;

			return polygon;
		}

		private static IPolygon CreatePolygon(
			[NotNull] ISegmentCollection segments,
			[CanBeNull] ISpatialReference spatialReference,
			bool? zAware,
			bool? mAware)
		{
			Assert.ArgumentNotNull(segments, nameof(segments));

			if (spatialReference == null)
			{
				spatialReference = ((IGeometry) segments).SpatialReference;
			}

			if (zAware == null)
			{
				zAware = GeometryUtils.IsZAware((IGeometry) segments);
			}

			if (mAware == null)
			{
				mAware = GeometryUtils.IsMAware((IGeometry) segments);
			}

			IPolygon polygon = CreatePolygon(spatialReference, zAware, mAware);

			var parts = segments as IGeometryCollection;

			// CreateRing clones the path - no clone needed.
			if (parts != null)
			{
				foreach (IGeometry part in GeometryUtils.GetParts(parts))
				{
					var path = (IPath) part;

					((IGeometryCollection) polygon).AddGeometry(CreateRing(path),
					                                            ref _emptyRef,
					                                            ref _emptyRef);
				}
			}
			else
			{
				var path = (IPath) segments;

				((IGeometryCollection) polygon).AddGeometry(CreateRing(path),
				                                            ref _emptyRef,
				                                            ref _emptyRef);
			}

			return polygon;
		}

		[NotNull]
		public static IPolygon CreatePolygon([NotNull] WKSPointZ[] wksPoints,
		                                     [CanBeNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(wksPoints, nameof(wksPoints));

			IPolygon polygon = new PolygonClass();
			((IZAware) polygon).ZAware = true;

			GeometryUtils.SetWKSPointZs(polygon, wksPoints);

			polygon.SpatialReference = spatialReference;
			return polygon;
		}

		/// <summary>
		/// Creates a point.
		/// </summary>
		/// <param name="wksPointZ"></param>
		/// <returns></returns>
		[NotNull]
		public static IPoint CreatePoint(WKSPointZ wksPointZ)
		{
			return CreatePoint(wksPointZ.X, wksPointZ.Y, wksPointZ.Z);
		}

		/// <summary>
		/// Creates a point.
		/// </summary>
		/// <param name="wksPointZ"></param>
		/// <param name="spatialReference"></param>
		/// <returns></returns>
		[NotNull]
		public static IPoint CreatePoint(WKSPointZ wksPointZ,
		                                 ISpatialReference spatialReference)
		{
			IPoint result = CreatePoint(wksPointZ);

			result.SpatialReference = spatialReference;

			return result;
		}

		/// <summary>
		/// Creates a point.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <returns></returns>
		[NotNull]
		public static IPoint CreatePoint(double x, double y)
		{
			IPoint point = new PointClass();
			point.PutCoords(x, y);

			return point;
		}

		/// <summary>
		/// Creates a z aware point.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		/// <returns></returns>
		[NotNull]
		public static IPoint CreatePoint(double x, double y, double z)
		{
			IPoint point = CreatePoint(x, y);

			MakeZAware(point);

			point.Z = z;

			return point;
		}

		/// <summary>
		/// Creates a z and m aware point.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		/// <param name="m">The m.</param>
		/// <param name="sref"></param>
		/// <returns></returns>
		[NotNull]
		public static IPoint CreatePoint(double x, double y, double z, double m,
		                                 ISpatialReference sref = null)
		{
			IPoint point = CreatePoint(x, y, z);

			point.SpatialReference = sref;

			((IMAware) point).MAware = true;

			point.M = m;

			return point;
		}

		[NotNull]
		public static IPoint CreatePoint(double x, double y,
		                                 [CanBeNull] ISpatialReference sref)
		{
			IPoint result = new PointClass();

			result.PutCoords(x, y);
			result.SpatialReference = sref;

			return result;
		}

		/// <summary>
		/// Creates a multipatch using the rings in the provided polygon. The orientation of the rings,
		/// i.e. the IsExterior property is used to determine whether they are added as inner or outer
		/// rings. Hence non-simple geometries (which cannot be fixed by a Simplify() on the multipatch)
		/// can result if for example only one inner ring is provided.
		/// Non-linear segments are not supported by multipatches and will be left out.
		/// </summary>
		/// <param name="zAwarePolygon"></param>
		/// <returns></returns>
		[NotNull]
		public static IMultiPatch CreateMultiPatch([NotNull] IPolygon zAwarePolygon)
		{
			Assert.ArgumentNotNull(zAwarePolygon, nameof(zAwarePolygon));
			Assert.ArgumentCondition(GeometryUtils.IsZAware(zAwarePolygon),
			                         "Polygon must be z-aware");

			var inputGeometryCollection = (IGeometryCollection) zAwarePolygon;

			IMultiPatch multipatch = new MultiPatchClass();
			((IGeometry) multipatch).SpatialReference = zAwarePolygon.SpatialReference;

			if (GeometryUtils.IsMAware(zAwarePolygon))
			{
				GeometryUtils.MakeMAware(multipatch);
			}

			// important to make sure PointID values are also transferred:
			if (GeometryUtils.IsPointIDAware(zAwarePolygon))
			{
				GeometryUtils.MakePointIDAware(multipatch);
			}

			foreach (IGeometry geometry in GeometryUtils.GetParts(inputGeometryCollection))
			{
				var ring = (IRing) geometry;
				IRing clonedRing = Clone(ring);

				AddRingToMultiPatch(clonedRing, multipatch);
			}

			return multipatch;
		}

		public static IMultiPatch CreateMultiPatch([NotNull] IRing ring)
		{
			IMultiPatch result = CreateEmptyMultiPatch(ring);

			AddRingToMultiPatch(ring, result, esriMultiPatchRingType.esriMultiPatchOuterRing);

			return result;
		}

		[CanBeNull]
		public static IMultiPatch CreateMultiPatch([NotNull] IEnumerable<IRing> outerRings)
		{
			IMultiPatch result = null;

			foreach (IRing ring in outerRings)
			{
				if (result == null)
				{
					result = CreateEmptyMultiPatch(ring);
				}

				AddRingToMultiPatch(ring, result, esriMultiPatchRingType.esriMultiPatchOuterRing);
			}

			return result;
		}

		public static void AddRingToMultiPatch([NotNull] IRing ring,
		                                       [NotNull] IMultiPatch multipatch)
		{
			esriMultiPatchRingType ringType =
				ring.IsExterior
					? esriMultiPatchRingType.esriMultiPatchOuterRing
					: esriMultiPatchRingType.esriMultiPatchInnerRing;

			// Avoid incorrect classification of vertical rings:
			// The problem is that vertical rings do not properly transport their interiour-ness
			// when they are polygon rings (by orientation). Either some different logic is 
			// required to find out whether they are interior or not or a thorough geometric 
			// analysis on the final geometry.
			if (ringType == esriMultiPatchRingType.esriMultiPatchInnerRing &&
			    Math.Abs(((IArea) ring).Area) < double.Epsilon)
			{
				ringType = esriMultiPatchRingType.esriMultiPatchOuterRing;
			}

			AddRingToMultiPatch(ring, multipatch, ringType);
		}

		public static void AddRingToMultiPatch([NotNull] IRing ring,
		                                       [NotNull] IMultiPatch multipatch,
		                                       esriMultiPatchRingType ringType)
		{
			((IGeometryCollection) multipatch).AddGeometry(ring, ref _emptyRef, ref _emptyRef);

			multipatch.PutRingType(ring, ringType);
		}

		[NotNull]
		public static IMultiPatch CreateMultiPatch([NotNull] IGeometry zAwareGeometry,
		                                           double extrusionDistance)
		{
			Assert.ArgumentNotNull(zAwareGeometry, nameof(zAwareGeometry));
			Assert.ArgumentCondition(
				zAwareGeometry.GeometryType == esriGeometryType.esriGeometryPolygon ||
				zAwareGeometry.GeometryType == esriGeometryType.esriGeometryPolyline,
				"Unsupported geometry type to create multipatch");

			var inputGeometryCollection = (IGeometryCollection) zAwareGeometry;

			if (inputGeometryCollection.GeometryCount == 1 ||
			    zAwareGeometry.GeometryType == esriGeometryType.esriGeometryPolyline)
			{
				return ExtrudeGeometry(zAwareGeometry, extrusionDistance);
			}

			// for multipart rings:
			// process each ring separately to avoid simplification (overlapping parts are normally desired in multipatches. 
			// If the output is desired to be polygon-like simple, the input should be simple
			var extrudedRings = new List<IGeometry>();
			foreach (
				IGeometry ring in GeometryUtils.GetParts((IGeometryCollection) zAwareGeometry))
			{
				extrudedRings.Add(ExtrudeGeometry(ring, extrusionDistance));
			}

			return (IMultiPatch) GeometryUtils.Union(extrudedRings);
		}

		// TODO REVISE (generalizedbuffer)
		[NotNull]
		public static IPolygon CreateBuffer(IGeometry geometry, double distance,
		                                    bool knownSimple = false)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.False(geometry.IsEmpty, "Cannot create buffer for empty geometry");

			var cloned = false;
			IEnvelope envelope = geometry.Envelope;
			IGeometry source;

			if (knownSimple || ((ITopologicalOperator) geometry).IsSimple)
			{
				source = geometry;
			}
			else
			{
				source = Clone(geometry);
				((ITopologicalOperator) source).Simplify();
				cloned = true;
			}

			// note: geometry may become empty when calling IsSimple
			if (source.IsEmpty)
			{
				throw new ArgumentException(
					string.Format(
						"Cannot create buffer for invalid geometry. Envelope {0}",
						GeometryUtils.ToString(envelope)));
			}

			var sourceZ = source as IZAware;
			if (sourceZ != null && sourceZ.ZAware)
			{
				if (! sourceZ.ZSimple)
				{
					if (! cloned)
					{
						source = Clone(source);
					}

					((IZ) source).SetConstantZ(0); // TODO revise
				}
			}

			// handle zero length curves / zero area areas
			var curve = source as ICurve;
			if (curve != null && curve.Length <= 0)
			{
				source = curve.FromPoint;
			}
			else
			{
				var area = source as IArea;
				if (area != null && area.Area <= 0)
				{
					source = area.Centroid;
				}
			}

			// create the buffer
			IGeometry buffer;
			try
			{
				buffer = GeometryUtils.GeneralizedBuffer(source, distance);
			}
			catch (Exception)
			{
				_msg.DebugFormat("source: {0}", GeometryUtils.ToString(source));
				throw;
			}

			((ITopologicalOperator) buffer).Simplify();

			return (IPolygon) buffer;
		}

		/// <summary>
		/// Creates the union of an enumeration of geometries. Handles different
		/// geometry types by converting lower-dimensional geometries to the 
		/// highest-dimensional geometry in the enumeration. For this conversion,
		/// an expansion distance is used.
		/// </summary>
		/// <param name="geometries">The geometries.</param>
		/// <param name="expansionDistance">The expansion distance.</param>
		/// <returns></returns>
		[NotNull]
		public static IGeometry CreateUnion([NotNull] IEnumerable<IGeometry> geometries,
		                                    double expansionDistance = 0)
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));

			var input = new List<IGeometry>(geometries);

			var unionResults = new List<IGeometry>();

			foreach (KeyValuePair<esriGeometryType, List<IGeometry>> pair
				in GetGeometriesByType(input))
			{
				unionResults.Add(GeometryUtils.Union(pair.Value));
			}

			if (unionResults.Count == 1)
			{
				return unionResults[0];
			}

			esriGeometryType? targetType = null;
			foreach (IGeometry unionResult in unionResults)
			{
				if (unionResult.GeometryType == esriGeometryType.esriGeometryPolygon ||
				    unionResult.GeometryType == esriGeometryType.esriGeometryMultiPatch)
				{
					targetType = esriGeometryType.esriGeometryPolygon;
					break;
				}

				if (unionResult.GeometryType == esriGeometryType.esriGeometryPolyline)
				{
					targetType = esriGeometryType.esriGeometryPolyline;
				}
				else if (unionResult.GeometryType ==
				         esriGeometryType.esriGeometryMultipoint ||
				         unionResult.GeometryType == esriGeometryType.esriGeometryPoint)
				{
					if (targetType == null)
					{
						targetType = esriGeometryType.esriGeometryMultipoint;
					}
				}
			}

			Assert.NotNull(targetType, "unable to determine target type");

			var unionSameType = new List<IGeometry>();
			foreach (IGeometry unionResult in unionResults)
			{
				unionSameType.Add(GetUnionInput(targetType.Value, unionResult,
				                                expansionDistance));
			}

			return GeometryUtils.Union(unionSameType);
		}

		[CanBeNull]
		public static IRing CreateCircle(IPoint point1, IPoint point2,
		                                 IPoint point3, double minSegmentLenght)
		{
			IPoint centerPoint;
			double radius;
			return CreateCircle(point1, point2, point3, minSegmentLenght,
			                    out radius, out centerPoint);
		}

		[CanBeNull]
		public static IRing CreateCircle(IPoint point1, IPoint point2,
		                                 IPoint point3, double minSegmentLenght,
		                                 out double radius)
		{
			IPoint centerPoint;
			return CreateCircle(point1, point2, point3, minSegmentLenght,
			                    out radius, out centerPoint);
		}

		[CanBeNull]
		public static IRing CreateCircle(IPoint point1, IPoint point2,
		                                 IPoint point3, double minSegmentLenght,
		                                 out double radius,
		                                 out IPoint centerPoint)
		{
			// Make temporary circular arc based on three points
			ICircularArc circularArc = CreateCircularArc(point1, point2, point3);

			Assert.NotNull(circularArc, "ICircularArc null");

			// Fill circlePoints with circular arc extension points 
			IPointCollection circlePoints = new RingClass();

			centerPoint = circularArc.CenterPoint;
			radius = circularArc.Radius;
			double circleBoundary = radius * 2 * Math.PI;

			// Max number of lines in boundary polyline
			var maxParts = (int) (circleBoundary / minSegmentLenght);

			if (maxParts < 2)
			{
				// Circle is too small, cant be connected in network without 
				// violating minimum segment lenght
				return null;
			}

			// Add starting point
			object obj = Type.Missing;
			circlePoints.AddPoint(circularArc.FromPoint, ref obj, ref obj);

			// add other points
			IPoint point = new PointClass();
			double distance = 0;
			for (var parts = 0; parts < maxParts; parts++)
			{
				distance += minSegmentLenght;

				// Ask point in given distance
				circularArc.QueryPoint(esriSegmentExtension.esriExtendEmbedded,
				                       distance, false, point);

				// add point to circle points
				circlePoints.AddPoint(point, ref obj, ref obj);
			}

			// Check if last point too close to first point
			double lastDistance = GeometryUtils.GetPointDistance(
				circlePoints.Point[0],
				circlePoints.Point[circlePoints.PointCount - 1]);

			if (lastDistance < minSegmentLenght)
			{
				circlePoints.RemovePoints(circlePoints.PointCount - 1, 1);
			}

			// close line to ring
			var ring = (IRing) circlePoints;
			ring.Close();

			return ring;
		}

		[NotNull]
		public static IPolyline CreateOutline([NotNull] IEnvelope envelope)
		{
			Assert.ArgumentNotNull(envelope, nameof(envelope));

			if (envelope.IsEmpty)
			{
				return new PolylineClass {SpatialReference = envelope.SpatialReference};
			}

			var polygon = CreatePolygon(envelope);

			return CreateOutline(polygon);
		}

		[NotNull]
		public static IPolyline CreateOutline([NotNull] IPolygon polygon)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			if (polygon.IsEmpty)
			{
				return new PolylineClass {SpatialReference = polygon.SpatialReference};
			}

			return (IPolyline) ((ITopologicalOperator) polygon).Boundary;
		}

		[NotNull]
		public static IPoint CreateEmptyPoint([NotNull] IGeometry prototype)
		{
			Assert.ArgumentNotNull(prototype, nameof(prototype));

			var result = new PointClass();
			ConfigureGeometry(result, prototype);

			Assert.True(result.IsEmpty, "Oops, new PointClass is not empty");

			return result;
		}

		[NotNull]
		public static IPolyline CreateEmptyPolyline([NotNull] IGeometry prototype)
		{
			Assert.ArgumentNotNull(prototype, nameof(prototype));

			var result = new PolylineClass();
			ConfigureGeometry(result, prototype);

			Assert.True(result.IsEmpty, "Oops, new PolylineClass is not empty");

			return result;
		}

		[NotNull]
		public static IPolyline CreateEmptyPolyline([CanBeNull] ISpatialReference sref,
		                                            bool isZAware = false,
		                                            bool isMAware = false,
		                                            bool isIDAware = false)
		{
			var result = new PolylineClass();

			ConfigureGeometry(result, sref, isZAware, isMAware, isIDAware);

			return result;
		}

		[NotNull]
		public static IPolygon CreateEmptyPolygon([NotNull] IGeometry prototype)
		{
			Assert.ArgumentNotNull(prototype, nameof(prototype));

			var result = new PolygonClass();
			ConfigureGeometry(result, prototype);

			Assert.True(result.IsEmpty, "Oops, new PolygonClass is not empty");

			return result;
		}

		[NotNull]
		public static IMultipoint CreateEmptyMultipoint([NotNull] IGeometry prototype)
		{
			Assert.ArgumentNotNull(prototype, nameof(prototype));

			var result = new MultipointClass();
			ConfigureGeometry(result, prototype);

			Assert.True(result.IsEmpty, "Oops, new MultipointClass is not empty");

			return result;
		}

		[NotNull]
		public static IMultiPatch CreateEmptyMultiPatch([NotNull] IGeometry prototype)
		{
			Assert.ArgumentNotNull(prototype, nameof(prototype));

			var result = new MultiPatchClass();

			ConfigureGeometry(result, prototype);

			return result;
		}

		[NotNull]
		public static IRing CreateEmptyRing([NotNull] IGeometry prototype)
		{
			Assert.ArgumentNotNull(prototype, nameof(prototype));

			var result = new RingClass();

			ConfigureGeometry(result, prototype);

			return result;
		}

		[NotNull]
		public static IRing CreateEmptyRing(bool zAware, bool mAware,
		                                    [CanBeNull] ISpatialReference spatialReference = null)
		{
			var result = new RingClass();

			ConfigureGeometry(result, spatialReference, zAware, mAware, false);

			return result;
		}

		[NotNull]
		public static IPath CreateEmptyPath([NotNull] IGeometry prototype)
		{
			Assert.ArgumentNotNull(prototype, nameof(prototype));

			var result = new PathClass();

			ConfigureGeometry(result, prototype);

			return result;
		}

		[NotNull]
		public static IPath CreateEmptyPath(bool zAware, bool mAware,
		                                    [CanBeNull] ISpatialReference spatialReference = null)
		{
			var result = new PathClass();

			ConfigureGeometry(result, spatialReference, zAware, mAware, false);

			return result;
		}

		#region Non-public members

		private static void PutPoint([NotNull] WKSPoint[] pointArray, int index,
		                             double x, double y)
		{
			Assert.ArgumentNotNull(pointArray, nameof(pointArray));

			pointArray[index].X = x;
			pointArray[index].Y = y;
		}

		private static void PutPoint([NotNull] WKSPointZ[] pointArray, int index,
		                             double x, double y, double z)
		{
			Assert.ArgumentNotNull(pointArray, nameof(pointArray));

			pointArray[index].X = x;
			pointArray[index].Y = y;
			pointArray[index].Z = z;
		}

		private static void MakeZAware([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			((IZAware) geometry).ZAware = true;
		}

		private static bool IsZAware([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			return ((IZAware) geometry).ZAware;
		}

		[NotNull]
		private static IGeometry GetUnionInput(esriGeometryType targetGeometryType,
		                                       [NotNull] IGeometry geometry,
		                                       double expansionDistance)
		{
			switch (targetGeometryType)
			{
				case esriGeometryType.esriGeometryPolygon:
					return CreatePolygonUnionInput(geometry, expansionDistance);

				case esriGeometryType.esriGeometryPolyline:
					return CreatePolylineUnionInput(geometry, expansionDistance);

				case esriGeometryType.esriGeometryMultipoint:
					return CreateMultipointUnionInput(geometry);

				default:
					throw new InvalidOperationException(
						string.Format("Unsupported target type: {0}", targetGeometryType));
			}
		}

		[NotNull]
		private static IGeometry CreateMultipointUnionInput([NotNull] IGeometry geometry)
		{
			if (geometry is IMultipoint)
			{
				return geometry;
			}

			if (geometry is IPointCollection)
			{
				return CreateMultipoint((IPointCollection) geometry);
			}

			if (geometry is IPoint)
			{
				return CreateMultipoint((IPoint) geometry);
			}

			throw new NotSupportedException(
				string.Format("Unsupported geometry type: {0}", geometry.GeometryType));
		}

		[NotNull]
		private static IGeometry CreatePolylineUnionInput([NotNull] IGeometry geometry,
		                                                  double expansionDistance)
		{
			if (geometry is IPolygon)
			{
				return ((ITopologicalOperator) geometry).Boundary;
			}

			if (geometry is IPolyline)
			{
				return geometry;
			}

			if (geometry is IPoint)
			{
				return CreatePolylineFromPoint((IPoint) geometry, expansionDistance);
			}

			if (geometry is IMultiPatch)
			{
				return
					((ITopologicalOperator) CreatePolygon((IMultiPatch) geometry)).Boundary;
			}

			if (geometry is IPointCollection)
			{
				IPoint point = new PointClass();

				try
				{
					var points = (IPointCollection) geometry;
					int pointCount = points.PointCount;

					var polys = new List<IGeometry>(pointCount);

					for (var i = 0; i < pointCount; i++)
					{
						points.QueryPoint(i, point);

						polys.Add(CreatePolylineFromPoint(point, expansionDistance));
					}

					return GeometryUtils.Union(polys);
				}
				finally
				{
					Marshal.ReleaseComObject(point);
				}
			}

			throw new NotSupportedException(
				string.Format("Unsupported geometry type: {0}", geometry.GeometryType));
		}

		[NotNull]
		private static IGeometry CreatePolylineFromPoint([NotNull] IPoint point,
		                                                 double expansionDistance)
		{
			return ((ITopologicalOperator) CreatePolygonFromPoint(
					       point, expansionDistance)).Boundary;
		}

		[NotNull]
		private static IGeometry CreatePolygonUnionInput([NotNull] IGeometry geometry,
		                                                 double expansionDistance)
		{
			if (geometry is IPolygon)
			{
				return geometry;
			}

			if (geometry is IPolyline)
			{
				return GeometryUtils.GeneralizedBuffer(geometry, expansionDistance);
			}

			if (geometry is IPoint)
			{
				return CreatePolygonFromPoint((IPoint) geometry, expansionDistance);
			}

			if (geometry is IMultiPatch)
			{
				return CreatePolygon((IMultiPatch) geometry);
			}

			if (geometry is IPointCollection)
			{
				IPoint point = new PointClass();

				try
				{
					var points = (IPointCollection) geometry;
					int pointCount = points.PointCount;

					var polys = new List<IGeometry>(pointCount);

					for (var i = 0; i < pointCount; i++)
					{
						points.QueryPoint(i, point);

						polys.Add(CreatePolygonFromPoint(point, expansionDistance));
					}

					return GeometryUtils.Union(polys);
				}
				finally
				{
					Marshal.ReleaseComObject(point);
				}
			}

			throw new NotSupportedException(
				string.Format("Unsupported geometry type: {0}", geometry.GeometryType));
		}

		private static IPolygon CreatePolygonFromPoint(IPoint point,
		                                               double expansionDistance)
		{
			double width = expansionDistance * 2;
			double height = width;

			return CreatePolygon(CreateEnvelope(point, width, height));
		}

		[NotNull]
		private static Dictionary<esriGeometryType, List<IGeometry>> GetGeometriesByType(
			[NotNull] IEnumerable<IGeometry> geometries)
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));

			var result = new Dictionary<esriGeometryType, List<IGeometry>>();

			foreach (IGeometry geometry in geometries)
			{
				List<IGeometry> list;
				if (! result.TryGetValue(geometry.GeometryType, out list))
				{
					list = new List<IGeometry>();
					result.Add(geometry.GeometryType, list);
				}

				list.Add(geometry);
			}

			return result;
		}

		private static IMultiPatch ExtrudeGeometry(IGeometry zAwareGeometry,
		                                           double extrusionDistance)
		{
			IGeometry highLevelGeometry = GeometryUtils.GetHighLevelGeometry(zAwareGeometry);

			IConstructMultiPatch constructMultiPatch = new MultiPatchClass();

			((IGeometry) constructMultiPatch).SpatialReference =
				zAwareGeometry.SpatialReference;

			constructMultiPatch.ConstructExtrude(extrusionDistance, highLevelGeometry);

			return (IMultiPatch) constructMultiPatch;
		}

		private static void ConfigureGeometry(IGeometry target,
		                                      [NotNull] IGeometry prototype)
		{
			Assert.ArgumentNotNull(prototype, nameof(prototype));

			ISpatialReference sref = prototype.SpatialReference;

			bool isZAware = GeometryUtils.IsZAware(prototype);
			bool isMAware = GeometryUtils.IsMAware(prototype);
			bool isIDAware = GeometryUtils.IsPointIDAware(prototype);

			ConfigureGeometry(target, sref, isZAware, isMAware, isIDAware);
		}

		private static void ConfigureGeometry([NotNull] IGeometry target,
		                                      [CanBeNull] ISpatialReference sref,
		                                      bool isZAware,
		                                      bool isMAware,
		                                      bool isIDAware)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			target.SpatialReference = sref;

			// Beware that low-level geometries like paths
			// or segments do not implement IZAware etc.

			var zAware = target as IZAware;
			if (zAware != null && zAware.ZAware != isZAware)
			{
				zAware.ZAware = isZAware;
			}

			var mAware = target as IMAware;
			if (mAware != null && mAware.MAware != isMAware)
			{
				mAware.MAware = isMAware;
			}

			var idAware = target as IPointIDAware;
			if (idAware != null && idAware.PointIDAware != isIDAware)
			{
				idAware.PointIDAware = isIDAware;
			}
		}

		#endregion
	}
}
