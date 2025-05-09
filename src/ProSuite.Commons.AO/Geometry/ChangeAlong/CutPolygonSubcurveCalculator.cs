using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
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

			// calculate classic subcurves
			var classicCurves = new List<CutSubcurve>();

			_standardSubcurveCalculator.CalculateSubcurves(sourceGeometry,
			                                               targetPolyline, classicCurves,
			                                               trackCancel);

			Predicate<CutSubcurve> canCut =
				cutSubcurve =>
					(cutSubcurve.CanReshape ||
					 cutSubcurve.IsReshapeMemberCandidate) &&
					GeometryUtils.InteriorIntersects(
						sourceGeometry,
						GeometryUtils.GetHighLevelGeometry(cutSubcurve.Path));

			List<CutSubcurve> usableClassicCurves =
				classicCurves.Where(cutSubcurve => canCut(cutSubcurve)).ToList();

			_msg.DebugFormat("Usable classic subcurves: {0} of {1}",
			                 usableClassicCurves.Count,
			                 classicCurves.Count);

			IPolygon sourcePolygon = sourceGeometry.GeometryType ==
			                         esriGeometryType.esriGeometryPolygon
				                         ? (IPolygon) sourceGeometry
				                         : GeometryFactory.CreatePolygon(sourceGeometry);

			Stopwatch watch =
				_msg.DebugStartTiming(
					"Calculating additional cut lines using topological operator");

			List<CutSubcurve> usableCutLines = CalculateUsableTopoOpCutLines(
				sourcePolygon, targetPolyline, usableClassicCurves);

			if (sourcePolygon != sourceGeometry)
			{
				Marshal.ReleaseComObject(sourcePolygon);
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
					if (! IsPathFullyCovered(trimmedPath, sourcePolygon,
					                         usableClassicCurves))
					{
						usableCutLines.Add(trimmedPath);
					}
				}
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
