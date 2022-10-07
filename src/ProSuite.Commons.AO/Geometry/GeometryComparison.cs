using System;
using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using Array = System.Array;

namespace ProSuite.Commons.AO.Geometry
{
	public class GeometryComparison
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IGeometry _baseGeometry;
		private readonly IGeometry _compareGeometry;
		private readonly double _xyTolerance;
		private readonly double _zTolerance;

		private Dictionary<WKSPointZ, VertexIndex> _baseGeometryCoords3D;
		private Dictionary<WKSPointZ, VertexIndex> _compareGeometryCoords3D;

		private Dictionary<WKSPointZ, VertexIndex> _baseGeometryCoords2D;
		private Dictionary<WKSPointZ, VertexIndex> _compareGeometryCoords2D;

		// TODO: Consider some other indexed structure instead of dictionaries: IndexedPoints
		private Dictionary<WKSPointZ, List<VertexIndex>> _baseCoordinateDuplicates3D;
		private Dictionary<WKSPointZ, List<VertexIndex>> _compareCoordinateDuplicates3D;

		private Dictionary<WKSPointZ, List<VertexIndex>> _baseCoordinateDuplicates2D;
		private Dictionary<WKSPointZ, List<VertexIndex>> _compareCoordinateDuplicates2D;

		private IGeometry _highLevelCompareGeometry;

		private List<int> _baseGeometryPartStartIndex;
		private List<int> _compareGeometryPartStartIndex;

		private readonly IPoint _pointTemplate;

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryComparison"/> class.
		/// </summary>
		/// <param name="baseGeometry">The base geometry. Tt should be snappd to its spatial reference and not changed as long as this instance is in use.</param>
		/// <param name="compareGeometry">The geometry to compare with, in the same spatial reference as the base
		/// geometry. Tt should be snappd to its spatial reference and not changed as long as this instance is in use.</param>
		public GeometryComparison([NotNull] IGeometry baseGeometry,
		                          [NotNull] IGeometry compareGeometry)
			: this(baseGeometry,
			       compareGeometry,
			       GeometryUtils.GetXyResolution(baseGeometry),
			       GeometryUtils.GetZResolution(baseGeometry)) { }

		[Obsolete]
		public GeometryComparison([NotNull] WKSPointZ[] baseCoords,
		                          [NotNull] WKSPointZ[] compareCoords,
		                          ISpatialReference spatialReference,
		                          double xyTolerance, double zTolerance)
			: this(
				GeometryFactory.CreateMultipoint(baseCoords, spatialReference),
				GeometryFactory.CreateMultipoint(compareCoords, spatialReference),
				xyTolerance, zTolerance) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryComparison"/> class.
		/// </summary>
		/// <param name="baseGeometry">The base geometry. NOTE: it should be snapped to its spatial reference.</param>
		/// <param name="compareGeometry">The geometry to compare with, in the same spatial reference as the base
		/// geometry. NOTE: it should be snapped to its spatial reference.</param>
		/// <param name="xyTolerance"></param>
		/// <param name="zTolerance"></param>
		public GeometryComparison([NotNull] IGeometry baseGeometry,
		                          [NotNull] IGeometry compareGeometry,
		                          double xyTolerance, double zTolerance)
		{
			// NOTE: Do not use SpatialReferenceUtils.AreEqual with the parameters 
			//       comparePrecisionAndTolerance=true and compareVerticalCoordinateSystems= true because
			//       this also compares M resolution which can differ despite EnsureSpatialReference was called
			//       Additionally, M-comparison is currently not implemented.

			//       A specific comparison method should be provided that compares only the relevant aspects of the spatial reference
			//       -> M-resolution/tolerance only if the geometry is M-aware, etc.
			const bool comparePrecisionAndTolerance = true;
			const bool compareVerticalCoordinateSystems = false;

			Assert.ArgumentCondition(
				SpatialReferenceUtils.AreEqual(baseGeometry.SpatialReference,
				                               compareGeometry.SpatialReference,
				                               comparePrecisionAndTolerance,
				                               compareVerticalCoordinateSystems),
				"The spatial reference of the base and the compare geometries must be the same.");

			if (GeometryUtils.IsZAware(baseGeometry) &&
			    GeometryUtils.IsZAware(compareGeometry))
			{
				Assert.ArgumentCondition(
					MathUtils.AreEqual(GeometryUtils.GetZResolution(baseGeometry),
					                   GeometryUtils.GetZResolution(compareGeometry)),
					"The Z-Resolution of the base and the compare geometries must be the same.");
			}

			_baseGeometry = baseGeometry;
			_compareGeometry = compareGeometry;

			_xyTolerance = xyTolerance;
			_zTolerance = zTolerance;

			_pointTemplate = new PointClass();
			_pointTemplate.SpatialReference = _baseGeometry.SpatialReference;

			if (! double.IsNaN(_zTolerance))
			{
				SetZTolerance(_pointTemplate, _zTolerance);
			}
		}

		/// <summary>
		/// Calculates the different points between the base (source) geometry and the 
		/// compare (target) geometry. The result are the points that are in the
		/// source but not in the target.
		/// NOTE: This method performs well but potentially reports too many differences
		/// such as points closer than the tolerance that get 'snapped' to a different
		/// value on the tolerance-grid used by the WKSPointZComparer. To be on the safe
		/// side double-check with CompareGeometryContainsPoint3D
		/// Additionally this method misses differences such as the same points being connected
		/// in different order!
		/// </summary>
		/// <returns></returns>
		public IDictionary<WKSPointZ, VertexIndex> GetDifference(bool compare3D)
		{
			// TODO: to avoid tolerance-issues consider using GetChangedVertices implementation
			Dictionary<WKSPointZ, VertexIndex> baseCoordinates, compareCoordinates;

			CreateDictionaries(compare3D, out baseCoordinates, out compareCoordinates);

			return GetPointDifference(baseCoordinates, compareCoordinates);
		}

		public bool CompareGeometryContainsPoint3D(WKSPointZ point)
		{
			Assert.ArgumentNotNull(point, nameof(point));

			if (_compareGeometryCoords3D == null)
			{
				_compareGeometryCoords3D = CreateCoordinateDictionary(
					_compareGeometry, true, out _compareCoordinateDuplicates3D);
			}

			if (_compareGeometryCoords3D.ContainsKey(point))
			{
				return true;
			}

			if (_compareGeometryCoords2D == null)
			{
				_compareGeometryCoords2D = CreateCoordinateDictionary(
					_compareGeometry, false, out _compareCoordinateDuplicates2D);
			}

			VertexIndex coordinateFound2D;
			if (_compareGeometryCoords2D.TryGetValue(point, out coordinateFound2D))
			{
				// There is a 2D-equal point but no 3D-equal point at this location. Check tolerance:
				QueryCompareGeometryCoordinates(coordinateFound2D, _pointTemplate);

				Assert.NotNaN(_zTolerance, "Z Tolerance not initialized");

				if (Math.Abs(point.Z - _pointTemplate.Z) <= _zTolerance)
				{
					return true;
				}

				// Check duplicate points
				// there is a 2D point but no 3D point -> could be on a vertical segment or a duplicate at the same xy location (within the tolerance?)
				List<VertexIndex> duplicate2DVertices;
				if (_compareCoordinateDuplicates2D.TryGetValue(point, out duplicate2DVertices))
				{
					foreach (VertexIndex vertexIndex in duplicate2DVertices)
					{
						QueryCompareGeometryCoordinates(vertexIndex, _pointTemplate);

						if (! Disjoint3D(point, _pointTemplate))
						{
							return true;
						}
					}

					// It could be on a vertical segment - disjoint check below
				}
				else
				{
					return false;
				}
			}

			// The point could be on the interior of a segment
			if (_highLevelCompareGeometry == null)
			{
				_highLevelCompareGeometry = GeometryUtils.GetHighLevelGeometry(_compareGeometry,
					true);

				// for _zTolerance == 0 this is not correct but at least it's as small as possible
				SetZTolerance(_highLevelCompareGeometry, _zTolerance);

				GeometryUtils.AllowIndexing(_highLevelCompareGeometry);
			}

			// This is often wrong when there is a 2D intersection with another vertex! 
			bool disjoint = Disjoint3D(point, _highLevelCompareGeometry);

			return ! disjoint;
		}

		/// <summary>
		/// Returns vertices that are different between the base and the compare geometry.
		/// NOTE: This method misses differences such as the same points being connected
		/// in different order!
		/// </summary>
		/// <param name="symmetric">Wether the symmetric difference should be returned or not.
		/// Symmetric difference: both set of points are returned:
		/// - points that exist in the base geometry but not in the compare geometry
		/// - points that exist in the compare geometry but not in the base geometry
		/// Non-symmetric difference: only the points in the base geometry that do not
		/// exist in the compare geometry are returned.</param>
		/// <returns>The difference vertices</returns>
		[NotNull]
		public IList<WKSPointZ> GetDifferentVertices(bool symmetric)
		{
			bool reportDuplicateVertices;

			switch (_baseGeometry.GeometryType)
			{
				case esriGeometryType.esriGeometryMultipoint:
					reportDuplicateVertices = true;
					break;

				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryLine:
				case esriGeometryType.esriGeometryCircularArc:
				case esriGeometryType.esriGeometryEllipticArc:
				case esriGeometryType.esriGeometryBezier3Curve:
				case esriGeometryType.esriGeometryPath:
				case esriGeometryType.esriGeometryPolyline:
				case esriGeometryType.esriGeometryRing:
				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryMultiPatch:
					reportDuplicateVertices = false;
					break;

				case esriGeometryType.esriGeometryNull:
				case esriGeometryType.esriGeometryEnvelope:
				case esriGeometryType.esriGeometryAny:
				case esriGeometryType.esriGeometryBag:
				case esriGeometryType.esriGeometryTriangleStrip:
				case esriGeometryType.esriGeometryTriangleFan:
				case esriGeometryType.esriGeometryRay:
				case esriGeometryType.esriGeometrySphere:
				case esriGeometryType.esriGeometryTriangles:
					throw new ArgumentException(
						string.Format("Unsupported geometry type: {0}",
						              _baseGeometry.GeometryType));
				default:
					throw new ArgumentException(
						string.Format("Unknown geometry type: {0}",
						              _baseGeometry.GeometryType));
			}

			return GetDifferentVertices(symmetric, reportDuplicateVertices);
		}

		/// <summary>
		/// Returns vertices that are different between the base and the compare geometry.
		/// NOTE: This method misses differences such as the same points being connected
		/// in different order!
		/// </summary>
		/// <param name="symmetric">Wether the symmetric difference should be returned or not.
		/// Symmetric difference: both set of points are returned:
		/// - points that exist in the base geometry but not in the compare geometry
		/// - points that exist in the compare geometry but not in the base geometry
		/// Non-symmetric difference: only the points in the base geometry that do not
		/// exist in the compare geometry are returned.</param>
		/// <param name="reportDuplicateVertices">Wether or not duplicate vertices in one
		/// geometry should be reported as difference</param>
		/// <returns>The difference vertices</returns>
		[NotNull]
		public IList<WKSPointZ> GetDifferentVertices(bool symmetric,
		                                             bool reportDuplicateVertices)
		{
			if (_baseGeometry.GeometryType == esriGeometryType.esriGeometryPoint)
			{
				return GetPointDifference(symmetric);
			}

			WKSPointZ[] baseCoords = GeometryUtils.GetWKSPointZs(_baseGeometry, true);
			WKSPointZ[] compareCoords = GeometryUtils.GetWKSPointZs(_compareGeometry, true);

			return GetChangedVertices(baseCoords, compareCoords,
			                          reportDuplicateVertices, ! symmetric);
		}

		/// <summary>
		/// Determines whether the two geometries have the same set of vertices. 
		/// Duplicate vertices are ignored.
		/// </summary>
		/// <returns></returns>
		public bool HaveSameVertices(bool ignoreDuplicateVertices = true)
		{
			WKSPointZ[] baseCoords = GeometryUtils.GetWKSPointZs(_baseGeometry, true);
			WKSPointZ[] compareCoords = GeometryUtils.GetWKSPointZs(_compareGeometry, true);

			return WKSPointZUtils.HaveSameVertices(baseCoords, compareCoords, _xyTolerance,
			                                       _zTolerance,
			                                       ignoreDuplicateVertices);
		}

		[NotNull]
		private IList<WKSPointZ> GetPointDifference(bool symmetric)
		{
			var result = new List<WKSPointZ>();

			if (! GeometryUtils.IsSamePoint((IPoint) _baseGeometry,
			                                (IPoint) _compareGeometry,
			                                _xyTolerance, _zTolerance))
			{
				result.Add(WKSPointZUtils.CreatePoint((IPoint) _baseGeometry));

				if (symmetric)
				{
					result.Add(WKSPointZUtils.CreatePoint((IPoint) _compareGeometry));
				}
			}

			return result;
		}

		/// <summary>
		/// Gets all the segments from the base geometry (source) whose to- and/or from-point
		/// is not contained by the target geometry (in 3D). Assumes that base and compare geometries
		/// are congruent in 2D.
		/// </summary>
		/// <param name="zDifferentPoints"></param>
		/// <returns></returns>
		public IPolyline GetBaseSegmentZDifferences(
			out IDictionary<WKSPointZ, VertexIndex> zDifferentPoints)
		{
			Assert.True(_baseGeometry is IPolyline || _baseGeometry is IPath,
			            "Not implemented for geometries other than lines.");

			Stopwatch watch = _msg.DebugStartTiming("Calculating Z difference on source...");

			// Get the points that are in the base but not in the target:
			zDifferentPoints = GetDifference(true);

			IDictionary<VertexIndex, ISegment> sourceSegmentsToAdd =
				new Dictionary<VertexIndex, ISegment>();

			foreach (
				KeyValuePair<WKSPointZ, VertexIndex> differentPointOnPath in zDifferentPoints)
			{
				bool targetContainsPoint =
					CompareGeometryContainsPoint3D(differentPointOnPath.Key);

				if (! targetContainsPoint)
				{
					// Take the respective segment before and after this point as difference
					VertexIndex vertexIndex = differentPointOnPath.Value;

					int previousSegmentIdx = vertexIndex.VertexIndexInPart - 1;

					var previousVertexIdx = new VertexIndex(vertexIndex.PartIndex,
					                                        previousSegmentIdx,
					                                        false);
					if (previousSegmentIdx >= 0 &&
					    ! sourceSegmentsToAdd.ContainsKey(previousVertexIdx))
					{
						ISegment segment = GetBaseSegment(previousVertexIdx);

						sourceSegmentsToAdd.Add(previousVertexIdx, segment);
					}

					if (! vertexIndex.IsLastInPart &&
					    ! sourceSegmentsToAdd.ContainsKey(vertexIndex))
					{
						ISegment segment = GetBaseSegment(vertexIndex);
						sourceSegmentsToAdd.Add(vertexIndex, segment);
					}
				}
			}

			IPolyline sourceDifferences =
				GeometryFactory.CreatePolyline(_baseGeometry.SpatialReference, true,
				                               GeometryUtils.IsMAware(_baseGeometry));

			var segmentArray = new ISegment[sourceSegmentsToAdd.Count];

			sourceSegmentsToAdd.Values.CopyTo(segmentArray, 0);

			GeometryUtils.GeometryBridge.SetSegments((ISegmentCollection) sourceDifferences,
			                                         ref segmentArray);

			_msg.DebugStopTiming(watch,
			                     "Calculated Z differences on source and built difference line");

			return sourceDifferences;
		}

		/// <summary>
		/// This method is sensitive to geometries that share the same points
		/// but not in the same order.
		/// </summary>
		/// <param name="baseCoords"></param>
		/// <param name="compareCoords"></param>
		/// <param name="reportDuplicateVertices"></param>
		/// <param name="baseInsertsOnly"></param>
		/// <returns></returns>
		[NotNull]
		private IList<WKSPointZ> GetChangedVertices(
			WKSPointZ[] baseCoords, WKSPointZ[] compareCoords,
			bool reportDuplicateVertices, bool baseInsertsOnly)
		{
			Assert.ArgumentNotNull(baseCoords, nameof(baseCoords));
			Assert.ArgumentNotNull(compareCoords, nameof(compareCoords));

			var comparer = new WKSPointZComparer(_xyTolerance, _zTolerance, 0, 0, 0);
			Array.Sort(baseCoords, comparer);
			Array.Sort(compareCoords, comparer);

			var result = new List<WKSPointZ>();

			var i1 = 0;
			var i2 = 0;

			while (i1 < baseCoords.Length && i2 < compareCoords.Length)
			{
				WKSPointZ a = baseCoords[i1];
				WKSPointZ b = compareCoords[i2];

				bool inSync = GeometryUtils.IsSamePoint(a, b, _xyTolerance, _zTolerance);

				if (! inSync)
				{
					// a is greater than b, advance b until in sync with a
					bool matchAdvanceCompareCoords = AdvanceIndexUntilMatch(
						compareCoords, ref i2, a, comparer, result, ! baseInsertsOnly,
						reportDuplicateVertices);

					// b is greater than a, advance a until in sync with b
					bool matchAdvanceBaseCoords = AdvanceIndexUntilMatch(
						baseCoords, ref i1, b, comparer, result, true, reportDuplicateVertices);

					if (matchAdvanceCompareCoords || matchAdvanceBaseCoords)
					{
						inSync = true;
					}
				}

				// Advance to next set of coordinates only if in sync to make sure no difference points are missed
				if (inSync)
				{
					++i1;
					++i2;
				}

				// Skip identical points in sequence otherwise From/To points in rotated ring get reported as changes
				// NOTE: SameCoords tolerates index out of bounds!
				while (WKSPointZUtils.ArePointsEqual(baseCoords, i1, i1 - 1, _xyTolerance,
				                                     _zTolerance))
				{
					bool compareCoordsHasDuplicateToo =
						inSync &&
						WKSPointZUtils.ArePointsEqual(compareCoords, i2, i2 - 1, _xyTolerance,
						                              _zTolerance);

					if (compareCoordsHasDuplicateToo)
					{
						// in sync and both have a duplicate at the same location -> unchanged, do not report
						// but increment both indexes
						++i2;
					}
					else
					{
						// not in sync or compare geometry has no duplicate -> changed, report on request
						if (reportDuplicateVertices)
						{
							result.Add(baseCoords[i1]);
						}
					}

					// eventually landing at the next non-identical point:
					++i1;
				}

				// process remaining duplicates at current location (or at original location if not in sync)
				while (WKSPointZUtils.ArePointsEqual(compareCoords, i2, i2 - 1, _xyTolerance,
				                                     _zTolerance))
				{
					if (reportDuplicateVertices && ! baseInsertsOnly)
					{
						result.Add(compareCoords[i2]);
					}

					++i2;
				}
			}

			AddRemainingPoints(baseCoords, compareCoords, i1, i2, result,
			                   reportDuplicateVertices, baseInsertsOnly);

			return result;
		}

		private static IDictionary<WKSPointZ, VertexIndex> GetPointDifference(
			IEnumerable<KeyValuePair<WKSPointZ, VertexIndex>> basePoints,
			IDictionary<WKSPointZ, VertexIndex> differentFrom)
		{
			IDictionary<WKSPointZ, VertexIndex> result =
				new Dictionary<WKSPointZ, VertexIndex>();

			foreach (KeyValuePair<WKSPointZ, VertexIndex> baseCoord in basePoints)
			{
				if (! differentFrom.ContainsKey(baseCoord.Key))
				{
					result.Add(baseCoord);
				}
			}

			return result;
		}

		private void CreateDictionaries(bool compare3D,
		                                out Dictionary<WKSPointZ, VertexIndex>
			                                baseCoordinates,
		                                out Dictionary<WKSPointZ, VertexIndex>
			                                compareCoordinates)
		{
			if (compare3D)
			{
				if (_baseGeometryCoords3D == null)
				{
					_baseGeometryCoords3D = CreateCoordinateDictionary(_baseGeometry, true,
						out
						_baseCoordinateDuplicates3D);
				}

				if (_compareGeometryCoords3D == null)
				{
					_compareGeometryCoords3D = CreateCoordinateDictionary(_compareGeometry, true,
						out
						_compareCoordinateDuplicates3D);
				}

				baseCoordinates = _baseGeometryCoords3D;
				compareCoordinates = _compareGeometryCoords3D;
			}
			else
			{
				if (_baseGeometryCoords2D == null)
				{
					_baseGeometryCoords2D = CreateCoordinateDictionary(_baseGeometry, false,
						out
						_baseCoordinateDuplicates2D);
				}

				if (_compareGeometryCoords2D == null)
				{
					_compareGeometryCoords2D = CreateCoordinateDictionary(_compareGeometry, false,
						out
						_compareCoordinateDuplicates2D);
				}

				baseCoordinates = _baseGeometryCoords2D;
				compareCoordinates = _compareGeometryCoords2D;
			}
		}

		[NotNull]
		private Dictionary<WKSPointZ, VertexIndex> CreateCoordinateDictionary(
			[NotNull] IGeometry geometry,
			bool compare3D,
			out Dictionary<WKSPointZ, List<VertexIndex>> duplicateCoordinates)
		{
			double zTolerance = compare3D
				                    ? _zTolerance
				                    : double.NaN;

			var comparer = new WKSPointZComparer(_xyTolerance, zTolerance,
			                                     geometry.SpatialReference);

			var coordinateDictionary =
				new Dictionary<WKSPointZ, VertexIndex>(((IPointCollection) geometry).PointCount,
				                                       comparer);

			WKSPointZ[] pointArray = GeometryUtils.GetWKSPointZs(geometry, true);

			// NOTE: there can be ambiguity if two different segments have the same point (rings, boundary loops) 
			//		 or in the 2D case if two points differ only in Z. Hence duplicates must be explicitly managed.
			var currentPartIdx = 0;
			var currentIndexInPart = 0;
			var geometryCollection = geometry as IGeometryCollection;

			int partCount = geometryCollection?.GeometryCount ?? 1;

			duplicateCoordinates = new Dictionary<WKSPointZ, List<VertexIndex>>(partCount);
			// one duplicate for each ring

			IPointCollection currentPartPoints =
				geometryCollection == null
					? (IPointCollection) geometry
					: null;

			foreach (WKSPointZ wksPointZ in pointArray)
			{
				if (currentPartPoints == null && geometryCollection != null)
				{
					currentPartPoints =
						geometryCollection.get_Geometry(currentPartIdx) as IPointCollection;
				}

				bool isLastInPart = currentPartPoints != null &&
				                    currentIndexInPart == currentPartPoints.PointCount - 1;

				var currentVertexIndex = new VertexIndex(currentPartIdx, currentIndexInPart,
				                                         isLastInPart);

				if (! coordinateDictionary.ContainsKey(wksPointZ))
				{
					coordinateDictionary.Add(wksPointZ, currentVertexIndex);
				}
				else if (! compare3D && ! double.IsNaN(_zTolerance))
				{
					VertexIndex alreadyAdded = coordinateDictionary[wksPointZ];

					List<VertexIndex> duplicateIndices;
					if (! duplicateCoordinates.TryGetValue(wksPointZ, out duplicateIndices))
					{
						duplicateIndices = new List<VertexIndex>(2) {alreadyAdded};
						duplicateCoordinates.Add(wksPointZ, duplicateIndices);
					}

					duplicateIndices.Add(currentVertexIndex);
				}

				if (isLastInPart)
				{
					currentPartIdx++;
					currentPartPoints = null;
					currentIndexInPart = 0;
				}
				else
				{
					currentIndexInPart++;
				}
			}

			return coordinateDictionary;
		}

		/// <summary>
		/// Adds the remaining points starting from baseIndexStart for baseCoords and from 
		/// compareIndexStart for compareCoords.
		/// </summary>
		/// <param name="baseCoords"></param>
		/// <param name="compareCoords"></param>
		/// <param name="baseIndexStart"></param>
		/// <param name="compareIndexStart"></param>
		/// <param name="result"></param>
		/// <param name="reportDuplicateVertices"></param>
		/// <param name="baseInsertsOnly"></param>
		private void AddRemainingPoints(
			WKSPointZ[] baseCoords,
			WKSPointZ[] compareCoords,
			int baseIndexStart,
			int compareIndexStart,
			List<WKSPointZ> result,
			bool reportDuplicateVertices,
			bool baseInsertsOnly)
		{
			// add all remaining points from baseCoords
			for (int i = baseIndexStart; i < baseCoords.Length; i++)
			{
				bool sameAsPrevious = WKSPointZUtils.ArePointsEqual(
					baseCoords, i, i - 1, _xyTolerance,
					_zTolerance);

				if (sameAsPrevious && ! reportDuplicateVertices)
				{
					continue;
				}

				result.Add(baseCoords[i]);
			}

			if (! baseInsertsOnly)
			{
				// add all remaining points from baseCoords
				for (int i = compareIndexStart; i < compareCoords.Length; i++)
				{
					bool sameAsPrevious = WKSPointZUtils.ArePointsEqual(compareCoords, i, i - 1,
						_xyTolerance,
						_zTolerance);

					if (sameAsPrevious && ! reportDuplicateVertices)
					{
						continue;
					}

					result.Add(compareCoords[i]);
				}
			}
		}

		/// <summary>
		/// Advances the vertex index of coordsToAdvance starting at currentIndex until 
		/// the point at the index matches coordinateToMatch. All intermediate points are
		/// added to changedCoords.
		/// </summary>
		/// <param name="coordsToAdvance">The list of coordinates (sorted) to go through
		/// until a coordinate matches <paramref name="coordinateToMatch"/>.</param>
		/// <param name="index">The index to start at which shall be incremented unit the coordinates match.</param>
		/// <param name="coordinateToMatch">The target coordinate.</param>
		/// <param name="comparer">The comparer.</param>
		/// <param name="changedCoords">The list of coordinates that do not match.</param>
		/// <param name="addChangesToResult">Whether the changed points should be added to changedCoords</param>
		/// <param name="reportDuplicateVertices">Whether duplicate vertices in the coordsToAdvance should be reported or not.</param>
		/// <returns>Whether a match was found or not.</returns>
		private bool AdvanceIndexUntilMatch(
			[NotNull] WKSPointZ[] coordsToAdvance,
			ref int index,
			WKSPointZ coordinateToMatch,
			[NotNull] IComparer<WKSPointZ> comparer,
			[NotNull] ICollection<WKSPointZ> changedCoords,
			bool addChangesToResult,
			bool reportDuplicateVertices)
		{
			WKSPointZ currentCoord = coordsToAdvance[index];

			while (comparer.Compare(coordinateToMatch, currentCoord) >= 0 &&
			       index < coordsToAdvance.Length)
			{
				// check if within the tolerance - performance could be improved if comparer could directly handle
				// the tolerance.
				bool useTolerance = Math.Abs(_xyTolerance) > double.Epsilon ||
				                    ! double.IsNaN(_zTolerance) &&
				                    Math.Abs(_zTolerance) > double.Epsilon;

				if (useTolerance &&
				    GeometryUtils.IsSamePoint(currentCoord, coordinateToMatch,
				                              _xyTolerance, _zTolerance))
				{
					return true;
				}

				if (addChangesToResult)
				{
					// except if it's a duplicate that should not be reported
					bool sameAsPrevious = WKSPointZUtils.ArePointsEqual(
						coordsToAdvance, index, index - 1,
						_xyTolerance, _zTolerance);

					if (! sameAsPrevious || reportDuplicateVertices)
					{
						changedCoords.Add(currentCoord);
					}
				}

				++index;

				if (index < coordsToAdvance.Length)
				{
					currentCoord = coordsToAdvance[index];
				}
			}

			return false;
		}

		private bool Disjoint3D(WKSPointZ wksPoint, IPoint point)
		{
			return ! GeometryUtils.IsSamePoint(
				       wksPoint.X, wksPoint.Y, wksPoint.Z,
				       point.X, point.Y, point.Z,
				       _xyTolerance, _zTolerance);
		}

		private bool Disjoint3D(WKSPointZ wksPoint, IGeometry geometry)
		{
			_pointTemplate.PutCoords(wksPoint.X, wksPoint.Y);
			_pointTemplate.Z = wksPoint.Z;

			bool disjoint =
				((IRelationalOperator3D) geometry).Disjoint3D(_pointTemplate);

			return disjoint;
		}

		private static void SetZTolerance(IGeometry geometry, double zTolerance)
		{
			var srTolerance =
				(ISpatialReferenceTolerance) ((IClone) geometry.SpatialReference).Clone();

			srTolerance.ZTolerance = zTolerance;

			if (srTolerance.ZToleranceValid != esriSRToleranceEnum.esriSRToleranceOK)
			{
				srTolerance.SetMinimumZTolerance();
			}

			GeometryUtils.EnsureSpatialReference(geometry, (ISpatialReference) srTolerance);
		}

		private ISegment GetBaseSegment(VertexIndex atFromVertexIndex)
		{
			var fromSegments = _baseGeometry as ISegmentCollection;

			if (fromSegments == null)
			{
				throw new NotImplementedException(
					"Cannot get base segments, no segment collection");
			}

			var geometryCollection = _baseGeometry as IGeometryCollection;

			if (geometryCollection == null || atFromVertexIndex.PartIndex == 0)
			{
				return fromSegments.Segment[atFromVertexIndex.VertexIndexInPart];
			}

			if (! (_baseGeometry is ISegmentCollection))
			{
				throw new NotImplementedException(
					string.Format("Not implemented for geometry type {0}",
					              _compareGeometry.GeometryType));
			}

			// This makes a big difference for geometries with hundreds of parts (such as the one 
			// used in CanCalculateDifferenceInHugeLockergestein())
			int globalSegmentIndex = GetGlobalSegmentIndex(
				atFromVertexIndex, GetBaseGeometryPartStartIndex(geometryCollection));

			ISegment segment = fromSegments.Segment[globalSegmentIndex];

			return segment;
		}

		private void QueryCompareGeometryCoordinates(VertexIndex atVertexIndex,
		                                             [NotNull] IPoint result)
		{
			if (_compareGeometry.GeometryType == esriGeometryType.esriGeometryMultipoint)
			{
				((IPointCollection) _compareGeometry).QueryPoint(atVertexIndex.PartIndex, result);
				return;
			}

			var geometryCollection = _compareGeometry as IGeometryCollection;

			if (geometryCollection == null || atVertexIndex.PartIndex == 0)
			{
				((IPointCollection) _compareGeometry).QueryPoint(
					atVertexIndex.VertexIndexInPart, result);
				return;
			}

			if (! (_compareGeometry is ISegmentCollection))
			{
				throw new NotImplementedException(
					string.Format("Not implemented for geometry type {0}",
					              _compareGeometry.GeometryType));
			}

			// This makes a big difference for geometries with hundreds of parts (proved by CanCalculateDifferenceInHugeLockergestein())
			int globalIndex = GetGlobalVertexIndex(
				atVertexIndex, GetCompareGeometryPartStartIndex(geometryCollection));

			((IPointCollection) _compareGeometry).QueryPoint(globalIndex, result);
		}

		private static int GetGlobalVertexIndex(VertexIndex atVertexIndex,
		                                        IList<int> partStartVertxIndexes)
		{
			int globalIndex = partStartVertxIndexes[atVertexIndex.PartIndex] +
			                  atVertexIndex.VertexIndexInPart;
			return globalIndex;
		}

		private static int GetGlobalSegmentIndex(
			VertexIndex fromVertexIndex,
			IList<int> partStartVertxIndexes)
		{
			// each part has one segment less than vertices:
			int globalIndex = partStartVertxIndexes[fromVertexIndex.PartIndex]
			                  - fromVertexIndex.PartIndex
			                  + fromVertexIndex.VertexIndexInPart;

			return globalIndex;
		}

		/// <summary>
		/// Returns a list with the global index of the first vertex in each geometry part of the
		/// compare geometry. This improves the of the global index calculation.
		/// </summary>
		/// <param name="geometryCollection"></param>
		/// <returns></returns>
		private List<int> GetCompareGeometryPartStartIndex(
			IGeometryCollection geometryCollection)
		{
			if (_compareGeometryPartStartIndex == null)
			{
				int partCount = geometryCollection.GeometryCount;

				_compareGeometryPartStartIndex = new List<int>(partCount);

				var firstIndexInPart = 0;

				for (var i = 0; i < partCount; i++)
				{
					_compareGeometryPartStartIndex.Add(firstIndexInPart);

					IGeometry partGeometry = geometryCollection.Geometry[i];

					var points = partGeometry as IPointCollection;

					if (points != null)
					{
						firstIndexInPart += points.PointCount;
					}
					else if (partGeometry is IPoint)
					{
						firstIndexInPart++;
					}
				}
			}

			return _compareGeometryPartStartIndex;
		}

		/// <summary>
		/// Returns a list with the global index of the first vertex in each geometry part of the
		/// base geometry. This improves the of the global index calculation.
		/// </summary>
		/// <param name="geometryCollection"></param>
		/// <returns></returns>
		private List<int> GetBaseGeometryPartStartIndex(
			IGeometryCollection geometryCollection)
		{
			if (_baseGeometryPartStartIndex == null)
			{
				int partCount = geometryCollection.GeometryCount;

				_baseGeometryPartStartIndex = new List<int>(partCount);

				var firstIndexInPart = 0;

				for (var i = 0; i < partCount; i++)
				{
					_baseGeometryPartStartIndex.Add(firstIndexInPart);

					IGeometry partGeometry = geometryCollection.Geometry[i];

					var points = partGeometry as IPointCollection;

					if (points != null)
					{
						firstIndexInPart += points.PointCount;
					}
					else if (partGeometry is IPoint)
					{
						firstIndexInPart++;
					}
				}
			}

			return _baseGeometryPartStartIndex;
		}
	}
}
