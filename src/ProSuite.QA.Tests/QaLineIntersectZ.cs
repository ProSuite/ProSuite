using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.SpatialRelations;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check if there are any crossing lines that are too close
	/// to each other within several line layers
	/// </summary>
	[CLSCompliant(false)]
	[UsedImplicitly]
	[TopologyTest]
	[ZValuesTest]
	[LinearNetworkTest]
	[IntersectionParameterTest]
	public class QaLineIntersectZ : QaSpatialRelationSelfBase
	{
		[CanBeNull] private readonly string _zOrderConstraintSql;
		private readonly double _minimumZDifference;
		private readonly double _maximumZDifference;
		[CanBeNull] private string _minimumZDifferenceExpressionSql;
		[CanBeNull] private string _maximumZDifferenceExpressionSql;
		[CanBeNull] private ZDifferenceBoundExpression _minimumZDifferenceExpression;
		[CanBeNull] private ZDifferenceBoundExpression _maximumZDifferenceExpression;
		[CanBeNull] private ZOrderConstraint _zOrderConstraint;

		[NotNull] private readonly IPoint _intersectionPointTemplate = new PointClass();
		[CanBeNull] private readonly Dictionary<string, object> _zDifferenceColumnValue;

		private const string _zDifferenceColumn = "_ZDifference";

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ConstraintNotFulfilled = "ConstraintNotFulfilled";

			public const string ZDifferenceAtLineIntersection_LargerThanLimit =
				"ZDifferenceAtLineIntersection.LargerThanLimit";

			public const string ZDifferenceAtLineIntersection_SmallerThanLimit =
				"ZDifferenceAtLineIntersection.SmallerThanLimit";

			public Code() : base("LineIntersectZ") { }
		}

		#endregion

		[Doc("QaLineIntersectZ_0")]
		public QaLineIntersectZ(
			[Doc("QaLineIntersectZ_polylineClasses")]
			IList<IFeatureClass> polylineClasses,
			[Doc("QaLineIntersectZ_limit_0")] double limit)
			: this(polylineClasses, limit, string.Empty) { }

		[Doc("QaLineIntersectZ_1")]
		public QaLineIntersectZ(
			[Doc("QaLineIntersectZ_polylineClass")]
			IFeatureClass polylineClass,
			[Doc("QaLineIntersectZ_limit_0")] double limit)
			: this(polylineClass, limit, string.Empty) { }

		[Doc("QaLineIntersectZ_2")]
		public QaLineIntersectZ(
			[Doc("QaLineIntersectZ_polylineClass")]
			IFeatureClass polylineClass,
			[Doc("QaLineIntersectZ_limit_1")] double limit,
			[Doc("QaLineIntersectZ_constraint")] string constraint)
			: this(new[] {polylineClass}, limit, constraint) { }

		[Doc("QaLineIntersectZ_3")]
		public QaLineIntersectZ(
			[Doc("QaLineIntersectZ_polylineClasses")]
			IList<IFeatureClass> polylineClasses,
			[Doc("QaLineIntersectZ_limit_1")] double limit,
			[Doc("QaLineIntersectZ_constraint")] string constraint)
			: this(polylineClasses, limit, 0, constraint) { }

		[Doc("QaLineIntersectZ_4")]
		public QaLineIntersectZ(
			[Doc("QaLineIntersectZ_polylineClasses")]
			IList<IFeatureClass> polylineClasses,
			[Doc("QaLineIntersectZ_minimumZDifference")]
			double minimumZDifference,
			[Doc("QaLineIntersectZ_maximumZDifference")]
			double maximumZDifference,
			[Doc("QaLineIntersectZ_constraint")] string constraint)
			: base(polylineClasses, esriSpatialRelEnum.esriSpatialRelCrosses)
		{
			_minimumZDifference = minimumZDifference;
			_maximumZDifference = maximumZDifference;
			_zOrderConstraintSql = constraint?.Trim();

			if (constraint != null &&
			    constraint.IndexOf(_zDifferenceColumn,
			                       StringComparison.InvariantCultureIgnoreCase) >= 0)
			{
				_zDifferenceColumnValue = new Dictionary<string, object>();
			}
		}

		[TestParameter]
		[Doc("QaLineIntersectZ_MinimumZDifferenceExpression")]
		public string MinimumZDifferenceExpression
		{
			get { return _minimumZDifferenceExpressionSql; }
			set
			{
				var trimmed = value?.Trim();
				if (trimmed != _minimumZDifferenceExpressionSql)
				{
					_minimumZDifferenceExpressionSql = trimmed;
					_minimumZDifferenceExpression = null;
				}
			}
		}

		[TestParameter]
		[Doc("QaLineIntersectZ_MaximumZDifferenceExpression")]
		public string MaximumZDifferenceExpression
		{
			get { return _maximumZDifferenceExpressionSql; }
			set
			{
				var trimmed = value?.Trim();
				if (trimmed != _maximumZDifferenceExpressionSql)
				{
					_maximumZDifferenceExpressionSql = trimmed;
					_maximumZDifferenceExpression = null;
				}
			}
		}

		protected override int FindErrors(IRow row1, int tableIndex1,
		                                  IRow row2, int tableIndex2)
		{
			if (row1 == row2)
			{
				return NoError;
			}

			var feature1 = (IFeature) row1;
			var feature2 = (IFeature) row2;

			IGeometry crossings = GetLineCrossings(feature1, feature2);

			if (crossings.IsEmpty)
			{
				return NoError;
			}

			var points = (IPointCollection) crossings;
			int pointCount = points.PointCount;

			int errorCount = 0;
			for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
			{
				points.QueryPoint(pointIndex, _intersectionPointTemplate);

				errorCount += CheckIntersection(_intersectionPointTemplate,
				                                feature1, tableIndex1,
				                                feature2, tableIndex2);
			}

			return errorCount;
		}

		[NotNull]
		private static IGeometry GetLineCrossings([NotNull] IFeature feature1,
		                                          [NotNull] IFeature feature2)
		{
			var shape1 = (IPolyline) feature1.Shape;
			var shape2 = (IPolyline) feature2.Shape;

			return IntersectionUtils.GetLineCrossings(shape1, shape2);
		}

		private int CheckIntersection([NotNull] IPoint intersectionPoint,
		                              [NotNull] IFeature feature1, int tableIndex1,
		                              [NotNull] IFeature feature2, int tableIndex2)
		{
			double feature1Z = intersectionPoint.Z;

			if (double.IsNaN(feature1Z))
			{
				return NoError;
			}

			double feature2Z = GeometryUtils.GetZValueFromGeometry(
				feature2.Shape, intersectionPoint,
				GeometryUtils.GetXyTolerance(feature2));

			if (double.IsNaN(feature2Z))
			{
				return NoError;
			}

			double dz = Math.Abs(feature1Z - feature2Z);

			double minimumZDifference =
				feature1Z >= feature2Z
					? GetMinimumZDifference(feature1, tableIndex1,
					                        feature2, tableIndex2)
					: GetMinimumZDifference(feature2, tableIndex2,
					                        feature1, tableIndex1);
			double maximumZDifference =
				feature1Z >= feature2Z
					? GetMaximumZDifference(feature1, tableIndex1,
					                        feature2, tableIndex2)
					: GetMaximumZDifference(feature2, tableIndex2,
					                        feature1, tableIndex1);

			int errorCount = 0;

			if (minimumZDifference > 0 && dz < minimumZDifference)
			{
				// a z difference smaller than the minimum is always an error
				errorCount += ReportIssue(
					$"Z distance is {dz:N2}. Minimum allowed distance is {minimumZDifference}",
					intersectionPoint,
					Codes[Code.ZDifferenceAtLineIntersection_SmallerThanLimit],
					TestUtils.GetShapeFieldName(feature1),
					dz, feature1, feature2);
			}

			if (maximumZDifference > 0 && dz > maximumZDifference)
			{
				// a z difference larger than the maximum is always an error
				errorCount += ReportIssue(
					$"Z distance is {dz:N2}. Maximum allowed distance is {maximumZDifference}",
					intersectionPoint,
					Codes[Code.ZDifferenceAtLineIntersection_LargerThanLimit],
					TestUtils.GetShapeFieldName(feature1),
					dz, feature1, feature2);
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

		private double GetMinimumZDifference([NotNull] IFeature upper,
		                                     int tableIndexUpper,
		                                     [NotNull] IFeature lower,
		                                     int tableIndexLower)
		{
			if (string.IsNullOrEmpty(_minimumZDifferenceExpressionSql))
			{
				return _minimumZDifference;
			}

			if (_minimumZDifferenceExpression == null)
			{
				_minimumZDifferenceExpression =
					new ZDifferenceBoundExpression(_minimumZDifferenceExpressionSql,
					                               GetSqlCaseSensitivity());
			}

			return _minimumZDifferenceExpression.GetDouble(
				       upper, tableIndexUpper,
				       lower, tableIndexLower) ?? 0;
		}

		private double GetMaximumZDifference([NotNull] IFeature upper,
		                                     int tableIndexUpper,
		                                     [NotNull] IFeature lower,
		                                     int tableIndexLower)
		{
			if (string.IsNullOrEmpty(_maximumZDifferenceExpressionSql))
			{
				return _maximumZDifference;
			}

			if (_maximumZDifferenceExpression == null)
			{
				_maximumZDifferenceExpression =
					new ZDifferenceBoundExpression(_maximumZDifferenceExpressionSql,
					                               GetSqlCaseSensitivity());
			}

			return _maximumZDifferenceExpression.GetDouble(
				       upper, tableIndexUpper,
				       lower, tableIndexLower) ?? 0;
		}

		private int CheckConstraint([NotNull] IRow upperRow, int upperTableIndex,
		                            [NotNull] IRow lowerRow, int lowerTableIndex,
		                            [NotNull] IPoint intersection, double zDifference)
		{
			if (string.IsNullOrEmpty(_zOrderConstraintSql))
			{
				return NoError;
			}

			if (_zOrderConstraint == null)
			{
				_zOrderConstraint = new ZOrderConstraint(_zOrderConstraintSql,
				                                         GetSqlCaseSensitivity());
			}

			if (_zDifferenceColumnValue != null)
			{
				_zDifferenceColumnValue[_zDifferenceColumn] = zDifference;
			}

			string message;
			if (_zOrderConstraint.IsFulfilled(upperRow, upperTableIndex,
			                                  lowerRow, lowerTableIndex,
			                                  out message,
			                                  _zDifferenceColumnValue))
			{
				return NoError;
			}

			string description =
				string.Format("Constraint '{0}' is not fulfilled: {1}",
				              _zOrderConstraintSql, message);
			return ReportIssue(description, intersection,
			                   Codes[Code.ConstraintNotFulfilled],
			                   TestUtils.GetShapeFieldName(upperRow),
			                   zDifference,
			                   upperRow, lowerRow);
		}

		private int ReportIssue([NotNull] string description,
		                        [NotNull] IPoint intersectionPoint,
		                        IssueCode issueCode,
		                        [CanBeNull] string affectedComponent,
		                        double zDifference,
		                        params IRow[] rows)
		{
			// May be intersectionPoint == _intersectionPointTemplate
			// --> create a clone, so that not the same errorGeometry instance exists in different errors

			IGeometry errorGeometry = GeometryFactory.Clone(intersectionPoint);
			return ReportError(description, errorGeometry, issueCode, affectedComponent,
			                   new List<object> {zDifference},
			                   rows);
		}

		private class ZOrderConstraint : RowPairCondition
		{
			public ZOrderConstraint([CanBeNull] string condition,
			                        bool caseSensitive)
				: base(condition, true, true, "U", "L",
				       caseSensitive) { }

			protected override void AddUnboundColumns(Action<string, Type> addColumn,
			                                          IList<ITable> tables)
			{
				addColumn(_zDifferenceColumn, typeof(double));
			}
		}

		private class ZDifferenceBoundExpression : MultiTableDoubleFieldExpression
		{
			public ZDifferenceBoundExpression([NotNull] string expression,
			                                  bool caseSensitive)
				: base(expression, "U", "L", caseSensitive) { }
		}
	}
}
