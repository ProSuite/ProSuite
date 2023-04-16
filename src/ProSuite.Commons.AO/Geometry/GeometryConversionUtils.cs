using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.ExtractParts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.AO.Geometry
{
	public static class GeometryConversionUtils
	{
		public static IArrayProvider<WKSPointZ> WksPointArrayProvider { get; set; }

		public static List<Pnt3D> GetPntList(IGeometry points,
		                                     bool excludeZs = false)
		{
			var pointCollection = (IPointCollection) points;

			IEnumerable<Pnt3D> pntsEnum = GetPnts3D(points, 0, null, excludeZs);

			var pntList = new List<Pnt3D>(pointCollection.PointCount);
			pntList.AddRange(pntsEnum);

			return pntList;
		}

		public static IEnumerable<Pnt3D> GetPnts3D(IGeometry pointCollection,
		                                           int start = 0,
		                                           int? count = null,
		                                           bool excludeZs = false)
		{
			// NOTE: Z-awareness of a low-level geometry is inconsistent with its high-level
			// geometry in server 11!
			// GeometryUtils.IsZAware(pointCollection);
			bool zAware = ! excludeZs;

			var points = (IPointCollection4) pointCollection;

			int pointCount = count ?? points.PointCount;

			WKSPointZ[] wksPointZs = GetWksPoints(points, start, pointCount);

			IEnumerable<Pnt3D> pntsEnum =
				GetPnts3D(wksPointZs, pointCount, zAware);

			return pntsEnum;
		}

		public static IEnumerable<Pnt3D> GetPnts3D(WKSPointZ[] wksPointZs, int pointCount,
		                                           bool zAware)
		{
			if (zAware)
			{
				for (var i = 0; i < pointCount; i++)
				{
					yield return new Pnt3D(wksPointZs[i].X, wksPointZs[i].Y,
					                       wksPointZs[i].Z);
				}
			}
			else
			{
				for (var i = 0; i < pointCount; i++)
				{
					yield return new Pnt3D(wksPointZs[i].X, wksPointZs[i].Y, double.NaN);
				}
			}
		}

		public static Linestring GetLinestring(IPath path,
		                                       bool excludeZs = false)
		{
			IEnumerable<Pnt3D> path1Pnts3D = GetPnts3D(path, 0, null, excludeZs);

			return new Linestring(path1Pnts3D);
		}

		public static void AddPoints(IEnumerable<IPnt> points, IMultipoint toResult)
		{
			Func<IPnt, WKSPointZ> createPoint;

			if (GeometryUtils.IsZAware(toResult))
				createPoint = WKSPointZUtils.CreatePoint;
			else
				createPoint =
					p => WKSPointZUtils.CreatePoint(p.X, p.Y, double.NaN);

			WKSPointZ[] wksPointZs = points.Select(p => createPoint(p)).ToArray();

			GeometryUtils.AddWKSPointZs((IPointCollection4) toResult, wksPointZs);
		}

		public static void AddPaths([NotNull] IList<Linestring> linestringsToAdd,
		                            [NotNull] IPolyline toResult)
		{
			Func<IPnt, WKSPointZ> createPoint;
			if (GeometryUtils.IsZAware(toResult))
				createPoint = WKSPointZUtils.CreatePoint;
			else
				createPoint = p => WKSPointZUtils.CreatePoint(p.X, p.Y, double.NaN);

			IPath pathTemplate = GeometryFactory.CreateEmptyPath(toResult);
			foreach (Linestring resultLinestring in linestringsToAdd)
			{
				var pathPoints = new WKSPointZ[resultLinestring.SegmentCount + 1];

				var i = 0;
				foreach (Line3D line3D in resultLinestring)
				{
					pathPoints[i++] = createPoint(line3D.StartPoint);
				}

				Pnt3D last = resultLinestring[resultLinestring.SegmentCount - 1].EndPoint;
				pathPoints[i] = createPoint(last);

				IPath path = GeometryFactory.Clone(pathTemplate);
				GeometryUtils.AddWKSPointZs((IPointCollection4) path, pathPoints);
				((IGeometryCollection) toResult).AddGeometry(path);
			}
		}

		public static void AddRingGroup(IMultiPatch result,
		                                RingGroup ringGroup, bool simplify,
		                                bool setPointIds)
		{
			IRing ringTemplate = GeometryFactory.CreateEmptyRing(result);

			int? pointId = setPointIds ? ringGroup.Id : null;

			if (simplify)
			{
				IPolygon poly = CreatePolygon(result, ringTemplate, ringGroup);

				GeometryUtils.Simplify(poly);

				if (! poly.IsEmpty)
				{
					AddToMultipatch(result, poly, false,
					                pointId);

					return;
				}
			}

			Linestring exteriorRing = ringGroup.ExteriorRing;
			IList<Linestring> interiorRings = ringGroup.InteriorRings.ToList();

			AddToMultipatch(result, ringTemplate, exteriorRing, interiorRings, false,
			                pointId);
		}

		public static IMultiPatch CreateMultipatch([NotNull] IEnumerable<RingGroup> fromRingGroups,
		                                           [NotNull] IGeometry prototypeGeometry)
		{
			IMultiPatch resultMultipatch =
				GeometryFactory.CreateEmptyMultiPatch(prototypeGeometry);

			foreach (RingGroup poly in fromRingGroups)
			{
				// TODO: Support vertical ring group with inner rings
				bool simplify = poly.InteriorRings.Any();

				AddRingGroup(
					resultMultipatch, poly, simplify, poly.Id != null);
			}

			return resultMultipatch;
		}

		public static IPolygon CreatePolygon(IGeometry template,
		                                     IRing ringTemplate,
		                                     RingGroup ringGroup)
		{
			IPolygon poly = GeometryFactory.CreateEmptyPolygon(template);

			IRing mainRing = CreateRing(ringGroup.ExteriorRing, ringTemplate);

			((IGeometryCollection) poly).AddGeometry(mainRing);

			foreach (Linestring interiorRing in ringGroup.InteriorRings)
			{
				IRing inner = CreateRing(interiorRing, ringTemplate);
				((IGeometryCollection) poly).AddGeometry(inner);
			}

			((IGeometryCollection) poly).GeometriesChanged();

			return poly;
		}

		public static IPolygon CreatePolygon(IGeometry template,
		                                     IEnumerable<Linestring> rings)
		{
			IPolygon poly = GeometryFactory.CreateEmptyPolygon(template);
			IRing ringTemplate = GeometryFactory.CreateEmptyRing(template);

			foreach (Linestring closedLinestring in rings)
			{
				Assert.True(closedLinestring.IsClosed, "Linestring is not closed.");

				IRing ring = CreateRing(closedLinestring, ringTemplate);

				((IGeometryCollection) poly).AddGeometry(ring);
			}

			((IGeometryCollection) poly).GeometriesChanged();

			return poly;
		}

		public static IRing CreateRing(Linestring closedLinestring, IRing ringTemplate)
		{
			int pointCount = closedLinestring.SegmentCount + 1;

			WKSPointZ[] wksPointZs =
				GetWksPoints(closedLinestring.GetPoints(), pointCount);

			IRing ring = GeometryFactory.Clone(ringTemplate);

			ring.SetEmpty();

			GeometryUtils.AddWKSPointZs((IPointCollection4) ring, wksPointZs, pointCount);

			return ring;
		}

		public static IPointCollection CreatePointCollection(
			[NotNull] IGeometry templateGeometry,
			[NotNull] IEnumerable<IPnt> points,
			[CanBeNull] EnvelopeXY insideAoi = null,
			double xyTolerance = 0)
		{
			IPointCollection result =
				(IPointCollection) GeometryFactory.CreateEmptyMultipoint(templateGeometry);

			foreach (IPnt pnt in points)
			{
				if (insideAoi != null &&
				    GeomRelationUtils.AreBoundsDisjoint(pnt, insideAoi, xyTolerance))
				{
					continue;
				}

				result.AddPoint(CreatePoint(pnt, templateGeometry.SpatialReference));
			}

			return result;
		}

		public static IList<Linestring> PrepareIntersectingLinestrings(
			[NotNull] IPolycurve polycurve,
			[CanBeNull] IEnvelope aoi,
			double tolerance)
		{
			IEnumerable<IPath> paths = GeometryUtils.GetPaths(polycurve);
			var result = new List<Linestring>();

			if (aoi == null || aoi.IsEmpty)
			{
				return result;
			}

			bool ignoreZs = ! GeometryUtils.IsZAware(polycurve);

			IEnvelope envelope = new EnvelopeClass();

			int totalPoints = GeometryUtils.GetPointCount(polycurve);

			foreach (IPath path in paths)
			{
				path.QueryEnvelope(envelope);

				if (GeometryUtils.Disjoint(aoi, envelope, tolerance))
				{
					continue;
				}

				int useSpatialIndexThreshold = totalPoints > 300 ? 0 : int.MaxValue;
				Linestring linestring = CreateLinestring(path, ignoreZs, useSpatialIndexThreshold);

				result.Add(linestring);
			}

			return result;
		}

		[NotNull]
		public static Multipoint<IPnt> CreateMultipoint([NotNull] IMultipoint multipoint)
		{
			bool zAware = GeometryUtils.IsZAware(multipoint);

			Multipoint<IPnt> result = new Multipoint<IPnt>(
				GetPoints(multipoint, zAware),
				GeometryUtils.GetPointCount(multipoint));

			return result;
		}

		[NotNull]
		public static Multipoint<IPnt> CreateMultipoint([NotNull] params IMultipoint[] multipoints)
		{
			if (multipoints.Length == 0)
			{
				return Multipoint<IPnt>.CreateEmpty();
			}

			bool zAware = false;
			Multipoint<IPnt> result = null;
			foreach (IMultipoint aoMultipoint in multipoints)
			{
				if (result == null)
				{
					zAware = GeometryUtils.IsZAware(aoMultipoint);
					result = new Multipoint<IPnt>(GetPoints(aoMultipoint, zAware));
				}
				else
				{
					result.AddPoints(GetPoints(aoMultipoint, zAware));
				}
			}

			return Assert.NotNull(result);
		}

		private static IEnumerable<IPnt> GetPoints(IGeometry aoPointCollection,
		                                           bool pnt3D)
		{
			return GeometryUtils.GetPoints(aoPointCollection).Select(p => CreatePnt(p, pnt3D));
		}

		[NotNull]
		public static Multipoint<IPnt> CreateMultipoint([NotNull] IPoint point)
		{
			bool zAware = GeometryUtils.IsZAware(point);

			Multipoint<IPnt> result =
				new Multipoint<IPnt>(new List<IPnt> { CreatePnt(point, zAware) });

			return result;
		}

		public static IPnt CreatePnt(IPoint p, bool pnt3D)
		{
			return pnt3D ? (IPnt) new Pnt3D(p.X, p.Y, p.Z) : new Pnt2D(p.X, p.Y);
		}

		/// <summary>
		/// Creates a multi-polycurve from the provided polycurve with special consideration for
		/// non-linear segments.
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="omitNonLinearSegments">If true, non-linear segments will be skipped,
		/// otherwise non-linear segments will be stroked (linearized).</param>
		/// <param name="tolerance"></param>
		/// <param name="aoi"></param>
		/// <returns></returns>
		public static MultiPolycurve CreateMultiPolycurve([NotNull] IPolycurve polycurve,
		                                                  bool omitNonLinearSegments,
		                                                  double tolerance = 0,
		                                                  [CanBeNull] IEnvelope aoi = null)
		{
			var result = new List<Linestring>();

			bool hasNonLinearSegments = GeometryUtils.HasNonLinearSegments(polycurve);

			if (! hasNonLinearSegments)
			{
				return CreateMultiPolycurve(polycurve, tolerance, aoi);
			}

			IEnumerable<IPath> paths = GeometryUtils.GetPaths(polycurve);

			foreach (IPath path in paths.Where(p => ! IsDisjoint(p, aoi)))
			{
				if (! omitNonLinearSegments)
				{
					result.Add(new Linestring(GetLinearized(path, tolerance)));
				}
				else
				{
					List<Linestring> pathLinestrings =
						GetLinearLinestrings(path).ToList();

					// allow merging the last with the first in case the second segment is omitted
					// (e.g. because it is non-linear)
					int lastIdx = pathLinestrings.Count - 1;

					if (pathLinestrings.Count > 1 &&
					    pathLinestrings[0].StartPoint.Equals(
						    pathLinestrings[lastIdx].EndPoint))
					{
						var lastAndFirst = new List<Linestring>
						                   {
							                   pathLinestrings[lastIdx],
							                   pathLinestrings[0]
						                   };

						Linestring newFirst =
							GeomTopoOpUtils.MergeConnectedLinestrings(
								lastAndFirst, null, tolerance);

						pathLinestrings[0] = newFirst;
						pathLinestrings.RemoveAt(lastIdx);
					}

					result.AddRange(pathLinestrings);
				}
			}

			return new MultiPolycurve(result);
		}

		public static MultiPolycurve CreateMultiPolycurve([NotNull] IPolycurve polycurve,
		                                                  double tolerance = 0,
		                                                  [CanBeNull] IEnvelope aoi = null)
		{
			// Note: Getting the paths from the GeometryCollection takes a large percentage of the entire method.
			//       However, figuring out the point count per part and extracting a sub-sequence of the array
			//       is not faster. Most likely because unpacking the coordinates has to take place anyway.
			IEnumerable<IPath> paths = GeometryUtils.GetPaths(polycurve);

			bool ignoreZs = ! GeometryUtils.IsZAware(polycurve);

			return CreateMultiPolycurve(paths, ignoreZs, tolerance, aoi);
		}

		public static MultiPolycurve CreateMultiPolycurve([NotNull] IEnumerable<IPath> paths,
		                                                  bool excludeZs = false,
		                                                  double tolerance = 0,
		                                                  [CanBeNull] IEnvelope aoi = null)
		{
			var result = new MultiPolycurve(new List<Linestring>());

			foreach (IPath path in paths)
			{
				if (IsDisjoint(path, aoi))
				{
					continue;
				}

				// TODO: Consider filtering segments also by AOI

				// linestring without spatial index
				Linestring linestring = CreateLinestring(path, excludeZs, int.MaxValue);

				result.AddLinestring(linestring);
			}

			return result;
		}

		public static IList<Linestring> PrepareIntersectingPaths(
			[NotNull] IPolycurve polycurve,
			int createSpatialIndexThreshold)
		{
			bool ignoreZs = ! GeometryUtils.IsZAware(polycurve);

			return PrepareIntersectingPaths(
				// ReSharper disable once RedundantEnumerableCastCall
				GeometryUtils.GetPaths(polycurve).Cast<IGeometry>(),
				ignoreZs,
				createSpatialIndexThreshold);
		}

		public static IList<Linestring> PrepareIntersectingPaths(
			[NotNull] IEnumerable<IGeometry> paths,
			bool excludeZs,
			int createSpatialIndexThreshold)
		{
			var result = new List<Linestring>();

			foreach (IGeometry path in paths)
			{
				Linestring linestring =
					CreateLinestring(path, excludeZs, createSpatialIndexThreshold);

				result.Add(linestring);
			}

			return result;
		}

		public static RingGroup CreateRingGroup(
			[NotNull] IPolygon singleExteriorRingPoly)
		{
			IList<IRing> exteriorRings;
			IList<IRing> innerRings =
				GeometryUtils.GetRings(singleExteriorRingPoly, out exteriorRings);

			Assert.AreEqual(1, exteriorRings.Count,
			                "Provided polygon has more than 1 exterior ring");

			bool ignoreZs = ! GeometryUtils.IsZAware(singleExteriorRingPoly);

			return CreateRingGroup(exteriorRings[0], innerRings, ignoreZs);
		}

		public static RingGroup CreateRingGroup([NotNull] GeometryPart part)
		{
			RingGroup result = CreateRingGroup(
				Assert.NotNull(part.MainOuterRing),
				part.InnerRings.Cast<IRing>().ToList(),
				! part.IsZAware);

			return result;
		}

		public static RingGroup CreateRingGroup([NotNull] IRing exterior,
		                                        [CanBeNull] IList<IRing> interior,
		                                        bool excludeZs = false)
		{
			Assert.ArgumentNotNull(exterior, nameof(exterior));

			if (interior == null)
			{
				interior = new List<IRing>(0);
			}

			Linestring exteriorRing =
				CreateLinestring(Assert.NotNull(exterior), excludeZs);

			Assert.True(exteriorRing.IsClosed, "Expected a closed outer ring");

			List<Linestring> interiorRings = new List<Linestring>(interior.Count);
			foreach (IRing innerRing in interior)
			{
				var interiorLinestring =
					new Linestring(CreateLinestring(innerRing, excludeZs));

				Assert.True(exteriorRing.IsClosed, "Expected only closed inner rings");

				interiorRings.Add(interiorLinestring);
			}

			var result = new RingGroup(exteriorRing, interiorRings);

			return result;
		}

		public static IEnumerable<RingGroup> CreateRingGroups(
			[NotNull] IMultiPatch multipatch,
			bool enforcePositiveOrientation = true)
		{
			foreach (GeometryPart multipatchPart in GeometryPart.FromGeometry(multipatch))
			{
				RingGroup ringGroup = CreateRingGroup(multipatchPart);

				if (enforcePositiveOrientation &&
				    ringGroup.ClockwiseOriented == false)
				{
					// Point intersect rings even if they are facing downward (with negative orientation)
					ringGroup.ReverseOrientation();
				}

				yield return ringGroup;
			}
		}

		public static Polyhedron CreatePolyhedron(IMultiPatch multipatch,
		                                          bool assumePartVertexIds = false,
		                                          bool orientRingsClockwise = false)
		{
			var ringGroups = new List<RingGroup>();

			int index = 0;
			foreach (GeometryPart multipatchPart in GeometryPart.FromGeometry(multipatch))
			{
				RingGroup ringGroup = CreateRingGroup(multipatchPart);

				if (assumePartVertexIds &&
				    GeometryUtils.HasUniqueVertexId(multipatchPart.FirstGeometry, out int vertexId))
				{
					ringGroup.Id = vertexId;
				}
				else
				{
					ringGroup.Id = index;
				}

				index += ringGroup.PartCount;

				if (orientRingsClockwise)
				{
					ringGroup.TryOrientProperly();
				}

				ringGroups.Add(ringGroup);
			}

			return new Polyhedron(ringGroups);
		}

		public static Linestring CreateLinestring([NotNull] IGeometry path,
		                                          bool excludeZs = false,
		                                          int createSpatialIndexThreshold = 300)
		{
			Assert.ArgumentCondition(! GeometryUtils.HasNonLinearSegments(path),
			                         "Non-linear segments");

			List<Pnt3D> path2Pnts3D = GetPntList(path, excludeZs);

			var linestring = new Linestring(path2Pnts3D);

			if (linestring.SegmentCount > createSpatialIndexThreshold)
			{
				double gridSize = ((ICurve) path).Length / linestring.SegmentCount;

				linestring.SpatialIndex =
					SpatialHashSearcher<int>.CreateSpatialSearcher(linestring, gridSize);
			}

			return linestring;
		}

		public static IPolyline CreatePolyline(
			[NotNull] IEnumerable<IntersectionPath3D> intersectionPaths,
			[NotNull] ISpatialReference spatialReference)
		{
			IEnumerable<Linestring> linestrings = intersectionPaths.Select(ip => ip.Segments);

			return CreatePolyline(linestrings, spatialReference);
		}

		public static IPolyline CreatePolyline(
			[NotNull] IEnumerable<Linestring> linestrings,
			[NotNull] ISpatialReference spatialReference)
		{
			var paths = new List<IPath>();

			foreach (Linestring lineString in linestrings)
			{
				List<IPoint> points =
					lineString.GetPoints()
					          .Select(p => GeometryFactory.CreatePoint(p.X, p.Y, p.Z))
					          .ToList();

				IPath path = GeometryFactory.CreatePath(points);

				paths.Add(path);
			}

			if (paths.Count == 0)
			{
				return GeometryFactory.CreateEmptyPolyline(spatialReference, true);
			}

			return GeometryFactory.CreatePolyline(paths, spatialReference, true,
			                                      false);
		}

		public static EnvelopeXY CreateEnvelopeXY([NotNull] IEnvelope envelope)
		{
			return new EnvelopeXY(envelope.XMin, envelope.YMin, envelope.XMax, envelope.YMax);
		}

		public static IPoint CreatePoint(IPnt pnt, ISpatialReference spatialReference)
		{
			double z = pnt is Pnt3D pnt3D ? pnt3D.Z : double.NaN;

			return GeometryFactory.CreatePoint(pnt.X, pnt.Y, z, double.NaN, spatialReference);
		}

		private static IEnumerable<Line3D> GetLinearized([NotNull] IPath path,
		                                                 double maxDeviation)
		{
			IPoint fromPoint = new PointClass();
			IPoint toPoint = new PointClass();

			Pnt3D startPnt = null;

			bool zAware = GeometryUtils.IsZAware(path);

			const double defaultToleranceFactor = 10000;

			IEnvelope envelope = path.Envelope;

			if (path.SpatialReference == null)
			{
				if (maxDeviation == 0)
				{
					maxDeviation = Math.Max(envelope.Width, envelope.Height) /
					               defaultToleranceFactor;
				}
			}
			else
			{
				if (maxDeviation == 0)
				{
					double tolerance = GeometryUtils.GetXyTolerance(path);
					maxDeviation = tolerance;
				}
			}

			ISegmentCollection segmentCollection = (ISegmentCollection) path;
			foreach (ILine line in
			         GeometryUtils.GetLinearizedSegments(segmentCollection, maxDeviation))
			{
				if (startPnt == null)
				{
					line.QueryFromPoint(fromPoint);
					startPnt = CreatePnt3d(fromPoint, zAware);
				}

				line.QueryToPoint(toPoint);

				Pnt3D endPnt = CreatePnt3d(toPoint, zAware);
				Line3D line3D = new Line3D(startPnt, endPnt);

				yield return line3D;

				startPnt = endPnt;
			}
		}

		private static IEnumerable<Linestring> GetLinearLinestrings([NotNull] IPath path)
		{
			IPoint fromPoint = new PointClass();
			IPoint toPoint = new PointClass();

			Pnt3D startPnt = null;

			List<Line3D> currentLines = new List<Line3D>();

			bool zAware = GeometryUtils.IsZAware(path);
			if (GeometryUtils.HasNonLinearSegments(path))
			{
				bool restart = true;
				foreach (ISegment segment in GetSegments(path))
				{
					if (segment.GeometryType == esriGeometryType.esriGeometryLine)
					{
						if (restart || startPnt == null)
						{
							segment.QueryFromPoint(fromPoint);
							startPnt = CreatePnt3d(fromPoint, zAware);
						}

						segment.QueryToPoint(toPoint);

						Pnt3D endPnt = CreatePnt3d(toPoint, zAware);
						Line3D line3D = new Line3D(startPnt, endPnt);

						currentLines.Add(line3D);

						startPnt = endPnt;
						restart = false;
					}
					else
					{
						if (currentLines.Count > 0)
						{
							yield return new Linestring(currentLines);
						}

						currentLines.Clear();
						restart = true;
					}
				}

				if (currentLines.Count > 0)
				{
					yield return new Linestring(currentLines);
				}
			}
			else
			{
				yield return GetLinestring(path, ! zAware);
			}
		}

		private static Pnt3D CreatePnt3d(IPoint point, bool useZ)
		{
			double z = useZ ? point.Z : double.NaN;

			return new Pnt3D(point.X, point.Y, z);
		}

		private static IEnumerable<ISegment> GetSegments(IPath path)
		{
			var allSegments = (ISegmentCollection) path;

			ISegment segment;

			for (var i = 0; i < allSegments.SegmentCount; i++)
			{
				allSegments.QuerySegments(i, 1, out segment);

				yield return segment;
			}
		}

		private static bool IsDisjoint([NotNull] IPath path,
		                               [CanBeNull] IEnvelope aoi)
		{
			if (aoi == null || aoi.IsEmpty)
			{
				return false;
			}

			IEnvelope envelope = new EnvelopeClass();
			double xyTolerance = GeometryUtils.GetXyTolerance(path);

			path.QueryEnvelope(envelope);

			return GeometryUtils.Disjoint(aoi, envelope, xyTolerance);
		}

		private static void AddToMultipatch(IMultiPatch result, IPolygon poly,
		                                    bool invert, int? pointId)
		{
			IList<IRing> exteriorRings;
			var innerRings = GeometryUtils.GetRings(poly, out exteriorRings);

			Assert.AreEqual(1, exteriorRings.Count, "Unexpected ring count");

			IRing mainRing = exteriorRings[0];

			if (pointId != null)
			{
				GeometryUtils.AssignConstantPointID((IPointCollection) mainRing,
				                                    pointId.Value);
			}

			if (invert)
			{
				mainRing.ReverseOrientation();

				foreach (IRing innerRing in innerRings)
				{
					innerRing.ReverseOrientation();
				}
			}

			GeometryFactory.AddRingToMultiPatch(
				mainRing, result, esriMultiPatchRingType.esriMultiPatchOuterRing);

			foreach (IRing innerRing in innerRings)
			{
				if (pointId != null)
				{
					GeometryUtils.AssignConstantPointID((IPointCollection) innerRing,
					                                    pointId.Value);
				}

				GeometryFactory.AddRingToMultiPatch(
					innerRing, result,
					esriMultiPatchRingType.esriMultiPatchInnerRing);
			}
		}

		private static void AddToMultipatch(IMultiPatch result,
		                                    IRing ringTemplate,
		                                    Linestring exteriorRing,
		                                    IList<Linestring> interiorRings,
		                                    bool invert,
		                                    int? pointId)
		{
			if (invert)
			{
				exteriorRing.ReverseOrientation();

				foreach (Linestring interiorRing in interiorRings)
				{
					interiorRing.ReverseOrientation();
				}
			}

			AddRing(result, exteriorRing, ringTemplate,
			        esriMultiPatchRingType.esriMultiPatchOuterRing, pointId);

			foreach (Linestring interiorRing in interiorRings)
			{
				AddRing(result, interiorRing, ringTemplate,
				        esriMultiPatchRingType.esriMultiPatchInnerRing, pointId);
			}
		}

		private static void AddRing(IMultiPatch result, Linestring closedLinestring,
		                            IRing ringTemplate, esriMultiPatchRingType ringType,
		                            int? pointId)
		{
			IRing ring = CreateRing(closedLinestring, ringTemplate);

			if (pointId != null)
				GeometryUtils.AssignConstantPointID((IPointCollection) ring,
				                                    pointId.Value);

			((IGeometryCollection) result).AddGeometry(ring);

			result.PutRingType(ring, ringType);
		}

		private static WKSPointZ[] GetWksPoints(IPointCollection originalPoints,
		                                        int start = 0, int? count = null)
		{
			// Getting the coordinates through a (re-used) array improves performance by at least a magnitude
			WKSPointZ[] wksPointArry = GetWksPointArray(originalPoints.PointCount);

			//// This results in Exception from HRESULT: 0x80040585 if the array is too large -> cannot re-use array
			//GeometryUtils.GeometryBridge.QueryWKSPointZs((IPointCollection4)sourcePointCollection, 0,	ref sourcePoints);

			int pointCount = count ?? originalPoints.PointCount;

			((IPointCollection4) originalPoints).QueryWKSPointZs(
				start, pointCount, out wksPointArry[0]);

			return wksPointArry;
		}

		private static WKSPointZ[] GetWksPoints(IEnumerable<Pnt3D> pnts, int pointCount)
		{
			WKSPointZ[] wksPointArray = GetWksPointArray(pointCount);

			var count = 0;
			foreach (Pnt3D pnt3D in pnts)
			{
				if (count == pointCount)
				{
					break;
				}

				wksPointArray[count++] =
					WKSPointZUtils.CreatePoint(pnt3D.X, pnt3D.Y, pnt3D.Z);
			}

			return wksPointArray;
		}

		private static WKSPointZ[] GetWksPointArray(int pointCount)
		{
			// TODO: Consider conditional compilation if > .NET 3.5...
			if (WksPointArrayProvider == null)
			{
				WksPointArrayProvider = new ArrayProvider<WKSPointZ>();
			}

			return WksPointArrayProvider.GetArray(pointCount);
		}
	}
}
