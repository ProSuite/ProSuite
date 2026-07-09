using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.CreateFootprint;
using ProSuite.Commons.AO.Geometry.ExtractParts;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.Cut
{
	public static class CutGeometryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Cuts the provided multipatch along the specified cutLine.
		/// </summary>
		/// <param name="multipatch"></param>
		/// <param name="cutLine"></param>
		/// <param name="zSource">The source of the Z values to be used for the new vertices of the result features.</param>
		/// <param name="usedCutLine">The cut result containing information about the cut operation.</param>
		/// <param name="nonSimpleFootprintAction">The action to be performed if one of the result multipatches
		/// has a degenerate footprint (because it is a sliver polygon with sub-tolerance self intersections).</param>
		/// <returns></returns>
		public static IDictionary<IPolygon, IMultiPatch> TryCut(
			[NotNull] IMultiPatch multipatch,
			[NotNull] IPolyline cutLine,
			ChangeAlongZSource zSource,
			[CanBeNull] CutPolyline usedCutLine = null,
			DegenerateMultipatchFootprintAction nonSimpleFootprintAction =
				DegenerateMultipatchFootprintAction.Throw)
		{
			Assert.ArgumentNotNull(multipatch, nameof(multipatch));
			Assert.ArgumentNotNull(cutLine, nameof(cutLine));

			bool allRings = GeometryUtils.IsRingBasedMultipatch(multipatch);

			Assert.True(allRings,
			            "The multipatch geometry contains triangles, triangle fans or triangle strips, which are currently not supported");

			// NOTE: ITopologicalOperator4 is not supported by multipatch, ITopologicalOperator3.Cut returns non-Z-aware polygons
			// -> Use GeomUtils.Cut implementation which could eventually classify the left/right parts and avoid footprint-cutting
			//    only to find the correct assignment to result features.

			// NOTE: At 12.0, creating the multipatches from the boundary is often incorrect (unless all rings are positive)
			double xyTolerance = GeometryUtils.GetXyTolerance(multipatch);
			IPolygon footprint = CreateFootprintUtils.GetFootprint(multipatch, xyTolerance);

			IList<IGeometry> cutFootprintParts =
				TryCutRingGroups(footprint, cutLine, ChangeAlongZSource.Target);

			if (cutFootprintParts == null || cutFootprintParts.Count == 0)
			{
				_msg.DebugFormat(
					"Not even the footprint could be cut. No multipatch cutting performed.");
				if (usedCutLine != null)
				{
					usedCutLine.Polyline = cutLine;
					usedCutLine.SuccessfulCut = false;
				}

				return new Dictionary<IPolygon, IMultiPatch>(0);
			}

			// Get the 'connected components', i.e. outer ring with respective inner rings.
			IList<GeometryPart> multipatchParts =
				GeometryPart.FromGeometry(multipatch).ToList();

			Dictionary<RingGroup, List<RingGroup>> splitPartsByFootprintPart =
				PrepareSplitPartsDictionary(cutFootprintParts);

			foreach (GeometryPart part in multipatchParts)
			{
				CutAndAssignToFootprintParts(part, cutLine, splitPartsByFootprintPart,
				                             zSource);
			}

			// make a separate multipatch per footprint-part with all respective ring-groups
			Dictionary<IPolygon, IMultiPatch> result = BuildResultMultipatches(
				multipatch, splitPartsByFootprintPart, nonSimpleFootprintAction);

			if (usedCutLine != null && splitPartsByFootprintPart.Values.Count > 1)
			{
				// TODO: Extract actual cutLine from inBound/outBound intersections
				usedCutLine.Polyline = cutLine;
				usedCutLine.SuccessfulCut = false;
			}

			return result;
		}

		[CanBeNull]
		public static IList<IGeometry> TryCut([NotNull] IPolygon inputPolygon,
		                                      [NotNull] IPolyline cutPolyline,
		                                      ChangeAlongZSource zSource)
		{
			IList<IGeometry> result;

			double originalArea = ((IArea) inputPolygon).Area;

			if (IntersectionUtils.UseCustomIntersect &&
			    ! GeometryUtils.HasNonLinearSegments(inputPolygon) &&
			    ! GeometryUtils.HasNonLinearSegments(cutPolyline) &&
			    ! GeometryUtils.IsMAware(inputPolygon))
			{
				result = TryCutXY(inputPolygon, cutPolyline, zSource);
			}
			else
			{
				result = TryCutArcObjects(inputPolygon, cutPolyline, zSource);
			}

			if (result != null)
			{
				double newArea = result.Sum(geometry => ((IArea) geometry).Area);
				double tolerance = originalArea * 0.001;
				if (! MathUtils.AreEqual(newArea, originalArea, tolerance))
				{
					_msg.DebugFormat("Input Polygon:{0}{1}", Environment.NewLine,
					                 GeometryUtils.ToXmlString(inputPolygon));
					_msg.DebugFormat("Cut polyline:{0}{1}", Environment.NewLine,
					                 GeometryUtils.ToXmlString(cutPolyline));

					throw new AssertionException(
						"The cut operation has significantly changed the polygon area! Please report this polygon and the cut line.");
				}
			}

			return result;
		}

		[CanBeNull]
		private static List<IGeometry> TryCutArcObjects(
			[NotNull] IPolygon inputPolygon,
			[NotNull] IPolyline cutPolyline,
			ChangeAlongZSource zSource)
		{
			cutPolyline = ChangeAlongZUtils.PrepareCutPolylineZs(cutPolyline, zSource);

			var existingFeature = new List<IPolygon>();
			var newFeatures = new List<IPolygon>();
			foreach (IPolygon connectedComponent in GeometryUtils.GetConnectedComponents(
				         inputPolygon))
			{
				Plane3D plane = null;
				if (zSource == ChangeAlongZSource.SourcePlane)
				{
					double zTolerance = GeometryUtils.GetZTolerance(inputPolygon);
					plane = ChangeZUtils.GetPlane(
						GeometryConversionUtils.GetPntList(connectedComponent),
						zTolerance);
				}

				var cutComponents = TryCut(connectedComponent, cutPolyline);

				if (cutComponents == null)
				{
					existingFeature.Add(connectedComponent);
				}
				else
				{
					var largest = GeometryUtils.GetLargestGeometry(cutComponents);

					foreach (IPolygon cutComponent in cutComponents.Cast<IPolygon>())
					{
						if (plane != null)
						{
							ChangeAlongZUtils.AssignZ((IPointCollection) cutComponent,
							                          plane);
						}
						else if (zSource == ChangeAlongZSource.InterpolatedSource &&
						         GeometryUtils.IsZAware(cutComponent))
						{
							((IZ) cutComponent).CalculateNonSimpleZs();
						}

						if (cutComponent == largest)
						{
							existingFeature.Add(cutComponent);
						}
						else
						{
							newFeatures.Add(cutComponent);
						}
					}
				}
			}

			if (newFeatures.Count == 0)
			{
				return null;
			}

			var result = new List<IGeometry>
			             {
				             GeometryUtils.Union(existingFeature),
			             };

			result.AddRange(newFeatures.Cast<IGeometry>());
			return result;
		}

		[CanBeNull]
		public static IList<IGeometry> TryCut([NotNull] IPolygon inputPolygon,
		                                      [NotNull] IPolyline cutLine,
		                                      bool verifyTargetZs = true)
		{
			var result = new List<IGeometry>();

			try
			{
				IGeometryCollection resultCollection;

				try
				{
					resultCollection =
						((ITopologicalOperator4) inputPolygon).Cut2(cutLine);
				}
				catch (COMException comEx)
				{
					const int cannotBePerfomredOnNonSimpleGeometry = -2147220968;

					if (comEx.ErrorCode == cannotBePerfomredOnNonSimpleGeometry)
					{
						_msg.Debug("Cut2 failed. Trying again after simplify...", comEx);

						GeometryUtils.Simplify(inputPolygon);

						// and try again 
						resultCollection =
							((ITopologicalOperator4) inputPolygon).Cut2(cutLine);
					}
					else
					{
						throw;
					}
				}

				if (verifyTargetZs)
				{
					// TOP-4666: Sometimes NaN or 0 Z values are introduced instead of the Z values from the sketch
					if (! VerifyZs(inputPolygon, resultCollection, cutLine))
					{
						resultCollection =
							((ITopologicalOperator4) inputPolygon).Cut2(cutLine);
					}

					// The second time works correctly:
					if (! VerifyZs(inputPolygon, resultCollection, cutLine))
					{
						_msg.Warn(
							"Z verification failed. Please review Z values carefully and report in case of wrong Z values.");
					}
				}

				bool inputIsZAware = GeometryUtils.IsZAware(inputPolygon);

				foreach (IGeometry resultGeometry in GeometryUtils.GetParts(resultCollection))
				{
					// Bug DPS/#258: Non-Z-aware polygons become Z-aware by Cut
					if (! inputIsZAware && GeometryUtils.IsZAware(resultGeometry))
					{
						GeometryUtils.MakeNonZAware(resultGeometry);
					}

					result.Add(resultGeometry);
				}
			}
			catch (Exception e)
			{
				// TODO: Catch specific exception only
				// Typically:
				// A polygon cut operation could not classify all parts of the polygon as left or right of the cutting line.
				_msg.Debug("Unable to cut polygon", e);

				return null;
			}

			return result;
		}

		/// <summary>
		/// Cuts a polyline into polyline pieces using the specified cutPolyline. In case the polyline:
		/// - Is single part: The resulting parts will be exploded into individual geometries.
		///   The resulting geometries are all single part
		/// - Is multipart: The resulting parts will be assigned to their respective collection
		///   (left of cut line / right of cut line / touching both sides of cut line) and these
		///   collections will be merged into one result geometry each. The non-cut parts are 
		///   assigned to the largest of these result geometries.
		/// This behaviour is more or less consistent with the cut polygon behaviour (which in itself is
		/// not quite consistent regarding left and right) and could be easily made configurable by users 
		/// ('allow multipart result').
		/// </summary>
		/// <param name="inputPolyline"></param>
		/// <param name="cutPolyline"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IList<IGeometry> TryCut([NotNull] IPolyline inputPolyline,
		                                      [NotNull] IPolyline cutPolyline)
		{
			var splitPoints = (IPointCollection)
				IntersectionUtils.GetIntersectionPoints(inputPolyline, cutPolyline);

			if (splitPoints.PointCount == 0)
			{
				return null;
			}

			List<IGeometry> splitGeometries =
				GeometryUtils.GetPartCount(inputPolyline) > 1
					? SplitMultipartPolyline(inputPolyline, cutPolyline, splitPoints)
					: SplitPolyline(inputPolyline, splitPoints);

			// largest first
			splitGeometries.Sort(CompareGeometrySize);
			splitGeometries.Reverse();

			return splitGeometries;
		}

		public static IEnumerable<IPath> GetTrimmedCutLines(
			[NotNull] IPolyline cutPolyline,
			[NotNull] IPolygon sourcePolygon,
			[NotNull] IList<IGeometry> cutGeometries)
		{
			IEnumerable<IPath> targetPaths = GeometryUtils.GetPaths(cutPolyline);

			foreach (IPath path in targetPaths)
			{
				IPath trimmedPath = TrimCutLine(
					path, sourcePolygon, cutGeometries);

				// the path must touch at least 2 cut geometries
				if (PathRunsAlongTwoPolygons(trimmedPath, cutGeometries))
				{
					yield return trimmedPath;
				}
			}
		}

		private static List<IGeometry> SplitMultipartPolyline(
			[NotNull] IPolyline inputPolyline,
			[NotNull] IPolyline cutPolyline,
			[NotNull] IPointCollection splitPoints)
		{
			var result = new List<IGeometry>();

			var resultLeft = new List<IGeometry>();
			var resultRight = new List<IGeometry>();
			var resultBoth = new List<IGeometry>();

			// Make sure only the non-split parts remain part of the original geometry
			var notSplitPaths = new List<IPath>();

			foreach (IPath inputPath in GeometryUtils.GetPaths(inputPolyline))
			{
				var highLevelPath =
					(IPolyline) GeometryUtils.GetHighLevelGeometry(inputPath);

				if (GeometryUtils.Intersects(highLevelPath, (IGeometry) splitPoints))
				{
					SplitPolyline(highLevelPath, splitPoints, cutPolyline, resultLeft,
					              resultRight,
					              resultBoth);
				}
				else
				{
					notSplitPaths.Add(inputPath);
				}
			}

			// Union the 3 sets and make individual polylines
			if (resultLeft.Count > 0)
			{
				result.Add(GeometryUtils.Union(resultLeft));
			}

			if (resultRight.Count > 0)
			{
				result.Add(GeometryUtils.Union(resultRight));
			}

			if (resultBoth.Count > 0)
			{
				result.Add(GeometryUtils.Union(resultBoth));
			}

			IGeometry largestGeometry =
				Assert.NotNull(GeometryUtils.GetLargestGeometry(result));

			// add the non-split paths to the largest
			object missing = Type.Missing;
			foreach (IPath notSplitPath in notSplitPaths)
			{
				((IGeometryCollection) largestGeometry).AddGeometry(
					notSplitPath, ref missing,
					ref missing);
			}

			return result;
		}

		private static int CompareGeometrySize([CanBeNull] IGeometry geometry1,
		                                       [CanBeNull] IGeometry geometry2)
		{
			if (geometry1 == null && geometry2 == null)
			{
				return 0;
			}

			if (geometry1 == null)
			{
				return -1;
			}

			if (geometry2 == null)
			{
				return 1;
			}

			Assert.ArgumentCondition(geometry1.GeometryType == geometry2.GeometryType,
			                         "CompareGeometrySize: All geometry types must be the same.");

			double size1 = GeometryUtils.GetGeometrySize(geometry1);
			double size2 = GeometryUtils.GetGeometrySize(geometry2);

			if (size1 > size2)
			{
				return 1;
			}

			if (size2 < size1)
			{
				return -1;
			}

			return 0;
		}

		private static List<IGeometry> SplitPolyline(IPolyline inputPolyline,
		                                             IPointCollection splitPoints)
		{
			const bool createParts = true;
			const bool projectPointsOntoPathToSplit = true;
			GeometryUtils.CrackPolycurve(
				inputPolyline, splitPoints, projectPointsOntoPathToSplit, createParts,
				null);

			IList<IGeometry> splitGeometries = GeometryUtils.Explode(inputPolyline);

			return splitGeometries.ToList();
		}

		private static void SplitPolyline(IPolyline inputPolyline,
		                                  IPointCollection splitPoints,
		                                  IPolyline cutLine,
		                                  IList<IGeometry> resultLeft,
		                                  IList<IGeometry> resultRight,
		                                  IList<IGeometry> resultBoth)
		{
			const bool createParts = true;
			const bool projectPointsOntoPathToSplit = true;
			IList<IPoint> splittedAtPoints = GeometryUtils.CrackPolycurve(
				inputPolyline, splitPoints, projectPointsOntoPathToSplit, createParts,
				null);

			IList<IGeometry> splitGeometries = GeometryUtils.Explode(inputPolyline);

			IPoint queryPoint = new PointClass();
			queryPoint.SpatialReference = inputPolyline.SpatialReference;

			foreach (IGeometry splitGeometry in splitGeometries)
			{
				bool isLeft;
				bool isRight;
				GetSideOfCutLine(cutLine, splitGeometry, splittedAtPoints, queryPoint,
				                 out isLeft, out isRight);

				if (isLeft && isRight)
				{
					resultBoth.Add(splitGeometry);
				}
				else if (isLeft)
				{
					resultLeft.Add(splitGeometry);
				}
				else if (isRight)
				{
					resultRight.Add(splitGeometry);
				}
			}
		}

		private static void GetSideOfCutLine([NotNull] IPolyline cutLine,
		                                     [NotNull] IGeometry splitGeometry,
		                                     [NotNull] IList<IPoint> splittedAtPoints,
		                                     [NotNull] IPoint queryPoint,
		                                     out bool isLeft, out bool isRight)
		{
			double xyTolerance = GeometryUtils.GetXyTolerance(cutLine);

			var splitLine = (IPolyline) splitGeometry;

			IPoint fromPoint = splitLine.FromPoint;
			IPoint toPoint = splitLine.ToPoint;

			isLeft = false;
			isRight = false;

			if (splittedAtPoints.Any(p => GeometryUtils.Intersects(p, fromPoint)))
			{
				// find out if the result line is at the right or lift

				splitLine.QueryPoint(esriSegmentExtension.esriNoExtension,
				                     xyTolerance,
				                     false, queryPoint);

				if (IsPointLeftOfCurve(queryPoint, cutLine))
				{
					isLeft = true;
				}
				else
				{
					isRight = true;
				}
			}

			if (splittedAtPoints.Any(p => GeometryUtils.Intersects(p, toPoint)))
			{
				splitLine.QueryPoint(esriSegmentExtension.esriNoExtension,
				                     splitLine.Length - xyTolerance,
				                     false, queryPoint);

				if (IsPointLeftOfCurve(queryPoint, cutLine))
				{
					isLeft = true;
				}
				else
				{
					isRight = true;
				}
			}
		}

		private static bool IsPointLeftOfCurve([NotNull] IPoint point,
		                                       [NotNull] ICurve curve)
		{
			double distanceAlongCurve = -1;
			double distanceFromCurve = -1;
			var rightSide = false;

			IPoint nearestPoint = new PointClass();

			curve.QueryPointAndDistance(esriSegmentExtension.esriNoExtension,
			                            point, false, nearestPoint,
			                            ref distanceAlongCurve,
			                            ref distanceFromCurve,
			                            ref rightSide);

			Assert.True(distanceFromCurve > 0, "Point is on the curve");

			return ! rightSide;
		}

		private static bool VerifyZs([NotNull] IPolygon originalPolygon,
		                             [NotNull] IGeometryCollection resultPolygons,
		                             [NotNull] IPolyline cutLine)
		{
			foreach (IGeometry resultPolygon in GeometryUtils.GetParts(resultPolygons))
			{
				// Sketch should have been Z-Simplified
				if (GeometryUtils.HasUndefinedZValues(resultPolygon) &&
				    ! GeometryUtils.HasUndefinedZValues(originalPolygon) &&
				    ! GeometryUtils.HasUndefinedZValues(cutLine))
				{
					// NaN Z values have been introduced
					_msg.DebugFormat("Result polygon with NaN Zs: {0}",
					                 GeometryUtils.ToString(resultPolygon));
					return false;
				}

				// Less watertight, but 0-Z has so far only been observed in the unit test:
				if (GeometryUtils.IsZAware(originalPolygon) &&
				    MathUtils.AreEqual(0, resultPolygon.Envelope.ZMin) &&
				    originalPolygon.Envelope.ZMin > 0 && cutLine.Envelope.ZMin > 0)
				{
					// 0 Z values have been introduced
					return false;
				}
			}

			return true;
		}

		private static IList<IList<RingGroup>> TryCutConnectedComponents(
			IPolygon inputPolygon,
			IPolyline cutPolyline,
			ChangeAlongZSource zSource)
		{
			double tolerance = GeometryUtils.GetXyTolerance(inputPolygon);
			double zTolerance = GeometryUtils.GetZTolerance(inputPolygon);

			var result = new List<IList<RingGroup>>();

			foreach (IPolygon connectedComponent in GeometryUtils.GetConnectedComponents(
				         inputPolygon))
			{
				RingGroup ringGroup =
					GeometryConversionUtils.CreateRingGroup(connectedComponent);

				IList<RingGroup> cutRingGroups = CutRingGroupPlanar(
					ringGroup, cutPolyline, tolerance, zSource, zTolerance);

				result.Add(cutRingGroups);
			}

			return result;
		}

		[CanBeNull]
		private static IList<IGeometry> TryCutXY(
			IPolygon inputPolygon,
			IPolyline cutPolyline,
			ChangeAlongZSource zSource)
		{
			// TODO:
			// In order to avoid the arbitrary grouping of multipart polygons, try to apply left/right logic
			// provided by GeomTopoOpUtils

			double tolerance = GeometryUtils.GetXyTolerance(inputPolygon);
			double zTolerance = GeometryUtils.GetZTolerance(inputPolygon);

			MultiPolycurve inputMultipoly =
				GeometryConversionUtils.CreateMultiPolycurve(inputPolygon);

			var cutLine = GeometryFactory.Clone(cutPolyline);

			if (GeometryUtils.IsZAware(cutLine) &&
			    zSource != ChangeAlongZSource.Target)
			{
				((IZAware) cutLine).DropZs();
			}

			Plane3D plane = null;
			if (zSource == ChangeAlongZSource.SourcePlane)
			{
				plane = ChangeZUtils.GetPlane(
					inputMultipoly.GetPoints().ToList(), zTolerance);
			}

			GeometryUtils.Simplify(cutLine, true, true);

			MultiPolycurve cutLinestrings = GeometryConversionUtils.CreateMultiPolycurve(cutLine);

			bool isMultipart = GeometryUtils.GetExteriorRingCount(inputPolygon) > 1;

			IList<MultiLinestring> resultGeoms =
				GeomTopoOpUtils.CutXY(inputMultipoly, cutLinestrings, tolerance, ! isMultipart);

			var result = new List<IGeometry>();
			foreach (MultiLinestring resultPoly in resultGeoms)
			{
				if (plane != null)
				{
					resultPoly.AssignUndefinedZs(plane);
				}
				else
				{
					resultPoly.InterpolateUndefinedZs();
				}

				result.Add(GeometryConversionUtils.CreatePolygon(inputPolygon,
				                                                 resultPoly.GetLinestrings()));
			}

			Marshal.ReleaseComObject(cutLine);

			return result.Count == 0 ? null : result;
		}

		[CanBeNull]
		private static IList<IGeometry> TryCutRingGroups(
			IPolygon inputPolygon,
			IPolyline cutPolyline,
			ChangeAlongZSource zSource)
		{
			// TODO:
			// In order to avoid the arbitrary grouping of multipart polygons, try to apply left/right logic
			// provided by GeomTopoOpUtils

			var existingFeature = new List<IPolygon>();
			var newFeatures = new List<IPolygon>();

			var cutResultsPerConnectedComponent =
				TryCutConnectedComponents(inputPolygon, cutPolyline, zSource);

			foreach (IList<RingGroup> cutRingGroups in cutResultsPerConnectedComponent)
			{
				IRing ringTemplate = GeometryFactory.CreateEmptyRing(inputPolygon);

				var cutResults =
					cutRingGroups.Select(rg =>
						                     GeometryConversionUtils.CreatePolygon(
							                     inputPolygon, ringTemplate, rg))
					             .ToList();

				var largest = GeometryUtils.GetLargestGeometry(cutResults);

				foreach (IPolygon cutComponent in cutResults)
				{
					if (cutComponent == largest)
					{
						existingFeature.Add(cutComponent);
					}
					else
					{
						newFeatures.Add(cutComponent);
					}
				}
			}

			if (newFeatures.Count == 0)
			{
				// no cut happened:
				return null;
			}

			var result = new List<IGeometry>
			             {
				             GeometryUtils.Union(existingFeature)
			             };

			result.AddRange(newFeatures.Cast<IGeometry>());

			return result;
		}

		#region Multipatch cutting

		private static Dictionary<RingGroup, List<RingGroup>> PrepareSplitPartsDictionary(
			IList<IGeometry> cutFootprintParts)
		{
			var splitPartsByFootprintPart =
				new Dictionary<RingGroup, List<RingGroup>>();

			foreach (IGeometry cutFootprintPart in cutFootprintParts)
			{
				var footprintPartGeom =
					GeometryConversionUtils.CreateRingGroup((IPolygon) cutFootprintPart);

				splitPartsByFootprintPart.Add(footprintPartGeom, new List<RingGroup>());
			}

			return splitPartsByFootprintPart;
		}

		private static void CutAndAssignToFootprintParts(
			[NotNull] GeometryPart multipatchPart,
			[NotNull] IPolyline cutLine,
			[NotNull] IDictionary<RingGroup, List<RingGroup>> splitPartsByFootprintPart,
			ChangeAlongZSource zSource)
		{
			double tolerance = GeometryUtils.GetXyTolerance(multipatchPart.FirstGeometry);
			double zTolerance = GeometryUtils.GetZTolerance(multipatchPart.FirstGeometry);

			RingGroup ringGroup = GeometryConversionUtils.CreateRingGroup(multipatchPart);

			int pointId;
			if (GeometryUtils.HasUniqueVertexId(
				    Assert.NotNull(multipatchPart.MainOuterRing), out pointId))
			{
				ringGroup.Id = pointId;
			}

			bool inverted = ringGroup.ClockwiseOriented == false;

			if (inverted)
			{
				ringGroup.ReverseOrientation();
			}

			IList<RingGroup> cutRingGroups =
				CutRingGroupPlanar(ringGroup, cutLine, tolerance, zSource, zTolerance);

			AssignResultsToFootprintParts(cutRingGroups, splitPartsByFootprintPart,
			                              tolerance);

			if (inverted)
			{
				foreach (RingGroup cutRingGroup in cutRingGroups)
				{
					cutRingGroup.ReverseOrientation();
				}
			}
		}

		[NotNull]
		private static IList<RingGroup> CutRingGroupPlanar(
			[NotNull] RingGroup ringGroup,
			[NotNull] IPolyline cutLine,
			double tolerance,
			ChangeAlongZSource zSource,
			double zTolerance)
		{
			cutLine = GeometryFactory.Clone(cutLine);

			bool cutLineHasZs = GeometryUtils.IsZAware(cutLine);

			if (cutLineHasZs &&
			    zSource != ChangeAlongZSource.Target)
			{
				((IZAware) cutLine).DropZs();
			}

			Plane3D plane = null;
			if (zSource == ChangeAlongZSource.SourcePlane)
			{
				plane = ChangeZUtils.GetPlane(ringGroup.ExteriorRing, zTolerance);
			}

			GeometryUtils.Simplify(cutLine, true, true);

			MultiPolycurve cutLinestrings = new MultiPolycurve(
				GeometryUtils.GetPaths(cutLine).Select(cutPath =>
					                                       GeometryConversionUtils.CreateLinestring(
						                                       cutPath, ! cutLineHasZs)));

			IList<RingGroup> resultGroups =
				GeomTopoOpUtils.CutPlanar(ringGroup, cutLinestrings, tolerance);

			foreach (RingGroup resultPoly in resultGroups)
			{
				resultPoly.Id = ringGroup.Id;

				if (plane != null)
				{
					resultPoly.AssignUndefinedZs(plane);
				}
				else
				{
					resultPoly.InterpolateUndefinedZs();
				}
			}

			Marshal.ReleaseComObject(cutLine);

			if (resultGroups.Count == 0)
			{
				// Return uncut original
				resultGroups.Add(ringGroup);
			}

			return resultGroups;
		}

		private static Dictionary<IPolygon, IMultiPatch> BuildResultMultipatches(
			[NotNull] IMultiPatch prototype,
			[NotNull] IDictionary<RingGroup, List<RingGroup>> splitResultsByFootprintPart,
			DegenerateMultipatchFootprintAction nonSimpleBoundaryAction)
		{
			var result = new Dictionary<IPolygon, IMultiPatch>();

			IRing emptyRing = GeometryFactory.CreateEmptyRing(prototype);

			foreach (var outerRingsByFootprintPart in splitResultsByFootprintPart)
			{
				RingGroup footprintPart = outerRingsByFootprintPart.Key;
				List<RingGroup> resultPolys = outerRingsByFootprintPart.Value;

				IMultiPatch resultMultipatch =
					GeometryConversionUtils.CreateMultipatch(resultPolys, prototype);

				if (resultPolys.Any(p => p.Id != null))
				{
					GeometryUtils.MakePointIDAware(resultMultipatch);
				}

				// Guard against multipatches with wrong footprint. They are so broken (IRelationalOps are wrong) that
				// it's not worth using them any further...
				if (IsMultipatchWithDegenerateFootprint(resultMultipatch))
				{
					switch (nonSimpleBoundaryAction)
					{
						case DegenerateMultipatchFootprintAction.Throw:
							throw new DegenerateResultGeometryException(
								"The multipatch cut operation resulted in a multipatch with degenerate footprint.");
						case DegenerateMultipatchFootprintAction.Discard:
							_msg.DebugFormat(
								"Discarding result multipatch with degenerate boundary: {0}",
								GeometryUtils.ToString(resultMultipatch));
							continue;
						case DegenerateMultipatchFootprintAction.Keep:
							_msg.DebugFormat(
								"Detected result multipatch with degenerate boundary (it will be kept): {0}",
								GeometryUtils.ToString(resultMultipatch));
							break;
					}
				}

				IPolygon footprintPoly =
					GeometryConversionUtils.CreatePolygon(emptyRing, emptyRing, footprintPart);

				result.Add(footprintPoly, resultMultipatch);
			}

			return result;
		}

		private static void AssignResultsToFootprintParts(
			IEnumerable<RingGroup> resultRingGroups,
			IDictionary<RingGroup, List<RingGroup>> splitPartsByFootprintPart,
			double tolerance)
		{
			foreach (RingGroup resultPoly in resultRingGroups)
			{
				var assignmentCount = 0;

				foreach (RingGroup cutFootprintPart in splitPartsByFootprintPart.Keys)
				{
					bool interiorIntersects;

					if (resultPoly.IsVertical(tolerance))
					{
						// Check if it has linear intersections with the footprints -> true
						// Otherwise, check if it is contained.
						interiorIntersects = GeomRelationUtils.HaveLinearIntersectionsXY(
							cutFootprintPart.ExteriorRing, resultPoly.ExteriorRing, tolerance);

						if (! interiorIntersects)
						{
							// Check if it is completely inside (or at max touches the boundary)
							bool contained = GeomRelationUtils.PolycurveContainsXY(
								cutFootprintPart.ExteriorRing, resultPoly.ExteriorRing, tolerance);

							interiorIntersects = contained;
						}
					}
					else
					{
						interiorIntersects = GeomRelationUtils.InteriorIntersectXY(
							cutFootprintPart, resultPoly, tolerance);
					}

					if (interiorIntersects)
					{
						assignmentCount++;

						splitPartsByFootprintPart[cutFootprintPart].Add(resultPoly);
					}
				}

				Assert.AreEqual(1, assignmentCount,
				                "Unexpected number of assignments to footprint parts.");
			}
		}

		public static bool IsMultipatchWithDegenerateFootprint(IMultiPatch multipatch)
		{
			// Detects the issue reproduced in GeometryIssuesReproTest.Repro_ITopologicalOperator_Boundary_IncorrectForSpecificMultipatch_Plus_IRelationalOperator_Wrong()
			// Diagnosing the problem is limited to simple 1-ring multipatches - multi-ring multipatches are more complex / risky
			var geometryCollection = (IGeometryCollection) multipatch;

			if (geometryCollection.GeometryCount != 1)
			{
				return false;
			}

			double tolerance = GeometryUtils.GetXyTolerance(multipatch);

			var officialResult =
				(IPolyline) ((ITopologicalOperator) multipatch).Boundary;

			// TOP-5258 (the boundary difference can easily be larger than the tolerance even in 'normal' cases
			//          if the cut line runs close to an edge (the boundary is probably simplified in AO!)
			if (((IPointCollection) officialResult).PointCount != 5)
			{
				return false;
			}

			// TODO: Implement proper boundary/union calculation (GeomUtils) instead of this fuzzy stuff!
			//       -> Could also improve QA performance for tests that use the boundary as well.
			// -> The generated, unsimplified boundary could be checked for self-intersections or
			// collapsing on simplify in order to produce a more useful error message and potentially
			// offer to discard the degenerative part.
			// TODO: Probably the result should be returned anyway (using minimum-tolerance logic for
			// relational operators and a separate post-validation step should be performed.
			// This would allow for client-code to do whatever is appropriate after performing its
			// own due-diligence.

			var singleRing = (IRing) geometryCollection.Geometry[0];

			tolerance = 2 * Math.Sqrt(2) * tolerance;

			if (! MathUtils.AreEqual(singleRing.Length, officialResult.Length, tolerance))
			{
				var poly =
					(IPolygon) GeometryUtils.GetHighLevelGeometry(singleRing, true);
				GeometryUtils.Simplify(poly, true);

				return ! MathUtils.AreEqual(poly.Length, officialResult.Length,
				                            tolerance);
			}

			return false;
		}

		public static bool IsVerticalRing(IRing ring)
		{
			double tolerance = GeometryUtils.GetXyTolerance(ring);

			// For performance improvement:
			// Worst case: Half the diagonal tolerance offset times half the ring length (assuming sliver)
			double threshold = 1.5 * tolerance / 2 * ring.Length / 2;

			if (Math.Abs(((IArea) ring).Area) > threshold)
			{
				return false;
			}

			Linestring closedLinestring = GeometryConversionUtils.CreateLinestring(ring);

			return closedLinestring.IsVerticalRing(tolerance);
		}

		#endregion

		#region Trimmed cut line calculation

		[NotNull]
		private static IPath TrimCutLine([NotNull] IPath cutLine,
		                                 [NotNull] IPolygon originalPolygon,
		                                 [NotNull] IList<IGeometry> cutGeometries)
		{
			var intersectionPoints =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					(IPolyline) GeometryUtils.GetHighLevelGeometry(cutLine),
					originalPolygon, true,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			double tolerance = GeometryUtils.GetXyTolerance(originalPolygon);

			var highLevelPath = (IPolyline) GeometryUtils.GetHighLevelGeometry(cutLine);

			List<KeyValuePair<IPoint, double>> orderedIntersections =
				GeometryUtils.GetDistancesAlongPolycurve(
					intersectionPoints, highLevelPath, tolerance, false);

			// ascending sort order
			orderedIntersections.Sort((x, y) => x.Value.CompareTo(y.Value));

			double fromDistance =
				GetFirstIntersectionTouchingTwoGeometries(
					orderedIntersections, cutGeometries);

			orderedIntersections.Reverse();

			double toDistance = GetFirstIntersectionTouchingTwoGeometries(
				orderedIntersections, cutGeometries);

			ICurve result;
			if (fromDistance >= 0 && toDistance >= 0)
			{
				cutLine.GetSubcurve(fromDistance, toDistance, false, out result);
			}
			else
			{
				result = cutLine;
			}

			return (IPath) result;
		}

		private static double GetFirstIntersectionTouchingTwoGeometries(
			[NotNull] IEnumerable<KeyValuePair<IPoint, double>> intersections,
			[NotNull] IList<IGeometry> geometries)
		{
			double resultDistance = -1;

			foreach (KeyValuePair<IPoint, double> orderedIntersection in intersections)
			{
				if (TouchesTwoOrMoreGeometries(orderedIntersection.Key, geometries))
				{
					resultDistance = orderedIntersection.Value;
					break;
				}
			}

			return resultDistance;
		}

		private static bool TouchesTwoOrMoreGeometries(
			[NotNull] IPoint point, IEnumerable<IGeometry> geometries)
		{
			var touchCount = 0;

			foreach (IGeometry part in geometries)
			{
				IGeometry highLevelGeometry =
					GeometryUtils.GetHighLevelGeometry(part, true);

				if (GeometryUtils.Touches(point, highLevelGeometry))
				{
					touchCount++;

					if (touchCount >= 2)
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool PathRunsAlongTwoPolygons([NotNull] IPath path,
		                                             [NotNull] IList<IGeometry> polygons)
		{
			// For a potential performance improvement, see StickyIntersections
			var touchCount = 0;

			IGeometry highLevelPath = GeometryUtils.GetHighLevelGeometry(path, true);

			// it's a bag of polygons
			foreach (IGeometry polygon in polygons)
			{
				if (GeometryUtils.Touches(highLevelPath, polygon))
				{
					// filter out those that only touch in the start point and do not run along the boundary
					IPolyline outline = GeometryFactory.CreatePolyline(polygon);

					IPolyline lineAlongBoundary = IntersectionUtils.GetIntersectionLines(
						outline, (IPolycurve) highLevelPath, true, true);

					if (! lineAlongBoundary.IsEmpty)
					{
						touchCount++;

						if (touchCount == 2)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		#endregion
	}
}
