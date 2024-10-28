using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.Cracking;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Progress;

namespace ProSuite.Commons.AO.Geometry.Generalize
{
	public static class GeneralizeUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Calculates the segments shorter than the specified length per feature
		/// within the provided perimeter.
		/// </summary>
		/// <param name="forFeatureVertexInfos"></param>
		/// <param name="use2DLengthOnly"></param>
		/// <param name="perimeter"></param>
		/// <param name="trackCancel"></param>
		/// <returns>The short segments per feature</returns>
		public static void CalculateShortSegments(
			[NotNull] ICollection<FeatureVertexInfo> forFeatureVertexInfos,
			bool use2DLengthOnly,
			[CanBeNull] IGeometry perimeter,
			[CanBeNull] ITrackCancel trackCancel)
		{
			Assert.ArgumentNotNull(forFeatureVertexInfos, nameof(forFeatureVertexInfos));

			Stopwatch watch =
				_msg.DebugStartTiming(
					"Getting short segments for {0} features.",
					forFeatureVertexInfos.Count);

			var shortSegmentCount = 0;
			var shortFeatureCount = 0;

			foreach (FeatureVertexInfo vertexInfo in forFeatureVertexInfos)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return;
				}

				try
				{
					IList<esriSegmentInfo> removableSegments = CalculateShortSegments(vertexInfo,
						use2DLengthOnly,
						perimeter);

					shortSegmentCount += removableSegments.Count;
					shortFeatureCount++;
				}
				catch (Exception)
				{
					_msg.ErrorFormat("Error calculating generalized points for {0}",
					                 GdbObjectUtils.ToString(vertexInfo.Feature));
					throw;
				}
			}

			_msg.DebugStopTiming(watch,
			                     "Found {0} segments shorter than minimum segment length in {1} of {2} features.",
			                     shortSegmentCount, shortFeatureCount,
			                     forFeatureVertexInfos.Count);
		}

		public static IList<esriSegmentInfo> CalculateShortSegments(
			[NotNull] FeatureVertexInfo forFeatureVertexInfo,
			bool use2DLengthOnly,
			[CanBeNull] IGeometry perimeter)
		{
			_msg.VerboseDebug(
				() =>
					$"Getting short segments for {GdbObjectUtils.ToString(forFeatureVertexInfo.Feature)}");

			var minimumSegmentLength =
				(double) Assert.NotNull(forFeatureVertexInfo.MinimumSegmentLength,
				                        "Minimum segment length not set.");

			IList<esriSegmentInfo> shortSegments = GetShortSegments(
				forFeatureVertexInfo.Feature, perimeter, minimumSegmentLength, use2DLengthOnly);

			IList<esriSegmentInfo> protectedSegments = GetProtectedSegments(
				shortSegments, forFeatureVertexInfo.CrackPointCollection);

			IList<esriSegmentInfo> removableSegments =
				shortSegments.Where(shortSegment => ! protectedSegments.Contains(shortSegment))
				             .ToList();

			forFeatureVertexInfo.ShortSegments = removableSegments;

			forFeatureVertexInfo.NonRemovableShortSegments = protectedSegments;

			return removableSegments;
		}

		/// <summary>
		/// Deletes the short segments from the provided features and stores the features.
		/// </summary>
		/// <param name="fromFeatures"></param>
		/// <param name="updatedGeometries"></param>
		/// <param name="use2DLengthOnly"></param>
		/// <param name="inPerimeter"></param>
		/// <param name="progressFeedback"></param>
		/// <param name="cancel"></param>
		/// <param name="notifications"></param>
		public static void DeleteShortSegments(
			ICollection<FeatureVertexInfo> fromFeatures,
			IDictionary<IFeature, IGeometry> updatedGeometries,
			bool use2DLengthOnly,
			[CanBeNull] IGeometry inPerimeter,
			[CanBeNull] IProgressFeedback progressFeedback,
			[CanBeNull] ITrackCancel cancel,
			[CanBeNull] NotificationCollection notifications)
		{
			progressFeedback?.SetRange(0, fromFeatures.Count);

			var featureCount = 0;
			var totalRemovedCount = 0;

			foreach (FeatureVertexInfo featureVertexInfo in fromFeatures)
			{
				if (cancel != null && ! cancel.Continue())
				{
					return;
				}

				if (featureVertexInfo.ShortSegments == null ||
				    featureVertexInfo.ShortSegments.Count == 0)
				{
					continue;
				}

				Assert.NotNull(featureVertexInfo.MinimumSegmentLength,
				               "Minimum segment length not set.");

				var minSegmentLength = (double) featureVertexInfo.MinimumSegmentLength;

				try
				{
					IGeometry updateGeometry;
					if (
						! updatedGeometries.TryGetValue(featureVertexInfo.Feature,
						                                out updateGeometry))
					{
						updateGeometry = featureVertexInfo.Feature.ShapeCopy;
					}
					else
					{
						// the geometry was already updated - recalculate the short segments:
						featureVertexInfo.ShortSegments =
							GetShortSegments((IPolycurve) updateGeometry, inPerimeter,
							                 minSegmentLength,
							                 use2DLengthOnly);
					}

					var polycurveToUpdate = updateGeometry as IPolycurve;

					Assert.NotNull(polycurveToUpdate, "Feature's shape must be a polycurve");

					int removeCount = DeleteShortSegments(polycurveToUpdate, featureVertexInfo,
					                                      use2DLengthOnly, inPerimeter);

					if (removeCount > 0)
					{
						if (updatedGeometries.ContainsKey(featureVertexInfo.Feature))
						{
							updatedGeometries[featureVertexInfo.Feature] = polycurveToUpdate;
						}
						else
						{
							updatedGeometries.Add(featureVertexInfo.Feature, polycurveToUpdate);
						}

						featureCount++;
						totalRemovedCount += removeCount;
					}
				}
				catch (Exception)
				{
					_msg.InfoFormat("Error enforcing short segment in {0}",
					                GdbObjectUtils.ToString(featureVertexInfo.Feature));
					throw;
				}

				if (progressFeedback != null)
				{
					progressFeedback.Advance(
						"Stored {0} of {1} features with enforced segment length",
						featureCount, fromFeatures.Count);
				}
			}

			string msgFormat = featureCount == 1
				                   ? "Removed {0} segment(s) from {1} geometry"
				                   : "Removed {0} segment(s) from {1} geometries";

			_msg.InfoFormat(msgFormat, totalRemovedCount, featureCount);
		}

		/// <summary>
		/// Gets points weeded at the specified tolerance and under consideration of shared vertices if required.
		/// </summary>
		/// <param name="forFeatureVertexInfos"></param>
		/// <param name="weedTolerance"></param>
		/// <param name="only2D"></param>
		/// <param name="omitNonLinearSegments"></param>
		/// <param name="inPerimeter"></param>
		/// <param name="trackCancel"></param>
		/// <returns></returns>
		public static void CalculateWeedPoints(
			[NotNull] IEnumerable<FeatureVertexInfo> forFeatureVertexInfos,
			double weedTolerance,
			bool only2D,
			bool omitNonLinearSegments,
			[CanBeNull] IGeometry inPerimeter,
			[CanBeNull] ITrackCancel trackCancel)
		{
			foreach (FeatureVertexInfo featureVertexInfo in forFeatureVertexInfos)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return;
				}

				IFeature feature = featureVertexInfo.Feature;

				_msg.DebugFormat("Calculating weed points for {0}",
				                 GdbObjectUtils.ToString(feature));

				try
				{
					// intersect on perimeter and cut into separate paths at protected points
					IPolycurve originalGeometry = GetOriginalGeometry(feature,
						featureVertexInfo);
					IPointCollection weededPoints;
					try
					{
						weededPoints = CrackUtils.GetWeedPoints(
							originalGeometry, weedTolerance, only2D, inPerimeter,
							omitNonLinearSegments,
							DatasetUtils.GetSpatialReference(featureVertexInfo.Feature));
					}
					catch (Exception e)
					{
						_msg.Debug(
							$"Generalisation error for geometry {GeometryUtils.ToXmlString(originalGeometry)}",
							e);

						_msg.WarnFormat("Cannot generalize {0}: {1}", RowFormat.Format(feature),
						                e.Message);
						continue;
					}

					// NOTE: At least in advanced generalize consider only showing those crack points that
					//		 actually protect an existing vertex (i.e. only those on an existing vertex)
					//		 -> could be done in RemovableSegments
					//		 In theory it would also be nice to see the points that would be weeded if they were not protected
					//		 however this means double the processing and the result is not always consistent because
					//		 if an otherwise protected point is weeded some other point would probably remain (problem 
					//		 of different start point).
					//		 It might be helpful if only those crack points would be shown that are
					//		 actual vertices but again in case of un-selected neighbour it nicely shows the cut points
					//		 of the weeding and makes the weed points more plausible in certain cases.
					//		 -> consider making the symbol slightly bigger for real-vertex crack points (or square)

					// this is good for protecting shared vertices between selected and unselected features
					// for guaranteed identical generalization of shared edges between selected features
					// build topology graph!

					// NOTE: in theory this is only necessary if the input is not cut into separate paths at all protected points
					// -> consider only cutting at intersection points between sources (shared line end points)
					// NOTE: sometimes the intersection is rather inaccurate -> this still reduces points!
					featureVertexInfo.NonDeletablePoints = RemoveProtectedPoints(weededPoints,
						featureVertexInfo);

					featureVertexInfo.PointsToDelete = weededPoints;
				}
				catch (Exception)
				{
					_msg.ErrorFormat("Error calculating generalized points for {0}",
					                 GdbObjectUtils.ToString(feature));
					throw;
				}
			}
		}

		private static IPolycurve GetOriginalGeometry(
			[NotNull] IFeature feature,
			[CanBeNull] FeatureVertexInfo featureVertexInfo)
		{
			bool isPolygon = feature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon;

			if (featureVertexInfo == null)
			{
				return GeometryFactory.CreatePolyline(feature.Shape);
			}

			featureVertexInfo.SimplifyCrackPoints();

			_msg.VerboseDebug(
				() =>
					$"Cutting input geometry with protected points. Generalization Info: {featureVertexInfo.ToString(true)}");

			IPolyline originalGeometry = featureVertexInfo.OriginalClippedPolyline;

			if (featureVertexInfo.CrackPointCollection == null ||
			    featureVertexInfo.CrackPointCollection.PointCount == 0)
			{
				return originalGeometry;
			}

			// TODO: consider consolidating points equal in XY: get average Z (or Z from non-editable feature)
			//		 to really make coincident!
			IPointCollection protectedPoints = featureVertexInfo.CrackPointCollection;

			IGeometryCollection splittedResult = null;
			foreach (IPath path in GeometryUtils.GetPaths(originalGeometry))
			{
				bool splitHappenedAtFrom;

				double cutOffDistance = GeometryUtils.GetXyTolerance(originalGeometry);
				const bool projectPointsOntoPathToSplit = true;

				IGeometryCollection splittedPaths = GeometryUtils.SplitPath(
					path, protectedPoints, projectPointsOntoPathToSplit, cutOffDistance,
					out splitHappenedAtFrom);

				if (isPolygon && path.IsClosed && ! splitHappenedAtFrom &&
				    splittedPaths.GeometryCount > 1)
				{
					MergeLastWithFirstPart(splittedPaths);
				}

				if (splittedResult == null)
				{
					splittedResult = splittedPaths;
				}
				else
				{
					splittedResult.AddGeometryCollection(splittedPaths);
				}
			}

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug(
					() =>
						$"Original feature {GdbObjectUtils.ToString(feature)} splitted by protected points: {GeometryUtils.ToString((IGeometry) splittedResult)}");
			}

			return splittedResult as IPolycurve;
		}

		private static void MergeLastWithFirstPart(IGeometryCollection paths)
		{
			int lastIndex = paths.GeometryCount - 1;

			var lastPart = (ISegmentCollection) paths.get_Geometry(lastIndex);
			var firstPart = (ISegmentCollection) paths.get_Geometry(0);

			lastPart.AddSegmentCollection(firstPart);

			paths.RemoveGeometries(0, 1);
		}

		[CanBeNull]
		private static IPointCollection RemoveProtectedPoints(
			[NotNull] IPointCollection fromWeededPoints,
			[CanBeNull] FeatureVertexInfo featureVertexInfo)
		{
			if (featureVertexInfo?.CrackPointCollection == null)
			{
				return null;
			}

			// TODO: the removedPoints are missing the protected points due to shared segments
			//       -> also get weed points on the start/end point of shared segments/paths
			IPointCollection protectedPoints = featureVertexInfo.CrackPointCollection;

			IPointCollection removedPoints = CrackUtils.RemovePoints(fromWeededPoints,
				protectedPoints);

			//// TODO: Also remove those weed points that would change an adjacent segment on which there is
			//// a crack point (i.e. one without pre-existing vertex)

			//double searchTolerance = GeometryUtils.GetXyTolerance(featureVertexInfo.OriginalClippedPolyline);

			//foreach (IPoint point in GeometryUtils.GetPoints(fromWeededPoints))
			//{
			//    int partIndex;
			//    int segmentIndex = SegmentReplacementUtils.GetSegmentIndex(
			//        featureVertexInfo.OriginalClippedPolyline, point,
			//        searchTolerance,
			//        out partIndex, true);

			//    //IGeometry highLevelSegment = GeometryUtils.GetHighLevelGeometry(SegmentReplacementUtils.GetSegment(featureVertexInfo.OriginalClippedPolyline), true)
			//    //if (GeometryUtils.Disjoint())
			//}

			return removedPoints;
		}

		[NotNull]
		public static IList<esriSegmentInfo> GetShortSegments(
			[NotNull] IFeature feature,
			[CanBeNull] IGeometry inPerimeter,
			double minimumSegmentLength,
			bool use2DLengthOnly)
		{
			if (feature.Shape.GeometryType == esriGeometryType.esriGeometryMultiPatch)
			{
				Assert.ArgumentCondition(use2DLengthOnly == false,
				                         "Invalid parameter value for multipatch: use2DLengthOnly == true");
				return CalculateMultipatchShortSegments((IMultiPatch) feature.ShapeCopy,
				                                        minimumSegmentLength, inPerimeter);
			}

			Assert.True(
				feature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon ||
				feature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline,
				"GetShortSegments: Geometry type {0} is not valid.",
				feature.Shape.GeometryType);

			// always work on copy otherwise errors can occur on store
			var polycurve = (IPolycurve) feature.ShapeCopy;

			// projecting the perimeter could be done just once if all features have the same SR
			return GetShortSegments(polycurve, inPerimeter, minimumSegmentLength,
			                        use2DLengthOnly);
		}

		[NotNull]
		public static List<esriSegmentInfo> CalculateMultipatchShortSegments(
			[NotNull] IMultiPatch multipatch,
			double segmentLength3DToEnforce,
			[CanBeNull] IGeometry inPerimeter = null)
		{
			var sourceCollection = (IGeometryCollection) multipatch;

			var resultList = new List<esriSegmentInfo>();

			for (var i = 0; i < sourceCollection.GeometryCount; i++)
			{
				IGeometry originalGeometryPart = sourceCollection.get_Geometry(i);

				var highLevelPolycurve =
					(IPolycurve) GeometryUtils.GetHighLevelGeometry(originalGeometryPart);

				const bool use3DLength = true;
				IList<esriSegmentInfo> shortSegments =
					GeometryUtils.GetShortSegments((ISegmentCollection) highLevelPolycurve,
					                               segmentLength3DToEnforce,
					                               use3DLength, inPerimeter);

				resultList.AddRange(shortSegments);
			}

			return resultList;
		}

		[NotNull]
		public static IList<esriSegmentInfo> GetShortSegments(IPolycurve polycurve,
		                                                      IGeometry inPerimeter,
		                                                      double minimumSegmentLength,
		                                                      bool use2DLengthOnly)
		{
			IGeometry projectedPerimeter = null;
			if (inPerimeter != null)
			{
				if (GeometryUtils.EnsureSpatialReference(inPerimeter, polycurve.SpatialReference,
				                                         out projectedPerimeter))
				{
					_msg.DebugFormat("Perimeter was projected.");
				}

				if (GeometryUtils.Disjoint(polycurve, projectedPerimeter))
				{
					return new List<esriSegmentInfo>();
				}
			}

			bool use3DLength = GeometryUtils.IsZAware(polycurve) && ! use2DLengthOnly;

			IList<esriSegmentInfo> shortSegments;
			if (GeometryUtils.HasNonLinearSegments(polycurve) ||
			    ! IntersectionUtils.UseCustomIntersect)
			{
				shortSegments =
					GeometryUtils.GetShortSegments((ISegmentCollection) polycurve,
					                               minimumSegmentLength, use3DLength,
					                               projectedPerimeter);
			}
			else
			{
				MultiPolycurve multiPolycurve =
					GeometryConversionUtils.CreateMultiPolycurve(polycurve);

				EnvelopeXY aoi =
					inPerimeter != null
						? GeometryConversionUtils.CreateEnvelopeXY(inPerimeter.Envelope)
						: null;

				shortSegments = new List<esriSegmentInfo>();
				foreach (SegmentIndex segmentIndex in GeomUtils.GetShortSegmentIndexes(
					         multiPolycurve, minimumSegmentLength, use3DLength, aoi))
				{
					esriSegmentInfo segInfo = new esriSegmentInfo();
					segInfo.iPart = segmentIndex.PartIndex;
					segInfo.iRelSegment = segmentIndex.LocalIndex;
					segInfo.iAbsSegment =
						multiPolycurve.GetGlobalSegmentIndex(
							segmentIndex.PartIndex, segmentIndex.LocalIndex);
					segInfo.pSegment =
						((ISegmentCollection) polycurve).get_Segment(segInfo.iAbsSegment);

					shortSegments.Add(segInfo);
				}
			}

			if (projectedPerimeter != inPerimeter)
			{
				Marshal.ReleaseComObject(projectedPerimeter);
			}

			return shortSegments;
		}

		public static IList<esriSegmentInfo> GetFilteredSegments(
			[NotNull] IList<esriSegmentInfo> shortSegmentInfos,
			[CanBeNull] IGeometry selectionGeometry,
			bool mustContain)
		{
			IList<esriSegmentInfo> filteredSegments;

			if (selectionGeometry == null)
			{
				filteredSegments = shortSegmentInfos;
			}
			else
			{
				Predicate<IGeometry> selectionPredicate;
				if (mustContain)
				{
					Assert.True(selectionGeometry is IPolygon || selectionGeometry is IEnvelope,
					            "Unsupported selection geometry");

					selectionPredicate =
						highLevelSegment =>
							GeometryUtils.Contains(selectionGeometry, highLevelSegment);
				}
				else
				{
					selectionPredicate =
						highLevelSegment =>
							GeometryUtils.Intersects(selectionGeometry, highLevelSegment);
				}

				filteredSegments = GetFilteredSegments(shortSegmentInfos,
				                                       selectionPredicate);
			}

			return filteredSegments;
		}

		public static void CalculateProtectionPoints(
			[NotNull] ICollection<FeatureVertexInfo> generalizationInfos,
			[NotNull] ICollection<IFeature> selectedFeatures,
			IList<IFeature> vertexProtectingFeatures,
			bool linearizeNonLinearSegments,
			TargetFeatureSelection vertexProtectingFeatureSelection,
			ITrackCancel trackCancel)
		{
			_msg.DebugFormat(
				"Calculating topologically important vertices for {0} selected features...",
				selectedFeatures.Count);

			// Against unselected targets: use intersection point option 'all points'
			CrackPointCalculator crackPointCalculator = CreateProtectedPointsCalculator();

			crackPointCalculator.NonLinearSegmentTreatment =
				linearizeNonLinearSegments
					? NonLinearSegmentHandling.Linearize
					: NonLinearSegmentHandling.Omit;

			IEnumerable<IFeature> targetFeatures = vertexProtectingFeatures.Where(
				vertexProtectingFeature => ! selectedFeatures.Contains(vertexProtectingFeature));

			CrackUtils.AddTargetIntersectionCrackPoints(
				generalizationInfos, targetFeatures, vertexProtectingFeatureSelection,
				crackPointCalculator, trackCancel);

			// Against selected targets: use intersection point option 'linear intersection end points'
			// assuming that previously matching vertices from different features get weeded the same way
			crackPointCalculator.IntersectionPointOption =
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints;

			CrackUtils.AddTargetIntersectionCrackPoints(
				generalizationInfos,
				vertexProtectingFeatures.Where(selectedFeatures.Contains),
				vertexProtectingFeatureSelection, crackPointCalculator, trackCancel);
		}

		/// <summary>
		/// Creates a crack point calculator that selects also existing vertices as crack points.
		/// To calculate protected points shared with non-generalized geometries the intersection 
		/// option IncludeLinearIntersectionAllPoints should be used.
		/// To calculate protected points shared with geometries that also should be generalized
		/// the intersection option IncludeLinearIntersectionEndpoints should be used
		/// </summary>
		/// <param name="intersectionOption"></param>
		/// <returns></returns>
		public static CrackPointCalculator CreateProtectedPointsCalculator(
			IntersectionPointOptions intersectionOption =
				IntersectionPointOptions.IncludeLinearIntersectionAllPoints)
		{
			double? snapTolerance = null;

			// NOTE: do not set the minimum segment length here because a non-null value
			//		 means that crack points should be excluded if they are too close to the 
			//       next vertex -> make this more explicit by new property on VertexInfo
			double? minimumSegmentLength = null;
			const bool addCrackPointsAlsoOnExistingVertices = true;

			// distinguish between selected protecting features (only protect start/end points)
			// and un-selected protecting features (protect every intersecting vertex)
			var crackPointCalculator =
				new CrackPointCalculator(
					snapTolerance, minimumSegmentLength, addCrackPointsAlsoOnExistingVertices, false, intersectionOption, null);

			return crackPointCalculator;
		}

		private static IList<esriSegmentInfo> GetProtectedSegments(
			[NotNull] ICollection<esriSegmentInfo> fromShortSegments,
			[CanBeNull] IPointCollection crackPoints)
		{
			if (crackPoints == null)
			{
				return new List<esriSegmentInfo>(0);
			}

			IList<esriSegmentInfo> protectedSegments =
				new List<esriSegmentInfo>(fromShortSegments.Count);

			foreach (esriSegmentInfo shortSegment in fromShortSegments)
			{
				IPoint fromPoint = shortSegment.pSegment.FromPoint;

				if (! GeometryUtils.Intersects(fromPoint, (IGeometry) crackPoints))
				{
					continue;
				}

				IPoint toPoint = shortSegment.pSegment.ToPoint;

				if (GeometryUtils.IsSamePoint(fromPoint, toPoint, double.Epsilon, double.Epsilon))
				{
					// never protect 0-length segments
					continue;
				}

				if (GeometryUtils.Intersects(toPoint, (IGeometry) crackPoints))
				{
					protectedSegments.Add(shortSegment);
				}
			}

			return protectedSegments;
		}

		private static IList<esriSegmentInfo> GetFilteredSegments(
			IEnumerable<esriSegmentInfo> shortSegmentInfos, Predicate<IGeometry> predicate)
		{
			IList<esriSegmentInfo> filteredSegments = new List<esriSegmentInfo>();

			foreach (esriSegmentInfo shortSegmentInfo in shortSegmentInfos)
			{
				IGeometry highLevelSegment =
					GeometryUtils.GetHighLevelGeometry(shortSegmentInfo.pSegment, false);

				if (predicate(highLevelSegment))
				{
					filteredSegments.Add(shortSegmentInfo);
				}
			}

			return filteredSegments;
		}

		private static int DeleteShortSegments(
			[NotNull] IPolycurve fromPolycurve,
			[NotNull] FeatureVertexInfo vertexInfo,
			bool use2DLengthOnly,
			[CanBeNull] IGeometry inPerimeter)
		{
			IList<esriSegmentInfo> shortSegmentInfos = vertexInfo.ShortSegments;

			Assert.NotNull(shortSegmentInfos);

			_msg.DebugFormat("Deleting {0} short segments from {1}",
			                 shortSegmentInfos.Count,
			                 GdbObjectUtils.ToString(vertexInfo.Feature));

			Assert.NotNull(vertexInfo.MinimumSegmentLength, "Minimum segment length not set.");
			//double minimumSegmentLength = (double)vertexInfo.MinimumSegmentLength;

			var fromSegmentCollection = (ISegmentCollection) fromPolycurve;

			int originalSegmentCount = fromSegmentCollection.SegmentCount;

			RemoveShortSegments(fromPolycurve, vertexInfo, use2DLengthOnly,
			                    inPerimeter);

			fromSegmentCollection.SegmentsChanged();

			//// re-consider cost vs. information need:
			//int remainingCount = GetShortSegments(
			//    fromPolycurve, inPerimeter, minimumSegmentLength, use2DLengthOnly).Count;

			int deletedSegmentsCount = originalSegmentCount -
			                           fromSegmentCollection.SegmentCount;

			return deletedSegmentsCount;
		}

		/// <summary>
		/// Removes the shorts segments of the specified featureVertexInfo unless they are protected
		/// by the specified featureVertexInfo's CrackPoints. The minimum of the featureVertexInfo must
		/// be set.
		/// </summary>
		/// <param name="fromPolycurve"></param>
		/// <param name="featureVertexInfo"></param>
		/// <param name="use2DLengthOnly"></param>
		/// <param name="inPerimeter"></param>
		/// <returns></returns>
		private static void RemoveShortSegments(
			[NotNull] IPolycurve fromPolycurve,
			[NotNull] FeatureVertexInfo featureVertexInfo,
			bool use2DLengthOnly,
			[CanBeNull] IGeometry inPerimeter)
		{
			Assert.ArgumentNotNull(fromPolycurve, nameof(fromPolycurve));
			Assert.ArgumentNotNull(featureVertexInfo, nameof(featureVertexInfo));
			Assert.ArgumentCondition(featureVertexInfo.ShortSegments != null,
			                         "featureVertexInfo's ShortSegments is null");
			Assert.ArgumentCondition(featureVertexInfo.MinimumSegmentLength != null,
			                         "featureVertexInfo's MinimumSegmentLength is null");

			var notifications = new NotificationCollection();

			Assert.NotNull(featureVertexInfo.MinimumSegmentLength,
			               "Minimum segment length not set.");
			var minimumSegmentLength = (double) featureVertexInfo.MinimumSegmentLength;

			IList<esriSegmentInfo> shortSegments = featureVertexInfo.ShortSegments;

			SegmentReplacementUtils.RemoveShortSegments(fromPolycurve, shortSegments,
			                                            minimumSegmentLength, use2DLengthOnly,
			                                            featureVertexInfo.CrackPointCollection,
			                                            inPerimeter, notifications);

			if (notifications.Count > 0)
			{
				_msg.WarnFormat("Feature {0}: {1}",
				                GdbObjectUtils.ToString(featureVertexInfo.Feature),
				                notifications.Concatenate(" "));
			}
		}
	}
}
