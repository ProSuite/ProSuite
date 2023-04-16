
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ProSuite.Commons.Collections;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public static class ProxyUtils
	{
		// always access by property
		[CanBeNull][ThreadStatic] private static IEnvelope _envelopeTemplate;

		[NotNull]
		private static IEnvelope EnvelopeTemplate
			=> _envelopeTemplate ?? (_envelopeTemplate = new EnvelopeClass());

		[NotNull]
		public static Box CreateBox([NotNull] IGeometry geometry,
		                            double expansionDistance = 0)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var envelope = geometry as IEnvelope;
			if (envelope == null)
			{
				geometry.QueryEnvelope(EnvelopeTemplate);
				envelope = EnvelopeTemplate;
			}

			double xMin;
			double yMin;
			double xMax;
			double yMax;
			envelope.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			return new Box(
				new Pnt2D(xMin - expansionDistance, yMin - expansionDistance),
				new Pnt2D(xMax + expansionDistance, yMax + expansionDistance));
		}

		public static WKSEnvelope GetWKSEnvelope([NotNull] IGeometry geometry)
		{
			geometry.QueryEnvelope(EnvelopeTemplate);

			WKSEnvelope geometryEnvelope;
			EnvelopeTemplate.QueryWKSCoords(out geometryEnvelope);
			return geometryEnvelope;
		}

		private static int GetCount<T>([NotNull] IEnumerable<T> objects)
		{
			var collection = objects as ICollection<T>;

			return collection?.Count ?? -1;
		}

		public static List<Pnt> GetPoints([NotNull] IEnumerable<SegmentProxy> segments)
		{
			const bool excludeRingEndpoint = false;
			return GetPoints(segments, excludeRingEndpoint);
		}

		[NotNull]
		private static List<Pnt> GetPoints([NotNull] IEnumerable<SegmentProxy> segments,
		                                   bool reportRingStartPointOnlyOnce)
		{
			Assert.ArgumentNotNull(segments, nameof(segments));

			int lastPartIndex = -1;
			int lastSegmentIndex = -1;

			ICollection<SegmentProxy> segmentCollection =
				CollectionUtils.GetCollection(segments);

			int segmentCount = GetCount(segmentCollection);

			List<Pnt> points = segmentCount > 0
				                   ? new List<Pnt>(segmentCount * 2)
				                   : new List<Pnt>();

			const bool as3D = true;
			Pnt firstPointInPart = null;
			foreach (SegmentProxy segment in segmentCollection)
			{
				bool isFirstPointInPart = segment.PartIndex != lastPartIndex;

				if (isFirstPointInPart || segment.SegmentIndex != lastSegmentIndex + 1)
				{
					Pnt startPoint = segment.GetStart(as3D);

					if (isFirstPointInPart)
					{
						firstPointInPart = startPoint;
					}

					points.Add(startPoint);
					lastPartIndex = segment.PartIndex;
				}

				Pnt endPoint = segment.GetEnd(as3D);

				if (!reportRingStartPointOnlyOnce ||
				    firstPointInPart == null ||
				    !IsEqual(endPoint, firstPointInPart))
				{
					points.Add(endPoint);
				}

				lastSegmentIndex = segment.SegmentIndex;
			}

			return points;
		}

		[NotNull]
		public static PolylineClass CreatePolyline([NotNull] IGeometry template)
		{
			return CreatePolyline(new[] { template });
		}

		[NotNull]
		public static PolylineClass CreatePolyline<T>(
			[NotNull] IEnumerable<T> templateEnum)
			where T : IGeometry
		{
			var line = new PolylineClass();
			AdaptAware(line, templateEnum);
			return line;
		}

		[NotNull]
		public static PolygonClass CreatePolygon<T>([NotNull] IEnumerable<T> templateEnum)
			where T : IGeometry
		{
			var poly = new PolygonClass();
			AdaptAware(poly, templateEnum);
			return poly;
		}

		[NotNull]
		public static PolygonClass CreatePolygon([NotNull] IGeometry template)
		{
			return CreatePolygon(new[] { template });
		}

		[NotNull]
		public static MultipointClass CreateMultipoint([NotNull] IGeometry template)
		{
			return CreateMultipoint(new[] { template });
		}

		[NotNull]
		public static MultipointClass CreateMultipoint<T>(
			[NotNull] IEnumerable<T> templateEnum)
			where T : IGeometry
		{
			var multiPoint = new MultipointClass();
			AdaptAware(multiPoint, templateEnum);
			return multiPoint;
		}


		private static void AdaptAware<T>([NotNull] IGeometry unaware,
		                                  [NotNull] IEnumerable<T> templateEnum)
			where T : IGeometry
		{
			foreach (T template in templateEnum)
			{
				var zAware = template as IZAware;
				if (zAware != null)
				{
					((IZAware)unaware).ZAware = zAware.ZAware;
				}

				break;
			}
		}

		[NotNull]
		public static Plane3D CreatePlane3D([NotNull] IEnumerable<SegmentProxy> ringSegments)
		{
			return Plane3D.FitPlane(
				GetPoints(ringSegments, false)
					.Select(p => new Pnt3D(p.X, p.Y, p[2]))
					.ToList(), true);
		}

		[NotNull]
		public static Plane CreatePlane([NotNull] IEnumerable<SegmentProxy> segments)
		{
			const bool reportRingStartPointOnlyOnce = true;
			List<Pnt> points = GetPoints(segments, reportRingStartPointOnlyOnce);

			return CreatePlane(points);
		}

		[NotNull]
		public static Plane CreatePlane<T>([NotNull] IList<T> points) where T : IPnt
		{
			Assert.ArgumentNotNull(points, nameof(points));

			int pointCount = points.Count;

			var x = new double[pointCount];
			var y = new double[pointCount];
			var z = new double[pointCount];

			for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
			{
				IPnt point = points[pointIndex];

				x[pointIndex] = point.X;
				y[pointIndex] = point.Y;
				z[pointIndex] = point[2];
			}

			var plane = new Plane(x, y, z);
			return plane;
		}

		private static bool IsEqual([NotNull] IPnt p1, [NotNull] IPnt p2)
		{
			for (var i = 0; i < 3; i++)
			{
				if (Math.Abs(p1[i] - p2[i]) > double.Epsilon)
				{
					return false;
				}
			}

			return true;
		}


		[NotNull]
		public static IIndexedMultiPatch CreateIndexedMultiPatch(
			[NotNull] IMultiPatch multiPatch)
		{
			var indexedMultiPatch = new IndexedMultiPatch(multiPatch);
			return indexedMultiPatch;
		}

		public static IEnumerable<SegmentProxy> GetSegments(
			[NotNull] IMultiPatch multiPatch)
		{
			return GetPatchProxies(multiPatch).SelectMany(p => p.GetSegments());
		}


		[NotNull]
		public static IPolyline GetSubpart([NotNull] ISegmentCollection segments,
		                                   int partIndex,
		                                   int startSegmentIndex,
		                                   int endSegmentIndex)
		{
			object missing = Type.Missing;
			ISegmentCollection polyline = new PolylineClass();
			((IZAware)polyline).ZAware = ((IZAware)segments).ZAware;

			for (int segmentIndex = startSegmentIndex;
			     segmentIndex <= endSegmentIndex;
			     segmentIndex++)
			{
				ISegment segment;
				IEnumSegment enumSegs = segments.EnumSegments;
				enumSegs.SetAt(partIndex, segmentIndex);
				enumSegs.Next(out segment, ref partIndex, ref segmentIndex);

				ISegment addSegment;
				if (enumSegs.IsRecycling)
				{
					addSegment = GeometryFactory.Clone(segment);

					// release the segment, otherwise "pure virtual function call" occurs 
					// when there are certain circular arcs (IsLine == true ?)
					Marshal.ReleaseComObject(segment);
				}
				else
				{
					addSegment = segment;
				}

				polyline.AddSegment(addSegment, ref missing, ref missing);
			}

			return (IPolyline)polyline;
		}

		public static WKSPointZ GetWksPoint(IPnt p)
		{
			var wks = new WKSPointZ
			          {
				          X = p.X,
				          Y = p.Y
			          };

			var point3D = p as Pnt3D;
			if (point3D != null)
			{
				wks.Z = point3D.Z;
			}

			return wks;
		}

		[NotNull]
		internal static Pnt2D CreatePoint2D([NotNull] IPoint point)
		{
			double x;
			double y;
			point.QueryCoords(out x, out y);

			return new Pnt2D(x, y);
		}

		[NotNull]
		public static Pnt CreatePoint3D([NotNull] IPoint point)
		{
			double x;
			double y;
			point.QueryCoords(out x, out y);

			return new Pnt3D(x, y, point.Z);
		}

		[NotNull]
		public static Pnt CreatePoint3D(WKSPointZ wksPoint)
		{
			Pnt result = Pnt.Create(3);

			result.X = wksPoint.X;
			result.Y = wksPoint.Y;
			result[2] = wksPoint.Z;

			return result;
		}

		internal static IEnumerable<PatchProxy> GetPatchProxies(
			[NotNull] IMultiPatch multiPatch)
		{
			int minPartIndex = 0;
			var parts = multiPatch as IGeometryCollection;
			if (parts == null)
			{
				// no geometry collection
				yield return new PatchProxy(0, 0, (IPointCollection4)multiPatch);
			}
			else
			{
				int partCount = parts.GeometryCount;

				for (int partIndex = 0; partIndex < partCount; partIndex++)
				{
					var patch = (IPointCollection4)parts.Geometry[partIndex];
					try
					{
						var patchProxy = new PatchProxy(partIndex, minPartIndex, patch);
						minPartIndex += patchProxy.PlanesCount;

						yield return patchProxy;
					}
					finally
					{
						Marshal.ReleaseComObject(patch);
					}
				}
			}
		}

		public static void CalculateProjectedArea(
			[NotNull] Plane plane,
			[NotNull] IEnumerable<Pnt> closedRingPoints,
			out double area,
			out double perimeter)
		{
			List<WKSPointZ> projected = ProjectToPlane(plane, closedRingPoints);

			area = GetArea(projected);
			perimeter = GetLength(projected);
		}

		/// <summary>
		/// Get points projected to plane
		/// </summary>
		[NotNull]
		public static List<WKSPointZ> ProjectToPlane([NotNull] Plane plane,
		                                             [NotNull] IEnumerable<Pnt> points)
		{
			if (plane.IsDefined)
			{
				WKSPointZ planeVector1;
				WKSPointZ planeVector2; // orthogonal to planeVector1
				plane.GetPlaneVectors(out planeVector1, out planeVector2);

				var projectedPoints = new List<WKSPointZ>();

				foreach (Pnt point in points)
				{
					var projected =
						new WKSPointZ
						{
							X = planeVector1.X * point.X +
							    planeVector1.Y * point.Y +
							    planeVector1.Z * point[2],
							Y = planeVector2.X * point.X +
							    planeVector2.Y * point.Y +
							    planeVector2.Z * point[2]
						};

					projectedPoints.Add(projected);
				}

				return projectedPoints;
			}

			// the plane is not defined

			var linears = new List<WKSPointZ>();

			Pnt start = null;
			Pnt direction = null;

			foreach (Pnt point in points)
			{
				if (start == null)
				{
					start = point;
				}

				double dx = point.X - start.X;
				double dy = point.Y - start.Y;
				double dz = point[2] - start[2];

				double l = Math.Sqrt(dx * dx + dy * dy + dz * dz);

				if (Math.Abs(l) > double.Epsilon)
				{
					if (direction == null)
					{
						direction = new Pnt3D(dx, dy, dz);
					}
					else
					{
						l = l * Math.Sign(direction.X * dx + direction.Y * dy +
						                  direction[2] * dz);
					}
				}

				var linear = new WKSPointZ { X = l };

				linears.Add(linear);
			}

			return linears;
		}

		private static double GetArea([NotNull] IEnumerable<WKSPointZ> points)
		{
			double area = 0;
			var last = new WKSPointZ();
			var first = true;
			double y0 = 0;
			// y-offset of the co-ordinate system, so that ym does not get to large (will be Y of first point)
			foreach (WKSPointZ point in points)
			{
				if (first)
				{
					y0 = point.Y;
					first = false;
				}
				else
				{
					double dx = point.X - last.X;
					double ym = (point.Y + last.Y) / 2 - y0;

					area += dx * ym;
				}

				last = point;
			}

			return area;
		}

		private static double GetLength(IEnumerable<WKSPointZ> points)
		{
			double length = 0;
			var last = new WKSPointZ();
			var first = true;
			foreach (WKSPointZ point in points)
			{
				if (!first)
				{
					double dx = point.X - last.X;
					double dy = point.Y - last.Y;

					length += Math.Sqrt(dx * dx + dy * dy);
				}

				first = false;
				last = point;
			}

			return length;
		}

	}
}
