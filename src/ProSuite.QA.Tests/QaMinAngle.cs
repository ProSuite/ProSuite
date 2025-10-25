using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[IntersectionParameterTest]
	public class QaMinAngle : ContainerTest
	{
		private const AngleUnit _defaultAngularUnit = DefaultAngleUnit;

		private IList<IFeatureClassFilter> _filter;
		private IList<QueryFilterHelper> _helper;
		private bool _is3D;
		private double _limitCstr;

		private double _limitRad;
		private double _limitCos2_;

		// point templates to avoid creating Point Instances
		private IPoint _firstSegmentStartPoint;
		private IPoint _firstSegmentEndPoint;
		private IPoint _lastSegmentStartPoint;
		private IPoint _lastSegmentEndPoint;
		private IPoint _comparePointTemplate;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string AngleTooSmall = "AngleTooSmall";
			public const string EndPointsDoNotFit = "EndPointsDoNotFit";

			public Code() : base("MinAngle") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMinAngle_0))]
		public QaMinAngle(
			[Doc(nameof(DocStrings.QaMinAngle_polylineClass))]
			IReadOnlyFeatureClass polylineClass,
			[Doc(nameof(DocStrings.QaMinAngle_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinAngle_is3D))]
			bool is3D)
			: base(polylineClass)
		{
			Init(limit, is3D);
		}

		[Doc(nameof(DocStrings.QaMinAngle_1))]
		public QaMinAngle(
				[Doc(nameof(DocStrings.QaMinAngle_polylineClasses))]
				IList<IReadOnlyFeatureClass> polylineClasses,
				[Doc(nameof(DocStrings.QaMinAngle_limit))]
				double limit,
				[Doc(nameof(DocStrings.QaMinAngle_is3D))]
				bool is3D)
			// ReSharper disable once PossiblyMistakenUseOfParamsMethod
			: base(CastToTables(polylineClasses))
		{
			Init(limit, is3D);
		}

		[Doc(nameof(DocStrings.QaMinAngle_1))]
		public QaMinAngle(
				[Doc(nameof(DocStrings.QaMinAngle_polylineClasses))]
				IList<IReadOnlyFeatureClass> polylineClasses,
				[Doc(nameof(DocStrings.QaMinAngle_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polylineClasses, limit, false) { }

		[InternallyUsedTest]
		public QaMinAngle(QaMinAngleDefinition definition)
			: this(definition.PolylineClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.Limit,
			       definition.Is3D
			)
		{
			AngularUnit = definition.AngularUnit;
		}

		[TestParameter(_defaultAngularUnit)]
		[Doc(nameof(DocStrings.QaMinAngle_AngularUnit))]
		public AngleUnit AngularUnit
		{
			get { return AngleUnit; }
			set { AngleUnit = value; }
		}

		private void Init(double limit, bool is3D)
		{
			_filter = null;
			_limitCstr = limit;
			_is3D = is3D;

			_limitCos2_ = -1;
		}

		private void InitLimit()
		{
			_limitRad = FormatUtils.AngleInUnits2Radians(_limitCstr, AngularUnit);
			double cos = Math.Cos(_limitRad);
			if (cos < 0)
			{
				throw new ArgumentException("Angle must be smaller than PI / 2");
			}

			_limitCos2_ = cos * cos;

			_firstSegmentStartPoint = new PointClass();
			_firstSegmentEndPoint = new PointClass();
			_lastSegmentStartPoint = new PointClass();
			_lastSegmentEndPoint = new PointClass();

			_comparePointTemplate = new PointClass();
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			// preparing
			int errorCount = 0;
			if (_filter == null)
			{
				InitFilter();
			}

			if (_limitCos2_ < 0)
			{
				InitLimit();
			}

			// iterating over all needed tables
			var feature = (IReadOnlyFeature) row;

			var polyline = (IPolyline) feature.Shape;

			foreach (IPath path in GeometryUtils.GetPaths(polyline))
			{
				errorCount += CheckPathEndPoints(feature, path);
			}

			return errorCount;
		}

		private int CheckPathEndPoints([NotNull] IReadOnlyFeature feature, [NotNull] IPath path)
		{
			var vertices = (IPointCollection) path;

			// TODO should use segment-based angle calculation (only for non-linear segments)

			int pointCount = vertices.PointCount;

			vertices.QueryPoint(0, _firstSegmentStartPoint);
			vertices.QueryPoint(1, _firstSegmentEndPoint);

			double firstSegmentSquaredLength =
				GeometryMathUtils.GetDistanceSquared(_firstSegmentEndPoint,
				                                     _firstSegmentStartPoint, _is3D);

			vertices.QueryPoint(pointCount - 2, _lastSegmentStartPoint);
			vertices.QueryPoint(pointCount - 1, _lastSegmentEndPoint);

			double lastSegmentSquaredLength =
				GeometryMathUtils.GetDistanceSquared(_lastSegmentStartPoint, _lastSegmentEndPoint,
				                                     _is3D);

			int errorCount = 0;
			int compareTableIndex = -1;
			bool skip = IgnoreUndirected;

			foreach (var table in InvolvedTables)
			{
				var compareFeatureClass = (IReadOnlyFeatureClass) table;
				compareTableIndex++;
				_helper[compareTableIndex].MinimumOID = -1;

				if (feature.Table == compareFeatureClass)
				{
					skip = false;
					if (IgnoreUndirected)
					{
						_helper[compareTableIndex].MinimumOID = feature.OID;
					}
				}

				if (skip)
				{
					continue;
				}

				errorCount += ExecutePoint(_firstSegmentStartPoint, _firstSegmentEndPoint,
				                           firstSegmentSquaredLength,
				                           compareFeatureClass, compareTableIndex, feature);

				errorCount += ExecutePoint(_lastSegmentEndPoint, _lastSegmentStartPoint,
				                           lastSegmentSquaredLength,
				                           compareFeatureClass, compareTableIndex, feature);
			}

			return errorCount;
		}

		private int ExecutePoint([NotNull] IPoint connectPoint,
		                         [NotNull] IPoint otherSegmentEndPoint,
		                         double squaredSegmentLength,
		                         [NotNull] IReadOnlyFeatureClass compareFeatureClass,
		                         int compareTableIndex,
		                         [NotNull] IReadOnlyFeature feature)
		{
			IFeatureClassFilter filter = _filter[compareTableIndex];

			filter.FilterGeometry = connectPoint; // search at connect point of segment

			int errorCount = 0;

			foreach (var row in Search(compareFeatureClass,
			                           _filter[compareTableIndex],
			                           _helper[compareTableIndex]))
			{
				var compareFeature = (IReadOnlyFeature) row;
				errorCount += CheckFeature(connectPoint, feature, compareFeature,
				                           otherSegmentEndPoint, squaredSegmentLength);
			}

			return errorCount;
		}

		private int CheckFeature([NotNull] IPoint connectPoint,
		                         [NotNull] IReadOnlyFeature feature,
		                         [NotNull] IReadOnlyFeature compareFeature,
		                         [NotNull] IPoint otherSegmentEndPoint,
		                         double squaredSegmentLength)
		{
			bool sameFeature = false;
			if (feature == compareFeature)
			{
				// same feature. Angle must be checked only if From == To
				sameFeature = true;

				if (connectPoint == _firstSegmentStartPoint)
				{
					// skip this feature
					return 0;
				}
			}

			var comparePolyline = (IPolyline) compareFeature.Shape;

			bool compareConnectionFound = false;

			int errorCount = 0;

			foreach (IPath comparePath in GeometryUtils.GetPaths(comparePolyline))
			{
				var compareVertices = (IPointCollection) comparePath;

				IPoint compareStartPoint = _comparePointTemplate;
				compareVertices.QueryPoint(0, compareStartPoint);

				IPoint comparePoint;
				if (GeometryUtils.AreEqualInXY(connectPoint, compareStartPoint))
				{
					// the connect point connects to the start point of the compare path
					// --> get the end point of the first compare path segment as compare point
					comparePoint = _comparePointTemplate;
					compareVertices.QueryPoint(1, comparePoint);
				}
				else if (! sameFeature)
				{
					IPoint compareEndPoint = _comparePointTemplate;

					comparePath.QueryToPoint(compareEndPoint);

					if (GeometryUtils.AreEqualInXY(connectPoint, compareEndPoint))
					{
						// the connect point connects to the end point of the compare path
						// --> get the start point of the last compare path segment as compare point
						comparePoint = _comparePointTemplate;
						compareVertices.QueryPoint(compareVertices.PointCount - 2, comparePoint);
					}
					else
					{
						// the connect point matches neither the start nor the end point of the
						// compare path. Get next compare path
						continue;
					}
				}
				else
				{
					// same feature, get next compare path
					compareConnectionFound = true;
					continue;
				}

				compareConnectionFound = true;
				errorCount += CheckAngle(connectPoint, otherSegmentEndPoint,
				                         squaredSegmentLength,
				                         comparePoint, feature, compareFeature);

				// to avoid irreversible VM increase
				Marshal.ReleaseComObject(comparePath);
			}

			if (! compareConnectionFound)
			{
				// this is possible because of tolerance and resolution
				errorCount += ReportError(
					"End points do not fit",
					InvolvedRowUtils.GetInvolvedRows(feature, compareFeature),
					connectPoint, Codes[Code.EndPointsDoNotFit],
					TestUtils.GetShapeFieldName(feature));
			}

			return errorCount;
		}

		private void InitFilter()
		{
			CopyFilters(out _filter, out _helper);
			foreach (var filter in _filter)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelTouches;
			}
		}

		private int CheckAngle([NotNull] IPoint connectPoint,
		                       [NotNull] IPoint otherSegmentEndPoint,
		                       double squaredSegmentLength,
		                       [NotNull] IPoint comparePoint,
		                       [NotNull] IReadOnlyRow feature,
		                       [NotNull] IReadOnlyRow compareFeature)
		{
			double distanceSquaredToComparePoint;
			double prod = GetProd(connectPoint, otherSegmentEndPoint, comparePoint,
			                      out distanceSquaredToComparePoint);

			if (prod > 0)
			{
				double cos2 = prod * prod / (squaredSegmentLength * distanceSquaredToComparePoint);

				if (cos2 > _limitCos2_)
				{
					double angleRadians = Math.Acos(Math.Sqrt(cos2));

					string description = string.Format("Angle {0} < {1}",
					                                   FormatAngle(angleRadians, "N2"),
					                                   FormatAngle(_limitRad, "N2"));

					return ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(feature, compareFeature),
						GeometryFactory.Clone(connectPoint),
						Codes[Code.AngleTooSmall], TestUtils.GetShapeFieldName(feature),
						values: new object[] { MathUtils.ToDegrees(angleRadians) });
				}
			}

			return NoError;
		}

		private double GetProd([NotNull] IPoint connectPoint,
		                       [NotNull] IPoint otherSegmentEndPoint,
		                       [NotNull] IPoint comparePoint,
		                       out double distanceSquaredToComparePoint)
		{
			double connectX;
			double connectY;
			connectPoint.QueryCoords(out connectX, out connectY);

			double otherSegmentEndX;
			double otherSegmentEndY;
			otherSegmentEndPoint.QueryCoords(out otherSegmentEndX, out otherSegmentEndY);

			double compareX;
			double compareY;
			comparePoint.QueryCoords(out compareX, out compareY);

			double dxCompare = compareX - connectX;
			double dxSegment = otherSegmentEndX - connectX;

			double dyCompare = compareY - connectY;
			double dySegment = otherSegmentEndY - connectY;

			distanceSquaredToComparePoint = dxCompare * dxCompare + dyCompare * dyCompare;
			double prod = dxCompare * dxSegment + dyCompare * dySegment;

			if (_is3D)
			{
				double dzCompare = comparePoint.Z - connectPoint.Z;
				double dzSegment = otherSegmentEndPoint.Z - connectPoint.Z;

				distanceSquaredToComparePoint += dzCompare * dzCompare;
				prod += dzCompare * dzSegment;
			}

			return prod;
		}
	}
}
