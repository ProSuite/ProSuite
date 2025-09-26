using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Coincidence;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using IPnt = ProSuite.Commons.Geom.IPnt;
using Pnt = ProSuite.Commons.Geom.Pnt;
using SegmentUtils_ = ProSuite.QA.Container.Geometry.SegmentUtils_;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaPointNotNear : ContainerTest
	{
		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string PointTooClose = "PointTooClose";

			public const string PointTooClose_ConstraintNotFulfilled =
				"PointTooClose.ConstraintNotFulfilled";

			public const string PointWithin = "PointWithin";

			public const string PointWithin_ConstraintNotFulfilled =
				"PointWithin.ConstraintNotFulfilled";

			public Code() : base("PointNotNear") { }
		}

		#endregion

		private const GeometryComponent _defaultGeometryComponent =
			GeometryComponent.EntireGeometry;

		private IEnvelope _box;
		private IList<IFeatureClassFilter> _filter;
		private IList<QueryFilterHelper> _helper;

		private readonly IPoint _nearPoint;
		private readonly int _tableCount;
		private IPoint _pointTemplate;

		[NotNull] private readonly string _shapeFieldName;
		[NotNull] private readonly ISpatialReference _spatialReference;
		private readonly int _referenceClassCount;
		[CanBeNull] private IList<GeometryComponent> _geometryComponents;

		[NotNull] private IList<string> _validRelationConstraints =
			new ReadOnlyList<string>(new List<string>());

		[CanBeNull] private List<IValidRelationConstraint> _validConstraints;
		[CanBeNull] private readonly string _pointDistanceExpressionSql;
		[CanBeNull] private readonly IList<string> _referenceDistanceExpressionsSql;
		[CanBeNull] private DoubleFieldExpression _pointDistanceExpression;
		[CanBeNull] private List<DoubleFieldExpression> _referenceDistanceExpressions;
		[CanBeNull] private readonly IList<string> _referenceRightSideDistanceSqls;
		[CanBeNull] private readonly IList<string> _referenceFlipExpressions;

		[CanBeNull] private List<DoubleFieldExpression> _referenceRightSideDistanceExpressions;

		[CanBeNull] private List<RowCondition> _referenceFlipConditions;

		private const double _defaultMinimumErrorLineLength = -1;
		private const int _firstReferenceClassIndex = 1;
		private readonly bool _useDistanceExpressions;
		[NotNull] private readonly IDictionary<int, double> _xyToleranceByTableIndex;
		private readonly double _pointClassXyTolerance;

		[Doc(nameof(DocStrings.QaPointNotNear_0))]
		public QaPointNotNear(
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_pointClass))]
			IReadOnlyFeatureClass pointClass,
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceClass))]
			IReadOnlyFeatureClass referenceClass,
			[Doc(nameof(DocStrings.QaPointNotNear_limit))]
			double limit)
			: this(pointClass, new[] { referenceClass }, limit) { }

		[Doc(nameof(DocStrings.QaPointNotNear_1))]
		public QaPointNotNear(
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_pointClass))]
			IReadOnlyFeatureClass pointClass,
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceClasses))]
			IList<IReadOnlyFeatureClass>
				referenceClasses,
			[Doc(nameof(DocStrings.QaPointNotNear_limit))]
			double limit)
			: base(CastToTables(new[] { pointClass }, referenceClasses))
		{
			Assert.ArgumentNotNull(pointClass, nameof(pointClass));
			Assert.ArgumentNotNull(referenceClasses, nameof(referenceClasses));

			_shapeFieldName = pointClass.ShapeFieldName;
			_spatialReference = pointClass.SpatialReference;
			SearchDistance = limit;
			_filter = null;
			_tableCount = InvolvedTables.Count;
			_referenceClassCount = _tableCount - 1;
			_nearPoint = new PointClass();

			_xyToleranceByTableIndex =
				TestUtils.GetXyToleranceByTableIndex(InvolvedTables);
			_pointClassXyTolerance = _xyToleranceByTableIndex[0];
		}

		[Doc(nameof(DocStrings.QaPointNotNear_2))]
		public QaPointNotNear(
				[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_pointClass))]
				IReadOnlyFeatureClass pointClass,
				[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceClasses))]
				IList<IReadOnlyFeatureClass>
					referenceClasses,
				[Doc(nameof(DocStrings.QaPointNotNear_searchDistance))]
				double searchDistance,
				[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_pointDistanceExpression))]
				string
					pointDistanceExpression,
				[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceDistanceExpressions))]
				IList<string>
					referenceDistanceExpressions)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(pointClass, referenceClasses, searchDistance, pointDistanceExpression,
			       referenceDistanceExpressions,
			       referenceRightSideDistances: null,
			       referenceFlipExpressions: null) { }

		[Doc(nameof(DocStrings.QaPointNotNear_3))]
		public QaPointNotNear(
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_pointClass))]
			IReadOnlyFeatureClass pointClass,
			[NotNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceClasses))]
			IList<IReadOnlyFeatureClass>
				referenceClasses,
			[Doc(nameof(DocStrings.QaPointNotNear_searchDistance))]
			double searchDistance,
			[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_pointDistanceExpression))]
			string
				pointDistanceExpression,
			[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceDistanceExpressions))]
			IList<string>
				referenceDistanceExpressions,
			[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceRightSideDistances))]
			IList<string>
				referenceRightSideDistances,
			[CanBeNull] [Doc(nameof(DocStrings.QaPointNotNear_referenceFlipExpressions))]
			IList<string>
				referenceFlipExpressions)
			: base(CastToTables(new[] { pointClass }, referenceClasses))
		{
			Assert.ArgumentNotNull(pointClass, nameof(pointClass));
			Assert.ArgumentNotNull(referenceClasses, nameof(referenceClasses));
			Assert.ArgumentCondition(referenceDistanceExpressions == null ||
			                         referenceDistanceExpressions.Count == 0 ||
			                         referenceDistanceExpressions.Count == 1 ||
			                         referenceDistanceExpressions.Count ==
			                         referenceClasses.Count,
			                         "unexpected number of reference distance expression " +
			                         "(must be 0, 1, or # of references tables)");
			Assert.ArgumentCondition(referenceRightSideDistances == null ||
			                         referenceRightSideDistances.Count == 0 ||
			                         referenceRightSideDistances.Count == 1 ||
			                         referenceRightSideDistances.Count ==
			                         _referenceClassCount,
			                         "unexpected number of reference right side distances " +
			                         "(must be 0, 1, or equal to the number of reference classes");
			Assert.ArgumentCondition(referenceFlipExpressions == null ||
			                         referenceFlipExpressions.Count == 0 ||
			                         referenceFlipExpressions.Count == 1 ||
			                         referenceFlipExpressions.Count ==
			                         _referenceClassCount,
			                         "unexpected number of reference flip expressions " +
			                         "(must be 0, 1, or equal to the number of reference classes");

			_shapeFieldName = pointClass.ShapeFieldName;
			_spatialReference = pointClass.SpatialReference;
			SearchDistance = searchDistance;
			_pointDistanceExpressionSql = pointDistanceExpression;
			_referenceDistanceExpressionsSql = referenceDistanceExpressions;
			_useDistanceExpressions = true;
			_referenceRightSideDistanceSqls =
				referenceRightSideDistances?.ToList() ?? new List<string>();
			_referenceFlipExpressions =
				referenceFlipExpressions?.ToList() ?? new List<string>();

			AddCustomQueryFilterExpression(pointDistanceExpression);
			foreach (string sql in _referenceDistanceExpressionsSql ?? new List<string>())
			{
				AddCustomQueryFilterExpression(sql);
			}

			foreach (string sql in _referenceRightSideDistanceSqls)
			{
				AddCustomQueryFilterExpression(sql);
			}

			foreach (string sql in _referenceFlipExpressions)
			{
				AddCustomQueryFilterExpression(sql);
			}

			_filter = null;
			_tableCount = InvolvedTables.Count;
			_referenceClassCount = referenceClasses.Count;
			_nearPoint = new PointClass();

			_xyToleranceByTableIndex =
				TestUtils.GetXyToleranceByTableIndex(InvolvedTables);
			_pointClassXyTolerance = _xyToleranceByTableIndex[0];
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaPointNotNear_AllowCoincidentPoints))]
		public bool AllowCoincidentPoints { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaPointNotNear_GeometryComponents))]
		public IList<GeometryComponent> GeometryComponents
		{
			get { return _geometryComponents; }
			set
			{
				if (value != null &&
				    value.Count > 1 &&
				    value.Count != _referenceClassCount)
				{
					throw new ArgumentException(
						$"Expected 0, 1 or {_referenceClassCount} GeometryComponents");
				}

				_geometryComponents = value;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaPointNotNear_ValidRelationConstraints))]
		public IList<string> ValidRelationConstraints
		{
			get { return _validRelationConstraints; }
			set
			{
				Assert.ArgumentCondition(value == null ||
				                         value.Count == 0 || value.Count == 1 ||
				                         value.Count == _referenceClassCount,
				                         "unexpected number of valid relation constraints " +
				                         "(must be 0, 1, or equal to the number of reference classes");
				if (value != null && value.Count > 1 &&
				    value.Count != InvolvedTables.Count - 1)
				{
					throw new InvalidOperationException(
						string.Format("Expected 0, 1 or {0} ValidRelationConstraints",
						              InvolvedTables.Count - 1));
				}

				_validConstraints = null; // trigger re-initialization
				_validRelationConstraints =
					new ReadOnlyList<string>(value?.ToList() ?? new List<string>());
			}
		}

		[TestParameter(_defaultMinimumErrorLineLength)]
		[Doc(nameof(DocStrings.QaPointNotNear_MinimumErrorLineLength))]
		public double MinimumErrorLineLength { get; set; } =
			_defaultMinimumErrorLineLength;

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			// preparing
			if (_filter == null)
			{
				InitFilter();
			}

			if (_validConstraints == null)
			{
				const bool constraintIsDirected = true;
				_validConstraints =
					_validRelationConstraints
						.Select(constraint => new ValidRelationConstraint(constraint,
							        constraintIsDirected,
							        GetSqlCaseSensitivity()))
						.Cast<IValidRelationConstraint>()
						.ToList();
			}

			if (tableIndex > 0)
			{
				return NoError;
			}

			var feature = row as IReadOnlyFeature;

			if (feature == null)
			{
				return NoError;
			}

			var errorCount = 0;
			for (int referencedClassIndex = _firstReferenceClassIndex;
			     referencedClassIndex < _tableCount;
			     referencedClassIndex++)
			{
				var referenceClass = (IReadOnlyFeatureClass) InvolvedTables[referencedClassIndex];
				_helper[referencedClassIndex].MinimumOID = -1;

				errorCount += CheckTable(feature, referenceClass, referencedClassIndex);
			}

			return errorCount;
		}

		[NotNull]
		private List<DoubleFieldExpression> GetReferenceDistanceExpressions(
			[CanBeNull] IList<string> expressions)
		{
			var result = new List<DoubleFieldExpression>();
			if (expressions != null &&
			    expressions.Count > 0)
			{
				for (int referencedClassIndex = _firstReferenceClassIndex;
				     referencedClassIndex < _tableCount;
				     referencedClassIndex++)
				{
					int expressionIndex = expressions.Count == 1
						                      ? 0
						                      : referencedClassIndex - 1;
					string expression = expressions[expressionIndex];

					result.Add(new DoubleFieldExpression(
						           InvolvedTables[referencedClassIndex],
						           expression,
						           caseSensitive:
						           GetSqlCaseSensitivity(referencedClassIndex)));
				}
			}

			return result;
		}

		[NotNull]
		private List<RowCondition> CreateFlipConditions(
			[CanBeNull] IList<string> flipExpressions)
		{
			if (flipExpressions == null || flipExpressions.Count == 0)
			{
				return new List<RowCondition>();
			}

			var result = new List<RowCondition>();
			for (int referencedClassIndex = _firstReferenceClassIndex;
			     referencedClassIndex < _tableCount;
			     referencedClassIndex++)
			{
				int expressionIndex = flipExpressions.Count == 1
					                      ? 0
					                      : referencedClassIndex - 1;
				string expression = flipExpressions[expressionIndex];

				result.Add(new RowCondition(InvolvedTables[referencedClassIndex],
				                            expression,
				                            caseSensitive:
				                            GetSqlCaseSensitivity(referencedClassIndex)));
			}

			return result;
		}

		// TODO 1:1 copy from QaNodeLineCoincidence. Move to TestUtils
		/// <summary>
		/// Gets the nodes.
		/// </summary>
		/// <param name="shape">The shape.</param>
		/// <param name="pointTemplate">The point template.</param>
		/// <returns>The nodes for the shape. The same point instance (the template) is returned for each iteration. 
		/// Therefore the returned points must be immediately processed and must not be put in a list or otherwise kept around (unless cloned)</returns>
		[NotNull]
		private static IEnumerable<IPoint> GetNodes([NotNull] IGeometry shape,
		                                            [NotNull] IPoint pointTemplate)
		{
			switch (shape.GeometryType)
			{
				case esriGeometryType.esriGeometryPoint:
					yield return (IPoint) shape;
					break;

				case esriGeometryType.esriGeometryPolyline:
					var polyline = (IPolyline) shape;
					var parts = polyline as IGeometryCollection;

					if (parts == null || parts.GeometryCount == 1)
					{
						// single-part polyline
						polyline.QueryFromPoint(pointTemplate);
						yield return pointTemplate;

						polyline.QueryToPoint(pointTemplate);
						yield return pointTemplate;
					}
					else
					{
						// multipart polyline; get the from/to points of each individual path
						foreach (IGeometry part in GeometryUtils.GetParts(parts))
						{
							foreach (IPoint point in GetNodes(part, pointTemplate))
							{
								yield return point;
							}
						}
					}

					break;

				case esriGeometryType.esriGeometryPath:
					var path = (IPath) shape;

					path.QueryFromPoint(pointTemplate);
					yield return pointTemplate;

					path.QueryToPoint(pointTemplate);
					yield return pointTemplate;

					break;

				default:
					yield break;
			}
		}

		private int CheckTable([NotNull] IReadOnlyFeature feature,
		                       [NotNull] IReadOnlyFeatureClass referenceClass,
		                       int referenceClassIndex)
		{
			GeometryComponent geometryComponent =
				GetGeometryComponent(referenceClassIndex);

			IValidRelationConstraint validConstraint =
				GetValidRelationConstraint(referenceClassIndex);

			if (_pointTemplate == null)
			{
				_pointTemplate = new PointClass();
			}

			var errorCount = 0;

			foreach (IPoint point in GetNodes(feature.Shape, _pointTemplate))
			{
				IFeatureClassFilter filter = PrepareSpatialFilter(point, referenceClassIndex);
				QueryFilterHelper helper = _helper[referenceClassIndex];

				const int pointClassIndex = 0;
				foreach (IReadOnlyRow referenceRow in Search(
					         referenceClass, filter, helper))
				{
					if (TestUtils.IsSameRow(feature, referenceRow))
					{
						continue;
					}

					var referenceFeature = (IReadOnlyFeature) referenceRow;

					if (validConstraint != null &&
					    validConstraint.HasConstraint &&
					    validConstraint.IsFulfilled(feature, pointClassIndex,
					                                referenceFeature, referenceClassIndex,
					                                out string _))
					{
						continue;
					}

					double standardDistance;
					double? rightSideDistance;
					double maxNeededDistance = GetMaxNeededDistance(
						feature, referenceFeature, referenceClassIndex,
						out standardDistance, out rightSideDistance);
					maxNeededDistance = Math.Max(maxNeededDistance, _pointClassXyTolerance);

					double distance = GetDistance(point, referenceFeature, _nearPoint,
					                              geometryComponent, maxNeededDistance,
					                              out bool isWithinPolygon, out bool onRightSide);

					if (double.IsNaN(distance))
					{
						continue;
					}

					if (AllowCoincidentPoints &&
					    IsSmallerThanXyTolerance(distance, referenceClassIndex))
					{
						continue;
					}

					if (distance > maxNeededDistance)
					{
						continue;
					}

					double minimumDistance = maxNeededDistance;
					if (rightSideDistance.HasValue)
					{
						if (_referenceFlipConditions == null)
						{
							_referenceFlipConditions =
								CreateFlipConditions(_referenceFlipExpressions);
						}

						bool flip = _referenceFlipConditions.Count > 0
							            ? _referenceFlipConditions[
								            referenceClassIndex -
								            _firstReferenceClassIndex].IsFulfilled(
								            referenceFeature)
							            : false;

						double useDistance = onRightSide == flip
							                     ? standardDistance
							                     : rightSideDistance.Value;
						if (distance > useDistance)
						{
							continue;
						}

						minimumDistance = useDistance;
					}

					errorCount += ReportError(feature, referenceFeature,
					                          point, _nearPoint,
					                          distance, minimumDistance,
					                          isWithinPolygon, geometryComponent,
					                          validConstraint);
				}
			}

			return errorCount;
		}

		private bool IsSmallerThanXyTolerance(double distance, int referenceClassIndex)
		{
			// use maximum of point xy tolerance/reference class xy tolerance
			double xyTolerance = Math.Max(_pointClassXyTolerance,
			                              _xyToleranceByTableIndex[referenceClassIndex]);

			return distance <= xyTolerance;
		}

		private double GetMaxNeededDistance([NotNull] IReadOnlyFeature pointFeature,
		                                    [NotNull] IReadOnlyFeature referenceFeature,
		                                    int referenceClassIndex,
		                                    out double standardDistance,
		                                    out double? rightSideDistance)
		{
			if (_referenceRightSideDistanceExpressions == null)
			{
				_referenceRightSideDistanceExpressions =
					GetReferenceDistanceExpressions(_referenceRightSideDistanceSqls);
			}

			double? referenceRightSideDistance =
				_referenceRightSideDistanceExpressions.Count > 0
					? _referenceRightSideDistanceExpressions[
						referenceClassIndex - _firstReferenceClassIndex].GetDouble(
						referenceFeature)
					: null;

			if (! _useDistanceExpressions)
			{
				standardDistance = SearchDistance;
				rightSideDistance = referenceRightSideDistance;
				return SearchDistance;
			}

			// lazy initialization
			if (_pointDistanceExpression == null &&
			    StringUtils.IsNotEmpty(_pointDistanceExpressionSql))
			{
				_pointDistanceExpression = new DoubleFieldExpression(
					InvolvedTables[0],
					_pointDistanceExpressionSql,
					caseSensitive: GetSqlCaseSensitivity(0));
			}

			double? pointDistance = _pointDistanceExpression?.GetDouble(pointFeature);

			if (_referenceDistanceExpressions == null)
			{
				_referenceDistanceExpressions =
					GetReferenceDistanceExpressions(_referenceDistanceExpressionsSql);
			}

			// get distances from expressions
			double? referenceDistance =
				_referenceDistanceExpressions.Count > 0
					? _referenceDistanceExpressions[
							referenceClassIndex - _firstReferenceClassIndex]
						.GetDouble(referenceFeature)
					: null;

			standardDistance = (pointDistance ?? 0) + (referenceDistance ?? 0);
			rightSideDistance = referenceRightSideDistance.HasValue
				                    ? (pointDistance ?? 0) + referenceRightSideDistance
				                    : null;
			return Math.Max(standardDistance, rightSideDistance ?? 0);
		}

		private int ReportError([NotNull] IReadOnlyFeature pointFeature,
		                        [NotNull] IReadOnlyFeature referenceFeature,
		                        [NotNull] IPoint point,
		                        [NotNull] IPoint nearPoint,
		                        double distance,
		                        double minimumDistance, bool isWithinPolygon,
		                        GeometryComponent geometryComponent,
		                        [CanBeNull] IValidRelationConstraint validConstraint)
		{
			IssueCode issueCode;
			string description;
			IGeometry errorGeometry;

			if (isWithinPolygon)
			{
				description = "Point lies within polygon";
				errorGeometry = GeometryFactory.Clone(point);

				issueCode = validConstraint == null
					            ? Codes[Code.PointWithin]
					            : Codes[Code.PointWithin_ConstraintNotFulfilled];
			}
			else
			{
				if (geometryComponent == GeometryComponent.EntireGeometry)
				{
					description =
						string.Format(
							"Point is too close to reference feature: {0}",
							FormatLengthComparison(distance, "<", minimumDistance,
							                       _spatialReference));
				}
				else
				{
					description =
						string.Format(
							"Point is too close to {0} of reference feature: {1}",
							GeometryComponentUtils.GetDisplayText(geometryComponent),
							FormatLengthComparison(distance, "<", minimumDistance,
							                       _spatialReference));
				}

				bool reportAsConnectionLine = MinimumErrorLineLength >= 0 &&
				                              distance >= MinimumErrorLineLength;

				errorGeometry =
					GetErrorGeometry(point, nearPoint, reportAsConnectionLine);

				issueCode = validConstraint == null
					            ? Codes[Code.PointTooClose]
					            : Codes[Code.PointTooClose_ConstraintNotFulfilled];
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(pointFeature, referenceFeature),
				errorGeometry, issueCode, _shapeFieldName);
		}

		[NotNull]
		private IFeatureClassFilter PrepareSpatialFilter([NotNull] IPoint point,
		                                                 int referenceClassIndex)
		{
			point.QueryEnvelope(_box);
			_box.Expand(SearchDistance, SearchDistance, false);

			IFeatureClassFilter filter = _filter[referenceClassIndex];
			filter.FilterGeometry = _box;

			return filter;
		}

		[NotNull]
		private static IGeometry GetErrorGeometry([NotNull] IPoint point,
		                                          [NotNull] IPoint nearPoint,
		                                          bool reportAsConnectionLine)
		{
			if (reportAsConnectionLine)
			{
				IPolyline line = GeometryFactory.CreateLine(GeometryFactory.Clone(point),
				                                            GeometryFactory.Clone(
					                                            nearPoint));
				((ITopologicalOperator) line).Simplify();

				// check if the line is too short, if so report the point only
				return line.IsEmpty
					       ? GeometryFactory.Clone(point)
					       : (IGeometry) line;
			}

			IMultipoint multipoint = GeometryFactory.CreateMultipoint(point, nearPoint);
			((ITopologicalOperator) multipoint).Simplify();

			return multipoint;
		}

		[CanBeNull]
		private IValidRelationConstraint GetValidRelationConstraint(int tableIndex)
		{
			if (_validConstraints == null || _validConstraints.Count == 0)
			{
				return null;
			}

			return _validConstraints.Count == 1
				       ? _validConstraints[0]
				       : _validConstraints[tableIndex - 1];
		}

		private GeometryComponent GetGeometryComponent(int tableIndex)
		{
			if (_geometryComponents == null || _geometryComponents.Count == 0)
			{
				return _defaultGeometryComponent;
			}

			return _geometryComponents.Count == 1
				       ? _geometryComponents[0]
				       : _geometryComponents[tableIndex - 1];
		}

		/// <summary>
		/// Returns -1 if neighbourFeature is polygon, geometryComponent is entireGeometry and point lies within neighbourFeature.
		/// Returns NaN if the geometry component of the neighbourFeature is empty, e.g. InteriorVertices of a 2-point line.
		/// Otherwise, returns the distance of the point to the neighbourFeature.
		/// </summary>
		private static double GetDistance([NotNull] IPoint point,
		                                  [NotNull] IReadOnlyFeature neighbourFeature,
		                                  [NotNull] IPoint nearestPoint,
		                                  GeometryComponent geometryComponent,
		                                  double maxNeededDistance,
		                                  out bool isWithinPolygon,
		                                  out bool onRightSide)
		{
			isWithinPolygon = false;
			if (geometryComponent == GeometryComponent.Boundary &&
			    neighbourFeature.Shape is IPolygon)
			{
				return GetDistanceToCurve(point, neighbourFeature, nearestPoint,
				                          maxNeededDistance, out onRightSide);
			}

			if (geometryComponent == GeometryComponent.Vertices)
			{
				onRightSide = false;
				return GetDistanceToVertices(point, neighbourFeature, nearestPoint,
				                             maxNeededDistance);
			}

			// other cases: create component geometry explicitly
			IGeometry neighbourGeometry =
				Assert.NotNull(GeometryComponentUtils.GetGeometryComponent(
					               neighbourFeature, geometryComponent));

			if (geometryComponent == GeometryComponent.EntireGeometry &&
			    neighbourGeometry is IPolygon)
			{
				if (((IRelationalOperator) neighbourGeometry).Disjoint(point))
				{
					// the point is outside the polygon - get the distance to the boundary
					// (faster than getting the distance to the entire polygon)
					return GetDistanceToCurve(point, neighbourFeature, nearestPoint,
					                          maxNeededDistance, out onRightSide);
				}

				// the point is inside the polygon or exactly on the boundary
				isWithinPolygon = true;
				onRightSide = false; // polygons borders are defined counter clock wise
				return -1;
			}

			if (geometryComponent == GeometryComponent.EntireGeometry &&
			    neighbourGeometry is IPolyline)
			{
				return GetDistanceToCurve(point, neighbourFeature, nearestPoint,
				                          maxNeededDistance, out onRightSide);
			}

			// DPS#248: GeometryComponent.InteriorVertices of 2-point lines the geometry is empty:
			if (neighbourGeometry.IsEmpty)
			{
				onRightSide = false;
				return double.NaN;
			}

			// Use IProximityOperator.QueryNearestPoint so that the error geometry can be constructed
			((IProximityOperator) neighbourGeometry).QueryNearestPoint(
				point, esriSegmentExtension.esriNoExtension, nearestPoint);
			onRightSide = false;
			return ((IProximityOperator) point).ReturnDistance(nearestPoint);
		}

		public static bool UseQueryPointAndDistance { get; set; }

		private static double GetDistanceToCurve(
			[NotNull] IPoint point,
			[NotNull] IReadOnlyFeature neighbourFeature,
			[NotNull] IPoint nearestPoint,
			double maxNeededDistance,
			out bool onRightSide)
		{
			var neighborCurve = (ICurve) neighbourFeature.Shape;

			if (UseQueryPointAndDistance)
			{
				const bool asRatio = false;
				double along = 0;
				double distance = 0;
				var rightSide = false;

				// TLMQA-292 (EBG - BB): most time spent here (> 90%)
				neighborCurve.QueryPointAndDistance(esriSegmentExtension.esriNoExtension,
				                                    point, asRatio, nearestPoint,
				                                    ref along, ref distance, ref rightSide);
				onRightSide = rightSide;
				return distance;
			}

			{
				double nearestDistance = maxNeededDistance * 1.01;
				bool? nearestOnRightSide = null;
				SegmentProxy nearestSegment = null;
				double nearestFraction = 0;

				double x;
				double y;
				point.QueryCoords(out x, out y);
				Pnt qaPoint = new Pnt2D(x, y);
				foreach (SegmentProxy segmentProxy in
				         EnumSegments(qaPoint, neighbourFeature, maxNeededDistance))
				{
					bool? onSegmentRightSide;
					double alongFraction;
					double offset = GetOffset(qaPoint, segmentProxy,
					                          out alongFraction, out onSegmentRightSide);
					if (offset <= nearestDistance)
					{
						if (! onSegmentRightSide.HasValue && ! nearestOnRightSide.HasValue
						                                  && nearestSegment != null)
						{
							nearestOnRightSide = GetOnRightSide(
								nearestSegment, nearestFraction, segmentProxy, alongFraction,
								neighborCurve);
						}
						else if (offset < nearestDistance)
						{
							nearestOnRightSide = onSegmentRightSide;
						}

						nearestDistance = offset;
						nearestSegment = segmentProxy;
						nearestFraction = alongFraction;
					}
				}

				if (nearestSegment != null)
				{
					double f = Math.Min(1, Math.Max(0, nearestFraction));
					IPnt p = nearestSegment.GetPointAt(f, as3D: true);
					nearestPoint.PutCoords(p.X, p.Y);
					nearestPoint.Z = p[2];

					if (! nearestOnRightSide.HasValue)
					{
						// Extend segment lineary to determine right side
						Pnt s = nearestSegment.GetStart(as3D: false);
						Pnt e = nearestSegment.GetEnd(as3D: false);

						if (nearestFraction >= 1)
						{
							nearestOnRightSide = (e - s).VectorProduct(e - qaPoint) > 0;
						}
						else if (nearestFraction <= 0)
						{
							nearestOnRightSide = (e - s).VectorProduct(s - qaPoint) > 0;
						}
					}
				}

				onRightSide = nearestOnRightSide ?? false;
				return nearestDistance;
			}
		}

		private static bool? GetOnRightSide(
			[NotNull] SegmentProxy nearestSegment, double nearestFraction,
			[NotNull] SegmentProxy segmentProxy, double alongFraction,
			[NotNull] IGeometry baseGeometry)
		{
			if (nearestSegment.PartIndex != segmentProxy.PartIndex)
			{
				return null;
			}

			SegmentProxy from;
			SegmentProxy to;
			if (nearestFraction >= 1)
			{
				if (alongFraction > 0)
				{
					return null;
				}

				ISegmentProxy next = nearestSegment.GetNextSegment(baseGeometry);
				if (next == null)
				{
					return null;
				}

				if (next.SegmentIndex != segmentProxy.SegmentIndex)
				{
					return null;
				}

				from = nearestSegment;
				to = segmentProxy;
			}
			else if (nearestFraction <= 0)
			{
				if (alongFraction < 1)
				{
					return null;
				}

				ISegmentProxy next = segmentProxy.GetNextSegment(baseGeometry);
				if (next == null)
				{
					return null;
				}

				if (next.SegmentIndex != nearestSegment.SegmentIndex)
				{
					return null;
				}

				from = segmentProxy;
				to = nearestSegment;
			}
			else
			{
				// unexpected
				return null;
			}

			Pnt vectorFrom = from.GetEnd(as3D: false) - from.GetStart(as3D: false);
			Pnt vectorTo = to.GetEnd(as3D: false) - to.GetStart(as3D: false);

			bool onRightSide = vectorFrom.VectorProduct(vectorTo) > 0;
			return onRightSide;
		}

		private static double GetDistanceToVertices([NotNull] IPoint point,
		                                            [NotNull] IReadOnlyFeature neighbourFeature,
		                                            [NotNull] IPoint nearestPoint,
		                                            double maxNeededDistance)
		{
			double minDistance2 = double.MaxValue;

			SegmentProxy nearestSegment = null;
			var isEndNearest = false;
			Pnt qaPoint = ProxyUtils.CreatePoint3D(point);

			IEnumerable<SegmentProxy> segments = EnumSegments(qaPoint, neighbourFeature,
			                                                  maxNeededDistance);
			foreach (SegmentProxy segment in segments)
			{
				if (GetNearest(qaPoint, segment.GetStart(false), ref minDistance2))
				{
					nearestSegment = segment;
					isEndNearest = false;
				}

				if (GetNearest(qaPoint, segment.GetEnd(false), ref minDistance2))
				{
					nearestSegment = segment;
					isEndNearest = true;
				}
			}

			double minDistance;
			if (nearestSegment != null)
			{
				minDistance = Math.Sqrt(minDistance2);

				if (minDistance <= maxNeededDistance)
				{
					Pnt nearest = ! isEndNearest
						              ? nearestSegment.GetStart(true)
						              : nearestSegment.GetEnd(true);
					nearestPoint.PutCoords(nearest.X, nearest.Y);
					nearestPoint.Z = nearest[2];
				}
			}
			else
			{
				minDistance = double.MaxValue;
			}

			return minDistance;
		}

		private static IEnumerable<SegmentProxy> EnumSegments(
			[NotNull] Pnt point,
			[NotNull] IReadOnlyFeature neighbourFeature,
			double maxNeededDistance)
		{
			IIndexedSegments segments = IndexedSegmentUtils.GetIndexedGeometry(
				neighbourFeature, releaseOnDispose: false);

			IBox box = GeomUtils.GetExpanded(point, maxNeededDistance);

			foreach (SegmentProxy segment in segments.GetSegments(box))
			{
				yield return segment;
			}
		}

		private static bool GetNearest([NotNull] Pnt point,
		                               [NotNull] Pnt nearPoint,
		                               ref double minDistance2)
		{
			double distance2 = point.Dist2(nearPoint);

			if (distance2 >= minDistance2)
			{
				return false;
			}

			minDistance2 = distance2;
			return true;
		}

		private static double GetOffset([NotNull] Pnt pnt,
		                                [NotNull] SegmentProxy segmentProxy,
		                                out double fraction,
		                                out bool? onRightSide)
		{
			double? offset;
			fraction = SegmentUtils_.GetClosestPointFraction(
				segmentProxy, pnt, out offset, out onRightSide, as3D: false);

			double distance2;
			if (fraction < 0)
			{
				distance2 = pnt.Dist2(segmentProxy.GetStart(as3D: false));
				onRightSide = null;
			}
			else if (fraction > 1)
			{
				distance2 = pnt.Dist2(segmentProxy.GetEnd(as3D: false));
				onRightSide = null;
			}
			else
			{
				if (! offset.HasValue)
				{
					Pnt s = segmentProxy.GetStart(as3D: false);
					Pnt e = segmentProxy.GetEnd(as3D: false);
					double vectorProd = (pnt - s).VectorProduct(s - e);
					onRightSide = vectorProd < 0;
					distance2 = pnt.Dist2(segmentProxy.GetPointAt(fraction, as3D: false));
				}
				else
				{
					distance2 = 0; // offset has value and will be used as distance
				}
			}

			double distance = offset ?? Math.Sqrt(distance2);
			return distance;
		}

		private void InitFilter()
		{
			CopyFilters(out _filter, out _helper);
			foreach (var filter in _filter)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}

			foreach (QueryFilterHelper filterHelper in _helper)
			{
				filterHelper.ForNetwork = true;
			}

			_box = new EnvelopeClass();
		}
	}
}
