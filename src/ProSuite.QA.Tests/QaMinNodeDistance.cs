using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	[LinearNetworkTest]
	[ZValuesTest]
	public class QaMinNodeDistance : ContainerTest
	{
		private IEnvelope _box;
		private IList<IFeatureClassFilter> _filter;
		private IList<QueryFilterHelper> _helper;
		private readonly double _tolerance;
		private readonly bool _is3D;
		private readonly double _maxZDifference;
		private readonly string _validRelationConstraintSql;

		private readonly double _searchDistanceSquared;
		private readonly double _toleranceSquared;

		private ValidRelationConstraint _validRelationConstraint;

		private readonly IPoint _sourceP0Template = new PointClass();
		private readonly IPoint _sourceP1Template = new PointClass();
		private readonly IPoint _targetP0Template = new PointClass();
		private readonly IPoint _targetP1Template = new PointClass();
		private const int _noMaxZDifference = -1;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NodeDistanceTooSmall = "NodeDistanceTooSmall";

			public const string NodeDistanceTooSmall_ConstraintNotFulfilled =
				"NodeDistanceTooSmall.ConstraintNotFulfilled";

			public const string ZDifferenceTooLarge = "ZDifferenceTooLarge";

			public const string ZDifferenceTooLarge_ConstraintNotFulfilled =
				"ZDifferenceTooLarge.ConstraintNotFulfilled";

			public Code() : base("MinNodeDistance") { }
		}

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.QaMinNodeDistance_0))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D)
			: this(featureClass, near, GeometryUtils.GetXyTolerance(featureClass.SpatialReference),
			       is3D, _noMaxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_1))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D)
			: this(featureClasses, near,
			       GeometryUtils.GetXyTolerance(featureClasses[0].SpatialReference), is3D) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_2))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D)
			: this(featureClass, near, tolerance, is3D, _noMaxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_3))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D)
			: this(featureClasses, near, tolerance, is3D, _noMaxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_4))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
			double maxZDifference)
			: this(featureClass, near, GeometryUtils.GetXyTolerance(featureClass.SpatialReference),
			       false,
			       maxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_5))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
			double maxZDifference)
			: this(featureClasses, near,
			       GeometryUtils.GetXyTolerance(featureClasses[0].SpatialReference),
			       maxZDifference) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_6))]
		public QaMinNodeDistance(
				[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
				double near,
				[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
				double tolerance,
				[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
				double maxZDifference)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, tolerance, maxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_7))]
		public QaMinNodeDistance(
				[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
				IList<IReadOnlyFeatureClass> featureClasses,
				[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
				double near,
				[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
				double tolerance,
				[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
				double maxZDifference)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, near, tolerance, maxZDifference, null) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_1))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near)
			: this(featureClasses, near,
			       GeometryUtils.GetXyTolerance(featureClasses[0].SpatialReference), false) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_9))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
			double maxZDifference,
			[Doc(nameof(DocStrings.QaMinNodeDistance_validRelationConstraint))] [CanBeNull]
			string
				validRelationConstraint)
			: this(featureClass, near, tolerance, false,
			       maxZDifference, validRelationConstraint) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_10))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_maxZDifference))]
			double maxZDifference,
			[Doc(nameof(DocStrings.QaMinNodeDistance_validRelationConstraint))] [CanBeNull]
			string
				validRelationConstraint)
			: this(featureClasses, near, tolerance, false,
			       maxZDifference, validRelationConstraint) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_11))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaMinNodeDistance_validRelationConstraint))] [CanBeNull]
			string
				validRelationConstraint)
			: this(
				featureClass, near, tolerance, is3D, _noMaxZDifference, validRelationConstraint) { }

		[Doc(nameof(DocStrings.QaMinNodeDistance_12))]
		public QaMinNodeDistance(
			[Doc(nameof(DocStrings.QaMinNodeDistance_featureClasses))]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaMinNodeDistance_near))]
			double near,
			[Doc(nameof(DocStrings.QaMinNodeDistance_tolerance))]
			double tolerance,
			[Doc(nameof(DocStrings.QaMinNodeDistance_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaMinNodeDistance_validRelationConstraint))] [CanBeNull]
			string
				validRelationConstraint)
			: this(
				featureClasses, near, tolerance, is3D, _noMaxZDifference,
				validRelationConstraint) { }

		[InternallyUsedTest]
		public QaMinNodeDistance(QaMinNodeDistanceDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>()
			                 .ToList(),
			       definition.Near,
			       GetXyTolerance(definition),
			       definition.Is3D,
			       definition.MaxZDifference,
			       definition.ValidRelationConstraint) { }

		private static double GetXyTolerance(QaMinNodeDistanceDefinition definition)
		{
			if (! double.IsNaN(definition.Tolerance))
			{
				return definition.Tolerance;
			}

			// The tolerance was not provided, take it from the feature class.
			IFeatureClassSchemaDef featureClassSchemaDef = definition.FeatureClasses[0];
			IReadOnlyFeatureClass featureClass = (IReadOnlyFeatureClass) featureClassSchemaDef;

			return GeometryUtils.GetXyTolerance(featureClass.SpatialReference);
		}

		private QaMinNodeDistance(
			[NotNull] IReadOnlyFeatureClass featureClass,
			double near, double tolerance, bool is3D, double maxZDifference,
			[CanBeNull] string validRelationConstraint)
			: this(new[] { featureClass }, near, tolerance, is3D, maxZDifference,
			       validRelationConstraint) { }

		private QaMinNodeDistance(
			[NotNull] IList<IReadOnlyFeatureClass> featureClasses,
			double near, double tolerance, bool is3D, double maxZDifference,
			[CanBeNull] string validRelationConstraint)
			: base(CastToTables((IEnumerable<IReadOnlyFeatureClass>) featureClasses))
		{
			SearchDistance = near;

			_searchDistanceSquared = near * near;
			_toleranceSquared = tolerance < 0
				                    ? 0
				                    : tolerance * tolerance;
			_tolerance = tolerance;
			_is3D = is3D;
			_maxZDifference = maxZDifference;

			_validRelationConstraintSql = StringUtils.IsNotEmpty(validRelationConstraint)
				                              ? validRelationConstraint
				                              : null;
			AddCustomQueryFilterExpression(validRelationConstraint);

			_filter = null;
		}

		#endregion

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			// preparing
			int errorCount = 0;
			if (_filter == null)
			{
				InitFilter();
			}

			if (_validRelationConstraint == null)
			{
				const bool constraintIsDirected = false;
				_validRelationConstraint =
					new ValidRelationConstraint(_validRelationConstraintSql,
					                            constraintIsDirected,
					                            GetSqlCaseSensitivity());
			}

			IPoint p0;
			IPoint p1;
			QueryPoints(((IReadOnlyFeature) row).Shape,
			            _sourceP0Template, _sourceP1Template,
			            out p0, out p1);

			// iterating over all needed tables
			int involvedTableIndex = -1;
			bool bSkip = IgnoreUndirected;

			foreach (IReadOnlyTable table in InvolvedTables)
			{
				var fcNeighbor = (IReadOnlyFeatureClass) table;

				involvedTableIndex++;
				_helper[involvedTableIndex].MinimumOID = -1;
				if (row.Table == fcNeighbor)
				{
					bSkip = false;
					if (IgnoreUndirected)
					{
						_helper[involvedTableIndex].MinimumOID = row.OID;
					}
				}

				if (bSkip)
				{
					continue;
				}

				// only test from table of input row onwards
				errorCount += ExecutePoint(p0, 0, fcNeighbor, involvedTableIndex, row, tableIndex);

				if (p1 != null)
				{
					errorCount +=
						ExecutePoint(p1, 1, fcNeighbor, involvedTableIndex, row, tableIndex);
				}
			}

			return errorCount;
		}

		private int ExecutePoint([NotNull] IPoint point, int pointIndex,
		                         [NotNull] IReadOnlyFeatureClass neighbor,
		                         int neighborTableIndex,
		                         [NotNull] IReadOnlyRow row0,
		                         int tableIndex0)
		{
			IFeatureClassFilter filter = _filter[neighborTableIndex];

			UpdateSearchEnvelope(_box, point);
			filter.FilterGeometry = _box;

			esriGeometryType neighborType = neighbor.ShapeType;

			int errorCount = 0;

			foreach (IReadOnlyRow neighborRow in Search(
				         neighbor, _filter[neighborTableIndex], _helper[neighborTableIndex]))
			{
				var neighborFeature = (IReadOnlyFeature) neighborRow;
				bool sameFeature = tableIndex0 == neighborTableIndex &&
				                   row0 == neighborFeature;

				if (sameFeature && neighborType == esriGeometryType.esriGeometryPoint)
				{
					// no self-check needed for points from same table index
					continue;
				}

				bool coincidentIsError = ! sameFeature && _tolerance < 0;

				IPoint p0;
				IPoint p1;
				QueryPoints(neighborFeature.Shape,
				            _targetP0Template, _targetP1Template,
				            out p0, out p1);

				bool validRelationConstraintFulfilled = false;

				// make sure that same endpoint on same feature (from same table index) is not compared
				if (! sameFeature || pointIndex > 0)
				{
					errorCount += CheckDistance(point, p0,
					                            row0, tableIndex0,
					                            neighborFeature, neighborTableIndex,
					                            coincidentIsError,
					                            out validRelationConstraintFulfilled);
				}

				if (p1 != null && ! validRelationConstraintFulfilled)
				{
					// make sure that same endpoint on same feature (from same table index) is not compared
					if (! sameFeature || pointIndex != 1)
					{
						errorCount += CheckDistance(point, p1,
						                            row0, tableIndex0,
						                            neighborFeature, neighborTableIndex,
						                            coincidentIsError,
						                            out validRelationConstraintFulfilled);
					}
				}
			}

			return errorCount;
		}

		private void UpdateSearchEnvelope([NotNull] IEnvelope searchEnvelope,
		                                  [NotNull] IPoint point)
		{
			double x;
			double y;
			point.QueryCoords(out x, out y);

			double d = SearchDistance;

			searchEnvelope.PutCoords(x - d, y - d, x + d, y + d);
		}

		private void InitFilter()
		{
			CopyFilters(out _filter, out _helper);
			foreach (var filter in _filter)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}

			_box = new EnvelopeClass();
		}

		private static void QueryPoints([NotNull] IGeometry pointOrPolyline,
		                                [NotNull] IPoint p0Template,
		                                [NotNull] IPoint p1Template,
		                                [NotNull] out IPoint p0,
		                                [CanBeNull] out IPoint p1)
		{
			var point = pointOrPolyline as IPoint;
			if (point != null)
			{
				p0 = point;
				p1 = null;
			}
			else
			{
				var polyline = (IPolyline) pointOrPolyline;

				polyline.QueryFromPoint(p0Template);
				p0 = p0Template;

				polyline.QueryToPoint(p1Template);
				p1 = p1Template;
			}
		}

		private int CheckDistance([NotNull] IPoint p0, [NotNull] IPoint p1,
		                          [NotNull] IReadOnlyRow row0, int tableIndex0,
		                          [NotNull] IReadOnlyRow row1, int tableIndex1,
		                          bool coincidentIsError,
		                          out bool validRelationConstraintFulfilled)
		{
			double pointDistanceSquared = GeometryMathUtils.GetDistanceSquared(p0, p1, _is3D);

			validRelationConstraintFulfilled = false;

			bool isCoincident = pointDistanceSquared <= _toleranceSquared;

			if ((coincidentIsError || ! isCoincident) &&
			    pointDistanceSquared < _searchDistanceSquared)
			{
				if (_validRelationConstraint.IsFulfilled(row0, tableIndex0,
				                                         row1, tableIndex1))
				{
					validRelationConstraintFulfilled = true;
					return NoError;
				}

				IGeometry errorGeometry = CreateErrorGeometry(p0, p1);

				double dist = Math.Sqrt(pointDistanceSquared);
				string description = string.Format("Nodedistance {0}",
				                                   FormatLengthComparison(dist, "<",
					                                   SearchDistance,
					                                   p0.SpatialReference));

				IssueCode issueCode = _validRelationConstraintSql == null
					                      ? Codes[Code.NodeDistanceTooSmall]
					                      : Codes[Code.NodeDistanceTooSmall_ConstraintNotFulfilled];

				// TODO differentiate issue code if exactly coincident?
				return ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(row0, row1),
					errorGeometry, issueCode, TestUtils.GetShapeFieldName(row0),
					values: new object[] { dist });
			}

			if (_maxZDifference >= 0 && pointDistanceSquared < _searchDistanceSquared)
			{
				double z0 = p0.Z;
				double z1 = p1.Z;

				double absZDifference = Math.Abs(z0 - z1);

				if (absZDifference > _maxZDifference)
				{
					if (MathUtils.AreSignificantDigitsEqual(absZDifference, _maxZDifference))
					{
						// difference is not significant
						return NoError;
					}

					if (_validRelationConstraint.IsFulfilled(row0, tableIndex0,
					                                         row1, tableIndex1))
					{
						validRelationConstraintFulfilled = true;
						return NoError;
					}

					IGeometry errorGeometry = CreateErrorGeometry(p0, p1);

					string description = string.Format("Z-Difference {0}",
					                                   FormatComparison(absZDifference, ">",
						                                   _maxZDifference, "N1"));
					IssueCode issueCode = _validRelationConstraintSql == null
						                      ? Codes[Code.ZDifferenceTooLarge]
						                      : Codes[
							                      Code.ZDifferenceTooLarge_ConstraintNotFulfilled];

					return ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row0, row1), errorGeometry,
						issueCode, TestUtils.GetShapeFieldName(row0),
						values: new object[] { absZDifference });
				}
			}

			return NoError;
		}

		[NotNull]
		private static IGeometry CreateErrorGeometry([NotNull] IPoint p0,
		                                             [NotNull] IPoint p1)
		{
			return GeometryUtils.AreEqualInXY(p0, p1)
				       ? (IGeometry) GeometryFactory.Clone(p0)
				       : GeometryFactory.CreatePolyline(p0, p1);
		}
	}
}
