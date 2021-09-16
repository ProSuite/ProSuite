using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	/// <summary>
	/// A subcurve that is typically created by the intersection of the reshaping geometry 
	/// with the geometry to be reshaped. The shortest possible line to reshape.
	/// </summary>
	public class CutSubcurve
	{
		// TODO: implement IDispose to release COM objects?

		#region Field declarations

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private IPath _path;

		private readonly bool _touchAtFromPoint;
		private readonly bool _touchAtToPoint;

		private readonly bool? _touchingDifferentParts;

		private readonly SubcurveNode _fromNode;
		private readonly SubcurveNode _toNode;

		private double? _lineAngleAtFrom;
		private double? _lineAngleAtTo;

		private bool? _canReshape;
		private bool? _isReshapeMemberCandidate;

		#endregion

		#region Constructors

		public CutSubcurve([NotNull] IPath path,
		                   bool touchAtFromPoint,
		                   bool touchAtToPoint,
		                   [CanBeNull] SubcurveNode fromNode = null,
		                   [CanBeNull] SubcurveNode toNode = null,
		                   bool? touchingDifferentParts = false)
		{
			_path = path;

			_touchAtToPoint = touchAtToPoint;
			_touchAtFromPoint = touchAtFromPoint;

			_fromNode = fromNode;
			_toNode = toNode;
			_touchingDifferentParts = touchingDifferentParts;
		}

		public CutSubcurve([NotNull] IPath path,
		                   bool canReshape,
		                   bool isCandidate,
		                   bool isFiltered,
		                   [CanBeNull] ISegment targetSegmentAtFrom,
		                   [CanBeNull] ISegment targetSegmentAtTo,
		                   [CanBeNull] IEnumerable<IPoint> extraTargetInsertPoints)
		{
			_path = path;
			_canReshape = canReshape;
			_isReshapeMemberCandidate = isCandidate;
			IsFiltered = isFiltered;

			if (targetSegmentAtFrom != null)
			{
				FromPointIsStitchPoint = true;
				TargetSegmentAtFromPoint = targetSegmentAtFrom;
			}

			if (targetSegmentAtTo != null)
			{
				ToPointIsStitchPoint = true;
				TargetSegmentAtToPoint = targetSegmentAtTo;
			}

			ExtraTargetInsertPoints = extraTargetInsertPoints?.ToList();
		}

		#endregion

		public bool CanReshape
		{
			get
			{
				if (_canReshape == null)
				{
					_canReshape = CanReshapeCore();
				}

				return _canReshape.Value;
			}
		}

		public bool IsReshapeMemberCandidate
		{
			get
			{
				if (IsFiltered)
				{
					return false;
				}

				if (_isReshapeMemberCandidate == null)
				{
					_isReshapeMemberCandidate = GetIsReshapeMemberCandidate();

					if (_msg.IsVerboseDebugEnabled)
					{
						if ((bool) _isReshapeMemberCandidate)
						{
							_msg.Debug("Assigning true");
						}

						_msg.VerboseDebugFormat(
							"Assigning {0} to IsReshapeMemberCandidate of {1}",
							_isReshapeMemberCandidate, this);
					}
				}

				return (bool) _isReshapeMemberCandidate;
			}
			private set
			{
				//Assert.True(_isReshapeMemberCandidate == null ||
				//            _isReshapeMemberCandidate == value,
				//            "Assigning conflicting value to IsReshapeMemberCandidate: {0} of {1}. Previously {2}",
				//            value, this, _isReshapeMemberCandidate);

				if (! (_isReshapeMemberCandidate == null ||
				       _isReshapeMemberCandidate == value))
				{
					_msg.DebugFormat(
						"Assigning conflicting value to IsReshapeMemberCandidate: {0} of {1}. Previously {2}",
						value, this, _isReshapeMemberCandidate);
				}

				_isReshapeMemberCandidate = value;
			}
		}

		public IPath Path
		{
			get { return _path; }
			protected set { _path = value; }
		}

		protected virtual IPoint FromPointOnTarget => Path.FromPoint;

		protected virtual IPoint ToPointOnTarget => Path.ToPoint;

		private bool TouchAtToPoint => _touchAtToPoint;

		private bool TouchAtFromPoint => _touchAtFromPoint;

		private SubcurveNode FromNode => _fromNode;

		public double LineAngleAtFrom
		{
			get
			{
				if (_lineAngleAtFrom == null)
				{
					_lineAngleAtFrom = GetLineAngle(this, _fromNode);
				}

				return (double) _lineAngleAtFrom;
			}
		}

		public double LineAngleAtTo
		{
			get
			{
				if (_lineAngleAtTo == null)
				{
					_lineAngleAtTo = GetLineAngle(this, _toNode);
				}

				return (double) _lineAngleAtTo;
			}
		}

		public SubcurveNode ToNode => _toNode;

		[CanBeNull]
		public List<IPoint> ExtraTargetInsertPoints { get; set; }

		public bool IsFiltered { get; set; }

		private bool Equals(CutSubcurve other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return
				Equals(other.Source, Source) &&
				other._touchAtFromPoint.Equals(_touchAtFromPoint) &&
				other._touchAtToPoint.Equals(_touchAtToPoint) &&
				(_path.Equals(other._path) || // path can be reference equal
				 ((IClone) _path).IsEqual((IClone) other._path)); // or IClone.IsEqual
		}

		private static bool Equals(IFeature source1, IFeature source2)
		{
			if (ReferenceEquals(source1, source2))
			{
				return true;
			}

			if (source1 == null || source2 == null)
			{
				return false;
			}

			return GdbObjectUtils.IsSameObject(source1, source2,
			                                   ObjectClassEquality.SameTableSameVersion);
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

			if (obj.GetType() != typeof(CutSubcurve))
			{
				return false;
			}

			return Equals((CutSubcurve) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = _path.GetHashCode();
				result = (result * 397) ^ _touchAtFromPoint.GetHashCode();
				result = (result * 397) ^ _touchAtToPoint.GetHashCode();
				return result;
			}
		}

		public override string ToString()
		{
			string pathInfo;
			if (_path == null)
			{
				pathInfo = "<path is null>";
			}
			else if (_path.IsEmpty)
			{
				pathInfo = "<path is empty>";
			}
			else
			{
				pathInfo = string.Format("path from {0}|{1} to {2}|{3} with length {4}",
				                         _path.FromPoint.X, _path.FromPoint.Y,
				                         _path.ToPoint.X, _path.ToPoint.Y, _path.Length);
			}

			return string.Format("CutSubcurve with {0}", pathInfo);
		}

		public IEnumerable<IPoint> GetPotentialTargetInsertPoints()
		{
			yield return FromPointOnTarget;
			yield return ToPointOnTarget;

			if (ExtraTargetInsertPoints != null)
			{
				foreach (IPoint point in ExtraTargetInsertPoints)
				{
					yield return point;
				}
			}
		}

		public bool TryJoinNonForkingNeighbourCandidates(
			[NotNull] CutSubcurve subcurve,
			[NotNull] List<CutSubcurve> replacedSubcurves,
			out CutSubcurve result)
		{
			if (! subcurve.IsReshapeMemberCandidate ||
			    subcurve.IsFiltered)
			{
				result = null;
				return false;
			}

			result = subcurve;

			if (! result.TouchAtFromPoint)
			{
				TryMergeWithSingleAdjacentCandidate(ref result, subcurve.FromNode,
				                                    replacedSubcurves);
			}

			if (! result.TouchAtToPoint)
			{
				TryMergeWithSingleAdjacentCandidate(ref result, subcurve.ToNode,
				                                    replacedSubcurves);
			}

			return result != subcurve;
		}

		/// <summary>
		/// Whether or not the to-point of the subcurve is a stitch point, i.e. an intersection point 
		/// with no vertex in the target.
		/// <seealso cref="FromPointIsStitchPoint"/>
		/// </summary>
		public bool ToPointIsStitchPoint { get; set; }

		/// <summary>
		/// Whether or not the from-point of the subcurve is a stitch point, i.e. an intersection point 
		/// with no vertex in the target. If yes it can/should be removed in case several adjacent subcurves
		/// are merged together. The removal of this point before reshaping is necessary to avoid inserting 
		/// artificial vertices into the reshape source.
		/// </summary>
		public bool FromPointIsStitchPoint { get; set; }

		/// <summary>
		/// The segment in the target at the location of the from-point in case FromPointIsStitchPoint equals true.
		/// This can be used to easily and exactly reconstruct the course of the target in the reshape line. 
		/// </summary>
		[CanBeNull]
		public ISegment TargetSegmentAtFromPoint { get; set; }

		/// <summary>
		/// The segment in the target at the location of the to-point in case ToPointIsStitchPoint equals true.
		/// This can be used to easily and exactly reconstruct the course of the target in the reshape line. 
		/// </summary>
		[CanBeNull]
		public ISegment TargetSegmentAtToPoint { get; set; }

		/// <summary>
		/// Link to the source feature this subcurve applies to. This can be used in the case where multiple 
		/// sources are treated individually to identify for which feature a specific reshape curve is appliccable.
		/// </summary>
		[CanBeNull]
		public GdbObjectReference? Source { get; set; }

		#region Private and protected members

		protected virtual bool CanReshapeCore()
		{
			if (TouchAtFromPoint && TouchAtToPoint)
			{
				if (_touchingDifferentParts == null)
				{
					return true;
				}

				if (! (bool) _touchingDifferentParts)
				{
					return true;
				}
			}

			return false;
		}

		[NotNull]
		private SubcurveNode OtherNode([NotNull] SubcurveNode thisNode)
		{
			if (thisNode == _toNode)
			{
				return _fromNode;
			}

			Assert.AreEqual(thisNode, _fromNode,
			                "thisNode does neither reference to- nor from-node.");
			return _toNode;
		}

		private static double GetLineAngle([NotNull] CutSubcurve subcurve,
		                                   SubcurveNode atNode)
		{
			var segments = (ISegmentCollection) subcurve.Path;

			ISegment segment;

			var reverseOrientation = false;

			if (atNode == subcurve.ToNode)
			{
				// use last segment and the line needs to be inverted
				segment = segments.Segment[segments.SegmentCount - 1];

				reverseOrientation = true;
			}
			else
			{
				segment = segments.Segment[0];
			}

			var line = segment as ILine;

			if (line == null)
			{
				line = new LineClass();
				segment.QueryTangent(esriSegmentExtension.esriNoExtension, 1, true, 10,
				                     line);
			}

			double angle = line.Angle;

			if (reverseOrientation)
			{
				angle = angle >= Math.PI
					        ? angle - Math.PI
					        : angle + Math.PI;
			}

			return angle;
		}

		private bool GetIsReshapeMemberCandidate()
		{
			if (CanReshape)
			{
				return false;
			}

			// exclude the ones that directly connect different parts - cannot be usedd
			if (TouchAtToPoint && TouchAtFromPoint &&
			    _touchingDifferentParts != null && (bool) _touchingDifferentParts)
			{
				return false;
			}

			_msg.VerboseDebug("GetIsReshapeMemberCandidate: Trying to connect.");

			// traverse graph by first starting with FromNode to try to connect to the line to be reshaped
			if (TryCanConnect(FromNode, cutSubcurve => ! cutSubcurve.IsFiltered))
			{
				return true;
			}

			// try again starting with to-node in case the from-node check has used some 
			// essential nodes from to-node check:
			return TryCanConnect(ToNode, cutSubcurve => ! cutSubcurve.IsFiltered);
		}

		private bool TryCanConnect(SubcurveNode firstNode,
		                           Predicate<CutSubcurve> connectCondition)
		{
			// traverse graph using this node without going through other node
			SubcurveNode otherNode = OtherNode(firstNode);

			IList<SubcurveNode> checkedNodes = new List<SubcurveNode> {otherNode};
			IList<CutSubcurve> intermediateCurves = new List<CutSubcurve>();

			if (IsConnected(this, firstNode, checkedNodes, intermediateCurves,
			                connectCondition))
			{
				// open up the other node
				checkedNodes.Remove(otherNode);

				// now go through other node
				if (IsConnected(this, otherNode, checkedNodes, intermediateCurves,
				                connectCondition))
				{
					// for performance reasons notify all the used nodes that
					// they are also connected
					AssignCandidateStatus(intermediateCurves);
					return true;
				}
			}

			return false;
		}

		private static void AssignCandidateStatus(IEnumerable<CutSubcurve> toCurves)
		{
			foreach (CutSubcurve subcurve in toCurves)
			{
				if (! subcurve.CanReshape)
				{
					subcurve.IsReshapeMemberCandidate = true;
				}
			}
		}

		private static bool IsConnected(CutSubcurve subcurve, SubcurveNode atNode,
		                                ICollection<SubcurveNode> checkedNodes,
		                                ICollection<CutSubcurve> intermediateCurves,
		                                Predicate<CutSubcurve> connectCondition)
		{
			// TODO: cache the thisSubcurve on the subcurve to improve performance
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Checking if connected at {0}/{1}", atNode.X, atNode.Y);
			}

			if (connectCondition != null && ! connectCondition(subcurve))
			{
				return false;
			}

			if (atNode == subcurve.FromNode && subcurve.TouchAtFromPoint ||
			    atNode == subcurve.ToNode && subcurve.TouchAtToPoint)
			{
				checkedNodes.Add(atNode);
				intermediateCurves.Add(subcurve);

				return true;
			}

			if (checkedNodes.Contains(atNode))
			{
				return false;
			}

			// block the node to avoid circles 
			checkedNodes.Add(atNode);

			// NOTE: sometimes reshape candidates are not found because the node traversal cuts off the other
			//	     node's path (and vice versa when the second starts first):
			// TODO: always try the left-most / or for the other node the right-most subcurve (invert in second run)
			//       to avoid cutting the other node's path -> this is not the perfect solution either (debug)
			// TODO: minimal but robust solution: make sure both ends are connected regardless of the other's path (only exclude other node)

			//IEnumerable<CutSubcurve> leaves = traverseRightCurvesFirst
			//                                    ? atNode.ConnectedSubcurvesFromRightToLeft(subcurve)
			//                                    : atNode.ConnectedSubcurvesFromLeftToRight(subcurve);

			foreach (CutSubcurve connectedSubcurve in atNode.ConnectedSubcurves)
			{
				if (connectedSubcurve != subcurve &&
				    IsConnected(connectedSubcurve, connectedSubcurve.OtherNode(atNode),
				                checkedNodes,
				                intermediateCurves, connectCondition))
				{
					intermediateCurves.Add(subcurve);

					return true;
				}
			}

			return false;
		}

		private static void TryMergeWithSingleAdjacentCandidate(
			ref CutSubcurve thisSubcurve, SubcurveNode inNode,
			List<CutSubcurve> replacedSubcurves)
		{
			CutSubcurve singleConnectedCandidataCurve = null;

			foreach (CutSubcurve adjacentSubcurve in inNode.ConnectedSubcurves)
			{
				if (adjacentSubcurve == thisSubcurve)
				{
					continue;
				}

				if (! adjacentSubcurve.IsReshapeMemberCandidate ||
				    adjacentSubcurve.IsFiltered)
				{
					// already green or red or grey:
					continue;
				}

				if (replacedSubcurves.Contains(adjacentSubcurve))
				{
					continue;
				}

				if (TouchesSourceInNode(adjacentSubcurve, inNode))
				{
					// do not merge across source lines
					continue;
				}

				if (singleConnectedCandidataCurve == null)
				{
					singleConnectedCandidataCurve = adjacentSubcurve;
				}
				else
				{
					// more than one - cannot merge
					return;
				}
			}

			if (singleConnectedCandidataCurve == null)
			{
				return;
			}

			// Add to replaced curves before assigning the final thisSubcurve through merging the two
			replacedSubcurves.Add(thisSubcurve);
			replacedSubcurves.Add(singleConnectedCandidataCurve);

			thisSubcurve = Replace(thisSubcurve, singleConnectedCandidataCurve, inNode);
		}

		private void AddExtraPotentialTargetInsertPoint(IPoint point)
		{
			if (ExtraTargetInsertPoints == null)
			{
				ExtraTargetInsertPoints = new List<IPoint>();
			}

			ExtraTargetInsertPoints.Add(point);
		}

		private static bool TouchesSourceInNode(CutSubcurve subcurve, SubcurveNode inNode)
		{
			if (subcurve.FromNode.Equals(inNode))
			{
				return subcurve.TouchAtFromPoint;
			}

			if (subcurve.ToNode.Equals(inNode))
			{
				return subcurve.TouchAtToPoint;
			}

			// Assert.CantReach() here?
			return false;
		}

		private static CutSubcurve Replace(CutSubcurve cutSubcurve1,
		                                   CutSubcurve cutSubcurve2,
		                                   SubcurveNode mergeNode)
		{
			IGeometryCollection mergedCollection =
				ReshapeUtils.GetSimplifiedReshapeCurves(
					new List<CutSubcurve> {cutSubcurve1, cutSubcurve2});

			Assert.AreEqual(1, mergedCollection.GeometryCount,
			                "Unexpected number of geometries after merging adjacent subcurves");

			var newPath = (IPath) mergedCollection.get_Geometry(0);

			bool touchAtFrom;
			SubcurveNode oldNodeAtFrom = GetNodeAt(newPath.FromPoint, cutSubcurve1,
			                                       cutSubcurve2,
			                                       out touchAtFrom);

			bool touchAtTo;
			SubcurveNode oldNodeAtTo = GetNodeAt(newPath.ToPoint, cutSubcurve1,
			                                     cutSubcurve2,
			                                     out touchAtTo);

			if (oldNodeAtFrom.ConnectedSubcurves.Contains(cutSubcurve1))
			{
				oldNodeAtFrom.ConnectedSubcurves.Remove(cutSubcurve1);
			}

			if (oldNodeAtFrom.ConnectedSubcurves.Contains(cutSubcurve2))
			{
				oldNodeAtFrom.ConnectedSubcurves.Remove(cutSubcurve2);
			}

			if (oldNodeAtTo.ConnectedSubcurves.Contains(cutSubcurve1))
			{
				oldNodeAtTo.ConnectedSubcurves.Remove(cutSubcurve1);
			}

			if (oldNodeAtTo.ConnectedSubcurves.Contains(cutSubcurve2))
			{
				oldNodeAtTo.ConnectedSubcurves.Remove(cutSubcurve2);
			}

			var result = new CutSubcurve(newPath, touchAtFrom, touchAtTo, oldNodeAtFrom,
			                             oldNodeAtTo);

			result.Source = cutSubcurve1.Source;

			oldNodeAtFrom.ConnectedSubcurves.Add(result);
			oldNodeAtTo.ConnectedSubcurves.Add(result);

			// Old target intersection points: Add them if they were not stitch points removed after simplify:
			IPoint connectPoint = GeometryFactory.CreatePoint(mergeNode.X, mergeNode.Y,
			                                                  newPath.SpatialReference);

			int? connectIndex = GeometryUtils.FindHitVertexIndex(
				newPath, connectPoint, GeometryUtils.GetXyTolerance(newPath), out int _);

			if (connectIndex != null)
			{
				result.AddExtraPotentialTargetInsertPoint(
					((IPointCollection) newPath).get_Point((int) connectIndex));
			}

			return result;
		}

		private static SubcurveNode GetNodeAt(IPoint checkPoint, CutSubcurve cutSubcurve1,
		                                      CutSubcurve cutSubcurve2,
		                                      out bool isTouchingSource)
		{
			SubcurveNode result;

			if (GeometryUtils.AreEqualInXY(checkPoint, cutSubcurve1.Path.FromPoint))
			{
				result = cutSubcurve1.FromNode;
				isTouchingSource = cutSubcurve1.TouchAtFromPoint;
			}
			else if (GeometryUtils.AreEqualInXY(checkPoint, cutSubcurve1.Path.ToPoint))
			{
				result = cutSubcurve1.ToNode;
				isTouchingSource = cutSubcurve1.TouchAtToPoint;
			}
			else if (GeometryUtils.AreEqualInXY(checkPoint, cutSubcurve2.Path.FromPoint))
			{
				result = cutSubcurve2.FromNode;
				isTouchingSource = cutSubcurve2.TouchAtFromPoint;
			}
			else
			{
				Assert.True(
					GeometryUtils.AreEqualInXY(checkPoint, cutSubcurve2.Path.ToPoint),
					"Unexpected situation after merging adjacent subcurves.");

				result = cutSubcurve2.ToNode;
				isTouchingSource = cutSubcurve2.TouchAtToPoint;
			}

			return result;
		}

		#endregion
	}
}
