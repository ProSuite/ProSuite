using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

using Geometry = ArcGIS.Core.Geometry.Geometry;
using Multipoint = ArcGIS.Core.Geometry.Multipoint;
using Polygon = ArcGIS.Core.Geometry.Polygon;
using Polyline = ArcGIS.Core.Geometry.Polyline;

namespace ProSuite.AGP.Editing.SplitAndMerge
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

			private ReadOnlyPointCollection _pointColl; //= Multipart.Points;
			private int _nrPoints;

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
			public ReadOnlyPointCollection Points
			{
				get => _pointColl;
				set
				{
					_pointColl = value;
					_nrPoints = _pointColl.Count;
				}
			}

			/// <summary>
			/// Returns the count of points, that need to get connected
			/// with other parts (real start and endpoints need no more connection)
			/// </summary>
			public int ConnectPoints => _connPoints;

			/// <summary>
			/// Returns the flag that identifies a part with the endpoint
			/// from the original line
			/// </summary>
			public bool HoldsEndPoint
			{
				get { return _hasEndPoint; }
				set { _hasEndPoint = value; }
			}

			public double StartDistanceAlongSourceLine
			{
				get { return _startDistanceAlongSourceLine; }
				set { _startDistanceAlongSourceLine = value; }
			}

			#endregion

			public LineFragment([NotNull] ReadOnlyPointCollection pointCollection,
			                    [NotNull] Polygon perimeter)
			{
				_pointColl = pointCollection;
				_nrPoints = _pointColl.Count;
				EvaluateConnectionFlag(perimeter);
			}

			private void EvaluateConnectionFlag([NotNull] Polygon perimeter)
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
				if (PointLiesOnPerimeterBorder(_pointColl[0], perimeter))
				{
					_connPoints++;
				}

				if (PointLiesOnPerimeterBorder(
					    _pointColl[_nrPoints - 1], perimeter))
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
			private static bool PointLiesOnPerimeterBorder([NotNull] MapPoint point,
			                                               [NotNull] Polygon perimeter)
			{
				// TODO
				// ?! Check point on perimeter border
				// We need to know, if the point lies on the border of the
				// given perimeter. How can we do that?
				// The perimeter can be a free choosen polygon (does not do be a rectangle)
				// Perhaps we need to loop through all perimeter segements and check the point
				// distance to the line -> ICurve.QueryPointAndDistance could be helpfull...

				//IPoint retVal = new PointClass();
				//perimeter.QueryPointAndDistance(
				//	esriSegmentExtension.esriNoExtension, point, false, retVal,
				//	ref distAlongCurve, ref distFromCurve, ref rightSide);

				MapPoint retVal = GeometryEngine.Instance.QueryPointAndDistance(
					perimeter, SegmentExtensionType.NoExtension, point, AsRatioOrLength.AsRatio,
					out var distAlongCurve, out var distFromCurve, out var onRightSide);

				return GeometryUtils.AreEqualInXY(point, retVal);
			}

			/// <summary>
			/// Calculates the distance of the fragments start- (useStartPoint=TRUE)
			/// or endpoint (useStartPoint=FALSE) with the given point.
			/// </summary>
			/// <param name="point">Point for calculating the distance</param>
			/// <param name="useStartPoint">Flag if the FromPoint (TRUE) or the ToPoint
			///                             (FALSE) should be used to calculate the distance</param>
			/// <returns>The distance of the two points</returns>
			public double GetPointDistance([NotNull] MapPoint point, bool useStartPoint)
			{
				LineSegment tmpLine = GetLine(point, useStartPoint);
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
			public LineSegment GetLine(MapPoint toPoint, bool useStartPoint)
			{
				return LineBuilderEx.CreateLineSegment(toPoint, useStartPoint
					                                           ? _pointColl[0]
					                                           : _pointColl[_nrPoints - 1], toPoint.SpatialReference);
			}

			/// <summary>
			/// Checks if a given point exists (same coordinates) in
			/// the stored fragment
			/// </summary>
			/// <param name="point">Point to check</param>
			/// <returns>TRUE if the point is found, FALSE otherwise</returns>
			public bool PointExistsOnFragment([NotNull] MapPoint point)
			{
				//_pointColl
				AttributeFlags attributeFlags = point.GetAttributeFlags();
				MultipointBuilderEx geometryBuilder = new MultipointBuilderEx(_pointColl, attributeFlags);

				MapPointBuilderEx hitPoint = new MapPointBuilderEx().Configure(point);
				return HitTestPointZs(geometryBuilder.ToGeometry(), point, GeometryUtils.GetXyTolerance(point), 0,
				                      true,
				                      hitPoint);
			}


		}

		#endregion Nested class LineFragment

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
						if (!_usedFragments[i])
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
			public LineFragmentsCache([NotNull] Polyline lineParts,
			                          [NotNull] Polygon perimeter,
			                          [NotNull] Polyline sourceLine)
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
			private void GetPartsFromPolyline([NotNull] Polyline line,
			                                  [NotNull] Polygon perimeter,
			                                  [NotNull] Polyline sourceLine)
			{
				// Reset members
				_fragments.Clear();
				_usedFragments.Clear();
				_fragmentCount = GeometryUtils.GetPartCount(line);

				// Get parts from polyline
				foreach (Polyline geometry in GeometryUtils.GetParts(line))
				{
					_fragments.Add(new LineFragment(geometry.Points, perimeter));
					_usedFragments.Add(false);
				}

				ReorderFragments(sourceLine);

				// Setting the endPoint holding flag
				// Because we reordered the parts, only the last part must be checked
				// if he holds the endPoint
				_fragments[_fragmentCount - 1].HoldsEndPoint =
					_fragments[_fragmentCount - 1].PointExistsOnFragment(GeometryUtils.GetEndPoint(sourceLine));
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
					if (!_usedFragments[idx])
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
					if (!_usedFragments[idx])
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
					if (!_usedFragments[idx])
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
					if (!_usedFragments[idx])
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
			private void ReorderFragments([NotNull] Polyline sourceLine)
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

			private void EnsureOrientationAndSetDistanceAlong([NotNull] Polyline sourceLine)
			{
				int size = _fragments.Count;

				for (var partIdx = 0; partIdx < size; partIdx++)
				{
					LineFragment fragment = _fragments[partIdx];

					Polyline fragmentCurve = PolylineBuilderEx.CreatePolyline(fragment.Points);
					MapPoint fragmentCurveFromPoint = GeometryUtils.GetStartPoint(fragmentCurve);
					MapPoint fragmentCurveToPoint = GeometryUtils.GetEndPoint(fragmentCurve);

					MapPoint sourceLineFromPoint = GeometryUtils.GetStartPoint(sourceLine);
					MapPoint sourceLineToPoint = GeometryUtils.GetEndPoint(sourceLine);

					// Ensure orientation
					if (GeometryUtils.IsClosed(sourceLine))
					{
						if (GeometryUtils.AreEqual(sourceLineFromPoint,
						                           fragmentCurveFromPoint))
						{
							// special case: distance along also returns 0 for the tail
							// -> use the mid-point instead of the coincident start/end point.
							fragmentCurveFromPoint = GeometryEngine.Instance.QueryPoint(fragmentCurve,
							                                   SegmentExtensionType.NoExtension,
							                                   0.5, AsRatioOrLength.AsRatio);

						}
						else if (GeometryUtils.AreEqual(sourceLineFromPoint,
						                                fragmentCurveToPoint))
						{
							fragmentCurveToPoint = GeometryEngine.Instance.QueryPoint(fragmentCurve,
								SegmentExtensionType.NoExtension,
								0.5, AsRatioOrLength.AsRatio);

						}
					}

					double distanceToFromPoint = GetDistanceAlong(sourceLine, fragmentCurveFromPoint);
					double distanceToToPoint = GetDistanceAlong(sourceLine, fragmentCurveToPoint);

					if (distanceToFromPoint > distanceToToPoint)
					{
						// The line was flipped either by ITopologicalOperator or the postprocessing split
						fragmentCurve = GeometryUtils.ReverseOrientation(fragmentCurve);

						//write new Pointorder back
						fragment.Points = fragmentCurve.Points;
					}

					fragment.StartDistanceAlongSourceLine = distanceToFromPoint;
				}
			}

			public static double GetDistanceAlong(Polyline curve, MapPoint point,
			                                           bool asRatio = false)
			{
				const SegmentExtensionType extension = SegmentExtensionType.NoExtension;

				AsRatioOrLength asRatioOrLength =
					asRatio ? AsRatioOrLength.AsRatio : AsRatioOrLength.AsLength;

				GeometryEngine.Instance.QueryPointAndDistance(
					curve, extension, point, asRatioOrLength,
					out double distanceAlong, out _, out _);
				return distanceAlong;
			}


			#endregion Public methods

		}

		#endregion Nested class LineFragmentsCache

		private enum LinesFlippedState
		{
			SameDirection,
			InversedDirection,
			Unsure
		}

		#region Fields

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static object _missingRef = Type.Missing;

		#endregion Fields

		#region Public methods

		[CanBeNull]
		public static Multipoint MergeStates(
			[NotNull] Multipoint thisState,
			[NotNull] Multipoint otherState,
			[NotNull] Polygon perimeter)
		{
			if (thisState == null || otherState == null)
			{
				return null;
			}

			Multipoint thisPart = GetMultipointPartByPerimeter(thisState, perimeter, true);
			Multipoint otherPart = GetMultipointPartByPerimeter(otherState, perimeter, false);

			if (thisPart.IsEmpty)
			{
				return otherPart;
			}

			if (otherPart.IsEmpty)
			{
				return thisPart;
			}

			IList<Multipoint> geometries = new List<Multipoint> { thisPart, otherPart };
			Multipoint outMultipoint = GeometryUtils.Union(geometries) as Multipoint;

			if (outMultipoint == null || outMultipoint.IsEmpty)
			{
				return null;
			}

			outMultipoint = GeometryUtils.Simplify(outMultipoint, true);

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
		public static Polyline MergeStates(
			[NotNull] Polyline thisState,
			[NotNull] Polyline otherState,
			[NotNull] Polygon perimeter,
			[CanBeNull] PolylineBuilderEx discontinuityLines = null,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(thisState, nameof(thisState));
			Assert.ArgumentNotNull(otherState, nameof(otherState));
			Assert.ArgumentNotNull(perimeter, nameof(perimeter));

			// TODO: properly support multipart polylines by associating parts with each other
			//       and by calling MergeStates for each part-pair. -> only return null if part count differs.
			if (!CheckMultiparts(thisState, otherState, notifications))
			{
				return null;
			}

			switch (AreLinesFlipped(thisState, otherState))
			{
				case LinesFlippedState.SameDirection:
					//nothing to do
					break;

				case LinesFlippedState.InversedDirection:
					otherState = GeometryUtils.ReverseOrientation(otherState);
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

			//GeometryUtils.ToXmlFile(thisState, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "pro_thisState.xml"));
			//GeometryUtils.ToXmlFile(otherState, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "pro_otherState.xml"));

			Polyline thisParts = GetLinePartsByPerimeter(thisState, perimeter, true);
			Polyline otherParts = GetLinePartsByPerimeter(otherState, perimeter, false);

			//GeometryUtils.ToXmlFile(thisParts, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "pro_thisParts.xml"));
			//GeometryUtils.ToXmlFile(otherParts, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "pro_otherParts.xml"));

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
			Polyline newLine = MergeLineParts(thisState, thisParts, otherState, otherParts,
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
		public static Polygon MergeStates(
			[NotNull] Polygon thisState,
			[NotNull] Polygon otherState,
			[NotNull] Polygon perimeter,
			[CanBeNull] PolylineBuilderEx discontinuityLines = null,
			[CanBeNull] NotificationCollection notifications = null,
			bool removeDiscontinuitiesInResult = true)
		{

			//var builder = new PolylineBuilderEx().Configure(discontinuityLines);
			//var geometryCollection = (Multipart)discontinuityLines.ToGeometry();

			Polygon thisParts = GetPolygonPartsByPerimeter(thisState, perimeter, true);
			Polygon otherParts = GetPolygonPartsByPerimeter(otherState, perimeter, false);

			//thisParts = GeometryUtils.Simplify(thisParts, true);
			//otherParts = GeometryUtils.Simplify(otherParts, true);

			GeometryUtils.ToXmlFile(thisParts, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "pro_thisParts.xml"));
			GeometryUtils.ToXmlFile(otherParts, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "pro_otherParts.xml"));

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

			Polygon newPoly = MergePolygonParts(thisParts, otherParts);
			GeometryUtils.ToXmlFile(newPoly, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "pro_newPoly.xml"));

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
				newPoly = RemoveProcessGeneratedPoints(newPoly, thisState, otherState, perimeter,
				                                        notifications);
			}
			GeometryUtils.ToXmlFile(newPoly, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "pro_newPolyPointsRemoved.xml"));

			return ValidatePolygon(newPoly, notifications);
		}

		#endregion Public methods

		#region Private methods

		#region Private methods (MergeEditedVertexes)
		#endregion Private methods (MergeEditedVertexes)

		#region Private methods (Merge multipoint)

		[NotNull]
		private static Multipoint GetMultipointPartByPerimeter([NotNull] Multipoint multipoint,
		                                                       [NotNull] Polygon perimeter, bool insidePart)
		{
			Multipoint newMultipoint;
			if (insidePart)
			{
				_msg.VerboseDebug(
					() =>
						$"Getting inside part of multipoint: {GeometryUtils.ToString(multipoint)}");

				// 10.2.2: The Z values come from the first geometry, even if it is a polygon and the results are multipoints!
				GeometryDimensionType dimension = GetGeometryDimensionType(multipoint);
				newMultipoint = GeometryUtils.Intersection(perimeter,
				                                           multipoint, dimension) as Multipoint;
			}
			else
			{
				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.VerboseDebug(
						() =>
							$"Getting outside part of multipoint: {GeometryUtils.ToString(multipoint)}");
				}

				// Starting with 10.2.2 difference works now also with multipoints (with a handful of workarounds in IntersectionUtils)
				newMultipoint = GeometryUtils.Difference(multipoint, perimeter) as Multipoint;
			}

			Assert.NotNull(newMultipoint, "Merge result is null or not a multipart");

			MultipointBuilderEx newMultipointBuilder = new MultipointBuilderEx(newMultipoint);
			newMultipointBuilder.HasID = multipoint.HasID;
			newMultipointBuilder.HasM = multipoint.HasM;
			newMultipointBuilder.HasZ = multipoint.HasZ;
			newMultipoint = newMultipointBuilder.ToGeometry();

			return newMultipoint;
		}

		#endregion Private methods (Merge multipoint)

		#region Private methods (Merge polyline)

		/// <summary>
		/// Checks if one or both of the given lines are multipart
		/// </summary>
		/// <param name="thisState">State of the this.</param>
		/// <param name="otherState">State of the other.</param>
		/// <param name="notifications">The notifications.</param>
		/// <returns></returns>
		private static bool CheckMultiparts([NotNull] Polyline thisState,
		                                    [NotNull] Polyline otherState,
		                                    [CanBeNull] NotificationCollection notifications)
		{

			int thisGeometryCount = thisState.PartCount;
			int otherGeometryCount = otherState.PartCount;
			//int thisGeometryCount = ((IGeometryCollection)thisState).GeometryCount;
			//int otherGeometryCount = ((IGeometryCollection)otherState).GeometryCount;

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
		private static LinesFlippedState AreLinesFlipped([NotNull] Polyline line1,
														 [NotNull] Polyline line2)
		{
			double xyTolerance = line1.SpatialReference.XYTolerance;
			MapPoint fromPoint1 = GeometryUtils.GetStartPoint(line1);
			MapPoint toPoint1 = GeometryUtils.GetEndPoint(line1);
			MapPoint fromPoint2 = GeometryUtils.GetStartPoint(line2);
			MapPoint toPoint2 = GeometryUtils.GetEndPoint(line2);

			bool isSameFromPoint = GeometryUtils.IsSamePointXY(fromPoint1, fromPoint2, xyTolerance);
			bool isSameToPoint = GeometryUtils.IsSamePointXY(toPoint1, toPoint2, xyTolerance);

			//if (GeometryUtils.EndPointsAreEqual(line1, line2))
			if (isSameFromPoint && isSameToPoint)
			{
				return LinesFlippedState.SameDirection;
			}

			// Approach 2: If approach 1 is not satisfactory, check if any of the
			// vertexes of the first line can be found in the seconde line,
			// if more than one is found, the relative order of the points must be the
			// same in both lines.
			int thisPntPos = -1, thisPntPos2 = -1;
			int otherPntPos = -1;
			//var thisPntColl = (IPointCollection)line1;
			//var otherPntColl = (IPointCollection)line2;
			ReadOnlyPointCollection thisPntColl = line1.Points;
			ReadOnlyPointCollection otherPntColl = line2.Points;
			int tSize = thisPntColl.Count;
			int oSize = otherPntColl.Count;



			for (var tPntIdx = 0; tPntIdx < tSize; tPntIdx++)
			{
				//var pntRelOp = (IRelationalOperator)thisPntColl.Point[tPntIdx];
				MapPoint thisPnt = thisPntColl[tPntIdx];

				for (var oPntIdx = 0; oPntIdx < oSize; oPntIdx++)
				{
					MapPoint otherPnt = otherPntColl[oPntIdx];

					if(! ProSuite.Commons.AGP.Core.Spatial.GeometryUtils.AreEqualInXY(thisPnt, otherPnt))
					//if (!pntRelOp.Equals(otherPntColl.Point[oPntIdx]))
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

					if ((thisPntPos2 - thisPntPos > 0) == (otherPntPos2 - otherPntPos > 0))
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
				double relativeThisPos = tSize / (double)thisPntPos;
				double relativeOtherPos = oSize / (double)otherPntPos;
				if ((relativeThisPos < 2) == (relativeOtherPos < 2))
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
		private static Polyline GetLinePartsByPerimeter([NotNull] Polyline line,
														 [NotNull] Polygon perimeter,
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

			Polyline newLine;
			if (insideParts)
			{
				_msg.VerboseDebug(() => $"Getting inside part of line: {GeometryUtils.ToString(line)}");

				//var topoOp4 = (ITopologicalOperator4)perimeter;

				// if it does not intersect -> empty polygon is returned!
				//Geometry intersectGeometry = topoOp4.Intersect(line, line.Dimension);
				GeometryDimensionType dimension = GetGeometryDimensionType(line);
				Geometry intersectGeometry = GeometryUtils.Intersection(perimeter, line, dimension);

				if (intersectGeometry.IsEmpty)
				{
					// return empty geometry but of the correct type
					newLine = PolylineBuilderEx.CreatePolyline();
				}
				else
				{
					newLine = (Polyline)intersectGeometry;
				}
			}
			else
			{
				_msg.VerboseDebug(() => $"Getting outside part of line: {GeometryUtils.ToString(line)}");

				//var topoOp4 = (ITopologicalOperator)line;
				//newLine = (Polyline)topoOp4.Difference(perimeter);
				newLine = GeometryUtils.Difference(line, perimeter) as Polyline;
			}

			newLine = EnsureFromToForClosedLines(line, perimeter, newLine, insideParts);
			//newLine was changed

			PolylineBuilderEx newLineBuilder = new PolylineBuilderEx((Polyline)newLine);
			newLineBuilder.HasID = line.HasID;
			newLineBuilder.HasM = line.HasM;
			newLineBuilder.HasZ = line.HasZ;

			newLine = newLineBuilder.ToGeometry();

			return newLine;
		}

		private static Polyline EnsureFromToForClosedLines([NotNull] Polyline originalLine,
		                                               [NotNull] Polygon perimeter,
		                                               [NotNull] Polyline lineParts,
		                                               bool insideParts)
		{
			// Correct the problem described above - line parts can be merged if they originate from a closed line.
			if (! originalLine.IsClosed())
			{
				return lineParts;
			}

			_msg.DebugFormat(
				"Post-processing Topo-Op for closed lines. InsideParts: {0}", insideParts);
			// if the inside parts (intersect) were extracted and the end point is inside
			// or if the outside parts (difference) were extracted and the end point is on the outside part...
			MapPoint originalLineToPoint = GeometryUtils.GetEndPoint(originalLine);
			//bool contains = GeometryUtils.Contains(perimeter, endPoint);
			//bool disjoint = GeometryUtils.Disjoint(perimeter, endPoint);
			if (insideParts && GeometryUtils.Contains(perimeter, originalLineToPoint) ||
			    ! insideParts && GeometryUtils.Disjoint(perimeter, originalLineToPoint))
			{
				// check each of ne new line parts if one of them holds the original end point
				// if not: it was changed by topologicaloperator
				var endPointChanged = true;
				foreach (var geometry in GeometryUtils.GetParts(lineParts))
				{
					var newLinePart = (Polyline) geometry;
					if (GeometryUtils.AreEqualInXY(originalLineToPoint,
					                               GeometryUtils.GetEndPoint(newLinePart)))
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

					//lineParts.SplitAtPoint(originalLine.ToPoint, false, true,
					//                       out bool _, out int _, out int _);

					Multipoint multipoint =
						MultipointBuilderEx.CreateMultipoint(originalLineToPoint);

					Multipart modifiedLineParts;
					IList<MapPoint> splitPoints =
						GeometryUtils.SplitPolycurve(
							lineParts as Multipart, multipoint, false, true,
							GeometryUtils.GetXyTolerance(lineParts), out modifiedLineParts);

					return (Polyline)modifiedLineParts;
				}

			}

			return originalLine;
		}

		/// <summary>
		/// Checks if the given line is valid (not empty, 0 length, mutlipart or selfintersecting)
		/// </summary>
		[CanBeNull]
		private static Polyline ValidateLine([NotNull] Polyline line,
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

			//bei self-intersections erzeugt SimplifyType.Planar neue Parts analog zu ArcObjects
			//var lineTopoOp = (ITopologicalOperator3)line;
			//lineTopoOp.IsKnownSimple_2 = false;
			//lineTopoOp.Simplify();

			line = GeometryUtils.Simplify(line, SimplifyType.Planar, true);

			// simplify could have made the geometry empty (or only now .IsEmpty returns
			// the correct result)
			if (line.IsEmpty)
			{
				const string message = "The line has become empty by simplifying it.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return null;
			}

			// MAware geometry wont result several geometries after simplify, 
			// check possible selfintersection separately
			bool valid = CheckPossibleMAwareMultipart(line);

			if (!valid)
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
			//if (((IGeometryCollection) line).GeometryCount > 1)
			if (line.PartCount > 1)
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

		private static bool CheckPossibleMAwareMultipart([NotNull] Polyline geometry)
		{
			if (!GeometryUtils.IsMAware(geometry))
			//if (!(geometry is IMAware mAware) || mAware.MAware == false)
			{
				return true; // no special check needed
			}

			var unawareGeometry = (Polyline)GetSimplifiedMUnawareGeometry(geometry);

			//return unawareGeometry != null &&
			//       ((IGeometryCollection)unawareGeometry).GeometryCount == 1;
			return unawareGeometry != null && unawareGeometry.PartCount == 1;
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
		private static Polyline MergeLineParts(
			[NotNull] Polyline thisLine, [NotNull] Polyline thisParts,
			[NotNull] Polyline otherLine, [NotNull] Polyline otherParts,
			[NotNull] Polygon perimeter, [CanBeNull] PolylineBuilderEx discontinuities)
		{
			PolylineBuilderEx newLineBuilderEx = new PolylineBuilderEx().Configure(thisLine);

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
				Polyline checkLine = thisPartsCache.Fragments.Contains(currentPart)
					                     ? thisLine
					                     : otherLine;

				AddPointsToCollection(newLineBuilderEx, currentPart.Points, checkLine);

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

			return newLineBuilderEx.ToGeometry();
		}

		/// <summary>
		/// Checks whether the start point of this line is inside the perimeter or not.
		/// </summary>
		/// <returns>TRUE if startpoint of thisLine lies inside the perimeter</returns>
		private static bool DoesThisLineStartInside([NotNull] Polyline thisLine,
		                                            [NotNull] Polyline otherLine,
		                                            [NotNull] Polygon perimeter)
		{
			bool thisInside =
				GeometryUtils.Contains(perimeter, GeometryUtils.GetStartPoint(thisLine)); //FromPoint
			bool otherInside = GeometryUtils.Contains(perimeter, GeometryUtils.GetStartPoint(otherLine)); //FromPoint

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

		[CanBeNull]
		private static LineFragment GetNextFragment(
			[NotNull] LineFragment currentPart,
			[NotNull] LineFragmentsCache thisPartsCache,
			[NotNull] LineFragmentsCache otherPartsCache,
			[NotNull] PolylineBuilderEx discontinuityLines)
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
				thisPart.Points[0], false);
			double otherDist = currentPart.GetPointDistance(
				otherPart.Points[0], false);

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
			[NotNull] PolylineBuilderEx discontinuities)
		{
			// TODO: move the checking to the AddPointsToCollection method (remembering
			//       the last fragment's cut-point with the perimeter line.
			// TODO: handle the 3D-issues: test if the line.Length is really the 3D length.

			if (discontinuities == null)
			{
				return;
			}
			LineSegment line = currentPart.GetLine(nextPart.Points[0], false);

			if (line.Length > 0) // _minDiscontinuityLenth)
			{
				//Polyline singleLine = PolylineBuilderEx.CreatePolyline(line);
				discontinuities.AddSegment(line, true);

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
		private static void AddPointsToCollection([NotNull] PolylineBuilderEx pointColl,
		                                          [NotNull] ReadOnlyPointCollection addPoints,
												  [NotNull] Polyline checkLine)
		{
			//Polyline geometry = pointColl.ToGeometry();

			bool pointCollIsEmpty = pointColl.IsEmpty;
			IList<MapPoint> collMapPoints = new List<MapPoint>();

			int nrPoints = addPoints.Count;
			for (var pntIdx = 0; pntIdx < nrPoints; pntIdx++)
			{
				MapPoint addPoint = addPoints[pntIdx];
				// Start or endpoint, check before adding
				if (pntIdx == 0 || pntIdx == nrPoints - 1)
				{
					if (PointExistsInGeometry(addPoint, checkLine))
					{
						if (pointCollIsEmpty)
						{
							collMapPoints.Add(addPoint);
						}
						else
						{
							//geometry = (Polyline)GeometryUtils.AddVertex(geometry, addPoint, out int partIndex, out int vertexIndex);
							GeometryUtils.AddVertex(pointColl, addPoint, 0);
						}
					}
				}
				// Normally add the point
				else
				{
					if (pointCollIsEmpty)
					{
						collMapPoints.Add(addPoint);
					}
					else
					{
						//geometry = (Polyline)GeometryUtils.AddVertex(geometry, addPoint, out int partIndex, out int vertexIndex);
						GeometryUtils.AddVertex(pointColl, addPoint, 0);
					}
				}
			}

			if (pointCollIsEmpty && collMapPoints.Count > 0)
			{
				pointColl.AddPart(collMapPoints);
			}
		}

		#endregion Private methods (Merge polyline)

		#region Private methods (Merge polygon)

		/// <summary>
		/// Cuts the polygon by the perimeter border.
		/// If insidePart = TRUE, the part of the polygon inside the perimeter is returned,
		/// if insidePart = FALSE, the part of the polygon outside the perimeter is returned.
		/// </summary>
		[CanBeNull]
		private static Polygon GetPolygonPartsByPerimeter([NotNull] Polygon polygon,
		                                                  [NotNull] Polygon perimeter,
		                                                  bool insideParts)
		{
			Polygon newPolygon = null;
			if (insideParts)
			{
				_msg.VerboseDebug(
					() => $"Getting inside part of polygon: {GeometryUtils.ToString(polygon)}");

				//var topoOp4 = (ITopologicalOperator4)perimeter;
				//newPolygon = topoOp4.Intersect(polygon, polygon.Dimension) as IPolygon;
				GeometryDimensionType dimension = GetGeometryDimensionType(polygon);
				newPolygon = GeometryUtils.Intersection(perimeter, polygon, dimension) as Polygon;
			}
			else
			{
				_msg.VerboseDebug(
					() => $"Getting outside part of polygon: {GeometryUtils.ToString(polygon)}");

				//var topoOp4 = (ITopologicalOperator)polygon;
				//newPolygon = topoOp4.Difference(perimeter) as IPolygon;
				newPolygon = GeometryUtils.Difference(polygon, perimeter) as Polygon;
			}

			PolygonBuilderEx newPolygonBuilder = new PolygonBuilderEx((Polygon)newPolygon);
			newPolygonBuilder.HasID = polygon.HasID;
			newPolygonBuilder.HasM = polygon.HasM;
			newPolygonBuilder.HasZ = polygon.HasZ;

			newPolygon = newPolygonBuilder.ToGeometry();


			return newPolygon;
		}

		[CanBeNull]
		private static Polygon MergePolygonParts([NotNull] Polygon thisPart,
		                                 [NotNull] Polygon otherPart)
		{
			IList<Polygon> geometries = new List<Polygon> { thisPart, otherPart };
			var newPolygon = GeometryUtils.Union(geometries) as Polygon;

			return newPolygon;
		}

		private static void CheckDiscontinuity([NotNull] Geometry unionPoly,
		                                       [NotNull] Polygon perimeter,
		                                       [CanBeNull] PolylineBuilderEx discontinuities)
		{
			if (discontinuities == null)
			{
				return;
			}

			// get the segments of the union poly that are coincident with the perimeter
			Geometry intersection =
				GeometryUtils.Intersection(perimeter, unionPoly,
				                           GeometryDimensionType.EsriGeometry1Dimension);

			if (intersection.IsEmpty)
			{
				return;
			}

			Polyline polyline = intersection as Polyline;
			PolylineBuilderEx lineColl = new PolylineBuilderEx(polyline);

			// TODO: filter out those who were on the line in the first place
			foreach (Geometry geometry in GeometryUtils.GetParts(polyline))
			{
				var discontinuityLine = (Polyline)geometry;
				if (discontinuityLine.Length > 0)
				{
					discontinuities.AddPart(discontinuityLine.Parts[0]);
				}
				//ICollection<Segment> segments = null;
				//discontinuityLine.GetAllSegments(ref segments);

				//foreach (Segment segment in segments)
				//{
				//	if (segment.Length > 0) //_minDiscontinuityLenth)
				//	{
				//		discontinuities.AddSegment(segment);
				//	}
				//}
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
		private static Polygon RemoveProcessGeneratedPoints([NotNull] Polygon unionPoly,
		                                                 [NotNull] Polygon thisState,
		                                                 [NotNull] Polygon otherState,
		                                                 [NotNull] Polygon perimeter,
														 [CanBeNull] NotificationCollection notifications)
		{
			IList<MapPoint> pointsToRemove = new List<MapPoint>();

			int intersectingVertices = 0, removedVertices = 0;

			Polyline perimeterBoundary = GeometryFactory.CreatePolyline(perimeter);
			//GeometryUtils.ToXmlFile(perimeterBoundary, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "pro_perimeterBoundary.xml"));

			bool addNotification = false;

			//IGeometryBag componentBag = IPolygon4.ConnectedComponentBag;
			//IGeometryCollection componentBagCol = (IGeometryCollection) componentBag;
			// Multipart, 3 connected Components aber 4 Parts (Rings)
			//ConnectedComponentBag ergibt 3 Polygone 
			//   /\      /\      /\
			//  /  \    /  \    /  \
			// |    |  |    |  | /\ |
			//  \  /    \  /    \\//
			//   \/      \/      \/

			foreach (Polygon polygonPart in GeometryUtils.ConnectedComponents(unionPoly))
			{
				//Multipoint intersectionPoints = IntersectionUtils.GetIntersectionPoints(perimeterBoundary, polygonPart);
				// ConnectLineCalculatorBase.GetCrossingPointCount

				if (GeometryEngine.Instance.Crosses(perimeterBoundary, polygonPart))
				{
					var intersections = IntersectionUtils.GetIntersectionPoints(perimeter, polygonPart);
					_msg.DebugFormat(
						"{0} intersection points between perimeter and union-poly part",
						intersections.PointCount);

					foreach (MapPoint queryPoint in intersections.Points)
					{
						System.Diagnostics.Debug.WriteLine($"{queryPoint.X}, {queryPoint.Y}");
						if (PointExistsInGeometry(queryPoint, polygonPart))
						{
							// it's a real vertex in the union poly
							intersectingVertices++;

							if (!(PointExistsInGeometry(queryPoint, thisState) ||
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

			pointsToRemove = pointsToRemove.Reverse().ToList();
			//Multipoint multipoint = GeometryFactory.CreateMultipoint(pointsToRemove);
			//GeometryUtils.ToXmlFile(multipoint, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "pro_pointsToRemove.xml"));
			foreach (MapPoint point in pointsToRemove)
			{
				System.Diagnostics.Debug.WriteLine($"{point.X}, {point.Y}");
			}

			MultipartBuilderEx unionPolyBuilder = unionPoly.ToBuilder();
			RemoveCutPointsService.RemovePoints(unionPolyBuilder, pointsToRemove);
			return (Polygon)unionPolyBuilder.ToGeometry();
		}

		[CanBeNull]
		private static Polygon ValidatePolygon([NotNull] Polygon polygon,
		                                       [CanBeNull] NotificationCollection notifications)
		{
			// To make sure IsEmpty returns the correct result simplify first
			// For example the polygons could have been disjoint originally.
			GeometryUtils.ToXmlFile(polygon, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "proPolygon.xml"));
			Polygon simplifiedPolygon = GeometryUtils.Simplify(polygon, true);
			GeometryUtils.ToXmlFile(simplifiedPolygon, System.IO.Path.Combine("C:\\Temp\\TestGeometry", "proSimplifiedPolygon.xml"));

			if (simplifiedPolygon.IsEmpty)
			{
				// simplify could have made the geometry empty (or only now .IsEmpty returns
				// the correct result)

				const string message = "The polygon has become empty by simplifying it.";
				NotificationUtils.Add(notifications, message);
				_msg.Debug(message);
				return null;
			}

			// Check multipart has only one positive Area Part (excluding holes)
			bool areaValid = HasOnlyOnePositiveAreaPart(simplifiedPolygon);

			if (!areaValid)
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

			return simplifiedPolygon;
		}

		public static bool HasOnlyOnePositiveAreaPart([NotNull] Polygon polygon)
		{
			if (polygon is null || polygon.IsEmpty)
			{
				return false;
			}

			var positiveParts = 0;
			var flags = polygon.GetAttributeFlags();
			var sref = polygon.SpatialReference;
			int partCount = polygon.Parts.Count;
			for (int i = 0; i < partCount; i++)
			{
				var segments = polygon.Parts[i];
				var part = PolygonBuilderEx.CreatePolygon(segments, flags, sref);
				if (part.Area > 0)
				{
					positiveParts++;
				}
				if (positiveParts > 1)
				{
					_msg.DebugFormat(
						"The tested geometry holds {0} positive parts.\n" +
						"Only 1 positive part is allowed", positiveParts);
					return false;
				}
			}

			return positiveParts > 0;


			//List<Geometry> singleParts = new List<Geometry>();
			//foreach (var polygonPart in polygon.Parts)
			//{
			//	// use the PolygonBuilder turning the segments into a standalone 
			//	// polygon instance
			//	singleParts.Add(PolygonBuilderEx.CreatePolygon(polygonPart));
			//}

			//// get an enumeration of clockwise geometries (area > 0) ordered by the area
			//var clockwiseParts = singleParts.Where(geom => ((Polygon)geom).Area > 0)
			//                                .OrderByDescending(geom => ((Polygon)geom).Area);
			//if (clockwiseParts.Any())
			//{
			//	if (clockwiseParts.Count() > 1)
			//	{
			//		_msg.DebugFormat(
			//			"The tested geometry holds {0} positive parts.\n" +
			//			"Only 1 positive part is allowed", clockwiseParts.Count());
			//		return false;
			//	}
			//	return true;
			//}
			//return false;
		}

		#endregion Private methods (Merge polygon)

		/// <summary>
		/// Checks if the given point does exist in the given geometry.
		/// </summary>
		private static bool PointExistsInGeometry([NotNull] MapPoint point,
		                                          [NotNull] Geometry geometry)
		{
			try
			{

				MapPointBuilderEx hitPoint = new MapPointBuilderEx().Configure(point);
				return HitTestPointZs(geometry, point, GeometryUtils.GetXyTolerance(point), 0, true,
				                      hitPoint);

				//double xyTolerance = GeometryUtils.GetXyTolerance(point);
				////Geometry hitGeometryPart = GeometryUtils.GetHitGeometryPart(point, geometry, xyTolerance);
				//double distance = GeometryEngine.Instance.Distance(geometry, point);
				////double geodesicDistance = GeometryEngine.Instance.GeodesicDistance(geometry, point);

				//ProximityResult proximity = GeometryEngine.Instance.NearestPoint(geometry, point);
				//int partIndex = proximity.PartIndex;
				//if (proximity.Distance <= xyTolerance)
				//{
				//	return true;
				//}

				//return false;
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
				return false;
			}
		}

		/// <summary>
		///  Get Mless version of MAware geometry
		/// </summary>
		[CanBeNull]
		private static Geometry GetSimplifiedMUnawareGeometry([NotNull] Geometry geometry)
		{
			Geometry unawareGeometry = GeometryFactory.Clone(geometry);

			GeometryBuilderEx geometryBuilderEx = unawareGeometry.ToBuilder();
			geometryBuilderEx.HasM = false;
			//var mUnaware = (IMAware)unawareGeometry;
			//mUnaware.MAware = false;

			unawareGeometry = GeometryUtils.Simplify(unawareGeometry, true);

			// simplify could have made the geometry empty (or only now .IsEmpty returns
			// the correct result
			if (unawareGeometry.IsEmpty)
			{
				_msg.Debug("The geometry has become empty by simplifying it.");
				return null;
			}

			return unawareGeometry;
		}


		private static bool HitTestPointZs([NotNull] Geometry geometry,
		                                   [NotNull] MapPoint queryPoint,
		                                   double xyTolerance,
		                                   double zTolerance,
		                                   bool ignoreZ,
		                                   [NotNull] MapPointBuilderEx hitPoint)
		{
			//HitTestWksPointZs
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(queryPoint, nameof(queryPoint));
			Assert.ArgumentNotNull(hitPoint, nameof(hitPoint));
			//Assert.True(geometry is Multipart,
			//			"Only geometries supporting Multipart " +
			//			"can be used with HitTestPointZs.");

			ReadOnlyPointCollection points = null;
			if (geometry is Multipart multipart)
			{
				points = multipart.Points;
			}
			else if (geometry is Multipoint multipoint)
			{
				points = multipoint.Points;
			}
			else if (geometry is Polyline polyline)
			{
				points = polyline.Points;
			}
			else if (geometry is Polygon polygon)
			{
				points = polygon.Points;
			}

			double queryX = queryPoint.X;
			double queryY = queryPoint.Y;
			double queryZ = queryPoint.Z;

			double minDistance = double.NaN;

			foreach (MapPoint point in points)
			{
				double distance;
				bool isInTolerance = IsInTolerance(point, queryX, queryY, queryZ,
				                                   xyTolerance, zTolerance,
				                                   ignoreZ, out distance);

				if (!isInTolerance)
				{
					continue;
				}

				if (!double.IsNaN(minDistance) && distance >= minDistance)
				{
					continue;
				}

				minDistance = distance;

				hitPoint.X = point.X;
				hitPoint.Y = point.Y;
				hitPoint.Z = point.Z;
			}

			return !double.IsNaN(minDistance);
		}

		private static bool IsInTolerance(MapPoint p1,
		                                  double p2X, double p2Y, double p2Z,
		                                  double xyTolerance, double zTolerance,
		                                  bool ignoreZ,
		                                  out double distance)
		{
			return IsInTolerance(p1.X, p1.Y, p1.Z,
			                     p2X, p2Y, p2Z,
			                     xyTolerance, zTolerance, ignoreZ, out distance);
		}

		private static bool IsInTolerance(double x1, double y1, double z1,
		                                 double x2, double y2, double z2,
		                                 double xyTolerance, double zTolerance,
		                                 bool ignoreZ, out double distance)
		{
			double xDistance = Math.Abs(x1 - x2);
			double yDistance = Math.Abs(y1 - y2);

			double xyDistance = Math.Sqrt(xDistance * xDistance + yDistance * yDistance);

			if (ignoreZ)
			{
				distance = xyDistance;
				return xyDistance < xyTolerance;
			}

			double zDistance = Math.Abs(z1 - z2);

			distance = Math.Sqrt(xyDistance * xyDistance + zDistance * zDistance);
			return xyDistance < xyTolerance && zDistance < zTolerance;
		}

		private static GeometryDimensionType GetGeometryDimensionType(Geometry geometry)
		{
			int geometryDimension = geometry.Dimension;
			switch (geometryDimension)
			{
				case 0:
					return GeometryDimensionType.EsriGeometry0Dimension;
				case 1:
					return GeometryDimensionType.EsriGeometry1Dimension;
				case 2:
					return GeometryDimensionType.EsriGeometry2Dimension;
				default:
					return GeometryDimensionType.EsriGeometryUnknownDimension;
			}
		}

		#endregion Private methods

	}
}
