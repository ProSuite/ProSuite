using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Finds all invalid geometries (null, empty, not simple)
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaSimpleGeometry : ContainerTest
	{
		private readonly bool _allowNonPlanarLines;
		private readonly ISpatialReference _spatialReference;
		private readonly ISpatialReference _reducedToleranceSpatialReference;
		private readonly esriGeometryType _shapeType;
		private readonly double _xyResolution;
		private readonly double _zResolution;
		private readonly double _xyTolerance;
		private readonly bool _usesReducedSimplifyTolerance;
		private readonly IPoint _pointTemplate = new PointClass();
		private const double _minimumToleranceFactor = 0.1;
		private const double _maximumToleranceFactor = 1.0;
		private const double _noChangeToleranceFactor = 1.0;

		private const double _defaultToleranceFactor = 0.4; // 0.3546??  -> 1 / (sqrt(2) / 2)

		private readonly string _shapeFieldName;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ShortSegment = "ShortSegment";
			public const string SelfIntersection = "SelfIntersection";
			public const string DuplicatePoints = "DuplicatePoints";
			public const string IdenticalRings = "IdenticalRings";
			public const string UnclosedRing = "UnclosedRing";
			public const string EmptyPart = "EmptyPart";
			public const string IncorrectRingOrientation = "RingOrientation";
			public const string IncorrectSegmentOrientation = "SegmentOrientation";
			public const string Undefined = "Undefined";
			public const string Unknown = "Unknown";
			public const string Null = "NullShape";
			public const string Empty = "EmptyShape";

			public Code() : base("SimpleGeometry") { }
		}

		#endregion

		// Note: ContainerTests cannot test for missing or empty geometries
		// because the container's search filter will miss such features!

		[Doc(nameof(DocStrings.QaSimpleGeometry_0))]
		public QaSimpleGeometry(
				[Doc(nameof(DocStrings.QaSimpleGeometry_featureClass))]
				IReadOnlyFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, false, _defaultToleranceFactor) { }

		[Doc(nameof(DocStrings.QaSimpleGeometry_1))]
		public QaSimpleGeometry(
				[Doc(nameof(DocStrings.QaSimpleGeometry_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaSimpleGeometry_allowNonPlanarLines))]
				bool allowNonPlanarLines)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, allowNonPlanarLines, _defaultToleranceFactor) { }

		[Doc(nameof(DocStrings.QaSimpleGeometry_2))]
		public QaSimpleGeometry(
			[Doc(nameof(DocStrings.QaSimpleGeometry_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaSimpleGeometry_allowNonPlanarLines))]
			bool allowNonPlanarLines,
			[Doc(nameof(DocStrings.QaSimpleGeometry_toleranceFactor))]
			double toleranceFactor)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentCondition(toleranceFactor >= _minimumToleranceFactor &&
			                         toleranceFactor <= _maximumToleranceFactor,
			                         "Invalid tolerance factor: {0}; value must be between {1} and {2}",
			                         toleranceFactor,
			                         _minimumToleranceFactor,
			                         _maximumToleranceFactor);

			_shapeType = featureClass.ShapeType;
			_allowNonPlanarLines = allowNonPlanarLines;
			_spatialReference = featureClass.SpatialReference;
			_shapeFieldName = featureClass.ShapeFieldName;

			Assert.ArgumentCondition(_spatialReference != null,
			                         "feature class has no spatial reference");

			_xyResolution = SpatialReferenceUtils.GetXyResolution(_spatialReference);

			_zResolution = DatasetUtils.GetGeometryDef(featureClass).HasZ
				               ? GeometryUtils.GetZResolution(_spatialReference)
				               : double.NaN;

			_xyTolerance = ((ISpatialReferenceTolerance) _spatialReference).XYTolerance;

			if (toleranceFactor.Equals(_noChangeToleranceFactor))
			{
				_reducedToleranceSpatialReference = _spatialReference;
				_usesReducedSimplifyTolerance = false;
			}
			else
			{
				_reducedToleranceSpatialReference = GetReducedToleranceSpatialReference(
					_spatialReference, toleranceFactor);
				_usesReducedSimplifyTolerance = true;
			}
		}

		[InternallyUsedTest]
		public QaSimpleGeometry([NotNull] QaSimpleGeometryDefinition definition)
			: this((IReadOnlyFeatureClass)definition.FeatureClass,
			       definition.AllowNonPlanarLines, definition.ToleranceFactor)
		{ }

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			IssueCode issueCode;
			IGeometry shape = feature.Shape;
			if (shape == null)
			{
				return ReportError(
					LocalizableStrings.QaSimpleGeometry_Null,
					InvolvedRowUtils.GetInvolvedRows(feature),
					null, Codes[Code.Null], _shapeFieldName);
			}

			if (shape.IsEmpty)
			{
				return ReportError(
					LocalizableStrings.QaSimpleGeometry_Empty,
					InvolvedRowUtils.GetInvolvedRows(feature),
					GeometryFactory.Clone(shape), Codes[Code.Empty], _shapeFieldName);
			}

			// allow non-planar lines for network edges?
			// bool allowNonPlanarLines = IsNetworkEdge(row) (based on feature class)
			// Important: (row is IEdgeFeature) won't work since the row may be a proxy;

			ISpatialReference targetSpatialReference = _usesReducedSimplifyTolerance
				                                           ? _reducedToleranceSpatialReference
				                                           : _spatialReference;

			string nonSimpleReason;
			if (IsGeometrySimple(shape, targetSpatialReference,
			                     out nonSimpleReason,
			                     out issueCode))
			{
				return NoError;
			}

			// TODO: if allowNonPlanarLines, then short segments may be reported, but they are not located
			// if allowNonPlanarLines is set to false, then the error geometry indicates the short segments
			// TODO: deal with this by ignoring the "short segments" error if allowNonPlanarLines is true and no difference is detected?
			// NOTE: this seems to have been a COMBINED self-intersection/short-segment situation. 
			// In other cases the short segments are located also with allowNonPlanarLines=true
			IGeometry errorGeometry;

			if (! TryGetErrorGeometry(shape, feature, _allowNonPlanarLines,
			                          targetSpatialReference, issueCode,
			                          out errorGeometry))
			{
				return NoError;
			}

			return ReportError(
				nonSimpleReason, InvolvedRowUtils.GetInvolvedRows(feature), errorGeometry,
				issueCode, _shapeFieldName);
		}

		[NotNull]
		private static ISpatialReference GetReducedToleranceSpatialReference(
			[NotNull] ISpatialReference spatialReference, double toleranceFactor)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			var result = (ISpatialReference) ((IClone) spatialReference).Clone();

			var srefTolerance = (ISpatialReferenceTolerance) result;
			srefTolerance.XYTolerance = srefTolerance.XYTolerance * toleranceFactor;

			if (srefTolerance.XYToleranceValid ==
			    esriSRToleranceEnum.esriSRToleranceIsTooSmall)
			{
				var srefResolution = (ISpatialReferenceResolution) result;

				const bool standardUnits = true;
				double resolution = srefResolution.XYResolution[standardUnits];
				srefResolution.set_XYResolution(standardUnits, resolution * toleranceFactor);
			}

			return result;
		}

		private bool TryGetErrorGeometry([NotNull] IGeometry shape,
		                                 [NotNull] IReadOnlyFeature feature,
		                                 bool allowNonPlanarLines,
		                                 [NotNull] ISpatialReference targetSpatialReference,
		                                 [CanBeNull] IssueCode issueCode,
		                                 [CanBeNull] out IGeometry errorGeometry)
		{
			IGeometry simplified = GeometryFactory.Clone(shape);
			GeometryUtils.EnsureSpatialReference(simplified, targetSpatialReference);

			var releaseSimplified = true;

			try
			{
				// preserve from/to point of polygons to avoid reporting start point
				// in difference detection with reportDuplicateVertices == true
				GeometryUtils.Simplify(simplified,
				                       allowReorder: false,
				                       allowPathSplitAtIntersections: ! allowNonPlanarLines);

				if (simplified.IsEmpty)
				{
					// Simplify() emptied the shape. 
					// Use the original extent as the error geometry, if it is not empty
					errorGeometry = TestUtils.GetEnlargedExtentPolygon(shape, _xyTolerance);
					return true;
				}

				IGeometry projectedShape;
				GeometryUtils.EnsureSpatialReference(shape, targetSpatialReference, false,
				                                     out projectedShape);

				IList<WKSPointZ> changedPoints = GetChangedPoints(projectedShape,
					simplified,
					allowNonPlanarLines);

				if (changedPoints.Count > 0)
				{
					errorGeometry = GetErrorGeometry(changedPoints, feature);
					return true;
				}

				// no changed points found yet

				if (! GeometryUtils.AreEqual(projectedShape, simplified))
				{
					// there is a difference, but the change detection missed it
					// -> return entire simplified geometry

					_msg.Debug(
						"Actual difference not detected in QaSimpleGeometry.GetChangedPoints():");
					_msg.Debug("Simplified geometry:");
					_msg.Debug(GeometryUtils.ToString(simplified));
					_msg.Debug("Original geometry:");
					_msg.Debug(GeometryUtils.ToString(shape));

					releaseSimplified = false;
					errorGeometry = simplified;
					return true;
				}

				if ((_shapeType == esriGeometryType.esriGeometryPolyline ||
				     _shapeType == esriGeometryType.esriGeometryPolygon) &&
				    (Equals(issueCode, Codes[Code.SelfIntersection]) ||
				     Equals(issueCode, Codes[Code.ShortSegment])))
				{
					double zTolerance = GeometryUtils.GetZTolerance(shape);

					// if there are vertical segments, then let's assume this is the only reason --> no error
					if (HasVerticalSegments(simplified, zTolerance))
					{
						errorGeometry = null;
						return false; // no error
					}
				}

				// no good reason found, return the entire simplified geometry

				_msg.Debug("Unable to determine exact location of non-simple reason:");
				_msg.Debug("Simplified geometry:");
				_msg.Debug(GeometryUtils.ToString(simplified));
				_msg.Debug("Original geometry:");
				_msg.Debug(GeometryUtils.ToString(shape));

				releaseSimplified = false;
				errorGeometry = simplified;
				return true;
			}
			finally
			{
				if (releaseSimplified)
				{
					Marshal.ReleaseComObject(simplified);
				}
			}
		}

		private static bool HasVerticalSegments([NotNull] IGeometry shape,
		                                        double zTolerance)
		{
			if (! GeometryUtils.IsZAware(shape))
			{
				return false;
			}

			var segments = shape as ISegmentCollection;
			if (segments == null)
			{
				return false;
			}

			foreach (ISegment segment in GeometryUtils.GetSegments(
				         segments.EnumSegments, allowRecycling: true))
			{
				bool isVertical = IsVertical(segment, zTolerance);

				if (isVertical)
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsVertical([NotNull] ISegment segment, double zTolerance)
		{
			if (! MathUtils.AreSignificantDigitsEqual(0d, segment.Length))
			{
				// 2D length is non-zero
				return false;
			}

			// 2D-length is zero
			double fromZ;
			double toZ;
			((ISegmentZ) segment).GetZs(out fromZ, out toZ);

			double zDifference = Math.Abs(toZ - fromZ);
			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(fromZ, toZ);

			if (double.IsNaN(zTolerance))
			{
				zTolerance = 0;
			}

			return ! MathUtils.IsWithinTolerance(zDifference, zTolerance, epsilon);
		}

		[NotNull]
		private IList<WKSPointZ> GetChangedPoints([NotNull] IGeometry projectedShape,
		                                          [NotNull] IGeometry simplified,
		                                          bool allowNonPlanarLines)
		{
			var geometryComparison = new GeometryComparison(
				projectedShape, simplified, _xyResolution, _zResolution);

			// return points that are removed / added by simplify -> symmetric = true

			// also report duplicate points, such as duplicate points removed by simplify
			IList<WKSPointZ> result = geometryComparison.GetDifferentVertices(
				symmetric: true, reportDuplicateVertices: true);

			if (_shapeType == esriGeometryType.esriGeometryPolyline)
			{
				var polylineOriginal = (IPolyline) projectedShape;
				var polylineSimplified = (IPolyline) simplified;

				// add split points in poly lines
				AddChangedLineEnds(polylineOriginal, polylineSimplified, result);

				// pan handles are still not found (simplify seems to do nothing): end point touches interior:
				if (! allowNonPlanarLines)
				{
					AddInteriorTouchingLineEnds(polylineOriginal, result);
				}
			}

			return result;
		}

		private void AddInteriorTouchingLineEnds(
			[NotNull] IPolyline polyline,
			[NotNull] ICollection<WKSPointZ> changedPoints)
		{
			foreach (IPath path in GeometryUtils.GetPaths(polyline))
			{
				if (path.IsClosed)
				{
					continue;
				}

				AddInteriorTouchingVertex(path, 0, changedPoints);
				AddInteriorTouchingVertex(path, ((IPointCollection) path).PointCount - 1,
				                          changedPoints);
			}
		}

		private void AddInteriorTouchingVertex(
			[NotNull] IPath path,
			int vertexIndex,
			[NotNull] ICollection<WKSPointZ> changedPoints)
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(path, _spatialReference);
			var points = (IPointCollection) polyline;

			points.QueryPoint(vertexIndex, _pointTemplate);

			// TODO revise vertex index?
			points.RemovePoints(vertexIndex, 1);

			if (GeometryUtils.Intersects(_pointTemplate, polyline))
				//GeometryUtils.AllowIndexing((IGeometry) highLevelPathPoints);

				//int? foundSegmentIdx =
				//    GeometryUtils.FindHitSegmentIndex((IGeometry) highLevelPathPoints, testPoint,
				//                                      GeometryUtils.GetXyResolution(_spatialReference));

				//if (foundSegmentIdx != null)
			{
				WKSPointZ wksPoint;

				double x;
				double y;
				_pointTemplate.QueryCoords(out x, out y);

				wksPoint.X = x;
				wksPoint.Y = y;
				wksPoint.Z = _pointTemplate.Z;

				changedPoints.Add(wksPoint);
			}

			Marshal.ReleaseComObject(polyline);
		}

		private void AddChangedLineEnds([NotNull] IPolyline polyline,
		                                [NotNull] IPolyline simplified,
		                                [NotNull] ICollection<WKSPointZ> toChangedPoints)
		{
			IMultipoint shapeEnds =
				GeometryFactory.CreateMultipoint(GetPathEndPoints(polyline));
			IMultipoint simplifiedEnds =
				GeometryFactory.CreateMultipoint(GetPathEndPoints(simplified));

			var geometryComparison = new GeometryComparison(
				shapeEnds, simplifiedEnds, _xyResolution, _zResolution);

			const bool symmetric = true;
			const bool reportDuplicateVertices = false;
			IList<WKSPointZ> changedLineEnds =
				geometryComparison.GetDifferentVertices(symmetric, reportDuplicateVertices);

			// duplicate changed points are ok, the result is simplified
			foreach (WKSPointZ changedLineEnd in changedLineEnds)
			{
				toChangedPoints.Add(changedLineEnd);
			}
		}

		[NotNull]
		private static IEnumerable<IPoint> GetPathEndPoints([NotNull] IPolyline polyline)
		{
			foreach (IPath path in GeometryUtils.GetPaths(polyline))
			{
				yield return path.FromPoint;
				yield return path.ToPoint;
			}
		}

		[NotNull]
		private static IGeometry GetErrorGeometry(
			[NotNull] ICollection<WKSPointZ> changedPoints,
			[NotNull] IReadOnlyFeature feature)
		{
			var changedPointArray = new WKSPointZ[changedPoints.Count];
			changedPoints.CopyTo(changedPointArray, 0);

			IGeometry result = GeometryFactory.CreateMultipoint(
				changedPointArray,
				DatasetUtils.GetGeometryDef((IReadOnlyFeatureClass) feature.Table));

			// duplicate points are returned for self-intersections -> simplify
			GeometryUtils.Simplify(result);

			return result;
		}

		private bool IsGeometrySimple([NotNull] IGeometry geometry,
		                              [NotNull] ISpatialReference targetSpatialReference,
		                              out string nonSimpleReasonDescription,
		                              [CanBeNull] out IssueCode issueCode)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			GeometryNonSimpleReason? nonSimpleReason;
			bool isSimple = GeometryUtils.IsGeometrySimple(geometry,
			                                               targetSpatialReference,
			                                               _allowNonPlanarLines,
			                                               out nonSimpleReasonDescription,
			                                               out nonSimpleReason);

			if (isSimple)
			{
				issueCode = null;
				return true;
			}

			Assert.NotNull(nonSimpleReason, "Geometry is not simple, but reason is null");
			issueCode = GetIssueCode(nonSimpleReason.Value);

			return false;
		}

		[CanBeNull]
		private static IssueCode GetIssueCode(GeometryNonSimpleReason nonSimpleReason)
		{
			return Codes[GetCode(nonSimpleReason)];
		}

		[NotNull]
		private static string GetCode(GeometryNonSimpleReason nonSimpleReason)
		{
			switch (nonSimpleReason)
			{
				case GeometryNonSimpleReason.Unknown:
					return Code.Unknown;

				case GeometryNonSimpleReason.ShortSegments:
					return Code.ShortSegment;

				case GeometryNonSimpleReason.SelfIntersections:
					return Code.SelfIntersection;

				case GeometryNonSimpleReason.DuplicatePoints:
					return Code.DuplicatePoints;

				case GeometryNonSimpleReason.IdenticalRings:
					return Code.IdenticalRings;

				case GeometryNonSimpleReason.UnclosedRing:
					return Code.UnclosedRing;

				case GeometryNonSimpleReason.EmptyPart:
					return Code.EmptyPart;

				case GeometryNonSimpleReason.IncorrectRingOrientation:
					return Code.IncorrectRingOrientation;

				case GeometryNonSimpleReason.IncorrectSegmentOrientation:
					return Code.IncorrectSegmentOrientation;

				default:
					return Code.Undefined;
			}
		}
	}
}
