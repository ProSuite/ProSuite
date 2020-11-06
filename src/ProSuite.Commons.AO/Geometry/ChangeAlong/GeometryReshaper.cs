using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.LinearNetwork.Editing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	[CLSCompliant(false)]
	public class GeometryReshaper : GeometryReshaperBase
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IFeature _feature;

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryReshaper"/> class.
		/// </summary>
		/// <param name="feature">The feature whose geometry shall be reshaped</param>
		/// <param name="editOperationObservers">the observers (e.g. AutoAttributeUpdater) which react on an edit initiated by this class</param>
		public GeometryReshaper([NotNull] IFeature feature, IList<ToolEditOperationObserver> editOperationObservers)
			: base(new List<IFeature> {feature}, editOperationObservers)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			_feature = feature;
			EditOperationObservers = editOperationObservers;
		}

		public bool AllowOpenJawReshape { private get; set; }

		public override IDictionary<IGeometry, NotificationCollection> Reshape(
			IPath reshapePath,
			bool tryReshapeRingNonDefaultSide,
			NotificationCollection notifications)
		{
			IGeometry geometryToReshape;

			bool reshaped = TryReshape(reshapePath, tryReshapeRingNonDefaultSide,
			                           notifications,
			                           out geometryToReshape);

			var reshapedGeometries = new Dictionary<IGeometry, NotificationCollection>();

			if (reshaped)
			{
				reshapedGeometries.Add(geometryToReshape, notifications);
			}

			return reshapedGeometries;
		}

		public override IDictionary<IGeometry, NotificationCollection> Reshape(
			IList<IPath> reshapePaths, bool tryReshapeRingNonDefaultSide,
			NotificationCollection notifications)
		{
			if (reshapePaths.Count == 1)
			{
				return Reshape(reshapePaths[0], tryReshapeRingNonDefaultSide,
				               notifications);
			}

			throw new NotImplementedException("Multi-path reshape not implemented");
		}

		public override bool AddRefreshAreaPadding => OpenJawReshapeOcurred;

		protected override Dictionary<IGeometry, NotificationCollection> ReshapeCore(
			IReshapeAlongCurves reshapeAlongCurves,
			Predicate<IPath> canReshapePredicate,
			bool useNonDefaultReshapeSide,
			NotificationCollection notifications)
		{
			// In the polygon selection case the user might want to BOTH
			// reshape the green curves AND preselect+TryReshape the yellow ones fully inside...
			const bool includeAllPreSelectedCandidates = true;
			IList<CutSubcurve> curvesToReshape =
				reshapeAlongCurves.GetSelectedReshapeCurves(canReshapePredicate,
				                                            includeAllPreSelectedCandidates);

			if (curvesToReshape.Count > 0)
			{
				return ReshapeCore(curvesToReshape, useNonDefaultReshapeSide,
				                   notifications);
			}

			return new Dictionary<IGeometry, NotificationCollection>(0);
		}

		protected override Dictionary<IGeometry, NotificationCollection> ReshapeCore(
			IList<CutSubcurve> reshapeCurves,
			bool useNonDefaultReshapeSide,
			NotificationCollection notifications)
		{
			var reshapedGeometries = new Dictionary<IGeometry, NotificationCollection>();

			Assert.ArgumentNotNull(reshapeCurves, nameof(reshapeCurves));
			Assert.AreEqual(1, ReshapeGeometryCloneByFeature.Count,
			                "Unexpected number of reshape features: {0}",
			                ReshapeGeometryCloneByFeature.Count);

			IGeometry geometryToReshape = ReshapeGeometryCloneByFeature[_feature];

			IGeometry reshapedGeometry = null;

			if (reshapeCurves.Count > 0)
			{
				ICollection<ReshapeInfo> reshapeInfos;

				reshapedGeometry = ReshapeWithSimplifiedCurves(
					geometryToReshape, reshapeCurves, useNonDefaultReshapeSide,
					notifications, out reshapeInfos);

				AddPotentialTargetInsertPoints(reshapeCurves);

				AddToRefreshArea(reshapeInfos);

				ReleaseReshapeInfos(reshapeInfos);
			}

			if (reshapedGeometry != null)
			{
				reshapedGeometries.Add(reshapedGeometry, notifications);
			}

			return reshapedGeometries;
		}

		protected override void StoreReshapedGeometryCore(
			IFeature feature,
			IGeometry newGeometry,
			NotificationCollection notifications)
		{
			if (! HasEndpointChanged(feature, newGeometry))
			{
				base.StoreReshapedGeometryCore(feature, newGeometry, notifications);

				return;
			}

			_msg.DebugFormat("Saving open jaw reshape on network feature {0}...",
			                 GdbObjectUtils.ToString(feature));

			if (MoveLineEndJunction && NetworkFeatureFinder != null &&
			    newGeometry.GeometryType == esriGeometryType.esriGeometryPolyline)
			{
				StoreOpenJawReshapeWithEndPointMove(feature, newGeometry, notifications);
			}
			else
			{
				StoreOpenJawReshape(feature, newGeometry);
			}
		}

		public override IDictionary<IGeometry, NotificationCollection>
			EnsureResultsNotOverlapping(
				IDictionary<IGeometry, NotificationCollection> reshapedGeometries,
				IList<IPath> reshapePaths)
		{
			// Single reshapes never overlap
			return reshapedGeometries;
		}

		private bool HasEndpointChanged(IFeature feature, IGeometry newGeometry)
		{
			if (OpenJawReshapeOcurred)
			{
				return true;
			}

			if (newGeometry.GeometryType != esriGeometryType.esriGeometryPolyline)
			{
				return false;
			}

			// The end points might have changed in Z only which is not classified as open-jaw:
			var originalPolyline = (IPolyline) feature.Shape;
			var newPolyline = (IPolyline) newGeometry;

			return ! GeometryUtils.AreEqual(originalPolyline.FromPoint, newPolyline.FromPoint) ||
			       ! GeometryUtils.AreEqual(originalPolyline.ToPoint, newPolyline.ToPoint);
		}

		public void StoreOpenJawReshapeWithEndPointMove(
			[NotNull] IFeature reshapedFeature,
			[NotNull] IGeometry newGeometry,
			[CanBeNull] NotificationCollection notifications)
		{
			Assert.NotNull(NetworkFeatureFinder);

			LinearNetworkNodeUpdater linearNetworkUpdater = NetworkFeatureUpdater ??
			                                                new LinearNetworkNodeUpdater(
				                                                NetworkFeatureFinder);

			linearNetworkUpdater.BarrierGeometryOriginal = reshapedFeature.Shape as IPolyline;
			linearNetworkUpdater.BarrierGeometryChanged = newGeometry as IPolyline;

			linearNetworkUpdater.UpdateFeatureEndpoint(reshapedFeature, newGeometry,
			                                           notifications);

			AddToRefreshArea(linearNetworkUpdater.RefreshEnvelope);
		}

		/// <summary>
		/// Reshapes the geometry of the feature to reshape.
		/// </summary>
		/// <param name="reshapeCurves"></param>
		/// <param name="useNonDefaultReshapeSide">Whether the non-default side of inside-only 
		/// polygon reshapes should be used (i.e. the smaller part).</param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		[CanBeNull]
		private IGeometry Reshape([NotNull] IList<CutSubcurve> reshapeCurves,
		                          bool useNonDefaultReshapeSide,
		                          [CanBeNull] NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(reshapeCurves, nameof(reshapeCurves));
			Assert.AreEqual(1, ReshapeGeometryCloneByFeature.Count,
			                "Unexpected number of reshape features: {0}",
			                ReshapeGeometryCloneByFeature.Count);

			IGeometry geometryToReshape = ReshapeGeometryCloneByFeature[_feature];

			IGeometry reshapedGeometry = null;

			if (reshapeCurves.Count > 0)
			{
				ICollection<ReshapeInfo> reshapeInfos;

				reshapedGeometry = ReshapeWithSimplifiedCurves(
					geometryToReshape, reshapeCurves, useNonDefaultReshapeSide,
					notifications, out reshapeInfos);

				AddPotentialTargetInsertPoints(reshapeCurves);

				AddToRefreshArea(reshapeInfos);

				ReleaseReshapeInfos(reshapeInfos);
			}

			return reshapedGeometry;
		}

		/// <summary>
		/// Saves the reshape result of the Reshape method.
		/// </summary>
		/// <param name="transaction"></param>
		/// <param name="editWorkspace"></param>
		/// <param name="undoMessage"></param>
		/// <param name="reshapedGeometry"></param>
		/// <param name="notifications"></param>
		public void SaveResult([NotNull] IGdbTransaction transaction,
		                       [NotNull] IWorkspace editWorkspace,
		                       [NotNull] string undoMessage,
		                       [NotNull] IGeometry reshapedGeometry,
		                       [CanBeNull] NotificationCollection notifications)
		{
			var reshapedGeometries = new Dictionary<IGeometry, NotificationCollection>
			                         {
				                         {
					                         reshapedGeometry,
					                         notifications
				                         }
			                         };

			SaveResult(transaction, editWorkspace, undoMessage, reshapedGeometries);
		}

		private bool TryReshape([NotNull] IPath reshapePath,
		                        bool tryReshapeRingNonDefaultSide,
		                        [CanBeNull] NotificationCollection notifications,
		                        out IGeometry geometryToReshape)
		{
			Assert.Null(TargetFeatures,
			            "Target features are set. This method does not support target feature updates.");

			geometryToReshape = ReshapeGeometryCloneByFeature[_feature];

			var reshapeInfo = new ReshapeInfo(geometryToReshape, reshapePath,
			                                  notifications)
			                  {
				                  ReshapeResultFilter =
					                  GetResultFilter(tryReshapeRingNonDefaultSide),
				                  AllowSimplifiedReshapeSideDetermination =
					                  AllowSimplifiedReshapeSideDetermination,
				                  AllowOpenJawReshape = AllowOpenJawReshape
			                  };

			IList<ReshapeInfo> reshapeInfos;
			bool reshaped = ReshapeUtils.ReshapeAllGeometryParts(reshapeInfo, reshapePath,
			                                                     out reshapeInfos);

			if (reshaped)
			{
				reshaped = AreAllReshapesAllowed(reshapeInfos, notifications);
			}

			if (reshaped)
			{
				AddToRefreshArea(reshapeInfos);

				NotificationIsWarning = reshapeInfo.NotificationIsWarning;

				if (reshapeInfo.IsOpenJawReshape)
				{
					OpenJawReshapeOcurred = true;
					OpenJawIntersectionPointCount =
						reshapeInfo.IntersectionPoints.PointCount;
				}
			}

			foreach (ReshapeInfo singleReshape in reshapeInfos)
			{
				singleReshape.Dispose();
			}

			return reshaped;
		}

		#region Private methods for storing Y-Reshape

		private void StoreOpenJawReshape([NotNull] IFeature feature,
		                                 [NotNull] IGeometry newGeometry)
		{
			//const bool allowSnappingEdgeEndPoints = true;

			if (NetworkFeatureUpdater != null)
			{
				NetworkFeatureUpdater.StoreSingleFeatureShape(feature, newGeometry);
			}
			else
			{
				GdbObjectUtils.SetFeatureShape(feature, newGeometry);
				feature.Store();
			}

			AddToRefreshArea(newGeometry);

			//NetworkUtils.SetFeatureShape(
			//	feature, newGeometry,
			//	GeometricNetworkConnectOption.DisconnectAndReconnect, allowSnappingEdgeEndPoints);
		}

		#endregion
	}
}
