using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.EdgeMatch;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[EdgeMatchTest]
	public class QaEdgeMatchBorderingPoints : ContainerTest
	{
		private readonly List<int> _pointClass1Indexes;
		private readonly int _borderClass1Index;
		private readonly List<int> _pointClass2Indexes;
		private readonly List<int> _allPointClassIndexes;
		private readonly int _borderClass2Index;
		private readonly double _searchDistance;

		private ISpatialReference _spatialReference;
		private IList<ISpatialFilter> _filters;
		private IList<QueryFilterHelper> _filterHelpers;

		private string _pointClass1BorderMatchConditionSql;
		private string _pointClass2BorderMatchConditionSql;
		private string _borderingPointMatchConditionSql;
		private string _borderingPointAttributeConstraintSql;

		private BorderMatchCondition _pointClass1BorderMatchCondition;
		private BorderMatchCondition _pointClass2BorderMatchCondition;
		private PointMatchCondition _borderingPointMatchCondition;
		private PointAttributeConstraint _borderingPointAttributeConstraint;

		private bool _isBorderingPointAttributeConstraintSymmetric =
			_defaultIsBorderingPointAttributeConstraintSymmetric;

		private const bool _defaultIsBorderingPointAttributeConstraintSymmetric = false;

		private readonly IDictionary<int, esriGeometryType> _geometryTypesByTableIndex;
		private readonly IDictionary<int, double> _xyToleranceByTableIndex;

		private readonly IEnvelope _searchEnvelopeTemplate = new EnvelopeClass();

		private IEnvelope _tileEnvelope;

		private string _borderingPointEqualAttributes;
		private EqualFieldValuesCondition _borderPointEqualFieldValuesCondition;
		private readonly HashSet<RowPair> _reportedRowPairs = new HashSet<RowPair>();

		[CanBeNull] private static TestIssueCodes _codes;
		private IList<string> _borderingPointEqualAttributeOptions;

		private const double _defaultCoincidenceTolerance = 0; // 0 --> exact match required

		private const bool _defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident =
			false;

		private const bool _defaultAllowNoFeatureWithinSearchDistance = false;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled = false;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoMatch_CandidateExists_ConstraintsFulfilled =
				"NoMatch.CandidateExists.ConstraintsFulfilled";

			public const string
				NoMatch_CandidateExists_BordersNotCoincident_ConstraintsFulfilled
					= "NoMatch.CandidateExists.BordersNotCoincident+ConstraintsFulfilled";

			public const string NoMatch_CandidateExists_ConstraintsNotFulfilled =
				"NoMatch.CandidateExists.ConstraintsNotFulfilled";

			public const string
				NoMatch_CandidateExists_BordersNotCoincident_ConstraintsNotFulfilled
					= "NoMatch.CandidateExists.BordersNotCoincident+ConstraintsNotFulfilled";

			public const string NoMatch_NoCandidate = "NoMatch.NoCandidate";

			public const string Match_ConstraintsNotFulfilled =
				"Match.ConstraintsNotFulfilled";

			public Code() : base("BorderingPoints") { }
		}

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_0))]
		public QaEdgeMatchBorderingPoints(
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_pointClass1))] [NotNull]
			IFeatureClass pointClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_borderClass1))] [NotNull]
			IFeatureClass
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_pointClass2))] [NotNull]
			IFeatureClass pointClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_borderClass2))] [NotNull]
			IFeatureClass
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_searchDistance))]
			double searchDistance)
			: this(new[] {pointClass1}, borderClass1,
			       new[] {pointClass2}, borderClass2,
			       searchDistance) { }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_1))]
		public QaEdgeMatchBorderingPoints(
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_pointClasses1))] [NotNull]
			IList<IFeatureClass>
				pointClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_borderClass1))] [NotNull]
			IFeatureClass
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_pointClasses2))] [NotNull]
			IList<IFeatureClass>
				pointClasses2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_borderClass2))] [NotNull]
			IFeatureClass
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_searchDistance))]
			double searchDistance)
			: base(CastToTables(pointClasses1, new[] {borderClass1},
			                    pointClasses2, new[] {borderClass2}))
		{
			Assert.ArgumentNotNull(pointClasses1, nameof(pointClasses1));
			Assert.ArgumentNotNull(borderClass1, nameof(borderClass1));
			Assert.ArgumentNotNull(pointClasses2, nameof(pointClasses2));
			Assert.ArgumentNotNull(borderClass2, nameof(borderClass2));

			SearchDistance = searchDistance;

			foreach (IFeatureClass pointClass in Union(pointClasses1, pointClasses2))
			{
				Assert.ArgumentCondition(
					pointClass.ShapeType == esriGeometryType.esriGeometryPoint,
					$"Point feature class expected: {DatasetUtils.GetName(pointClass)}");
			}

			foreach (IFeatureClass borderClass in new[] {borderClass1, borderClass2})
			{
				Assert.ArgumentCondition(
					borderClass.ShapeType == esriGeometryType.esriGeometryPolyline ||
					borderClass.ShapeType == esriGeometryType.esriGeometryPolygon,
					$"Polyline or polygon feature class expected: {DatasetUtils.GetName(borderClass)}");
			}

			_searchDistance = searchDistance;

			_pointClass1Indexes = new List<int>(pointClasses1.Count);
			for (var i = 0; i < pointClasses1.Count; i++)
			{
				_pointClass1Indexes.Add(i);
			}

			_borderClass1Index = pointClasses1.Count;

			_pointClass2Indexes = new List<int>(pointClasses2.Count);
			for (var i = 0; i < pointClasses2.Count; i++)
			{
				_pointClass2Indexes.Add(_borderClass1Index + 1 + i);
			}

			_allPointClassIndexes = new List<int>();
			_allPointClassIndexes.AddRange(_pointClass1Indexes);
			_allPointClassIndexes.AddRange(_pointClass2Indexes);

			_borderClass2Index = _borderClass1Index + pointClasses2.Count + 1;

			_geometryTypesByTableIndex = GetGeometryTypesByTableIndex(InvolvedTables);
			_xyToleranceByTableIndex = GetXyToleranceByTableIndex(InvolvedTables);

			// defaults
			AllowDisjointCandidateFeatureIfBordersAreNotCoincident =
				_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident;
			AllowNoFeatureWithinSearchDistance = _defaultAllowNoFeatureWithinSearchDistance;
			AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled =
				_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled;
		}

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_PointClass1BorderMatchCondition))]
		[TestParameter]
		public string PointClass1BorderMatchCondition
		{
			get { return _pointClass1BorderMatchConditionSql; }
			set
			{
				_pointClass1BorderMatchConditionSql = value;
				_pointClass1BorderMatchCondition = null;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_PointClass2BorderMatchCondition))]
		[TestParameter]
		public string PointClass2BorderMatchCondition
		{
			get { return _pointClass2BorderMatchConditionSql; }
			set
			{
				_pointClass2BorderMatchConditionSql = value;
				_pointClass2BorderMatchCondition = null;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_BorderingPointMatchCondition))]
		[TestParameter]
		public string BorderingPointMatchCondition
		{
			get { return _borderingPointMatchConditionSql; }
			set
			{
				_borderingPointMatchConditionSql = value;
				_borderingPointMatchCondition = null;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_BorderingPointAttributeConstraint))]
		[TestParameter]
		public string BorderingPointAttributeConstraint
		{
			get { return _borderingPointAttributeConstraintSql; }
			set
			{
				_borderingPointAttributeConstraintSql = value;
				_borderingPointAttributeConstraint = null;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_IsBorderingPointAttributeConstraintSymmetric))]
		[TestParameter(_defaultIsBorderingPointAttributeConstraintSymmetric)]
		public bool IsBorderingPointAttributeConstraintSymmetric
		{
			get { return _isBorderingPointAttributeConstraintSymmetric; }
			set
			{
				_isBorderingPointAttributeConstraintSymmetric = value;
				_borderingPointAttributeConstraint = null;
			}
		}

		// NOTE blank is not supported as field separator (as it may be used as multi-value separator)
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_BorderPointEqualAttributes))]
		[TestParameter]
		public string BorderingPointEqualAttributes
		{
			get { return _borderingPointEqualAttributes; }
			set
			{
				_borderingPointEqualAttributes = value;
				_borderPointEqualFieldValuesCondition = null;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_BorderingPointEqualAttributeOptions))]
		[TestParameter]
		public IList<string> BorderingPointEqualAttributeOptions
		{
			get { return _borderingPointEqualAttributeOptions; }
			set
			{
				_borderingPointEqualAttributeOptions = value;
				_borderPointEqualFieldValuesCondition = null;
			}
		}

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_ReportIndividualAttributeConstraintViolations))]
		public bool ReportIndividualAttributeConstraintViolations { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_CoincidenceTolerance))]
		[TestParameter(_defaultCoincidenceTolerance)]
		public double CoincidenceTolerance { get; set; }

		[Doc(nameof(DocStrings.
				QaEdgeMatchBorderingPoints_AllowDisjointCandidateFeatureIfBordersAreNotCoincident
			))]
		[TestParameter(_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident)]
		public bool AllowDisjointCandidateFeatureIfBordersAreNotCoincident { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchBorderingPoints_AllowNoFeatureWithinSearchDistance))]
		[TestParameter(_defaultAllowNoFeatureWithinSearchDistance)]
		public bool AllowNoFeatureWithinSearchDistance { get; set; }

		[Doc(nameof(DocStrings.
			QaEdgeMatchBorderingPoints_AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled
		))]
		[TestParameter(
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)]
		public bool AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled { get; set; }

		protected override void BeginTileCore(BeginTileParameters parameters)
		{
			_tileEnvelope = parameters.TileEnvelope;
			_reportedRowPairs.Clear();
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			var feature = row as IFeature;
			if (feature == null)
			{
				return NoError;
			}

			if (_filters == null)
			{
				InitFilters();
			}

			if (_spatialReference == null)
			{
				_spatialReference = GetSpatialReference();
			}

			int borderClassIndex;
			int neighborBorderClassIndex;
			ICollection<int> sameSidePointClassIndexes;
			IEnumerable<int> neighborPointClassIndexes = GetNeighborPointClassIndexes(
				tableIndex,
				out borderClassIndex,
				out neighborBorderClassIndex,
				out sameSidePointClassIndexes);

			if (neighborPointClassIndexes == null)
			{
				return NoError;
			}

			Assert.True(borderClassIndex >= 0, "Unexpected border class index");
			Assert.True(neighborBorderClassIndex >= 0,
			            "Unexpected neighboring border class index");

			return CheckBorderPoint(feature, tableIndex, borderClassIndex,
			                        neighborPointClassIndexes, neighborBorderClassIndex);
		}

		private int CheckBorderPoint([NotNull] IFeature pointFeature, int pointClassIndex,
		                             int borderClassIndex,
		                             [NotNull] IEnumerable<int> neighborPointClassIndexes,
		                             int neighborBorderClassIndex)
		{
			// determine if the feature ends on the border 
			var point = (IPoint) pointFeature.Shape;
			if (point.IsEmpty)
			{
				return NoError;
			}

			if (IsOutsideCurrentTile(point))
			{
				return NoError;
			}

			EnsureBorderingLineMatchCondition();
			EnsureBorderingLineAttributeConstraint();
			EnsureBorderingLineEqualFieldValuesCondition();

			BorderConnection borderConnection = GetBorderConnection(point, pointFeature,
			                                                        pointClassIndex,
			                                                        borderClassIndex);

			if (borderConnection == null)
			{
				// point does not touch border
				return NoError;
			}

			return CheckBorderConnection(borderConnection,
			                             neighborPointClassIndexes, neighborBorderClassIndex,
			                             _borderingPointMatchCondition);
		}

		private int CheckBorderConnection(
			[NotNull] BorderConnection borderConnection,
			[NotNull] IEnumerable<int> neighborPointClassIndexes,
			int neighborBorderClassIndex,
			[NotNull] PointMatchCondition borderingPointMatchCondition)
		{
			double coincidenceTolerance = GetCoincidenceTolerance(borderConnection.ClassIndex);

			var anyNeighborFound = false;

			var errorCount = 0;

			foreach (int neighborPointClassIndex in neighborPointClassIndexes)
			{
				foreach (IRow neighborRow in SearchNeighborRows(borderConnection.Point,
				                                                neighborPointClassIndex))
				{
					if (WasReportedInOppositeDirection(borderConnection.Feature,
					                                   borderConnection.ClassIndex,
					                                   neighborRow,
					                                   neighborPointClassIndex))
					{
						return NoError;
					}

					if (! borderingPointMatchCondition.IsFulfilled(borderConnection.Feature,
					                                               borderConnection.ClassIndex,
					                                               neighborRow,
					                                               neighborPointClassIndex))
					{
						continue;
					}

					var neighborFeature = (IFeature) neighborRow;
					var neighborPoint = (IPoint) neighborFeature.Shape;

					double pointDistance = GeometryUtils.GetPointDistance(borderConnection.Point,
					                                                      neighborPoint);

					if (pointDistance > _searchDistance)
					{
						// the point is outside the search distance
						continue;
					}

					// determine if the neighbor point is connected to it's border
					BorderConnection neighborBorderConnection = GetBorderConnection(neighborPoint,
					                                                                neighborFeature,
					                                                                neighborPointClassIndex,
					                                                                neighborBorderClassIndex);

					if (neighborBorderConnection == null)
					{
						// there is no neighboring point within the search distance on its border
						continue;
					}

					anyNeighborFound = true;

					ICollection<AttributeConstraintViolation> constraintViolations =
						GetAttributeConstraintViolations(borderConnection,
						                                 neighborFeature,
						                                 neighborPointClassIndex);

					if (EdgeMatchUtils.IsWithinTolerance(pointDistance, coincidenceTolerance))
					{
						// the points are coincident
						if (constraintViolations.Count == 0)
						{
							return NoError;
						}

						AddReportedRowPair(borderConnection.Feature,
						                   borderConnection.ClassIndex,
						                   neighborFeature,
						                   neighborPointClassIndex);

						foreach (AttributeConstraintViolation constraintViolation in
							constraintViolations)
						{
							errorCount += ReportError(
								constraintViolation.Description,
								GeometryFactory.Clone(borderConnection.Point),
								Codes[Code.Match_ConstraintsNotFulfilled],
								constraintViolation.AffectedComponents,
								new[] {constraintViolation.TextValue},
								borderConnection.Feature, neighborFeature);
						}

						return errorCount;
					}

					// the point is not coincident with the neighbor point
					bool areBordersCoincident = AreBordersCoincident(
						borderConnection, neighborBorderConnection);

					AddReportedRowPair(borderConnection.Feature,
					                   borderConnection.ClassIndex,
					                   neighborBorderConnection.Feature,
					                   neighborBorderConnection.ClassIndex);

					if (! areBordersCoincident &&
					    AllowDisjointCandidateFeatureIfBordersAreNotCoincident)
					{
						continue;
					}

					if (constraintViolations.Count == 0 &&
					    AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)
					{
						continue;
					}

					errorCount += ReportDisjointNeighborPointErrors(
						constraintViolations,
						borderConnection,
						neighborBorderConnection,
						areBordersCoincident,
						pointDistance);
				}
			}

			if (! anyNeighborFound)
			{
				if (! AllowNoFeatureWithinSearchDistance)
				{
					errorCount += ReportError(
						LocalizableStrings.QaEdgeMatchBorderingPoints_NoMatch_NoCandidate,
						GeometryFactory.Clone(borderConnection.Point),
						Codes[Code.NoMatch_NoCandidate], null,
						borderConnection.Feature);
				}
			}

			return errorCount;
		}

		private int ReportDisjointNeighborPointErrors(
			[NotNull] ICollection<AttributeConstraintViolation> constraintViolations,
			[NotNull] BorderConnection borderConnection,
			[NotNull] BorderConnection neighborBorderConnection,
			bool areBordersCoincident,
			double pointDistance)
		{
			IMultipoint errorGeometry = GeometryFactory.CreateMultipoint(
				GeometryFactory.Clone(borderConnection.Point),
				GeometryFactory.Clone(neighborBorderConnection.Point));

			string baseDescription = string.Format(
				LocalizableStrings.QaEdgeMatchBorderingPoints_NoMatch_CandidateExists,
				FormatLength(pointDistance, _spatialReference).Trim());

			IssueCode issueCode;
			string description;
			if (constraintViolations.Count == 0)
			{
				if (areBordersCoincident)
				{
					issueCode = Codes[Code.NoMatch_CandidateExists_ConstraintsFulfilled];
					description = baseDescription;
				}
				else
				{
					issueCode =
						Codes[
							Code.NoMatch_CandidateExists_BordersNotCoincident_ConstraintsFulfilled];
					description =
						$"{baseDescription} {LocalizableStrings.QaEdgeMatchBorderingPoints_BordersNotCoincident}";
				}

				return ReportError(description, errorGeometry,
				                   issueCode, null,
				                   borderConnection.Feature,
				                   neighborBorderConnection.Feature);
			}

			var errorCount = 0;
			foreach (AttributeConstraintViolation constraintViolation in constraintViolations)
			{
				if (areBordersCoincident)
				{
					issueCode = Codes[Code.NoMatch_CandidateExists_ConstraintsNotFulfilled];
					description = $"{baseDescription} {constraintViolation.Description}.";
				}
				else
				{
					issueCode =
						Codes[
							Code
								.NoMatch_CandidateExists_BordersNotCoincident_ConstraintsNotFulfilled];

					// description has final period
					description =
						$"{baseDescription} {LocalizableStrings.QaEdgeMatchBorderingPoints_BordersNotCoincident} {constraintViolation.Description}.";
				}

				errorCount += ReportError(description, errorGeometry,
				                          issueCode, constraintViolation.AffectedComponents,
				                          new[] {constraintViolation.TextValue},
				                          borderConnection.Feature,
				                          neighborBorderConnection.Feature);
			}

			return errorCount;
		}

		[NotNull]
		private ICollection<AttributeConstraintViolation> GetAttributeConstraintViolations(
			[NotNull] BorderConnection borderConnection,
			[NotNull] IFeature neighborFeature,
			int neighborClassIndex)
		{
			return EdgeMatchUtils.GetAttributeConstraintViolations(
				borderConnection.Feature, borderConnection.ClassIndex,
				neighborFeature, neighborClassIndex,
				_borderingPointAttributeConstraint,
				_borderPointEqualFieldValuesCondition,
				ReportIndividualAttributeConstraintViolations).ToList();
		}

		private double GetCoincidenceTolerance(int classIndex)
		{
			return CoincidenceTolerance < 0
				       ? _xyToleranceByTableIndex[classIndex]
				       : CoincidenceTolerance;
		}

		private bool AreBordersCoincident(
			[NotNull] BorderConnection borderConnection,
			[NotNull] BorderConnection neighborBorderConnection)
		{
			return IsCoincidentWithNeighborBorder(borderConnection,
			                                      neighborBorderConnection.Feature,
			                                      neighborBorderConnection.ClassIndex,
			                                      neighborBorderConnection.BorderClassIndex) &&
			       IsCoincidentWithNeighborBorder(neighborBorderConnection,
			                                      borderConnection.Feature,
			                                      borderConnection.ClassIndex,
			                                      borderConnection.BorderClassIndex);
		}

		private bool IsCoincidentWithNeighborBorder(
			[NotNull] BorderConnection borderConnection,
			[NotNull] IFeature neighborFeature, int neighborLineClassIndex,
			int neighborBorderClassIndex)
		{
			ITable neighborBorderClass = InvolvedTables[neighborBorderClassIndex];

			ISpatialFilter spatialFilter = _filters[neighborBorderClassIndex];
			spatialFilter.Geometry = borderConnection.Point;

			BorderMatchCondition neighborBorderMatchCondition =
				GetBorderMatchCondition(neighborLineClassIndex);

			foreach (IRow borderRow in Search(neighborBorderClass,
			                                  spatialFilter,
			                                  _filterHelpers[neighborBorderClassIndex]))
			{
				if (neighborBorderMatchCondition.IsFulfilled(neighborFeature,
				                                             neighborLineClassIndex,
				                                             borderRow,
				                                             neighborBorderClassIndex))
				{
					return true;
				}
			}

			return false;
		}

		private void AddReportedRowPair([NotNull] IRow row, int tableIndex,
		                                [NotNull] IRow neighborRow, int neighborTableIndex)
		{
			_reportedRowPairs.Add(new RowPair(tableIndex, row.OID,
			                                  neighborTableIndex, neighborRow.OID));
		}

		private bool WasReportedInOppositeDirection([NotNull] IRow row, int tableIndex,
		                                            [NotNull] IRow neighborRow,
		                                            int neighborTableIndex)
		{
			return _reportedRowPairs.Contains(new RowPair(neighborTableIndex, neighborRow.OID,
			                                              tableIndex, row.OID));
		}

		[NotNull]
		private IEnumerable<IRow> SearchNeighborRows([NotNull] IPoint borderConnection,
		                                             int neighborPointClassIndex)
		{
			ISpatialFilter spatialFilter = GetSearchFilter(neighborPointClassIndex,
			                                               borderConnection,
			                                               _searchDistance);

			return Search(InvolvedTables[neighborPointClassIndex],
			              spatialFilter,
			              _filterHelpers[neighborPointClassIndex],
			              borderConnection);
		}

		[NotNull]
		private ISpatialFilter GetSearchFilter(int tableIndex,
		                                       [NotNull] IPoint point,
		                                       double searchDistance)
		{
			ISpatialFilter filter = _filters[tableIndex];

			double x;
			double y;
			point.QueryCoords(out x, out y);

			_searchEnvelopeTemplate.PutCoords(x - searchDistance, y - searchDistance,
			                                  x + searchDistance, y + searchDistance);

			filter.Geometry = _searchEnvelopeTemplate;

			return filter;
		}

		[CanBeNull]
		private BorderConnection GetBorderConnection([NotNull] IPoint point,
		                                             [NotNull] IFeature pointFeature,
		                                             int pointClassIndex,
		                                             int borderClassIndex)
		{
			ICollection<IFeature> borderFeatures = GetConnectedBorderFeatures(point,
			                                                                  pointFeature,
			                                                                  pointClassIndex,
			                                                                  borderClassIndex);

			return borderFeatures.Count <= 0
				       ? null
				       : new BorderConnection(pointFeature, point, pointClassIndex,
				                              borderFeatures, borderClassIndex);
		}

		[NotNull]
		private ICollection<IFeature> GetConnectedBorderFeatures(
			[NotNull] IPoint point,
			[NotNull] IFeature lineFeature, int lineClassIndex,
			int borderClassIndex)
		{
			ITable borderClass = InvolvedTables[borderClassIndex];

			ISpatialFilter spatialFilter = _filters[borderClassIndex];
			spatialFilter.Geometry = point;

			var result = new List<IFeature>(5);

			BorderMatchCondition borderMatchCondition =
				GetBorderMatchCondition(lineClassIndex);

			foreach (IRow borderRow in Search(borderClass,
			                                  spatialFilter,
			                                  _filterHelpers[borderClassIndex]))
			{
				if (! borderMatchCondition.IsFulfilled(lineFeature, lineClassIndex,
				                                       borderRow, borderClassIndex))
				{
					continue;
				}

				result.Add((IFeature) borderRow);
			}

			return result;
		}

		[NotNull]
		private BorderMatchCondition GetBorderMatchCondition(int pointClassIndex)
		{
			if (IsPointClass1(pointClassIndex))
			{
				return _pointClass1BorderMatchCondition ??
				       (_pointClass1BorderMatchCondition =
					        new BorderMatchCondition(_pointClass1BorderMatchConditionSql,
					                                 GetSqlCaseSensitivity(pointClassIndex,
					                                                       _borderClass1Index)));
			}

			if (IsPointClass2(pointClassIndex))
			{
				return _pointClass2BorderMatchCondition ??
				       (_pointClass2BorderMatchCondition =
					        new BorderMatchCondition(_pointClass2BorderMatchConditionSql,
					                                 GetSqlCaseSensitivity(pointClassIndex,
					                                                       _borderClass2Index)));
			}

			throw new ArgumentException("Not a line class index");
		}

		private bool IsOutsideCurrentTile([NotNull] IPoint point)
		{
			return _tileEnvelope != null &&
			       ((IRelationalOperator) _tileEnvelope).Disjoint(point);
		}

		private void EnsureBorderingLineEqualFieldValuesCondition()
		{
			if (_borderPointEqualFieldValuesCondition != null)
			{
				return;
			}

			_borderPointEqualFieldValuesCondition =
				new EqualFieldValuesCondition(_borderingPointEqualAttributes,
				                              _borderingPointEqualAttributeOptions,
				                              GetTables(_allPointClassIndexes),
				                              GetSqlCaseSensitivity(_allPointClassIndexes));
		}

		private void EnsureBorderingLineAttributeConstraint()
		{
			if (_borderingPointAttributeConstraint != null)
			{
				return;
			}

			_borderingPointAttributeConstraint = new PointAttributeConstraint(
				_borderingPointAttributeConstraintSql,
				GetSqlCaseSensitivity(_allPointClassIndexes),
				! IsBorderingPointAttributeConstraintSymmetric);
		}

		private void EnsureBorderingLineMatchCondition()
		{
			if (_borderingPointMatchCondition != null)
			{
				return;
			}

			_borderingPointMatchCondition =
				new PointMatchCondition(_borderingPointMatchConditionSql,
				                        GetSqlCaseSensitivity(_allPointClassIndexes));
		}

		[NotNull]
		private IEnumerable<ITable> GetTables([NotNull] IEnumerable<int> tableIndexes)
		{
			return tableIndexes.Distinct().Select(tableIndex => InvolvedTables[tableIndex]);
		}

		[CanBeNull]
		private IEnumerable<int> GetNeighborPointClassIndexes(
			int tableIndex,
			out int borderLineClassIndex,
			out int neighborBorderLineClassIndex,
			out ICollection<int> sameSideLineClassIndexes)
		{
			if (IsPointClass1(tableIndex))
			{
				borderLineClassIndex = _borderClass1Index;
				neighborBorderLineClassIndex = _borderClass2Index;
				sameSideLineClassIndexes = _pointClass1Indexes;
				return _pointClass2Indexes;
			}

			if (IsPointClass2(tableIndex))
			{
				borderLineClassIndex = _borderClass2Index;
				neighborBorderLineClassIndex = _borderClass1Index;
				sameSideLineClassIndexes = _pointClass2Indexes;
				return _pointClass1Indexes;
			}

			// it's one of the border line classes --> ignore
			borderLineClassIndex = -1;
			neighborBorderLineClassIndex = -1;
			sameSideLineClassIndexes = null;
			return null;
		}

		private bool IsPointClass1(int tableIndex)
		{
			return tableIndex < _borderClass1Index;
		}

		private bool IsPointClass2(int tableIndex)
		{
			return tableIndex > _borderClass1Index && tableIndex < _borderClass2Index;
		}

		private void InitFilters()
		{
			CopyFilters(out _filters, out _filterHelpers);

			foreach (ISpatialFilter filter in _filters)
			{
				filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
			}

			_filters[_borderClass1Index].SpatialRel = GetBorderClassSpatialRelation(
				_geometryTypesByTableIndex[_borderClass1Index]);

			_filters[_borderClass2Index].SpatialRel = GetBorderClassSpatialRelation(
				_geometryTypesByTableIndex[_borderClass2Index]);
		}

		private static esriSpatialRelEnum GetBorderClassSpatialRelation(
			esriGeometryType borderGeometryType)
		{
			switch (borderGeometryType)
			{
				case esriGeometryType.esriGeometryPolyline:
					return esriSpatialRelEnum.esriSpatialRelIntersects;

				case esriGeometryType.esriGeometryPolygon:
					return esriSpatialRelEnum.esriSpatialRelTouches;

				default:
					throw new ArgumentException(
						$"Unexpected border geometry type: {borderGeometryType}");
			}
		}

		// TODO use from TestUtils after pullsubtrees
		[NotNull]
		private static IDictionary<int, double> GetXyToleranceByTableIndex(
			[NotNull] ICollection<ITable> involvedTables)
		{
			var result = new Dictionary<int, double>(involvedTables.Count);

			var index = 0;
			foreach (ITable table in involvedTables)
			{
				var featureClass = table as IFeatureClass;

				double xyTolerance;
				if (featureClass == null ||
				    ! DatasetUtils.TryGetXyTolerance(featureClass, out xyTolerance))
				{
					xyTolerance = 0;
				}

				result.Add(index, xyTolerance);
				index++;
			}

			return result;
		}

		// TODO use from TestUtils after pullsubtrees
		[NotNull]
		private static IDictionary<int, esriGeometryType> GetGeometryTypesByTableIndex(
			[NotNull] ICollection<ITable> involvedTables)
		{
			var result = new Dictionary<int, esriGeometryType>(involvedTables.Count);

			var index = 0;
			foreach (ITable table in involvedTables)
			{
				result.Add(index, ((IFeatureClass) table).ShapeType);
				index++;
			}

			return result;
		}

		private class RowPair : IEquatable<RowPair>
		{
			private readonly int _tableIndex1;
			private readonly int _oid1;
			private readonly int _tableIndex2;
			private readonly int _oid2;

			public RowPair(int tableIndex1, int oid1, int tableIndex2, int oid2)
			{
				_tableIndex1 = tableIndex1;
				_oid1 = oid1;
				_tableIndex2 = tableIndex2;
				_oid2 = oid2;
			}

			public bool Equals(RowPair other)
			{
				if (ReferenceEquals(null, other))
				{
					return false;
				}

				if (ReferenceEquals(this, other))
				{
					return true;
				}

				return _oid1 == other._oid1 && _tableIndex1 == other._tableIndex1 &&
				       _oid2 == other._oid2 && _tableIndex2 == other._tableIndex2;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
				{
					return false;
				}

				if (ReferenceEquals(this, obj))
				{
					return true;
				}

				if (obj.GetType() != GetType())
				{
					return false;
				}

				return Equals((RowPair) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = _tableIndex1;
					hashCode = (hashCode * 397) ^ _oid1;
					hashCode = (hashCode * 397) ^ _tableIndex2;
					hashCode = (hashCode * 397) ^ _oid2;
					return hashCode;
				}
			}
		}

		private class BorderConnection : EdgeMatchBorderConnection
		{
			public BorderConnection([NotNull] IFeature feature,
			                        [NotNull] IPoint point,
			                        int classIndex,
			                        [NotNull] ICollection<IFeature> borderFeatures,
			                        int borderClassIndex)
				: base(feature, classIndex, borderClassIndex)
			{
				Assert.ArgumentNotNull(borderFeatures, nameof(borderFeatures));

				Point = point;
			}

			[NotNull]
			public IPoint Point { get; }
		}

		private class BorderMatchCondition : RowPairCondition
		{
			public BorderMatchCondition([CanBeNull] string condition, bool caseSensitive)
				: base(condition, true, true, "POINT", "BORDER", caseSensitive) { }
		}

		private class PointMatchCondition : RowPairCondition
		{
			public PointMatchCondition([CanBeNull] string condition, bool caseSensitive)
				: base(condition, true, true, "POINT1", "POINT2", caseSensitive) { }
		}

		private class PointAttributeConstraint : RowPairCondition
		{
			public PointAttributeConstraint([CanBeNull] string condition, bool caseSensitive,
			                                bool isDirected)
				: base(condition, isDirected, true, "POINT1", "POINT2",
				       caseSensitive, conciseMessage: true) { }
		}
	}
}