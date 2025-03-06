using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public static class ChangeGeometryAlongUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Limited reshape curve calculation without support for multiple-sources-as-union, adjust and preview-calculation
		/// </summary>
		/// <param name="sourceFeatures"></param>
		/// <param name="targetFeatures"></param>
		/// <param name="visibleExtent"></param>
		/// <param name="tolerance"></param>
		/// <param name="bufferOptions"></param>
		/// <param name="filterOptions"></param>
		/// <param name="resultSubcurves"></param>
		/// <param name="trackCancel"></param>
		/// <returns></returns>
		public static ReshapeAlongCurveUsability CalculateReshapeCurves(
			[NotNull] IList<IFeature> sourceFeatures,
			[NotNull] IList<IFeature> targetFeatures,
			[CanBeNull] IEnvelope visibleExtent,
			double tolerance,
			TargetBufferOptions bufferOptions,
			ReshapeCurveFilterOptions filterOptions,
			IList<CutSubcurve> resultSubcurves,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			ISubcurveCalculator curveCalculator = new ReshapableSubcurveCalculator();

			if (tolerance >= 0)
			{
				curveCalculator.CustomTolerance = tolerance;
			}

			return CalculateChangeAlongCurves(sourceFeatures, targetFeatures, visibleExtent,
			                                  tolerance, bufferOptions, filterOptions,
			                                  resultSubcurves, curveCalculator, trackCancel);
		}

		/// <summary>
		/// Cut curve calculation.
		/// </summary>
		/// <param name="sourceFeatures"></param>
		/// <param name="targetFeatures"></param>
		/// <param name="visibleExtent"></param>
		/// <param name="tolerance"></param>
		/// <param name="bufferOptions"></param>
		/// <param name="filterOptions"></param>
		/// <param name="resultSubcurves"></param>
		/// <param name="trackCancel"></param>
		/// <returns></returns>
		public static ReshapeAlongCurveUsability CalculateCutCurves(
			[NotNull] IList<IFeature> sourceFeatures,
			[NotNull] IList<IFeature> targetFeatures,
			[CanBeNull] IEnvelope visibleExtent,
			double tolerance,
			TargetBufferOptions bufferOptions,
			ReshapeCurveFilterOptions filterOptions,
			IList<CutSubcurve> resultSubcurves,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			ISubcurveCalculator curveCalculator = new CutPolygonSubcurveCalculator();

			if (tolerance >= 0)
			{
				curveCalculator.CustomTolerance = tolerance;
			}

			return CalculateChangeAlongCurves(sourceFeatures, targetFeatures, visibleExtent,
			                                  tolerance, bufferOptions, filterOptions,
			                                  resultSubcurves, curveCalculator, trackCancel);
		}

		/// <summary>
		/// Limited reshape curve calculation without support for multiple-sources-as-union,
		/// adjust and preview-calculation.
		/// </summary>
		/// <param name="sourceFeatures"></param>
		/// <param name="targetFeatures"></param>
		/// <param name="visibleExtent"></param>
		/// <param name="tolerance"></param>
		/// <param name="bufferOptions"></param>
		/// <param name="filterOptions"></param>
		/// <param name="resultSubcurves"></param>
		/// <param name="curveCalculator"></param>
		/// <param name="trackCancel"></param>
		/// <returns></returns>
		public static ReshapeAlongCurveUsability CalculateChangeAlongCurves(
			[NotNull] IList<IFeature> sourceFeatures,
			[NotNull] IList<IFeature> targetFeatures,
			[CanBeNull] IEnvelope visibleExtent,
			double tolerance,
			[NotNull] TargetBufferOptions bufferOptions,
			[NotNull] ReshapeCurveFilterOptions filterOptions,
			IList<CutSubcurve> resultSubcurves,
			[NotNull] ISubcurveCalculator curveCalculator,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			Assert.ArgumentCondition(
				sourceFeatures.All(
					f => curveCalculator.CanUseSourceGeometryType(
						DatasetUtils.GetShapeType(f.Class))),
				"Source feature list contains invalid geometry type(s)");

			Assert.ArgumentCondition(targetFeatures.All(CanUseAsTargetFeature),
			                         "Target feature list contains invalid features");

			if (sourceFeatures.Count == 0)
			{
				return ReshapeAlongCurveUsability.NoSource;
			}

			if (targetFeatures.Count == 0)
			{
				return ReshapeAlongCurveUsability.NoTarget;
			}

			visibleExtent = visibleExtent ?? UnionExtents(sourceFeatures, targetFeatures);

			IEnvelope clipExtent =
				GetClipExtent(visibleExtent,
				              bufferOptions.BufferTarget ? bufferOptions.BufferDistance : 0);

			curveCalculator.SubcurveFilter =
				new SubcurveFilter(new StaticExtentProvider(visibleExtent));

			IGeometry targetGeometry = BuildTargetGeometry(targetFeatures, clipExtent);

			IPolyline targetLine = PrepareTargetLine(
				sourceFeatures, targetGeometry, clipExtent, bufferOptions,
				out string _, trackCancel);

			if (targetLine == null || targetLine.IsEmpty)
			{
				return ReshapeAlongCurveUsability.NoTarget;
			}

			curveCalculator.Prepare(sourceFeatures, targetFeatures, clipExtent, filterOptions);

			ReshapeAlongCurveUsability result;
			if (sourceFeatures.Count == 1)
			{
				result = RecalculateReshapableSubcurves(
					sourceFeatures[0], targetLine, curveCalculator,
					resultSubcurves, trackCancel);
			}
			else
			{
				result = AddAdditionalSingleGeometryReshapeCurves(
					sourceFeatures, targetLine, null, curveCalculator, resultSubcurves,
					trackCancel);
			}

			// TODO: Adjust lines, Difference areas

			return result;
		}

		[CanBeNull]
		public static IEnvelope GetClipExtent([CanBeNull] IEnvelope visibleExtent,
		                                      double targetBufferDistance)
		{
			return EnlargeEnvelopeScope(visibleExtent, targetBufferDistance);
		}

		public static void PrepareSubcurveCalculator(
			ISubcurveCalculator subCurveCalculator,
			[NotNull] IList<IFeature> sourceFeatures,
			[NotNull] IList<IFeature> targetFeatures,
			bool useMinimalTolerance,
			ReshapeCurveFilterOptions filterOptions,
			[CanBeNull] IEnvelope clipExtent)
		{
			Assert.ArgumentNotNull(subCurveCalculator, nameof(subCurveCalculator));

			subCurveCalculator.UseMinimumTolerance = useMinimalTolerance;

			subCurveCalculator.Prepare(sourceFeatures, targetFeatures, clipExtent, filterOptions);
		}

		[CanBeNull]
		public static IPolyline PrepareTargetLine(
			[NotNull] IList<IFeature> selectedFeatures,
			[NotNull] IGeometry targetGeometry,
			[CanBeNull] IEnvelope clipExtent,
			[NotNull] TargetBufferOptions bufferOptions,
			out string reasonForNullOrEmpty,
			[CanBeNull] ITrackCancel trackCancel)
		{
			IGeometry target = targetGeometry;

			// Ensure target's SR already here
			ISpatialReference sr = GetShapeSpatialReference(selectedFeatures[0]);

			if (GeometryUtils.EnsureSpatialReference(target, sr))
			{
				_msg.Debug("Target geometry needed projection.");
			}

			reasonForNullOrEmpty = null;

			IPolyline targetLine =
				GetPreprocessedGeometryForExtent(target, clipExtent);

			if (targetLine.IsEmpty)
			{
				reasonForNullOrEmpty = "Reshape-along-target is outside main window extent.";
				return targetLine;
			}

			if (trackCancel != null && ! trackCancel.Continue())
			{
				reasonForNullOrEmpty = "Cancelled";
				return null;
			}

			if (bufferOptions.BufferTarget)
			{
				var bufferNotifications = new NotificationCollection();

				targetLine = BufferTargetLine(targetLine, bufferOptions, clipExtent,
				                              bufferNotifications, trackCancel);

				if (targetLine == null)
				{
					reasonForNullOrEmpty =
						$"Unable to buffer target geometry: {bufferNotifications.Concatenate(". ")}";
				}
			}

			return targetLine;
		}

		public static IPolyline GetPreprocessedGeometryForExtent(
			IGeometry geometry,
			[CanBeNull] IEnvelope clipExtent)
		{
			IPolyline processedGeometry;

			if (geometry.GeometryType == esriGeometryType.esriGeometryMultiPatch)
			{
				geometry = GeometryFactory.CreatePolygon(geometry);
			}

			if (clipExtent != null)
			{
				// NOTE: convert polygons to polylines first otherwise clipped target lines intersect the 
				//		 clipped polygon on the display boundary
				processedGeometry = GetClippedOutline(geometry, clipExtent);
			}
			else
			{
				processedGeometry = GeometryFactory.CreatePolyline(geometry);

				// For symmetry with clipped case: merge adjacent lines to avoid non-matching difference/target parts
				// in ReshapableSubcurveCalculator.GetTargetSegmentsAlong() used for minimum tolerance
				GeometryUtils.Simplify(processedGeometry, true, true);
			}

			return processedGeometry;
		}

		public static ReshapeAlongCurveUsability RecalculateReshapableSubcurves(
			[NotNull] IFeature sourceFeature,
			[NotNull] IPolyline targetPolyline,
			ISubcurveCalculator curveCalculator,
			IList<CutSubcurve> resultSubcurves,
			[CanBeNull] ITrackCancel trackCancel)
		{
			ReshapeAlongCurveUsability result;

			IGeometry editGeometry = sourceFeature.Shape;

			try
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return ReshapeAlongCurveUsability.Undefined;
				}

				result = curveCalculator.CalculateSubcurves(
					editGeometry, targetPolyline, resultSubcurves, trackCancel);

				foreach (CutSubcurve reshapeSubcurve in resultSubcurves)
				{
					reshapeSubcurve.Source = new GdbObjectReference(sourceFeature);
				}

				if (trackCancel != null && ! trackCancel.Continue())
				{
					return ReshapeAlongCurveUsability.Undefined;
				}
			}
			finally
			{
				Marshal.ReleaseComObject(editGeometry);
			}

			return result;
		}

		public static ReshapeAlongCurveUsability AddAdditionalSingleGeometryReshapeCurves(
			[NotNull] IList<IFeature> sourceFeatures,
			[NotNull] IPolyline targetPolyline,
			[CanBeNull] IPolyline unionLine,
			ISubcurveCalculator subcurveCalculator,
			IList<CutSubcurve> resultSubcurves,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var result = ReshapeAlongCurveUsability.Undefined;

			IEnumerable<CutSubcurve> allSingleGeoReshapeCurves =
				GetIndividualGeometriesReshapeCurves(sourceFeatures, targetPolyline,
				                                     subcurveCalculator);

			// where there are single and union reshape curves show the user only those reshape 
			// curves that reshape the union otherwise it can get confusing and several reshape 
			// lines overlap which cannot be seen.

			foreach (CutSubcurve singleGeometryReshapeCurve in allSingleGeoReshapeCurves)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return ReshapeAlongCurveUsability.Undefined;
				}

				// do not add those curves that are on the current union boundary 

				// TODO: if all (reshapable) curves are filtered out due to intersection with source union:
				// consider still adding them to avoid confusion (the target is not visible) or generally
				// add a fill symbol for all target features.
				if (unionLine != null)
				{
					IGeometry highLevelSubcurve = GeometryUtils.GetHighLevelGeometry(
						singleGeometryReshapeCurve.Path, true);

					if (HasLinearIntersections(unionLine, highLevelSubcurve))
					{
						continue;
					}
				}

				if (singleGeometryReshapeCurve.CanReshape)
				{
					result = ReshapeAlongCurveUsability.CanReshape;
				}

				resultSubcurves.Add(singleGeometryReshapeCurve);
			}

			if (result == ReshapeAlongCurveUsability.Undefined)
			{
				result = resultSubcurves.Count == 0
					         ? ReshapeAlongCurveUsability.NoReshapeCurves
					         : ReshapeAlongCurveUsability
						         .InsufficientOrAmbiguousReshapeCurves;
			}

			return result;
		}

		#region Input line preparation

		private static ISpatialReference GetShapeSpatialReference(
			[NotNull] IFeature selectedFeature)
		{
			IGeometry source = selectedFeature.Shape;

			ISpatialReference result = source.SpatialReference;

			Marshal.ReleaseComObject(source);

			return result;
		}

		[CanBeNull]
		private static IEnvelope EnlargeEnvelopeScope([CanBeNull] IEnvelope clipExtent,
		                                              double targetBufferDistance)
		{
			if (clipExtent == null)
			{
				return null;
			}

			IEnvelope envelopeScope = clipExtent;

			if (targetBufferDistance > 0)
			{
				// clone before changing (due to ClipExtent)
				envelopeScope = GeometryFactory.Clone(envelopeScope);

				// avoid buffer end artefacts
				envelopeScope.Expand(targetBufferDistance,
				                     targetBufferDistance, false);
			}

			return envelopeScope;
		}

		private static IPolyline GetClippedOutline(IGeometry geometry,
		                                           IEnvelope clipEnvelope)
		{
			IPolyline geometryAsLine = GeometryFactory.CreatePolyline(geometry);

			IPolyline result =
				GeometryUtils.GetClippedPolyline(geometryAsLine, clipEnvelope);

			// in case of cut polygon boundaries: merge the lines at poly start/end point:
			// NOTE: If there are short-ish segments, i.e. 2.5 times the tolerance, this results in changed segments and 
			//       hence a difference (and a reshape line) where there is no real difference. Consider enhancing SegmentReplacementUtils.JoinConnectedPaths()
			GeometryUtils.Simplify(result, true, true);

			Marshal.ReleaseComObject(geometryAsLine);

			return result;
		}

		[NotNull]
		private static IGeometry BuildTargetGeometry(
			[NotNull] ICollection<IFeature> targetFeatures,
			[CanBeNull] IEnvelope clipExtent)
		{
			var targetGeometries = new List<IGeometry>(targetFeatures.Count);
			foreach (IFeature targetFeature in targetFeatures)
			{
				IGeometry targetShape = targetFeature.Shape;

				targetGeometries.Add(
					GetPreprocessedGeometryForExtent(targetShape, clipExtent));

				Marshal.ReleaseComObject(targetShape);
			}

			IGeometry targetGeometry = GeometryUtils.UnionGeometries(targetGeometries);
			return targetGeometry;
		}

		[CanBeNull]
		private static IPolyline BufferTargetLine([NotNull] IPolyline targetLine,
		                                          [NotNull] TargetBufferOptions options,
		                                          [CanBeNull] IEnvelope envelopeScope,
		                                          [CanBeNull]
		                                          NotificationCollection bufferNotifications,
		                                          [CanBeNull] ITrackCancel trackCancel)
		{
			Assert.ArgumentNotNull(targetLine, nameof(targetLine));
			Assert.ArgumentCondition(! targetLine.IsEmpty,
			                         "BufferTargetLine: Target line is empty.");

			IPolyline result = null;

			IPolygon targetBuffer;

			if (AdjustUtils.TryBuffer(targetLine,
			                          options.BufferDistance,
			                          options.LogInfoPointThreshold,
			                          "Buffering target geometry...",
			                          bufferNotifications, out targetBuffer))
			{
				Assert.NotNull(targetBuffer, "targetBuffer");

				if (trackCancel != null && ! trackCancel.Continue())
				{
					return targetLine;
				}

				if (options.EnforceMinimumBufferSegmentLength)
				{
					// TODO: removing short segments is slow if many adjacent segments 
					//		 need to be removed.
					if (trackCancel != null && ! trackCancel.Continue())
					{
						return targetLine;
					}

					if (((IPointCollection) targetLine).PointCount > options.LogInfoPointThreshold)
					{
						_msg.Info("Removing short segments from buffer...");
					}

					EnforceMinimumSegmentLength(targetBuffer,
					                            options.BufferMinimumSegmentLength,
					                            envelopeScope);
				}

				result = GeometryFactory.CreatePolyline(targetBuffer);

				Marshal.ReleaseComObject(targetBuffer);
			}

			return result;
		}

		private static void EnforceMinimumSegmentLength(IGeometry geometry,
		                                                double minimumSegmentLength,
		                                                [CanBeNull] IEnvelope envelopeScope)
		{
			IPolygon scope = null;

			if (envelopeScope != null)
			{
				scope = GeometryFactory.CreatePolygon(envelopeScope);
			}

			RemoveShortSegments(geometry, minimumSegmentLength, scope);
		}

		private static void RemoveShortSegments(IGeometry geometry, double minimumLength,
		                                        IPolygon scope)
		{
			Assert.ArgumentNotNaN(minimumLength, nameof(minimumLength));
			Assert.ArgumentCondition(minimumLength > 0,
			                         "Minimum segment length must be larger than 0");

			var polycurve = geometry as IPolycurve;
			Assert.ArgumentCondition(polycurve != null,
			                         "Geometry is null or not a polycurve");

			SegmentReplacementUtils.RemoveShortSegments(
				polycurve, minimumLength, scope, null);
		}

		#endregion

		[NotNull]
		private static IEnumerable<CutSubcurve> GetIndividualGeometriesReshapeCurves(
			[NotNull] IList<IFeature> sourceFeatures,
			[NotNull] IPolyline targetPolyline,
			[NotNull] ISubcurveCalculator subcurveCalculator)
		{
			var result = new List<CutSubcurve>();

			_msg.DebugFormat(
				"GetIndividualGeometriesReshapeCurves: calculating curves for {0} geometries..",
				sourceFeatures.Count);

			foreach (IFeature sourceFeature in sourceFeatures)
			{
				IGeometry sourceGeometry = sourceFeature.Shape;

				var individualResultList = new List<CutSubcurve>();
				ReshapeAlongCurveUsability individualResult =
					subcurveCalculator.CalculateSubcurves(
						sourceGeometry, targetPolyline, individualResultList, null);

				foreach (CutSubcurve subcurve in individualResultList)
				{
					subcurve.Source = new GdbObjectReference(sourceFeature);
				}

				result.AddRange(individualResultList);

				_msg.VerboseDebug(
					() => $"Individual geometry's subcurve calculation result: {individualResult}");

				Marshal.ReleaseComObject(sourceGeometry);
			}

			return result;
		}

		private static bool HasLinearIntersections(
			[NotNull] IGeometry geometry1,
			[NotNull] IGeometry geometry2)
		{
			if (GeometryUtils.Intersects(geometry1, geometry2))
			{
				var topoOp = (ITopologicalOperator) geometry1;
				IGeometry intersection = topoOp.Intersect(
					geometry2, esriGeometryDimension.esriGeometry1Dimension);

				if (! intersection.IsEmpty)
				{
					return true;
				}
			}

			return false;
		}

		[NotNull]
		private static IEnvelope UnionExtents([NotNull] IList<IFeature> sourceFeatures,
		                                      [NotNull] IList<IFeature> targetFeatures)
		{
			IEnvelope result = null;

			foreach (IFeature sourceFeature in sourceFeatures)
			{
				if (result == null)
				{
					result = sourceFeature.Extent;
				}
				else
				{
					result.Union(sourceFeature.Extent);
				}
			}

			Assert.NotNull(result);

			foreach (IFeature targetFeature in targetFeatures)
			{
				result.Union(targetFeature.Extent);
			}

			double xyTolerance = GeometryUtils.GetXyTolerance(result);
			result.Expand(xyTolerance, xyTolerance, false);

			return result;
		}

		private static bool CanUseAsTargetFeature(IFeature target)
		{
			esriGeometryType? shapeType = DatasetUtils.GetShapeType(target.Class);

			return shapeType == esriGeometryType.esriGeometryPolygon ||
			       shapeType == esriGeometryType.esriGeometryPolyline ||
			       shapeType == esriGeometryType.esriGeometryMultiPatch;
		}
	}
}
