using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class ReshapableSubcurveCalculator : ISubcurveCalculator
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region ISubcurveCalculator implementation

		public bool UseMinimumTolerance { get; set; }

		public double? CustomTolerance { get; set; }

		public SubcurveFilter SubcurveFilter { get; set; }

		public IEnvelope ClipExtent { get; set; }

		public bool CanUseSourceGeometryType(esriGeometryType geometryType)
		{
			return geometryType == esriGeometryType.esriGeometryPolyline ||
			       geometryType == esriGeometryType.esriGeometryPolygon;
		}

		public void Prepare(IEnumerable<IFeature> sourceFeatures,
		                    IList<IFeature> targetFeatures,
		                    IEnvelope processingExtent,
		                    ReshapeCurveFilterOptions filterOptions)
		{
			ClipExtent = processingExtent;

			IList<IGeometry> sourceGeometries = GdbObjectUtils.GetGeometries(sourceFeatures);

			// Consider remembering the pre-processed sources. But clipping is really fast.
			List<IPolyline> preprocessedSource =
				sourceGeometries
					.Select(
						g => ChangeGeometryAlongUtils.GetPreprocessedGeometryForExtent(
							g, processingExtent))
					.ToList();

			var targetGeometries = GdbObjectUtils.GetGeometries(targetFeatures);

			SubcurveFilter.PrepareFilter(
				preprocessedSource, targetGeometries, UseMinimumTolerance, filterOptions);

			foreach (IGeometry sourceGeometry in sourceGeometries)
			{
				Marshal.ReleaseComObject(sourceGeometry);
			}

			foreach (IGeometry targetGeometry in targetGeometries)
			{
				Marshal.ReleaseComObject(targetGeometry);
			}
		}

		/// <summary>
		/// Calculates the DifferenceLines and adds the subcurves to the provided result list.
		/// </summary>
		/// <param name="sourceGeometry"></param>
		/// <param name="targetPolyline"></param>
		/// <param name="resultList">All resulting subcurves including the ones that cannot be used to reshape</param>
		/// <param name="trackCancel"></param>
		/// <returns></returns>
		public ReshapeAlongCurveUsability CalculateSubcurves(
			IGeometry sourceGeometry,
			IPolyline targetPolyline,
			IList<CutSubcurve> resultList,
			ITrackCancel trackCancel)
		{
			Assert.ArgumentNotNull(sourceGeometry);
			Assert.ArgumentNotNull(targetPolyline);
			Assert.ArgumentNotNull(resultList);

			Stopwatch watch = _msg.DebugStartTiming();

			IPolyline preprocessedSourcePolyline =
				ChangeGeometryAlongUtils.GetPreprocessedGeometryForExtent(
					sourceGeometry, ClipExtent);

			if (preprocessedSourcePolyline.IsEmpty)
			{
				_msg.WarnFormat("Source feature is outside the processing extent.");
				return ReshapeAlongCurveUsability.NoSource;
			}

			IPointCollection intersectionPoints;
			IGeometryCollection differences = CalculateDifferences(
				preprocessedSourcePolyline,
				targetPolyline,
				trackCancel,
				out intersectionPoints);

			if (trackCancel != null && ! trackCancel.Continue())
			{
				return ReshapeAlongCurveUsability.Undefined;
			}

			if (differences == null)
			{
				return ReshapeAlongCurveUsability.AlreadyCongruent;
			}

			SubcurveFilter?.PrepareForSource(sourceGeometry);

			bool canReshape = CalculateReshapeSubcurves(
				preprocessedSourcePolyline, targetPolyline, differences,
				intersectionPoints, resultList, trackCancel);

			JoinNonForkingSubcurves(resultList);

			Marshal.ReleaseComObject(preprocessedSourcePolyline);
			Marshal.ReleaseComObject(differences);

			_msg.DebugStopTiming(
				watch, "RecalculateReshapableSubcurves: Total number of curves: {0}.",
				resultList.Count);

			if (canReshape)
			{
				return ReshapeAlongCurveUsability.CanReshape;
			}

			return resultList.Count == 0
				       ? ReshapeAlongCurveUsability.NoReshapeCurves
				       : ReshapeAlongCurveUsability.InsufficientOrAmbiguousReshapeCurves;
		}

		#endregion

		[CanBeNull]
		private IGeometryCollection CalculateDifferences(
			[NotNull] IPolyline preprocessedSourcePolyline,
			[NotNull] IPolyline targetPolyline,
			[CanBeNull] ITrackCancel trackCancel,
			out IPointCollection intersectionPoints)
		{
			double originalToleranceSource =
				GeometryUtils.GetXyTolerance(preprocessedSourcePolyline);
			double originalToleranceTarget = GeometryUtils.GetXyTolerance(targetPolyline);

			IGeometryCollection differences;
			intersectionPoints = null;

			try
			{
				if (UseMinimumTolerance)
				{
					GeometryUtils.SetMinimumXyTolerance(preprocessedSourcePolyline);
					GeometryUtils.SetMinimumXyTolerance(targetPolyline);
				}

				differences =
					GetReshapableAlongGeometries(preprocessedSourcePolyline,
					                             targetPolyline,
					                             originalToleranceSource);

				if (trackCancel != null && ! trackCancel.Continue())
				{
					return null;
				}

				if (differences == null)
				{
					return null;
				}

				// TODO: Consider using intersection point calculator from cracker in minimum-tolerance mode and / or
				//       use the target segments (GetTargetSegmentsAlong) also in non-minimum tolerance mode.
				//       This could possibly improve the situation from unit test CanReshapeAlongWithSmallIntersectionAngle
				const bool assumeIntersecting = true;
				intersectionPoints =
					(IPointCollection) IntersectionUtils.GetIntersectionPoints(
						(IPolyline) differences, preprocessedSourcePolyline,
						assumeIntersecting);
			}
			finally
			{
				if (UseMinimumTolerance)
				{
					// The Reshape Curves are sometimes not correct if not using the normal tolerance when splitting
					// (see unit test CanReshapeAlongEnsureCorrectTargetVerticesLocationWithMinimalTolerance)
					GeometryUtils.SetXyTolerance(preprocessedSourcePolyline,
					                             originalToleranceSource);
					GeometryUtils.SetXyTolerance(targetPolyline, originalToleranceTarget);

					if (intersectionPoints != null)
					{
						GeometryUtils.SetXyTolerance((IGeometry) intersectionPoints,
						                             originalToleranceSource);
					}

					// NOTE: difference will be cracked into reshape paths. However, if two points are closer than the tolerance
					//       the point (too) close to the intersection is eliminated by polycurve.SplitAtPoint resulting in
					//       a slightly changed reshape path -> keep the minimum tolerance for the difference to be more accurate.
				}
			}

			return differences;
		}

		private bool CalculateReshapeSubcurves(
			[NotNull] IPolyline preprocessedSourcePolyline,
			[NotNull] IPolyline targetPolyline,
			[NotNull] IGeometryCollection differences,
			[NotNull] IPointCollection intersectionPoints,
			[NotNull] ICollection<CutSubcurve> resultList,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var canReshape = false;

			foreach (
				CutSubcurve subcurve in
				CalculateReshapeSubcurves(preprocessedSourcePolyline, differences,
				                          intersectionPoints, targetPolyline))
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return canReshape;
				}

				if (subcurve.CanReshape)
				{
					canReshape = true;
				}

				subcurve.IsFiltered = SubcurveFilter != null &&
				                      SubcurveFilter.IsExcluded(subcurve);

				resultList.Add(subcurve);
			}

			return canReshape;
		}

		/// <summary>
		/// Calculates the CutSubcurves to reshape with, which are the difference-lines between the intersections
		/// with the sourcePolyline. CutSubcuves that cannot be used to reshape because they are disjoint or only intersect
		/// once are still added to the list of reshapeSubcurves.
		/// </summary>
		/// <param name="sourcePolyline">The source.</param>
		/// <param name="differences">The differences, i.e. where the target geometry is different from the source.</param>
		/// <param name="intersectionPoints"></param>
		/// <param name="targetPolyline"></param>
		/// <returns></returns>
		private IEnumerable<CutSubcurve> CalculateReshapeSubcurves(
			[NotNull] IPolyline sourcePolyline,
			[NotNull] IGeometryCollection differences,
			[NotNull] IPointCollection intersectionPoints,
			[NotNull] IPolyline targetPolyline)
		{
			Assert.ArgumentNotNull(sourcePolyline);
			Assert.ArgumentNotNull(differences);
			Assert.ArgumentCondition(! ((IGeometry) differences).IsEmpty,
			                         "Empty difference geometry.");
			Assert.ArgumentNotNull(targetPolyline);

			IDictionary<SubcurveNode, SubcurveNode> subcurveNodes =
				new Dictionary<SubcurveNode, SubcurveNode>();

			for (var i = 0; i < differences.GeometryCount; i++)
			{
				foreach (
					CutSubcurve subcurve in
					GetCutSubcurves(differences, i, sourcePolyline,
					                subcurveNodes, intersectionPoints, targetPolyline))
				{
					// Do not filter subcurves here because they are already connected via nodes. All 
					// subcurves should be maintained in the list to avoid orphaned subcurves in nodes.
					yield return subcurve;
				}
			}
		}

		/// <summary>
		/// Cuts the differences' part with the specified index by the specified cuttingGeometry
		/// and returns the cut subcurves that do not cross the differences' part
		/// </summary>
		/// <param name="differences"></param>
		/// <param name="differencePathIndexToSplit"></param>
		/// <param name="cuttingGeometry"></param>
		/// <param name="nodes"></param>
		/// <param name="intersectionPoints">IntersectionPoints between differences and cuttingGeometry.
		///   Should be calculated once and reused for performance reasons.</param>
		/// <param name="target"></param>
		/// <returns></returns>
		[NotNull]
		private IEnumerable<CutSubcurve> GetCutSubcurves(
			[NotNull] IGeometryCollection differences,
			int differencePathIndexToSplit,
			[NotNull] IGeometry cuttingGeometry,
			IDictionary<SubcurveNode, SubcurveNode> nodes,
			[NotNull] IPointCollection intersectionPoints,
			IPolyline target)
		{
			var curveToSplit = (IPath) differences.Geometry[differencePathIndexToSplit];

			IGeometry highLevelCurveToSplit = GeometryUtils.GetHighLevelGeometry(
				curveToSplit, true);

			// Enlarge search tolerance to avoid missing points because the intersection points are
			// 'found' between the target vertex and the actual source-target intersection.
			// But when using minimum tolerance, make sure we're not searching with the normal tolerance
			// otherwise stitch points close to target vertices are missed and the result becomes unnecessarily 
			// inaccurate by the two vertices being simplified (at data tolerance) into one intermediate.
			double searchToleranceFactor = 2 * Math.Sqrt(2);
			double stitchPointSearchTol =
				CustomTolerance ??
				GeometryUtils.GetXyTolerance((IGeometry) differences) * searchToleranceFactor;

			if (intersectionPoints.PointCount == 0 ||
			    GeometryUtils.Disjoint(cuttingGeometry, highLevelCurveToSplit))
			{
				_msg.VerboseDebug(
					() => "GetCutSubcurves: No intersections / disjoint geometries");

				yield return
					CreateCutSubcurve(curveToSplit, null, nodes, null, target,
					                  stitchPointSearchTol);
			}
			else
			{
				_msg.DebugFormat("GetCutSubcurves: Intersection Point Count: {0}.",
				                 intersectionPoints.PointCount);

				const bool projectPointsOntoPathToSplit = false;

				// NOTE: take tolerance from cutting geometry because the difference's spatial reference could 
				// have been changed to minimum tolerance which sometimes results in missed points due to cutOffDistance being too small
				double cutOffDistance = GeometryUtils.GetXyTolerance(cuttingGeometry);

				// the original subdivision at start/end point is not needed any more: merge if it doesn't touch 
				// any other difference part (to allow the other difference part to become a reshape candidate)
				IGeometryCollection subCurves = GeometryUtils.SplitPath(
					curveToSplit, intersectionPoints, projectPointsOntoPathToSplit,
					cutOffDistance,
					splittedCurves => UnlessTouchesOtherPart(splittedCurves, differences,
					                                         differencePathIndexToSplit));

				for (var i = 0; i < subCurves.GeometryCount; i++)
				{
					var subCurve = (IPath) subCurves.Geometry[i];

					bool? curveTouchesDifferentParts = SubCurveTouchesDifferentParts(
						subCurve,
						cuttingGeometry);

					IPath subCurveClone = GeometryFactory.Clone(subCurve);

					yield return
						CreateCutSubcurve(subCurveClone, intersectionPoints, nodes,
						                  curveTouchesDifferentParts, target,
						                  stitchPointSearchTol);
				}

				Marshal.ReleaseComObject(subCurves);
			}
		}

		private static bool? SubCurveTouchesDifferentParts(IPath subCurve,
		                                                   IGeometry cuttingGeometry)
		{
			bool? curveTouchesDifferentParts = null;

			if (((IGeometryCollection) cuttingGeometry).GeometryCount > 1)
			{
				double xyTolerance = GeometryUtils.GetXyTolerance(cuttingGeometry);

				IGeometry fromPointPart = GeometryUtils.GetHitGeometryPart(
					subCurve.FromPoint, cuttingGeometry, xyTolerance);

				IGeometry toPointPart = GeometryUtils.GetHitGeometryPart(
					subCurve.ToPoint, cuttingGeometry, xyTolerance);

				if (fromPointPart != toPointPart)
				{
					_msg.Debug("Line connecting different parts");
					curveTouchesDifferentParts = true;
				}
				else
				{
					curveTouchesDifferentParts = false;
				}
			}

			return curveTouchesDifferentParts;
		}

		private static CutSubcurve CreateCutSubcurve(
			[NotNull] IPath path,
			[CanBeNull] IPointCollection intersectionPoints,
			[NotNull] IDictionary<SubcurveNode, SubcurveNode> allNodes,
			bool? touchingDifferentParts,
			IPolyline targetPolyline,
			double stitchPointSearchTol)
		{
			var touchAtFromPoint = false;
			var touchAtToPoint = false;

			if (intersectionPoints != null)
			{
				touchAtFromPoint = GeometryUtils.Intersects(
					path.FromPoint, (IGeometry) intersectionPoints);

				touchAtToPoint = GeometryUtils.Intersects(
					path.ToPoint, (IGeometry) intersectionPoints);
			}

			var fromNode = new SubcurveNode(path.FromPoint.X, path.FromPoint.Y);

			if (allNodes.ContainsKey(fromNode))
			{
				fromNode = allNodes[fromNode];
			}
			else
			{
				allNodes.Add(fromNode, fromNode);
			}

			var toNode = new SubcurveNode(path.ToPoint.X, path.ToPoint.Y);

			if (allNodes.ContainsKey(toNode))
			{
				toNode = allNodes[toNode];
			}
			else
			{
				allNodes.Add(toNode, toNode);
			}

			var cutSubcurve = new CutSubcurve(path, touchAtFromPoint, touchAtToPoint,
			                                  fromNode, toNode, touchingDifferentParts);

			// Identify stitch points, i.e. points that do not exist in the target and should not end up in the 
			// reshaped geometry if several adjacent cutsubcurves are applied together.
			if (touchAtFromPoint)
			{
				IPoint point = cutSubcurve.Path.FromPoint;

				ISegment targetSegment;
				cutSubcurve.FromPointIsStitchPoint = IsNoTargetVertex(
					point, targetPolyline, stitchPointSearchTol, out targetSegment);

				cutSubcurve.TargetSegmentAtFromPoint = targetSegment;
			}

			if (touchAtToPoint)
			{
				IPoint point = cutSubcurve.Path.ToPoint;

				ISegment targetSegment;
				cutSubcurve.ToPointIsStitchPoint = IsNoTargetVertex(
					point, targetPolyline, stitchPointSearchTol, out targetSegment);

				cutSubcurve.TargetSegmentAtToPoint = targetSegment;
			}

			fromNode.ConnectedSubcurves.Add(cutSubcurve);
			toNode.ConnectedSubcurves.Add(cutSubcurve);

			return cutSubcurve;
		}

		private static bool IsNoTargetVertex(
			[NotNull] IPoint point,
			[NotNull] IPolyline target,
			double stitchPointSearchTol,
			[CanBeNull] out ISegment splitTargetSegment)
		{
			splitTargetSegment = null;

			int partIndex;

			int? targetVertexIdx = GeometryUtils.FindHitVertexIndex(
				target, point, stitchPointSearchTol,
				out partIndex);

			bool result = targetVertexIdx == null;

			if (result)
			{
				// The point did not exist in the target, get the original segment which is divided by the point in the CutSubcurve
				// -> it can be used to stitch the selected CutSubcurves back together when doing the actual reshape
				int? targetSegmentIdx = GeometryUtils.FindHitSegmentIndex(
					target, point, stitchPointSearchTol,
					out partIndex);

				if (targetSegmentIdx != null)
				{
					splitTargetSegment = GeometryUtils.GetSegment(
						(ISegmentCollection) target,
						partIndex, (int) targetSegmentIdx);
				}
			}

			return result;
		}

		private static bool UnlessTouchesOtherPart(
			[NotNull] IGeometryCollection splittedCurves,
			[NotNull] IGeometryCollection otherPartCollection,
			int thisPartIndex)
		{
			var firstPart = (ISegmentCollection) splittedCurves.Geometry[0];

			IPoint fromPoint = ((IPath) firstPart).FromPoint;

			var touchesOtherPart = false;
			for (var i = 0; i < otherPartCollection.GeometryCount; i++)
			{
				if (i != thisPartIndex)
				{
					var otherPart = (ICurve) otherPartCollection.Geometry[i];
					if (GeometryUtils.AreEqualInXY(fromPoint, otherPart.FromPoint) ||
					    GeometryUtils.AreEqualInXY(fromPoint, otherPart.ToPoint))
					{
						touchesOtherPart = true;
						break;
					}
				}
			}

			return ! touchesOtherPart;
		}

		/// <summary>
		/// Gets geometries along which the edit geometry (source) could be reshaped, i.e. the simplified
		/// difference between the source polyline and the target polyline. If they are identical null
		/// (instead of an empty geometry) is returned.
		/// </summary>
		/// <returns>The difference geometry</returns>
		[CanBeNull]
		private IGeometryCollection GetReshapableAlongGeometries(
			[NotNull] IPolyline sourcePolyline,
			[NotNull] IPolyline targetPolyline,
			double originalTolerance)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			IPolyline difference =
				GetDifferencePolylineXyz(targetPolyline, sourcePolyline);

			if (UseMinimumTolerance || CustomTolerance != null)
			{
				// Take the actual target segments from the actual target to avoid artificial part boundaries 
				// (like stitch points) in the interior of segments where the source and the target are almost congruent.
				// With these artificial part splits the split vertices are incorporated into the source by the reshape
				// even though they are not actually in the target.
				difference =
					GetTargetSegmentsAlong(targetPolyline, difference, originalTolerance);
			}

			_msg.DebugStopTiming(watch,
			                     "Calculated {0} differences between source and target geometry",
			                     ((IGeometryCollection) difference).GeometryCount);

			if (difference.IsEmpty)
			{
				Marshal.ReleaseComObject(difference);
				difference = null;
			}

			return (IGeometryCollection) difference;
		}

		/// <summary>
		/// Calculates the difference polyline between the input polyline and the differentFrom
		/// polyline. If the input geometries have Z values, additionally those pieces of the 
		/// onPolyline are added that intersect in XY but are different in Z.
		/// </summary>
		/// <param name="onPolyline">The polyline on which the result segments are located</param>
		/// <param name="differentFrom">The polyline from which the result segments must be different</param>
		/// <returns></returns>
		[NotNull]
		private IPolyline GetDifferencePolylineXyz(
			[NotNull] IPolyline onPolyline,
			[NotNull] IPolyline differentFrom)
		{
			double xyTolerance = CustomTolerance ?? GeometryUtils.GetXyTolerance(onPolyline);

			// For the Geom-implementation, use resolution / 2 because it is based on actual Z
			// differences without snapping intermediate results to spatial reference.
			double zTolerance =
				CustomTolerance ?? (UseMinimumTolerance
					                    ? GeometryUtils.GetZResolution(differentFrom) / 2
					                    : GeometryUtils.GetZTolerance(differentFrom));

			if (! GeometryUtils.IsZAware(onPolyline) || ! GeometryUtils.IsZAware(differentFrom))
			{
				zTolerance = double.NaN;
			}

			return ReshapeUtils.GetDifferencePolylineXyz(
				onPolyline, differentFrom, xyTolerance, zTolerance);
		}

		/// <summary>
		/// Gets the target segments that run along the alongPolyline. 
		/// Assumption: The along-polyline's paths do not interior-intersect multiple target paths, i.e.
		/// they are equal or a 'reduced' version of the target polyline (e.g. obtained by difference).
		/// </summary>
		/// <param name="targetPolyline"></param>
		/// <param name="alongPolyline"></param>
		/// <param name="originalTolerance"></param>
		/// <returns></returns>
		[NotNull]
		private IPolyline GetTargetSegmentsAlong(
			[NotNull] IPolyline targetPolyline,
			[NotNull] IPolyline alongPolyline,
			double originalTolerance)
		{
			// NOTE: The targetPolyline must be simplified i.e. the adjacent parts should be merged.

			double searchTolerance = CustomTolerance ?? originalTolerance;

			var exactDifferences =
				(IGeometryCollection) GeometryFactory.CreateEmptyGeometry(alongPolyline);

			foreach (IPath differencePath in GeometryUtils.GetPaths(alongPolyline))
			{
				IPath targetPath = GetUniqueTargetPathRunningAlong(
					targetPolyline, differencePath, searchTolerance);

				// NOTE: Sometimes, especially with non-linear segments and non-micro-resolution, this logic does not work because
				//       the difference between non-linear geometries can look extremely weird (i.e. incorrect multiparts)
				//       See CanReshapeAlongNonLinearSegmentsPolygonCircleWithMinimumTolerance() in GeometryReshaperTest
				if (targetPath == null)
				{
					_msg.DebugFormat(
						"Unable to use exact target geometry, because no unique target path found running along {0}. The standard difference (using minimum tolerance) is used instead.",
						GeometryUtils.ToString(differencePath));

					return alongPolyline;
				}

				Assert.NotNull(targetPath,
				               "No target part found when searching with the difference part");

				IGeometry exactDifferencePart;
				if (targetPath.IsClosed)
				{
					if (differencePath.IsClosed)
					{
						exactDifferencePart = GeometryFactory.Clone(targetPath);
					}
					else
					{
						IRing targetRing = GeometryFactory.CreateRing(targetPath);
						exactDifferencePart =
							GetRingSubcurveAlong(targetRing, differencePath);
					}
				}
				else
				{
					exactDifferencePart =
						SegmentReplacementUtils.GetSegmentsBetween(
							differencePath.FromPoint,
							differencePath.ToPoint, targetPath);
				}

				exactDifferences.AddGeometry(exactDifferencePart);
			}

			var result = (IPolyline) exactDifferences;

			GeometryUtils.Simplify(result, true, true);

			return result;
		}

		/// <summary>
		/// Gets the segments from the provided ring that run along the provided alongPath.
		/// </summary>
		/// <param name="targetRing"></param>
		/// <param name="alongPath"></param>
		/// <returns></returns>
		private static IGeometry GetRingSubcurveAlong([NotNull] IRing targetRing,
		                                              [NotNull] IPath alongPath)
		{
			IGeometry result = SegmentReplacementUtils.GetSegmentsBetween(
				alongPath.FromPoint,
				alongPath.ToPoint,
				targetRing);

			IGeometry highLevelResult =
				GeometryUtils.GetHighLevelGeometry(alongPath, true);

			IGeometry highLevelTarget = GeometryUtils.GetHighLevelGeometry(result, true);

			if (! GeometryUtils.InteriorIntersects(highLevelResult, highLevelTarget))
			{
				// try the other side of the ring
				result = SegmentReplacementUtils.GetSegmentsBetween(
					alongPath.ToPoint, alongPath.FromPoint, targetRing);
			}

			Marshal.ReleaseComObject(highLevelResult);
			Marshal.ReleaseComObject(highLevelTarget);

			return result;
		}

		[CanBeNull]
		private static IPath GetUniqueTargetPathRunningAlong(
			[NotNull] IPolyline targetPolyline,
			[NotNull] IPath testPath, double tolerance)
		{
			IPoint fromPoint = testPath.FromPoint;

			List<int> hitPartIndexes =
				GeometryUtils.FindPartIndices((IGeometryCollection) targetPolyline,
				                              fromPoint,
				                              tolerance).ToList();

			var targetCollection = (IGeometryCollection) targetPolyline;

			if (hitPartIndexes.Count == 1)
			{
				return (IPath) targetCollection.Geometry[hitPartIndexes[0]];
			}

			IPoint nearPoint = new PointClass();

			// Assemble all parts are also close to testPath's to-point
			IPoint toPoint = testPath.ToPoint;

			var candidates = new List<IPath>(hitPartIndexes.Count);

			foreach (int hitPartIndex in hitPartIndexes)
			{
				var candidate = (IPath) targetCollection.Geometry[hitPartIndex];

				double distanceTo = GeometryUtils.GetDistanceFromCurve(toPoint, candidate,
					nearPoint);

				if (distanceTo < tolerance)
				{
					candidates.Add(candidate);
				}
			}

			if (candidates.Count == 1)
			{
				return candidates[0];
			}

			// if there are still several, they start/end in the same point, check for interior intersection with the test path
			// NOTE: Use original tolerance because with the minimum tolerance / the very small map resolution misses 
			//       the part, especially when zoomed in a lot or with non-linear segments
			IGeometry highLevelTestPath = GeometryUtils.GetHighLevelGeometry(testPath);
			GeometryUtils.SetXyTolerance(highLevelTestPath, tolerance);

			foreach (IPath candidate in candidates)
			{
				IGeometry highLevelCandidate =
					GeometryUtils.GetHighLevelGeometry(candidate);

				if (GeometryUtils.InteriorIntersects(highLevelTestPath,
				                                     highLevelCandidate))
				{
					return candidate;
				}
			}

			return null;
		}

		private static void JoinNonForkingSubcurves(
			ICollection<CutSubcurve> inSubcurveCollection)
		{
			var removedCurves = new List<CutSubcurve>();
			var additionalCurves = new List<CutSubcurve>();

			foreach (CutSubcurve reshapeSubcurve in inSubcurveCollection)
			{
				if (removedCurves.Contains(reshapeSubcurve))
				{
					continue;
				}

				if (reshapeSubcurve.IsReshapeMemberCandidate)
				{
					// try building up a non-forking path from the non-touching end through a 
					// set of connected subcurves back to the geometry to reshape
					CutSubcurve currentInputCurve = reshapeSubcurve;
					CutSubcurve mergedCurve;

					while (reshapeSubcurve.TryJoinNonForkingNeighbourCandidates(
						       currentInputCurve, removedCurves, out mergedCurve))
					{
						currentInputCurve = mergedCurve;
					}

					if (currentInputCurve != reshapeSubcurve)
					{
						// one or more merges happened:
						additionalCurves.Add(currentInputCurve);
					}
				}
			}

			foreach (CutSubcurve removedCurve in removedCurves)
			{
				inSubcurveCollection.Remove(removedCurve);
			}

			foreach (CutSubcurve additionalCurve in additionalCurves)
			{
				inSubcurveCollection.Add(additionalCurve);
			}
		}
	}
}
