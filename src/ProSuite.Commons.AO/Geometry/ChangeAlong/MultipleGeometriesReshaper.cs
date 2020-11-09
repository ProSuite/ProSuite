using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.LinearNetwork.Editing;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	[CLSCompliant(false)]
	public class MultipleGeometriesReshaper : GeometryReshaperBase
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private IGeometry _originalUnion;
		private IGeometry _unionToReshape;

		private Dictionary<IGeometry, IList<ReshapeInfo>> _individualReshapes;

		#region Constructors

		//TODO STS use editOperationObservers as property instead of constructor, analog FeatureCutter
		public MultipleGeometriesReshaper(
			[NotNull] ICollection<IFeature> featuresToReshape,
			IReshapeAlongOptions reshapeAlongOptions,
			IList<ToolEditOperationObserver> editOperationObservers)
			: this(featuresToReshape, editOperationObservers)
		{
			MaxProlongationLengthFactor =
				reshapeAlongOptions.AdjustModeMaxSourceLineProlongationFactor;

			MultipleSourcesTreatIndividually =
				reshapeAlongOptions.MultipleSourcesTreatIndividually;
			MultipleSourcesTreatAsUnion = reshapeAlongOptions.MultipleSourcesTreatAsUnion;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MultipleGeometriesReshaper"/> class.
		/// </summary>
		/// <param name="featuresToReshape">The features whose shape shall be reshaped</param>
		public MultipleGeometriesReshaper(
			[NotNull] ICollection<IFeature> featuresToReshape,
			[CanBeNull] IList<ToolEditOperationObserver> editOperationObservers)
			: base(featuresToReshape, editOperationObservers)
		{
			Assert.True(featuresToReshape.Count > 1,
			            "Use GeometryReshaper for single feature reshape");

			MaxProlongationLengthFactor = 8.0;

			ReshapeGeometryCloneByFeature =
				new Dictionary<IFeature, IGeometry>(featuresToReshape.Count);

			foreach (IFeature feature in featuresToReshape)
			{
				ReshapeGeometryCloneByFeature.Add(feature, feature.ShapeCopy);

				if (XyTolerance == null)
				{
					XyTolerance = GeometryUtils.GetXyTolerance(feature.Shape);
				}
				else if (! MathUtils.AreEqual(XyTolerance.Value,
				                              GeometryUtils.GetXyTolerance(feature.Shape)))
				{
					_msg.Debug(
						"Reshape multiple geometries: Not all features have the same spatial reference (xy tolerance).");
				}
			}

			// create the origin union first, before some of the geometries are
			// reshaped individually which breaks the initial topological situation
			// this ensures that all geometries are reshaped
			//_originalUnion = GeometryUtils.UnionGeometries(GeometriesToReshape);

			//GeometryUtils.Simplify(_originalUnion);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MultipleGeometriesReshaper"/> class.
		/// </summary>
		/// <param name="featuresToReshape">The features whose shape shall be reshaped</param>
		public MultipleGeometriesReshaper(
			[NotNull] ICollection<IFeature> featuresToReshape)
			: base(featuresToReshape, null)
		{
			Assert.True(featuresToReshape.Count > 1,
			            "Use GeometryReshaper for single feature reshape");

			MaxProlongationLengthFactor = 8.0;

			ReshapeGeometryCloneByFeature =
				new Dictionary<IFeature, IGeometry>(featuresToReshape.Count);

			foreach (IFeature feature in featuresToReshape)
			{
				ReshapeGeometryCloneByFeature.Add(feature, feature.ShapeCopy);

				if (XyTolerance == null)
				{
					XyTolerance = GeometryUtils.GetXyTolerance(feature.Shape);
				}
				else if (! MathUtils.AreEqual(XyTolerance.Value,
				                              GeometryUtils.GetXyTolerance(feature.Shape)))
				{
					_msg.Debug(
						"Reshape multiple geometries: Not all features have the same spatial reference (xy tolerance).");
				}
			}

			// create the origin union first, before some of the geometries are
			// reshaped individually which breaks the initial topological situation
			// this ensures that all geometries are reshaped
			//_originalUnion = GeometryUtils.UnionGeometries(GeometriesToReshape);

			//GeometryUtils.Simplify(_originalUnion);
		}

		#endregion

		public double MaxProlongationLengthFactor { private get; set; }

		public bool MultipleSourcesTreatIndividually { private get; set; }

		public bool MultipleSourcesTreatAsUnion { private get; set; }

		/// <summary>
		/// The source-target pairs where the source point is an intersection between source geometries
		/// and the target is the desired location of this intersection in the result.
		/// </summary>
		public StickyIntersections StickyIntersectionPoints { get; set; }

		#region Overrides

		/// <summary>
		/// Reshapes the features using the provided reshape path. The reshape path can be a sketch which
		/// is not necessarily cut at the intersections with the geometries to reshape (unlike the cut reshape 
		/// subcurves provided by Reshape Along).
		/// How the features are processed:
		/// - If MultipleSourcesTreatAsUnion is false: only individual reshapes
		/// - If MultipleSourcesTreatAsUnion is true:
		///		- Line features:
		///			- If the union of all feature's geometries build a single path: No individual reshape is
		///			  performed (would result in problems when combined with proportionate-distribution strategy).
		///			  Instead reshape-as-union is performed using the closest-point-on-reshape-line strategy by
		///			  default and if this results in flipped lines, the proportinate-distribution strategy is used
		///			  as fall-back.
		///			- If the union of all features' geometries is not a single path (or have overlaps): Only the 
		///			  individual reshapes are performend.
		///		- Polygon features: Always treat individually first, optionally (if MultipleSourcesTreatAsUnion is true)
		///		  treat as union. Open issue: only treat as union if they touch and have no interior intersections (i.e. shared boundaries)
		///		- Line and polygon features (mixed selection): Only individual reshape!
		/// </summary>
		/// <param name="reshapePath"></param>
		/// <param name="tryReshapeRingNonDefaultSide"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		public override IDictionary<IGeometry, NotificationCollection> Reshape(
			IPath reshapePath,
			bool tryReshapeRingNonDefaultSide,
			NotificationCollection notifications)
		{
			bool reshapeIndividually, reshapeAsUnion;
			GetRequiredReshapeType(out reshapeIndividually, out reshapeAsUnion);

			Assert.True(reshapeIndividually || reshapeAsUnion,
			            "No reshape type was determined");

			IDictionary<IGeometry, NotificationCollection> result =
				new Dictionary<IGeometry, NotificationCollection>(
					ReshapeGeometryCloneByFeature.Count);

			bool reshapedIndividually = false, reshapedAsUnion = false;

			var individualNotifications = new NotificationCollection();
			var unionNotifications = new NotificationCollection();

			if (reshapeIndividually)
			{
				result = ReshapeGeometriesIndividually(
					reshapePath, tryReshapeRingNonDefaultSide,
					individualNotifications);

				if (result.Count > 0)
				{
					// there was a successful reshape, but other selected features might want Z-updates
					EnsureZInNonReshapedNeighbors(
						reshapePath, ReshapeGeometryCloneByFeature.Values, result);
				}

				reshapedIndividually = result.Count > 0;
			}

			if (reshapeAsUnion)
			{
				IDictionary<IGeometry, NotificationCollection> multiReshapeResult;

				if (GetGeometryType(ReshapeGeometryCloneByFeature.Values) ==
				    esriGeometryType.esriGeometryPolygon)
				{
					if (StickyIntersectionPoints == null)
					{
						StickyIntersectionPoints =
							new StickyIntersections(ReshapeGeometryCloneByFeature.Keys);
					}

					Dictionary<IGeometry, IGeometry> reshapeGeometryCloneByOriginal =
						ReshapeGeometryCloneByFeature.ToDictionary(pair => pair.Key.Shape,
							pair => pair.Value);

					var stickyIntersectionReshaper =
						new StickyIntersectionsMultiplePolygonReshaper(
							reshapeGeometryCloneByOriginal, _individualReshapes,
							StickyIntersectionPoints)
						{
							RefreshArea = RefreshArea,
							AddAutomaticSourceTargetPairs = MultipleSourcesTreatAsUnion
						};

					multiReshapeResult =
						stickyIntersectionReshaper.ReshapeGeometries(
							reshapePath, unionNotifications);

					AddCombinedReshapeRange(multiReshapeResult, result);

					EnsureTargetIntersectionPoints(result.Keys,
					                               stickyIntersectionReshaper
						                               .UsedTargetIntersections);
				}
				else
				{
					// implementation for lines and polygons-as-union in ReshapeAlong (no Sticky Intersections) implementation
					multiReshapeResult =
						ReshapeGeometriesAsUnion(
							new List<IGeometry>(ReshapeGeometryCloneByFeature.Values),
							reshapePath, tryReshapeRingNonDefaultSide,
							unionNotifications);

					AddCombinedReshapeRange(multiReshapeResult, result);
				}

				reshapedAsUnion = multiReshapeResult.Count > 0;
			}

			// TODO: specific reshape notification class to handle optional, warning-level etc. notifications
			if (notifications != null)
			{
				if (reshapedAsUnion && ! reshapedIndividually)
				{
					// only add union notifications, omit the reasons why the individual reshape did not work
					notifications.AddRange(unionNotifications);
				}
				else // add both notifications
				{
					notifications.AddRange(individualNotifications);
					notifications.AddRange(unionNotifications);
				}
			}

			return result;
		}

		public override IDictionary<IGeometry, NotificationCollection> Reshape(
			IList<IPath> reshapePaths, bool tryReshapeRingNonDefaultSide,
			NotificationCollection notifications)
		{
			Assert.ArgumentCondition(reshapePaths.Count > 0,
			                         "No reshape paths provided");

			IDictionary<IGeometry, NotificationCollection> result =
				new Dictionary<IGeometry, NotificationCollection>(
					GeometriesToReshape.Count);

			IPolygon sketchAsPolygon = null;

			if (RemoveClosedReshapePathAreas)
			{
				sketchAsPolygon = GeometryFactory.CreatePolygon(reshapePaths);

				if (! sketchAsPolygon.IsEmpty)
				{
					reshapePaths =
						GetRemainingNonHolePaths(reshapePaths, sketchAsPolygon);
				}
			}

			foreach (IPath reshapePath in reshapePaths)
			{
				AddCombinedReshapeRange(
					Reshape(reshapePath, tryReshapeRingNonDefaultSide, notifications),
					result);
			}

			Assert.NotNull(result);

			// Now remove the closed rings from the reshaped polygons
			if (sketchAsPolygon != null && ! sketchAsPolygon.IsEmpty)
			{
				ICollection<IPolygon> polygonsToReshape =
					GeometriesToReshape.OfType<IPolygon>().ToList();

				foreach (IPolygon reshapedPolygon in polygonsToReshape)
				{
					RemoveArea(sketchAsPolygon, reshapedPolygon, result, notifications);
				}
			}

			return result;
		}

		/// <summary>
		/// Reshapes the geometries according to the properties MultipleSourcesTreatIndividually
		/// and MultipleSourcesTreatAsUnion and the reshape curves in the provided reshapeAlongCurves.
		/// </summary>
		/// <param name="reshapeAlongCurves"></param>
		/// <param name="canReshapePredicate"></param>
		/// <param name="useNonDefaultReshapeSide"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		protected override Dictionary<IGeometry, NotificationCollection> ReshapeCore(
			IReshapeAlongCurves reshapeAlongCurves,
			Predicate<IPath> canReshapePredicate,
			bool useNonDefaultReshapeSide,
			NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(reshapeAlongCurves, nameof(reshapeAlongCurves));

			var reshapedGeometries = new Dictionary<IGeometry, NotificationCollection>();

			if (MultipleSourcesTreatIndividually)
			{
				const bool includeAllPreSelectedCandidates = true;

				IList<CutSubcurve> selectedReshapeCurves =
					reshapeAlongCurves.GetSelectedReshapeCurves(
						canReshapePredicate, includeAllPreSelectedCandidates);

				reshapedGeometries =
					ReshapeGeometriesIndividually(selectedReshapeCurves, useNonDefaultReshapeSide,
					                              notifications);
			}

			List<CutSubcurve> combinedGeometriesReshapeCurves =
				reshapeAlongCurves.GetCombinedGeometriesReshapeCurves(
					canReshapePredicate, true);

			if (combinedGeometriesReshapeCurves.Count > 0 && MultipleSourcesTreatAsUnion)
			{
				// add also single reshape paths because they can also change the union which
				// by modifying the base geometries: the individual geometry reshape path is 
				// also needed to connect to (union reshape pahts are typically shorter)
				const bool includeAllPreSelectedCandidates = true;

				IList<CutSubcurve> selectedReshapeCurves =
					reshapeAlongCurves.GetSelectedReshapeCurves(
						canReshapePredicate, includeAllPreSelectedCandidates);

				combinedGeometriesReshapeCurves.AddRange(selectedReshapeCurves);

				IEnumerable<KeyValuePair<IGeometry, NotificationCollection>>
					combinedReshapes =
						ReshapeGeometriesAsUnion(combinedGeometriesReshapeCurves,
						                         useNonDefaultReshapeSide,
						                         notifications);

				AddCombinedReshapeRange(combinedReshapes, reshapedGeometries);
			}

			return reshapedGeometries;
		}

		protected override Dictionary<IGeometry, NotificationCollection> ReshapeCore(
			IList<CutSubcurve> reshapeCurves,
			bool useNonDefaultReshapeSide,
			NotificationCollection notifications)
		{
			var reshapedGeometries = new Dictionary<IGeometry, NotificationCollection>();

			if (MultipleSourcesTreatIndividually)
			{
				reshapedGeometries =
					ReshapeGeometriesIndividually(
						reshapeCurves, useNonDefaultReshapeSide, notifications);
			}

			IList<CutSubcurve> combinedGeometriesReshapeCurves =
				reshapeCurves.Where(c => c.Source == null).ToList();

			if (combinedGeometriesReshapeCurves.Count > 0 && MultipleSourcesTreatAsUnion)
			{
				// Include also single reshape paths because they can also change the union 
				// by modifying the base geometries: the individual geometry reshape path is 
				// also needed to connect to (union reshape paths are typically shorter)
				combinedGeometriesReshapeCurves = reshapeCurves;
				//combinedGeometriesReshapeCurves.AddRange(
				//	reshapeAlongCurves.GetSelectedReshapeCurves(canReshapePredicate,
				//	                                            includeAllPreSelectedCandidates));

				IEnumerable<KeyValuePair<IGeometry, NotificationCollection>>
					combinedReshapes = ReshapeGeometriesAsUnion(
						combinedGeometriesReshapeCurves, useNonDefaultReshapeSide,
						notifications);

				AddCombinedReshapeRange(combinedReshapes, reshapedGeometries);
			}

			return reshapedGeometries;
		}

		protected override void StoreReshapedGeometryCore(
			IFeature feature,
			IGeometry newGeometry,
			NotificationCollection notifications)
		{
			if (MoveLineEndJunction && NetworkFeatureFinder != null &&
			    newGeometry.GeometryType == esriGeometryType.esriGeometryPolyline)
			{
				_msg.DebugFormat(
					"Saving reshape in network feature {0} and moving adjacent edges...",
					GdbObjectUtils.ToString(feature));

				LinearNetworkNodeUpdater linearNetworkUpdater = NetworkFeatureUpdater ??
				                                                new LinearNetworkNodeUpdater(
					                                                NetworkFeatureFinder);

				linearNetworkUpdater.BarrierGeometryOriginal =
					_originalUnion as IPolyline;
				linearNetworkUpdater.BarrierGeometryChanged =
					_unionToReshape as IPolyline;

				linearNetworkUpdater.ExcludeFromEndpointRelocation =
					new HashSet<IFeature>(ReshapeGeometryCloneByFeature.Keys);

				linearNetworkUpdater.UpdateFeature(feature, newGeometry);

				AddToRefreshArea(linearNetworkUpdater.RefreshEnvelope);
			}
			else
			{
				base.StoreReshapedGeometryCore(feature, newGeometry, notifications);
			}
		}

		public override bool AddRefreshAreaPadding => StickyIntersectionPoints != null &&
		                                              StickyIntersectionPoints.HasTargetPoints();

		public override IDictionary<IGeometry, NotificationCollection>
			EnsureResultsNotOverlapping(
				IDictionary<IGeometry, NotificationCollection> reshapedGeometries,
				IList<IPath> reshapePaths)
		{
			var handledContaining = new List<IGeometry>();

			foreach (KeyValuePair<IGeometry, IGeometry> tuple in
				CollectionUtils.GetAllTuples(reshapedGeometries.Keys))
			{
				var keyPolygon = tuple.Key as IPolygon;
				var valuePolygon = tuple.Value as IPolygon;

				if (keyPolygon == null || valuePolygon == null ||
				    handledContaining.Contains(keyPolygon) ||
				    handledContaining.Contains(valuePolygon))
				{
					continue;
				}

				if (GeometryUtils.Contains(keyPolygon, valuePolygon))
				{
					RemoveContainedGeometries(reshapedGeometries, keyPolygon,
					                          reshapePaths);
					handledContaining.Add(keyPolygon);
				}
				else if (GeometryUtils.Contains(valuePolygon, keyPolygon))
				{
					RemoveContainedGeometries(reshapedGeometries, valuePolygon,
					                          reshapePaths);
					handledContaining.Add(valuePolygon);
				}
			}

			return reshapedGeometries;
		}

		#endregion

		private void GetRequiredReshapeType(out bool reshapeIndividually,
		                                    out bool reshapeAsUnion)
		{
			IList<IGeometry> geometries = ReshapeGeometryCloneByFeature.Values.ToList();

			esriGeometryType geometryType = GetGeometryType(geometries);

			switch (geometryType)
			{
				case esriGeometryType.esriGeometryPolygon:
					// use assignment made by caller
					reshapeIndividually = MultipleSourcesTreatIndividually;

					reshapeAsUnion =
						MultipleSourcesTreatAsUnion ||
						StickyIntersectionPoints != null &&
						StickyIntersectionPoints.HasTargetPoints();

					break;
				case esriGeometryType.esriGeometryPolyline:
					// polyline: depends...

					InitializeOriginalUnion(geometries);

					if (((IGeometryCollection) _originalUnion).GeometryCount == 1)
					{
						// it's a line string that can be union-reshaped: only single reshape if no union-reshape
						reshapeAsUnion =
							MultipleSourcesTreatAsUnion ||
							StickyIntersectionPoints != null &&
							StickyIntersectionPoints.HasTargetPoints();
						reshapeIndividually = ! reshapeAsUnion;
					}
					else
					{
						// probably interior-intersecting lines that should be unioned more efficiently:
						reshapeIndividually = true;
						reshapeAsUnion = false;
					}

					break;
				case esriGeometryType.esriGeometryAny:
					// mixed selection
					reshapeIndividually = true;
					reshapeAsUnion = false;

					_msg.WarnFormat(
						"The selection contains various geometry types. Only single-feature reshapes are performed.");
					break;
				default:
					throw new InvalidOperationException("Unsupported geometry type");
			}
		}

		private static esriGeometryType GetGeometryType(IEnumerable<IGeometry> geometries)
		{
			var geometryType = esriGeometryType.esriGeometryNull;

			foreach (IGeometry geometry in geometries)
			{
				if (geometryType == esriGeometryType.esriGeometryNull)
				{
					geometryType = geometry.GeometryType;
				}
				else if (geometryType != geometry.GeometryType)
				{
					// mixed:
					return esriGeometryType.esriGeometryAny;
				}
			}

			Assert.False(geometryType == esriGeometryType.esriGeometryNull,
			             "No geometries to reshape");

			return geometryType;
		}

		/// <summary>
		/// Rehshapes all the provided geometries individually and calculates the reshape side
		/// regardless of the other geometries in the list. No Target features are updated as
		/// the provided reshape path is typically the edit sketch geometry.
		/// </summary>
		/// <param name="sketchPath"></param>
		/// <param name="tryReshapeRingNonDefaultSide"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		[NotNull]
		private IDictionary<IGeometry, NotificationCollection>
			ReshapeGeometriesIndividually
			([NotNull] IPath sketchPath,
			 bool tryReshapeRingNonDefaultSide,
			 [CanBeNull] NotificationCollection notifications)
		{
			var reshapedGeometries =
				new Dictionary<IGeometry, NotificationCollection>(
					ReshapeGeometryCloneByFeature.Count);

			Assert.Null(TargetFeatures,
			            "Target features are set. This method does not support target feature updates.");

			// for subsequent use in polygon-shared-boundary-Y-reshape
			_individualReshapes = new Dictionary<IGeometry, IList<ReshapeInfo>>();

			foreach (
				KeyValuePair<IFeature, IGeometry> featureGeometryPair in
				ReshapeGeometryCloneByFeature)
			{
				IGeometry geometryToReshape = featureGeometryPair.Value;

				var singleGeometryNotification = new NotificationCollection();

				var reshapeInfo = new ReshapeInfo(geometryToReshape, sketchPath,
				                                  singleGeometryNotification)
				                  {
					                  ReshapeResultFilter =
						                  GetResultFilter(tryReshapeRingNonDefaultSide),
					                  AllowSimplifiedReshapeSideDetermination =
						                  AllowSimplifiedReshapeSideDetermination
				                  };

				IList<ReshapeInfo> singleReshapeInfos;
				bool reshaped = ReshapeUtils.ReshapeAllGeometryParts(
					reshapeInfo, sketchPath,
					out singleReshapeInfos);

				if (reshaped)
				{
					reshaped = AreAllReshapesAllowed(singleReshapeInfos, notifications);
				}

				if (reshaped)
				{
					AddToRefreshArea(singleReshapeInfos);

					reshapedGeometries.Add(geometryToReshape, singleGeometryNotification);

					NotificationIsWarning |= reshapeInfo.NotificationIsWarning;

					if (_individualReshapes != null)
					{
						_individualReshapes.Add(geometryToReshape, singleReshapeInfos);
					}
					else
					{
						foreach (ReshapeInfo singleReshapeInfo in singleReshapeInfos)
						{
							singleReshapeInfo.Dispose();
						}
					}
				}
				else
				{
					NotificationUtils.Add(
						notifications,
						string.Format("{0}: {1}",
						              RowFormat.Format(featureGeometryPair.Key, true),
						              singleGeometryNotification.Concatenate(". ")));
				}
			}

			return reshapedGeometries;
		}

		private IPath ConnectPathsUsingPolyBoundary(IPolyline disconnectedPaths,
		                                            IPolygon polygon)
		{
			// If the reshape path does not cut across the polygon but along its boundary:
			//       _______
			//       |     |
			// ______|_____|_____
			//
			// The disconnected paths look like this:
			// ______       _____
			//
			// Add 'half' the polygon to create a continuous reshape path that can reshape also 'through' the polygon
			//       _______
			//       |     |
			// ______|     |_____
			//

			var touchPoints = new List<IPoint>();
			foreach (IPath disconnectedPath in GeometryUtils.GetPaths(disconnectedPaths))
			{
				if (GeometryUtils.Intersects(polygon, disconnectedPath.FromPoint))
				{
					touchPoints.Add(disconnectedPath.FromPoint);
				}

				if (GeometryUtils.Intersects(polygon, disconnectedPath.ToPoint))
				{
					touchPoints.Add(disconnectedPath.ToPoint);
				}
			}

			object missing = Type.Missing;

			foreach (IRing ring in GeometryUtils.GetRings(polygon))
			{
				List<IPoint> ringTouchingPoints = GetTouchingPoints(touchPoints, ring);

				if (ringTouchingPoints.Count == 2)
				{
					IPath missingPiece = SegmentReplacementUtils.GetSegmentsBetween(
						ringTouchingPoints[0], ringTouchingPoints[1], ring);

					((IGeometryCollection) disconnectedPaths).AddGeometry(
						missingPiece, missing,
						missing);
				}
			}

			// Re-simplify to merge the added pieces with the original paths
			GeometryUtils.Simplify(disconnectedPaths);

			Assert.AreEqual(1, GeometryUtils.GetPaths(disconnectedPaths).Count(),
			                "Unexpected number of parts in sketch after connections through polygon(s) added");

			return GeometryUtils.GetPaths(disconnectedPaths).Single();
		}

		private static List<IPoint> GetTouchingPoints(IEnumerable<IPoint> points,
		                                              IRing ring)
		{
			var result = new List<IPoint>(2);

			var highLevelRing = (IPolygon) GeometryUtils.GetHighLevelGeometry(ring, true);

			foreach (IPoint point in points)
			{
				if (GeometryUtils.Touches(highLevelRing, point))
				{
					result.Add(point);
				}
			}

			return result;
		}

		/// <summary>
		/// Reshapes the provided geometries with the provided reshape curves.
		/// </summary>
		/// <param name="reshapeCurves"></param>
		/// <param name="tryReshapeRingNonDefaultSide"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		[NotNull]
		private Dictionary<IGeometry, NotificationCollection>
			ReshapeGeometriesIndividually(
				[NotNull] IEnumerable<CutSubcurve> reshapeCurves,
				bool tryReshapeRingNonDefaultSide,
				[CanBeNull] NotificationCollection notifications)
		{
			ICollection<CutSubcurve> filteredCurves = CollectionUtils.GetCollection(
				reshapeCurves.Where(
					subcurve =>
						subcurve.CanReshape || subcurve.IsReshapeMemberCandidate));

			var reshapedGeometries = new Dictionary<IGeometry, NotificationCollection>();

			foreach (KeyValuePair<IFeature, IGeometry> source in
				ReshapeGeometryCloneByFeature)
			{
				IFeature featureToReshape = source.Key;
				IGeometry geometryToReshape = source.Value;

				var singleFeatureNotification = new NotificationCollection();

				// The individual curves should all have a source reference.
				Func<CutSubcurve, bool> isApplicable =
					curve => curve.Source.HasValue &&
					         curve.Source.Value.References(featureToReshape);

				List<CutSubcurve> applicableCurves =
					filteredCurves.Where(isApplicable).ToList();

				ICollection<ReshapeInfo> reshapeInfos;

				IGeometry reshapedGeometry = ReshapeWithSimplifiedCurves(
					geometryToReshape, applicableCurves, tryReshapeRingNonDefaultSide,
					singleFeatureNotification, out reshapeInfos);

				if (reshapedGeometry != null && TargetFeatures != null)
				{
					AddPotentialTargetInsertPoints(applicableCurves);
				}

				if (reshapedGeometry != null)
				{
					reshapedGeometries.Add(reshapedGeometry, singleFeatureNotification);

					AddToRefreshArea(reshapeInfos);
				}
				else
				{
					NotificationUtils.Add(notifications,
					                      singleFeatureNotification.Concatenate(" "));
				}
			}

			return reshapedGeometries;
		}

		private static void EnsureZInNonReshapedNeighbors(
			[NotNull] IPath reshapePath,
			[NotNull] IEnumerable<IGeometry> allGeometries,
			[NotNull] IDictionary<IGeometry, NotificationCollection> reshapedGeometries)
		{
			IList<IGeometry> nonReshapables = new List<IGeometry>();

			foreach (IGeometry geometry in allGeometries)
			{
				if (! reshapedGeometries.ContainsKey(geometry))
				{
					nonReshapables.Add(geometry);
				}
			}

			ReshapeUtils.EnsureZInNonReshapedNeighbors(reshapePath, nonReshapables,
			                                           reshapedGeometries);
		}

		#region Reshape geometries as union

		[NotNull]
		private IEnumerable<KeyValuePair<IGeometry, NotificationCollection>>
			ReshapeGeometriesAsUnion(
				[NotNull] IEnumerable<CutSubcurve> reshapeCurves,
				bool tryReshapeRingNonDefaultSide,
				[CanBeNull] NotificationCollection notifications)
		{
			List<IGeometry> geometriesToReshape = GeometriesToReshape;

			Assert.ArgumentCondition(geometriesToReshape.Count > 0,
			                         "No features to reshape.");

			InitializeOriginalUnion(GeometriesToReshape);

			IGeometryCollection reshapePathCollection =
				ReshapeUtils.GetSimplifiedReshapeCurves(reshapeCurves,
				                                        _originalUnion.SpatialReference,
				                                        UseMinimumTolerance);

			var allReshapedGeometries =
				new Dictionary<IGeometry, NotificationCollection>();

			// The union to reshape needs to reflect intermediate reshapes
			// otherwise incorrect source replacement curves will be calculated

			foreach (IGeometry geometry in GeometryUtils.GetParts(reshapePathCollection))
			{
				var path = (IPath) geometry;

				_msg.Debug("Processing combined geometries...");

				IEnumerable<KeyValuePair<IGeometry, NotificationCollection>>
					reshapedGeometries =
						ReshapeGeometriesAsUnion(
							geometriesToReshape, path, tryReshapeRingNonDefaultSide,
							notifications);

				foreach (
					KeyValuePair<IGeometry, NotificationCollection> reshapedGeometry in
					reshapedGeometries)
				{
					if (! allReshapedGeometries.ContainsKey(reshapedGeometry.Key))
					{
						allReshapedGeometries.Add(reshapedGeometry.Key,
						                          reshapedGeometry.Value);
					}
				}

				// TODO: Marshal.Release path? or reshapePathCollection? -> path might be referenced by new geometry?
			}

			return allReshapedGeometries;
		}

		/// <summary>
		/// Reshapes several polygons or several polylines along the specified reshape path.
		/// For best results the source geometries should be planar, i.e. have no interior 
		/// intersections. For polygons this implementation does not regard StickyIntersections.
		/// The shared boundary is simply prolonged until it hits the target reshapePath.
		/// </summary>
		/// <param name="geometriesToReshape"></param>
		/// <param name="reshapePath"></param>
		/// <param name="tryReshapeRingNonDefaultSide"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		[NotNull]
		private IDictionary<IGeometry, NotificationCollection>
			ReshapeGeometriesAsUnion(
				[NotNull] IList<IGeometry> geometriesToReshape,
				[NotNull] IPath reshapePath,
				bool tryReshapeRingNonDefaultSide,
				[CanBeNull] NotificationCollection notifications)
		{
			// TODO: out/ref parameter with non-reshapable geometries/notifications dictionary
			Assert.ArgumentCondition(geometriesToReshape.Count > 0,
			                         "No geometries to reshape provided.");

			IDictionary<IGeometry, NotificationCollection> reshapedGeometries =
				new Dictionary<IGeometry, NotificationCollection>();

			// General idea:
			// - If the geometries to reshape don't touch each other they are processed individually
			// - Otherwise the unioned geometries are reshaped to determine what exactly should be done:
			//   Use the difference of the source between before and after reshape to
			//   identify the path to be replaced on the source. This gives us start
			//	 and end of the connection line for the AdjustCutSubcurve.

			// TODO: (performance) remove the parts that are not changed by the reshape - or do this for each
			//		 part of the unioned polyline which was reshaped

			Assert.True(GeometryUtils.HaveUniqueGeometryType(geometriesToReshape),
			            "All geometries to reshape must have the same geometry type.");
			Assert.True(geometriesToReshape.Count > 1,
			            "Single source reshape cannot use MultipleGeometriesReshaper");

			// Reshape the union first, before changing the geometries to reshape!
			ReshapeInfo unionReshapeInfo;
			bool unionReshaped = TryReshapeUnion(geometriesToReshape, reshapePath,
			                                     tryReshapeRingNonDefaultSide,
			                                     out unionReshapeInfo);

			var singleGeometryReshapesInUnion =
				new List<PolycurveInUnionReshapeInfo>(geometriesToReshape.Count);

			if (unionReshaped && unionReshapeInfo.PartIndexToReshape != null)
			{
				var useFallbacks = false;

				var nonUnionReshapedGeometries = new List<IGeometry>();

				foreach (IGeometry geometryToReshape in geometriesToReshape)
				{
					IList<PolycurveInUnionReshapeInfo> singleReshapes =
						InitializeSingleGeometryReshapesInUnion(
							geometryToReshape, unionReshapeInfo, _originalUnion);

					if (singleReshapes.Count > 0)
					{
						singleGeometryReshapesInUnion.AddRange(singleReshapes);
					}
					else
					{
						// add to target features, could be the third already reshaped poly with potentially missing points
						nonUnionReshapedGeometries.Add(geometryToReshape);
					}
				}

				CalculateSingleReshapesInUnion(singleGeometryReshapesInUnion,
				                               unionReshapeInfo,
				                               notifications, ref useFallbacks);

				foreach (PolycurveInUnionReshapeInfo singleGeometryReshapeInUnion in
					singleGeometryReshapesInUnion)
				{
					ReshapeSingleGeometryInUnion(singleGeometryReshapeInUnion,
					                             useFallbacks,
					                             unionReshapeInfo, reshapedGeometries,
					                             notifications);
				}

				EnsureTargetIntersectionPoints(nonUnionReshapedGeometries,
				                               singleGeometryReshapesInUnion,
				                               useFallbacks);
			}

			return reshapedGeometries;
		}

		private bool TryReshapeUnion(IList<IGeometry> geometriesToReshape,
		                             IPath reshapePath,
		                             bool tryReshapeRingNonDefaultSide,
		                             out ReshapeInfo unionReshapeInfo)
		{
			InitializeOriginalUnion(geometriesToReshape);

			if (_unionToReshape == null)
			{
				_unionToReshape = GeometryFactory.Clone(_originalUnion);
			}

			var unionNotifications = new NotificationCollection();

			unionReshapeInfo =
				new ReshapeInfo(_unionToReshape, reshapePath, unionNotifications)
				{
					ReshapeResultFilter = GetResultFilter(tryReshapeRingNonDefaultSide)
				};

			bool unionReshaped =
				ReshapeUtils.ReshapeGeometry(unionReshapeInfo, reshapePath);

			_msg.DebugFormat("Union reshape notifications: {0}",
			                 unionNotifications.Concatenate(" "));

			return unionReshaped;
		}

		private void ReshapeSingleGeometryInUnion(
			[NotNull] PolycurveInUnionReshapeInfo singleGeometryReshapeInUnion,
			bool useFallbacks,
			[NotNull] ReshapeInfo unionReshapeInfo,
			[NotNull] IDictionary<IGeometry, NotificationCollection> reshapedGeometries,
			[CanBeNull] NotificationCollection notifications)
		{
			ReshapeInfo reshapeInfo = useFallbacks
				                          ? singleGeometryReshapeInUnion
					                          .FallbackReshapeInfo
				                          : singleGeometryReshapeInUnion.ReshapeInfo;

			bool reshaped;
			if (reshapeInfo.GeometryToReshape.GeometryType ==
			    esriGeometryType.esriGeometryPolygon)
			{
				reshaped = ReshapeSinglePolygonInUnion(reshapeInfo);
			}
			else
			{
				// Currently the fall-back should always be possible
				Assert.NotNull(reshapeInfo,
				               "Neither closest-target-point nor proportionate-distribution strategy resulted in valid reshape");

				reshaped =
					ReplacePolylineSegments(reshapeInfo, unionReshapeInfo, useFallbacks);
			}

			if (reshaped)
			{
				reshaped = IsReshapeAllowed(reshapeInfo, notifications);
			}

			// TODO: add notifications if already contained in reshapedGeometries
			if (reshaped &&
			    ! reshapedGeometries.ContainsKey(reshapeInfo.GeometryToReshape))
			{
				reshapedGeometries.Add(reshapeInfo.GeometryToReshape,
				                       reshapeInfo.Notifications);
			}
			else
			{
				if (reshapeInfo.Notifications != null)
				{
					NotificationUtils.Add(notifications,
					                      reshapeInfo.Notifications.Concatenate(" "));
				}
			}

			if (reshaped && reshapeInfo.CutReshapePath != null)
			{
				AddPotentialTargetInsertPoints(reshapeInfo.CutReshapePath);

				AddToRefreshArea(reshapeInfo);
			}
		}

		private static bool ReshapeSinglePolygonInUnion(ReshapeInfo reshapeInfo)
		{
			bool reshaped;
			if (reshapeInfo.CutReshapePath != null)
			{
				if (reshapeInfo.CutReshapePath.Path.Length > 0)
				{
					// reshape the known polygon part with the known cut reshape path
					reshaped = ReshapeUtils.ReshapePolygonOrMultipatch(reshapeInfo);
				}
				else
				{
					_msg.DebugFormat(
						"No need to reshape geometry with 0-length reshape path at {0} | {1}",
						reshapeInfo.CutReshapePath.Path.FromPoint.X,
						reshapeInfo.CutReshapePath.Path.FromPoint.Y);
					reshaped = false;
				}
			}
			else
			{
				Assert.NotNull(reshapeInfo.ReshapePath,
				               "Reshape path and cut reshape path are undefined");

				// try reshape if possible (typically for union-reshapes to the inside)
				var requiredPartIndexToReshape =
					(int) Assert.NotNull(reshapeInfo.PartIndexToReshape);

				IList<int> currentlyReshapableParts;
				reshapeInfo.IdentifyUniquePartIndexToReshape(
					out currentlyReshapableParts);

				if (currentlyReshapableParts.Contains(requiredPartIndexToReshape))
				{
					// reset to make sure no other parts are reshaped here:
					reshapeInfo.PartIndexToReshape = requiredPartIndexToReshape;
					reshaped = ReshapeUtils.ReshapeGeometryPart(
						reshapeInfo.GeometryToReshape,
						reshapeInfo);
				}
				else
				{
					reshaped = false;
				}
			}

			return reshaped;
		}

		private static bool ReplacePolylineSegments(ReshapeInfo reshapeInfo,
		                                            ReshapeInfo unionReshapeInfo,
		                                            bool useFallbacks)
		{
			var adjustCurve =
				(AdjustedCutSubcurve) Assert.NotNull(reshapeInfo.CutReshapePath);

			var pathToReshape = (IPath) reshapeInfo.GetGeometryPartToReshape();

			bool unionReshapeLineFromToPointTouches =
				IntersectsUnionReshapeLineFromTo(pathToReshape, unionReshapeInfo);

			// replace the full path if it's an intermediate path (should be made more explicit)
			if (useFallbacks && ! unionReshapeLineFromToPointTouches)
			{
				SegmentReplacementUtils.ReplaceSegments(
					pathToReshape,
					(ISegmentCollection) adjustCurve.PathOnTarget, 0,
					((ISegmentCollection) pathToReshape).SegmentCount - 1);
			}
			else
			{
				SegmentReplacementUtils.ReplaceSegments(
					pathToReshape,
					adjustCurve.PathOnTarget,
					reshapeInfo.ReplacedSegments.FromPoint,
					reshapeInfo.ReplacedSegments.ToPoint);
			}

			// Needed for length and envelope to be correct:
			((ISegmentCollection) reshapeInfo.GeometryToReshape).SegmentsChanged();

			return true;
		}

		private void InitializeOriginalUnion(IList<IGeometry> geometriesToReshape)
		{
			if (_originalUnion == null)
			{
				_originalUnion = GeometryUtils.UnionGeometries(geometriesToReshape);

				GeometryUtils.Simplify(_originalUnion, true, false);
			}
		}

		private static void AddCombinedReshapeRange(
			[NotNull] IEnumerable<KeyValuePair<IGeometry, NotificationCollection>>
				combinedReshapes,
			[NotNull] IDictionary<IGeometry, NotificationCollection> toDictionary)
		{
			foreach (
				KeyValuePair<IGeometry, NotificationCollection> combinedReshape in
				combinedReshapes)
			{
				if (! toDictionary.ContainsKey(combinedReshape.Key))
				{
					toDictionary.Add(combinedReshape.Key, combinedReshape.Value);
				}
			}
		}

		private IList<PolycurveInUnionReshapeInfo>
			InitializeSingleGeometryReshapesInUnion(
				[NotNull] IGeometry geometryToReshape,
				[NotNull] ReshapeInfo unionReshapeInfo,
				[NotNull] IGeometry originalUnion)
		{
			var result = new List<PolycurveInUnionReshapeInfo>();

			var geometryCollectionToReshape = (IGeometryCollection) geometryToReshape;

			for (var partIdxToReshape = 0;
			     partIdxToReshape < geometryCollectionToReshape.GeometryCount;
			     partIdxToReshape++)
			{
				// NOTE: no disjoint check here with the union part because it might be 
				//		 an island built from a gap between single geometries.

				var pathToReshape =
					(IPath) geometryCollectionToReshape.get_Geometry(partIdxToReshape);

				// Identify the source parts on the geometryToReshape that can/should be replaced:
				IEnumerable<IPath> sourceReplacementPaths =
					GetSourceReplacementPaths(pathToReshape, originalUnion,
					                          unionReshapeInfo);

				foreach (IPath sourceReplacementPath in sourceReplacementPaths)
				{
					result.Add(new PolycurveInUnionReshapeInfo(
						           (IPolycurve) geometryToReshape,
						           partIdxToReshape,
						           sourceReplacementPath));
				}
			}

			return result;
		}

		private void CalculateSingleReshapesInUnion(
			[NotNull] List<PolycurveInUnionReshapeInfo> singleReshapesInUnion,
			[NotNull] ReshapeInfo unionReshapeInfo,
			[CanBeNull] NotificationCollection notifications,
			ref bool useFallbackCutSubcurves)
		{
			List<KeyValuePair<IPoint, IPoint>> sourceTargetPairs = null;

			if (StickyIntersectionPoints != null &&
			    StickyIntersectionPoints.HasTargetPoints())
			{
				sourceTargetPairs =
					AssignReshapeUnionIntersectionTargets(singleReshapesInUnion,
					                                      StickyIntersectionPoints
						                                      .GetTargetPointCollection(),
					                                      unionReshapeInfo,
					                                      notifications);
			}

			foreach (
				PolycurveInUnionReshapeInfo polycurveInUnionReshapeInfo in
				singleReshapesInUnion)
			{
				CalculateReplacementForSourceSubCurve(polycurveInUnionReshapeInfo,
				                                      unionReshapeInfo,
				                                      sourceTargetPairs);

				if (polycurveInUnionReshapeInfo.RequiresUsingFallback)
				{
					useFallbackCutSubcurves = true;
				}
			}
		}

		#endregion

		#region Calculate source replacement paths

		/// <summary>
		/// Returns the paths in the geometry part to reshape that should be replaced by the reshape path
		/// in case of several touching polygons / polylines are being reshaped.
		/// </summary>
		/// <param name="geometryPartToReshape"></param>
		/// <param name="originalUnion"></param>
		/// <param name="unionReshapeInfo"></param>
		/// <returns></returns>
		[NotNull]
		private IEnumerable<IPath> GetSourceReplacementPaths(
			[NotNull] ICurve geometryPartToReshape,
			[NotNull] IGeometry originalUnion,
			[NotNull] ReshapeInfo unionReshapeInfo)
		{
			Assert.ArgumentNotNull(geometryPartToReshape, nameof(geometryPartToReshape));
			Assert.ArgumentNotNull(unionReshapeInfo, nameof(unionReshapeInfo));
			Assert.NotNull(unionReshapeInfo.GeometryToReshape,
			               "UnionedReshapedGeometry not initialized");

			var sourceReplacementPaths = new List<IPath>();

			if (unionReshapeInfo.GeometryToReshape.GeometryType ==
			    esriGeometryType.esriGeometryPolyline)
			{
				IPath sourceReplacementPath =
					GetPolylineSourceReplacement((IPath) geometryPartToReshape,
					                             unionReshapeInfo);

				if (sourceReplacementPath != null && ! sourceReplacementPath.IsEmpty)
				{
					sourceReplacementPaths.Add(sourceReplacementPath);
				}
			}
			else if (unionReshapeInfo.GeometryToReshape.GeometryType ==
			         esriGeometryType.esriGeometryPolygon)
			{
				// protect those lines in the before-reshape state that still exist in the
				// after-reshape state:
				// 1. Intersect polygon with reshaped unioned poly
				// 2. Additionally protect the boundaries where two polygons touch (which were dissoved by union)

				IGeometry highLevelCurveToReshape =
					GeometryUtils.GetHighLevelGeometry(geometryPartToReshape);

				IPolyline unionBoundary = GeometryFactory.CreatePolyline(originalUnion);
				IPolyline reshapedUnionBoundary =
					GeometryFactory.CreatePolyline(unionReshapeInfo.GeometryToReshape);

				IPolyline unionReshapeLines = CalculateUnionReshapeLines(unionBoundary,
					reshapedUnionBoundary);

				var sourceReplacementLines =
					(IGeometryCollection)
					GetPolygonBoundaryIntersections(highLevelCurveToReshape,
					                                unionReshapeLines);

				if (sourceReplacementLines != null)
				{
					for (var i = 0; i < sourceReplacementLines.GeometryCount; i++)
					{
						sourceReplacementPaths.Add(
							(IPath) sourceReplacementLines.get_Geometry(i));
					}
				}

				Marshal.ReleaseComObject(highLevelCurveToReshape);
				Marshal.ReleaseComObject(unionBoundary);
				Marshal.ReleaseComObject(reshapedUnionBoundary);
			}

			return sourceReplacementPaths;
		}

		private IPolyline CalculateUnionReshapeLines(IPolyline unionBoundary,
		                                             IPolyline reshapedUnionBoundary)
		{
			// Z-only difference should be calculated with normal tolerance  because for non-linear geometries intersect is not always 
			// symmetric when the tolerance is small
			double zTolerance = UseMinimumTolerance
				                    ? 0
				                    : GeometryUtils.GetXyTolerance(unionBoundary);

			IPolyline zOnlyDifference = ReshapeUtils.GetZOnlyDifference(
				unionBoundary, reshapedUnionBoundary, zTolerance);

			IPolyline unionReshapeLines = null;
			if (UseMinimumTolerance)
			{
				ReshapeUtils.ExecuteWithMinimumTolerance(
					delegate
					{
						unionReshapeLines = GetDifferencePolyline3D(
							unionBoundary, reshapedUnionBoundary, zOnlyDifference);
					}, unionBoundary, reshapedUnionBoundary);
			}
			else
			{
				unionReshapeLines = GetDifferencePolyline3D(
					unionBoundary, reshapedUnionBoundary, zOnlyDifference);
			}

			return unionReshapeLines;
		}

		[NotNull]
		private static IPolyline GetDifferencePolyline3D(
			[NotNull] IPolyline onPolyline,
			[NotNull] IPolyline differentFrom,
			[CanBeNull] IPolyline zOnlyDifference)
		{
			// Copied from ReshapableSubcurveCalculator

			IPolyline difference =
				ReshapeUtils.GetDifferencePolyline(onPolyline, differentFrom);

			// merge result with Z-difference
			if (zOnlyDifference != null && ! zOnlyDifference.IsEmpty)
			{
				difference = (IPolyline) GeometryUtils.Union(difference, zOnlyDifference);
			}

			// adjacent lines parts get ordered and create one single reshapable part
			GeometryUtils.Simplify(difference, true, true);

			return difference;
		}

		/// <summary>
		/// Calculates the full source replacement of a path in a polyline-union reshape,
		/// i.e. including those parts where the reshape line was snapped along the source.
		/// </summary>
		/// <param name="pathToReshape"></param>
		/// <param name="unionReshapeInfo"></param>
		/// <returns></returns>
		private static IPath GetPolylineSourceReplacement(IPath pathToReshape,
		                                                  ReshapeInfo unionReshapeInfo)
		{
			// get the intersection point with the union's reshape path
			// find out if the replaced segments are before or after the touch point
			// -> return as full source replacement
			IGeometry highLevelPathToReshape =
				GeometryUtils.GetHighLevelGeometry(pathToReshape, true);

			IPath reshapePath = Assert.NotNull(unionReshapeInfo.CutReshapePath).Path;
			IPoint unionReshapePathFrom = reshapePath.FromPoint;
			IPoint unionReshapePathTo = reshapePath.ToPoint;

			IPath result;

			if (GeometryUtils.Intersects(unionReshapePathFrom, highLevelPathToReshape))
			{
				result = GetFullReplacedSourcePath(pathToReshape, unionReshapeInfo,
				                                   unionReshapePathFrom);
			}
			else if (GeometryUtils.Intersects(unionReshapePathTo, highLevelPathToReshape))
			{
				result = GetFullReplacedSourcePath(pathToReshape, unionReshapeInfo,
				                                   unionReshapePathTo);
			}
			else
			{
				// no intersection with union reshape path
				// -> if not changed at all: polyline at the beginning / end of the string of lines
				// -> otherwise: it's an intermediate polyline
				IList<IPath> differences = GetDifferencePaths(
					pathToReshape, (IPolyline) unionReshapeInfo.GeometryToReshape);

				result = differences.Count == 0
					         ? null
					         : pathToReshape;
			}

			return result;
		}

		private static IPath GetFullReplacedSourcePath(IPath pathToReshape,
		                                               ReshapeInfo unionReshapeInfo,
		                                               IPoint unionReshapePathEnd)
		{
			ICurve result;
			// which is the end point that was not changed by the union reshape?
			bool fromPointUnreshaped =
				IsFromPointUnreshaped(pathToReshape, unionReshapeInfo);

			double distanceAlong = GeometryUtils.GetDistanceAlongCurve(pathToReshape,
				unionReshapePathEnd,
				true);

			// take the other end as start point for the source replacement
			double start, end;
			if (fromPointUnreshaped)
			{
				start = distanceAlong;
				end = 1.0;
			}
			else
			{
				start = 0.0;
				end = distanceAlong;
			}

			pathToReshape.GetSubcurve(start, end, true, out result);

			return (IPath) result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="outerPathToReshape">Path that is intersected by either the union reshape path's from- or to-point</param>
		/// <param name="unionReshapeInfo"></param>
		/// <returns></returns>
		private static bool IsFromPointUnreshaped(IPath outerPathToReshape,
		                                          ReshapeInfo unionReshapeInfo)
		{
			IPoint fromPoint = outerPathToReshape.FromPoint;
			IPoint toPoint = outerPathToReshape.ToPoint;

			var reshapedUnion = (IPolyline) unionReshapeInfo.GeometryToReshape;

			// 1. check if the union's end point coincides with the outerPathToReshape's end point
			bool fromPointUnreshaped =
				GeometryUtils.AreEqualInXY(fromPoint, reshapedUnion.FromPoint) ||
				GeometryUtils.AreEqualInXY(fromPoint, reshapedUnion.ToPoint);

			bool toPointUnreshaped =
				GeometryUtils.AreEqualInXY(toPoint, reshapedUnion.FromPoint) ||
				GeometryUtils.AreEqualInXY(toPoint, reshapedUnion.ToPoint);

			if (fromPointUnreshaped ^ toPointUnreshaped)
			{
				// either the from- (exclusive-)or the to-point was not reshaped:
				return fromPointUnreshaped;
			}

			// 2. Check if one of the points does not exist in the reshaped union
			fromPointUnreshaped = GeometryUtils.Intersects(fromPoint, reshapedUnion);
			toPointUnreshaped = GeometryUtils.Intersects(toPoint, reshapedUnion);

			if (fromPointUnreshaped ^ toPointUnreshaped)
			{
				// either the from- (exclusive-)or the to-point was not reshaped:
				return fromPointUnreshaped;
			}

			// Either both points intersect the reshaped union: this means one is an 
			//    intermediate point in the reshape path
			if (fromPointUnreshaped && toPointUnreshaped)
			{
				IGeometry highLevelUnionReshapePath =
					GeometryUtils.GetHighLevelGeometry(
						unionReshapeInfo.ReshapePath, true);

				return GeometryUtils.InteriorIntersects(
					toPoint, highLevelUnionReshapePath);
			}

			Assert.CantReach(
				"Neither of the path to reshape's end points intersect the reshaped union");

			return fromPointUnreshaped;
		}

		private static bool IntersectsUnionReshapeLineFromTo(IPath pathToReshape,
		                                                     ReshapeInfo unionReshapeInfo)
		{
			IGeometry highLevelPathToReshape =
				GeometryUtils.GetHighLevelGeometry(pathToReshape, true);

			IPath unionReshapePath = Assert.NotNull(unionReshapeInfo.CutReshapePath).Path;

			bool unionReshapeLineFromToPointTouches =
				GeometryUtils.Intersects(unionReshapePath.FromPoint,
				                         highLevelPathToReshape) ||
				GeometryUtils.Intersects(unionReshapePath.ToPoint,
				                         highLevelPathToReshape);

			Marshal.ReleaseComObject(highLevelPathToReshape);

			return unionReshapeLineFromToPointTouches;
		}

		[NotNull]
		private static List<IPath> GetDifferencePaths([NotNull] IGeometry onCurve,
		                                              [NotNull] IPolyline differentFrom)
		{
			var sourceReplacementPaths = new List<IPath>();

			IPolyline onPolyline =
				onCurve.GeometryType == esriGeometryType.esriGeometryPolyline
					? (IPolyline) onCurve
					: GeometryFactory.CreatePolyline(onCurve);

			var unreshapedLines =
				(IGeometryCollection)
				ReshapeUtils.GetDifferencePolyline(onPolyline, differentFrom);

			for (var i = 0; i < unreshapedLines.GeometryCount; i++)
			{
				sourceReplacementPaths.Add((IPath) unreshapedLines.get_Geometry(i));
			}

			return sourceReplacementPaths;
		}

		[CanBeNull]
		private static IPolyline GetPolygonBoundaryIntersections(
			[NotNull] IGeometry polygon,
			[NotNull] IPolyline polyline)
		{
			GeometryUtils.AllowIndexing(polygon);
			GeometryUtils.AllowIndexing(polyline);

			if (GeometryUtils.Disjoint(polygon, polyline))
			{
				return null;
			}

			// adjust along the intersection between the boundary and the polylinePart
			// do not adjust the parts that were removed by the simplify in the unioned geometry
			IPolyline polygonBoundary = GeometryFactory.CreatePolyline(polygon);

			const bool assumeIntersecting = true;
			const bool allowRandomStartPointsForClosedIntersections = true;
			IPolyline intersectLines = IntersectionUtils.GetIntersectionLines(
				polygonBoundary, polyline, assumeIntersecting,
				allowRandomStartPointsForClosedIntersections);

			Marshal.ReleaseComObject(polygonBoundary);

			GeometryUtils.Simplify(intersectLines);
			return intersectLines;
		}

		#endregion

		#region Calculate reshape curves to replace source replacement paths

		/// <summary>
		/// Calculates the part of the target (reshape line) that should be used
		/// to replace the sourceReplacementPath. The result is returned as an
		/// AdjustedCutSubcurve (including the connection lines which are not
		/// used for the actual reshape in the polyline reshape case).
		/// </summary>
		/// <param name="singleReshapeInUnion"></param>
		/// <param name="unionReshapeInfo"></param>
		/// <param name="sourceTargetPointPairs"></param>
		/// <returns></returns>
		private void CalculateReplacementForSourceSubCurve(
			[NotNull] PolycurveInUnionReshapeInfo singleReshapeInUnion,
			[NotNull] ReshapeInfo unionReshapeInfo,
			[CanBeNull] List<KeyValuePair<IPoint, IPoint>> sourceTargetPointPairs)
		{
			IPolycurve geometryToReshape = singleReshapeInUnion.GeometryToReshape;
			IPath sourceReplacementPath = singleReshapeInUnion.SourceReplacementPath;

			IGeometry geometryPartToReshape =
				((IGeometryCollection) geometryToReshape).get_Geometry(
					singleReshapeInUnion.GeometryPartToReshape);

			var connectLineCalculator = new MultiReshapeConnectLineCalculator(
				_originalUnion, geometryPartToReshape,
				unionReshapeInfo,
				MaxProlongationLengthFactor);

			connectLineCalculator.TargetConnectPointFrom =
				FindTargetConnectPoint(sourceReplacementPath.FromPoint,
				                       sourceTargetPointPairs);
			connectLineCalculator.TargetConnectPointTo =
				FindTargetConnectPoint(sourceReplacementPath.ToPoint,
				                       sourceTargetPointPairs);

			Stopwatch watch = _msg.DebugStartTiming();

			AdjustedCutSubcurve adjustCurve = null;
			AdjustedCutSubcurve fallback = null;

			// often in preview, the full ring is replaced, and the cut reshape path is null

			IPath reshapePathForUnion = unionReshapeInfo.CutReshapePath == null
				                            ? unionReshapeInfo.ReshapePath
				                            : unionReshapeInfo.CutReshapePath.Path;

			IPath startFallback;
			IPath endFallback;

			IPath startSourceConnection = connectLineCalculator.FindConnection(
				sourceReplacementPath, reshapePathForUnion, true,
				out startFallback);

			IPath endSourceConnection = connectLineCalculator.FindConnection(
				sourceReplacementPath, reshapePathForUnion, false,
				out endFallback);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("CalculatedAdjustedPath start connection: {0}",
				                 GeometryUtils.ToString(startSourceConnection));

				_msg.DebugFormat("CalculateAdjustedPath: end connection: {0}",
				                 GeometryUtils.ToString(endSourceConnection));
			}

			if (startSourceConnection != null && endSourceConnection != null)
			{
				adjustCurve = AdjustUtils.CreateAdjustedCutSubcurve(reshapePathForUnion,
					startSourceConnection,
					endSourceConnection);
			}

			if (geometryToReshape.GeometryType == esriGeometryType.esriGeometryPolyline)
			{
				if (! IsPolylineReshapeAdjustCurveValid(
					    adjustCurve, unionReshapeInfo,
					    connectLineCalculator.FallbackNotifications,
					    sourceReplacementPath))
				{
					adjustCurve = null;
				}

				fallback =
					GetProportionalDistributionAdjustLine(unionReshapeInfo,
					                                      reshapePathForUnion,
					                                      sourceReplacementPath);
			}
			else if (startFallback != null && endFallback != null)
			{
				fallback = AdjustUtils.CreateAdjustedCutSubcurve(reshapePathForUnion,
				                                                 startFallback,
				                                                 endFallback);
			}

			AddReshapeInfos(singleReshapeInUnion, adjustCurve, fallback,
			                reshapePathForUnion, connectLineCalculator, unionReshapeInfo);

			singleReshapeInUnion.RequiresUsingFallback = adjustCurve == null;

			_msg.DebugStopTiming(watch,
			                     "Calculated adjusted subcurve including connection lines to target");
		}

		private bool IsPolylineReshapeAdjustCurveValid(
			AdjustedCutSubcurve adjustCutSubcurve,
			ReshapeInfo unionReshapeInfo,
			NotificationCollection notifications,
			IPath sourceReplacementPath)
		{
			if (adjustCutSubcurve == null)
			{
				return false;
			}

			if (GeometryUtils.AreEqualInXY(adjustCutSubcurve.Path.FromPoint,
			                               adjustCutSubcurve.Path.ToPoint))
			{
				// potentially ends up with empty result
				return false;
			}

			return ! HasOrientationChanged(
				       unionReshapeInfo, notifications, sourceReplacementPath,
				       adjustCutSubcurve);
		}

		private bool HasOrientationChanged(ReshapeInfo unionReshapeInfo,
		                                   NotificationCollection notifications,
		                                   IPath sourceReplacementPath,
		                                   AdjustedCutSubcurve adjustCutSubcurve)
		{
			var result = false;

			if (adjustCutSubcurve.PathOnTarget.Length <
			    GeometryUtils.GetXyResolution(sourceReplacementPath))
			{
				// cannot determine properly false
				return false;
			}

			bool sourceOrientedAlongUnion = IsOrientedAlong(sourceReplacementPath,
			                                                (ICurve) _originalUnion);

			bool targetOrientedAlongUnion = IsOrientedAlong(
				adjustCutSubcurve.PathOnTarget,
				(ICurve)
				unionReshapeInfo
					.GeometryToReshape);

			if (sourceOrientedAlongUnion != targetOrientedAlongUnion)
			{
				// if ANY inversion occurs, disallow all other reshapes! They could also overlap without inversion.
				NotificationUtils.Add(notifications,
				                      "The resulting polyline would be flipped (inverted orientation) and the resulting polylines would overlap. Cannot use 'closest-point-on-target' strategy");

				result = true;
			}

			return result;
		}

		private static bool IsOrientedAlong(ICurve curve1, ICurve alongCurve2)
		{
			double fromDistance = GeometryUtils.GetDistanceAlongCurve(alongCurve2,
				curve1.FromPoint,
				false);

			double toDistance = GeometryUtils.GetDistanceAlongCurve(alongCurve2,
				curve1.ToPoint,
				false);

			return toDistance > fromDistance;
		}

		private static AdjustedCutSubcurve GetProportionalDistributionAdjustLine(
			ReshapeInfo unionReshapeInfo, IPath reshapePathForUnion,
			IPath sourceReplacement)
		{
			// if the union reshape path to reshape does not share a start or end point with the current path to reshape 
			// (i.e. it is an intermediate reshape):
			// Replace entire path, otherwise there could be gaps (or overlaps)
			ICurve pathOnTarget;

			double startDistance =
				GeometryUtils.GetDistanceAlongCurve(unionReshapeInfo.ReplacedSegments,
				                                    sourceReplacement.FromPoint, true);
			double endDistance =
				GeometryUtils.GetDistanceAlongCurve(unionReshapeInfo.ReplacedSegments,
				                                    sourceReplacement.ToPoint, true);

			if (startDistance > endDistance)
			{
				startDistance = 1.0 - startDistance;
				endDistance = 1.0 - endDistance;
			}

			reshapePathForUnion.GetSubcurve(startDistance, endDistance, true,
			                                out pathOnTarget);

			var fallback = new AdjustedCutSubcurve((IPath) pathOnTarget, null, null);

			return fallback;
		}

		private void AddReshapeInfos(
			[NotNull] PolycurveInUnionReshapeInfo toPolycurveInUnionReshape,
			[CanBeNull] AdjustedCutSubcurve adjustCutSubcurve,
			AdjustedCutSubcurve fallbackCutSubcurve,
			[NotNull] IPath unionReshapePath,
			MultiReshapeConnectLineCalculator connectLineCalculator,
			ReshapeInfo unionReshapeInfo)
		{
			IGeometry geometryToReshape = toPolycurveInUnionReshape.GeometryToReshape;

			int partIdxToReshape = toPolycurveInUnionReshape.GeometryPartToReshape;
			IPath sourceReplacementPath = toPolycurveInUnionReshape.SourceReplacementPath;

			toPolycurveInUnionReshape.ReshapeInfo = CreatePartReshapeInfo(
				geometryToReshape, partIdxToReshape, sourceReplacementPath,
				adjustCutSubcurve, unionReshapePath,
				unionReshapeInfo.ReshapeResultFilter,
				connectLineCalculator.Notifications);

			toPolycurveInUnionReshape.FallbackReshapeInfo = CreatePartReshapeInfo(
				geometryToReshape, partIdxToReshape, sourceReplacementPath,
				fallbackCutSubcurve, unionReshapePath,
				unionReshapeInfo.ReshapeResultFilter,
				connectLineCalculator.FallbackNotifications);
		}

		[CanBeNull]
		private ReshapeInfo CreatePartReshapeInfo(
			[NotNull] IGeometry geometryToReshape,
			int partIdxToReshape,
			[NotNull] IPath sourceReplacementPath,
			[CanBeNull] AdjustedCutSubcurve adjustCutSubcurve,
			[NotNull] IPath unionReshapePath,
			ReshapeResultFilter resultFilter,
			NotificationCollection notifications)
		{
			IPath reshapePath = adjustCutSubcurve != null
				                    ? adjustCutSubcurve.Path
				                    : unionReshapePath;

			var reshapeInfo =
				new ReshapeInfo(geometryToReshape, reshapePath,
				                notifications)
				{
					PartIndexToReshape = partIdxToReshape,
					CutReshapePath = adjustCutSubcurve,
					ReshapeResultFilter = resultFilter,
					ReplacedSegments = sourceReplacementPath
				};

			return reshapeInfo;
		}

		#endregion

		#region Manage the user-defined target intersection points

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourcePoint"></param>
		/// <param name="sourceTargetPointPairs"></param>
		/// <returns></returns>
		[CanBeNull]
		private IPoint FindTargetConnectPoint(
			[NotNull] IPoint sourcePoint,
			[CanBeNull] IEnumerable<KeyValuePair<IPoint, IPoint>> sourceTargetPointPairs)
		{
			if (sourceTargetPointPairs == null)
			{
				return null;
			}

			foreach (
				KeyValuePair<IPoint, IPoint> sourceTargetPointPair in
				sourceTargetPointPairs)
			{
				if (GeometryUtils.AreEqualInXY(sourcePoint, sourceTargetPointPair.Key))
				{
					return sourceTargetPointPair.Value;
				}
			}

			return null;
		}

		///  <summary>
		///  Assigns the source points # that must be moved in a reshape to their respective target points * on the reshape line:
		///  		
		///  ---------------
		///  |    |   |    |
		///  |    |   |    |
		///  |    |   |    |
		///  -----#---#-----
		///   \           /
		///    \         /
		///     \       /
		///      *     *
		///       \   /
		///        \ /
		/// 
		///              \    /
		///  Reshape line:\  /
		///                \/
		/// 
		///  Source intersection points: #
		///  Target intersection points: *
		///  </summary>
		///  <param name="singleGeometryReshapesInUnion"></param>
		///  <param name="targetIntersectionPoints"></param>
		///  <param name="unionReshapeInfo"></param>
		/// <param name="notifications"></param>
		[CanBeNull]
		private static List<KeyValuePair<IPoint, IPoint>>
			AssignReshapeUnionIntersectionTargets(
				[NotNull] IEnumerable<PolycurveInUnionReshapeInfo> singleGeometryReshapesInUnion,
				[NotNull] IPointCollection targetIntersectionPoints,
				[NotNull] ReshapeInfo unionReshapeInfo,
				[CanBeNull] NotificationCollection notifications)
		{
			var highLevelUnionReshapeLine =
				(IPolycurve) GeometryUtils.GetHighLevelGeometry(
					Assert.NotNull(unionReshapeInfo.CutReshapePath).Path);

			IPointCollection sourcePointsToMove =
				GetSourceIntersectionPointsToMove(singleGeometryReshapesInUnion,
				                                  highLevelUnionReshapeLine);

			if (sourcePointsToMove.PointCount < targetIntersectionPoints.PointCount)
			{
				NotificationUtils.Add(notifications,
				                      "Too many target intersection points defined, using closest points");
			}
			else if (targetIntersectionPoints.PointCount < sourcePointsToMove.PointCount)
			{
				NotificationUtils.Add(notifications,
				                      "Too few target intersection points defined, using closest points");
			}

			// remove those target intersection points that are not on the used part of the sketch:
			var usableTargetIntersections =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					(IGeometry) targetIntersectionPoints, highLevelUnionReshapeLine,
					true);

			if (usableTargetIntersections.PointCount <
			    targetIntersectionPoints.PointCount)
			{
				NotificationUtils.Add(notifications,
				                      "One or more target intersection points cannot be used because it is outside the relevant sketch part. Please ensure they are placed between the intersection points of the sketch with the unioned selection");
			}

			if (sourcePointsToMove.PointCount == 0 ||
			    usableTargetIntersections.PointCount == 0)
			{
				return null;
			}

			IDictionary<IPoint, IPoint> result;
			if (sourcePointsToMove.PointCount != usableTargetIntersections.PointCount)
			{
				result = ReshapeUtils.PairByDistance(sourcePointsToMove,
				                                     usableTargetIntersections);
			}
			else
			{
				result = PairByOrderingAlongLines(sourcePointsToMove,
				                                  usableTargetIntersections,
				                                  unionReshapeInfo,
				                                  highLevelUnionReshapeLine);
			}

			return result.ToList();
		}

		/// <summary>
		/// Ensures that the target intersection points are also added to the provided geometries (those that were
		/// reshaped reshaped individually):
		/// ---------------
		/// |        |    |
		/// |        |    |
		/// |        |    |
		/// ---------#-----
		///  \           /
		///   \         /
		///    \       /
		///     \     /
		///      \   *
		///       \ /
		///
		///             \    /
		/// Reshape line:\  /
		///               \/
		///
		/// Source intersection points: #
		/// Target intersection point created by shared source boundary prolongation (instead of user-defined): *
		/// 
		/// </summary>
		/// <param name="geometries"></param>
		/// <param name="singleGeometryReshapesInUnion"></param>
		/// <param name="useFallbacks"></param>
		private static void EnsureTargetIntersectionPoints(
			IEnumerable<IGeometry> geometries,
			IEnumerable<PolycurveInUnionReshapeInfo> singleGeometryReshapesInUnion,
			bool useFallbacks)
		{
			var pointsOnTarget = new List<IPoint>();

			foreach (PolycurveInUnionReshapeInfo singleReshape in
				singleGeometryReshapesInUnion
			)
			{
				ReshapeInfo reshapeInfo = useFallbacks
					                          ? singleReshape.FallbackReshapeInfo
					                          : singleReshape.ReshapeInfo;

				var adjustedCutSubcurve =
					reshapeInfo.CutReshapePath as AdjustedCutSubcurve;

				if (adjustedCutSubcurve != null)
				{
					pointsOnTarget.AddRange(
						adjustedCutSubcurve.GetPotentialTargetInsertPoints());
				}
			}

			EnsureTargetIntersectionPoints(geometries, pointsOnTarget);
		}

		/// <summary>
		/// Ensures that the target intersection points are also added to the provided geometries (those that were
		/// reshaped reshaped individually):
		/// ---------------
		/// |        |    |
		/// |        |    |
		/// |        |    |
		/// ---------#-----
		///  \           /
		///   \         /
		///    \       /
		///     \     /
		///      \   *
		///       \ /
		///
		///             \    /
		/// Reshape line:\  /
		///               \/
		///
		/// Source intersection points: #
		/// Target intersection point created by shared source boundary prolongation (instead of user-defined): *
		/// 
		/// </summary>
		/// <param name="geometries"></param>
		/// <param name="targetIntersectionPoints"></param>
		private static void EnsureTargetIntersectionPoints(
			IEnumerable<IGeometry> geometries,
			IList<IPoint> targetIntersectionPoints)
		{
			foreach (IGeometry nonReshapableGeometry in geometries)
			{
				double xyTolerance = GeometryUtils.GetXyTolerance(nonReshapableGeometry);

				// Existing vertices do not need updating:
				const bool allowZDifference = true;

				ReshapeUtils.EnsurePointsExistInTarget(
					nonReshapableGeometry, targetIntersectionPoints, xyTolerance,
					allowZDifference);
			}
		}

		private static IDictionary<IPoint, IPoint> PairByOrderingAlongLines(
			IPointCollection sourcePoints, IPointCollection targetPoints,
			ReshapeInfo unionReshapeInfo, IPolycurve highLevelUnionReshapeLine)
		{
			var unionSourceReplacedPolyline =
				(IPolycurve) GeometryUtils.GetHighLevelGeometry(
					unionReshapeInfo.ReplacedSegments);

			// order the same way as the reshape path
			if (! GeometryUtils.AreEqualInXY(
				    unionSourceReplacedPolyline.FromPoint,
				    highLevelUnionReshapeLine.FromPoint))
			{
				unionSourceReplacedPolyline.ReverseOrientation();
			}

			double tolerance = GeometryUtils.GetXyTolerance(highLevelUnionReshapeLine);

			List<KeyValuePair<IPoint, double>> sourceIntersections =
				GeometryUtils.GetDistancesAlongPolycurve(
					sourcePoints, unionSourceReplacedPolyline,
					tolerance, false);

			// ascending sort order
			sourceIntersections.Sort((x, y) => x.Value.CompareTo(y.Value));

			// Order the target intersection points along the reshape line

			List<KeyValuePair<IPoint, double>> targetIntersections =
				GeometryUtils.GetDistancesAlongPolycurve(
					targetPoints, highLevelUnionReshapeLine,
					tolerance, false);

			// ascending sort order
			targetIntersections.Sort((x, y) => x.Value.CompareTo(y.Value));

			// Make sure they are all on their line:
			Assert.AreEqual(sourceIntersections.Count, targetIntersections.Count,
			                "The source intersection count is not equal the target intersection count.");

			var sourceTargetPairs = new Dictionary<IPoint, IPoint>();
			for (var i = 0; i < sourceIntersections.Count; i++)
			{
				sourceTargetPairs.Add(sourceIntersections[i].Key,
				                      targetIntersections[i].Key);
			}

			return sourceTargetPairs;
		}

		private static IPointCollection GetSourceIntersectionPointsToMove(
			[NotNull] IEnumerable<PolycurveInUnionReshapeInfo> singleGeometryReshapesInUnion,
			[NotNull] IPolycurve highLevelUnionReshapeLine)
		{
			var sourcePointsToMove = (IPointCollection)
				GeometryFactory.CreateEmptyMultipoint(
					highLevelUnionReshapeLine);

			foreach (
				PolycurveInUnionReshapeInfo polycurveInUnionReshapeInfo in
				singleGeometryReshapesInUnion)
			{
				AddIfReplacedSourcePoint(
					polycurveInUnionReshapeInfo.SourceReplacementPath.FromPoint,
					highLevelUnionReshapeLine, sourcePointsToMove);

				AddIfReplacedSourcePoint(
					polycurveInUnionReshapeInfo.SourceReplacementPath.ToPoint,
					highLevelUnionReshapeLine, sourcePointsToMove);
			}

			return sourcePointsToMove;
		}

		private static void AddIfReplacedSourcePoint(IPoint sourcePoint,
		                                             IGeometry highLevelUnionReshapeLine,
		                                             IPointCollection toPointCollection)
		{
			bool isReplacedSourceIntersection =
				! GeometryUtils.Intersects(highLevelUnionReshapeLine, sourcePoint);

			if (isReplacedSourceIntersection &&
			    ! GeometryUtils.Intersects(sourcePoint, (IGeometry) toPointCollection))
			{
				toPointCollection.AddPoint(sourcePoint);
			}
		}

		#endregion

		#region Ensure Results do not Overlap

		private void RemoveContainedGeometries(
			IDictionary<IGeometry, NotificationCollection> reshapedGeometries,
			IPolygon containingPolygon, IList<IPath> reshapePaths)
		{
			// Get original shape:
			var containedGeometries = new List<KeyValuePair<IFeature, IGeometry>>();

			IGeometry originalContainingGeometry = null;
			foreach (
				KeyValuePair<IFeature, IGeometry> keyValuePair in
				ReshapeGeometryCloneByFeature)
			{
				IGeometry reshapedGeometry = keyValuePair.Value;

				if (reshapedGeometry == containingPolygon)
				{
					originalContainingGeometry = keyValuePair.Key.Shape;
				}
				else if (GeometryUtils.Contains(containingPolygon, reshapedGeometry))
				{
					containedGeometries.Add(keyValuePair);
				}
			}

			Assert.True(containedGeometries.Count > 0, "No contained geometries found");
			Assert.NotNull(originalContainingGeometry);

			var reductionGeometries = new List<IGeometry>();
			foreach (KeyValuePair<IFeature, IGeometry> containedGeometry in
				containedGeometries
			)
			{
				IGeometry reshapedContainedPoly = containedGeometry.Value;
				IGeometry originalContainedGeometry = containedGeometry.Key.Shape;

				IPolygon reduction = CalculateReductionPolygon(
					reshapedContainedPoly, reshapePaths, originalContainedGeometry,
					originalContainingGeometry);

				reductionGeometries.Add(reduction);
			}

			var reductionPolygon = (IPolygon) GeometryUtils.Union(reductionGeometries);

			RemoveArea(reductionPolygon, containingPolygon, reshapedGeometries, null);
		}

		/// <summary>
		/// Calculates the geometry to be removed from the containing geometry.
		/// </summary>
		/// <param name="containedGeometry"></param>
		/// <param name="reshapePaths"></param>
		/// <param name="originalSmallGeometry"></param>
		/// <param name="originalContainingGeometry"></param>
		/// <returns></returns>
		private IPolygon CalculateReductionPolygon(IGeometry containedGeometry,
		                                           IList<IPath> reshapePaths,
		                                           IGeometry originalSmallGeometry,
		                                           IGeometry originalContainingGeometry)
		{
			// Overlaps in the original should still be overlaps -> remove the overlap from the reduction geometry

			// Get the area that is overlapping in the original
			IGeometry overlap = IntersectionUtils.GetIntersection(originalSmallGeometry,
				originalContainingGeometry);

			// reshape the original overlap using the sketch and the target intersection points!
			if (! overlap.IsEmpty && StickyIntersectionPoints != null)
			{
				overlap = ReshapeSticky(reshapePaths, overlap) ?? overlap;
			}

			IGeometry reductionGeometry =
				((ITopologicalOperator) containedGeometry).Difference(overlap);

			// Now do the same with gaps (islands): gaps between geometries should not be filled -> add to reduction geometry
			// NOTE: Using auto-complete would only work in some specific cut-back cases
			IGeometry originalUnion =
				GeometryUtils.UnionFeatures(ReshapeGeometryCloneByFeature.Keys.ToList());

			IList<IRing> exteriorRings;
			IList<IRing> interiorRings = GeometryUtils.GetRings(originalUnion,
			                                                    out exteriorRings);

			if (interiorRings.Count > 0)
			{
				IPolygon holes = GeometryFactory.CreatePolygon(interiorRings);

				GeometryUtils.Simplify(holes);

				IGeometry reshapedHoles = ReshapeSticky(reshapePaths, holes);

				reductionGeometry =
					GeometryUtils.Union(reductionGeometry, reshapedHoles ?? holes);
			}

			return (IPolygon) reductionGeometry;
		}

		private IGeometry ReshapeSticky(IEnumerable<IPath> reshapePaths,
		                                IGeometry geometry)
		{
			IGeometry geometryToReshape = GeometryFactory.Clone(geometry);

			var features = new Dictionary<IGeometry, IGeometry>
			               {
				               {
					               geometry,
					               geometryToReshape
				               }
			               };

			var stickyIntersectionReshaper =
				new StickyIntersectionsMultiplePolygonReshaper(
					features,
					new Dictionary<IGeometry, IList<ReshapeInfo>>(),
					StickyIntersectionPoints)
				{
					RefreshArea = RefreshArea
				};

			IDictionary<IGeometry, NotificationCollection> result =
				new Dictionary<IGeometry, NotificationCollection>();
			foreach (IPath reshapePath in reshapePaths)
			{
				AddCombinedReshapeRange(
					stickyIntersectionReshaper.ReshapeGeometries(reshapePath, null),
					result);
			}

			return result.Count > 0
				       ? geometryToReshape
				       : null;
		}

		#endregion

		private List<IPath> GetRemainingNonHolePaths(IEnumerable<IPath> reshapePaths,
		                                             IPolygon sketchAsPolygon)
		{
			// Filter the reshape paths and try to make those that are not part of the ring simple
			var nonHolePaths = new List<IPath>();
			var mergeablePaths = new List<IPath>();

			var targetPoints =
				(IGeometry) StickyIntersectionPoints?.GetTargetPointCollection();

			IPolyline sketchAsPolygonBoundary =
				GeometryFactory.CreatePolyline(sketchAsPolygon);

			foreach (IPath reshapePath in reshapePaths)
			{
				IGeometry highLevelPath =
					GeometryUtils.GetHighLevelGeometry(reshapePath, true);

				if (! GeometryUtils.InteriorIntersects(
					    sketchAsPolygonBoundary, highLevelPath))
				{
					// The reshapePath is not part of any hole
					if (targetPoints != null &&
					    GeometryUtils.Intersects(highLevelPath, targetPoints))
					{
						IPoint fromPoint = reshapePath.FromPoint;
						IPoint toPoint = reshapePath.ToPoint;

						// One or the other end has a target intersection point. It can be reshaped directly unless
						// the other end touches a hole and has no target intersection point. In the latter case a merge is required

						if (! GeometryUtils.Intersects(fromPoint, targetPoints) &&
						    GeometryUtils.Touches(fromPoint, sketchAsPolygon))
						{
							mergeablePaths.Add(reshapePath);
						}
						else if (! GeometryUtils.Intersects(toPoint, targetPoints) &&
						         GeometryUtils.Touches(toPoint, sketchAsPolygon))
						{
							mergeablePaths.Add(reshapePath);
						}
						else
						{
							// either both from- and to-points have target intersection points
							// or one is at the sketch end

							// Reshape as separate paths, connected by source-target connections
							nonHolePaths.Add(reshapePath);
						}
					}
					else
					{
						// try make  a working reshape path by simplifying collection
						mergeablePaths.Add(reshapePath);
					}
				}
			}

			if (mergeablePaths.Count > 0)
			{
				IPolyline remainingPaths = GeometryFactory.CreatePolyline(mergeablePaths);
				GeometryUtils.Simplify(remainingPaths, true, true);

				if (((IGeometryCollection) remainingPaths).GeometryCount > 1)
				{
					ConnectPathsUsingPolyBoundary(remainingPaths, sketchAsPolygon);
				}

				nonHolePaths.AddRange(GeometryUtils.GetPaths(remainingPaths));
			}

			return nonHolePaths;
		}
	}
}
