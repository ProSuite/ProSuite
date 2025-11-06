using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
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
	[MValuesTest]
	public class QaMeasuresAtPoints : ContainerTest
	{
		private readonly IReadOnlyFeatureClass _pointClass;
		private readonly string _expectedMValueExpression;
		private readonly LineMSource _lineMSource;
		private readonly bool _requireLine;
		private readonly bool _ignoreUndefinedExpectedMValue;
		private readonly string _matchExpression;
		private readonly double[] _mTolerance;
		private readonly int _tableCount;
		private readonly ISpatialReference _spatialReference;

		private DoubleFieldExpression _mExpressionHelper;
		private MatchExpression _matchExpressionHelper;
		private readonly IPoint _segmentFromPoint = new PointClass();
		private readonly IPoint _segmentToPoint = new PointClass();

		private bool _expressionHelpersInitialized;

		private IEnvelope _box;
		private IList<IFeatureClassFilter> _filter;
		private IList<QueryFilterHelper> _helper;
		private IPoint _nearPoint;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string ExpressionResultNotNumeric = "ExpressionResultNotNumeric";

			public const string UndefinedMValue_FromPointFeature =
				"UndefinedMValue.FromPointFeature";

			public const string UndefinedMValue_FromExpression =
				"UndefinedMValue.FromExpression";

			public const string PointNotNearLine = "PointNotNearLine";
			public const string NoVertexNearPoint = "NoVertexNearPoint";
			public const string MValueNotAsExpected = "MValueNotAsExpected";

			public Code() : base("MeasuresAtPoints") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMeasuresAtPoints_0))]
		public QaMeasuresAtPoints(
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_pointClass))] [NotNull]
			IReadOnlyFeatureClass pointClass,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_expectedMValueExpression))] [CanBeNull]
			string expectedMValueExpression,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_lineClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> lineClasses,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_searchDistance))]
			double searchDistance,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_mTolerance))]
			double mTolerance,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_lineMSource))]
			LineMSource lineMSource,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_requireLine))]
			bool requireLine)
			: this(
				pointClass, expectedMValueExpression, lineClasses, searchDistance, mTolerance,
				// ReSharper disable once IntroduceOptionalParameters.Global
				lineMSource, requireLine, false, null) { }

		[Doc(nameof(DocStrings.QaMeasuresAtPoints_0))]
		public QaMeasuresAtPoints(
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_pointClass))] [NotNull]
			IReadOnlyFeatureClass pointClass,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_expectedMValueExpression))] [CanBeNull]
			string expectedMValueExpression,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_lineClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> lineClasses,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_searchDistance))]
			double searchDistance,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_mTolerance))]
			double mTolerance,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_lineMSource))]
			LineMSource lineMSource,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_requireLine))]
			bool requireLine,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_ignoreUndefinedExpectedMValue))]
			bool ignoreUndefinedExpectedMValue,
			[Doc(nameof(DocStrings.QaMeasuresAtPoints_matchExpression))] [CanBeNull]
			string matchExpression)
			: base(
				CastToTables(
					(IEnumerable<IReadOnlyFeatureClass>) Union(new[] { pointClass }, lineClasses)))
		{
			Assert.NotNull(pointClass, "pointClass");
			Assert.NotNull(lineClasses, "lineClasses");
			Assert.True(searchDistance >= 0, "SearchDistance < 0");

			_tableCount = lineClasses.Count + 1;
			_pointClass = pointClass;
			_expectedMValueExpression = StringUtils.IsNotEmpty(expectedMValueExpression)
				                            ? expectedMValueExpression
				                            : null;

			_lineMSource = lineMSource;
			_requireLine = requireLine;
			_ignoreUndefinedExpectedMValue = ignoreUndefinedExpectedMValue;
			_matchExpression = StringUtils.IsNotEmpty(matchExpression)
				                   ? matchExpression
				                   : null;

			SearchDistance = searchDistance;

			_spatialReference = pointClass.SpatialReference;

			_mTolerance = GetMTolerances(lineClasses, mTolerance, _tableCount);
		}

		[InternallyUsedTest]
		public QaMeasuresAtPoints(
			[NotNull] QaMeasuresAtPointsDefinition definition)
			: this((IReadOnlyFeatureClass) definition.PointClass,
			       definition.ExpectedMValueExpression,
			       definition.LineClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.SearchDistance,
			       definition.MTolerance,
			       definition.LineMSource,
			       definition.RequireLine,
			       definition.IgnoreUndefinedExpectedMValue,
			       definition.MatchExpression) { }

		[CanBeNull]
		private MatchExpression GetMatchExpressionHelper(
			[CanBeNull] string matchExpression)
		{
			return matchExpression != null
				       ? new MatchExpression(matchExpression,
				                             GetSqlCaseSensitivity())
				       : null;
		}

		[CanBeNull]
		private DoubleFieldExpression GetMExpression(
			[NotNull] IReadOnlyFeatureClass pointClass,
			[CanBeNull] string expression)
		{
			Assert.ArgumentNotNull(pointClass, nameof(pointClass));

			if (expression != null)
			{
				const bool evaluateImmediately = true;

				var table = (IReadOnlyTable) pointClass;
				return new DoubleFieldExpression(table, expression,
				                                 evaluateImmediately,
				                                 GetSqlCaseSensitivity(table));
			}

			if (DatasetUtils.GetGeometryDef(pointClass).HasM)
			{
				return null;
			}

			throw new ArgumentException(
				string.Format(
					"'pointClass' {0} is not M-aware and there is no 'expectedMValueExpression' defined",
					pointClass.Name));
		}

		[NotNull]
		private static double[] GetMTolerances(
			[NotNull] IEnumerable<IReadOnlyFeatureClass> lineClasses,
			double mTolerance,
			int totalTableCount)
		{
			Assert.ArgumentNotNull(lineClasses, nameof(lineClasses));

			var result = new double[totalTableCount];

			int tableIndex = 0;
			foreach (IReadOnlyFeatureClass lineClass in lineClasses)
			{
				tableIndex++; // skip point class (index=0)

				if (! DatasetUtils.GetGeometryDef(lineClass).HasM)
				{
					throw new ArgumentException(
						string.Format("{0} is not M-aware",
						              lineClass.Name));
				}

				if (mTolerance >= 0)
				{
					result[tableIndex] = mTolerance;
				}
				else
				{
					double datasetMTolerance;
					bool hasValidMTolerance = DatasetUtils.TryGetMTolerance(
						lineClass.SpatialReference,
						out datasetMTolerance);

					if (! hasValidMTolerance)
					{
						throw new ArgumentException(
							string.Format("{0} has an undefined or invalid M tolerance",
							              lineClass.Name));
					}

					result[tableIndex] = datasetMTolerance;
				}
			}

			return result;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			// preparing
			if (_filter == null)
			{
				Init();
			}

			const int pointTableIndex = 0;

			if (tableIndex != pointTableIndex)
			{
				return NoError;
			}

			var pointFeature = row as IReadOnlyFeature;

			if (pointFeature == null)
			{
				return NoError;
			}

			if (! _expressionHelpersInitialized)
			{
				_matchExpressionHelper = GetMatchExpressionHelper(_matchExpression);
				_mExpressionHelper = GetMExpression(_pointClass, _expectedMValueExpression);

				_expressionHelpersInitialized = true;
			}

			var point = (IPoint) pointFeature.Shape;

			point.QueryEnvelope(_box);
			_box.Expand(SearchDistance, SearchDistance, false);

			double? expectedMValue;

			if (_mExpressionHelper != null)
			{
				try
				{
					expectedMValue = _mExpressionHelper.GetDouble(pointFeature);
				}
				catch (Exception e)
				{
					string description = string.Format(
						"Invalid value from expected M expression '{0}'. Numeric value expected ({1})",
						_expectedMValueExpression, e.Message);

					return ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(pointFeature),
						pointFeature.ShapeCopy, Codes[Code.ExpressionResultNotNumeric], null);
				}
			}
			else
			{
				expectedMValue = point.M;
			}

			if (expectedMValue == null || double.IsNaN(expectedMValue.Value))
			{
				if (_ignoreUndefinedExpectedMValue)
				{
					// expected value is undefined; just ignore (don't require NaN M values on line)
					return NoError;
				}

				string description;
				if (string.IsNullOrEmpty(_expectedMValueExpression))
				{
					description = "Undefined M value on point feature";
					return ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(pointFeature),
						pointFeature.ShapeCopy, Codes[Code.UndefinedMValue_FromPointFeature],
						TestUtils.GetShapeFieldName(pointFeature));
				}

				description = string.Format(
					"Undefined expected M value from expression '{0}'",
					_expectedMValueExpression);

				return ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(pointFeature),
					pointFeature.ShapeCopy, Codes[Code.UndefinedMValue_FromExpression], null);
			}

			int errorCount = 0;

			bool hasNeighborFeature = false;

			for (int neighborTableIndex = 1;
			     neighborTableIndex < _tableCount;
			     neighborTableIndex++)
			{
				var neighborClass = (IReadOnlyFeatureClass) InvolvedTables[neighborTableIndex];
				_helper[neighborTableIndex].MinimumOID = -1;

				int neighborFeatureCount;
				errorCount += CheckTable(pointFeature, tableIndex, point,
				                         expectedMValue.Value,
				                         neighborClass, neighborTableIndex,
				                         out neighborFeatureCount);

				if (neighborFeatureCount > 0)
				{
					hasNeighborFeature = true;
				}
			}

			if (_requireLine && ! hasNeighborFeature)
			{
				var error = (IPoint) pointFeature.ShapeCopy;

				string description =
					string.Format(
						"Point does not lie closer than {0} to any line",
						FormatLength(SearchDistance, _spatialReference));

				errorCount += ReportError(
					description, InvolvedRowUtils.GetInvolvedRows(pointFeature), error,
					Codes[Code.PointNotNearLine], TestUtils.GetShapeFieldName(pointFeature));
			}

			return errorCount;
		}

		private void Init()
		{
			_nearPoint = new PointClass();
			_box = new EnvelopeClass();

			CopyFilters(out _filter, out _helper);
			foreach (var filter in _filter)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}

			if (_requireLine)
			{
				foreach (QueryFilterHelper filterHelper in _helper)
				{
					filterHelper.ForNetwork = true;
				}
			}
		}

		private int CheckTable([NotNull] IReadOnlyFeature pointFeature,
		                       int pointTableIndex,
		                       [NotNull] IPoint point,
		                       double expectedMValue,
		                       [NotNull] IReadOnlyFeatureClass neighborClass,
		                       int neighborTableIndex,
		                       out int neighborCount)
		{
			neighborCount = 0;
			int errorCount = 0;

			// TODO check only the NEAREST line? 
			// Otherwise there may be errors due to search tolerance where line features connect on the same route
			// - however if the nearest point is a vertex and ANOTHER line connects to the same vertex, then 
			//   it should be checked too

			IFeatureClassFilter filter = _filter[neighborTableIndex];
			filter.FilterGeometry = _box;

			foreach (IReadOnlyRow row in Search(neighborClass,
			                                    _filter[neighborTableIndex],
			                                    _helper[neighborTableIndex]))
			{
				var neighborFeature = (IReadOnlyFeature) row;

				if (_matchExpressionHelper != null &&
				    ! _matchExpressionHelper.IsFulfilled(
					    pointFeature, pointTableIndex,
					    neighborFeature, neighborTableIndex))
				{
					// feature does not fulfill the (defined) match condition -> ignore
					continue;
				}

				IPoint nearestPoint;
				double distance = GetDistance(point, neighborFeature, out nearestPoint);

				if (double.IsNaN(distance) || nearestPoint == null)
				{
					continue;
				}

				neighborCount++;

				if (distance > SearchDistance)
				{
					if (_lineMSource == LineMSource.VertexRequired)
					{
						errorCount += ReportError(
							"No vertex near calibration point",
							InvolvedRowUtils.GetInvolvedRows(pointFeature, neighborFeature),
							pointFeature.ShapeCopy, Codes[Code.NoVertexNearPoint],
							TestUtils.GetShapeFieldName(pointFeature));
					}

					continue;
				}

				double actualMValue = nearestPoint.M;

				if (double.IsNaN(actualMValue))
				{
					errorCount += ReportError(
						string.Format("Expected M: {0}, actual M: undefined", expectedMValue),
						InvolvedRowUtils.GetInvolvedRows(pointFeature, neighborFeature),
						GeometryFactory.Clone(nearestPoint),
						Codes[Code.UndefinedMValue_FromPointFeature],
						TestUtils.GetShapeFieldName(pointFeature));
				}
				else
				{
					double mDifference = actualMValue - expectedMValue;
					double mTolerance = _mTolerance[neighborTableIndex];

					if (Math.Abs(mDifference) > mTolerance)
					{
						string description =
							string.Format("Expected M: {0}, actual M: {1}; difference: {2} ",
							              expectedMValue, actualMValue, mDifference);

						errorCount += ReportError(
							description,
							InvolvedRowUtils.GetInvolvedRows(pointFeature, neighborFeature),
							GeometryFactory.Clone(nearestPoint),
							Codes[Code.MValueNotAsExpected],
							TestUtils.GetShapeFieldName(pointFeature));
					}
				}
			}

			return errorCount;
		}

		private double GetDistance([NotNull] IPoint point,
		                           [NotNull] IReadOnlyFeature neighborFeature,
		                           [CanBeNull] out IPoint nearestPoint)
		{
			var neighborCurve = (ICurve) neighborFeature.Shape;

			double nearestDistance;
			int partIndex;
			int segmentIndex;
			bool found = GetNearestDistance(point, neighborCurve,
			                                _nearPoint, SearchDistance,
			                                out nearestDistance, out partIndex,
			                                out segmentIndex);

			if (! found)
			{
				nearestPoint = null;
				return double.NaN;
			}

			if (_lineMSource == LineMSource.Nearest)
			{
				nearestPoint = _nearPoint;
				return nearestDistance;
			}

			IPoint nearestVertex;
			double nearestVertexDistance = GetNearestVertexDistance(point, neighborCurve,
				partIndex, segmentIndex,
				out nearestVertex);

			if (_lineMSource == LineMSource.VertexRequired ||
			    nearestVertexDistance < SearchDistance)
			{
				nearestPoint = nearestVertex;
				return nearestVertexDistance;
			}

			// return nearest distance/point
			nearestPoint = _nearPoint;
			return nearestDistance;
		}

		private double GetNearestVertexDistance([NotNull] IPoint point,
		                                        [NotNull] ICurve curve,
		                                        int partIndex, int segmentIndex,
		                                        out IPoint nearestVertex)
		{
			ISegment segment = GeometryUtils.GetSegment((ISegmentCollection) curve, partIndex,
			                                            segmentIndex);

			segment.QueryFromPoint(_segmentFromPoint);
			segment.QueryToPoint(_segmentToPoint);

			var pointProximityOperator = (IProximityOperator) point;

			double fromPointDistance = pointProximityOperator.ReturnDistance(_segmentFromPoint);
			double toPointDistance = pointProximityOperator.ReturnDistance(_segmentToPoint);

			if (fromPointDistance <= toPointDistance)
			{
				nearestVertex = _segmentFromPoint;
				return fromPointDistance;
			}

			nearestVertex = _segmentToPoint;
			return toPointDistance;
		}

		private static bool GetNearestDistance([NotNull] IPoint point,
		                                       [NotNull] ICurve neighborCurve,
		                                       [NotNull] IPoint pointTemplate,
		                                       double searchTolerance,
		                                       out double distance,
		                                       out int partIndex,
		                                       out int segmentIndex)
		{
			distance = 0;
			bool rightSide = false;
			partIndex = 0;
			segmentIndex = 0;

			var hitTest = (IHitTest) neighborCurve;

			return hitTest.HitTest(point, searchTolerance,
			                       esriGeometryHitPartType.esriGeometryPartBoundary,
			                       pointTemplate,
			                       ref distance, ref partIndex, ref segmentIndex, ref rightSide);
		}

		private class MatchExpression : RowPairCondition
		{
			public MatchExpression([CanBeNull] string constraint, bool caseSensitive)
				: base(constraint, true, true, "P", "L", caseSensitive) { }
		}
	}
}
