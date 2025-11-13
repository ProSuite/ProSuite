using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.EdgeMatch;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[EdgeMatchTest]
	public class QaEdgeMatchCrossingLines : ContainerTest
	{
		private readonly List<int> _lineClass1Indexes;
		private readonly int _borderClass1Index;
		private readonly List<int> _lineClass2Indexes;
		private readonly List<int> _allLineClassIndexes;
		private readonly int _borderClass2Index;
		private readonly double _searchDistance;
		private double _minimumErrorConnectionLineLength;
		private double _maximumEndPointConnectionDistance;

		private ISpatialReference _spatialReference;
		private IList<IFeatureClassFilter> _filters;
		private IList<QueryFilterHelper> _filterHelpers;
		private readonly IDictionary<int, esriGeometryType> _geometryTypesByTableIndex;
		private readonly IDictionary<int, double> _xyToleranceByTableIndex;
		private readonly PointPool _pointPool = new PointPool();

		private readonly IEnvelope _searchEnvelopeTemplate = new EnvelopeClass();
		private readonly IPoint _pointTemplate = new PointClass();

		private IEnvelope _tileEnvelope;

		private string _lineClass1BorderMatchConditionSql;
		private string _lineClass2BorderMatchConditionSql;
		private string _crossingLineMatchConditionSql;
		private string _crossingLineAttributeConstraintSql;

		private BorderMatchCondition _lineClass1BorderMatchCondition;
		private BorderMatchCondition _lineClass2BorderMatchCondition;
		private LineMatchCondition _crossingLineMatchCondition;
		private LineAttributeConstraint _crossingLineAttributeConstraint;

		private bool _isCrossingLineAttributeConstraintSymmetric =
			_defaultIsCrossingLineAttributeConstraintSymmetric;

		private const double _defaultCoincidenceTolerance = 0;
		// 0 --> exact match required

		private const bool _defaultIsCrossingLineAttributeConstraintSymmetric = false;
		private const bool _defaultAllowNoFeatureWithinSearchDistance = false;

		private const bool _defaultIgnoreAttributesConstraintsIfThreeOrMoreConnected =
			false;

		private const bool
			_defaultAllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide
				= true;

		private const bool
			_defaultIgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance =
				true;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident = false;

		private const bool
			_defaultAllowEndPointsConnectingToInteriorOfValidNeighborLine =
				false;

		private const bool _defaultIgnoreEndPointsOfBorderingLines = true;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled =
				false;

		private string _crossingLineEqualAttributes;
		private EqualFieldValuesCondition _crossingLineEqualFieldValuesCondition;

		private readonly HashSet<FeaturePointPair> _reportedPointPairs =
			new HashSet<FeaturePointPair>();

		private IList<string> _crossingLineEqualAttributeOptions;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoMatch_NoCandidate = "NoMatch.NoCandidate";

			public const string NoMatch_NoCandidate_ConnectedOnSameSide =
				"NoMatch.NoCandidate.ConnectedOnSameSide";

			public const string NoMatch_CandidateExists_ConstraintsFulfilled =
				"NoMatch.CandidateExists.ConstraintsFulfilled";

			// +
			public const string
				NoMatch_CandidateExists_BordersNotCoincident_ConstraintsFulfilled =
					"NoMatch.CandidateExists.BordersNotCoincident+ConstraintsFulfilled";

			public const string NoMatch_CandidateExists_ConstraintsNotFulfilled =
				"NoMatch.CandidateExists.ConstraintsNotFulfilled";

			public const string
				NoMatch_CandidateExists_BordersNotCoincident_ConstraintsNotFulfilled
					=
					"NoMatch.CandidateExists.BordersNotCoincident+ConstraintsNotFulfilled";

			// +
			public const string
				NoMatch_CandidateExists_EndPointOutsideTolerance_ConstraintsFulfilled
					=
					"NoMatch.CandidateExists.EndPointOutsideTolerance+ConstraintsFulfilled";

			// +
			public const string
				NoMatch_CandidateExists_EndPointOutsideTolerance_ConstraintsNotFulfilled
					=
					"NoMatch.CandidateExists.EndPointOutsideTolerance+ConstraintsNotFulfilled";

			// +
			public const string
				NoMatch_CandidateExists_EndPointOutsideTolerance_BordersNotCoincident_ConstraintsFulfilled
					=
					"NoMatch.CandidateExists.EndPointOutsideTolerance+BordersNotCoincident+ConstraintsFulfilled";

			// +
			public const string
				NoMatch_CandidateExists_EndPointOutsideTolerance_BordersNotCoincident_ConstraintsNotFulfilled
					=
					"NoMatch.CandidateExists.EndPointOutsideTolerance+BordersNotCoincident+ConstraintsNotFulfilled";

			public const string Match_ConstraintsNotFulfilled =
				"Match.ConstraintsNotFulfilled";

			public Code() : base("CrossingLines") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_0))]
		public QaEdgeMatchCrossingLines(
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_lineClass1))] [NotNull]
			IReadOnlyFeatureClass
				lineClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_borderClass1))] [NotNull]
			IReadOnlyFeatureClass
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_lineClass2))] [NotNull]
			IReadOnlyFeatureClass
				lineClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_borderClass2))] [NotNull]
			IReadOnlyFeatureClass
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_searchDistance))]
			double
				searchDistance)
			: this(new[] { lineClass1 }, borderClass1,
			       new[] { lineClass2 }, borderClass2,
			       searchDistance) { }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_1))]
		public QaEdgeMatchCrossingLines(
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_lineClasses1))] [NotNull]
			IList<IReadOnlyFeatureClass>
				lineClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_borderClass1))] [NotNull]
			IReadOnlyFeatureClass
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_lineClasses2))] [NotNull]
			IList<IReadOnlyFeatureClass>
				lineClasses2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_borderClass2))] [NotNull]
			IReadOnlyFeatureClass
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_searchDistance))]
			double
				searchDistance)
			: base(CastToTables(lineClasses1, new[] { borderClass1 },
			                    lineClasses2, new[] { borderClass2 }))
		{
			Assert.ArgumentNotNull(lineClasses1, nameof(lineClasses1));
			Assert.ArgumentNotNull(borderClass1, nameof(borderClass1));
			Assert.ArgumentNotNull(lineClasses2, nameof(lineClasses2));
			Assert.ArgumentNotNull(borderClass2, nameof(borderClass2));

			SearchDistance = searchDistance;

			foreach (IReadOnlyFeatureClass lineClass in Union(lineClasses1, lineClasses2))
			{
				Assert.ArgumentCondition(
					lineClass.ShapeType == esriGeometryType.esriGeometryPolyline,
					string.Format("Polyline feature class expected: {0}",
					              lineClass.Name));
			}

			foreach (IReadOnlyFeatureClass borderClass in new[] { borderClass1, borderClass2 })
			{
				Assert.ArgumentCondition(
					borderClass.ShapeType == esriGeometryType.esriGeometryPolyline ||
					borderClass.ShapeType == esriGeometryType.esriGeometryPolygon,
					string.Format("Polyline or polygon feature class expected: {0}",
					              borderClass.Name));
			}

			_searchDistance = searchDistance;

			_lineClass1Indexes = new List<int>(lineClasses1.Count);
			for (var i = 0; i < lineClasses1.Count; i++)
			{
				_lineClass1Indexes.Add(i);
			}

			_borderClass1Index = lineClasses1.Count;

			_lineClass2Indexes = new List<int>(lineClasses2.Count);
			for (var i = 0; i < lineClasses2.Count; i++)
			{
				_lineClass2Indexes.Add(_borderClass1Index + 1 + i);
			}

			_allLineClassIndexes = new List<int>();
			_allLineClassIndexes.AddRange(_lineClass1Indexes);
			_allLineClassIndexes.AddRange(_lineClass2Indexes);

			_borderClass2Index = _borderClass1Index + lineClasses2.Count + 1;

			_geometryTypesByTableIndex =
				TestUtils.GetGeometryTypesByTableIndex(InvolvedTables);
			_xyToleranceByTableIndex =
				TestUtils.GetXyToleranceByTableIndex(InvolvedTables);

			// defaults
			CoincidenceTolerance = _defaultCoincidenceTolerance;
			AllowNoFeatureWithinSearchDistance =
				_defaultAllowNoFeatureWithinSearchDistance;
			AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide =
				_defaultAllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide;
			IgnoreAttributeConstraintsIfThreeOrMoreConnected =
				_defaultIgnoreAttributesConstraintsIfThreeOrMoreConnected;
			AllowDisjointCandidateFeatureIfBordersAreNotCoincident =
				_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident;
			AllowEndPointsConnectingToInteriorOfValidNeighborLine =
				_defaultAllowEndPointsConnectingToInteriorOfValidNeighborLine;
			IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance =
				_defaultIgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance;
			IgnoreEndPointsOfBorderingLines = _defaultIgnoreEndPointsOfBorderingLines;
			AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled =
				_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled;
		}

		[InternallyUsedTest]
		public QaEdgeMatchCrossingLines(
			[NotNull] QaEdgeMatchCrossingLinesDefinition definition)
			: this(definition.LineClasses1.Cast<IReadOnlyFeatureClass>().ToList(),
			       (IReadOnlyFeatureClass) definition.BorderClass1,
			       definition.LineClasses2.Cast<IReadOnlyFeatureClass>().ToList(),
			       (IReadOnlyFeatureClass) definition.BorderClass2,
			       definition.SearchDistance)
		{
			MinimumErrorConnectionLineLength = definition.MinimumErrorConnectionLineLength;
			MaximumEndPointConnectionDistance = definition.MaximumEndPointConnectionDistance;
			LineClass1BorderMatchCondition = definition.LineClass1BorderMatchCondition;
			LineClass2BorderMatchCondition = definition.LineClass2BorderMatchCondition;
			CrossingLineMatchCondition = definition.CrossingLineMatchCondition;
			CrossingLineAttributeConstraint = definition.CrossingLineAttributeConstraint;
			IsCrossingLineAttributeConstraintSymmetric =
				definition.IsCrossingLineAttributeConstraintSymmetric;
			CrossingLineEqualAttributes = definition.CrossingLineEqualAttributes;
			CrossingLineEqualAttributeOptions = definition.CrossingLineEqualAttributeOptions;
			ReportIndividualAttributeConstraintViolations =
				definition.ReportIndividualAttributeConstraintViolations;
			CoincidenceTolerance = definition.CoincidenceTolerance;
			AllowNoFeatureWithinSearchDistance = definition.AllowNoFeatureWithinSearchDistance;
			IgnoreAttributeConstraintsIfThreeOrMoreConnected =
				definition.IgnoreAttributeConstraintsIfThreeOrMoreConnected;
			AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide =
				definition.AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide;
			AllowDisjointCandidateFeatureIfBordersAreNotCoincident =
				definition.AllowDisjointCandidateFeatureIfBordersAreNotCoincident;
			IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance = definition
				.IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance;
			AllowEndPointsConnectingToInteriorOfValidNeighborLine =
				definition.AllowEndPointsConnectingToInteriorOfValidNeighborLine;
			IgnoreEndPointsOfBorderingLines = definition.IgnoreEndPointsOfBorderingLines;
			AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled = definition
				.AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled;
		}

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_MinimumErrorConnectionLineLength))]
		[TestParameter(0)]
		public double MinimumErrorConnectionLineLength
		{
			get { return _minimumErrorConnectionLineLength; }
			set
			{
				Assert.ArgumentCondition(value >= 0, "Value must be >= 0");

				_minimumErrorConnectionLineLength = value;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_MaximumEndPointConnectionDistance))]
		[TestParameter(0)]
		public double MaximumEndPointConnectionDistance
		{
			get { return _maximumEndPointConnectionDistance; }
			set
			{
				Assert.ArgumentCondition(value >= 0, "Value must be >= 0");

				_maximumEndPointConnectionDistance = value;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_LineClass1BorderMatchCondition))]
		[TestParameter]
		public string LineClass1BorderMatchCondition
		{
			get { return _lineClass1BorderMatchConditionSql; }
			set
			{
				_lineClass1BorderMatchConditionSql = value;
				AddCustomQueryFilterExpression(value);
				_lineClass1BorderMatchCondition = null;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_LineClass2BorderMatchCondition))]
		[TestParameter]
		public string LineClass2BorderMatchCondition
		{
			get { return _lineClass2BorderMatchConditionSql; }
			set
			{
				_lineClass2BorderMatchConditionSql = value;
				AddCustomQueryFilterExpression(value);
				_lineClass2BorderMatchCondition = null;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_CrossingLineMatchCondition))]
		[TestParameter]
		public string CrossingLineMatchCondition
		{
			get { return _crossingLineMatchConditionSql; }
			set
			{
				_crossingLineMatchConditionSql = value;
				AddCustomQueryFilterExpression(value);
				_crossingLineMatchCondition = null;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_CrossingLineAttributeConstraint))]
		[TestParameter]
		public string CrossingLineAttributeConstraint
		{
			get { return _crossingLineAttributeConstraintSql; }
			set
			{
				_crossingLineAttributeConstraintSql = value;
				AddCustomQueryFilterExpression(value);
				_crossingLineAttributeConstraint = null;
			}
		}

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_IsCrossingLineAttributeConstraintSymmetric))]
		[TestParameter(_defaultIsCrossingLineAttributeConstraintSymmetric)]
		public bool IsCrossingLineAttributeConstraintSymmetric
		{
			get { return _isCrossingLineAttributeConstraintSymmetric; }
			set
			{
				_isCrossingLineAttributeConstraintSymmetric = value;
				_crossingLineAttributeConstraint = null;
			}
		}

		// NOTE blank is not supported as field separator (as it may be used as multi-value separator)
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_CrossingLineEqualAttributes))]
		[TestParameter]
		public string CrossingLineEqualAttributes
		{
			get { return _crossingLineEqualAttributes; }
			set
			{
				_crossingLineEqualAttributes = value;
				AddCustomQueryFilterExpression(value);
				_crossingLineEqualFieldValuesCondition = null;
			}
		}

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_CrossingLineEqualAttributeOptions))]
		[TestParameter]
		public IList<string> CrossingLineEqualAttributeOptions
		{
			get { return _crossingLineEqualAttributeOptions; }
			set
			{
				_crossingLineEqualAttributeOptions = value;
				if (value != null)
				{
					foreach (string option in value)
					{
						AddCustomQueryFilterExpression(option);
					}
				}

				_crossingLineEqualFieldValuesCondition = null;
			}
		}

		[TestParameter(false)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_ReportIndividualAttributeConstraintViolations))]
		public bool ReportIndividualAttributeConstraintViolations { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_CoincidenceTolerance))]
		[TestParameter(_defaultCoincidenceTolerance)]
		public double CoincidenceTolerance { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_AllowNoFeatureWithinSearchDistance))]
		[TestParameter(_defaultAllowNoFeatureWithinSearchDistance)]
		public bool AllowNoFeatureWithinSearchDistance { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_IgnoreAttributeConstraintsIfThreeOrMoreConnected
		))]
		[TestParameter(_defaultIgnoreAttributesConstraintsIfThreeOrMoreConnected)]
		public bool IgnoreAttributeConstraintsIfThreeOrMoreConnected { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide
		))]
		[TestParameter(_defaultAllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide
		)]
		public bool AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_AllowDisjointCandidateFeatureIfBordersAreNotCoincident
		))]
		[TestParameter(_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident)
		]
		public bool AllowDisjointCandidateFeatureIfBordersAreNotCoincident { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance
		))]
		[TestParameter(
			_defaultIgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance)
		]
		public bool IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_AllowEndPointsConnectingToInteriorOfValidNeighborLine
		))]
		[TestParameter(_defaultAllowEndPointsConnectingToInteriorOfValidNeighborLine)]
		public bool AllowEndPointsConnectingToInteriorOfValidNeighborLine { get; set; }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingLines_IgnoreEndPointsOfBorderingLines))]
		[TestParameter(_defaultIgnoreEndPointsOfBorderingLines)]
		public bool IgnoreEndPointsOfBorderingLines { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingLines_AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled
		))]
		[TestParameter(
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)]
		public bool AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled { get; set; }

		protected override void BeginTileCore(BeginTileParameters parameters)
		{
			_tileEnvelope = parameters.TileEnvelope;
			_reportedPointPairs.Clear();
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = row as IReadOnlyFeature;
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
			ICollection<int> sameSideLineClassIndexes;
			ICollection<int> neighborLineClassIndexes = GetNeighborLineClassIndexes(
				tableIndex,
				out borderClassIndex,
				out neighborBorderClassIndex,
				out sameSideLineClassIndexes);

			if (neighborLineClassIndexes == null)
			{
				return NoError;
			}

			Assert.True(borderClassIndex >= 0, "Unexpected border class index");
			Assert.True(neighborBorderClassIndex >= 0,
			            "Unexpected neighboring border class index");

			try
			{
				return CheckConnections(feature, tableIndex,
				                        sameSideLineClassIndexes, borderClassIndex,
				                        neighborLineClassIndexes,
				                        neighborBorderClassIndex);
			}
			finally
			{
				_pointPool.Free();
			}
		}

		[CanBeNull]
		private ICollection<int> GetNeighborLineClassIndexes(
			int tableIndex,
			out int borderClassIndex,
			out int neighborBorderClassIndex,
			out ICollection<int> sameSideLineClassIndexes)
		{
			if (IsLineClass1(tableIndex))
			{
				borderClassIndex = _borderClass1Index;
				neighborBorderClassIndex = _borderClass2Index;
				sameSideLineClassIndexes = _lineClass1Indexes;
				return _lineClass2Indexes;
			}

			if (IsLineClass2(tableIndex))
			{
				borderClassIndex = _borderClass2Index;
				neighborBorderClassIndex = _borderClass1Index;
				sameSideLineClassIndexes = _lineClass2Indexes;
				return _lineClass1Indexes;
			}

			// it's one of the border classes --> ignore
			borderClassIndex = -1;
			neighborBorderClassIndex = -1;
			sameSideLineClassIndexes = null;
			return null;
		}

		private int CheckConnections([NotNull] IReadOnlyFeature feature,
		                             int tableIndex,
		                             [NotNull] ICollection<int>
			                             sameSideLineClassIndexes,
		                             int borderClassIndex,
		                             [NotNull] ICollection<int>
			                             neighborLineClassIndexes,
		                             int neighborBorderClassIndex)
		{
			// determine if the feature ends on the border 
			var polyline = (IPolyline) feature.Shape;
			if (polyline.IsEmpty)
			{
				return NoError;
			}

			ICollection<BorderConnection> borderConnections =
				GetBorderConnections(polyline,
				                     feature,
				                     tableIndex,
				                     borderClassIndex);

			EnsureCrossingLineMatchCondition();
			EnsureCrossingLineAttributeConstraint();
			EnsureCrossingLineEqualFieldValuesCondition();

			// search neighboring features (within search distance, also connected to THEIR border)

			var errorCount = 0;

			foreach (BorderConnection borderConnection in borderConnections)
			{
				if (IgnoreEndPointsOfBorderingLines &&
				    borderConnection.EndSegmentFollowsBorder)
				{
					continue;
				}

				IList<IReadOnlyFeature> connectionsOnSameSide =
					GetConnectionsOnSameSide(feature, tableIndex,
					                         borderConnection.Point,
					                         sameSideLineClassIndexes);

				errorCount += CheckBorderConnection(borderConnection,
				                                    neighborLineClassIndexes,
				                                    neighborBorderClassIndex,
				                                    _crossingLineMatchCondition,
				                                    connectionsOnSameSide);
			}

			return errorCount;
		}

		private void EnsureCrossingLineEqualFieldValuesCondition()
		{
			if (_crossingLineEqualFieldValuesCondition != null)
			{
				return;
			}

			_crossingLineEqualFieldValuesCondition =
				new EqualFieldValuesCondition(_crossingLineEqualAttributes,
				                              _crossingLineEqualAttributeOptions,
				                              GetTables(_allLineClassIndexes),
				                              GetSqlCaseSensitivity(
					                              _allLineClassIndexes));
		}

		private void EnsureCrossingLineAttributeConstraint()
		{
			if (_crossingLineAttributeConstraint != null)
			{
				return;
			}

			bool isDirected = ! IsCrossingLineAttributeConstraintSymmetric;

			_crossingLineAttributeConstraint = new LineAttributeConstraint(
				_crossingLineAttributeConstraintSql,
				GetSqlCaseSensitivity(_allLineClassIndexes),
				isDirected);
		}

		private void EnsureCrossingLineMatchCondition()
		{
			if (_crossingLineMatchCondition != null)
			{
				return;
			}

			_crossingLineMatchCondition =
				new LineMatchCondition(_crossingLineMatchConditionSql,
				                       GetSqlCaseSensitivity(_allLineClassIndexes));
		}

		[NotNull]
		private IEnumerable<IReadOnlyTable> GetTables([NotNull] IEnumerable<int> tableIndexes)
		{
			return
				tableIndexes.Distinct()
				            .Select(tableIndex => InvolvedTables[tableIndex]);
		}

		private IList<IReadOnlyFeature> GetConnectionsOnSameSide(
			[NotNull] IReadOnlyFeature feature,
			int tableIndex,
			[NotNull] IPoint borderConnection,
			[NotNull] IEnumerable<int> sameSideLineClassIndexes)
		{
			var connectionsOnSameSide = new List<IReadOnlyFeature>();
			var table = feature.Table;

			long oid = feature.OID;

			foreach (int classIndex in sameSideLineClassIndexes)
			{
				IFeatureClassFilter spatialFilter = _filters[classIndex];
				spatialFilter.FilterGeometry = borderConnection;

				IReadOnlyTable searchTable = InvolvedTables[classIndex];

				bool sameTable = searchTable == table;

				foreach (IReadOnlyFeature otherFeature in
				         Search(InvolvedTables[classIndex], spatialFilter,
				                _filterHelpers[classIndex])
					         .Cast<IReadOnlyFeature>())
				{
					if (sameTable && otherFeature.OID == oid)
					{
						continue;
					}

					if (_crossingLineMatchCondition.IsFulfilled(feature, tableIndex,
					                                            otherFeature,
					                                            classIndex))
					{
						// the line pair is crossing (-> not on same side)
						continue;
					}

					var otherPolyline = (IPolyline) otherFeature.Shape;

					if (((IRelationalOperator) otherPolyline).Touches(borderConnection))
					{
						// connected at end point
						connectionsOnSameSide.Add(otherFeature);
					}
				}
			}

			return connectionsOnSameSide;
		}

		[NotNull]
		private IFeatureClassFilter GetSearchFilter(int tableIndex,
		                                            [NotNull] IPoint point,
		                                            double searchDistance)
		{
			IFeatureClassFilter filter = _filters[tableIndex];

			double x;
			double y;
			point.QueryCoords(out x, out y);

			_searchEnvelopeTemplate.PutCoords(x - searchDistance, y - searchDistance,
			                                  x + searchDistance, y + searchDistance);

			filter.FilterGeometry = _searchEnvelopeTemplate;

			return filter;
		}

		private int CheckBorderConnection(
			[NotNull] BorderConnection borderConnection,
			[NotNull] IEnumerable<int> neighborLineClassIndexes,
			int neighborBorderLineClassIndex,
			[NotNull] LineMatchCondition crossingLineMatchCondition,
			[NotNull] IList<IReadOnlyFeature> connectionsOnSameSide)
		{
			var errorCount = 0;
			IReadOnlyFeature feature = borderConnection.Feature;
			bool isStartPoint = borderConnection.EndPoint.IsStartPoint;
			int lineClassIndex = borderConnection.ClassIndex;

			var connectionsOnNeighborSide = new List<BorderConnection>();
			var connectionsWithNeighborBorder = new List<BorderConnection>();
			var unconnectedNeighborEndpoints = new List<UnconnectedNeighborEndpoint>();

			foreach (int neighborLineClassIndex in neighborLineClassIndexes)
			{
				bool reportedAnyInOppositeDirection;
				PrepareConnections(borderConnection, neighborLineClassIndex,
				                   neighborBorderLineClassIndex,
				                   crossingLineMatchCondition,
				                   connectionsOnNeighborSide,
				                   connectionsWithNeighborBorder,
				                   unconnectedNeighborEndpoints,
				                   out reportedAnyInOppositeDirection);
				if (reportedAnyInOppositeDirection)
				{
					return errorCount;
				}
			}

			int connectionedFeaturesCount = 1 + connectionsOnSameSide.Count +
			                                connectionsOnNeighborSide.Count;
			foreach (BorderConnection neighborConnection in connectionsOnNeighborSide)
			{
				bool neighborIsStartPoint = neighborConnection.EndPoint.IsStartPoint;

				if (WasReportedInOppositeDirection(feature, isStartPoint,
				                                   lineClassIndex,
				                                   neighborConnection.Feature,
				                                   neighborIsStartPoint,
				                                   neighborConnection.ClassIndex))
				{
					continue;
				}

				AddReportedRowPair(feature, isStartPoint, lineClassIndex,
				                   neighborConnection.Feature, neighborIsStartPoint,
				                   neighborConnection.ClassIndex);

				if (IgnoreAttributeConstraintsIfThreeOrMoreConnected &&
				    connectionedFeaturesCount >= 3)
				{
					continue;
				}

				foreach (AttributeConstraintViolation constraintViolation in
				         GetAttributeConstraintViolations(
					         borderConnection, neighborConnection))
				{
					errorCount += ReportError(
						constraintViolation.Description,
						InvolvedRowUtils.GetInvolvedRows(feature, neighborConnection.Feature),
						GeometryFactory.Clone(borderConnection.Point),
						Codes[Code.Match_ConstraintsNotFulfilled],
						constraintViolation.AffectedComponents,
						values: new[] { constraintViolation.TextValue });
				}
			}

			if (connectionsOnNeighborSide.Count > 0)
			{
				return errorCount;
			}

			foreach (BorderConnection neighborConnection
			         in connectionsWithNeighborBorder)
			{
				foreach (AttributeConstraintViolation constraintViolation in
				         GetAttributeConstraintViolations(
					         borderConnection, neighborConnection))
				{
					errorCount += ReportError(
						constraintViolation.Description,
						InvolvedRowUtils.GetInvolvedRows(feature, neighborConnection.Feature),
						GeometryFactory.Clone(borderConnection.Point),
						Codes[Code.Match_ConstraintsNotFulfilled],
						constraintViolation.AffectedComponents,
						values: new[] { constraintViolation.TextValue });
				}
			}

			if (connectionsWithNeighborBorder.Count > 0)
			{
				return errorCount;
			}

			if (unconnectedNeighborEndpoints.Count == 0)
			{
				if (! AllowNoFeatureWithinSearchDistance)
				{
					if (connectionsOnSameSide.Count > 0)
					{
						if (! AllowNoFeatureWithinSearchDistanceIfConnectedOnSameSide)
						{
							errorCount += ReportError(
								LocalizableStrings
									.QaEdgeMatchCrossingLines_NoMatch_NoCandidate_ConnectedOnSameSide,
								InvolvedRowUtils.GetInvolvedRows(feature),
								GeometryFactory.Clone(borderConnection.Point),
								Codes[Code.NoMatch_NoCandidate_ConnectedOnSameSide], null);
							return errorCount;
						}
					}
					else
					{
						errorCount += ReportError(
							LocalizableStrings
								.QaEdgeMatchCrossingLines_NoMatch_NoCandidate,
							InvolvedRowUtils.GetInvolvedRows(feature),
							GeometryFactory.Clone(borderConnection.Point),
							Codes[Code.NoMatch_NoCandidate], null);
						return errorCount;
					}
				}
			}

			// there is one or more neighbor feature within the tolerance

			foreach (
				UnconnectedNeighborEndpoint unconnectedNeighborEndpoint in
				unconnectedNeighborEndpoints)
			{
				BorderConnection neighborBorderConnection =
					unconnectedNeighborEndpoint.NeighborBorderConnection;

				bool areBordersCoincident = AreBordersCoincident(
					borderConnection, neighborBorderConnection);

				AddReportedRowPair(feature, isStartPoint, lineClassIndex,
				                   neighborBorderConnection.Feature,
				                   neighborBorderConnection.EndPoint.IsStartPoint,
				                   neighborBorderConnection.ClassIndex);

				if (! areBordersCoincident &&
				    AllowDisjointCandidateFeatureIfBordersAreNotCoincident)
				{
					continue;
				}

				ICollection<AttributeConstraintViolation> constraintViolations =
					GetAttributeConstraintViolations(borderConnection, neighborBorderConnection);

				if (constraintViolations.Count == 0 &&
				    AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)
				{
					continue;
				}

				bool endPointsWithinMaximumDistance =
					_maximumEndPointConnectionDistance <= 0 ||
					unconnectedNeighborEndpoint.DistanceToEndpoint <=
					_maximumEndPointConnectionDistance;

				bool connectEndPoints = endPointsWithinMaximumDistance;
				double connectionLength;
				IGeometry errorGeometry = GetErrorGeometry(borderConnection,
				                                           unconnectedNeighborEndpoint,
				                                           connectEndPoints,
				                                           out connectionLength);

				if (constraintViolations.Count == 0)
				{
					IssueCode issueCode;
					string description = GetErrorDescription(
						areBordersCoincident, endPointsWithinMaximumDistance,
						connectionLength,
						unconnectedNeighborEndpoint.DistanceToEndpoint,
						null, out issueCode);

					errorCount += ReportError(
						description,
						InvolvedRowUtils.GetInvolvedRows(feature, neighborBorderConnection.Feature),
						errorGeometry, issueCode, null);
				}
				else
				{
					foreach (AttributeConstraintViolation constraintViolation in
					         constraintViolations
					        )
					{
						IssueCode issueCode;
						string description = GetErrorDescription(
							areBordersCoincident, endPointsWithinMaximumDistance,
							connectionLength,
							unconnectedNeighborEndpoint.DistanceToEndpoint,
							constraintViolation, out issueCode);

						errorCount += ReportError(
							description,
							InvolvedRowUtils.GetInvolvedRows(
								feature, neighborBorderConnection.Feature),
							errorGeometry, issueCode, constraintViolation.AffectedComponents,
							values: new[] { constraintViolation.TextValue });
					}
				}
			}

			return errorCount;
		}

		[NotNull]
		private ICollection<AttributeConstraintViolation> GetAttributeConstraintViolations(
			[NotNull] BorderConnection borderConnection,
			[NotNull] BorderConnection neighborConnection)
		{
			return EdgeMatchUtils.GetAttributeConstraintViolations(
				borderConnection.Feature, borderConnection.ClassIndex,
				neighborConnection.Feature, neighborConnection.ClassIndex,
				_crossingLineAttributeConstraint,
				_crossingLineEqualFieldValuesCondition,
				ReportIndividualAttributeConstraintViolations).ToList();
		}

		private void PrepareConnections([NotNull] BorderConnection borderConnection,
		                                int neighborLineClassIndex,
		                                int neighborBorderLineClassIndex,
		                                [NotNull] LineMatchCondition
			                                crossingLineMatchCondition,
		                                [NotNull] List<BorderConnection>
			                                connectionsOnNeighborSide,
		                                [NotNull] List<BorderConnection>
			                                connectionsWithNeighborBorder,
		                                [NotNull] List<UnconnectedNeighborEndpoint>
			                                unconnectedNeighborEndpoints,
		                                out bool reportedAnyInOppositeDirection)
		{
			IReadOnlyFeature feature = borderConnection.Feature;
			bool isStartPoint = borderConnection.EndPoint.IsStartPoint;
			int lineClassIndex = borderConnection.ClassIndex;

			double neighborXyTolerance =
				_xyToleranceByTableIndex[neighborLineClassIndex];

			foreach (IReadOnlyFeature neighborFeature in
			         SearchNeighborFeatures(borderConnection.Point, neighborLineClassIndex)
			        )
			{
				if (! crossingLineMatchCondition.IsFulfilled(feature, lineClassIndex,
				                                             neighborFeature,
				                                             neighborLineClassIndex))
				{
					continue;
				}

				var neighborPolyline = (IPolyline) neighborFeature.Shape;

				double distanceToNeighborLine = GetDistance(borderConnection.Point,
				                                            neighborPolyline);

				if (distanceToNeighborLine > _searchDistance)
				{
					// the line is outside the search distance
					continue;
				}

				// determine if the neighbor line is connected to it's border
				ICollection<BorderConnection> neighborBorderConnections =
					GetBorderConnections(neighborPolyline,
					                     neighborFeature,
					                     neighborLineClassIndex,
					                     neighborBorderLineClassIndex);

				if (neighborBorderConnections.Count <= 0)
				{
					// there is no neighboring line within the search distance which is connected to it's border
					continue;
				}

				// at least one neighboring line connected to its border found

				double minDistance = double.MaxValue;
				BorderConnection nearestNeighborBorderConnection = null;

				double coincidenceTolerance =
					GetCoincidenceTolerance(borderConnection.ClassIndex);

				foreach (
					BorderConnection neighborBorderConnection in
					neighborBorderConnections)
				{
					if (WasReportedInOppositeDirection(feature, isStartPoint,
					                                   lineClassIndex,
					                                   neighborFeature,
					                                   neighborBorderConnection
						                                   .EndPoint.IsStartPoint,
					                                   neighborLineClassIndex))
					{
						reportedAnyInOppositeDirection = true;
						return;
					}

					double distanceToNeighborBorderConnection =
						GeometryUtils.GetPointDistance(borderConnection.Point,
						                               neighborBorderConnection.Point);

					if (
						EdgeMatchUtils.IsWithinTolerance(
							distanceToNeighborBorderConnection,
							coincidenceTolerance))
					{
						// the line is connected to a neighbor line end point
						connectionsOnNeighborSide.Add(neighborBorderConnection);

						continue;
					}

					// the line is not connected to the neighbor line end point

					if (distanceToNeighborBorderConnection > _searchDistance)
					{
						// the neighbor border connection end point is outside the search distance
						if (
							IgnoreNeighborLinesWithBorderConnectionOutsideSearchDistance)
						{
							continue;
						}
					}

					// test if it connects to the interior of the neighbor line
					if (distanceToNeighborLine < neighborXyTolerance)
					{
						// end point does not connect to the end point, but to the interior of the neighbor line
						// TODO additionally require a vertex on the neighbor line at the connection location?

						if (AllowEndPointsConnectingToInteriorOfValidNeighborLine)
						{
							bool isCoincidentWithNeighborBorder =
								IsCoincidentWithNeighborBorder(borderConnection,
								                               neighborFeature,
								                               neighborLineClassIndex,
								                               neighborBorderLineClassIndex);

							if (isCoincidentWithNeighborBorder)
							{
								connectionsWithNeighborBorder.Add(
									neighborBorderConnection);
								continue;
							}
						}
					}

					// update information about the nearest border connection
					if (distanceToNeighborBorderConnection < minDistance)
					{
						minDistance = distanceToNeighborBorderConnection;
						nearestNeighborBorderConnection = neighborBorderConnection;
					}
				}

				if (nearestNeighborBorderConnection != null)
				{
					unconnectedNeighborEndpoints.Add(
						new UnconnectedNeighborEndpoint(
							nearestNeighborBorderConnection,
							minDistance,
							neighborPolyline,
							distanceToNeighborLine));
				}
			}

			reportedAnyInOppositeDirection = false;
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
			                                      neighborBorderConnection
				                                      .BorderClassIndex) &&
			       IsCoincidentWithNeighborBorder(neighborBorderConnection,
			                                      borderConnection.Feature,
			                                      borderConnection.ClassIndex,
			                                      borderConnection.BorderClassIndex);
		}

		private static double GetDistance([NotNull] IPoint point,
		                                  [NotNull] IPolyline polyline)
		{
			var proximity = (IProximityOperator) polyline;
			return proximity.ReturnDistance(point);
		}

		private bool WasReportedInOppositeDirection([NotNull] IReadOnlyRow row,
		                                            bool isStartPoint,
		                                            int tableIndex,
		                                            [NotNull] IReadOnlyRow neighborRow,
		                                            bool isNeighborStartPoint,
		                                            int neighborTableIndex)
		{
			return
				_reportedPointPairs.Contains(new FeaturePointPair(neighborTableIndex,
					                             neighborRow.OID,
					                             isNeighborStartPoint,
					                             tableIndex, row.OID,
					                             isStartPoint));
		}

		private void AddReportedRowPair([NotNull] IReadOnlyRow row, bool isStartPoint,
		                                int tableIndex,
		                                [NotNull] IReadOnlyRow neighborRow,
		                                bool isNeighborStartPoint,
		                                int neighborTableIndex)
		{
			_reportedPointPairs.Add(new FeaturePointPair(tableIndex, row.OID,
			                                             isStartPoint,
			                                             neighborTableIndex,
			                                             neighborRow.OID,
			                                             isNeighborStartPoint));
		}

		[NotNull]
		private IEnumerable<IReadOnlyFeature> SearchNeighborFeatures(
			[NotNull] IPoint borderConnection,
			int neighborLineClassIndex)
		{
			IFeatureClassFilter spatialFilter = GetSearchFilter(neighborLineClassIndex,
			                                                    borderConnection,
			                                                    _searchDistance);

			return Search(InvolvedTables[neighborLineClassIndex],
			              spatialFilter,
			              _filterHelpers[neighborLineClassIndex]).Cast<IReadOnlyFeature>();
		}

		[NotNull]
		private string GetErrorDescription(
			bool areBordersCoincident,
			bool endPointsWithinMaximumDistance,
			double connectionLength,
			double distanceToEndPoint,
			[CanBeNull] AttributeConstraintViolation constraintViolation,
			out IssueCode issueCode)
		{
			// can't report the distance from this end point to the neighbor polyline, 
			// as this would result in unequal error descriptions for the two testing directions, 
			// and duplicate errors

			if (endPointsWithinMaximumDistance)
			{
				string description = string.Format(
					LocalizableStrings
						.QaEdgeMatchCrossingLines_NoMatch_CandidateExists,
					FormatLength(connectionLength, _spatialReference).Trim());

				if (constraintViolation == null)
				{
					if (areBordersCoincident)
					{
						issueCode =
							Codes[Code.NoMatch_CandidateExists_ConstraintsFulfilled];
						return description;
					}

					issueCode =
						Codes[
							Code
								.NoMatch_CandidateExists_BordersNotCoincident_ConstraintsFulfilled
						];

					return
						$"{description} {LocalizableStrings.QaEdgeMatchCrossingLines_BordersNotCoincident}";
				}

				// constraints are not fulfilled
				if (areBordersCoincident)
				{
					issueCode = Codes[Code.NoMatch_CandidateExists_ConstraintsNotFulfilled];
					return $"{description} {constraintViolation.Description}.";
				}

				issueCode =
					Codes[
						Code
							.NoMatch_CandidateExists_BordersNotCoincident_ConstraintsNotFulfilled
					];

				return
					$"{description} {LocalizableStrings.QaEdgeMatchCrossingLines_BordersNotCoincident} {constraintViolation.Description}.";
			}
			// end points further away than maximum distance

			if (constraintViolation == null)
			{
				string code =
					areBordersCoincident
						? Code
							.NoMatch_CandidateExists_EndPointOutsideTolerance_ConstraintsFulfilled
						: Code
							.NoMatch_CandidateExists_EndPointOutsideTolerance_BordersNotCoincident_ConstraintsFulfilled;

				issueCode = Codes[code];

				string description =
					string.Format(
						      LocalizableStrings
							      .QaEdgeMatchCrossingLines_NoMatch_CandidateExists_EndPointOutsideTolerance,
						      FormatLength(distanceToEndPoint, _spatialReference).Trim(),
						      FormatLength(connectionLength, _spatialReference))
					      .Trim();

				return areBordersCoincident
					       ? description
					       : $"{description} {LocalizableStrings.QaEdgeMatchCrossingLines_BordersNotCoincident}";
			}
			else
			{
				// attribute constraints are not fulfilled

				string code =
					areBordersCoincident
						? Code
							.NoMatch_CandidateExists_EndPointOutsideTolerance_ConstraintsNotFulfilled
						: Code
							.NoMatch_CandidateExists_EndPointOutsideTolerance_BordersNotCoincident_ConstraintsNotFulfilled;

				issueCode = Codes[code];

				string description = string.Format(
					                           LocalizableStrings
						                           .QaEdgeMatchCrossingLines_NoMatch_CandidateExists_EndPointOutsideTolerance,
					                           FormatLength(distanceToEndPoint,
					                                        _spatialReference).Trim(),
					                           FormatLength(connectionLength,
					                                        _spatialReference))
				                           .Trim();

				return areBordersCoincident
					       ? $"{description} {constraintViolation.Description}."
					       : $"{description} {LocalizableStrings.QaEdgeMatchCrossingLines_BordersNotCoincident} {constraintViolation.Description}.";
			}
		}

		[NotNull]
		private IGeometry GetErrorGeometry(
			[NotNull] BorderConnection borderConnection,
			[NotNull] UnconnectedNeighborEndpoint unconnectedNeighborEndpoint,
			bool connectToEndpoint,
			out double connectionLength)
		{
			IPoint connectionPoint;
			if (connectToEndpoint)
			{
				connectionPoint =
					unconnectedNeighborEndpoint.NeighborBorderConnection.Point;
				connectionLength = unconnectedNeighborEndpoint.DistanceToEndpoint;
			}
			else
			{
				// else: connect to nearest location along neighbor line
				var proxyOp =
					(IProximityOperator) unconnectedNeighborEndpoint.NeighborPolyline;
				proxyOp.QueryNearestPoint(borderConnection.Point,
				                          esriSegmentExtension.esriNoExtension,
				                          _pointTemplate);

				if (! _pointTemplate.IsEmpty)
				{
					connectionPoint = _pointTemplate;
					connectionLength = ((IProximityOperator) borderConnection.Point)
						.ReturnDistance(
							connectionPoint);
				}
				else
				{
					connectionPoint =
						unconnectedNeighborEndpoint.NeighborBorderConnection.Point;
					connectionLength = unconnectedNeighborEndpoint.DistanceToEndpoint;
				}
			}

			if (connectionLength < _minimumErrorConnectionLineLength)
			{
				return GeometryFactory.CreateMultipoint(
					GeometryFactory.Clone(borderConnection.Point),
					GeometryFactory.Clone(connectionPoint));
			}

			return GeometryFactory.CreatePolyline(
				GeometryFactory.Clone(borderConnection.Point),
				GeometryFactory.Clone(connectionPoint));
		}

		[NotNull]
		private ICollection<BorderConnection> GetBorderConnections(
			[NotNull] IPolyline polyline,
			[NotNull] IReadOnlyFeature lineFeature,
			int lineClassIndex,
			int borderClassIndex)
		{
			var result = new List<BorderConnection>(2);

			foreach (LineEndPoint lineEndPoint in GetEndPoints(polyline))
			{
				if (IsOutsideCurrentTile(lineEndPoint.Point))
				{
					continue;
				}

				ICollection<IPolyline> borderFeatures =
					GetConnectedBorderShapes(lineEndPoint.Point,
					                         lineFeature,
					                         lineClassIndex,
					                         borderClassIndex);

				if (borderFeatures.Count > 0)
				{
					result.Add(new BorderConnection(lineFeature, lineEndPoint,
					                                lineClassIndex,
					                                borderFeatures, borderClassIndex));
				}
			}

			return result;
		}

		private bool IsOutsideCurrentTile([NotNull] IPoint point)
		{
			return _tileEnvelope != null &&
			       ((IRelationalOperator) _tileEnvelope).Disjoint(point);
		}

		private bool IsCoincidentWithNeighborBorder(
			[NotNull] BorderConnection borderConnection,
			[NotNull] IReadOnlyFeature neighborFeature, int neighborLineClassIndex,
			int neighborBorderClassIndex)
		{
			IReadOnlyTable neighborBorderClass = InvolvedTables[neighborBorderClassIndex];

			IFeatureClassFilter spatialFilter = _filters[neighborBorderClassIndex];
			spatialFilter.FilterGeometry = borderConnection.Point;

			BorderMatchCondition neighborBorderMatchCondition =
				GetBorderMatchCondition(neighborLineClassIndex);

			foreach (IReadOnlyRow borderRow in Search(neighborBorderClass,
			                                          spatialFilter,
			                                          _filterHelpers[neighborBorderClassIndex])
			        )
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

		[NotNull]
		private ICollection<IPolyline> GetConnectedBorderShapes(
			[NotNull] IPoint point,
			[NotNull] IReadOnlyFeature lineFeature, int lineClassIndex,
			int borderClassIndex)
		{
			IReadOnlyTable borderClass = InvolvedTables[borderClassIndex];

			IFeatureClassFilter spatialFilter = _filters[borderClassIndex];
			spatialFilter.FilterGeometry = point;

			var result = new List<IPolyline>(5);

			BorderMatchCondition borderMatchCondition =
				GetBorderMatchCondition(lineClassIndex);

			foreach (IReadOnlyFeature borderFeature in
			         Search(borderClass, spatialFilter, _filterHelpers[borderClassIndex])
				         .Cast<IReadOnlyFeature>())
			{
				if (! borderMatchCondition.IsFulfilled(lineFeature, lineClassIndex,
				                                       borderFeature, borderClassIndex))
				{
					continue;
				}

				IGeometry borderShape = borderFeature.Shape;
				var borderLine = borderShape as IPolyline;
				if (borderLine == null)
				{
					var poly = borderShape as IPolygon;
					if (poly != null)
					{
						borderLine =
							(IPolyline) GeometryUtils.GetBoundary(borderShape);

						if (((IRelationalOperator) borderLine).Disjoint(point))
						{
							borderLine = null;
						}
					}
				}

				if (borderLine != null)
				{
					result.Add(borderLine);
				}
			}

			return result;
		}

		[NotNull]
		private BorderMatchCondition GetBorderMatchCondition(int lineClassIndex)
		{
			if (IsLineClass1(lineClassIndex))
			{
				return _lineClass1BorderMatchCondition ??
				       (_lineClass1BorderMatchCondition =
					        new BorderMatchCondition(
						        _lineClass1BorderMatchConditionSql,
						        GetSqlCaseSensitivity(lineClassIndex,
						                              _borderClass1Index)));
			}

			if (IsLineClass2(lineClassIndex))
			{
				return _lineClass2BorderMatchCondition ??
				       (_lineClass2BorderMatchCondition =
					        new BorderMatchCondition(
						        _lineClass2BorderMatchConditionSql,
						        GetSqlCaseSensitivity(lineClassIndex,
						                              _borderClass2Index)));
			}

			throw new ArgumentException("Not a line class index");
		}

		[NotNull]
		private IEnumerable<LineEndPoint> GetEndPoints([NotNull] IPolyline polyline)
		{
			IPoint template1 = _pointPool.CreatePoint();
			IPoint template2 = _pointPool.CreatePoint();

			polyline.QueryFromPoint(template1);
			yield return new LineEndPoint(template1, true);

			polyline.QueryToPoint(template2);
			yield return new LineEndPoint(template2, false);
		}

		private void InitFilters()
		{
			CopyFilters(out _filters, out _filterHelpers);

			foreach (var filter in _filters)
			{
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects;
			}

			_filters[_borderClass1Index].SpatialRelationship = GetBorderClassSpatialRelation(
				_geometryTypesByTableIndex[_borderClass1Index]);

			_filters[_borderClass2Index].SpatialRelationship = GetBorderClassSpatialRelation(
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
						string.Format("Unexpected border geometry type: {0}",
						              borderGeometryType));
			}
		}

		private bool IsLineClass1(int tableIndex)
		{
			return tableIndex < _borderClass1Index;
		}

		private bool IsLineClass2(int tableIndex)
		{
			return tableIndex > _borderClass1Index && tableIndex < _borderClass2Index;
		}

		#region Nested types

		private class FeaturePointPair : IEquatable<FeaturePointPair>
		{
			private readonly int _tableIndex1;
			private readonly long _oid1;
			private readonly bool _isStartPoint1;
			private readonly int _tableIndex2;
			private readonly long _oid2;
			private readonly bool _isStartPoint2;

			public FeaturePointPair(int tableIndex1, long oid1, bool isStartPoint1,
			                        int tableIndex2, long oid2, bool isStartPoint2)
			{
				_tableIndex1 = tableIndex1;
				_oid1 = oid1;
				_isStartPoint1 = isStartPoint1;
				_tableIndex2 = tableIndex2;
				_oid2 = oid2;
				_isStartPoint2 = isStartPoint2;
			}

			public bool Equals(FeaturePointPair other)
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
				       _isStartPoint1 == other._isStartPoint1 &&
				       _oid2 == other._oid2 && _tableIndex2 == other._tableIndex2 &&
				       _isStartPoint2 == other._isStartPoint2;
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

				return Equals((FeaturePointPair) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = _tableIndex1;
					hashCode = (hashCode * 397) ^ _oid1.GetHashCode();
					hashCode = (hashCode * 397) ^ (_isStartPoint1 ? 0 : 1);
					hashCode = (hashCode * 397) ^ _tableIndex2;
					hashCode = (hashCode * 397) ^ _oid2.GetHashCode();
					hashCode = (hashCode * 397) ^ (_isStartPoint2 ? 0 : 1);
					return hashCode;
				}
			}
		}

		private class BorderConnection : EdgeMatchBorderConnection
		{
			private readonly ICollection<IPolyline> _borderShapes;
			private bool? _endSegmentFollowsBorder;

			public BorderConnection([NotNull] IReadOnlyFeature feature,
			                        [NotNull] LineEndPoint endPoint,
			                        int classIndex,
			                        [NotNull] ICollection<IPolyline> borderShapes,
			                        int borderClassIndex)
				: base(feature, classIndex, borderClassIndex)
			{
				EndPoint = endPoint;
				_borderShapes = borderShapes;
			}

			[NotNull]
			public LineEndPoint EndPoint { get; }

			[NotNull]
			public IPoint Point => EndPoint.Point;

			public bool EndSegmentFollowsBorder
			{
				get
				{
					if (_endSegmentFollowsBorder == null)
					{
						_endSegmentFollowsBorder =
							IsEndSegmentFollowingBorder(EndPoint,
							                            (IPolyline) Feature.Shape,
							                            _borderShapes);
					}

					return _endSegmentFollowsBorder.Value;
				}
			}

			private static bool IsEndSegmentFollowingBorder(
				[NotNull] LineEndPoint endPoint,
				[NotNull] IPolyline polyline,
				[NotNull] IEnumerable<IPolyline> borderShapes)
			{
				// TODO get subcurve of a given minimum length?
				var segments = (ISegmentCollection) polyline;
				ISegment endSegment = endPoint.IsStartPoint
					                      ? segments.Segment[0]
					                      : segments.Segment[segments.SegmentCount - 1
					                      ];

				IPolyline segmentLine = GeometryFactory.CreatePolyline(
					endSegment, polyline.SpatialReference, false, false);

				foreach (IPolyline borderShape in borderShapes)
				{
					IPolyline intersection = GetLinearIntersection(segmentLine,
						borderShape);

					if (! intersection.IsEmpty)
					{
						return true;
					}
				}

				return false;
			}
		}

		[NotNull]
		private static IPolyline GetLinearIntersection([NotNull] IPolyline line,
		                                               [NotNull] IGeometry geometry)
		{
			return (IPolyline) IntersectionUtils.Intersect(
				line, geometry,
				esriGeometryDimension.esriGeometry1Dimension);
		}

		private class LineEndPoint
		{
			public LineEndPoint([NotNull] IPoint point, bool isStartPoint)
			{
				Point = point;
				IsStartPoint = isStartPoint;
			}

			[NotNull]
			public IPoint Point { get; }

			public bool IsStartPoint { get; }
		}

		private class UnconnectedNeighborEndpoint
		{
			public UnconnectedNeighborEndpoint(
				[NotNull] BorderConnection neighborBorderConnection,
				double distanceToEndpoint,
				[NotNull] IPolyline neighborPolyline,
				double distanceToNeighborPolyline)
			{
				Assert.ArgumentNotNull(neighborBorderConnection,
				                       nameof(neighborBorderConnection));
				Assert.ArgumentNotNull(neighborPolyline, nameof(neighborPolyline));

				NeighborBorderConnection = neighborBorderConnection;
				DistanceToEndpoint = distanceToEndpoint;
				NeighborPolyline = neighborPolyline;
				DistanceToNeighborPolyline = distanceToNeighborPolyline;
			}

			[NotNull]
			public IPolyline NeighborPolyline { get; }

			[UsedImplicitly]
			public double DistanceToNeighborPolyline { get; }

			[NotNull]
			public BorderConnection NeighborBorderConnection { get; }

			public double DistanceToEndpoint { get; }
		}

		private class PointPool
		{
			[NotNull] private readonly List<PooledPoint> _pooledPoints =
				new List<PooledPoint>();

			public void Free()
			{
				foreach (PooledPoint entry in _pooledPoints)
				{
					entry.InUse = false;
				}
			}

			public IPoint CreatePoint()
			{
				foreach (PooledPoint pooledPoint in _pooledPoints)
				{
					if (! pooledPoint.InUse)
					{
						pooledPoint.InUse = true;
						return pooledPoint.Point;
					}
				}

				// no free point found
				var newPooledPoint = new PooledPoint { InUse = true };

				_pooledPoints.Add(newPooledPoint);

				return newPooledPoint.Point;
			}

			private class PooledPoint
			{
				public PooledPoint()
				{
					Point = new PointClass();
				}

				public IPoint Point { get; }

				public bool InUse { get; set; }
			}
		}

		private class BorderMatchCondition : RowPairCondition
		{
			public BorderMatchCondition([CanBeNull] string condition,
			                            bool caseSensitive)
				: base(condition, true, true, "LINE", "BORDER", caseSensitive) { }
		}

		private class LineMatchCondition : RowPairCondition
		{
			public LineMatchCondition([CanBeNull] string condition, bool caseSensitive)
				: base(condition, true, true, "LINE1", "LINE2", caseSensitive) { }
		}

		private class LineAttributeConstraint : RowPairCondition
		{
			public LineAttributeConstraint([CanBeNull] string condition,
			                               bool caseSensitive,
			                               bool isDirected)
				: base(condition, isDirected, true, "LINE1", "LINE2",
				       caseSensitive, conciseMessage: true) { }
		}

		#endregion
	}
}
