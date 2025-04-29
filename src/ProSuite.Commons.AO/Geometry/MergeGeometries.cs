using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry
{
	/// <summary>
	/// The class holding methods to merge two geometries
	/// modified by two different user on either side of the cutting
	/// perimeter.
	/// </summary>
	public static class MergeGeometries
	{
		#region Nested class LineFragment

		/// <summary>
		/// Stores information about one line fragment generated
		/// by processing the original line states with the perimeter.
		/// </summary>
		private class LineFragment
		{
			#region Fields

			private readonly IPointCollection _pointColl;
			private readonly int _nrPoints;

			private int _connPoints;

			//private int _firstSourcePointNr = -1;
			private bool _hasEndPoint;
			private double _startDistanceAlongSourceLine;

			// ?! ConnectPoints need to be added?
			// Normally the connectPoints (start/end point) of a fragment
			// will not be used to create the result line. But it could
			// happen that one of this point was already present in the original
			// line state, not created by cutting the line with the perimeter.
			// Such points need to be added to the new geometry. Perhaps the
			// check if we have such points in this fragment can be done
			// here already. At the moment, the "AddPointsToCollection" and
			// "PointExistsInLine" methods handles such situations.

			#endregion

			#region Properties

			/// <summary>
			/// Returns the fragment points
			/// </summary>
			public IPointCollection Points => _pointColl;

			///// <summary>
			///// Returns the count of points for this fragment
			///// </summary>
			//public int NrPoints
			//{
			//    get { return _nrPoints; }
			//}

			/// <summary>
			/// Returns the count of points, that need to get connected
			/// with other parts (real start and endpoints need no more connection)
			/// </summary>
			public int ConnectPoints => _connPoints;

			///// <summary>
			///// Returns the fragment as a polyline
			///// </summary>
			//public IPolyline Polyline
			//{
			//    get
			//    {
			//        IPolyline line = new PolylineClass();
			//        for (int pntIdx = 0, size = _pointColl.PointCount;
			//             pntIdx < size;
			//             pntIdx++)
			//        {
			//            ((IPointCollection) line).AddPoint(
			//                _pointColl.get_Point(pntIdx),
			//                ref _missingRef, ref _missingRef);
			//        }
			//        return line;
			//    }
			//}

			/// <summary>
			/// Returns the flag that identifies a part with the endpoint
			/// from the original line
			/// </summary>
			public bool HoldsEndPoint
			{
				get { return _hasEndPoint; }
				set { _hasEndPoint = value; }
			}

			///// <summary>
			///// Member used to reorder the fragments in the LinePartsCache
			///// </summary>
			//public int FirstOrderPointNr
			//{
			//    get { return _firstSourcePointNr; }
			//    set { _firstSourcePointNr = value; }
			//}

			public double StartDistanceAlongSourceLine
			{
				get { return _startDistanceAlongSourceLine; }
				set { _startDistanceAlongSourceLine = value; }
			}

			#endregion

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="pointCollection"></param>
			/// <param name="perimeter"></param>
			public LineFragment([NotNull] IPointCollection pointCollection,
			                    [NotNull] IPolygon perimeter)
			{
				_pointColl = pointCollection;
				_nrPoints = _pointColl.PointCount;
				EvaluateConnectionFlag(perimeter);
			}

			/// <summary>
			/// Sets the flag, if the fragment belongs to one of the parts that
			/// we need to use while connecting the fragments.
			/// </summary>
			private void EvaluateConnectionFlag([NotNull] IPolygon perimeter)
			{
				// ?! Connecting-Fragment ?
				// The start or end point of the fragment must
				// hit the border of the perimeter if the fragment needs
				// to be included.
				// If both points are not placed on the border, the fragment
				// could be a multipart element of the lineState (if this is allowed).
				// If the fragment is a original multipart fragment, how can
				// we know when we had to add it to the new geometrie.
				// -> If we have a multipart fragment, there must be two
				// fragments that have one of the endpoints matching one of
				// the endpoints of the multipart fragment. So we will need to check that
				// when we add a fragment to the new geometry.
				_connPoints = 0;
				if (PointLiesOnPerimeterBorder(_pointColl.Point[0], perimeter))
				{
					_connPoints++;
				}

				if (PointLiesOnPerimeterBorder(
					_pointColl.Point[_nrPoints - 1], perimeter))
				{
					_connPoints++;
				}
			}

			/// <summary>
			/// Checks if a given point lies on the border of a given polygon
			/// </summary>
			/// <param name="point">Point to check</param>
			/// <param name="perimeter">Perimeter that defines the border</param>
			/// <returns>TRUE if the point lies on the border, FALSE otherwise</returns>
			private static bool PointLiesOnPerimeterBorder([NotNull] IPoint point,
			                                               [NotNull] IPolygon perimeter)
			{
				// TODO
				// ?! Check point on perimeter border
				// We need to know, if the point lies on the border of the
				// given perimeter. How can we do that?
				// The perimeter can be a free choosen polygon (does not do be a rectangle)
				// Perhaps we need to loop through all perimeter segements and check the point
				// distance to the line -> ICurve.QueryPointAndDistance could be helpfull...
				IPoint retVal = new PointClass();
				var rightSide = false;
				double distAlongCurve = 0, distFromCurve = 0;
				perimeter.QueryPointAndDistance(
					esriSegmentExtension.esriNoExtension, point, false, retVal,
					ref distAlongCurve, ref distFromCurve, ref rightSide);
				return ((IRelationalOperator) point).Equals(retVal);
			}

			/// <summary>
			/// Calculates the distance of the fragments start- (useStartPoint=TRUE)
			/// or endpoint (useStartPoint=FALSE) with the given point.
			/// </summary>
			/// <param name="point">Point for calculating the distance</param>
			/// <param name="useStartPoint">Flag if the FromPoint (TRUE) or the ToPoint
			///                             (FALSE) should be used to calculate the distance</param>
			/// <returns>The distance of the two points</returns>
			public double GetPointDistance([NotNull] IPoint point, bool useStartPoint)
			{
				ILine tmpLine = GetLine(point, useStartPoint);
				return tmpLine.Length;
			}

			/// <summary>
			/// Constructs a line from the fragments start- (useStartPoint=TRUE)
			/// or endpoint (useStartPoint=FALSE) to the specified point.
			/// </summary>
			/// <param name="toPoint">Point for constructing the line</param>
			/// <param name="useStartPoint">Flag if the FromPoint (TRUE) or the ToPoint
			///                             (FALSE) should be used to construct the line.</param>
			/// <returns>The line between of the two points</returns>
			[NotNull]
			public ILine GetLine([NotNull] IPoint toPoint, bool useStartPoint)
			{
				ILine result = new LineClass();
				result.FromPoint = toPoint;
				result.ToPoint = useStartPoint
					                 ? _pointColl.Point[0]
					                 : _pointColl.Point[_nrPoints - 1];
				return result;
			}

			/// <summary>
			/// Checks if a given point exists (same coordinates) in
			/// the stored fragment
			/// </summary>
			/// <param name="point">Point to check</param>
			/// <returns>TRUE if the point is found, FALSE otherwise</returns>
			public bool PointExistsOnFragment([NotNull] IPoint point)
			{
				IPoint hitPoint = new PointClass();
				return GeometryUtils.HitTestWksPointZs((IGeometry) _pointColl, point,
				                                       GeometryUtils.GetXyTolerance(point),
				                                       0, true, hitPoint);
				//IRelationalOperator relOp = (IRelationalOperator) point;
				//for (int pntIdx = 0, size = _pointColl.PointCount;
				//     pntIdx < size;
				//     pntIdx++)
				//{
				//    if (relOp.Equals(_pointColl.get_Point(pntIdx)))
				//    {
				//        return true;
				//    }
				//}
				//return false;
			}
		}

		#endregion

		#region Nested class LineFragmentsCache

		/// <summary>
		/// Helper class to store the parts of a line
		/// and flags if the parts are used already
		/// </summary>
		private class LineFragmentsCache
		{
			#region Fields

			// List with PointCollections
			private List<LineFragment> _fragments = new List<LineFragment>();

			// List with bool flag (TRUE if a part is used)
			private List<bool> _usedFragments = new List<bool>();

			// Count of parts
			private int _fragmentCount;

			#endregion

			#region Properties

			/// <summary>
			/// Returns the list with the parts (IPointCollections)
			/// </summary>
			public List<LineFragment> Fragments => _fragments;

			///// <summary>
			///// Returns the list with the used flags (bool)
			///// </summary>
			//public List<bool> UsedFlags
			//{
			//    get { return _usedFragments; }
			//}

			/// <summary>
			/// Returns the count of the unused parts
			/// </summary>
			public int NrUnusedParts
			{
				get
				{
					var count = 0;
					int size = _usedFragments.Count;

					for (var i = 0; i < size; i++)
					{
						if (! _usedFragments[i])
						{
							count++;
						}
					}

					return count;
				}
			}

			#endregion

			/// <summary>
			/// Constructor that initializes the lists with the geometry
			/// information of the given lineParts
			/// </summary>
			/// <param name="lineParts">The line parts, i.e. the intersection or difference with the perimeter.</param>
			/// <param name="perimeter">The perimeter.</param>
			/// <param name="sourceLine">The (original) source line.</param>
			public LineFragmentsCache([NotNull] IPolyline lineParts,
			                          [NotNull] IPolygon perimeter,
			                          [NotNull] IPolyline sourceLine)
			{
				GetPartsFromPolyline(lineParts, perimeter, sourceLine);
			}

			#region Public methods

			/// <summary>
			/// Gets the parts of a polyline and stores them into
			/// the member lists
			/// </summary>
			/// <param name="line">The line.</param>
			/// <param name="perimeter">The perimeter.</param>
			/// <param name="sourceLine">The source line.</param>
			private void GetPartsFromPolyline([NotNull] IPolyline line,
			                                  [NotNull] IPolygon perimeter,
			                                  [NotNull] IPolyline sourceLine)
			{
				// Reset members
				_fragments.Clear();
				_usedFragments.Clear();
				_fragmentCount = 0;

				// Get parts from polyline
				var geoColl = line as IGeometryCollection;
				if (geoColl != null)
				{
					_fragmentCount = geoColl.GeometryCount;

					for (var geoIdx = 0; geoIdx < _fragmentCount; geoIdx++)
					{
						_fragments.Add(new LineFragment(
							               (IPointCollection) geoColl.Geometry[geoIdx],
							               perimeter));

						_usedFragments.Add(false);
					}
				}

				ReorderFragments(sourceLine);

				// Setting the endPoint holding flag
				// Because we reordered the parts, only the last part must be checked
				// if he holds the endPoint
				_fragments[_fragmentCount - 1].HoldsEndPoint =
					_fragments[_fragmentCount - 1].PointExistsOnFragment(sourceLine.ToPoint);
			}

			/// <summary>
			/// Returns the part in the list with the given index.
			/// The used flag will not be changed.
			/// </summary>
			/// <param name="partNr"></param>
			/// <returns>NULL if index does not match list bounds</returns>
			[CanBeNull]
			private LineFragment GetPartByNr(int partNr)
			{
				if (partNr < _fragmentCount && partNr >= 0)
				{
					return _fragments[partNr];
				}

				return null;
			}

			/// <summary>
			/// Gets the part from the given nummer of the list
			/// and flags it as used.
			/// </summary>
			/// <param name="partNr"></param>
			/// <returns></returns>
			[CanBeNull]
			private LineFragment UsePartByNr(int partNr)
			{
				LineFragment fragment = GetPartByNr(partNr);

				if (fragment != null)
				{
					_usedFragments[partNr] = true;
				}

				return fragment;
			}

			///// <summary>
			///// Returns the used flag of the part with the given index
			///// of the part list.
			///// </summary>
			///// <param name="partNr"></param>
			///// <returns></returns>
			//public bool GetPartUsedFlagByNr(int partNr)
			//{
			//    if (partNr < _fragmentCount && partNr >= 0)
			//    {
			//        return _usedFragments[partNr];
			//    }

			//    return true;
			//}

			/// <summary>
			/// Gets the next unused part of the list.
			/// </summary>
			/// <returns>NULL if all parts are used</returns>
			[CanBeNull]
			public LineFragment GetNextUnusedPart()
			{
				int size = _usedFragments.Count;

				for (var idx = 0; idx < size; idx++)
				{
					if (! _usedFragments[idx])
					{
						return GetPartByNr(idx);
					}
				}

				return null;
			}

			/// <summary>
			/// Gets the next unused part of the list and flags
			/// it as used.
			/// </summary>
			/// <returns></returns>
			[CanBeNull]
			public LineFragment UseNextUnusedPart()
			{
				int size = _usedFragments.Count;

				for (var idx = 0; idx < size; idx++)
				{
					if (! _usedFragments[idx])
					{
						return UsePartByNr(idx);
					}
				}

				return null;
			}

			/// <summary>
			/// Checks if there are still unused parts
			/// </summary>
			/// <returns></returns>
			public bool HasMoreUnusedParts()
			{
				int size = _usedFragments.Count;
				for (var idx = 0; idx < size; idx++)
				{
					if (! _usedFragments[idx])
					{
						return true;
					}
				}

				return false;
			}

			/// <summary>
			/// Gets the count of unused connect points of all fragments
			/// </summary>
			/// <returns></returns>
			public int GetNrUnusedConnectPoints()
			{
				var conPoints = 0;
				int size = _usedFragments.Count;

				for (var idx = 0; idx < size; idx++)
				{
					if (! _usedFragments[idx])
					{
						conPoints += _fragments[idx].ConnectPoints;
					}
				}

				return conPoints;
			}

			/// <summary>
			/// Re-orders the parts by the given line and reverses the orientation
			/// if necessary to conform with the orginial source line.
			/// </summary>
			/// <param name="sourceLine">The source line.</param>
			private void ReorderFragments([NotNull] IPolyline sourceLine)
			{
				EnsureOrientationAndSetDistanceAlong(sourceLine);

				// ReOrder the Array - TODO: use sorted dictionary
				var newPartArray = new List<LineFragment>();
				var newFlagArray = new List<bool>();
				while (_fragments.Count > 0)
				{
					var selFragment = 0;
					double smallestNr = _fragments[0].StartDistanceAlongSourceLine;
					int size = _fragments.Count;

					for (var partIdx = 1; partIdx < size; partIdx++)
					{
						if (smallestNr < _fragments[partIdx].StartDistanceAlongSourceLine)
						{
							continue;
						}

						smallestNr = _fragments[partIdx].StartDistanceAlongSourceLine;
						selFragment = partIdx;
					}

					newPartArray.Add(_fragments[selFragment]);
					newFlagArray.Add(_usedFragments[selFragment]);
					_fragments.RemoveAt(selFragment);
					_usedFragments.RemoveAt(selFragment);
				}

				_fragments = newPartArray;
				_usedFragments = newFlagArray;
			}

			private void EnsureOrientationAndSetDistanceAlong([NotNull] IPolyline sourceLine)
			{
				int size = _fragments.Count;

				for (var partIdx = 0; partIdx < size; partIdx++)
				{
					LineFragment fragment = _fragments[partIdx];

					var fragmentCurve = (ICurve) fragment.Points;
					IPoint fromPoint = GeometryFactory.Clone(fragmentCurve.FromPoint);
					IPoint toPoint = GeometryFactory.Clone(fragmentCurve.ToPoint);

					// Ensure orientation
					if (sourceLine.IsClosed)
					{
						if (GeometryUtils.AreEqual(sourceLine.FromPoint,
						                           fragmentCurve.FromPoint))
						{
							// special case: distance along also returns 0 for the tail
							// -> use the mid-point instead of the coincident start/end point.
							fragmentCurve.QueryPoint(
								esriSegmentExtension.esriNoExtension, 0.5,
								true, fromPoint);
						}
						else if (GeometryUtils.AreEqual(sourceLine.FromPoint,
						                                fragmentCurve.ToPoint))
						{
							fragmentCurve.QueryPoint(
								esriSegmentExtension.esriNoExtension, 0.5,
								true, toPoint);
						}
					}

					double distanceToFromPoint = GetDistanceAlong(sourceLine, fromPoint);
					double distanceToToPoint = GetDistanceAlong(sourceLine, toPoint);

					if (distanceToFromPoint > distanceToToPoint)
					{
						// The line was flipped either by ITopologicalOperator or the postprocessing split
						fragmentCurve.ReverseOrientation();
					}

					fragment.StartDistanceAlongSourceLine = distanceToFromPoint;

					//// ORIGINAL VERSION (Kills performance!)
					//// Why is FirstOrderPointNr overwritten

					//for (int pntIdx = 0, size2 = pointColl.PointCount;
					//     pntIdx < size2;
					//     pntIdx++)
					//{
					//    if (fragment.PointExistsOnFragment(
					//        pointColl.get_Point(pntIdx)))
					//    {
					//        fragment.FirstOrderPointNr = pntIdx;
					//    }
					//}
					//}
				}
			}

			private static double GetDistanceAlong([NotNull] IPolyline line,
			                                       [NotNull] IPoint atPointOnLine)
			{
				double distanceAlong = 0;

				var outPoint = (IPoint) ((IClone) atPointOnLine).Clone();
				double distanceFrom = 0;
				var righSide = false;
				var sourceCurve = (ICurve3) line;
				sourceCurve.QueryPointAndDistance(esriSegmentExtension.esriNoExtension,
				                                  atPointOnLine, false, outPoint,
				                                  ref distanceAlong,
				                                  ref distanceFrom, ref righSide);

				Assert.True(GeometryUtils.AreEqualInXY(atPointOnLine, outPoint),
				            "The provided point is not on the line.");

				return distanceAlong;
			}

			#endregion
		}

		#endregion

		private enum LinesFlippedState
		{
			SameDirection,
			InversedDirection,
			Unsure
		}

		#region Fields

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static object _missingRef = Type.Missing;

		#endregion

		#region Public methods

		/// <summary>
		/// Merges two polygon or polyline geometries, if possible, based on the comparison of changes.
		/// Both version's edits are maintained in the merge solution.
		/// </summary>
		/// <param name="polygonOrPolyline1">First polygon or polyline geometry</param>
		/// <param name="polygonOrPolyline2">Second geometry of the same type</param>
		/// <param name="commonAncestorGeometry">The common ancestor geometry - before any changes were made</param>
		/// <returns>The merged states or null if the merge could not be performed 
		/// (normally due to overlapping edits to the same vertices).</returns>
		[CanBeNull]
		public static IGeometry MergeEditedVertexes(
			[NotNull] IGeometry polygonOrPolyline1,
			[NotNull] IGeometry polygonOrPolyline2,
			[NotNull] IGeometry commonAncestorGeometry)
		{
			Assert.ArgumentNotNull(polygonOrPolyline1, nameof(polygonOrPolyline1));
			Assert.ArgumentNotNull(polygonOrPolyline2, nameof(polygonOrPolyline2));
			Assert.ArgumentNotNull(commonAncestorGeometry, nameof(commonAncestorGeometry));

			Assert.ArgumentCondition(
				polygonOrPolyline1.GeometryType == esriGeometryType.esriGeometryPolygon ||
				polygonOrPolyline1.GeometryType == esriGeometryType.esriGeometryPolyline,
				"Geometry is not of type Polygon or Polyline");

			Assert.ArgumentCondition(
				polygonOrPolyline1.GeometryType == polygonOrPolyline2.GeometryType &&
				polygonOrPolyline1.GeometryType == commonAncestorGeometry.GeometryType,
				"Geometries are not of the same type");

			IGeometry mergedGeometry;
			try
			{
				// NOTE: the process must be STA otherwise the cast will fail even if instantiated with the activator
				//       Resharper testrunner must be forced to use STA with App.config
				Type geometryEnvType = Type.GetTypeFromProgID("esriGeometry.GeometryEnvironment");
				var constructMerge = (IConstructMerge) Activator.CreateInstance(geometryEnvType);

				// TODO: Write own MergeGeometries logic (that merges the mergable parts and does not give up if 
				// there are any overlapping edits.

				// This method does crazy things to Z values:
				// - vertices are inserted at the intersections with the common ancestor
				// - *incorrect z values* are assigned to these vertices - apparently the
				//   common ancestor z values in case of changes to geometry2, and some 
				//   value between common ancestor z and geometry 1 z in case of geometry 1)
				// - If one of the geometries was moved to a different (disjoint) location, 
				//   the result is not z-aware, even if all the inputs were (TOP-5183).
				mergedGeometry = constructMerge.MergeGeometries(
					commonAncestorGeometry, polygonOrPolyline2, polygonOrPolyline1);

				FixIncorrectVertices(mergedGeometry, polygonOrPolyline1,
				                     polygonOrPolyline2, commonAncestorGeometry);
			}
			catch (COMException comEx)
			{
				// NOTE: even with (9.3.1 SP2) the geometry error alone is not enough - cannot remove the fdoErrors
				if (comEx.ErrorCode ==
				    (int) fdoError.FDO_E_WORKSPACE_EXTENSION_DATASET_CREATE_FAILED ||
				    comEx.ErrorCode ==
				    (int) fdoError.FDO_E_WORKSPACE_EXTENSION_DATASET_DELETE_FAILED ||
				    comEx.ErrorCode == (int) esriGeometryError.E_GEOMETRY_EDITED_REGIONS_OVERLAP ||
				    comEx.ErrorCode == (int) esriGeometryError.E_GEOMETRY_EDITS_OVERLAP)
				{
					_msg.Debug(
						"Error from overlapping edits on geometry. Unable to merge using IConstructMerge.MergeGeometries().",
						comEx);
					return null;
				}

				// NOTE: This really should not happen unless there are other unknown bugs
				_msg.Warn(
					"Unexpected COM Exception in automatic geometry merge. Unable to merge using IConstructMerge.MergeGeometries().",
					comEx);
				return null;
			}
			catch (Exception ex)
			{
				// Note: there are AccessViolations when one version was only changed in Z (or the vertex order was changed?)
				_msg.Warn(
					"Unexpected Exception in automatic geometry merge. Unable to merge using IConstructMerge.MergeGeometries().",
					ex);
				return null;
			}

			if (! GeometryUtils.IsZAware(mergedGeometry) &&
			    GeometryUtils.IsZAware(commonAncestorGeometry))
			{
				_msg.DebugFormat(
					"Automatic geometry merge resulted in lost Z-awareness. Unable to merge using IConstructMerge.MergeGeometries().: {0}",
					GeometryUtils.ToString(mergedGeometry));
				return null;
			}

			return mergedGeometry;
		}

		[CanBeNull]
		public static IMultipoint MergeStates([CanBeNull] IMultipoint thisState,
		                                      [CanBeNull] IMultipoint otherState,
		                                      [NotNull] IPolygon perimeter)
		{
			if (thisState == null || otherState == null)
			{
				return null;
			}

			IMultipoint thisPart = GetMultipointPartByPerimeter(thisState, perimeter, true);
			IMultipoint otherPart = GetMultipointPartByPerimeter(otherState, perimeter, false);

			if (thisPart.IsEmpty)
			{
				return otherPart;
			}

			if (otherPart.IsEmpty)
			{
				return thisPart;
			}

			//  topoOp2.Union(otherPart) as IMultipoint gives different output! Using GeometryUtils (adding point collection to point collection)
			var outMultipoint = GeometryUtils.Union(thisPart, otherPart) as IMultipoint;

			if (outMultipoint == null || outMultipoint.IsEmpty)
			{
				return null;
			}

			var mergedMpt = (ITopologicalOperator2) outMultipoint;

			mergedMpt.IsKnownSimple_2 = false;
			mergedMpt.Simplify();

			return outMultipoint;
		}

		/// <summary>
		/// Merges two states of a single-part polyline feature geometry.
		/// </summary>
		/// <param name="thisState">The first state of the polyline</param>
		/// <param name="otherState">The second state of the polyline</param>
		/// <param name="perimeter">The perimeter that cuts the polyline</param>
		/// <param name="discontinuityLines">The discontinuity lines. These
		/// are indications where (along the cutting perimeter) the two lines
		/// have changed i.e. the two splitted parts did not connect before they
		/// were re-joined. Provide an empty polyline geometry to retrieve this
		/// information or null.</param>
		/// <param name="notifications">The notifications.</param>
		/// <returns>
		/// Merged polyline or NULL if not automatically mergeable
		/// </returns>
		[CanBeNull]
		public static IPolyline MergeStates(
			[NotNull] IPolyline thisState,
			[NotNull] IPolyline otherState,
			[NotNull] IPolygon perimeter,
			[CanBeNull] IGeometryCollection discontinuityLines = null,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(thisState, nameof(thisState));
			Assert.ArgumentNotNull(otherState, nameof(otherState));
			Assert.ArgumentNotNull(perimeter, nameof(perimeter));

			// TODO: properly support multipart polylines by associating parts with each other
			//       and by calling MergeStates for each part-pair. -> only return null if part count differs.
			if (! CheckMultiparts(thisState, otherState, notifications))
			{
				return null;
			}

			switch (AreLinesFlipped(thisState, otherState))
			{
				case LinesFlippedState.SameDirection:
					//nothing to do
					break;

				case LinesFlippedState.InversedDirection:
					otherState.ReverseOrientation();
					break;

				case LinesFlippedState.Unsure:
					// still try to merge the geometry, there are several situations
					// which could result in Unsure (eg. entire line was rotated)
					const string message =
						"It was not possible to evaluate if one of the geometry states was flipped.";
					NotificationUtils.Add(notifications, message);
					_msg.Debug(message);
					//return null;
					break;
			}

			IPolyline thisParts = GetLinePartsByPerimeter(thisState, perimeter, true);
			IPolyline otherParts = GetLinePartsByPerimeter(otherState, perimeter, false);

			if (thisParts == null || otherParts == null)
			{
				const string message =
					"Line parts (splitting with perimeter) could not be generated.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return null;
			}

			if (thisParts.IsEmpty && otherParts.IsEmpty)
			{
				const string message = "Both line parts are empty -> feature should be deleted.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return thisParts;
			}

			if (thisParts.IsEmpty != otherParts.IsEmpty)
			{
				const string message =
					"One state is empty after cutting it with the perimeter -> Returning the other state.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);

				return thisParts.IsEmpty ? otherState : thisState;
			}

			// Create the new line
			IPolyline newLine = MergeLineParts(thisState, thisParts, otherState, otherParts,
			                                   perimeter, discontinuityLines);

			return ValidateLine(newLine, notifications);
		}

		/// <summary>
		/// Merges two states of a polygon features geometry.
		/// </summary>
		/// <param name="thisState">The first state of the polygon</param>
		/// <param name="otherState">The second state of the polygon</param>
		/// <param name="perimeter">The perimeter that cuts the polygon</param>
		/// <param name="discontinuityLines">The discontinuity lines. These
		/// are indications where (along the cutting perimeter) the two polygons
		/// have changed i.e. the two splitted parts did not connect before they
		/// were re-joined. Provide an empty polyline geometry to retrieve this
		/// information or null.</param>
		/// <param name="notifications">The notifications.</param>
		/// <param name="removeDiscontinuitiesInResult">Whether or not to remove
		/// points that where generated by splitting the states and reunion them.</param>
		/// <returns>Merged polygon or null if not automatically mergeable</returns>
		[CanBeNull]
		public static IPolygon MergeStates(
			[NotNull] IPolygon thisState,
			[NotNull] IPolygon otherState,
			[NotNull] IPolygon perimeter,
			[CanBeNull] IGeometryCollection discontinuityLines = null,
			[CanBeNull] NotificationCollection notifications = null,
			bool removeDiscontinuitiesInResult = true)
		{
			IPolygon thisParts = GetPolygonPartsByPerimeter(thisState, perimeter, true);
			IPolygon otherParts = GetPolygonPartsByPerimeter(otherState, perimeter, false);

			if (thisParts == null || otherParts == null)
			{
				const string message =
					"Polygon parts (splitting with perimeter) could not be generated.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return null;
			}

			if (thisParts.IsEmpty && otherParts.IsEmpty)
			{
				const string message = "Both polygon parts are empty -> feature should be deleted.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return thisParts;
			}

			if (thisParts.IsEmpty != otherParts.IsEmpty)
			{
				const string message =
					"One state is empty after cutting it with the perimeter -> Returning the other state.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);

				return thisParts.IsEmpty ? otherState : thisState;
			}

			IPolygon newPoly = MergePolygonParts(thisParts, otherParts);

			if (newPoly == null || newPoly.IsEmpty)
			{
				const string message =
					"The result of the union of both polygon parts is null or empty.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return null;
			}

			// Create the discontinuity lines
			CheckDiscontinuity(newPoly, perimeter, discontinuityLines);

			// After the union, all additional vertexes generated by cutting the states
			// with the perimeter and the union itself can be removed.
			if (removeDiscontinuitiesInResult)
			{
				RemoveProcessGeneratedPoints(newPoly, thisState, otherState, perimeter,
				                             notifications);
			}

			return ValidatePolygon(newPoly, notifications);
		}

		#endregion

		#region Private methods

		#region Private methods (MergeEditedVertexes)

		/// <summary>
		/// Work-around for Bug(s) in MergeGeometries
		/// </summary>
		/// <param name="mergedGeometry"></param>
		/// <param name="polygonOrPolyline1"></param>
		/// <param name="polygonOrPolyline2"></param>
		/// <param name="commonAncestorGeometry"></param>
		private static void FixIncorrectVertices([NotNull] IGeometry mergedGeometry,
		                                         [NotNull] IGeometry polygonOrPolyline1,
		                                         [NotNull] IGeometry polygonOrPolyline2,
		                                         [NotNull] IGeometry commonAncestorGeometry)
		{
			// incorrect z values are assigned at the connect points (where the different curve starts and ends) with the common ancestor
			CorrectIntersectionPointZs((ICurve) mergedGeometry, commonAncestorGeometry,
			                           polygonOrPolyline1);
			CorrectIntersectionPointZs((ICurve) mergedGeometry, commonAncestorGeometry,
			                           polygonOrPolyline2);

			// Identify intersection points between common ancestor and the replacement that remain after merge:
			SegmentReplacementUtils.RemovePhantomPointInserts((ICurve) mergedGeometry,
			                                                  commonAncestorGeometry,
			                                                  polygonOrPolyline1);
			SegmentReplacementUtils.RemovePhantomPointInserts((ICurve) mergedGeometry,
			                                                  commonAncestorGeometry,
			                                                  polygonOrPolyline2);
		}

		/// <summary>
		/// Corrects the z values at intersections between common ancestor and update where a vertex exists in both
		/// the update and the merged geometry but the update's Z value was lost.
		/// </summary>
		/// <param name="processedCurve"></param>
		/// <param name="originalGeometry"></param>
		/// <param name="replacementGeometry"></param>
		private static void CorrectIntersectionPointZs(
			[NotNull] ICurve processedCurve,
			[NotNull] IGeometry originalGeometry,
			[NotNull] IGeometry replacementGeometry)
		{
			Stopwatch correctZsWatch =
				_msg.DebugStartTiming("Correcting Z values of intersections with original...");

			IPolyline originalPolyline = GeometryFactory.CreatePolyline(originalGeometry);

			// TODO: make a method GetIntersectPoints containing all known work-arounds etc.
			IPointCollection intersections =
				GetIntersectPoints((ITopologicalOperator) originalPolyline, replacementGeometry,
				                   true);

			_msg.DebugFormat(
				"Found {0} intersections between original and replacement geometry",
				intersections.PointCount);

			IPoint testPoint = new PointClass();
			IPoint origHitPoint = new PointClass();
			IPoint newHitPoint = new PointClass();

			IHitTest orgHitTest = GeometryUtils.GetHitTest(replacementGeometry, true);
			IHitTest newHitTest = GeometryUtils.GetHitTest(processedCurve, true);

			double searchRadius = GeometryUtils.GetSearchRadius(processedCurve);

			var correctCount = 0;
			for (var i = 0; i < intersections.PointCount; i++)
			{
				intersections.QueryPoint(i, testPoint);

				bool existsInOriginal = FindPoint(testPoint, orgHitTest, searchRadius,
				                                  ref origHitPoint);

				bool existsInNew = FindPoint(testPoint, newHitTest, searchRadius, ref newHitPoint);

				_msg.DebugFormat("Checking point {0}|{1}", testPoint.X, testPoint.Y);
				if (existsInOriginal && existsInNew &&
				    Math.Abs(origHitPoint.Z - newHitPoint.Z) > double.Epsilon)
				{
					_msg.DebugFormat("Point exists in both geometries. Z-orig: {0}, Z-new: {1}",
					                 origHitPoint.Z, newHitPoint.Z);

					int partIdx;
					int segmentIdx = SegmentReplacementUtils.GetSegmentIndex(processedCurve,
						origHitPoint,
						searchRadius,
						out partIdx);

					SegmentReplacementUtils.EnsureVertexExists(origHitPoint, processedCurve,
					                                           segmentIdx, partIdx);

					correctCount++;
				}
			}

			Marshal.ReleaseComObject(intersections);

			_msg.DebugStopTiming(correctZsWatch,
			                     "Corrected {0} z values produced by geometry merge.",
			                     correctCount);
		}

		/// <summary>
		/// TODO: copy from geometry utils to add special handling of rings
		/// </summary>
		/// <param name="lineTopoOp">The line topo op.</param>
		/// <param name="lineOfInterest">The line of interest.</param>
		/// <param name="ignoreRingEndpoints">if set to <c>true</c> [ignore ring endpoints].</param>
		/// <returns></returns>
		[NotNull]
		private static IPointCollection GetIntersectPoints(
			[NotNull] ITopologicalOperator lineTopoOp,
			[NotNull] IGeometry lineOfInterest,
			bool ignoreRingEndpoints)
		{
			if (GeometryUtils.Disjoint((IGeometry) lineTopoOp, lineOfInterest))
			{
				return new MultipointClass();
			}

			// Get intersect points
			var intersectPoints =
				(IPointCollection) lineTopoOp.Intersect(
					lineOfInterest,
					esriGeometryDimension.esriGeometry0Dimension);

			// get shared lines, were not included in intersect points
			var intersectLines =
				(IGeometryCollection) lineTopoOp.Intersect(
					lineOfInterest,
					esriGeometryDimension.esriGeometry1Dimension);

			// add start and end point of shared lines to intersect points
			for (var i = 0; i < intersectLines.GeometryCount; i++)
			{
				var line = intersectLines.Geometry[i] as IPath;

				if (line == null)
				{
					continue;
				}

				if (ignoreRingEndpoints && line.IsClosed)
				{
					continue;
				}

				intersectPoints.AddPoint(line.ToPoint, ref _missingRef, ref _missingRef);
				intersectPoints.AddPoint(line.FromPoint, ref _missingRef, ref _missingRef);
			}

			GeometryUtils.Simplify((IGeometry) intersectPoints);

			return intersectPoints;
		}

		private static bool FindPoint([NotNull] IPoint testPoint,
		                              [NotNull] IHitTest hitTest,
		                              double searchRadius,
		                              [NotNull] ref IPoint hitPoint)
		{
			//IPoint hitPoint = new PointClass();
			double distance = 0;
			var hitPartIndex = 0;
			var hitSegmentIndex = 0;
			var right = false;

			bool found = hitTest.HitTest(testPoint, searchRadius,
			                             esriGeometryHitPartType.esriGeometryPartVertex,
			                             hitPoint,
			                             ref distance, ref hitPartIndex,
			                             ref hitSegmentIndex,
			                             ref right);
			return found;
		}

		#endregion

		#region Private methods (Merge multipoint)

		[NotNull]
		private static IMultipoint GetMultipointPartByPerimeter(
			[NotNull] IMultipoint multipoint, [NotNull] IPolygon perimeter, bool insidePart)
		{
			IMultipoint newMultipoint;
			if (insidePart)
			{
				_msg.VerboseDebug(() => $"Getting inside part of multipoint: {GeometryUtils.ToString(multipoint)}");

				// 10.2.2: The Z values come from the first geometry, even if it is a polygon and the results are multipoints!
				newMultipoint = IntersectionUtils.Intersect(
					                multipoint, perimeter, multipoint.Dimension) as IMultipoint;
			}
			else
			{
				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.VerboseDebug(() => $"Getting outside part of multipoint: {GeometryUtils.ToString(multipoint)}");
				}

				// Starting with 10.2.2 difference works now also with multipoints (with a handful of workarounds in IntersectionUtils)
				newMultipoint = IntersectionUtils.Difference(multipoint, perimeter) as IMultipoint;
			}

			Assert.NotNull(newMultipoint, "Merge result is null or not a multipart");

			return newMultipoint;
		}

		#endregion

		#region Private methods (Merge polyline)

		/// <summary>
		/// Checks if one or both of the given lines are multipart
		/// </summary>
		/// <param name="thisState">State of the this.</param>
		/// <param name="otherState">State of the other.</param>
		/// <param name="notifications">The notifications.</param>
		/// <returns></returns>
		private static bool CheckMultiparts([NotNull] IPolyline thisState,
		                                    [NotNull] IPolyline otherState,
		                                    [CanBeNull] NotificationCollection notifications)
		{
			int thisGeometryCount = ((IGeometryCollection) thisState).GeometryCount;
			int otherGeometryCount = ((IGeometryCollection) otherState).GeometryCount;

			if (thisGeometryCount != 1 || otherGeometryCount != 1)
			{
				// merging multipart polylines is strictly experimental. Consider returning null.

				_msg.DebugFormat("Multipart polylines. This part count: {0}", thisGeometryCount);
				NotificationUtils.Add(notifications,
				                      "One or both polylines to merge have multiple parts.");
			}

			if ((thisGeometryCount > 0 || otherGeometryCount > 0) &&
			    thisGeometryCount != otherGeometryCount)
			{
				string message =
					string.Format("One of the polyline states has more parts than the other. " +
					              "This geometry has {0} parts - Other geometry has {1} parts.",
					              thisGeometryCount, otherGeometryCount);

				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);

				return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if one of the lines where "flipped" by the user.
		/// </summary>
		/// <param name="line1"></param>
		/// <param name="line2"></param>
		/// <returns></returns>
		private static LinesFlippedState AreLinesFlipped([NotNull] IPolyline line1,
		                                                 [NotNull] IPolyline line2)
		{
			if (GeometryUtils.EndPointsAreEqual(line1, line2))
			{
				return LinesFlippedState.SameDirection;
			}

			// Approach 2: If approach 1 is not satisfactory, check if any of the
			// vertexes of the first line can be found in the seconde line,
			// if more than one is found, the relative order of the points must be the
			// same in both lines.
			int thisPntPos = -1, thisPntPos2 = -1;
			int otherPntPos = -1;
			var thisPntColl = (IPointCollection) line1;
			var otherPntColl = (IPointCollection) line2;
			int tSize = thisPntColl.PointCount;
			int oSize = otherPntColl.PointCount;

			for (var tPntIdx = 0; tPntIdx < tSize; tPntIdx++)
			{
				var pntRelOp = (IRelationalOperator) thisPntColl.Point[tPntIdx];

				for (var oPntIdx = 0; oPntIdx < oSize; oPntIdx++)
				{
					if (! pntRelOp.Equals(otherPntColl.Point[oPntIdx]))
					{
						continue;
					}

					// Store positions of identical points
					if (thisPntPos == -1)
					{
						thisPntPos = tPntIdx;
						otherPntPos = oPntIdx;
						break;
					}

					thisPntPos2 = tPntIdx;
					int otherPntPos2 = oPntIdx;

					// Check order of points
					if (thisPntPos2 <= -1)
					{
						continue;
					}

					if (thisPntPos2 - thisPntPos > 0 == otherPntPos2 - otherPntPos > 0)
					{
						return LinesFlippedState.SameDirection;
					}

					_msg.Debug(
						"One of the line geometry states is probably flipped (Uneven point order).");
					return LinesFlippedState.InversedDirection;
				}
			}

			// If only one matching vertex is found, than determine the order of it in
			// each line. If the order of both lines is below or over 50% then it's possible
			// that none of the lines where flipped. If the orders does not match, one
			// of the lines could be potentially flipped.
			if (thisPntPos > -1 && thisPntPos2 == -1)
			{
				double relativeThisPos = tSize / (double) thisPntPos;
				double relativeOtherPos = oSize / (double) otherPntPos;
				if (relativeThisPos < 2 == relativeOtherPos < 2)
				{
					return LinesFlippedState.SameDirection;
				}

				_msg.Debug(
					"One of the line geometry states is probably flipped (Relative positions are not identical).");
				return LinesFlippedState.InversedDirection;
			}

			// If none of the approaches works then it is not possible to get information
			// about flipped geometry
			return LinesFlippedState.Unsure;
		}

		/// <summary>
		/// Gets the parts of a given (single part) line inside or outside the given perimeter (polygon).
		/// The line's From and To points will be ensured to be also from and to points in one of the
		/// parts. This is also the case if the line is closed.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="perimeter"></param>
		/// <param name="insideParts">Whether the inside parts or the outside parts of the line should
		/// be returned.</param>
		/// <returns></returns>
		[CanBeNull]
		private static IPolyline GetLinePartsByPerimeter([NotNull] IPolyline line,
		                                                 [NotNull] IPolygon perimeter,
		                                                 bool insideParts)
		{
			// NOTE: ITopologicalOperator behaves differently if the polyline is closed
			//       i.e. if the line's start and end point are at the same place such
			//       as a closed contour line. This is the case for both Difference() and Intersect()
			//       If the end point was inside the poly (intersect) or outside (difference)
			//       the result is sometimes a merged single line part rather than two parts
			//       that are joined at the original start/end point.
			//       This results in wrong results of MergeGeometreies 
			//       Solution: split the line if the previous end point is not an end point 
			//       of the resulting line.

			IPolyline newLine;
			if (insideParts)
			{
				_msg.VerboseDebug(() => $"Getting inside part of line: {GeometryUtils.ToString(line)}");

				var topoOp4 = (ITopologicalOperator4) perimeter;

				// if it does not intersect -> empty polygon is returned!
				IGeometry intersectGeometry = topoOp4.Intersect(line, line.Dimension);

				if (intersectGeometry.IsEmpty)
				{
					// return empty geometry but of the correct type
					newLine = new PolylineClass();
				}
				else
				{
					newLine = (IPolyline) intersectGeometry;
				}
			}
			else
			{
				_msg.VerboseDebug(() => $"Getting outside part of line: {GeometryUtils.ToString(line)}");

				var topoOp4 = (ITopologicalOperator) line;
				newLine = (IPolyline) topoOp4.Difference(perimeter);
			}

			EnsureFromToForClosedLines(line, perimeter, newLine, insideParts);

			return newLine;
		}

		private static void EnsureFromToForClosedLines([NotNull] IPolyline originalLine,
		                                               [NotNull] IPolygon perimeter,
		                                               [NotNull] IPolyline lineParts,
		                                               bool insideParts)
		{
			// Correct the problem described above - line parts can be merged if they originate from a closed line.
			if (! ((ICurve) originalLine).IsClosed)
			{
				return;
			}

			_msg.DebugFormat(
				"Post-processing Topo-Op for closed lines. InsideParts: {0}", insideParts);
			// if the inside parts (intersect) were extracted and the end point is inside
			// or if the outside parts (difference) were extracted and the end point is on the outside part...
			if (insideParts && GeometryUtils.Contains(perimeter, originalLine.ToPoint) ||
			    ! insideParts && GeometryUtils.Disjoint(perimeter, originalLine.ToPoint))
			{
				// check each of ne new line parts if one of them holds the original end point
				// if not: it was changed by topologicaloperator
				var endPointChanged = true;
				var newLineCollection = (IGeometryCollection) lineParts;
				for (var i = 0; i < newLineCollection.GeometryCount; i++)
				{
					var newLinePart = (ICurve) newLineCollection.Geometry[i];
					if (GeometryUtils.AreEqual(originalLine.ToPoint, newLinePart.ToPoint))
					{
						endPointChanged = false;
					}
				}

				// re-split if necessary
				if (endPointChanged)
				{
					// NOTE: the split can inverse the orientation of the line!
					_msg.DebugFormat(
						"Line part needed correction (re-split). insideParts: {0}",
						insideParts);

					lineParts.SplitAtPoint(originalLine.ToPoint, false, true,
					                       out bool _, out int _, out int _);
				}
			}
		}

		/// <summary>
		/// Checks if the given line is valid (not empty, 0 length, mutlipart or selfintersecting)
		/// </summary>
		[CanBeNull]
		private static IPolyline ValidateLine([NotNull] IPolyline line,
		                                      [CanBeNull] NotificationCollection
			                                      notifications)
		{
			// ?! Generated geometry checks

			// Empty or 0 length:
			// Normal checks if the created line is empty or has a length < 0

			// Selfintersecting:
			// The new generated line should not be selfintersecting -> this will
			// lead to a multipart geometry when stored in SDE

			// Multipart:
			// The new generated line should not be multipart -> swisstopo likes
			// to have only simple geometries.

			// SelfIntersecting could be tested also, when the new geometry
			// is simplified first and then the multipart check is run.

			// Empty or o length
			if (line.IsEmpty || line.Length <= 0)
			{
				const string message = "Merged line is empty or has a length <= 0.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return null;
			}

			var lineTopoOp = (ITopologicalOperator3) line;
			lineTopoOp.IsKnownSimple_2 = false;
			lineTopoOp.Simplify();

			// simplify could have made the geometry empty (or only now .IsEmpty returns
			// the correct result)
			if (line.IsEmpty)
			{
				const string message = "The line has become empty by simplifying it.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return null;
			}

			// EMA: Simplify creates multiparts... Check if simple before merge?
			// EMA: check if empty (again) after simplify -> it could have become empty (also polygons)

			// Simplify has eliminated SelfIntersecting, test not needed.
			// ! Had get SelfIntersecting-Error even for the
			// simpleMerge test where no SelfIntersecting could be recognized by
			// viewing the genrated line in ArcMap (perhaps some points where doublicated...)
			/*
            // Selfintersecting or other faults
            esriNonSimpleReasonEnum simpleExRes;
            if (!lineTopoOp.get_IsSimpleEx(out simpleExRes))
                Log.WriteLine("  !Generated line is not simple! SimpleEx Result: " + simpleExRes);
            */

			// MAware geometry wont result several geometries after simplify, 
			// check possible selfintersection separately
			bool valid = CheckPossibleMAwareMultipart(line);

			if (! valid)
			{
				const string message = "Merged line is MAware multipart.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return null;
			}

			// Multipart
			// TODO: need to consider if the orignal states of the lines where multipart
			// already. -> even then the output might not be ok as the wrong end points are re-connected
			// TODO: properly support multiparts by calling mergegeometries for each part
			if (((IGeometryCollection) line).GeometryCount > 1)
			{
				// still return the geometry
				// If multiparts are forbidden let the QA find it rather than wasting a potentially
				// large amount of work due to not merging the states here.
				const string message = "Merged line is multipart.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				//return null;
			}

			return line;
		}

		private static bool CheckPossibleMAwareMultipart([NotNull] IPolyline geometry)
		{
			if (! (geometry is IMAware mAware) || mAware.MAware == false)
			{
				return true; // no special check needed
			}

			var unawareGeometry = (IPolyline) GetSimplifiedMUnawareGeometry(geometry);

			return unawareGeometry != null &&
			       ((IGeometryCollection) unawareGeometry).GeometryCount == 1;
		}

		private static bool CheckPossibleMAwareMultipart([NotNull] IPolygon geometry)
		{
			if (! (geometry is IMAware mAware) || mAware.MAware == false)
			{
				return true; // no special check needed
			}

			var unawareGeometry = (IPolygon) GetSimplifiedMUnawareGeometry(geometry);

			return unawareGeometry != null &&
			       GeometryUtils.HasOnlyOnePositiveAreaPart(unawareGeometry);
		}

		/// <summary>
		/// The method will connect the parts from two original lines
		/// by using the theroreticaly approach defined in the
		/// GeometricConflictResolution_Scenarios.ppt
		/// </summary>
		/// <param name="thisLine">Source line (inside perimeter)</param>
		/// <param name="thisParts">Parts of source line (inside perimeter)</param>
		/// <param name="otherLine">Source line (outside perimeter)</param>
		/// <param name="otherParts">Parts of source line (outside perimeter)</param>
		/// <param name="perimeter">The perimeter.</param>
		/// <param name="discontinuities">The discontinuity lines.</param>
		/// <returns>Connected geometry or NULL</returns>
		[NotNull]
		private static IPolyline MergeLineParts(
			[NotNull] IPolyline thisLine, [NotNull] IPolyline thisParts,
			[NotNull] IPolyline otherLine, [NotNull] IPolyline otherParts,
			[NotNull] IPolygon perimeter, [CanBeNull] IGeometryCollection discontinuities)
		{
			IPolyline newLine = GeometryFactory.CreateEmptyPolyline(thisLine);

			// Check if the start point lies in the perimeter or not.
			bool thisStart = DoesThisLineStartInside(thisLine, otherLine, perimeter);

			// Store parts
			// It looks like the order of the part segements must not be the same
			// as the original segment order! But the algorithm will not work correct
			// if the parts have not the right order...
			var thisPartsCache = new LineFragmentsCache(thisParts, perimeter, thisLine);
			var otherPartsCache = new LineFragmentsCache(otherParts, perimeter, otherLine);

			// Connect the parts
			// The first part is the one with the starting point
			LineFragment currentPart = thisStart
				                           ? thisPartsCache.UseNextUnusedPart()
				                           : otherPartsCache.UseNextUnusedPart();

			// Loop to connect all the parts (if we use a part with the endpoint flag
			// set, we will stop. Because of that, it could happen, that not all parts
			// will be connected -> could be an error...)
			while (currentPart != null)
			{
				// add currentPart to the new line 
				// EMA: and if the fragment was from the other line?
				// -> on the other side the end point is missed if it was changed...

				// SCG fixing idea
				IPolyline checkLine = thisPartsCache.Fragments.Contains(currentPart)
					                      ? thisLine
					                      : otherLine;

				AddPointsToCollection((IPointCollection) newLine, currentPart.Points, checkLine);
				// AddPointsToCollection(pointColl, currentPart.Points, thisLine);

				// Stop loop if the last used part holds the endpoint
				if (currentPart.HoldsEndPoint)
				{
					break;
				}

				currentPart = GetNextFragment(currentPart, thisPartsCache, otherPartsCache,
				                              discontinuities);
			}

			// Check if every part was used...
			if (thisPartsCache.HasMoreUnusedParts() || otherPartsCache.HasMoreUnusedParts())
			{
				_msg.DebugFormat(
					"! Not all parts where used [Unused THIS-PartsCount: {0}" +
					" - Unused OTHER-PartsCount: {1}]",
					otherPartsCache.NrUnusedParts, thisPartsCache.NrUnusedParts);
			}

			return newLine;
		}

		/// <summary>
		/// Checks whether the start point of this line is inside the perimeter or not.
		/// </summary>
		/// <returns>TRUE if startpoint of thisLine lies inside the perimeter</returns>
		private static bool DoesThisLineStartInside([NotNull] IPolyline thisLine,
		                                            [NotNull] IPolyline otherLine,
		                                            [NotNull] IPolygon perimeter)
		{
			bool thisInside = GeometryUtils.Contains(perimeter, thisLine.FromPoint);
			bool otherInside = GeometryUtils.Contains(perimeter, otherLine.FromPoint);

			// If both startpoints lies on different sides, we will start with
			// the first outside part.
			// That can happen, when the startpoint is deleted (allowed edit)
			// The otherway round is not allowed (moving the start point from
			// outside to inside)
			// todo: Inform the user about the "not so clear" situation?
			if (thisInside != otherInside)
			{
				_msg.DebugFormat(
					"Start points of both line states are on different perimeter sides (thisInside = {0}, otherInside = {1})",
					thisInside, otherInside);
			}

			return thisInside;
		}

		/// <summary>
		/// Get the next part that will be added to the constructed line.
		/// </summary>
		/// <param name="currentPart">Current part (last part added to the line)</param>
		/// <param name="thisPartsCache">Parts of the line inside the perimeter</param>
		/// <param name="otherPartsCache">Parts of the line outside the perimeter</param>
		/// <param name="discontinuityLines">The discontinuity lines.</param>
		/// <returns>
		/// Fragment to add next to the line or null if no free fragment is found
		/// </returns>
		[CanBeNull]
		private static LineFragment GetNextFragment(
			[NotNull] LineFragment currentPart,
			[NotNull] LineFragmentsCache thisPartsCache,
			[NotNull] LineFragmentsCache otherPartsCache,
			[CanBeNull] IGeometryCollection discontinuityLines)
		{
			// TODO: get the discontinuities from where the points are added
			// to the collection?
			LineFragment thisPart = thisPartsCache.GetNextUnusedPart();
			LineFragment otherPart = otherPartsCache.GetNextUnusedPart();

			// If no fragment is found, return null
			if (thisPart == null && otherPart == null)
			{
				return null;
			}

			// If only one free fragment is found, return it.
			if (thisPart != null && otherPart == null)
			{
				CheckDiscontinuity(currentPart, thisPart, discontinuityLines);
				return thisPartsCache.UseNextUnusedPart();
			}

			if ( /*otherPart != null && */thisPart == null)
			{
				CheckDiscontinuity(currentPart, otherPart, discontinuityLines);
				return otherPartsCache.UseNextUnusedPart();
			}

			// Both parts present, check the distances to the current part
			double thisDist = currentPart.GetPointDistance(
				thisPart.Points.Point[0], false);
			double otherDist = currentPart.GetPointDistance(
				otherPart.Points.Point[0], false);

			// If a part of the point distance is 0 and the other one is >0, then return
			// the part with 0 distance
			if (Math.Abs(thisDist) < double.Epsilon && otherDist > 0)
			{
				CheckDiscontinuity(currentPart, thisPart, discontinuityLines);
				return thisPartsCache.UseNextUnusedPart();
			}

			if (Math.Abs(otherDist) < double.Epsilon && thisDist > 0)
			{
				CheckDiscontinuity(currentPart, otherPart, discontinuityLines);
				return otherPartsCache.UseNextUnusedPart();
			}

			// If thisDist is smaller than otherDist, check if the count of connecting
			// points is greater or same as the connecting points of the otherParts.
			// Do the same the otherway round, if otherDist is greater than thisDist.
			if (thisDist < otherDist)
			{
				if (thisPartsCache.GetNrUnusedConnectPoints() >=
				    otherPartsCache.GetNrUnusedConnectPoints())
				{
					CheckDiscontinuity(currentPart, thisPart, discontinuityLines);
					return thisPartsCache.UseNextUnusedPart();
				}

				CheckDiscontinuity(currentPart, otherPart, discontinuityLines);
				return otherPartsCache.UseNextUnusedPart();
			}

			if (otherPartsCache.GetNrUnusedConnectPoints() >=
			    thisPartsCache.GetNrUnusedConnectPoints())
			{
				CheckDiscontinuity(currentPart, otherPart, discontinuityLines);
				return otherPartsCache.UseNextUnusedPart();
			}

			CheckDiscontinuity(currentPart, thisPart, discontinuityLines);
			return thisPartsCache.UseNextUnusedPart();
		}

		private static void CheckDiscontinuity(
			[NotNull] LineFragment currentPart,
			[NotNull] LineFragment nextPart,
			[CanBeNull] IGeometryCollection discontinuities)
		{
			// TODO: move the checking to the AddPointsToCollection method (remembering
			//       the last fragment's cut-point with the perimeter line.
			// TODO: handle the 3D-issues: test if the line.Length is really the 3D length.

			if (discontinuities == null)
			{
				return;
			}

			ILine line = currentPart.GetLine(nextPart.Points.Point[0], false);

			if (line.Length > 0) // _minDiscontinuityLenth)
			{
				IGeometry singleLine = GeometryFactory.CreatePolyline(
					line,
					((IGeometry) discontinuities).SpatialReference,
					((IZAware) discontinuities).ZAware);

				discontinuities.AddGeometry(
					((IGeometryCollection) singleLine).Geometry[0],
					ref _missingRef, ref _missingRef);
			}

			// else: give feedback that small changes in the border area could have happened
		}

		/// <summary>
		/// Adds the currentPart to the pointColl.
		/// Before the points are added, they will be tested, if they exist
		/// in the checkLine, if so, the could be added, if not, the point
		/// must be generated by cutting the line with the perimeter, such
		/// points are not added (the test is only needed for the first and last
		/// point in the collection, all other points must be points of the checkLine)
		/// </summary>
		/// <param name="pointColl">Collection of points to enhance with the new points</param>
		/// <param name="addPoints">Collection of points to add</param>
		/// <param name="checkLine">Line for the check if the adding point is new</param>
		private static void AddPointsToCollection([NotNull] IPointCollection pointColl,
		                                          [NotNull] IPointCollection addPoints,
		                                          [NotNull] IPolyline checkLine)
		{
			int nrPoints = addPoints.PointCount;
			for (var pntIdx = 0; pntIdx < nrPoints; pntIdx++)
			{
				IPoint addPoint = addPoints.Point[pntIdx];

				// Start or endpoint, check before adding
				if (pntIdx == 0 || pntIdx == nrPoints - 1)
				{
					if (PointExistsInGeometry(addPoint, checkLine))
					{
						pointColl.AddPoint(addPoint, ref _missingRef, ref _missingRef);
					}
				}
				// Normally add the point
				else
				{
					pointColl.AddPoint(addPoint, ref _missingRef, ref _missingRef);
				}
			}
		}

		#endregion

		#region Private methods (Merge polygon)

		/// <summary>
		/// Cuts the polygon by the perimeter border.
		/// If insidePart = TRUE, the part of the polygon inside the perimeter is returned,
		/// if insidePart = FALSE, the part of the polygon outside the perimeter is returned.
		/// </summary>
		[CanBeNull]
		private static IPolygon GetPolygonPartsByPerimeter([NotNull] IPolygon polygon,
		                                                   [NotNull] IPolygon perimeter,
		                                                   bool insideParts)
		{
			IPolygon newPolygon;
			if (insideParts)
			{
				_msg.VerboseDebug(
					() => $"Getting inside part of polygon: {GeometryUtils.ToString(polygon)}");

				var topoOp4 = (ITopologicalOperator4) perimeter;
				newPolygon = topoOp4.Intersect(polygon, polygon.Dimension) as IPolygon;
			}
			else
			{
				_msg.VerboseDebug(
					() => $"Getting outside part of polygon: {GeometryUtils.ToString(polygon)}");

				var topoOp4 = (ITopologicalOperator) polygon;
				newPolygon = topoOp4.Difference(perimeter) as IPolygon;
			}

			return newPolygon;
		}

		[CanBeNull]
		private static IPolygon MergePolygonParts([NotNull] IPolygon thisPart,
		                                          [NotNull] IPolygon otherPart)
		{
			var topoOp4 = (ITopologicalOperator4) thisPart;
			var newPolygon = topoOp4.Union(otherPart) as IPolygon;

			return newPolygon;
		}

		private static void CheckDiscontinuity([NotNull] IGeometry unionPoly,
		                                       [NotNull] IPolygon perimeter,
		                                       [CanBeNull] IGeometryCollection
			                                       discontinuities)
		{
			if (discontinuities == null)
			{
				return;
			}

			// get the segments of the union poly that are coincident with the perimeter
			var topoOp = (ITopologicalOperator) perimeter;

			IGeometry intersection = topoOp.Intersect(
				unionPoly, esriGeometryDimension.esriGeometry1Dimension);

			if (intersection.IsEmpty)
			{
				return;
			}

			var polyline = (IPolyline) intersection;

			var lineColl = (IGeometryCollection) polyline;

			// TODO: filter out those who were on the line in the first place
			for (var i = 0; i < lineColl.GeometryCount; i++)
			{
				var discontinuityLine = (ICurve) lineColl.Geometry[i];

				if (discontinuityLine.Length > 0) //_minDiscontinuityLenth)
				{
					discontinuities.AddGeometry(discontinuityLine, ref _missingRef,
					                            ref _missingRef);
				}
			}
		}

		/// <summary>
		/// Removes the points that where generated by splitting the states
		/// and reunion them.
		/// </summary>
		/// <param name="unionPoly">The union poly.</param>
		/// <param name="thisState">State of the this.</param>
		/// <param name="otherState">State of the other.</param>
		/// <param name="perimeter">The perimeter.</param>
		/// <param name="notifications"></param>
		private static void RemoveProcessGeneratedPoints(
			[NotNull] IPolygon unionPoly,
			[NotNull] IPolygon thisState,
			[NotNull] IPolygon otherState,
			[NotNull] IPolygon perimeter,
			[CanBeNull] NotificationCollection notifications)
		{
			//// TODO: This is potentially very slow and could be easily improved
			////       by intersescting with the perimeter first

			//// TODO
			//// ?! Removing additional vertexes

			//// Approach 1: Every point of the union polygon can be checked, if it
			//// exists in the thisState (point is inside perimeter) oder otherState
			//// (point is outside perimeter) if not, delete the vertex.
			//// ! A polygon could be multi part. Its not clear if the parts
			//// have the same order in the different polygons.. Every part must be
			//// checked for the point (looks time expensive)

			//// Approach 2: All points of the union polygon that are placed on the
			//// border of the perimeter are candiates to delete. If such points
			//// have not a matching vertex in either orignal states, delete it.

			//// !! If points are compared, keep in mind that the coordinates
			//// could differ by the tolerance (IRelationOperator can help here)

			//// Approach 1:
			//IGeometryCollection unionGeos = (IGeometryCollection) unionPoly;
			//ArrayList thisPoints = CollectPolyPoints(thisState);
			//ArrayList otherPoints = CollectPolyPoints(otherState);
			//ArrayList removeIndices = new ArrayList();

			IList<IPoint> pointsToRemove = new List<IPoint>();

			int intersectingVertices = 0, removedVertices = 0;

			IPolyline perimeterBoundary = GeometryFactory.CreatePolyline(perimeter);

			bool addNotification = false;

			foreach (IPolygon polygonPart in GeometryUtils.GetConnectedComponents(unionPoly))
			{
				if (GeometryUtils.Crosses(perimeterBoundary, polygonPart))
				{
					var intersections = (IPointCollection)
						IntersectionUtils.GetIntersectionPoints(perimeter, polygonPart);

					_msg.DebugFormat(
						"{0} intersection points between perimeter and union-poly part",
						intersections.PointCount);

					for (var i = 0; i < intersections.PointCount; i++)
					{
						IPoint queryPoint = intersections.Point[i];
						if (PointExistsInGeometry(queryPoint, polygonPart))
						{
							// it's a real vertex in the union poly
							intersectingVertices++;

							if (! (PointExistsInGeometry(queryPoint, thisState) ||
							       PointExistsInGeometry(queryPoint, otherState)))
							{
								removedVertices++;
								pointsToRemove.Add(queryPoint);
							}
						}
					}
				}
				else
				{
					addNotification = true;
					_msg.Debug(
						"Union polygon has a part that is not crossed by perimeter boundary. No process-generated points are removed.");
				}
			}

			if (addNotification)
			{
				NotificationUtils.Add(notifications,
				                      "The result of the split/merge process contains artificial points on the perimeter.");
			}

			_msg.DebugFormat(
				"Merged geometry's vertices on the perimeter: {0} of which introduced by merge (and removed again): {1}",
				intersectingVertices, removedVertices);

			RemoveCutPointsService.RemovePoints(unionPoly, pointsToRemove);
		}

		[CanBeNull]
		private static IPolygon ValidatePolygon([NotNull] IPolygon polygon,
		                                        [CanBeNull] NotificationCollection notifications)
		{
			// To make sure IsEmpty returns the correct result simplify first
			// For example the polygons could have been disjoint originally.
			GeometryUtils.Simplify(polygon);

			if (polygon.IsEmpty)
			{
				// simplify could have made the geometry empty (or only now .IsEmpty returns
				// the correct result)

				const string message = "The polygon has become empty by simplifying it.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return null;
			}

			// Check multipart excluding holes
			bool areaValid = GeometryUtils.HasOnlyOnePositiveAreaPart(polygon);

			if (! areaValid)
			{
				// still return the geometry - it could be a very small extra part which
				// was added due to a 'peninsula' reaching back into the perimeter!
				// If multiparts are forbidden let the QA find it rather than wasting a potentially
				// large amount of work due to not merging the states here.
				const string message = "Merged polygon has more than one positive part.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				//return null;
			}

			return polygon;
		}

		#endregion

		/// <summary>
		/// Checks if the given point does exist in the given geometry.
		/// </summary>
		private static bool PointExistsInGeometry([NotNull] IPoint point,
		                                          [NotNull] IGeometry geometry)
		{
			IPoint hitPoint = new PointClass();
			return GeometryUtils.HitTestWksPointZs(geometry, point,
			                                       GeometryUtils.GetXyTolerance(point), 0,
			                                       true, hitPoint);
		}

		/// <summary>
		///  Get Mless version of MAware geometry
		/// </summary>
		[CanBeNull]
		private static IGeometry GetSimplifiedMUnawareGeometry([NotNull] IGeometry geometry)
		{
			IGeometry unawareGeometry = GeometryFactory.Clone(geometry);

			var mUnaware = (IMAware) unawareGeometry;
			mUnaware.MAware = false;

			GeometryUtils.Simplify(unawareGeometry);

			// simplify could have made the geometry empty (or only now .IsEmpty returns
			// the correct result
			if (unawareGeometry.IsEmpty)
			{
				_msg.Debug("The geometry has become empty by simplifying it.");
				return null;
			}

			return unawareGeometry;
		}

		#endregion
	}
}
