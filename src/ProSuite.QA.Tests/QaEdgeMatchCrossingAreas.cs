using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Collections;
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
	public class QaEdgeMatchCrossingAreas : ContainerTest
	{
		private readonly List<int> _areaClass1Indexes;
		private readonly int _borderClass1Index;
		private readonly List<int> _areaClass2Indexes;
		private readonly List<int> _allAreaClassIndexes;
		private readonly int _borderClass2Index;
		private readonly List<int> _boundingClass1Indexes;
		private readonly List<int> _boundingClass2Indexes;

		private readonly double _searchDistance;

		private ISpatialReference _spatialReference;
		private ISpatialReference _highResolutionSpatialReference;
		private IList<IFeatureClassFilter> _filters;
		private IList<QueryFilterHelper> _filterHelpers;
		private readonly IDictionary<int, esriGeometryType> _geometryTypesByTableIndex;
		private readonly IDictionary<int, double> _xyToleranceByTableIndex;

		private readonly IEnvelope _searchEnvelopeTemplate = new EnvelopeClass();

		private IEnvelope _tileEnvelope;
		private WKSEnvelope _tileWksEnvelope;
		private WKSEnvelope _allWksEnvelope;

		private readonly BorderConnectionCache _borderConnectionCache =
			new BorderConnectionCache();

		private readonly ConstraintErrorCache _constraintErrors = new ConstraintErrorCache();

		private readonly BorderConnectionUnion _borderConnectionUnion1 =
			new BorderConnectionUnion();

		private readonly BorderConnectionUnion _borderConnectionUnion2 =
			new BorderConnectionUnion();

		private string _areaClass1BorderMatchConditionSql;
		private string _areaClass2BorderMatchConditionSql;
		private string _areaClass1BoundingFeatureMatchConditionSql;
		private string _areaClass2BoundingFeatureMatchConditionSql;
		private string _crossingAreaMatchConditionSql;
		private string _crossingAreaAttributeConstraintSql;

		private BorderMatchCondition _areaClass1BorderMatchCondition;
		private BorderMatchCondition _areaClass2BorderMatchCondition;

		private BoundingFeatureMatchCondition _areaClass1BoundingFeatureMatchCondition;
		private BoundingFeatureMatchCondition _areaClass2BoundingFeatureMatchCondition;
		private AreaMatchCondition _crossingAreaMatchCondition;
		private AreaAttributeConstraint _crossingAreaAttributeConstraint;

		private bool _isCrossingAreaAttributeConstraintSymmetric =
			_defaultIsCrossingAreaAttributeConstraintSymmetric;

		private const bool _defaultIsCrossingAreaAttributeConstraintSymmetric = false;
		private const bool _defaultAllowNoFeatureWithinSearchDistance = false;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident = false;

		private const bool
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled = false;

		private string _crossingAreaEqualAttributes;
		private EqualFieldValuesCondition _crossingAreaEqualFieldValuesCondition;
		private BufferFactory _bufferFactory;
		private IList<string> _crossingAreaEqualAttributeOptions;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NoMatch_NoCandidate = "NoMatch.NoCandidate";

			public const string NoMatch_NoCandidate_PartlyOutsideVerifiedExtent =
				"NoMatch.NoCandidate.PartlyOutsideVerifiedExtent";

			public const string
				NoMatch_CandidateExists_BordersNotCoincident_ConstraintsFulfilled =
					"NoMatch.CandidateExists.BordersNotCoincident.ConstraintsFulfilled";

			public const string
				NoMatch_CandidateExists_BordersNotCoincident_ConstraintsNotFulfilled =
					"NoMatch.CandidateExists.BordersNotCoincident.ConstraintsNotFulfilled";

			public const string Match_ConstraintsNotFulfilled =
				"Match.ConstraintsNotFulfilled";

			public Code() : base("CrossingAreas") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_0))]
		public QaEdgeMatchCrossingAreas(
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_areaClass1))] [NotNull]
			IReadOnlyFeatureClass areaClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_borderClass1))] [NotNull]
			IReadOnlyFeatureClass
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_areaClass2))] [NotNull]
			IReadOnlyFeatureClass areaClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_borderClass2))] [NotNull]
			IReadOnlyFeatureClass
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_searchDistance))]
			double
				searchDistance,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_boundingClasses1))] [CanBeNull]
			IList<IReadOnlyFeatureClass>
				boundingClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_boundingClasses2))] [CanBeNull]
			IList<IReadOnlyFeatureClass>
				boundingClasses2)
			: this(new[] {areaClass1}, borderClass1,
			       new[] {areaClass2}, borderClass2,
			       searchDistance, boundingClasses1, boundingClasses2) { }

		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_1))]
		public QaEdgeMatchCrossingAreas(
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_areaClasses1))] [NotNull]
			IList<IReadOnlyFeatureClass>
				areaClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_borderClass1))] [NotNull]
			IReadOnlyFeatureClass
				borderClass1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_areaClasses2))] [NotNull]
			IList<IReadOnlyFeatureClass>
				areaClasses2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_borderClass2))] [NotNull]
			IReadOnlyFeatureClass
				borderClass2,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_searchDistance))]
			double
				searchDistance,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_boundingClasses1))] [CanBeNull]
			IList<IReadOnlyFeatureClass>
				boundingClasses1,
			[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_boundingClasses2))] [CanBeNull]
			IList<IReadOnlyFeatureClass>
				boundingClasses2)
			: base(CastToTables(areaClasses1, new[] {borderClass1},
			                    areaClasses2, new[] {borderClass2},
			                    boundingClasses1 ?? new IReadOnlyFeatureClass[] { },
			                    boundingClasses2 ?? new IReadOnlyFeatureClass[] { }))
		{
			Assert.ArgumentNotNull(areaClasses1, nameof(areaClasses1));
			Assert.ArgumentNotNull(borderClass1, nameof(borderClass1));
			Assert.ArgumentNotNull(areaClasses2, nameof(areaClasses2));
			Assert.ArgumentNotNull(borderClass2, nameof(borderClass2));

			SearchDistance = searchDistance;

			foreach (IReadOnlyFeatureClass areaClass in Union(areaClasses1, areaClasses2))
			{
				Assert.ArgumentCondition(
					areaClass.ShapeType == esriGeometryType.esriGeometryPolygon,
					string.Format("Polygon feature class expected: {0}",
					              areaClass.Name));
			}

			foreach (IReadOnlyFeatureClass borderClass in new[] {borderClass1, borderClass2})
			{
				Assert.ArgumentCondition(
					borderClass.ShapeType == esriGeometryType.esriGeometryPolyline ||
					borderClass.ShapeType == esriGeometryType.esriGeometryPolygon,
					string.Format("Polyline or polygon feature class expected: {0}",
					              borderClass.Name));
			}

			_searchDistance = searchDistance;

			_areaClass1Indexes = new List<int>(areaClasses1.Count);
			for (var i = 0; i < areaClasses1.Count; i++)
			{
				_areaClass1Indexes.Add(i);
			}

			_borderClass1Index = areaClasses1.Count;

			_areaClass2Indexes = new List<int>(areaClasses2.Count);
			for (var i = 0; i < areaClasses2.Count; i++)
			{
				_areaClass2Indexes.Add(_borderClass1Index + 1 + i);
			}

			_allAreaClassIndexes = new List<int>();
			_allAreaClassIndexes.AddRange(_areaClass1Indexes);
			_allAreaClassIndexes.AddRange(_areaClass2Indexes);

			_borderClass2Index = _borderClass1Index + areaClasses2.Count + 1;

			_geometryTypesByTableIndex = GetGeometryTypesByTableIndex(InvolvedTables);
			_xyToleranceByTableIndex = GetXyToleranceByTableIndex(InvolvedTables);

			_boundingClass1Indexes = new List<int>();
			if (boundingClasses1 != null)
			{
				for (var i = 0; i < boundingClasses1.Count; i++)
				{
					_boundingClass1Indexes.Add(_borderClass2Index + 1 + i);
				}
			}

			_boundingClass2Indexes = new List<int>();
			if (boundingClasses2 != null)
			{
				for (var i = 0; i < boundingClasses2.Count; i++)
				{
					_boundingClass2Indexes.Add(_borderClass2Index + _boundingClass1Indexes.Count +
					                           1 +
					                           i);
				}
			}

			// defaults
			AllowNoFeatureWithinSearchDistance = _defaultAllowNoFeatureWithinSearchDistance;
			AllowDisjointCandidateFeatureIfBordersAreNotCoincident =
				_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident;

			AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled =
				_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled;
		}

		[InternallyUsedTest]
		public QaEdgeMatchCrossingAreas(
			[NotNull] QaEdgeMatchCrossingAreasDefinition definition)
			: this(definition.AreaClasses1.Cast<IReadOnlyFeatureClass>().ToList(),
			       (IReadOnlyFeatureClass)definition.BorderClass1,
			       definition.AreaClasses2.Cast<IReadOnlyFeatureClass>().ToList(),
			       (IReadOnlyFeatureClass)definition.BorderClass2,
			       definition.SearchDistance,
			       definition.BoundingClasses1.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.BoundingClasses2.Cast<IReadOnlyFeatureClass>().ToList())
		{
			AreaClass1BorderMatchCondition = definition.AreaClass1BorderMatchCondition;
			AreaClass1BoundingFeatureMatchCondition = definition.AreaClass1BoundingFeatureMatchCondition;
			AreaClass2BoundingFeatureMatchCondition = definition.AreaClass2BoundingFeatureMatchCondition;
			AreaClass2BorderMatchCondition = definition.AreaClass2BorderMatchCondition;
			CrossingAreaMatchCondition = definition.CrossingAreaMatchCondition;
			CrossingAreaAttributeConstraint = definition.CrossingAreaAttributeConstraint;
			IsCrossingAreaAttributeConstraintSymmetric =
				definition.IsCrossingAreaAttributeConstraintSymmetric;
			CrossingAreaEqualAttributes = definition.CrossingAreaEqualAttributes;
			CrossingAreaEqualAttributeOptions = definition.CrossingAreaEqualAttributeOptions;
			ReportIndividualAttributeConstraintViolations = definition.ReportIndividualAttributeConstraintViolations;
			AllowNoFeatureWithinSearchDistance = definition.AllowNoFeatureWithinSearchDistance;
			AllowDisjointCandidateFeatureIfBordersAreNotCoincident = definition
				.AllowDisjointCandidateFeatureIfBordersAreNotCoincident;
			AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled =
				definition.AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_AreaClass1BorderMatchCondition))]
		public string AreaClass1BorderMatchCondition
		{
			get { return _areaClass1BorderMatchConditionSql; }
			set
			{
				_areaClass1BorderMatchConditionSql = value;
				AddCustomQueryFilterExpression(value);
				_areaClass1BorderMatchCondition = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_AreaClass1BoundingFeatureMatchCondition))]
		public string AreaClass1BoundingFeatureMatchCondition
		{
			get { return _areaClass1BoundingFeatureMatchConditionSql; }
			set
			{
				_areaClass1BoundingFeatureMatchConditionSql = value;
				AddCustomQueryFilterExpression(value);
				_areaClass1BoundingFeatureMatchCondition = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_AreaClass2BoundingFeatureMatchCondition))]
		public string AreaClass2BoundingFeatureMatchCondition
		{
			get { return _areaClass2BoundingFeatureMatchConditionSql; }
			set
			{
				_areaClass2BoundingFeatureMatchConditionSql = value;
				AddCustomQueryFilterExpression(value);
				_areaClass2BoundingFeatureMatchCondition = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_AreaClass2BorderMatchCondition))]
		public string AreaClass2BorderMatchCondition
		{
			get { return _areaClass2BorderMatchConditionSql; }
			set
			{
				_areaClass2BorderMatchConditionSql = value;
				AddCustomQueryFilterExpression(value);
				_areaClass2BorderMatchCondition = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_CrossingAreaMatchCondition))]
		public string CrossingAreaMatchCondition
		{
			get { return _crossingAreaMatchConditionSql; }
			set
			{
				_crossingAreaMatchConditionSql = value;
				AddCustomQueryFilterExpression(value);
				_crossingAreaMatchCondition = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_CrossingAreaAttributeConstraint))]
		public string CrossingAreaAttributeConstraint
		{
			get { return _crossingAreaAttributeConstraintSql; }
			set
			{
				_crossingAreaAttributeConstraintSql = value;
				AddCustomQueryFilterExpression(value);
				_crossingAreaAttributeConstraint = null;
			}
		}

		[TestParameter(_defaultIsCrossingAreaAttributeConstraintSymmetric)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingAreas_IsCrossingAreaAttributeConstraintSymmetric))]
		public bool IsCrossingAreaAttributeConstraintSymmetric
		{
			get { return _isCrossingAreaAttributeConstraintSymmetric; }
			set
			{
				_isCrossingAreaAttributeConstraintSymmetric = value;
				_crossingAreaAttributeConstraint = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_CrossingAreaEqualAttributes))]
		public string CrossingAreaEqualAttributes
		{
			get { return _crossingAreaEqualAttributes; }
			set
			{
				_crossingAreaEqualAttributes = value;
				AddCustomQueryFilterExpression(value);
				_crossingAreaEqualFieldValuesCondition = null;
			}
		}

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_CrossingAreaEqualAttributeOptions))]
		public IList<string> CrossingAreaEqualAttributeOptions
		{
			get { return _crossingAreaEqualAttributeOptions; }
			set
			{
				_crossingAreaEqualAttributeOptions = value;
				if (value != null)
				{
					foreach (string option in value)
					{
						AddCustomQueryFilterExpression(option);
					}
				}
				_crossingAreaEqualFieldValuesCondition = null;
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingAreas_ReportIndividualAttributeConstraintViolations))]
		public bool ReportIndividualAttributeConstraintViolations { get; set; }

		[TestParameter(_defaultAllowNoFeatureWithinSearchDistance)]
		[Doc(nameof(DocStrings.QaEdgeMatchCrossingAreas_AllowNoFeatureWithinSearchDistance))]
		public bool AllowNoFeatureWithinSearchDistance { get; set; }

		[TestParameter(_defaultAllowDisjointCandidateFeatureIfBordersAreNotCoincident)]
		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingAreas_AllowDisjointCandidateFeatureIfBordersAreNotCoincident
		))]
		public bool AllowDisjointCandidateFeatureIfBordersAreNotCoincident { get; set; }

		[Doc(nameof(DocStrings
			            .QaEdgeMatchCrossingAreas_AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled
		))]
		[TestParameter(
			_defaultAllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)]
		public bool AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled { get; set; }

		protected override int CompleteTileCore(TileInfo tileInfo)
		{
			var errorCount = 0;

			if (tileInfo.State == TileState.Initial)
			{
				_borderConnectionUnion1.Clear();
				_borderConnectionUnion2.Clear();
				_borderConnectionCache.Clear();
				_constraintErrors.Clear();
			}
			else
			{
				errorCount += CompareBorderConnectionList(_borderConnectionUnion1, tileInfo);
				errorCount += CompareBorderConnectionList(_borderConnectionUnion2, tileInfo);

				errorCount += ReportConstraintErrors(_constraintErrors, tileInfo);

				if (tileInfo.CurrentEnvelope != null)
				{
					WKSEnvelope tileWksBox = _tileWksEnvelope;
					WKSEnvelope allWksBox = _allWksEnvelope;

					_borderConnectionCache.Clear(tileWksBox, allWksBox);
					_constraintErrors.Clear(tileWksBox, allWksBox);

					_borderConnectionUnion1.Clear(tileWksBox, allWksBox);
					_borderConnectionUnion2.Clear(tileWksBox, allWksBox);
				}
			}

			return errorCount + base.CompleteTileCore(tileInfo);
		}

		protected override void BeginTileCore(BeginTileParameters parameters)
		{
			_tileEnvelope = parameters.TileEnvelope;

			if (_tileEnvelope != null)
			{
				_tileEnvelope.QueryWKSCoords(out _tileWksEnvelope);
			}

			if (parameters.TestRunEnvelope != null)
			{
				parameters.TestRunEnvelope.QueryWKSCoords(out _allWksEnvelope);
			}

			// _reportedRowPairs.Clear();
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
				if (_spatialReference != null)
				{
					_highResolutionSpatialReference =
						SpatialReferenceUtils.CreateSpatialReferenceWithMinimumTolerance(
							_spatialReference, 10);
				}
			}

			int borderLineClassIndex;
			int neighborBorderLineClassIndex;
			ICollection<int> neighborAreaClassIndexes = GetNeighborAreaClassIndexes(
				tableIndex,
				out borderLineClassIndex,
				out neighborBorderLineClassIndex);

			if (neighborAreaClassIndexes == null)
			{
				return NoError;
			}

			Assert.True(borderLineClassIndex >= 0, "Unexpected border line class index");
			Assert.True(neighborBorderLineClassIndex >= 0,
			            "Unexpected neighboring border line class index");

			CheckConnections(feature, tableIndex,
			                 borderLineClassIndex,
			                 neighborAreaClassIndexes,
			                 neighborBorderLineClassIndex);

			// Errors determined in CompleteTileCore
			return NoError;
		}

		[NotNull]
		private static IDictionary<int, double> GetXyToleranceByTableIndex(
			[NotNull] ICollection<IReadOnlyTable> involvedTables)
		{
			var result = new Dictionary<int, double>(involvedTables.Count);

			var index = 0;
			foreach (IReadOnlyTable table in involvedTables)
			{
				var featureClass = table as IReadOnlyFeatureClass;

				double xyTolerance;
				if (featureClass == null ||
				    ! DatasetUtils.TryGetXyTolerance(featureClass.SpatialReference,
				                                     out xyTolerance))
				{
					xyTolerance = 0;
				}

				result.Add(index, xyTolerance);
				index++;
			}

			return result;
		}

		[NotNull]
		private static IDictionary<int, esriGeometryType> GetGeometryTypesByTableIndex(
			[NotNull] ICollection<IReadOnlyTable> involvedTables)
		{
			var result = new Dictionary<int, esriGeometryType>(involvedTables.Count);

			var index = 0;
			foreach (IReadOnlyTable table in involvedTables)
			{
				result.Add(index, ((IReadOnlyFeatureClass) table).ShapeType);
				index++;
			}

			return result;
		}

		[CanBeNull]
		private ICollection<int> GetNeighborAreaClassIndexes(
			int tableIndex,
			out int borderLineClassIndex,
			out int neighborBorderLineClassIndex)
		{
			if (IsAreaClass1(tableIndex))
			{
				borderLineClassIndex = _borderClass1Index;
				neighborBorderLineClassIndex = _borderClass2Index;
				return _areaClass2Indexes;
			}

			if (IsAreaClass2(tableIndex))
			{
				borderLineClassIndex = _borderClass2Index;
				neighborBorderLineClassIndex = _borderClass1Index;
				return _areaClass1Indexes;
			}

			// it's one of the border line classes --> ignore
			borderLineClassIndex = -1;
			neighborBorderLineClassIndex = -1;
			return null;
		}

		private void CheckConnections([NotNull] IReadOnlyFeature feature,
		                              int areaClassIndex,
		                              int borderLineClassIndex,
		                              [NotNull] ICollection<int> neighborAreaClassIndexes,
		                              int neighborBorderLineClassIndex)
		{
			// determine if the feature connects to the border 
			var area = (IPolygon) feature.Shape;

			if (area.IsEmpty)
			{
				return;
			}

			IEnumerable<BorderConnection> borderConnections =
				GetBorderConnections(area, feature, areaClassIndex, borderLineClassIndex);
			BorderConnectionUnion borderUnion =
				Assert.NotNull(GetBorderConnectionUnion(areaClassIndex));

			EnsureCrossingAreaMatchCondition();
			EnsureCrossingAreaAttributeConstraint();
			EnsureCrossingAreaEqualFieldValuesCondition();

			// search neighboring features (within search distance, also connected to THEIR border)
			foreach (BorderConnection borderConnection in borderConnections)
			{
				NeighborsAndBounds neighborsAndBounds =
					borderUnion.GetNeighborsAndBounds(borderConnection);
				CheckBorderConnection(borderConnection, neighborsAndBounds,
				                      neighborAreaClassIndexes,
				                      neighborBorderLineClassIndex,
				                      _crossingAreaMatchCondition);

				CollectBoundingGeometries(borderConnection, neighborsAndBounds);
			}
		}

		[CanBeNull]
		private BorderConnectionUnion GetBorderConnectionUnion(int areaClassIndex)
		{
			if (IsAreaClass1(areaClassIndex))
			{
				return _borderConnectionUnion1;
			}

			if (IsAreaClass2(areaClassIndex))
			{
				return _borderConnectionUnion2;
			}

			return null;
		}

		private void EnsureCrossingAreaEqualFieldValuesCondition()
		{
			if (_crossingAreaEqualFieldValuesCondition != null)
			{
				return;
			}

			_crossingAreaEqualFieldValuesCondition =
				new EqualFieldValuesCondition(_crossingAreaEqualAttributes,
				                              _crossingAreaEqualAttributeOptions,
				                              GetTables(_allAreaClassIndexes),
				                              GetSqlCaseSensitivity(_allAreaClassIndexes));
		}

		private void EnsureCrossingAreaAttributeConstraint()
		{
			if (_crossingAreaAttributeConstraint != null)
			{
				return;
			}

			_crossingAreaAttributeConstraint = new AreaAttributeConstraint(
				_crossingAreaAttributeConstraintSql,
				GetSqlCaseSensitivity(_allAreaClassIndexes),
				! IsCrossingAreaAttributeConstraintSymmetric);
		}

		private void EnsureCrossingAreaMatchCondition()
		{
			if (_crossingAreaMatchCondition != null)
			{
				return;
			}

			_crossingAreaMatchCondition =
				new AreaMatchCondition(_crossingAreaMatchConditionSql,
				                       GetSqlCaseSensitivity(_allAreaClassIndexes));
		}

		[NotNull]
		private IEnumerable<IReadOnlyTable> GetTables([NotNull] IEnumerable<int> tableIndexes)
		{
			var result = new SimpleSet<int>();
			foreach (int tableIndex in tableIndexes)
			{
				if (result.Contains(tableIndex))
				{
					continue;
				}

				result.Add(tableIndex);
				yield return InvolvedTables[tableIndex];
			}
		}

		[NotNull]
		private IFeatureClassFilter GetSearchFilter(int tableIndex,
		                                            [NotNull] IPolyline line,
		                                            double searchDistance)
		{
			IFeatureClassFilter filter = _filters[tableIndex];

			WKSEnvelope envelope = ProxyUtils.GetWKSEnvelope(line);

			envelope.XMin -= searchDistance;
			envelope.XMax += searchDistance;
			envelope.YMin -= searchDistance;
			envelope.YMax += searchDistance;

			_searchEnvelopeTemplate.PutWKSCoords(envelope);

			filter.FilterGeometry = _searchEnvelopeTemplate;

			return filter;
		}

		private void CheckBorderConnection(
			[NotNull] BorderConnection borderConnection,
			NeighborsAndBounds neighborsAndBounds,
			[NotNull] IEnumerable<int> neighborAreaClassIndexes,
			int neighborBorderLineClassIndex,
			[NotNull] AreaMatchCondition crossingAreaMatchCondition)
		{
			if (borderConnection.UncoveredBoundary == null ||
			    borderConnection.UncoveredBoundary.IsEmpty)
			{
				return;
			}

			foreach (int neighborAreaClassIndex in neighborAreaClassIndexes)
			{
				foreach (IReadOnlyFeature neighborFeature in
				         SearchNeighborRows(borderConnection.AreaBoundaryAlongBorder,
				                            neighborAreaClassIndex))
				{
					if (! crossingAreaMatchCondition.IsFulfilled(borderConnection.Feature,
					                                             borderConnection.ClassIndex,
					                                             neighborFeature,
					                                             neighborAreaClassIndex))
					{
						continue;
					}

					var neighborArea = (IPolygon) neighborFeature.Shape;

					double distanceToNeighborArea =
						GetDistance(borderConnection.AreaBoundaryAlongBorder,
						            neighborArea);

					if (distanceToNeighborArea > _searchDistance)
					{
						// the line is outside the search distance
						continue;
					}

					// determine if the neighbor area is connected to it's border
					IEnumerable<BorderConnection> neighborBorderConnections =
						GetBorderConnections(neighborArea, neighborFeature, neighborAreaClassIndex,
						                     neighborBorderLineClassIndex);

					foreach (BorderConnection neighborBorderConnection in neighborBorderConnections)
					{
						CheckIsNeighborEqual(borderConnection,
						                     neighborBorderConnection,
						                     neighborsAndBounds);
					}
				}
			}
		}

		private void CheckIsNeighborEqual(
			[NotNull] BorderConnection borderConnection,
			[NotNull] BorderConnection neighborConnection,
			[NotNull] NeighborsAndBounds neighborsAndBounds)
		{
			if (neighborConnection.AreaBoundaryAlongBorder.IsEmpty)
			{
				return;
			}

			if (neighborsAndBounds.ContainsAny(neighborConnection))
			{
				return;
			}

			IPolyline areaBoundaryAlongBorder = borderConnection.AreaBoundaryAlongBorder;
			IPolyline neighborAreaBoundaryAlongBorder =
				neighborConnection.AreaBoundaryAlongBorder;

			IPolyline commonBorder = EdgeMatchUtils.GetCommonBorder(
				areaBoundaryAlongBorder,
				neighborAreaBoundaryAlongBorder,
				_xyToleranceByTableIndex[borderConnection.ClassIndex]);

			if (! commonBorder.IsEmpty)
			{
				foreach (AttributeConstraintViolation constraintViolation in
				         GetAttributeConstraintViolations(borderConnection, neighborConnection))
				{
					AddConstraintError(borderConnection, neighborConnection,
					                   GeometryFactory.Clone(commonBorder),
					                   Codes[Code.Match_ConstraintsNotFulfilled],
					                   constraintViolation.Description,
					                   constraintViolation.AffectedComponents,
					                   constraintViolation.TextValue);
				}
			}

			if (_searchDistance > 0)
			{
				IPolyline notEqualLine =
					GetNotEqualLine(commonBorder,
					                areaBoundaryAlongBorder,
					                neighborAreaBoundaryAlongBorder,
					                _xyToleranceByTableIndex[borderConnection.ClassIndex]);

				if (notEqualLine != null && ! notEqualLine.IsEmpty)
				{
					var neighborWithGap = new NeighborConnection(neighborConnection,
					                                             notEqualLine,
					                                             isGap: true);
					neighborsAndBounds.AddNeighbor(neighborWithGap);
				}
			}

			if (! commonBorder.IsEmpty)
			{
				var neighborExactMatch = new NeighborConnection(neighborConnection,
				                                                commonBorder);
				neighborsAndBounds.AddNeighbor(neighborExactMatch);
			}
		}

		[NotNull]
		private ICollection<AttributeConstraintViolation> GetAttributeConstraintViolations(
			[NotNull] BorderConnection borderConnection,
			[NotNull] BorderConnection neighborConnection)
		{
			return EdgeMatchUtils.GetAttributeConstraintViolations(
				borderConnection.Feature, borderConnection.ClassIndex,
				neighborConnection.Feature, neighborConnection.ClassIndex,
				_crossingAreaAttributeConstraint,
				_crossingAreaEqualFieldValuesCondition,
				ReportIndividualAttributeConstraintViolations).ToList();
		}

		private void AddConstraintError([NotNull] BorderConnection borderConnection,
		                                [NotNull] BorderConnection neighborConnection,
		                                [NotNull] IPolyline commonLine,
		                                [CanBeNull] IssueCode code,
		                                [NotNull] string constraintDescription,
		                                [CanBeNull] string affectedComponents,
		                                [CanBeNull] string textValue)
		{
			BorderConnection areaClass1Connection;
			BorderConnection areaClass2Connection;
			if (IsAreaClass1(borderConnection.ClassIndex))
			{
				areaClass1Connection = borderConnection;
				areaClass2Connection = neighborConnection;
			}
			else
			{
				areaClass2Connection = borderConnection;
				areaClass1Connection = neighborConnection;
			}

			_constraintErrors.Add(areaClass1Connection, areaClass2Connection, commonLine, code,
			                      constraintDescription, affectedComponents, textValue);
		}

		[CanBeNull]
		private IPolyline GetNotEqualLine(
			[NotNull] IPolyline commonBorder,
			[NotNull] IPolyline areaBoundaryAlongBorder,
			[NotNull] IPolyline neighborAreaBoundaryAlongBorder,
			double xyTolerance)
		{
			return EdgeMatchUtils.GetNotEqualLine(commonBorder, areaBoundaryAlongBorder,
			                                      neighborAreaBoundaryAlongBorder,
			                                      _searchDistance,
			                                      ref _bufferFactory,
			                                      xyTolerance);
		}

		[CanBeNull]
		private IPolyline GetNearPart([NotNull] IPolyline toBuffer,
		                              [NotNull] IPolyline line)
		{
			return EdgeMatchUtils.GetNearPart(toBuffer, line, _searchDistance,
			                                  ref _bufferFactory);
		}

		private void CollectBoundingGeometries(
			[NotNull] BorderConnection borderConnection,
			[NotNull] NeighborsAndBounds neighborsAndBounds)
		{
			int areaClassIndex = borderConnection.ClassIndex;

			IPolyline areaBoundaryAlongBorder = borderConnection.AreaBoundaryAlongBorder;

			var boundaryAlongBorderRelOp = (IRelationalOperator) areaBoundaryAlongBorder;

			BoundingFeatureMatchCondition boundingFeatureMatchCondition =
				GetBoundingFeatureMatchCondition(areaClassIndex);

			foreach (int boundingClassIndex in GetBoundingClassIndexes(areaClassIndex))
			{
				foreach (IReadOnlyFeature boundingFeature in
				         SearchBoundingRows(areaBoundaryAlongBorder, boundingClassIndex))
				{
					if (! boundingFeatureMatchCondition.IsFulfilled(
						    borderConnection.Feature, borderConnection.ClassIndex,
						    boundingFeature, boundingClassIndex))
					{
						continue;
					}

					if (! boundaryAlongBorderRelOp.Disjoint(boundingFeature.Shape))
					{
						neighborsAndBounds.AddBoundingGeometry(boundingFeature.ShapeCopy);
					}
				}
			}
		}

		[NotNull]
		private IEnumerable<int> GetBoundingClassIndexes(int areaClassIndex)
		{
			if (IsAreaClass1(areaClassIndex))
			{
				return _boundingClass1Indexes;
			}

			if (IsAreaClass2(areaClassIndex))
			{
				return _boundingClass2Indexes;
			}

			throw new InvalidOperationException("Not an area class index");
		}

		private static double GetDistance([NotNull] IPolyline polyline,
		                                  [NotNull] IPolygon polygon)
		{
			var proximity = (IProximityOperator) polygon;

			return proximity.ReturnDistance(polyline);
		}

		[NotNull]
		private IEnumerable<IReadOnlyFeature> SearchNeighborRows([NotNull] IPolyline borderLine,
		                                                         int neighborLineClassIndex)
		{
			IFeatureClassFilter spatialFilter = GetSearchFilter(neighborLineClassIndex,
			                                                    borderLine,
			                                                    _searchDistance);

			QueryFilterHelper filterHelper = _filterHelpers[neighborLineClassIndex];

			bool origForNetwork = filterHelper.ForNetwork;
			IEnumerable<IReadOnlyFeature> features;
			try
			{
				features =
					Search(InvolvedTables[neighborLineClassIndex], spatialFilter, filterHelper)
						.Cast<IReadOnlyFeature>();
			}
			finally
			{
				filterHelper.ForNetwork = origForNetwork;
			}

			return features;
		}

		[NotNull]
		private IEnumerable<IReadOnlyFeature> SearchBoundingRows([NotNull] IPolyline borderLine,
		                                                         int boundingClassIndex)
		{
			IFeatureClassFilter spatialFilter = GetSearchFilter(boundingClassIndex,
			                                                    borderLine,
			                                                    searchDistance: 0);

			return Search(InvolvedTables[boundingClassIndex],
			              spatialFilter,
			              _filterHelpers[boundingClassIndex])
				.Cast<IReadOnlyFeature>();
		}

		[NotNull]
		private IEnumerable<BorderConnection> GetBorderConnections(
			[NotNull] IPolygon area,
			[NotNull] IReadOnlyFeature areaFeature,
			int areaClassIndex,
			int borderClassIndex)
		{
			QueryFilterHelper borderClassFilterHelper = _filterHelpers[borderClassIndex];
			bool origForNetwork = borderClassFilterHelper.ForNetwork;
			try
			{
				borderClassFilterHelper.ForNetwork = true;
				return _borderConnectionCache.GetBorderConnections(
					area, areaFeature, areaClassIndex, borderClassIndex,
					InvolvedTables[borderClassIndex],
					_filters[borderClassIndex],
					borderClassFilterHelper,
					(table, filter, filterHelper) => Search(table, filter, filterHelper),
					GetBorderMatchCondition(areaClassIndex));
			}
			finally
			{
				borderClassFilterHelper.ForNetwork = origForNetwork;
			}
		}

		[NotNull]
		private BorderMatchCondition GetBorderMatchCondition(int areaClassIndex)
		{
			if (IsAreaClass1(areaClassIndex))
			{
				return _areaClass1BorderMatchCondition ??
				       (_areaClass1BorderMatchCondition =
					        new BorderMatchCondition(_areaClass1BorderMatchConditionSql,
					                                 GetSqlCaseSensitivity(areaClassIndex,
						                                 _borderClass1Index)));
			}

			if (IsAreaClass2(areaClassIndex))
			{
				return _areaClass2BorderMatchCondition ??
				       (_areaClass2BorderMatchCondition =
					        new BorderMatchCondition(_areaClass2BorderMatchConditionSql,
					                                 GetSqlCaseSensitivity(areaClassIndex,
						                                 _borderClass2Index)));
			}

			throw new ArgumentException("Not an area class index");
		}

		[NotNull]
		private BoundingFeatureMatchCondition GetBoundingFeatureMatchCondition(
			int areaClassIndex)
		{
			if (IsAreaClass1(areaClassIndex))
			{
				return _areaClass1BoundingFeatureMatchCondition ??
				       (_areaClass1BoundingFeatureMatchCondition =
					        new BoundingFeatureMatchCondition(
						        _areaClass1BoundingFeatureMatchConditionSql,
						        GetSqlCaseSensitivity(
							        _boundingClass1Indexes.Union(new[] {areaClassIndex}))));
			}

			if (IsAreaClass2(areaClassIndex))
			{
				return _areaClass2BoundingFeatureMatchCondition ??
				       (_areaClass2BoundingFeatureMatchCondition =
					        new BoundingFeatureMatchCondition(
						        _areaClass2BoundingFeatureMatchConditionSql,
						        GetSqlCaseSensitivity(
							        _boundingClass2Indexes.Union(new[] {areaClassIndex}))));
			}

			throw new ArgumentException("Not an area class index");
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
						$"Unexpected border geometry type: {borderGeometryType}");
			}
		}

		private bool IsAreaClass1(int tableIndex)
		{
			return tableIndex < _borderClass1Index;
		}

		private bool IsAreaClass2(int tableIndex)
		{
			return tableIndex > _borderClass1Index && tableIndex < _borderClass2Index;
		}

		private int ReportConstraintErrors(
			[NotNull] ConstraintErrorCache constraintErrors,
			[NotNull] TileInfo tileInfo)
		{
			if (tileInfo.State == TileState.Initial)
			{
				constraintErrors.Clear();
				return NoError;
			}

			int errorCount = NoError;

			WKSEnvelope tileWksBox = _tileWksEnvelope;
			WKSEnvelope allWksBox = _allWksEnvelope;

			var commonErrors = new List<ConstraintError>();

			foreach (ConstraintError error in constraintErrors.GetSortedErrors())
			{
				if (commonErrors.Count > 0 &&
				    constraintErrors.Compare(error, commonErrors[0]) != 0)
				{
					errorCount += ReportConstraintErrors(commonErrors, tileInfo, tileWksBox,
					                                     allWksBox);
					commonErrors.Clear();
				}

				commonErrors.Add(error);
			}

			errorCount +=
				ReportConstraintErrors(commonErrors, tileInfo, tileWksBox, allWksBox);

			return errorCount;
		}

		private int ReportConstraintErrors(
			[NotNull] IList<ConstraintError> constraintErrors,
			[NotNull] TileInfo tileInfo,
			WKSEnvelope tileWksBox,
			WKSEnvelope allWksBox)
		{
			if (constraintErrors.Count == 0)
			{
				return NoError;
			}

			ConstraintError first = constraintErrors[0];
			IReadOnlyFeature areaBorderFeature = first.BorderConnection.Feature;
			IReadOnlyFeature neighborFeature = first.NeighborBorderConnection.Feature;

			if (tileInfo.State != TileState.Final &&
			    (! EdgeMatchUtils.VerifyHandled(areaBorderFeature, tileWksBox, allWksBox) ||
			     ! EdgeMatchUtils.VerifyHandled(neighborFeature, tileWksBox, allWksBox)))
			{
				return NoError;
			}

			List<IPolyline> commonLines = constraintErrors.Select(error => error.ErrorLine)
			                                              .ToList();

			IPolyline errorGeometry = EdgeMatchUtils.Union(commonLines, null);

			string description;
			if (first.IssueCode.ID == Code.Match_ConstraintsNotFulfilled)
			{
				description =
					string.Format(
						LocalizableStrings.QaEdgeMatchCrossingAreas_Match_ConstraintsNotFulfilled,
						FormatLength(errorGeometry.Length, _spatialReference).Trim(),
						first.ConstraintDescription);
			}
			else
			{
				description = first.ConstraintDescription;
			}

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(areaBorderFeature, neighborFeature),
				GeometryFactory.Clone(errorGeometry), first.IssueCode, first.AffectedComponents,
				values: new[] {first.TextValue});
		}

		private int CompareBorderConnectionList(
			[NotNull] BorderConnectionUnion borderConnectionUnion,
			[NotNull] TileInfo tileInfo)
		{
			if (tileInfo.State == TileState.Initial)
			{
				borderConnectionUnion.Clear();
				return NoError;
			}

			int errorCount = NoError;

			// IPolyline completeBorder = borderConnectionUnion.CompleteBorder;
			WKSEnvelope tileWksBox = _tileWksEnvelope;
			WKSEnvelope allWksBox = _allWksEnvelope;

			IPolyline completeBoundary = borderConnectionUnion.GetCompleteBorder(tileInfo);

			IPolyline unmatchedBoundary =
				borderConnectionUnion.GetUnmatchedBoundary(completeBoundary);
			if (unmatchedBoundary == null || unmatchedBoundary.IsEmpty)
			{
				return errorCount;
			}

			IEnvelope tileEnvelope = Assert.NotNull(tileInfo.CurrentEnvelope);

			foreach (NeighborsAndBounds neighbors in borderConnectionUnion.Neighbors)
			{
				BorderConnection borderConnection = neighbors.BorderConnection;

				if (tileInfo.State != TileState.Final &&
				    ((IRelationalOperator) tileEnvelope).Disjoint(
					    borderConnection.AreaBoundaryAlongBorder.Envelope))
				{
					// will be handled in a later tile
					continue;
				}

				if (EdgeMatchUtils.IsDisjoint(borderConnection.AreaBoundaryAlongBorder,
				                              unmatchedBoundary))
				{
					// The border connection is completely matched by neighbor features
					borderConnection.UncoveredBoundary =
						GeometryFactory.CreateEmptyPolyline(
							borderConnection.AreaBoundaryAlongBorder);
					continue;
				}

				IPolyline uncoveredAreaBoundaryAlongBorder =
					EdgeMatchUtils.GetLinearIntersection(
						borderConnection.UncoveredBoundary,
						unmatchedBoundary,
						_xyToleranceByTableIndex[borderConnection.ClassIndex]);

				borderConnection.UncoveredBoundary = uncoveredAreaBoundaryAlongBorder;

				if (uncoveredAreaBoundaryAlongBorder.IsEmpty)
				{
					continue;
				}

				var incompleteGapErrorLines = new List<IPolyline>();
				var handledGapErrorLines = new List<IPolyline>();

				foreach (NeighborConnection neighbor in neighbors.NeighborConnections)
				{
					if (! neighbor.IsGap)
					{
						// this cannot be a mismatch error
						continue;
					}

					if (EdgeMatchUtils.IsDisjoint(neighbor.CommonLine,
					                              uncoveredAreaBoundaryAlongBorder))
					{
						continue;
					}

					IPolyline gapErrorLine =
						EdgeMatchUtils.GetLinearIntersection(
							uncoveredAreaBoundaryAlongBorder,
							neighbor.CommonLine,
							_xyToleranceByTableIndex[borderConnection.ClassIndex]);

					if (gapErrorLine.IsEmpty)
					{
						continue;
					}

					if (tileInfo.State != TileState.Final &&
					    ! EdgeMatchUtils.VerifyHandled(gapErrorLine, tileWksBox, allWksBox))
					{
						incompleteGapErrorLines.Add(gapErrorLine);
						continue;
					}

					handledGapErrorLines.Add(gapErrorLine);

					BorderConnection neighborConnection = neighbor.NeighborBorderConnection;

					if (! AllowDisjointCandidateFeatureIfBordersAreNotCoincident)
					{
						IPolyline errorLine = GetGapErrorGeometry(gapErrorLine, neighborConnection);

						// TODO really test this again? don't report if already reported before (i.e. when common border is not empty?)
						ICollection<AttributeConstraintViolation> constraintViolations =
							GetAttributeConstraintViolations(borderConnection, neighborConnection);

						foreach (AttributeConstraintViolation constraintViolation in
						         constraintViolations)
						{
							// description has final period
							AddConstraintError(
								borderConnection, neighborConnection, errorLine,
								Codes[
									Code
										.NoMatch_CandidateExists_BordersNotCoincident_ConstraintsNotFulfilled],
								$"{LocalizableStrings.QaEdgeMatchCrossingAreas_NoMatch_CandidateExists} {constraintViolation.Description}.",
								constraintViolation.AffectedComponents,
								constraintViolation.TextValue);
						}

						if (constraintViolations.Count == 0 &&
						    ! AllowDisjointCandidateFeatureIfAttributeConstraintsAreFulfilled)
						{
							AddConstraintError(
								borderConnection, neighborConnection, errorLine,
								Codes[
									Code
										.NoMatch_CandidateExists_BordersNotCoincident_ConstraintsFulfilled],
								LocalizableStrings
									.QaEdgeMatchCrossingAreas_NoMatch_CandidateExists, null, null);
						}
					}
				}

				foreach (IPolyline gapErrorLine in handledGapErrorLines)
				{
					uncoveredAreaBoundaryAlongBorder = EdgeMatchUtils.GetDifference(
						uncoveredAreaBoundaryAlongBorder, gapErrorLine,
						_xyToleranceByTableIndex[borderConnection.ClassIndex]);
				}

				borderConnection.UncoveredBoundary = uncoveredAreaBoundaryAlongBorder;

				if (! AllowNoFeatureWithinSearchDistance)
				{
					foreach (IPolyline incompleteGapErrorLine in incompleteGapErrorLines)
					{
						uncoveredAreaBoundaryAlongBorder = EdgeMatchUtils.GetDifference(
							uncoveredAreaBoundaryAlongBorder, incompleteGapErrorLine,
							_xyToleranceByTableIndex[borderConnection.ClassIndex]);
					}
				}

				if (! AllowNoFeatureWithinSearchDistance &&
				    ! uncoveredAreaBoundaryAlongBorder.IsEmpty)
				{
					var uncoveredParts = (IGeometryCollection) uncoveredAreaBoundaryAlongBorder;

					int partCount = uncoveredParts.GeometryCount;
					for (var partIndex = 0; partIndex < partCount; partIndex++)
					{
						IPolyline uncoveredPart =
							GeometryFactory.CreatePolyline(uncoveredParts.Geometry[partIndex]);

						if (IsBoundedByFeature(uncoveredPart, neighbors))
						{
							continue;
						}

						WKSEnvelope uncoveredBox = ProxyUtils.GetWKSEnvelope(uncoveredPart);

						// TODO revise
						if (uncoveredBox.XMax < tileWksBox.XMin ||
						    uncoveredBox.YMax < tileWksBox.YMin)
						{
							continue;
						}

						if (tileInfo.State != TileState.Final &&
						    (uncoveredBox.XMax >= tileWksBox.XMax ||
						     uncoveredBox.YMax >= tileWksBox.YMax))
						{
							continue;
						}

						IEnvelope verificationExtent = Assert.NotNull(tileInfo.AllBox);

						bool partlyOutside =
							! ((IRelationalOperator) verificationExtent).Contains(uncoveredPart);

						string codeId;
						string description;
						if (partlyOutside)
						{
							codeId = Code.NoMatch_NoCandidate_PartlyOutsideVerifiedExtent;
							description = string.Format(
								"{0}. {1}.",
								LocalizableStrings.QaEdgeMatchCrossingAreas_NoMatch_NoCandidate,
								LocalizableStrings
									.QaEdgeMatchCrossingAreas_PartlyOutsideVerifiedExtent);
						}
						else
						{
							codeId = Code.NoMatch_NoCandidate;
							description = LocalizableStrings
								.QaEdgeMatchCrossingAreas_NoMatch_NoCandidate;
						}

						errorCount += ReportError(
							description, InvolvedRowUtils.GetInvolvedRows(borderConnection.Feature),
							uncoveredPart, Codes[codeId], null);
					}
				}
			}

			return errorCount;
		}

		private static bool IsBoundedByFeature([NotNull] IPolyline uncoveredPart,
		                                       [NotNull] NeighborsAndBounds neighbors)
		{
			foreach (IGeometry boundingGeometry in neighbors.BoundingGeometries)
			{
				if (! EdgeMatchUtils.IsDisjoint(boundingGeometry, uncoveredPart))
				{
					return true;
				}
			}

			return false;
		}

		[NotNull]
		private IPolyline GetGapErrorGeometry([NotNull] IPolyline gapErrorLine,
		                                      [NotNull] BorderConnection neighborConnection)
		{
			IPolyline gapNeighborLine = GetNearPart(
				gapErrorLine, neighborConnection.AreaBoundaryAlongBorder);

			if (gapNeighborLine == null)
			{
				return GeometryFactory.Clone(gapErrorLine);
			}

			return EdgeMatchUtils.Union(new[] {gapErrorLine, gapNeighborLine},
			                            _highResolutionSpatialReference);
		}

		#region Nested types

		private class NeighborConnection : EdgeMatchNeighborConnection<BorderConnection>
		{
			public NeighborConnection([NotNull] BorderConnection neighborBorderConnection,
			                          [NotNull] IPolyline commonLine, bool isGap = false)
				: base(neighborBorderConnection, commonLine, isGap) { }
		}

		private class NeighborsAndBounds :
			EdgeMatchNeighbors<NeighborConnection, BorderConnection>
		{
			private readonly List<IGeometry> _boundingGeometries = new List<IGeometry>();

			public NeighborsAndBounds([NotNull] BorderConnection borderConnection)
				: base(borderConnection) { }

			public void AddBoundingGeometry(IGeometry geometry)
			{
				_boundingGeometries.Add(geometry);
			}

			[NotNull]
			public IEnumerable<IGeometry> BoundingGeometries => _boundingGeometries;
		}

		private class BorderConnectionUnion :
			EdgeMatchBorderConnectionUnion
			<NeighborsAndBounds, NeighborConnection, BorderConnection>
		{
			[NotNull]
			protected override NeighborsAndBounds CreateNeighbors(
				[NotNull] BorderConnection borderConnection)
			{
				return new NeighborsAndBounds(borderConnection);
			}

			[NotNull]
			public NeighborsAndBounds GetNeighborsAndBounds(
				[NotNull] BorderConnection borderConnection)
			{
				return GetNeighbors(borderConnection);
			}
		}

		private class BorderConnectionCache :
			EdgeMatchBorderConnectionCache<BorderConnection>
		{
			protected override BorderConnection
				CreateBorderConnection(IReadOnlyFeature feature, int featureClassIndex,
				                       IReadOnlyFeature borderFeature, int borderClassIndex,
				                       IPolyline lineAlongBorder, IPolyline uncoveredLine)
			{
				var created = new BorderConnection(feature, featureClassIndex,
				                                   borderFeature, borderClassIndex,
				                                   lineAlongBorder, uncoveredLine);
				return created;
			}

			protected override bool VerifyHandled([NotNull] BorderConnection borderConnection,
			                                      WKSEnvelope tileWksBox,
			                                      WKSEnvelope allWksBox)
			{
				return
					EdgeMatchUtils.VerifyHandled(borderConnection.Feature, tileWksBox, allWksBox) ||
					EdgeMatchUtils.VerifyHandled(borderConnection.BorderFeature, tileWksBox,
					                             allWksBox);
			}
		}

		private class BorderConnection : EdgeMatchSingleBorderConnection
		{
			public BorderConnection([NotNull] IReadOnlyFeature areaFeature,
			                        int areaClassIndex,
			                        [NotNull] IReadOnlyFeature borderFeature,
			                        int borderClassIndex,
			                        [NotNull] IPolyline areaBoundaryAlongBorder,
			                        [NotNull] IPolyline uncoveredBoundary)
				: base(
					areaFeature, areaClassIndex, borderFeature, borderClassIndex,
					areaBoundaryAlongBorder)
			{
				Assert.ArgumentNotNull(borderFeature, nameof(borderFeature));

				UncoveredBoundary = uncoveredBoundary;
			}

			[NotNull]
			public IPolyline AreaBoundaryAlongBorder => GeometryAlongBoundary;

			public IPolyline UncoveredBoundary { get; set; }
		}

		private class BorderMatchCondition : RowPairCondition
		{
			public BorderMatchCondition([CanBeNull] string condition, bool caseSensitive)
				: base(condition, true, true, "AREA", "BORDER", caseSensitive) { }
		}

		private class BoundingFeatureMatchCondition : RowPairCondition
		{
			public BoundingFeatureMatchCondition([CanBeNull] string condition,
			                                     bool caseSensitive)
				: base(condition, true, true, "AREA", "BOUNDINGFEATURE", caseSensitive) { }
		}

		private class AreaMatchCondition : RowPairCondition
		{
			public AreaMatchCondition([CanBeNull] string condition, bool caseSensitive)
				: base(condition, true, true, "AREA1", "AREA2", caseSensitive) { }
		}

		private class AreaAttributeConstraint : RowPairCondition
		{
			public AreaAttributeConstraint([CanBeNull] string condition, bool caseSensitive,
			                               bool isDirected)
				: base(condition, isDirected, true, "AREA1", "AREA2",
				       caseSensitive, conciseMessage: true) { }
		}

		private class ConstraintErrorCache :
			ConstraintErrorCache<ConstraintError, BorderConnection> { }

		private class ConstraintError : ConstraintError<BorderConnection> { }

		#endregion
	}
}
