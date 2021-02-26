using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaNoGaps : ContainerTest
	{
		private readonly double _sliverLimit;
		private readonly double _maxArea;
		private readonly double _subtileWidth;
		private readonly int _tileSubdivisionCount;
		private readonly bool _findGapsBelowTolerance;
		private readonly IList<IFeatureClass> _areaOfInterestClasses;

		private readonly List<IPolygon> _tileAreasOfInterest;
		private readonly List<IFeature> _tileFeatures;
		private readonly ISpatialReference _spatialReference;
		private readonly double _tolerance;
		private readonly double _minimumSubtileWidth;

		private Box _allBox;
		private KnownGaps _knownGaps;

		private static readonly IList<IFeatureClass> _emptyFeatureClasses =
			new List<IFeatureClass>();

		private readonly int _firstAreaOfInterestClassIndex;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string Gap_AreaTooSmall = "Gap.AreaTooSmall";
			public const string Gap_SliverRatioTooLarge = "Gap.SliverRatioTooLarge";

			public const string Gap_AreaTooSmallAndSliverRatioTooLarge =
				"Gap.AreaTooSmallAndSliverRatioTooLarge";

			public const string Gap = "Gap";

			public Code() : base("Gaps") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaNoGaps_0))]
		public QaNoGaps([Doc(nameof(DocStrings.QaNoGaps_polygonClass))] [NotNull]
		                IFeatureClass polygonClass,
		                [Doc(nameof(DocStrings.QaNoGaps_sliverLimit))] double sliverLimit,
		                [Doc(nameof(DocStrings.QaNoGaps_maxArea))] double maxArea)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, sliverLimit, maxArea, 0d, false) { }

		[Doc(nameof(DocStrings.QaNoGaps_1))]
		public QaNoGaps(
				[Doc(nameof(DocStrings.QaNoGaps_polygonClasses))] [NotNull]
				IList<IFeatureClass> polygonClasses,
				[Doc(nameof(DocStrings.QaNoGaps_sliverLimit))] double sliverLimit,
				[Doc(nameof(DocStrings.QaNoGaps_maxArea))] double maxArea)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClasses, sliverLimit, maxArea, 0d, false) { }

		[Doc(nameof(DocStrings.QaNoGaps_2))]
		public QaNoGaps([Doc(nameof(DocStrings.QaNoGaps_polygonClass))] [NotNull]
		                IFeatureClass polygonClass,
		                [Doc(nameof(DocStrings.QaNoGaps_sliverLimit))] double sliverLimit,
		                [Doc(nameof(DocStrings.QaNoGaps_maxArea))] double maxArea,
		                [Doc(nameof(DocStrings.QaNoGaps_subtileWidth))] double subtileWidth,
		                [Doc(nameof(DocStrings.QaNoGaps_findGapsBelowTolerance))]
		                bool findGapsBelowTolerance)
			: this(new List<IFeatureClass> {polygonClass}, sliverLimit, maxArea,
			       subtileWidth, 0,
			       findGapsBelowTolerance, _emptyFeatureClasses) { }

		[Doc(nameof(DocStrings.QaNoGaps_3))]
		public QaNoGaps(
			[Doc(nameof(DocStrings.QaNoGaps_polygonClasses))] [NotNull]
			IList<IFeatureClass> polygonClasses,
			[Doc(nameof(DocStrings.QaNoGaps_sliverLimit))] double sliverLimit,
			[Doc(nameof(DocStrings.QaNoGaps_maxArea))] double maxArea,
			[Doc(nameof(DocStrings.QaNoGaps_subtileWidth))] double subtileWidth,
			[Doc(nameof(DocStrings.QaNoGaps_findGapsBelowTolerance))]
			bool findGapsBelowTolerance)
			: this(polygonClasses, sliverLimit, maxArea,
			       subtileWidth, 0,
			       findGapsBelowTolerance, _emptyFeatureClasses) { }

		[Doc(nameof(DocStrings.QaNoGaps_4))]
		public QaNoGaps(
			[Doc(nameof(DocStrings.QaNoGaps_polygonClasses))] [NotNull]
			IList<IFeatureClass> polygonClasses,
			[Doc(nameof(DocStrings.QaNoGaps_sliverLimit))] double sliverLimit,
			[Doc(nameof(DocStrings.QaNoGaps_maxArea))] double maxArea,
			[Doc(nameof(DocStrings.QaNoGaps_areaOfInterestClasses))] [NotNull]
			IList<IFeatureClass>
				areaOfInterestClasses)
			: this(polygonClasses, sliverLimit, maxArea, 0d, false, areaOfInterestClasses) { }

		[Doc(nameof(DocStrings.QaNoGaps_5))]
		public QaNoGaps(
			[Doc(nameof(DocStrings.QaNoGaps_polygonClasses))] [NotNull]
			IList<IFeatureClass> polygonClasses,
			[Doc(nameof(DocStrings.QaNoGaps_sliverLimit))] double sliverLimit,
			[Doc(nameof(DocStrings.QaNoGaps_maxArea))] double maxArea,
			[Doc(nameof(DocStrings.QaNoGaps_subtileWidth))] double subtileWidth,
			[Doc(nameof(DocStrings.QaNoGaps_findGapsBelowTolerance))]
			bool findGapsBelowTolerance,
			[Doc(nameof(DocStrings.QaNoGaps_areaOfInterestClasses))] [NotNull]
			IList<IFeatureClass>
				areaOfInterestClasses)
			: this(polygonClasses, sliverLimit, maxArea,
			       subtileWidth, 0,
			       findGapsBelowTolerance, areaOfInterestClasses) { }

		[Obsolete]
		public QaNoGaps([NotNull] IFeatureClass polygonClass,
		                double sliverLimit,
		                double maxArea,
		                int tileSubdivisionCount)
			: this(
				new List<IFeatureClass> {polygonClass}, sliverLimit, maxArea,
				0, tileSubdivisionCount, false, _emptyFeatureClasses) { }

		[Obsolete]
		public QaNoGaps([NotNull] IList<IFeatureClass> polygonClasses,
		                double sliverLimit,
		                double maxArea,
		                int tileSubdivisionCount)
			: this(polygonClasses, sliverLimit, maxArea, 0,
			       tileSubdivisionCount, false, _emptyFeatureClasses) { }

		private QaNoGaps([NotNull] IList<IFeatureClass> polygonClasses,
		                 double sliverLimit, double maxArea,
		                 double subtileWidth, int tileSubdivisionCount,
		                 bool findGapsBelowTolerance,
		                 [NotNull] IList<IFeatureClass> areaOfInterestClasses)
			: base(CastToTables(polygonClasses, areaOfInterestClasses))
		{
			Assert.ArgumentNotNull(polygonClasses, nameof(polygonClasses));

			_firstAreaOfInterestClassIndex = polygonClasses.Count;

			_sliverLimit = sliverLimit;
			_maxArea = maxArea;
			_subtileWidth = subtileWidth;
			_tileSubdivisionCount = tileSubdivisionCount;
			_findGapsBelowTolerance = findGapsBelowTolerance;
			_areaOfInterestClasses = areaOfInterestClasses;

			_tileFeatures = new List<IFeature>();
			_tileAreasOfInterest = new List<IPolygon>();

			if (findGapsBelowTolerance)
			{
				double maximumResolution;
				_spatialReference = GetSpatialReferenceAtMaximumResolution(polygonClasses,
				                                                           out maximumResolution);

				_tolerance = maximumResolution * 2;
				_minimumSubtileWidth = maximumResolution * 10000;
			}
			else
			{
				_spatialReference = GetUniqueSpatialReference(polygonClasses, out _tolerance);
				_minimumSubtileWidth = _tolerance * 100;
			}

			KeepRows = true;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			var feature = row as IFeature;
			if (feature != null)
			{
				if (tableIndex >= _firstAreaOfInterestClassIndex)
				{
					var areaOfInterest = feature.Shape as IPolygon;
					if (areaOfInterest != null && ! areaOfInterest.IsEmpty)
					{
						_tileAreasOfInterest.Add(areaOfInterest);
					}
				}
				else
				{
					_tileFeatures.Add(feature);
				}
			}

			return NoError;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (args.State == TileState.Initial)
			{
				_tileFeatures.Clear();
				_tileAreasOfInterest.Clear();
				_allBox = null;
				_knownGaps = null;
				return NoError;
			}

			try
			{
				if (_allBox == null)
				{
					Assert.NotNull(args.AllBox, "args.AllBox");
					_allBox = QaGeometryUtils.CreateBox(Assert.NotNull(args.AllBox, "AllBox"));
				}

				if (_knownGaps == null)
				{
					_knownGaps = new KnownGaps(_maxArea, _tolerance, _allBox);
				}

				Assert.NotNull(args.CurrentEnvelope, "args.CurrentEnvelope");
				IEnvelope tileEnvelope =
					GeometryFactory.Clone(Assert.NotNull(args.CurrentEnvelope, "CurrentEnvelope"));
				tileEnvelope.SpatialReference = _spatialReference;

				var errorCount = 0;

				foreach (IEnvelope subtile in GetSubtiles(tileEnvelope))
				{
					errorCount += CheckSubtile(subtile, _allBox, _tileFeatures, _knownGaps);

					// this can be the entire (cloned) tileEnvelope:
					Marshal.ReleaseComObject(subtile);

					GC.Collect();
					GC.WaitForPendingFinalizers();
				}

				if (args.State == TileState.Final)
				{
					_knownGaps = null;
				}

				return errorCount;
			}
			finally
			{
				_tileFeatures.Clear();
			}
		}

		[NotNull]
		private IEnumerable<IEnvelope> GetSubtiles([NotNull] IEnvelope tileEnvelope)
		{
			return _subtileWidth > 0 && _tileSubdivisionCount == 0
				       ? GetSubtilesByWidth(tileEnvelope,
				                            _subtileWidth,
				                            _minimumSubtileWidth)
				       : GetSubtilesBySubdivisionCount(tileEnvelope,
				                                       _tileSubdivisionCount,
				                                       _minimumSubtileWidth);
		}

		[NotNull]
		private static IEnumerable<IEnvelope> GetSubtilesByWidth(
			[NotNull] IEnvelope tileEnvelope,
			double subtileWidth,
			double minimumSubtileWidth)
		{
			Assert.ArgumentNotNull(tileEnvelope, nameof(tileEnvelope));
			Assert.ArgumentCondition(subtileWidth > minimumSubtileWidth,
			                         "Invalid subtile width: {0} minimum value: {1}",
			                         subtileWidth, minimumSubtileWidth);

			// subdivide along X axis only
			// (subdividing along Y axis breaks the per-row processing of main tiles,
			//  changes to the 'completed tile' logic would be needed)

			double tileWidth = tileEnvelope.Width;

			if (subtileWidth + minimumSubtileWidth > tileWidth)
			{
				// the second subTile would be below the minimum width
				// -> return entire tile
				yield return tileEnvelope;
				yield break;
			}

			double xMin;
			double yMin;
			double xMax;
			double yMax;
			tileEnvelope.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			double subTileXMin = xMin;

			ISpatialReference spatialReference = tileEnvelope.SpatialReference;

			var lastTile = false;
			do
			{
				double remainder = xMax - (subTileXMin + subtileWidth);

				double subTileXMax;
				if (remainder >= minimumSubtileWidth)
				{
					subTileXMax = subTileXMin + subtileWidth;
				}
				else
				{
					// remainder is too small, return last tile to xMax
					subTileXMax = xMax;
					lastTile = true;
				}

				yield return GeometryFactory.CreateEnvelope(subTileXMin, yMin,
				                                            subTileXMax, yMax,
				                                            spatialReference);
				subTileXMin = subTileXMax;
			} while (! lastTile);
		}

		[NotNull]
		private static IEnumerable<IEnvelope> GetSubtilesBySubdivisionCount(
			[NotNull] IEnvelope tileEnvelope,
			int tileSubdivisionCount,
			double minimumSubtileWidth)
		{
			Assert.ArgumentNotNull(tileEnvelope, nameof(tileEnvelope));

			if (tileSubdivisionCount <= 1)
			{
				yield return tileEnvelope;
				yield break;
			}

			// subdivide along X axis only
			// (subdividing along Y axis breaks the per-row processing of main tiles,
			//  changes to the 'completed tile' logic would be needed)

			double xMin;
			double yMin;
			double xMax;
			double yMax;
			tileEnvelope.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			double tileWidth = tileEnvelope.Width;

			// one subdivision --> two subtiles
			// two subdivisions --> three subtiles
			int subTileCount = tileSubdivisionCount + 1;

			double subtileWidth = tileWidth / subTileCount;

			if (subtileWidth < minimumSubtileWidth)
			{
				// the subtiles become too small -> return the entire envelope
				yield return tileEnvelope;
				yield break;
			}

			double subTileXMin = xMin;

			ISpatialReference spatialReference = tileEnvelope.SpatialReference;

			for (var subTileIndexX = 0; subTileIndexX < subTileCount; subTileIndexX++)
			{
				double subTileXMax = subTileXMin + subtileWidth;

				yield return GeometryFactory.CreateEnvelope(subTileXMin, yMin,
				                                            subTileXMax, yMax,
				                                            spatialReference);

				subTileXMin = subTileXMax;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private int CheckSubtile([NotNull] IEnvelope tileEnvelope,
		                         [NotNull] Box allBox,
		                         [NotNull] IEnumerable<IFeature> features,
		                         [NotNull] KnownGaps knownGaps)
		{
			Assert.ArgumentNotNull(tileEnvelope, nameof(tileEnvelope));
			Assert.ArgumentNotNull(allBox, nameof(allBox));
			Assert.ArgumentNotNull(features, nameof(features));
			Assert.ArgumentNotNull(knownGaps, nameof(knownGaps));

			WKSEnvelope tileBox;
			WKSEnvelope clipBox;
			IEnvelope clipEnvelope =
				GetClipEnvelope(tileEnvelope, allBox, _tolerance,
				                out tileBox, out clipBox);

			IList<IGeometry> geometriesToRelease;
			IList<IGeometry> clippedPolygons;
			if (_findGapsBelowTolerance)
			{
				clippedPolygons = GetClippedPolygonCopies(
					clipEnvelope, _spatialReference, features);
				geometriesToRelease = clippedPolygons;
			}
			else
			{
				clippedPolygons = GetClippedPolygons(
					clipEnvelope, features, out geometriesToRelease);
			}

			IPolygon clipPolygon = GeometryFactory.CreatePolygon(clipEnvelope);

			IList<IPolygon> gapPolygons = GetGapPolygons(clippedPolygons,
			                                             clipPolygon,
			                                             clipEnvelope,
			                                             tileEnvelope).ToList();

			var errorCount = 0;

			foreach (IPolygon completedGap in
				knownGaps.GetCompletedGaps(gapPolygons, clipEnvelope, tileEnvelope))
			{
				errorCount += CheckGapPolygon(completedGap);
			}

			ReleaseGeometries(geometriesToRelease);

			return errorCount;
		}

		private static void ReleaseGeometries([NotNull] ICollection<IGeometry> geometries)
		{
			foreach (IGeometry geometry in geometries)
			{
				Marshal.ReleaseComObject(geometry);
			}

			geometries.Clear();
		}

		[NotNull]
		private static IList<IGeometry> GetClippedPolygons(
			[NotNull] IEnvelope clipEnvelope,
			[NotNull] IEnumerable<IFeature> features,
			[NotNull] out IList<IGeometry> copiedGeometries)
		{
			var result = new List<IGeometry>();

			copiedGeometries = new List<IGeometry>();

			foreach (IFeature tileRow in features)
			{
				var polygon = (IPolygon) tileRow.Shape;

				bool copied;
				IPolygon clipped = GetClippedPolygon(polygon, clipEnvelope, out copied);
				result.Add(clipped);

				if (copied)
				{
					copiedGeometries.Add(clipped);
				}
			}

			return result;
		}

		[NotNull]
		private static IList<IGeometry> GetClippedPolygonCopies(
			[NotNull] IEnvelope clipEnvelope,
			[NotNull] ISpatialReference spatialReference,
			[NotNull] IEnumerable<IFeature> features)
		{
			var result = new List<IGeometry>();

			foreach (IFeature tileRow in features)
			{
				var polygon = (IPolygon) tileRow.Shape;

				IPolygon clippedPolygonCopy = GetClippedPolygonCopy(polygon, clipEnvelope);

				// gcs/projection is already the same, this just overwrites the tolerance etc.:
				clippedPolygonCopy.SpatialReference = spatialReference;

				result.Add(clippedPolygonCopy);
			}

			return result;
		}

		[NotNull]
		private static IPolygon GetClippedPolygon([NotNull] IPolygon polygon,
		                                          [NotNull] IEnvelope clipEnvelope,
		                                          out bool copied)
		{
			if (((IRelationalOperator) clipEnvelope).Contains(polygon))
			{
				copied = false;
				return polygon; // don't copy
			}

			copied = true;
			return GeometryUtils.GetClippedPolygon(polygon, clipEnvelope);
		}

		[NotNull]
		private static IPolygon GetClippedPolygonCopy([NotNull] IPolygon polygon,
		                                              [NotNull] IEnvelope clipEnvelope)
		{
			bool copied;
			IPolygon result = GetClippedPolygon(polygon, clipEnvelope, out copied);

			return copied
				       ? result
				       : GeometryFactory.Clone(polygon);
		}

		[NotNull]
		private IEnumerable<IPolygon> GetGapPolygons(
			[NotNull] IList<IGeometry> clippedPolygons,
			[NotNull] IPolygon clipPolygon,
			[NotNull] IEnvelope clipEnvelope,
			[NotNull] IEnvelope tileEnvelope)
		{
			if (clippedPolygons.Count <= 0)
			{
				IPolygon clipPolygonClone = GeometryFactory.Clone(clipPolygon);
				clipPolygonClone.SpatialReference = _spatialReference;

				yield return clipPolygonClone;
				yield break;
			}

			var unionedPolygons =
				(IPolygon) GeometryUtils.UnionGeometries(clippedPolygons);

			var multiGapPolygon =
				(IPolygon4) ((ITopologicalOperator) clipPolygon).Difference(unionedPolygons);

			// if there are areaOfInterest feature classes: get the areas of interest that intersect
			// the multiGapPolygon, union them, and get the intersection between multiGapPolygon and 
			// the unioned area of interest
			if (_areaOfInterestClasses.Count > 0 && ! multiGapPolygon.IsEmpty)
			{
				multiGapPolygon =
					GetIntersectionWithAreasOfInterest(multiGapPolygon, clipEnvelope);
			}

			var connectedComponents =
				(IGeometryCollection) multiGapPolygon.ConnectedComponentBag;

			int gapCount = connectedComponents.GeometryCount;

			if (gapCount > 0)
			{
				var gapInOffsetAreaEvaluator = new GapInOffsetAreaEvaluator(tileEnvelope,
				                                                            clipEnvelope);

				for (var gapIndex = 0; gapIndex < gapCount; gapIndex++)
				{
					var gapPolygon = (IPolygon) connectedComponents.Geometry[gapIndex];

					if (! gapInOffsetAreaEvaluator.IsInOffsetArea(gapPolygon))
					{
						yield return gapPolygon;
					}
				}
			}
		}

		[NotNull]
		private IPolygon4 GetIntersectionWithAreasOfInterest(
			[NotNull] IPolygon4 multiGapPolygon,
			[NotNull] IEnvelope clipEnvelope)
		{
			if (_tileAreasOfInterest.Count == 0)
			{
				// return entire combined gap polygon
				return multiGapPolygon;
			}

			ISpatialReference spatialReference = multiGapPolygon.SpatialReference;

			// clip the areas of interest before unioning
			IPolygon unionedAreaOfInterest = UnionPolygons(
				GetClippedPolygons(_tileAreasOfInterest, clipEnvelope),
				spatialReference);

			GeometryUtils.AllowIndexing(unionedAreaOfInterest);

			var topoOp = (ITopologicalOperator) multiGapPolygon;
			var intersection = (IPolygon4) topoOp.Intersect(
				unionedAreaOfInterest,
				esriGeometryDimension.esriGeometry2Dimension);

			return intersection;
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetClippedPolygons(
			[NotNull] IEnumerable<IPolygon> polygons,
			[NotNull] IEnvelope clipEnvelope)
		{
			foreach (IPolygon polygon in polygons)
			{
				bool copied;
				IPolygon clipped = GetClippedPolygon(polygon, clipEnvelope, out copied);

				if (! clipped.IsEmpty)
				{
					yield return clipped;
				}
			}
		}

		[NotNull]
		private static IPolygon UnionPolygons([NotNull] IEnumerable<IGeometry> polygons,
		                                      [CanBeNull] ISpatialReference spatialReference)
		{
			const bool allowProjectingInput = true;
			IGeometryBag bag = GeometryFactory.CreateBag(polygons,
			                                             CloneGeometry.IfChangeNeeded,
			                                             spatialReference,
			                                             allowProjectingInput);

			var result = new PolygonClass
			             {
				             SpatialReference = spatialReference
			             };

			result.ConstructUnion((IEnumGeometry) bag);

			return result;
		}

		[NotNull]
		private static IEnvelope GetClipEnvelope([NotNull] IEnvelope tileEnvelope,
		                                         [NotNull] Box allBox,
		                                         double tolerance,
		                                         out WKSEnvelope tileBox,
		                                         out WKSEnvelope clipBox)
		{
			Assert.ArgumentNotNull(tileEnvelope, nameof(tileEnvelope));
			Assert.ArgumentNotNull(allBox, nameof(allBox));

			tileEnvelope.QueryWKSCoords(out tileBox);

			// the tolerance needs to be enlarged, otherwise there can be missed
			// errors when features touch tile boundaries
			double offsetDistance = tolerance * 3;

			// the clip box is enlarged to the left/bottom, by the tolerance,
			// unless the tile is to the left/bottom of the test box
			clipBox.XMin = Math.Max(allBox.Min.X, tileBox.XMin - offsetDistance);
			clipBox.YMin = Math.Max(allBox.Min.Y, tileBox.YMin - offsetDistance);
			clipBox.XMax = tileBox.XMax;
			clipBox.YMax = tileBox.YMax;

			IEnvelope result = GeometryFactory.Clone(tileEnvelope);
			result.PutWKSCoords(ref clipBox);

			return result;
		}

		private int CheckGapPolygon([NotNull] IPolygon gapPolygon)
		{
			double absArea = Math.Abs(((IArea) gapPolygon).Area);

			if (absArea < double.Epsilon)
			{
				// zero-area polygon, ignore
				return NoError;
			}

			if (_maxArea > 0 && absArea > _maxArea)
			{
				// large polygon, will not be reported
				return NoError;
			}

			ISpatialReference spatialReference = gapPolygon.SpatialReference;

			string sliverMsg = string.Empty;
			if (_sliverLimit > 0)
			{
				double perimeter = gapPolygon.Length;
				double ratio = perimeter * perimeter / absArea;
				if (ratio <= _sliverLimit)
				{
					// no sliver Polygon, will not be reported
					return NoError;
				}

				sliverMsg =
					string.Format(
						LocalizableStrings.QaNoGaps_SliverParameters,
						FormatAreaComparison(ratio, ">", _sliverLimit, spatialReference),
						FormatLength(perimeter, spatialReference));
			}

			var sb = new StringBuilder(LocalizableStrings.QaNoGaps_GapFound);
			sb.AppendFormat("{0}",
			                _maxArea > 0
				                ? FormatAreaComparison(absArea, "<=", _maxArea, spatialReference)
				                : FormatArea(absArea, spatialReference));
			sb.Append(sliverMsg);
			sb.Append(")");

			// the error polygon is guaranteed to be a clone already, no further cloning needed.
			return ReportError(sb.ToString(), gapPolygon, GetIssueCode(), null);
		}

		private IssueCode GetIssueCode()
		{
			if (_sliverLimit > 0 && _maxArea > 0)
			{
				return Codes[Code.Gap_AreaTooSmallAndSliverRatioTooLarge];
			}

			if (_sliverLimit > 0)
			{
				return Codes[Code.Gap_SliverRatioTooLarge];
			}

			if (_maxArea > 0)
			{
				return Codes[Code.Gap_AreaTooSmall];
			}

			return Codes[Code.Gap];
		}

		[NotNull]
		private static ISpatialReference GetSpatialReferenceAtMaximumResolution(
			[NotNull] IEnumerable<IFeatureClass> featureClasses,
			out double maxResolution)
		{
			Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));

			ISpatialReference result = null;
			maxResolution = 0;
			foreach (IFeatureClass featureClass in featureClasses)
			{
				var geoDataset = (IGeoDataset) featureClass;
				ISpatialReference spatialReference = geoDataset.SpatialReference;
				Assert.NotNull(spatialReference, "Dataset without spatial reference");

				if (result == null)
				{
					result = Clone(spatialReference);
				}

				double xyResolution = SpatialReferenceUtils.GetXyResolution(spatialReference);

				maxResolution = Math.Max(xyResolution, maxResolution);
			}

			Assert.NotNull(result, "no spatial reference found");

			((ISpatialReferenceTolerance) result).XYTolerance = maxResolution;

			return result;
		}

		[NotNull]
		private static ISpatialReference GetUniqueSpatialReference(
			[NotNull] IEnumerable<IFeatureClass> featureClasses, out double tolerance)
		{
			Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));

			ISpatialReference result = null;
			tolerance = 0;

			foreach (IFeatureClass featureClass in featureClasses)
			{
				var geoDataset = (IGeoDataset) featureClass;
				ISpatialReference spatialReference = geoDataset.SpatialReference;
				Assert.NotNull(spatialReference, "Dataset without spatial reference");

				if (result == null)
				{
					result = spatialReference;
					tolerance = ((ISpatialReferenceTolerance) result).XYTolerance;
				}
				else
				{
					const bool comparePrecisionAndTolerance = true;
					const bool compareVerticalCoordinateSystems = false;
					if (! SpatialReferenceUtils.AreEqual(result, spatialReference,
					                                     comparePrecisionAndTolerance,
					                                     compareVerticalCoordinateSystems))
					{
						throw new ArgumentException(
							"All datasets must have the same spatial reference (with equal tolerance/resolution)");
					}
				}
			}

			return Assert.NotNull(
				result, "No feature classes with spatial reference specified");
		}

		[NotNull]
		private static ISpatialReference Clone([NotNull] ISpatialReference spatialReference)
		{
			return (ISpatialReference) ((IClone) spatialReference).Clone();
		}

		/// <summary>
		/// Helper for evaluating if a gap is fully contained in the offset area between
		/// the tile envelope and the clip envelope (enlarged by a multiple of the xy tolerance to the west/south).
		/// </summary>
		private class GapInOffsetAreaEvaluator
		{
			[NotNull] private readonly IEnvelope _tileEnvelope;
			[NotNull] private readonly IEnvelope _clipEnvelope;
			private readonly double _tileXMin;
			private readonly double _tileYMin;

			private double? _offsetArea;
			private IEnvelope _gapEnvelopeTemplate;

			public GapInOffsetAreaEvaluator([NotNull] IEnvelope tileEnvelope,
			                                [NotNull] IEnvelope clipEnvelope)
			{
				Assert.ArgumentNotNull(tileEnvelope, nameof(tileEnvelope));
				Assert.ArgumentNotNull(clipEnvelope, nameof(clipEnvelope));

				_tileEnvelope = tileEnvelope;
				_clipEnvelope = clipEnvelope;

				_tileXMin = tileEnvelope.XMin;
				_tileYMin = tileEnvelope.YMin;
			}

			public bool IsInOffsetArea([NotNull] IPolygon gapPolygon)
			{
				if (_gapEnvelopeTemplate == null)
				{
					_gapEnvelopeTemplate = new EnvelopeClass();
				}

				gapPolygon.QueryEnvelope(_gapEnvelopeTemplate);

				double gapXMin;
				double gapYMin;
				double gapXMax;
				double gapYMax;
				_gapEnvelopeTemplate.QueryCoords(out gapXMin, out gapYMin,
				                                 out gapXMax, out gapYMax);

				if (gapXMin >= _tileXMin && gapYMin >= _tileYMin)
				{
					// the gap envelope does not overlap the left/lower tile boundary
					return false;
				}

				if (gapXMax <= _tileXMin || gapYMax <= _tileYMin)
				{
					// the gap envelope has no interior intersection with the tile envelope
					return true;
				}

				if (_offsetArea == null)
				{
					_offsetArea = ((IArea) _clipEnvelope).Area - ((IArea) _tileEnvelope).Area;
				}

				double gapArea = ((IArea) gapPolygon).Area;

				if (gapArea <= _offsetArea.Value)
				{
					// the gap *could* be fully within the offset area - check if it overlaps the tile envelope

					GeometryUtils.AllowIndexing(gapPolygon);
					if (! ((IRelationalOperator) _tileEnvelope).Overlaps(gapPolygon))
					{
						return true;
					}
				}

				return false;
			}
		}
	}
}
