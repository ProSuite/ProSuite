using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.CreateFootprint;
using ProSuite.Commons.AO.Geometry.Cut;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class CutPolygonSubcurveCalculator : ISubcurveCalculator
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly ISubcurveCalculator _standardSubcurveCalculator;

		public CutPolygonSubcurveCalculator()
		{
			_standardSubcurveCalculator = new ReshapableSubcurveCalculator();
		}

		public bool UseMinimumTolerance
		{
			get { return _standardSubcurveCalculator.UseMinimumTolerance; }
			set { _standardSubcurveCalculator.UseMinimumTolerance = value; }
		}

		public double? CustomTolerance
		{
			get { return _standardSubcurveCalculator.CustomTolerance; }
			set { _standardSubcurveCalculator.CustomTolerance = value; }
		}

		public SubcurveFilter SubcurveFilter
		{
			get { return _standardSubcurveCalculator.SubcurveFilter; }
			set { _standardSubcurveCalculator.SubcurveFilter = value; }
		}

		public IEnvelope ClipExtent
		{
			get { return _standardSubcurveCalculator.ClipExtent; }
			set { _standardSubcurveCalculator.ClipExtent = value; }
		}

		public void Prepare(IEnumerable<IFeature> selectedFeatures,
		                    IList<IFeature> targetFeatures,
		                    IEnvelope processingExtent,
		                    ReshapeCurveFilterOptions filterOptions)
		{
			_standardSubcurveCalculator.Prepare(selectedFeatures, targetFeatures,
			                                    processingExtent,
			                                    filterOptions);
		}

		public ReshapeAlongCurveUsability CalculateSubcurves(
			IGeometry sourceGeometry,
			IPolyline targetPolyline,
			IList<CutSubcurve> resultList,
			ITrackCancel trackCancel)
		{
			Assert.ArgumentNotNull(sourceGeometry, nameof(sourceGeometry));
			Assert.ArgumentNotNull(targetPolyline, nameof(targetPolyline));
			Assert.ArgumentNotNull(resultList, nameof(resultList));

			// For multipatch sources use the robust footprint (same routine as the cut itself,
			// CutGeometryUtils.TryCut) for ALL source-based reasoning below - the classic
			// subcurve calculation, the interior-intersection filter and the topological cut
			// lines. GeometryFactory.CreatePolygon / ITopologicalOperator.Boundary is unreliable
			// for freshly-cut multipatches (see CutGeometryUtils.IsMultipatchWithDegenerateFootprint
			// / TOP-5258): a stale boundary keeps the target line interior-intersecting the source,
			// so the cut subcurve stays green after the cut has already been applied.
			IPolygon sourceFootprint =
				sourceGeometry is IMultiPatch sourceMultipatch
					? CreateFootprintUtils.GetFootprint(
						sourceMultipatch, GeometryUtils.GetXyTolerance(sourceGeometry))
					: null;

			IGeometry effectiveSource = sourceFootprint ?? sourceGeometry;

			_msg.DebugFormat(
				"CutPolygonSubcurveCalculator: source is {0}, using {1} for subcurve calculation",
				sourceGeometry.GeometryType,
				sourceFootprint != null ? "robust footprint" : "source geometry");

			// calculate classic subcurves
			var classicCurves = new List<CutSubcurve>();

			_standardSubcurveCalculator.CalculateSubcurves(effectiveSource,
			                                               targetPolyline, classicCurves,
			                                               trackCancel);

			Predicate<CutSubcurve> canCut =
				cutSubcurve =>
					(cutSubcurve.CanReshape ||
					 cutSubcurve.IsReshapeMemberCandidate) &&
					GeometryUtils.InteriorIntersects(
						effectiveSource,
						GeometryUtils.GetHighLevelGeometry(cutSubcurve.Path));

			List<CutSubcurve> usableClassicCurves =
				classicCurves.Where(cutSubcurve => canCut(cutSubcurve)).ToList();

			_msg.DebugFormat("Usable classic subcurves: {0} of {1}",
			                 usableClassicCurves.Count,
			                 classicCurves.Count);

			Stopwatch watch =
				_msg.DebugStartTiming(
					"Calculating additional cut lines using topological operator");

			List<CutSubcurve> usableCutLines = CalculateUsableTopoOpCutLines(
				(IPolygon) effectiveSource, targetPolyline, usableClassicCurves);

			if (sourceFootprint != null)
			{
				Marshal.ReleaseComObject(sourceFootprint);
			}

			_msg.DebugStopTiming(watch, "Calculated {0} additional cut lines",
			                     usableCutLines.Count);

			foreach (CutSubcurve usableClassicCurve in usableClassicCurves)
			{
				resultList.Add(usableClassicCurve);
			}

			foreach (CutSubcurve usableTopoOpCutPath in usableCutLines)
			{
				resultList.Add(usableTopoOpCutPath);
			}

			return usableClassicCurves.Count == 0 && usableCutLines.Count == 0
				       ? ReshapeAlongCurveUsability.NoReshapeCurves
				       : ReshapeAlongCurveUsability.CanReshape;
		}

		public bool CanUseSourceGeometryType(esriGeometryType geometryType)
		{
			return geometryType == esriGeometryType.esriGeometryPolygon ||
			       geometryType == esriGeometryType.esriGeometryMultiPatch;
		}

		[NotNull]
		private static List<CutSubcurve> CalculateUsableTopoOpCutLines(
			[NotNull] IPolygon sourcePolygon,
			[NotNull] IPolyline targetPolyline,
			[NotNull] List<CutSubcurve> usableClassicCurves)
		{
			var usableCutLines = new List<IPath>();

			// Simplify target (in reshape the difference is simplified)
			GeometryUtils.Simplify(targetPolyline, true, true);

			// TODO: Consider using the correct Z-source also for the cut line calculation for correct feedback

			// Try with simple cut - using brute force.
			// in some situations it could yield additional curves (e.g. from-island-to-the-outside-and back-into-island)
			// NOTE: this is a best-effort enhancement on top of the classic curves. The brute-force
			// cut can hit geometry cases the cookie-cutter does not support (e.g. a cut ring that
			// touches the source outer ring in two points -> "Unexpected number of touching points"
			// in RingOperator.GetContainingRingIndex). Such a failure must not abort the whole
			// operation (the cut itself has already succeeded); just skip the additional lines.
			try
			{
				IList<IGeometry> cutResults = CutGeometryUtils.TryCut(
					sourcePolygon, targetPolyline, ChangeAlongZSource.Target);

				if (cutResults != null && cutResults.Count > 0)
				{
					// add those cut-lines that are not fully covered by classic curves within the source polygon
					foreach (IPath trimmedPath in
					         CutGeometryUtils.GetTrimmedCutLines(
						         targetPolyline, sourcePolygon, cutResults)
					        )
					{
						// A usable cut line must actually cross the source interior. After a cut
						// (e.g. multipatch cookie-cut), the target ring coincides with the now
						// shared boundary of a result piece; the brute-force cut re-detects it as a
						// candidate even though it only traces the boundary. The classic path already
						// rejects such lines via its InteriorIntersects filter, so apply the same gate
						// here - otherwise the just-applied cut stays drawn green.
						if (! IsPathFullyCovered(trimmedPath, sourcePolygon,
						                         usableClassicCurves) &&
						    GeometryUtils.InteriorIntersects(
							    sourcePolygon,
							    GeometryUtils.GetHighLevelGeometry(trimmedPath)))
						{
							usableCutLines.Add(trimmedPath);
						}
					}
				}
			}
			catch (Exception e)
			{
				_msg.Debug(
					"Additional (brute-force) cut lines could not be calculated. " +
					"Continuing with the classic cut curves only.", e);
			}

			// TODO: Is proper Touch- and Candidate-calculation required? If yes, extract base method from ReshapableSubcurveCalculator
			return usableCutLines.Select(
				                     cutPath => new CutSubcurve(cutPath, true, true))
			                     .ToList();
		}

		private static bool IsPathFullyCovered(
			[NotNull] IPath path,
			[NotNull] IPolygon insideSourcePolygon,
			[NotNull] IEnumerable<CutSubcurve> bySubCurves)
		{
			IPolyline classicCurvesPolyline = GeometryFactory.CreateEmptyPolyline(path);

			object missing = Type.Missing;
			foreach (IPath classicCurve in bySubCurves.Select(
				         cutSubcurve => cutSubcurve.Path))
			{
				((IGeometryCollection) classicCurvesPolyline).AddGeometry(classicCurve,
					ref missing,
					ref missing);
			}

			IGeometry highLevelPath = GeometryUtils.GetHighLevelGeometry(path);

			IGeometry highLevelPathInside =
				IntersectionUtils.GetIntersectionLines(
					(IPolyline) highLevelPath, insideSourcePolygon, true, true);

			IGeometry difference =
				ReshapeUtils.GetDifferencePolyline(
					(IPolyline) highLevelPathInside, classicCurvesPolyline);

			// Test: Simplify required?

			return difference.IsEmpty;
		}
	}
}
