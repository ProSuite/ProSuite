using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork.Editing
{
	/// <summary>
	/// Provides functionality to keep a linear edge-junction network consistent:
	/// - Inserts default junctions when an edge is created or updated an the end
	///   point is not already covered by a junction. The attributes of the inserted
	///   junction can be set by the <see cref="JunctionFeatureFactory"/>.
	///- Splits edges where new junction is inserted (manually or by the above mechanism)
	///  or updated and the new location is on the interior of an edge.
	/// NOTES:
	/// - Updated edge end points to not result in junction features to be dragged along,
	///   instead a new default junction is created. In order to drag the junction along,
	///   use Y-Reshape / Destroy and Rebuild with move-junction option or Topology edit tool.
	/// - Splits do not result in the current edit target to be inserted. Instead the
	///   network default junction class is created. In order to insert a different junction
	///   type, just insert the desired point feature and a split will happen due to the
	///   above mechanism.
	/// </summary>
	public class LinearNetworkEditAgent : EditOperationObserverBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly HashSet<IFeature> _createdInOperation;
		private readonly Dictionary<IFeature, IGeometry> _updatedInOperation;
		private readonly List<IFeature> _deletedInOperation;
		private readonly double _searchTolerance;

		public LinearNetworkEditAgent(
			[NotNull] LinearNetworkDef networkDefinition,
			[NotNull] ILinearNetworkFeatureFinder networkFeatureFinder)
		{
			NetworkDefinition = networkDefinition;
			NetworkFeatureFinder = networkFeatureFinder;

			// TODO: Use CustomTolerance if larger than tolerance
			_searchTolerance =
				SpatialReferenceUtils.GetXyTolerance(NetworkDefinition.GetSpatialReference());

			_createdInOperation = new HashSet<IFeature>();
			_updatedInOperation = new Dictionary<IFeature, IGeometry>(3);
			_deletedInOperation = new List<IFeature>(3);
		}

		[NotNull]
		public LinearNetworkDef NetworkDefinition { get; }

		[NotNull]
		public ILinearNetworkFeatureFinder NetworkFeatureFinder { get; }

		[CanBeNull]
		public IJunctionFeatureFactory JunctionFeatureFactory { get; set; }

		[CanBeNull]
		public IEnvelope RefreshArea { get; set; }

		public bool LeaveConnectedFeaturesInPlace { get; set; }

		public bool DeleteOrphanedJunctionsOnEdgeDelete { get; set; }

		public bool NoCaching { get; set; }

		public NetworkEditValidator NetworkEditValidator { get; set; }

		public IEnumerable<IFeature> GetCreatedInLastOperation()
		{
			return _createdInOperation;
		}

		public IEnumerable<IFeature> GetUpdatedInLastOperation()
		{
			return _updatedInOperation.Keys;
		}

		public override bool ObserveWorkspaceOperations => true;

		public override IEnumerable<IObjectClass> WorkspaceOperationObservableClasses
		{
			get
			{
				return NetworkDefinition.NetworkClassDefinitions.Select(
					networkClass => networkClass.FeatureClass);
			}
		}

		public override void StartedOperation()
		{
			_createdInOperation.Clear();
			_updatedInOperation.Clear();
			_deletedInOperation.Clear();
		}

		public override void Updating(IObject objectToBeStored)
		{
			if (IsCompletingOperation)
			{
				return;
			}

			IFeature storedFeature = objectToBeStored as IFeature;

			if (! NetworkDefinition.IsNetworkFeature(storedFeature))
			{
				return;
			}

			if (_updatedInOperation.ContainsKey(Assert.NotNull(storedFeature)))
			{
				return;
			}

			if (_createdInOperation.Contains(storedFeature))
			{
				return;
			}

			if (! ((IFeatureChanges) storedFeature).ShapeChanged)
			{
				return;
			}

			IGeometry originalShape = ((IFeatureChanges) storedFeature).OriginalShape;

			if (originalShape == null)
			{
				// ObjectClass events (on enterprise GDB) do not call OnCreate(), but OnChange() with null original
				_createdInOperation.Add(storedFeature);
			}
			else
			{
				_updatedInOperation.Add(storedFeature, originalShape);
			}
		}

		public override void Creating(IObject newObject)
		{
			if (IsCompletingOperation)
			{
				return;
			}

			IFeature newFeature = newObject as IFeature;

			if (NetworkDefinition.IsNetworkFeature(newFeature))
			{
				_createdInOperation.Add(newFeature);

				NetworkFeatureFinder.TargetFeatureCandidates?.Add(newFeature);
			}
		}

		public override void Deleting(IObject deletedObject)
		{
			if (IsCompletingOperation)
			{
				return;
			}

			IFeature deletedFeature = deletedObject as IFeature;

			if (NetworkDefinition.IsNetworkFeature(deletedFeature))
			{
				_deletedInOperation.Add(deletedFeature);

				NetworkFeatureFinder.TargetFeatureCandidates?.Remove(deletedFeature);
			}
		}

		public override void CompletingOperation()
		{
			if (! HasEdits())
			{
				return;
			}

			Stopwatch watch = _msg.DebugStartTiming();

			if (NoCaching)
			{
				ApplyNetworkUpdateRules();
			}
			else
			{
				IEnvelope aoi = GetCacheExtent();

				try
				{
					NetworkFeatureFinder.CacheTargetFeatureCandidates(aoi);

					ApplyNetworkUpdateRules();
				}
				finally
				{
					NetworkFeatureFinder.InvalidateTargetFeatureCache();
				}
			}

			_msg.DebugStopTiming(
				watch, "CompletingOperation - processed {0} original network updates",
				_createdInOperation.Count + _updatedInOperation.Count);
		}

		public override void CompletedOperation() { }

		public override int GetHashCode()
		{
			return NetworkDefinition.GetHashCode();
		}

		public override bool Equals(IEditOperationObserver other)
		{
			return Equals((object) other);
		}

		protected bool Equals(LinearNetworkEditAgent other)
		{
			return NetworkDefinition.Equals(other.NetworkDefinition);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((LinearNetworkEditAgent) obj);
		}

		public override string ToString()
		{
			return NetworkDefinition.Name ?? base.ToString();
		}

		private IEnvelope GetCacheExtent()
		{
			IEnvelope aoi = GeometryUtils.UnionFeatureEnvelopes(_createdInOperation);

			aoi.Union(GeometryUtils.UnionFeatureEnvelopes(_updatedInOperation.Keys));
			aoi.Union(GeometryUtils.UnionGeometryEnvelopes(_updatedInOperation.Values));
			aoi.Union(GeometryUtils.UnionFeatureEnvelopes(_deletedInOperation));

			if (! aoi.IsEmpty)
			{
				aoi.Expand(_searchTolerance, _searchTolerance, false);
			}

			return aoi;
		}

		private void ApplyNetworkUpdateRules()
		{
			NetworkEditValidator?.ValidateIndividualGeometries(
				_updatedInOperation, _createdInOperation, NetworkFeatureFinder);

			List<IFeature> junctions = new List<IFeature>();

			if (! LeaveConnectedFeaturesInPlace)
			{
				// Drag along existing features, junctions could split other edges (see below)
				IEnumerable<IFeature> otherDraggedAlongFeatures = DragAdjacentFeaturesAlong();

				junctions.AddRange(GetJunctionFeatures(otherDraggedAlongFeatures));
			}

			// Process edges first -> extra other-edge-splitting junctions might be created

			// Add Junctions for new edges
			foreach (IFeature edgeFeature in GetEdgeFeatures(_createdInOperation))
			{
				junctions.AddRange(InsertRequiredJunctions(edgeFeature));
			}

			// ... and updated edges
			foreach (IFeature edgeFeature in GetEdgeFeatures(_updatedInOperation.Keys))
			{
				junctions.AddRange(InsertRequiredJunctions(edgeFeature));
			}

			// Edge splitting due to user inserted/updated junctions:
			junctions.AddRange(GetJunctionFeatures(_createdInOperation));
			junctions.AddRange(GetJunctionFeatures(_updatedInOperation.Keys));

			TrySplitExistingEdgesByJunctions(junctions);

			if (! NetworkDefinition.HasDefaultJunctionClass)
			{
				// We cannot rely on junctions, the split logic has to be performed at inserted/updated edge end points
				TrySplitExistingEdgesByEdgeEndpoint();
			}

			if (DeleteOrphanedJunctionsOnEdgeDelete)
			{
				DeleteOrphanedJunctions(GetEdgeFeatures(_deletedInOperation));
			}

			NetworkEditValidator?.PerformFinalValidation(_updatedInOperation, _createdInOperation,
			                                             NetworkFeatureFinder);
		}

		private IEnumerable<IFeature> DragAdjacentFeaturesAlong()
		{
			if (_updatedInOperation.Count == 0)
			{
				return new List<IFeature>(0);
			}

			LinearNetworkNodeUpdater nodeUpdater =
				new LinearNetworkNodeUpdater(NetworkFeatureFinder)
				{
					KnownInserts = _createdInOperation,
					KnownDeletes = _deletedInOperation
				};

			DragAdjacentFeaturesAlong(nodeUpdater);

			if (nodeUpdater.RefreshEnvelope != null)
			{
				RefreshArea?.Union(nodeUpdater.RefreshEnvelope);
			}

			_msg.DebugFormat("Maintained connectivity by dragging end points with {0}",
			                 StringUtils.Concatenate(nodeUpdater.UpdatedFeatures,
			                                         f => GdbObjectUtils.ToString(f), ", "));

			return nodeUpdater.UpdatedFeatures;
		}

		private void DragAdjacentFeaturesAlong(LinearNetworkNodeUpdater nodeUpdater)
		{
			var edgeFeatureUpdates = GetEdgeFeatures(_updatedInOperation.Keys).ToList();
			var junctionUpdates = GetJunctionFeatures(_updatedInOperation.Keys).ToList();

			foreach (IFeature edgeFeature in edgeFeatureUpdates)
			{
				var originalPolyline = (IPolyline) _updatedInOperation[edgeFeature];

				IPolyline newPolyline = (IPolyline) edgeFeature.Shape;

				bool newPolylineChanged = nodeUpdater.UpdateEdgeNodes(edgeFeature, originalPolyline,
					newPolyline);

				if (newPolylineChanged)
				{
					GdbObjectUtils.SetFeatureShape(edgeFeature, newPolyline);
					edgeFeature.Store();
				}
			}

			foreach (IFeature junctionFeature in junctionUpdates)
			{
				var originalPoint = (IPoint) _updatedInOperation[junctionFeature];

				nodeUpdater.JunctionUpdated(originalPoint, (IPoint) junctionFeature.Shape);
			}
		}

		public IEnumerable<IFeature> InsertRequiredJunctions([NotNull] IFeature edgeFeature)
		{
			IFeature fromJunctionFeature = InsertRequiredJunction(edgeFeature, true);

			if (fromJunctionFeature != null)
			{
				yield return fromJunctionFeature;
			}

			IFeature toJunctionFeature = InsertRequiredJunction(edgeFeature, false);

			if (toJunctionFeature != null)
			{
				yield return toJunctionFeature;
			}
		}

		private IFeature InsertRequiredJunction(IFeature forEdgeFeature,
		                                        bool atEdgeFromPoint)
		{
			IPolyline edge = (IPolyline) forEdgeFeature.Shape;

			IPoint edgeEndPoint = atEdgeFromPoint ? edge.FromPoint : edge.ToPoint;

			IList<IFeature> foundJunctions =
				NetworkFeatureFinder.FindJunctionFeaturesAt(edgeEndPoint);

			if (foundJunctions.Count == 0)
			{
				IList<IFeature> otherPolylineFeatures = NetworkFeatureFinder.FindEdgeFeaturesAt(
					edgeEndPoint,
					f =>
						! GdbObjectUtils.IsSameObject(f, forEdgeFeature,
						                              ObjectClassEquality.SameTableSameVersion));

				if (LinearNetworkEditUtils.SnapPoint(edgeEndPoint, otherPolylineFeatures,
				                                     NetworkFeatureFinder))
				{
					UpdateEdgeEndPoint(forEdgeFeature, edge, atEdgeFromPoint, edgeEndPoint);
				}

				IFeature createdJunction = CreateJunction(edgeEndPoint);

				if (createdJunction != null)
				{
					return createdJunction;
				}
			}
			else
			{
				EnsureSnapped(forEdgeFeature, edge, edgeEndPoint, atEdgeFromPoint, foundJunctions);
			}

			return null;
		}

		private static void EnsureSnapped([NotNull] IFeature edgeFeature,
		                                  [NotNull] IPolyline edge,
		                                  [NotNull] IPoint edgeEnd,
		                                  bool isFromEnd,
		                                  [NotNull] IList<IFeature> snapTargetJunctions)
		{
			IFeature closestJunction = GetClosestJunction2D(snapTargetJunctions, edgeEnd);

			Assert.NotNull(closestJunction);

			var snapTargetPoint = (IPoint) closestJunction.Shape;

			double xyHalfRes = GeometryUtils.GetXyResolution(edge) / 2;
			double zHalfRes = GeometryUtils.GetZResolution(edge) / 2;

			if (! GeometryUtils.IsSamePoint(snapTargetPoint, edgeEnd, xyHalfRes,
			                                zHalfRes))
			{
				UpdateEdgeEndPoint(edgeFeature, edge, isFromEnd, snapTargetPoint);
			}
		}

		private static void UpdateEdgeEndPoint(IFeature edgeFeature, IPolyline edge, bool atFromEnd,
		                                       IPoint newPoint)
		{
			if (atFromEnd)
			{
				edge.FromPoint = newPoint;
			}
			else
			{
				edge.ToPoint = newPoint;
			}

			GdbObjectUtils.SetFeatureShape(edgeFeature, edge);
			edgeFeature.Store();
		}

		private static IFeature GetClosestJunction2D(IList<IFeature> junctions, IPoint toPoint)
		{
			IFeature closestJunction = null;

			double minDist = double.MaxValue;

			foreach (IFeature junction in junctions)
			{
				double distance = GeometryUtils.GetPointDistance((IPoint) junction.Shape, toPoint);

				if (distance < minDist)
				{
					closestJunction = junction;
					minDist = distance;
				}
			}

			return closestJunction;
		}

		[CanBeNull]
		private IFeature CreateJunction([NotNull] IPoint point)
		{
			IFeature junctionFeature = null;
			if (JunctionFeatureFactory != null &&
			    NetworkDefinition.GetDefaultJunctionClass(out _) != null)
			{
				// TODO: This is a very obscure and often confusing feature that should probably be removed altogether:
				junctionFeature = JunctionFeatureFactory.CreateJunction(NetworkDefinition);
			}

			if (junctionFeature == null)
			{
				junctionFeature = CreateDefaultJunction(NetworkDefinition);
			}

			if (junctionFeature == null)
			{
				return null;
			}

			GdbObjectUtils.SetFeatureShape(junctionFeature, point);

			junctionFeature.Store();

			_msg.DebugFormat("Created standard junction feature {0} at {1}|{2}",
			                 GdbObjectUtils.ToString(junctionFeature), point.X, point.Y);

			NetworkFeatureFinder.TargetFeatureCandidates?.Add(junctionFeature);

			return junctionFeature;
		}

		[CanBeNull]
		private static IFeature CreateDefaultJunction([NotNull] LinearNetworkDef linearNetwork)
		{
			int? defaultSubtype;
			IFeatureClass defaultJunctionClass =
				linearNetwork.GetDefaultJunctionClass(out defaultSubtype);

			if (defaultJunctionClass == null)
			{
				return null;
			}

			IFeature junctionFeature =
				GdbObjectUtils.CreateFeature(defaultJunctionClass, defaultSubtype ?? -1);

			return junctionFeature;
		}

		private void TrySplitExistingEdgesByJunctions([NotNull] IEnumerable<IFeature> junctions)
		{
			Dictionary<IFeature, List<IFeature>> junctionsPerEdge =
				new Dictionary<IFeature, List<IFeature>>();

			foreach (IFeature junctionFeature in junctions)
			{
				if (! NetworkDefinition.IsSplittingJunction(junctionFeature))
				{
					continue;
				}

				IPoint junctionPoint = (IPoint) junctionFeature.Shape;

				foreach (IFeature edgeFeature in NetworkFeatureFinder.FindEdgeFeaturesAt(
					         junctionPoint))
				{
					if (! NetworkDefinition.IsSplittingEdge(edgeFeature))
					{
						continue;
					}

					IPolyline edge = (IPolyline) edgeFeature.ShapeCopy;

					if (GeometryUtils.InteriorIntersects(edge, junctionPoint))
					{
						CollectionUtils.AddToValueList(junctionsPerEdge, edgeFeature,
						                               junctionFeature);
					}
				}
			}

			foreach (var kvp in junctionsPerEdge)
			{
				IFeature edgeFeature = kvp.Key;
				List<IFeature> splittingJunctions = kvp.Value;

				SplitAtJunctions(edgeFeature, splittingJunctions);
			}
		}

		private void TrySplitExistingEdgesByEdgeEndpoint()
		{
			var splitPointsPerEdge = new Dictionary<IFeature, List<IPoint>>();

			// Collect split points for inserts
			foreach (IFeature edgeFeature in GetEdgeFeatures(_createdInOperation))
			{
				if (! NetworkDefinition.IsSplittingEdge(edgeFeature))
				{
					continue;
				}

				AddSplitPoint(edgeFeature, true, splitPointsPerEdge);
				AddSplitPoint(edgeFeature, false, splitPointsPerEdge);
			}

			// Add split points of updated edge end points
			foreach (IFeature edgeFeature in GetEdgeFeatures(_updatedInOperation.Keys))
			{
				if (! NetworkDefinition.IsSplittingEdge(edgeFeature))
				{
					continue;
				}

				IPolyline updatedEdge = (IPolyline) edgeFeature.Shape;

				if (EndPointChanged(edgeFeature, true))
				{
					AddSplitPoint(updatedEdge.FromPoint, splitPointsPerEdge);
				}

				if (EndPointChanged(edgeFeature, false))
				{
					AddSplitPoint(updatedEdge.ToPoint, splitPointsPerEdge);
				}
			}

			// Apply the splits to the target edges
			foreach (var kvp in splitPointsPerEdge)
			{
				IFeature edgeFeature = kvp.Key;
				List<IPoint> splittingPoints = kvp.Value;

				IMultipoint splitPoints = GeometryFactory.CreateMultipoint(splittingPoints);

				_msg.DebugFormat("Splitting {0} by using {1} split junction(s) {1}",
				                 GdbObjectUtils.ToString(edgeFeature), splittingPoints.Count);

				List<IFeature> newEdges =
					LinearNetworkEditUtils.SplitAtPoints(edgeFeature,
					                                     (IPointCollection) splitPoints);

				foreach (IFeature newEdge in newEdges)
				{
					NetworkFeatureFinder.TargetFeatureCandidates?.Add(newEdge);
				}
			}
		}

		private bool EndPointChanged(IFeature edgeFeature, bool atFromEnd)
		{
			IPolyline newShape = (IPolyline) edgeFeature.Shape;
			IPolyline oldShape = (IPolyline) _updatedInOperation[edgeFeature];

			IPoint newPoint = atFromEnd ? newShape.FromPoint : newShape.ToPoint;
			IPoint oldPoint = atFromEnd ? oldShape.FromPoint : oldShape.ToPoint;

			return ! GeometryUtils.AreEqualInXY(newPoint, oldPoint);
		}

		private void AddSplitPoint(
			[NotNull] IFeature splittingEdgeFeature,
			bool atFromPoint,
			[NotNull] Dictionary<IFeature, List<IPoint>> toResultSplitPointsPerEdge)
		{
			IPolyline edge = (IPolyline) splittingEdgeFeature.Shape;

			IPoint endPoint = atFromPoint ? edge.FromPoint : edge.ToPoint;

			AddSplitPoint(endPoint, toResultSplitPointsPerEdge);
		}

		private void AddSplitPoint(
			[NotNull] IPoint splitPoint,
			[NotNull] Dictionary<IFeature, List<IPoint>> toResultSplitPointsPerEdge)
		{
			foreach (IFeature otherEdgeFeature in NetworkFeatureFinder.FindEdgeFeaturesAt(
				         splitPoint))
			{
				IPolyline otherEdge = (IPolyline) otherEdgeFeature.Shape;

				if (GeometryUtils.InteriorIntersects(otherEdge, splitPoint))
				{
					CollectionUtils.AddToValueList(toResultSplitPointsPerEdge, otherEdgeFeature,
					                               splitPoint);
				}
			}
		}

		private void SplitAtJunctions([NotNull] IFeature edgeFeature,
		                              [NotNull] ICollection<IFeature> splittingJunctions)
		{
			_msg.DebugFormat("Splitting {0} by using split junction(s) {1}",
			                 GdbObjectUtils.ToString(edgeFeature),
			                 StringUtils.Concatenate(splittingJunctions,
			                                         j => GdbObjectUtils.ToString(j), ", "));

			var newEdges =
				LinearNetworkEditUtils.SplitAtJunctions(edgeFeature, splittingJunctions);

			foreach (IFeature newEdge in newEdges)
			{
				NetworkFeatureFinder.TargetFeatureCandidates?.Add(newEdge);
			}
		}

		private void DeleteOrphanedJunctions(IEnumerable<IFeature> edgeFeatures)
		{
			foreach (IFeature edgeFeature in edgeFeatures)
			{
				IPolyline polyline = edgeFeature.Shape as IPolyline;

				if (polyline != null && ! polyline.IsEmpty)
				{
					DeleteOrphanedJunctions(polyline.FromPoint);
					DeleteOrphanedJunctions(polyline.ToPoint);
				}
			}
		}

		private void DeleteOrphanedJunctions(IPoint searchPoint)
		{
			int edgeCount = NetworkFeatureFinder.FindEdgeFeaturesAt(searchPoint).Count;

			if (edgeCount == 0)
			{
				foreach (var junction in NetworkFeatureFinder.FindJunctionFeaturesAt(searchPoint))
				{
					junction.Delete();

					NetworkFeatureFinder.TargetFeatureCandidates?.Remove(junction);

					_msg.DebugFormat("Deleted orphan junction {0} found at {1}|{2}",
					                 GdbObjectUtils.ToString(junction), searchPoint.X,
					                 searchPoint.Y);
				}
			}
		}

		private IEnumerable<IFeature> GetEdgeFeatures([NotNull] IEnumerable<IFeature> fromList)
		{
			return fromList.Where(f => NetworkDefinition.IsEdgeFeature(f));
		}

		private IEnumerable<IFeature> GetJunctionFeatures([NotNull] IEnumerable<IFeature> fromList)
		{
			return fromList.Where(f => NetworkDefinition.IsJunctionFeature(f));
		}

		private bool HasEdits()
		{
			return _createdInOperation.Count > 0 ||
			       _updatedInOperation.Count > 0 ||
			       _deletedInOperation.Count > 0;
		}
	}
}
