using System;
using System.Collections;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	[ZValuesTest]
	public class QaWithinZRange : ContainerTest
	{
		private readonly double _minimumZValue;
		private readonly double _maximumZValue;
		private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();
		private readonly bool _hasZ;
		private readonly bool _hasPointGeometry;
		private readonly string _shapeFieldName;
		[CanBeNull] private readonly List<double> _allowedZValues;
		[CanBeNull] private readonly ZValueComparer _zValueComparer;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ZOutsideRange_BelowMinimum = "ZOutsideRange.BelowMinimum";
			public const string ZOutsideRange_AboveMaximum = "ZOutsideRange.AboveMaximum";

			public Code() : base("WithinZRange") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaWithinZRange_0))]
		public QaWithinZRange(
				[Doc(nameof(DocStrings.QaWithinZRange_featureClass))] [NotNull]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaWithinZRange_minimumZValue))]
				double minimumZValue,
				[Doc(nameof(DocStrings.QaWithinZRange_maximumZValue))]
				double maximumZValue)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, minimumZValue, maximumZValue, null) { }

		[Doc(nameof(DocStrings.QaWithinZRange_1))]
		public QaWithinZRange(
			[Doc(nameof(DocStrings.QaWithinZRange_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaWithinZRange_minimumZValue))]
			double minimumZValue,
			[Doc(nameof(DocStrings.QaWithinZRange_maximumZValue))]
			double maximumZValue,
			[Doc(nameof(DocStrings.QaWithinZRange_allowedZValues))] [CanBeNull]
			IEnumerable<double>
				allowedZValues)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentCondition(maximumZValue >= minimumZValue,
			                         "Maximum z value must be equal or larger than minimum z value");

			_hasZ = DatasetUtils.GetGeometryDef(featureClass).HasZ;
			_shapeFieldName = featureClass.ShapeFieldName;

			_minimumZValue = minimumZValue;
			_maximumZValue = maximumZValue;

			if (allowedZValues != null)
			{
				double zTolerance;
				if (DatasetUtils.TryGetZTolerance(featureClass.SpatialReference, out zTolerance))
				{
					_zValueComparer = new ZValueComparer(zTolerance);
				}

				_allowedZValues = new List<double>(allowedZValues);
				_allowedZValues.Sort();
			}

			_hasPointGeometry =
				featureClass.ShapeType == esriGeometryType.esriGeometryPoint;
		}

		[InternallyUsedTest]
		public QaWithinZRange([NotNull] QaWithinZRangeDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass, definition.MinimumZValue,
			       definition.MaximumZValue, definition.AllowedZValues)
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
			Assert.ArgumentNotNull(row, nameof(row));

			if (! _hasZ)
			{
				return NoError;
			}

			var feature = (IReadOnlyFeature) row;

			IGeometry shape = feature.Shape;
			if (shape == null || shape.IsEmpty)
			{
				return NoError;
			}

			return CheckZRange(feature, shape);
		}

		private bool IsWithinZRange([NotNull] IGeometry geometry)
		{
			if (_hasPointGeometry)
			{
				double z = ((IPoint) geometry).Z;
				return z >= _minimumZValue && z <= _maximumZValue || IsAllowed(z);
			}

			geometry.QueryEnvelope(_envelopeTemplate);

			double zMin = _envelopeTemplate.ZMin;
			double zMax = _envelopeTemplate.ZMax;

			return zMin >= _minimumZValue && zMax <= _maximumZValue;
		}

		private bool IsAllowed(double z)
		{
			return _allowedZValues != null &&
			       _allowedZValues.BinarySearch(z, _zValueComparer) >= 0;
		}

		private int CheckZRange([NotNull] IReadOnlyFeature feature, [NotNull] IGeometry shape)
		{
			if (IsWithinZRange(shape))
			{
				return NoError;
			}

			switch (shape.GeometryType)
			{
				case esriGeometryType.esriGeometryPoint:
					return ReportError(feature, (IPoint) shape);

				case esriGeometryType.esriGeometryMultipoint:
					return CheckZRange(feature, (IMultipoint) shape);

				case esriGeometryType.esriGeometryPolyline:
					return CheckZRange(feature, (IPolyline) shape);

				case esriGeometryType.esriGeometryPolygon:
					return CheckZRange(feature, (IPolygon) shape);

				case esriGeometryType.esriGeometryMultiPatch:
					return CheckZRange(feature, (IMultiPatch) shape);

				case esriGeometryType.esriGeometryNull:
				case esriGeometryType.esriGeometryLine:
				case esriGeometryType.esriGeometryCircularArc:
				case esriGeometryType.esriGeometryEllipticArc:
				case esriGeometryType.esriGeometryBezier3Curve:
				case esriGeometryType.esriGeometryPath:
				case esriGeometryType.esriGeometryRing:
				case esriGeometryType.esriGeometryEnvelope:
				case esriGeometryType.esriGeometryAny:
				case esriGeometryType.esriGeometryBag:
				case esriGeometryType.esriGeometryTriangleStrip:
				case esriGeometryType.esriGeometryTriangleFan:
				case esriGeometryType.esriGeometryRay:
				case esriGeometryType.esriGeometrySphere:
				case esriGeometryType.esriGeometryTriangles:
					throw new ArgumentOutOfRangeException(
						string.Format(
							"Unsupported geometry type: {0}; a high level geometry type expected",
							shape.GeometryType));

				default:
					throw new ArgumentOutOfRangeException(
						string.Format("Unknown geometry type: {0}", shape.GeometryType));
			}
		}

		private int CheckZRange([NotNull] IReadOnlyFeature feature,
		                        [NotNull] IMultipoint multipoint)
		{
			return CheckZRange(feature, (IPointCollection) multipoint);
		}

		private int CheckZRange([NotNull] IReadOnlyFeature feature,
		                        [NotNull] IMultiPatch multiPatch)
		{
			return CheckZRange(feature, (IPointCollection) multiPatch);
		}

		private int CheckZRange([NotNull] IReadOnlyFeature feature,
		                        [NotNull] IPointCollection points)
		{
			IEnumVertex enumPoints = points.EnumVertices;
			enumPoints.Reset();

			var errorPointsBelow = new List<IPoint>();
			var errorPointsAbove = new List<IPoint>();

			IPoint currentPoint;
			enumPoints.Next(out currentPoint, out int _, out int _);

			double zMax = double.MinValue;
			double zMin = double.MaxValue;

			while (currentPoint != null)
			{
				double z = currentPoint.Z;

				if (! IsAllowed(z))
				{
					if (z < _minimumZValue)
					{
						errorPointsBelow.Add(GeometryFactory.Clone(currentPoint));
					}
					else if (z > _maximumZValue)
					{
						errorPointsAbove.Add(GeometryFactory.Clone(currentPoint));
					}

					if (z < zMin)
					{
						zMin = z;
					}
					else if (z > zMax)
					{
						zMax = z;
					}
				}

				enumPoints.Next(out currentPoint, out int _, out int _);
			}

			var errorCount = 0;

			if (errorPointsBelow.Count > 0)
			{
				errorCount += ReportError(
					GetErrorMessageBelow(errorPointsBelow, zMin),
					InvolvedRowUtils.GetInvolvedRows(feature),
					GetErrorGeometry(errorPointsBelow),
					null, _shapeFieldName);
			}

			if (errorPointsAbove.Count > 0)
			{
				string message = GetErrorMessageAbove(errorPointsAbove, zMax);

				errorCount += ReportError(
					message, InvolvedRowUtils.GetInvolvedRows(feature),
					GetErrorGeometry(errorPointsAbove),
					null, _shapeFieldName);
			}

			return errorCount;
		}

		private int CheckZRange([NotNull] IReadOnlyFeature feature, [NotNull] IPolygon polygon)
		{
			var errorCount = 0;

			// enumerate segments
			// for each continuous sequence of segments that cross one of the extreme values
			foreach (IRing ring in GeometryUtils.GetRings(polygon))
			{
				if (IsWithinZRange(ring))
				{
					continue;
				}

				// TODO allowed Z values
				foreach (ZRangeErrorSegments errorSegments in
				         ZRangeUtils.GetErrorSegments(
					         ring, _minimumZValue, _maximumZValue, IsAllowed))
				{
					errorCount += ReportError(feature, errorSegments);
				}
			}

			return errorCount;
		}

		private int CheckZRange([NotNull] IReadOnlyFeature feature,
		                        [NotNull] IPolyline polyline)
		{
			var errorCount = 0;

			// enumerate segments
			// for each continuous sequence of segments that cross one of the extreme values
			foreach (IPath path in GeometryUtils.GetPaths(polyline))
			{
				if (IsWithinZRange(path))
				{
					continue;
				}

				// TODO allowed Z values
				foreach (ZRangeErrorSegments errorSegments in
				         ZRangeUtils.GetErrorSegments(
					         path, _minimumZValue, _maximumZValue, IsAllowed))
				{
					errorCount += ReportError(feature, errorSegments);
				}
			}

			return errorCount;
		}

		private int ReportError([NotNull] IReadOnlyFeature feature, [NotNull] IPoint point)
		{
			double z = point.Z;

			string format;
			string code;
			if (z > _maximumZValue)
			{
				// TODO indicate "above" once allowed error matching does not rely on description
				format = "Point has Z value outside of valid range: {0:N2}";
				code = Code.ZOutsideRange_AboveMaximum;
			}
			else if (z < _minimumZValue)
			{
				// TODO indicate "below" once allowed error matching does not rely on description
				format = "Point has Z value outside of valid range: {0:N2}";
				code = Code.ZOutsideRange_BelowMinimum;
			}
			else
			{
				return NoError;
			}

			string message = string.Format(format, z);

			return ReportError(
				message, InvolvedRowUtils.GetInvolvedRows(feature), point,
				Codes[code], _shapeFieldName);
		}

		private int ReportError([NotNull] IReadOnlyFeature feature,
		                        [NotNull] ZRangeErrorSegments errorSegments)
		{
			IGeometry errorGeometry = GetErrorGeometry(errorSegments);

			IssueCode issueCode;
			string message = GetErrorMessage(errorSegments, out issueCode);

			return ReportError(
				message, InvolvedRowUtils.GetInvolvedRows(feature), errorGeometry,
				issueCode, _shapeFieldName);
		}

		[NotNull]
		private static IGeometry GetErrorGeometry([NotNull] IEnumerable<IPoint> points)
		{
			return GeometryFactory.CreateMultipoint(points);
		}

		[CanBeNull]
		private static IGeometry GetErrorGeometry(
			[NotNull] ZRangeErrorSegments errorSegments)
		{
			IPolyline polyline = errorSegments.CreatePolyline();

			GeometryUtils.SimplifyNetworkGeometry(polyline);

			return polyline.IsEmpty
				       ? (IGeometry) errorSegments.CreateStartPoint()
				       : polyline;
		}

		[NotNull]
		private string GetErrorMessage([NotNull] ZRangeErrorSegments errorSegments,
		                               [NotNull] out IssueCode issueCode)
		{
			string comparison;
			switch (errorSegments.ZRangeRelation)
			{
				case ZRangeRelation.AboveZMax:
					comparison =
						FormatComparison(errorSegments.MaxZ, ">", _maximumZValue, "N2");
					issueCode = Assert.NotNull(Codes[Code.ZOutsideRange_AboveMaximum]);

					return errorSegments.SegmentCount == 1
						       ? string.Format(
							       "One segment above valid Z range. Highest Z value: {0}",
							       comparison)
						       : string.Format(
							       "{0} segments above valid Z range. Highest Z value: {1}",
							       errorSegments.SegmentCount, comparison);

				case ZRangeRelation.BelowZMin:
					comparison =
						FormatComparison(errorSegments.MinZ, "<", _minimumZValue, "N2");
					issueCode = Assert.NotNull(Codes[Code.ZOutsideRange_BelowMinimum]);

					return errorSegments.SegmentCount == 1
						       ? string.Format(
							       "One segment below valid Z range. Lowest Z value: {0}",
							       comparison)
						       : string.Format(
							       "{0} segments below valid Z range. Lowest Z value: {1}",
							       errorSegments.SegmentCount, comparison);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[NotNull]
		private string GetErrorMessageAbove([NotNull] ICollection errorPointsAbove,
		                                    double zMax)
		{
			string comparison = FormatComparison(zMax, ">", _maximumZValue, "N2");

			return errorPointsAbove.Count == 1
				       ? string.Format(
					       "One point above valid Z range. Z value: {0}",
					       comparison)
				       : string.Format(
					       "{0} points above valid Z range. Highest Z value: {1}",
					       errorPointsAbove.Count, comparison);
		}

		[NotNull]
		private string GetErrorMessageBelow([NotNull] ICollection errorPointsBelow,
		                                    double zMin)
		{
			string comparison = FormatComparison(zMin, "<", _minimumZValue, "N2");

			return errorPointsBelow.Count == 1
				       ? string.Format(
					       "One point below valid Z range. Z value: {0}",
					       comparison)
				       : string.Format(
					       "{0} points below valid Z range. Lowest Z value: {1}",
					       errorPointsBelow.Count, comparison);
		}

		private class ZValueComparer : IComparer<double>
		{
			private readonly double _zTolerance;

			public ZValueComparer(double zTolerance)
			{
				_zTolerance = zTolerance;
			}

			public int Compare(double x, double y)
			{
				if (Math.Abs(x - y) <= _zTolerance)
				{
					return 0;
				}

				return x > y ? 1 : -1;
			}
		}
	}
}
