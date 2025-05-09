using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork.Editing
{
	/// <summary>
	/// Provides functionality to emulate the behaviour of geometric networks when updating edges,
	/// specifically dragging along connected junction/edge features with moved end points.
	/// In the special case of a node with degree 3 junctions are moved 'along' the existing
	/// edges (Y-reshape).
	/// Additionally, snapping onto existing edges and junctions is supported by using the
	/// <see cref="ILinearNetworkFeatureFinder.SearchTolerance"/>.
	/// Once the subclass for geometric networks is deleted, all virtual / protected keywords
	/// can be deleted / made private.
	/// </summary>
	public class LinearNetworkNodeUpdater
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public LinearNetworkNodeUpdater([NotNull] ILinearNetworkFeatureFinder networkFeatureFinder)
		{
			Assert.ArgumentNotNull(networkFeatureFinder, nameof(networkFeatureFinder));

			NetworkFeatureFinder = networkFeatureFinder;
		}

		protected ILinearNetworkFeatureFinder NetworkFeatureFinder { get; }

		/// <summary>
		/// The geometry that determines the topological relation with the adjacent features whose
		/// end points should be relocated based on the connectivity analysis. If an adjacent feature
		/// originally does not cross the BarrierGeometryOriginal geometry, the result should not
		/// cross the BarrierGeometryChanged.
		/// </summary>
		public IPolycurve BarrierGeometryOriginal { protected get; set; }

		/// <summary>
		/// The barrier geometry that should not be crossed because of relocating end points.
		/// </summary>
		public IPolycurve BarrierGeometryChanged { protected get; set; }

		/// <summary>
		/// The features that are explicitly updated and whose end points should not be relocated
		/// based on the connectivity with other updates.
		/// </summary>
		public HashSet<IFeature> ExcludeFromEndpointRelocation { get; set; }

		/// <summary>
		/// The features that were inserted in the transaction and whose end points should not
		/// be relocated based on the connectivity with other updates. The knowledge of the inserts
		/// is also important to avoid incorrect endpoint re-locations of adjacent edges during
		/// splits.
		/// </summary>
		public HashSet<IFeature> KnownInserts { get; set; }

		/// <summary>
		/// The features that were deleted in the transaction. They are important to avoid
		/// incorrect endpoint re-location during merge operations.
		/// </summary>
		public IList<IFeature> KnownDeletes { get; set; }

		/// <summary>
		/// The envelope that contains the changes and requires to be refreshed.
		/// </summary>
		[CanBeNull]
		public IEnvelope RefreshEnvelope { get; protected set; }

		/// <summary>
		/// The list of features updated by this instance.
		/// </summary>
		public IList<IFeature> UpdatedFeatures { get; } = new List<IFeature>();

		/// <summary>
		/// The new location of the edge end points that have been moved during the update.
		/// This could theoretically be used by the caller to perform subsequent splits.
		/// </summary>
		public IList<IPoint> MovedEdgeEndpoints { get; } = new List<IPoint>();

		public bool DisableSplittingByNodes { get; set; }

		public void UpdateFeature(
			[NotNull] IFeature reshapedFeature,
			[NotNull] IGeometry newGeometry)
		{
			var oldPolyline = (IPolyline) reshapedFeature.Shape;

			UpdateFeature(reshapedFeature, oldPolyline, newGeometry);
		}

		public void UpdateNodeFeature(
			[NotNull] IFeature junctionFeature,
			[NotNull] IPoint newPoint)
		{
			var oldPoint = (IPoint) junctionFeature.Shape;

			StoreSingleFeatureShape(junctionFeature, newPoint);

			// Drag along existing edges:
			JunctionUpdated(oldPoint, (IPoint) junctionFeature.Shape);

			if (! DisableSplittingByNodes)
			{
				// Split existing edges at the new junction:
				foreach (IFeature edgeFeature in NetworkFeatureFinder.FindEdgeFeaturesAt(newPoint))
				{
					IPolyline edge = (IPolyline) edgeFeature.ShapeCopy;

					if (! GeometryUtils.InteriorIntersects(edge, newPoint))
					{
						continue;
					}

					var newEdges =
						LinearNetworkEditUtils.SplitAtJunctions(
							edgeFeature, new[] { junctionFeature });

					foreach (IFeature newEdge in newEdges)
					{
						NetworkFeatureFinder.TargetFeatureCandidates?.Add(newEdge);
					}
				}
			}
		}

		protected virtual void UpdateFeature(
			[NotNull] IFeature reshapedFeature,
			[NotNull] IPolyline originalPolyline,
			[NotNull] IGeometry newGeometry)
		{
			var newPolyline = (IPolyline) newGeometry;

			UpdateEdgeNodes(reshapedFeature, originalPolyline, newPolyline);

			StoreSingleFeatureShape(reshapedFeature, newPolyline);

			AddToRefreshArea(originalPolyline);
			AddToRefreshArea(newPolyline);
		}

		/// <summary>
		/// Update the adjacent features at the changed end of the reshaped edge feature:
		/// - If not snapped to an edge: Pull the last segment of adjacent edges and a potential
		///   junction feature to the new end point's location.
		/// - If the new end point is snapped onto the adjacent edge's interior:
		///   The original split point (including the junction) is relocated along the snapped target.
		/// </summary>
		/// <param name="reshapedEdgeFeature"></param>
		/// <param name="originalPolyline"></param>
		/// <param name="newPolyline"></param>
		public bool UpdateEdgeNodes([NotNull] IFeature reshapedEdgeFeature,
		                            [NotNull] IPolyline originalPolyline,
		                            [NotNull] IPolyline newPolyline)
		{
			IPoint origFrom = originalPolyline.FromPoint;
			IPoint origTo = originalPolyline.ToPoint;

			IPoint newFrom = newPolyline.FromPoint;
			IPoint newTo = newPolyline.ToPoint;

			double xyResolution = GeometryUtils.GetXyResolution(reshapedEdgeFeature) / 2;
			double zResolution = GeometryUtils.GetZResolution(reshapedEdgeFeature) / 2;

			bool updatedAtFrom =
				! GeometryUtils.IsSamePoint(origFrom, newFrom, xyResolution, zResolution);
			bool updatedAtTo =
				! GeometryUtils.IsSamePoint(origTo, newTo, xyResolution, zResolution);

			bool flippedFrom = GeometryUtils.IsSamePoint(newFrom, origTo, xyResolution, double.NaN);
			bool flippedTo = GeometryUtils.IsSamePoint(newTo, origFrom, xyResolution, double.NaN);

			if (flippedFrom && flippedTo)
			{
				// TOP-5250: Be consistent with geometric network and keep the connectivity.
				return false;
			}

			bool fromPointSnapped = false, toPointSnapped = false;
			if (updatedAtFrom)
			{
				IList<IFeature> connectedNonReshapedEdgesAtFrom = GetConnectedNonReshapedEdges(
					reshapedEdgeFeature, originalPolyline, LineEnd.From);

				fromPointSnapped = EnsureConnectivityWithAdjacentFeatures(
					reshapedEdgeFeature, originalPolyline, true, newFrom,
					connectedNonReshapedEdgesAtFrom);

				MovedEdgeEndpoints.Add(newFrom);
			}

			if (updatedAtTo)
			{
				IList<IFeature> connectedNonReshapedEdgesAtTo = GetConnectedNonReshapedEdges(
					reshapedEdgeFeature, originalPolyline, LineEnd.To);

				toPointSnapped = EnsureConnectivityWithAdjacentFeatures(
					reshapedEdgeFeature, originalPolyline, false, newTo,
					connectedNonReshapedEdgesAtTo);

				MovedEdgeEndpoints.Add(newTo);
			}

			if (fromPointSnapped)
			{
				newPolyline.FromPoint = newFrom;
			}

			if (toPointSnapped)
			{
				newPolyline.ToPoint = newTo;
			}

			return fromPointSnapped || toPointSnapped;
		}

		public void JunctionUpdated([NotNull] IPoint originalLocation,
		                            [NotNull] IPoint newLocation)
		{
			// TODO: Delete found junction (subsumption) at the newPoint (e.g. if same subtype)
			// TODO: Something useful if junction was moved along an edge

			IList<IFeature> connectedEdges =
				NetworkFeatureFinder.FindEdgeFeaturesAt(
					originalLocation,
					f => GeometryUtils.Touches(f.Shape, originalLocation));

			MaintainConnectionWithOtherNetworkEdges(
				connectedEdges, originalLocation, newLocation);

			MoveJunctionFeatures(originalLocation, newLocation);
		}

		/// <summary>
		/// Updates the specified reshaped feature and the connected network features assuming that only 
		/// one end point has been updated in the newGeometry of the reshaped feature and the other end
		/// point has remained identical.
		/// </summary>
		/// <param name="reshapedFeature"></param>
		/// <param name="newGeometry"></param>
		/// <param name="notifications"></param>
		public virtual void UpdateFeatureEndpoint(
			[NotNull] IFeature reshapedFeature,
			[NotNull] IGeometry newGeometry,
			[CanBeNull] NotificationCollection notifications)
		{
			UpdateFeature(reshapedFeature, newGeometry);
		}

		/// <summary>
		/// Updates the specified feature with the new geometry without dragging along any other features.
		/// </summary>
		/// <param name="feature"></param>
		/// <param name="newShape"></param>
		public virtual void StoreSingleFeatureShape([NotNull] IFeature feature,
		                                            [NotNull] IGeometry newShape)
		{
			GdbObjectUtils.SetFeatureShape(feature, newShape);
			feature.Store();

			UpdatedFeatures.Add(feature);
		}

		protected IList<IFeature> GetConnectedNonReshapedEdges(
			[NotNull] IFeature withFeature,
			[NotNull] IPolyline originalPolyline,
			LineEnd atLineEnd)
		{
			IList<IFeature> connectedEdges =
				NetworkFeatureFinder.GetConnectedEdgeFeatures(
					withFeature, originalPolyline, atLineEnd);

			IList<IFeature> connectedNonReshapedEdges =
				connectedEdges.Where(
					              connectedEdge =>
						              ExcludeFromEndpointRelocation == null ||
						              ! ExcludeFromEndpointRelocation.Contains(connectedEdge))
				              .ToList();

			return connectedNonReshapedEdges;
		}

		private void MaintainConnectionWithOtherNetworkEdges(
			[NotNull] IEnumerable<IFeature> connectedEdges,
			[NotNull] IPolyline originalPolyline,
			[NotNull] IPoint newEndpoint,
			bool atFromPoint)
		{
			IPoint previousEndpoint = atFromPoint
				                          ? originalPolyline.FromPoint
				                          : originalPolyline.ToPoint;

			MaintainConnectionWithOtherNetworkEdges(
				connectedEdges, previousEndpoint, newEndpoint);
		}

		private void MaintainConnectionWithOtherNetworkEdges(
			[NotNull] IEnumerable<IFeature> connectedEdges,
			[NotNull] IPoint originalEndpoint,
			[NotNull] IPoint newEndpoint)
		{
			foreach (IFeature connectedEdge in connectedEdges)
			{
				_msg.DebugFormat(
					"MaintainConnectionWithOtherNetworkFeatures: Checking {0} for necessary update...",
					GdbObjectUtils.ToString(connectedEdge));

				// Possibly connectedEdges could be changed to connectedFeatures (including points)
				var connectedPolyline = connectedEdge.Shape as IPolyline;

				if (connectedPolyline == null)
				{
					continue;
				}

				// add to refresh area before updating
				AddToRefreshArea(connectedPolyline);

				IPolyline reconnectedLine;
				if (GeometryUtils.AreEqualInXY(originalEndpoint, connectedPolyline.FromPoint))
				{
					reconnectedLine = Reconnect(connectedPolyline, newEndpoint, true);
				}
				else if (GeometryUtils.AreEqualInXY(originalEndpoint, connectedPolyline.ToPoint))
				{
					reconnectedLine = Reconnect(connectedPolyline, newEndpoint, false);
				}
				else
				{
					continue;
				}

				StoreSingleFeatureShape(connectedEdge, Assert.NotNull(reconnectedLine));

				// and again after updating
				AddToRefreshArea(reconnectedLine);
			}
		}

		protected void AddToRefreshArea([NotNull] IGeometry geometry)
		{
			if (RefreshEnvelope == null)
			{
				RefreshEnvelope = geometry.Envelope;
			}
			else
			{
				RefreshEnvelope.Union(geometry.Envelope);
			}
		}

		[NotNull]
		private IPolyline Reconnect([NotNull] IPolyline polyline,
		                            [NotNull] IPoint newEndPoint, bool atFromPoint)
		{
			IPolyline result = GeometryFactory.Clone(polyline);

			bool avoidBarrier = BarrierGeometryOriginal != null &&
			                    ! GeometryUtils.InteriorIntersects(polyline,
				                    BarrierGeometryOriginal);

			SetEndpoint(result, newEndPoint, atFromPoint);

			if (avoidBarrier)
			{
				// try to connect in a clever way such that the topological relation to the barrier remains unchanged
				if (BarrierGeometryChanged != null &&
				    GeometryUtils.InteriorIntersects(polyline, BarrierGeometryChanged))
				{
					// cut back to the last vertex on the original side of the barrier, i.e. remove all 
					// vertices between the end point and the intersection point
					result = RemoveVerticesBeyondBarrier(polyline, result, newEndPoint,
					                                     atFromPoint);
				}
			}

			return result;
		}

		protected IPolyline RemoveVerticesBeyondBarrier([NotNull] IPolyline originalPolyline,
		                                                [NotNull] IPolyline
			                                                polylineWithUpdatedEnd,
		                                                [NotNull] IPoint newEndPoint,
		                                                bool newEndAtFromPoint)
		{
			var intersectionPoints =
				(IPointCollection) IntersectionUtils.GetIntersectionPoints(
					originalPolyline, BarrierGeometryChanged, true,
					IntersectionPointOptions.IncludeLinearIntersectionEndpoints);

			if (intersectionPoints.PointCount != 0)
			{
				IPolyline cracked = GeometryFactory.Clone(polylineWithUpdatedEnd);

				const bool createParts = true;
				const bool projectPointsOntoPathToSplit = true;
				GeometryUtils.CrackPolycurve(cracked, intersectionPoints,
				                             projectPointsOntoPathToSplit, createParts);

				IPoint unChangedPoint = newEndAtFromPoint
					                        ? polylineWithUpdatedEnd.ToPoint
					                        : polylineWithUpdatedEnd.FromPoint;

				foreach (IPath subCurve in GeometryUtils.GetPaths(cracked))
				{
					var highLevelSubCurve =
						(IPolyline) GeometryUtils.GetHighLevelGeometry(subCurve, true);

					if (GeometryUtils.Intersects(highLevelSubCurve, unChangedPoint))
					{
						// re-apply the end point - this could still result in bbarrier crossings, but only on the last segment
						SetEndpoint(highLevelSubCurve, newEndPoint, newEndAtFromPoint);

						polylineWithUpdatedEnd = highLevelSubCurve;
						break;
					}
				}
			}

			return polylineWithUpdatedEnd;
		}

		protected static void SetEndpoint(IPolyline polyline, IPoint newEndPoint,
		                                  bool atFromPoint)
		{
			if (atFromPoint)
			{
				polyline.FromPoint = newEndPoint;
			}
			else
			{
				polyline.ToPoint = newEndPoint;
			}
		}

		protected static bool IsRelocatedEndSnappedToAdjacentEdge(
			[NotNull] IPolyline originalPolyline,
			bool atFromPoint,
			[NotNull] IPoint newEndPoint,
			[NotNull] ICollection<IFeature> otherEdgesAtNewEnd,
			out IFeature targetEdgeFeature)
		{
			targetEdgeFeature = null;

			if (otherEdgesAtNewEnd.Count == 0)
			{
				return false;
			}

			IPoint origEndPoint =
				atFromPoint ? originalPolyline.FromPoint : originalPolyline.ToPoint;

			// Snap target definition: 
			//	- not the reshaped feature
			//  - intersection not at end point of target -> test for interior intersection
			foreach (IFeature candidateFeature in otherEdgesAtNewEnd)
			{
				if (! GeometryUtils.InteriorIntersects(newEndPoint, candidateFeature.Shape))
				{
					continue;
				}

				if (GeometryUtils.Touches(origEndPoint, candidateFeature.Shape))
				{
					targetEdgeFeature = candidateFeature;
					return true;
				}
			}

			return false;
		}

		private void ShortenTargetEdge([NotNull] IFeature targetEdge,
		                               [NotNull] IPoint originalEndPoint,
		                               [NotNull] IPoint newEndPoint,
		                               out IPolyline cutOffLine)
		{
			var targetPolyline = (IPolyline) targetEdge.Shape;

			// to save another round trip later on:
			AddToRefreshArea(targetPolyline);

			IPolyline shorterLine;
			IPolyline longerLine;
			Assert.True(GeometryUtils.TrySplitPolyline(
				            targetPolyline, newEndPoint, false, out shorterLine, out longerLine),
			            "Cannot split target edge");

			// TODO: exclude circles!
			IPolyline remainingLine;
			if (GeometryUtils.Touches(originalEndPoint, shorterLine))
			{
				cutOffLine = shorterLine;
				remainingLine = longerLine;
			}
			else
			{
				cutOffLine = longerLine;
				remainingLine = shorterLine;
			}

			StoreSingleFeatureShape(targetEdge, remainingLine);
		}

		private void ProlongThirdEdge([NotNull] IFeature thirdEdge,
		                              [NotNull] IPolyline cutOffLine)
		{
			Assert.ArgumentNotNull(thirdEdge, nameof(thirdEdge));

			_msg.VerboseDebug(
				() =>
					$"The third connected edge {GdbObjectUtils.ToString(thirdEdge)} is prolonged with cut-off line {GeometryUtils.ToString(cutOffLine)}");

			// add the cut off line to the third edge, take along the junction
			// (if using MaintainConnectivityStretchLastSegment here, it can result in 
			//  COM-exception 0x80044020)
			// TODO: Handle Z-differences according to some option (Union always merges and higher Z-value wins)
			var originalLine = (IPolyline) thirdEdge.ShapeCopy;
			var prolongedLine = (IPolyline) GeometryUtils.Union(originalLine, cutOffLine);

			// NOTE: The cutOffLine and the original thirdEdgeLine can have different orientation.
			//		 The union sometimes produces lines that use the cutOffLine's orientation
			//		 Maintaining the orientation of the third edge is crucial to avoid COM-exception 0x80044020
			EnsureOrientation(originalLine, prolongedLine);

			StoreSingleFeatureShape(thirdEdge, prolongedLine);

			AddToRefreshArea(prolongedLine);
		}

		private static void EnsureOrientation([NotNull] IPolyline original,
		                                      [NotNull] IPolyline prolonged)
		{
			if (GeometryUtils.AreEqualInXY(original.FromPoint, prolonged.FromPoint) ||
			    GeometryUtils.AreEqualInXY(original.ToPoint, prolonged.ToPoint))
			{
				return;
			}

			// otherwise we need flipping
			prolonged.ReverseOrientation();
			((ISegmentCollection) prolonged).SegmentsChanged();

			Assert.True(GeometryUtils.AreEqualInXY(original.FromPoint, prolonged.FromPoint) ||
			            GeometryUtils.AreEqualInXY(original.ToPoint, prolonged.ToPoint),
			            "Unexpected geometry after reshape. One end must stay stable.");
		}

		protected void RelocateEndpointAlongTarget(
			[NotNull] IFeature reshapedFeature,
			IPolyline originalPolyline,
			[NotNull] IPoint newEndPoint, bool isFromPoint,
			[NotNull] IFeature targetEdge,
			[NotNull] IList<IFeature> connectedNonReshapedEdges)
		{
			// cut back the target edge to re-connect at the new end point...

			IPoint originalEndPoint = isFromPoint
				                          ? originalPolyline.FromPoint
				                          : originalPolyline.ToPoint;

			IPolyline cutOffLine;
			ShortenTargetEdge(targetEdge, originalEndPoint, newEndPoint, out cutOffLine);

			// done with this one:
			connectedNonReshapedEdges.Remove(targetEdge);

			if (connectedNonReshapedEdges.Count == 1)
			{
				// put the cut-off part from the target into the remaining edge 
				ProlongThirdEdge(connectedNonReshapedEdges[0], cutOffLine);
			}
			// else: could analyze situation regarding intersections with reshaped line and choose the most appropriate to prolong...

			MaintainConnectionWithOtherFeatures(connectedNonReshapedEdges, reshapedFeature,
			                                    originalPolyline, isFromPoint, newEndPoint);
		}

		protected virtual void MaintainConnectionWithOtherFeatures(
			IEnumerable<IFeature> connectedEdges,
			IFeature reshapedFeature,
			IPolyline originalPolyline,
			bool openJawAtFromPoint,
			IPoint newEndPoint)
		{
			// update connected edges in the same style as the geometric network? Optional?
			MaintainConnectionWithOtherNetworkEdges(
				connectedEdges, originalPolyline, newEndPoint, openJawAtFromPoint);
		}

		protected IList<IFeature> FindOtherEdgeFeaturesAt(
			[NotNull] IFeature reshapedFeature,
			[NotNull] IPoint newEndPoint)
		{
			IList<IFeature> otherPolylineFeatures = NetworkFeatureFinder.FindEdgeFeaturesAt(
				newEndPoint,
				feature =>
					! GdbObjectUtils.IsSameObject(feature, reshapedFeature,
					                              ObjectClassEquality.SameTableSameVersion));

			return otherPolylineFeatures;
		}

		/// <summary>
		/// Ensures the connectivity of the specified feature with its previously connected features
		/// according to specific rules:
		/// - Split or merge operations: no action
		/// - Snap to an edge interior of an *adjacent* edge: endpoint relocation (Y-reshape style)
		/// - Snap to an edge interior of a non-adjacent edge: existing junction at previous end
		///   point gets moved if it has no other previous connection
		/// - Drag along features from original end point if:
		///    - End point was moved to 'empty field' (no other feature exists at new end point)
		///    - Z-only update
		///    - End point moved within search tolerance
		/// </summary>
		/// <param name="reshapedFeature"></param>
		/// <param name="originalPolyline"></param>
		/// <param name="atFromPoint"></param>
		/// <param name="newEndPoint"></param>
		/// <param name="adjacentNonReshapedEdges"></param>
		/// <returns>Whether the specified end point was snapped to adjacent features or not.</returns>
		private bool EnsureConnectivityWithAdjacentFeatures(
			[NotNull] IFeature reshapedFeature,
			[NotNull] IPolyline originalPolyline,
			bool atFromPoint,
			[NotNull] IPoint newEndPoint,
			[NotNull] IList<IFeature> adjacentNonReshapedEdges)
		{
			IList<IFeature> otherHitEdgesAtNewEnd = FindOtherEdgeFeaturesAt(
				reshapedFeature, newEndPoint);

			if (HasInsertAtOldAndNewEnd(adjacentNonReshapedEdges, otherHitEdgesAtNewEnd))
			{
				return false; // Split
			}

			if (HasDeleteAtOldAndNewEnd(originalPolyline, atFromPoint, newEndPoint))
			{
				return false; // Merge
			}

			bool endPointMovedWithinTolerance =
				ContainSameFeatures(adjacentNonReshapedEdges, otherHitEdgesAtNewEnd);

			bool result = ! endPointMovedWithinTolerance &&
			              LinearNetworkEditUtils.SnapPoint(newEndPoint, otherHitEdgesAtNewEnd,
			                                               NetworkFeatureFinder);

			IFeature targetEdge;
			bool snappedToAdjacentEdgeInterior = IsRelocatedEndSnappedToAdjacentEdge(
				originalPolyline, atFromPoint, newEndPoint, otherHitEdgesAtNewEnd, out targetEdge);

			if (snappedToAdjacentEdgeInterior)
			{
				RelocateEndpointAlongTarget(reshapedFeature, originalPolyline, newEndPoint,
				                            atFromPoint, targetEdge,
				                            adjacentNonReshapedEdges);

				MoveJunctionFeatures(originalPolyline, atFromPoint, newEndPoint);
			}
			else if (endPointMovedWithinTolerance ||
			         otherHitEdgesAtNewEnd.Count == 0 ||
			         IsZOnlyChange(originalPolyline, atFromPoint, newEndPoint))
			{
				// Drag along other edge endpoints if no other edge was hit ('disconnect and reconnect')
				// or the update was within the tolerance / Z-only
				MaintainConnectionWithOtherNetworkEdges(
					adjacentNonReshapedEdges, originalPolyline, newEndPoint, atFromPoint);

				// And the junction
				MoveJunctionFeatures(originalPolyline, atFromPoint, newEndPoint);
			}
			else if (adjacentNonReshapedEdges.Count == 0)
			{
				// New end point was snapped to other edges - drag the junction only if it is
				// not connected to any other edges at the previous location
				MoveJunctionFeatures(originalPolyline, atFromPoint, newEndPoint);
			}

			return result;
		}

		private static bool ContainSameFeatures(IList<IFeature> list1, IList<IFeature> list2)
		{
			return ! list1.Except(list2).Any() && ! list2.Except(list1).Any();
		}

		private bool HasDeleteAtOldAndNewEnd(IPolyline originalPolyline,
		                                     bool updatedAtFromPoint, IPoint newEndPoint)
		{
			// Safety net against merge operations:
			// The adjacent edges on the original update should presumably not be re-connected.
			// If there is a delete at both the original end point and the new end point it is
			// most likely a merge operation.

			if (KnownDeletes == null || KnownDeletes.Count == 0)
			{
				return false;
			}

			IPoint oldEndPoint = updatedAtFromPoint
				                     ? originalPolyline.FromPoint
				                     : originalPolyline.ToPoint;

			bool hasDeleteAtOldEnd = false;
			bool hasDeleteAtNewEnd = false;
			foreach (IFeature delete in KnownDeletes)
			{
				IPolyline polyline = delete.Shape as IPolyline;

				if (polyline == null)
				{
					continue;
				}

				IPoint deleteFrom = polyline.FromPoint;
				IPoint deleteTo = polyline.ToPoint;

				if (GeometryUtils.AreEqualInXY(deleteFrom, oldEndPoint) ||
				    GeometryUtils.AreEqualInXY(deleteTo, oldEndPoint))
				{
					hasDeleteAtOldEnd = true;
				}

				if (GeometryUtils.AreEqualInXY(deleteFrom, newEndPoint) ||
				    GeometryUtils.AreEqualInXY(deleteTo, newEndPoint))
				{
					hasDeleteAtNewEnd = true;
				}
			}

			return hasDeleteAtOldEnd && hasDeleteAtNewEnd;
		}

		private bool HasInsertAtOldAndNewEnd(
			[NotNull] IEnumerable<IFeature> adjacentNonReshapedEdges,
			[NotNull] IEnumerable<IFeature> otherHitEdgesAtNewEnd)
		{
			// Safety net against split operations:
			// The adjacent edges on the original end must not be re-connected to the update's end
			// but should remain a the insert's end.
			// If there are inserts both on the original end and the new end of an update, it is
			// most likely an insert!

			if (KnownInserts == null || KnownInserts.Count == 0)
			{
				return false;
			}

			if (adjacentNonReshapedEdges.Any(KnownInserts.Contains) &&
			    otherHitEdgesAtNewEnd.Any(KnownInserts.Contains))
			{
				return true;
			}

			return false;
		}

		private static bool IsZOnlyChange([NotNull] IPolyline originalPolyline,
		                                  bool atFromPoint,
		                                  [NotNull] IPoint newEndPoint)
		{
			IPoint oldPoint = atFromPoint ? originalPolyline.FromPoint : originalPolyline.ToPoint;

			return GeometryUtils.AreEqualInXY(oldPoint, newEndPoint);
		}

		private void MoveJunctionFeatures([NotNull] IPolyline originalPolyline,
		                                  bool atFromPoint,
		                                  [NotNull] IPoint newEndPoint)
		{
			IPoint previousEndpoint = atFromPoint
				                          ? originalPolyline.FromPoint
				                          : originalPolyline.ToPoint;

			MoveJunctionFeatures(previousEndpoint, newEndPoint);
		}

		private void MoveJunctionFeatures([NotNull] IPoint previousEndpoint,
		                                  [NotNull] IPoint newEndPoint)
		{
			IList<IFeature> junctionFeatures =
				NetworkFeatureFinder.FindJunctionFeaturesAt(previousEndpoint);

			if (junctionFeatures.Count == 0)
			{
				return;
			}

			// TOP-5858: Move all coincident junctions. There can be more than one at the same
			// location even in the same network. Hence we should not check if there is one already.
			// Except if the there is a junction feature at the target location that matches the
			// source feature's type
			// TODO: Find only other junctions of the same network class (including potential where clause):
			// -> Enhance NetworkFeatureFinder to support this: FindJunctionFeaturesAt(newEndPoint, inSameNetworkClassFeature)
			IList<IFeature> otherJunctionsAtNewEndPoint =
				NetworkFeatureFinder.FindJunctionFeaturesAt(newEndPoint).ToList();

			foreach (IFeature junctionFeature in junctionFeatures.ToList())
			{
				foreach (IFeature otherJunction in otherJunctionsAtNewEndPoint)
				{
					if (GdbObjectUtils.IsSameObject(junctionFeature, otherJunction,
					                                ObjectClassEquality.SameTableSameVersion))
					{
						continue;
					}

					// TODO: If only the relevant other junctions (i.e. from the same network class) are found
					//       this will become obsolete:
					if (DatasetUtils.IsSameObjectClass(otherJunction.Class, junctionFeature.Class,
					                                   ObjectClassEquality.SameTableSameVersion))
					{
						// TOP-5886: Do not move the junction on top of another junction of the same type
						junctionFeatures.Remove(junctionFeature);
						break;
					}
				}
			}

			foreach (IFeature junctionFeature in junctionFeatures)
			{
				StoreSingleFeatureShape(junctionFeature, newEndPoint);
			}
		}
	}
}
