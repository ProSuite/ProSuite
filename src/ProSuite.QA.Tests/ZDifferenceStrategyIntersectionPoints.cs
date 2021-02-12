using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.PointEnumerators;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	public class ZDifferenceStrategyIntersectionPoints : ZDifferenceStrategy
	{
		private readonly Func<int, bool> _useDistanceFromPlane;
		private readonly double _coplanarityTolerance;
		private readonly bool _ignoreNonCoplanarReferenceRings;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string TooSmall = "ZDifferenceTooSmall";
			public const string TooLarge = "ZDifferenceTooLarge";

			public const string ConstraintNotFulfilled = "ConstraintNotFulfilled";

			public const string UndefinedZ = "UndefinedZ";

			public const string FaceNotCoplanar = "FaceNotCoplanar";

			public Code() : base("IntersectionZDifference") { }
		}

		#endregion

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public ZDifferenceStrategyIntersectionPoints(
			double minimumZDifference, [CanBeNull] string minimumZDifferenceExpression,
			double maximumZDifference, [CanBeNull] string maximumZDifferenceExpression,
			[CanBeNull] string zRelationConstraint, bool expressionCaseSensitivity,
			[NotNull] IErrorReporting errorReporting,
			[NotNull] Func<double, string, double, string, string> formatComparisonFunction,
			Func<int, bool> useDistanceFromPlane = null,
			double coplanarityTolerance = 0,
			bool ignoreNonCoplanarReferenceRings = false)
			: base(minimumZDifference, minimumZDifferenceExpression,
			       maximumZDifference, maximumZDifferenceExpression,
			       zRelationConstraint, expressionCaseSensitivity,
			       errorReporting, formatComparisonFunction)
		{
			_useDistanceFromPlane = useDistanceFromPlane;
			_coplanarityTolerance = coplanarityTolerance;
			_ignoreNonCoplanarReferenceRings = ignoreNonCoplanarReferenceRings;
		}

		[PublicAPI]
		public bool IgnoreUndefinedZValues { get; set; }

		public static IEnumerable<IIntersectionPoint> GetIntersections(
			[NotNull] IGeometry shape1,
			[NotNull] IGeometry shape2,
			ISpatialReference spatialReference,
			double xyTolerance,
			Func<IEnumerable<Linestring>> getLineStrings1 = null,
			Func<IEnumerable<Linestring>> getLineStrings2 = null)
		{
			// NOTE it is assumed that the features are NOT disjoint			

			if (GeometryUtils.Touches(shape1, shape2))
			{
				yield break;
			}

			var geomType1 = shape1.GeometryType;
			var geomType2 = shape2.GeometryType;

			if (geomType1 == esriGeometryType.esriGeometryPoint)
			{
				var point = (IPoint) shape1;
				yield return new AoIntersectionPoint(point, GetZ(point, shape2));
			}
			else if (geomType2 == esriGeometryType.esriGeometryPoint)
			{
				var point = (IPoint) shape2;
				yield return new Pnt3DIntersectionPoint(new Pnt3D(point.X, point.Y,
				                                                  GetZ(point, shape1)),
				                                        spatialReference, point.Z);
			}
			else if (IsBasedOnLineStrings(shape1) && ! IsBasedOnLineStrings(shape2))
			{
				// TODO linestrings to (presumably) multipoint
				// TODO filter out points on line endpoints
			}
			else if (! IsBasedOnLineStrings(shape1) && IsBasedOnLineStrings(shape2))
			{
				// TODO (presumably) multipoint to linestrings
				// TODO filter out points on line endpoints
			}
			else if (IsBasedOnLineStrings(shape1) && IsBasedOnLineStrings(shape2))
			{
				var lineStrings1 = getLineStrings1?.Invoke().ToList() ??
				                   GetLineStrings(shape1);
				var lineStrings2 = getLineStrings2?.Invoke().ToList() ??
				                   GetLineStrings(shape2);

				// linestrings (from multipatch or polycurve) to other linestrings (multipatch/polycurve)
				foreach (var p in lineStrings1
					.SelectMany(
						l1 => lineStrings2
						      .Where(l2 => l1.ExtentsIntersectXY(l2, xyTolerance))
						      .SelectMany(l2 => GetIntersections(
							                  l1, l2,
							                  spatialReference, xyTolerance))))
				{
					// TODO filter out points on line endpoints
					yield return p;
				}
			}
			else
			{
				// unexpected geometry combination
				throw new ArgumentException(
					$@"Unexpected geometry combination: {shape1.GeometryType}/{shape2.GeometryType}");
			}
		}

		public static IEnumerable<Either<NonPlanarError, IEnumerable<IIntersectionPoint>>>
			GetDistanceToPlane([NotNull] IGeometry vertices,
			                   [NotNull] IFeature planarFeature,
			                   double coplanarityTolerance)
		{
			return GetPlanarRings(planarFeature, coplanarityTolerance)
				.Select(e => e.Select(plane => GetIntersectionPoints(plane, vertices)));
		}

		protected override int ReportErrors(IFeature feature1, int tableIndex1,
		                                    IFeature feature2, int tableIndex2)
		{
			if (_useDistanceFromPlane != null)
			{
				if (_useDistanceFromPlane(tableIndex1))
				{
					return ReportPlaneErrors(feature1, tableIndex1,
					                         feature2, tableIndex2);
				}

				if (_useDistanceFromPlane(tableIndex2))
				{
					return ReportPlaneErrors(feature2, tableIndex2,
					                         feature1, tableIndex1);
				}
			}

			return GetIntersections(feature1.Shape, feature2.Shape,
			                        DatasetUtils.GetSpatialReference(feature1),
			                        GeometryUtils.GetXyTolerance(feature1),
			                        () => GetLineStrings(feature1),
			                        () => GetLineStrings(feature2))
				.Sum(ip => CheckIntersection(ip.Point,
				                             ip.OtherZ,
				                             ip.Distance,
				                             feature1, tableIndex1,
				                             feature2, tableIndex2));
		}

		private int ReportPlaneErrors([NotNull] IFeature vertexFeature,
		                              int vertexTableIndex,
		                              [NotNull] IFeature planarFeature,
		                              int planarTableIndex)
		{
			Assert.False(_useDistanceFromPlane(planarTableIndex),
			             "Cannot compare plane with plane");

			return GetDistanceToPlane(vertexFeature.Shape,
			                          planarFeature,
			                          _coplanarityTolerance)
				.Sum(r => r.Match(err => HandleNonPlanarError(err, planarFeature),
				                  pds => pds.Sum(
					                  pd => CheckIntersection(
						                  pd.Point, pd.OtherZ, pd.Distance,
						                  vertexFeature, vertexTableIndex,
						                  planarFeature, planarTableIndex))));
		}

		private int HandleNonPlanarError([NotNull] NonPlanarError nonPlanarError,
		                                 [NotNull] IFeature planarFeature)
		{
			return _ignoreNonCoplanarReferenceRings
				       ? NoError
				       : ErrorReporting.Report(
					       nonPlanarError.Message,
					       nonPlanarError.SegmentsPlane.Geometry,
					       Codes[Code.FaceNotCoplanar],
					       ((IFeatureClass) planarFeature.Class).ShapeFieldName,
					       new object[] {nonPlanarError.MaximumOffset},
					       planarFeature);
		}

		[NotNull]
		private static IEnumerable<IIntersectionPoint> GetIntersectionPoints(
			[NotNull] SegmentsPlane segmentsPlane,
			[NotNull] IGeometry vertices)
		{
			// TODO ignore intersections with points that are too far outside of the ring footprint

			// NOTE for efficiency, this should only be calculated if an error would otherwise be reported
			// (requires reconstruction of the ring geometry)
			// -> add method to IIntersectionPoint to measure the actual distance to the ring (0 if inside)
			return (IEnumerable<IIntersectionPoint>) GetPoints(vertices)
				.Select(v => GetIntersectionPoint(segmentsPlane.Plane, v));
		}

		[NotNull]
		private static AoIntersectionPoint GetIntersectionPoint(
			[NotNull] Plane3D plane, [NotNull] IPoint pt) =>
			new AoIntersectionPoint(pt, plane.GetZ(pt.X, pt.Y),
			                        plane.GetDistanceAbs(pt.X, pt.Y, pt.Z));

		[NotNull]
		private static IEnumerable<IPoint> GetPoints([NotNull] IGeometry vertices)
		{
			// TODO for rings, exclude duplicate end point
			// (currently these are reported twice, deduped later by error administrator)

			return GeometryUtils.GetPoints(vertices);
		}

		private static IEnumerable<Either<NonPlanarError, SegmentsPlane>> GetPlanarRings(
			[NotNull] IFeature planarFeature, double coplanarityTolerance)
		{
			SegmentsPlaneProvider provider =
				SegmentsPlaneProvider.Create(planarFeature, false);

			SegmentsPlane segPlane;
			while ((segPlane = provider.ReadPlane()) != null)
			{
				// check for co-planarity
				double maximumOffset;
				yield return IsCoplanar(segPlane, planarFeature, coplanarityTolerance,
				                        out maximumOffset)
					             ? new Either<NonPlanarError, SegmentsPlane>(segPlane)
					             : new NonPlanarError("Coplanarity tolerance is exceeded",
					                                  maximumOffset, segPlane);
			}
		}

		private static bool IsCoplanar([NotNull] SegmentsPlane segmentsPlane,
		                               [NotNull] IFeature planarFeature,
		                               double coplanarityTolerance,
		                               out double maximumOffset)
		{
			maximumOffset = GetMaximumOffset(segmentsPlane);
			if (maximumOffset <= coplanarityTolerance)
			{
				return true;
			}

			var sref = Assert.NotNull(DatasetUtils.GetSpatialReference(planarFeature));

			var xyResolution = SpatialReferenceUtils.GetXyResolution(sref);
			var zResolution = SpatialReferenceUtils.GetZResolution(sref);

			return maximumOffset <= GeomUtils.AdjustCoplanarityTolerance(
				       segmentsPlane.Plane, coplanarityTolerance, zResolution,
				       xyResolution);
		}

		private static double GetMaximumOffset([NotNull] SegmentsPlane segmentsPlane) =>
			segmentsPlane
				.GetPoints()
				.Select(p => segmentsPlane.Plane.GetDistanceAbs(p.X, p.Y, p.Z))
				.Max();

		private static bool IsBasedOnLineStrings(IGeometry shape)
		{
			switch (shape.GeometryType)
			{
				case esriGeometryType.esriGeometryPolyline:
				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryMultiPatch:
					return true;

				default:
					return false;
			}
		}

		private static IEnumerable<Pnt3DIntersectionPoint> GetIntersections(
			Linestring lines1,
			Linestring lines2,
			ISpatialReference spatialReference,
			double xyTolerance)
		{
			// Increase tolerance by epsilon to find intersections that differ exactly by tolerance
			xyTolerance +=
				MathUtils.GetDoubleSignificanceEpsilon(lines1.XMax, lines1.YMax);

			var intersectionPoints =
				GeomTopoOpUtils.GetIntersectionPoints(lines1, lines2, xyTolerance);
			var nanTargetVertices =
				intersectionPoints.Where(p => double.IsNaN(p.VirtualTargetVertex));

			foreach (var nanVertex in nanTargetVertices)
			{
				_msg.Warn(
					$"Target vertex is NaN: {nanVertex}; linestring1: {lines1}; linestring2: {lines2}");
			}

			return intersectionPoints
			       .Where(p => ! double.IsNaN(p.VirtualTargetVertex))
			       .Where(p => p.Type == IntersectionPointType.Crossing)
			       .Select(p => new Pnt3DIntersectionPoint(p.Point,
			                                               spatialReference,
			                                               GetZ(p, lines2)));
		}

		private static double GetZ([NotNull] IntersectionPoint3D point,
		                           [NotNull] Linestring linestring)
		{
			double factor;
			var segmentIndex = point.GetLocalTargetIntersectionSegmentIdx(linestring,
			                                                              out factor);

			return linestring.GetSegment(segmentIndex).GetPointAlong(factor, true).Z;
		}

		private static double GetZ([NotNull] IPoint point,
		                           [NotNull] IGeometry otherGeometry)
		{
			if (otherGeometry.GeometryType == esriGeometryType.esriGeometryPoint)
			{
				return ((IPoint) otherGeometry).Z;
			}

			return GeometryUtils.GetZValueFromGeometry(
				otherGeometry, point,
				GeometryUtils.GetXyTolerance(otherGeometry));
		}

		private static IEnumerable<Linestring> GetLineStrings(
			[NotNull] IFeature feature)
		{
			var shape = feature.Shape;
			if (shape.GeometryType == esriGeometryType.esriGeometryMultiPatch)
			{
				var indexedMultipatch = feature as IIndexedMultiPatch;
				if (indexedMultipatch != null)
				{
					return new[] {GetLineString(indexedMultipatch.GetSegments())};
				}
			}

			return GetLineStrings(shape);
		}

		private static IEnumerable<Linestring> GetLineStrings([NotNull] IGeometry shape)
		{
			if (shape.GeometryType == esriGeometryType.esriGeometryMultiPatch)
			{
				var segments = QaGeometryUtils.GetSegments((IMultiPatch) shape);

				yield return GetLineString(segments);
			}
			else if (shape.GeometryType == esriGeometryType.esriGeometryPolygon ||
			         shape.GeometryType == esriGeometryType.esriGeometryPolyline)
			{
				foreach (var path in GetLinearizedPaths(shape))
				{
					yield return GeometryConversionUtils.GetLinestring(path);
				}
			}
			else
			{
				throw new ArgumentException(
					$@"Unexpected geometry type: {shape.GeometryType}",
					nameof(shape));
			}
		}

		[NotNull]
		private static IEnumerable<IPath> GetLinearizedPaths(
			[NotNull] IGeometry shape,
			double densifyMaxDeviationToleranceFactor = 1)
		{
			Assert.ArgumentNotNull(shape, nameof(shape));
			Assert.ArgumentCondition(densifyMaxDeviationToleranceFactor > 0,
			                         "tolerance factor must be > 0");

			if (! GeometryUtils.HasNonLinearSegments(shape))
			{
				return GeometryUtils.GetPaths(shape);
			}

			throw new NotImplementedException("Non-linear segment not yet supported");
			//var tolerance = GeometryUtils.GetXyTolerance(shape);
			//var maxDeviation = tolerance * densifyMaxDeviationToleranceFactor;

			//var densified = (IPolycurve) GeometryFactory.Clone(shape);

			//// NOTE: IPolycurve3D.Densify3D throws exception for non-linear segments, while the normal Densify() assigns z=NaN
			//densified.Densify(0, maxDeviation);

			//// NOTE *all* Z values are NaN after densify
			//// -> transfer Z values for input vertices, then interpolate Z

			//var result = GeometryUtils.ApplyTargetZs(densified, new IGeometry[] {shape},
			//                            MultiTargetSubMode.Highest);

			//Console.WriteLine(GeometryUtils.ToString(result));

			//return GeometryUtils.GetPaths(result);
		}

		[NotNull]
		private static Linestring GetLineString(
			[NotNull] IEnumerable<SegmentProxy> segments)
		{
			Pnt3D previousPoint = null;
			var points = new List<Pnt3D>();

			foreach (var segment in segments)
			{
				var start = (Pnt3D) segment.GetStart(true);
				var end = (Pnt3D) segment.GetEnd(true);

				if (! Equals(previousPoint, start))
				{
					points.Add(start);
				}

				points.Add(end);
				previousPoint = end;
			}

			return new Linestring(points);
		}

		private int CheckIntersection([NotNull] IPoint intersectionPoint,
		                              double feature2Z,
		                              double? dZ,
		                              [NotNull] IFeature feature1, int tableIndex1,
		                              [NotNull] IFeature feature2, int tableIndex2)
		{
			double feature1Z = intersectionPoint.Z;

			if (double.IsNaN(feature1Z))
			{
				return IgnoreUndefinedZValues
					       ? NoError
					       : ErrorReporting.Report("Z is NaN", intersectionPoint,
					                               Codes[Code.UndefinedZ],
					                               TestUtils.GetShapeFieldName(feature1),
					                               feature1, feature2);
			}

			//double feature2Z = GeometryUtils.GetZValueFromGeometry(
			//	feature2.Shape, intersectionPoint,
			//	GeometryUtils.GetXyTolerance(feature2));

			if (double.IsNaN(feature2Z))
			{
				return NoError;
			}

			double dz = dZ ?? Math.Abs(feature1Z - feature2Z);

			double minimumZDifference;
			double maximumZDifference;
			GetValidZDifferenceInterval(feature1, tableIndex1,
			                            feature2, tableIndex2,
			                            feature1Z, feature2Z,
			                            out minimumZDifference, out maximumZDifference);

			int errorCount = 0;

			if (minimumZDifference > 0 && dz < minimumZDifference)
			{
				// a z difference smaller than the minimum is always an error
				errorCount += ErrorReporting.Report(
					string.Format("The Z distance is too small ({0})",
					              FormatComparison(dz, minimumZDifference, "<")),
					intersectionPoint,
					Codes[Code.TooSmall],
					TestUtils.GetShapeFieldName(feature1),
					new object[] {dz},
					feature1, feature2);
			}

			if (maximumZDifference > 0 && dz > maximumZDifference)
			{
				// a z difference larger than the maximum is always an error
				errorCount += ErrorReporting.Report(
					string.Format("The Z distance is too large ({0})",
					              FormatComparison(dz, maximumZDifference, ">")),
					intersectionPoint,
					Codes[Code.TooLarge],
					TestUtils.GetShapeFieldName(feature1),
					new object[] {dz},
					feature1, feature2);
			}

			return errorCount +
			       (feature1Z >= feature2Z
				        ? CheckConstraint(feature1, tableIndex1,
				                          feature2, tableIndex2,
				                          intersectionPoint, dz)
				        : CheckConstraint(feature2, tableIndex2,
				                          feature1, tableIndex1,
				                          intersectionPoint, dz));
		}

		private void GetValidZDifferenceInterval(IFeature feature1, int tableIndex1,
		                                         IFeature feature2, int tableIndex2,
		                                         double feature1Z, double feature2Z,
		                                         out double minimumZDifference,
		                                         out double maximumZDifference)
		{
			minimumZDifference = feature1Z >= feature2Z
				                     ? GetMinimumZDifference(feature1, tableIndex1,
				                                             feature2, tableIndex2)
				                     : GetMinimumZDifference(feature2, tableIndex2,
				                                             feature1, tableIndex1);
			maximumZDifference = feature1Z >= feature2Z
				                     ? GetMaximumZDifference(feature1, tableIndex1,
				                                             feature2, tableIndex2)
				                     : GetMaximumZDifference(feature2, tableIndex2,
				                                             feature1, tableIndex1);
		}

		private int CheckConstraint([NotNull] IFeature upperFeature, int upperTableIndex,
		                            [NotNull] IFeature lowerFeature, int lowerTableIndex,
		                            [NotNull] IPoint intersectionPoint,
		                            double zDifference)
		{
			string conditionMessage;
			bool fulFilled = IsZRelationConditionFulfilled(
				upperFeature, upperTableIndex,
				lowerFeature, lowerTableIndex,
				zDifference,
				out conditionMessage);

			if (fulFilled)
			{
				return NoError;
			}

			string message = $"Z distance = {zDifference:N2}; {conditionMessage}";

			return ErrorReporting.Report(
				message, intersectionPoint,
				Codes[Code.ConstraintNotFulfilled],
				TestUtils.GetShapeFieldNames(upperFeature, lowerFeature),
				upperFeature, lowerFeature);
		}

		public interface IIntersectionPoint
		{
			[NotNull]
			IPoint Point { get; }

			double OtherZ { get; }

			double? Distance { get; }
		}

		private class Pnt3DIntersectionPoint : IIntersectionPoint
		{
			public Pnt3DIntersectionPoint([NotNull] Pnt3D point,
			                              [NotNull] ISpatialReference spatialReference,
			                              double otherZ,
			                              double? zDifference = null)
			{
				Point = GeometryFactory.CreatePoint(point.X, point.Y, point.Z, double.NaN,
				                                    spatialReference);
				Point.SnapToSpatialReference();
				OtherZ = otherZ;
				Distance = zDifference;
			}

			public IPoint Point { get; }

			public double OtherZ { get; }

			public double? Distance { get; }
		}

		private class AoIntersectionPoint : IIntersectionPoint
		{
			public AoIntersectionPoint(IPoint point,
			                           double otherZ,
			                           double? distance = null)
			{
				Point = point;
				OtherZ = otherZ;
				Distance = distance;
			}

			public IPoint Point { get; }

			public double OtherZ { get; }

			public double? Distance { get; }
		}

		public class NonPlanarError
		{
			[NotNull]
			public string Message { get; }

			public double MaximumOffset { get; }

			[NotNull]
			public SegmentsPlane SegmentsPlane { get; }

			public NonPlanarError(
				[NotNull] string message,
				double maximumOffset,
				[NotNull] SegmentsPlane segmentsPlane)
			{
				Message = message;
				MaximumOffset = maximumOffset;
				SegmentsPlane = segmentsPlane;
			}
		}
	}
}
