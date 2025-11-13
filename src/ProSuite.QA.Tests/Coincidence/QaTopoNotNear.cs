using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Properties;

namespace ProSuite.QA.Tests.Coincidence
{
	[UsedImplicitly]
	[ProximityTest]
	public partial class QaTopoNotNear : QaNearTopoBase
	{
		private const double _epsi = 1.0e-8;

		private const ConnectionMode _defaultConnectionMode =
			ConnectionMode.EndpointOnVertex;

		private const bool _defaultIgnoreLoopsWithinNearDistance = false;
		private const bool _defaultIgnoreInconsistentLineSymbolEnds = false;
		private const bool _defaultAllowCoincidentSections = false;
		private const LineCapStyle _defaultUnconnectedLineCapStyle = LineCapStyle.Round;
		private const LineCapStyle _defaultEndCapStyle = LineCapStyle.Round;
		private const double _defaultCrossingMinLengthFactor = -1.0;
		private const int _defaultMaxConnectionErrors = 8;

		private Dictionary<int, IFeatureClassFilter> _conflictFilters;
		private Dictionary<int, QueryFilterHelper> _conflictHelpers;

		private Dictionary<int, IFeatureClassFilter> _topoFilters;
		private Dictionary<int, QueryFilterHelper> _topoHelpers;
		private double _usedJunctionCoincidenceTolerance;
		private double _junctionCoincidenceToleranceSquare;

		private IList<SegmentRelation> _relationsToCheck;

		private readonly List<FeaturePoint> _junctions = new List<FeaturePoint>();

		[CanBeNull] private AllNotReportedPairConditions _allNotReportedConditions;
		private bool _allNotReportedInitialized;

		private WKSEnvelope _allEnvelope;
		private WKSEnvelope _currentEnvelope;

		private readonly Dictionary<int, IReadOnlyFeatureClass> _conflictTables =
			new Dictionary<int, IReadOnlyFeatureClass>();

		private readonly Dictionary<int, IReadOnlyFeatureClass> _topoTables =
			new Dictionary<int, IReadOnlyFeatureClass>();

		[NotNull] private readonly bool? _isDirected;
		// nullable/notnull to ensure initialization in constructors

		[CanBeNull] private IList<string> _rightSideNears;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public new static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NearlyCoincidentSection_Disjoint =
				"NearlyCoincidentSection.Disjoint";

			public const string NearlyCoincidentSection_Disjoint_NotFullyProcessed =
				"NearlyCoincidentSection.Disjoint.NotFullyProcessed";

			public const string NearlyCoincidentSection_WithinNear =
				"NearlyCoincidentSection.WithinNear";

			public const string NearlyCoincidentSection_Crossing =
				"NearlyCoincidentSection.Crossing";

			public const string NearlyCoincidentSection_Crossing_NotFullyProcessed =
				"NearlyCoincidentSection.Crossing.NotFullyProcessed";

			public const string NearlyCoincidentSection_Connected =
				"NearlyCoincidentSection.Connected";

			public const string NearlyCoincidentSection_Connected_NotFullyProcessed =
				"NearlyCoincidentSection.Connected.NotFullyProcessed";

			public const string NearlyCoincidentSection_Connected_Loop =
				"NearlyCoincidentSection.Connected.Loop";

			public const string NearlyCoincidentSection_Connected_Loop_WithinNear =
				"NearlyCoincidentSection.Connected.Loop.WithinNear";

			public const string NearlyCoincidentSection_WithinNear_Connected =
				"NearlyCoincidentSection.WithinNear.Connected";

			public const string CoincidentSection = "CoincidentSection";

			public const string ShortSubpart = "ShortSubpart";
			public const string InconsistentLineSymbolEnd = "InconsistentLineSymbolEnd";

			public Code() : base("NearCoincidence") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaTopoNotNear(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaNotNear_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, is3D, 1000.0) { }

		// ctor 1
		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaTopoNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: this(new[] { featureClass },
			       near / 2,
			       new ConstantFeatureDistanceProvider(near / 2),
			       new ConstantPairDistanceProvider(minLength),
			       new ConstantPairDistanceProvider(minLength), is3D)
		{
			_topoTables.Add(0, featureClass);
			_conflictTables.Add(0, featureClass);
			_isDirected = false;
		}

		// ctor 2
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNear(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaNotNear_reference))]
				IReadOnlyFeatureClass reference,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength,
				[Doc(nameof(DocStrings.QaNotNear_is3D))]
				bool is3D)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, is3D, 1000.0) { }

		// ctor 3
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IReadOnlyFeatureClass reference,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: this(new[] { featureClass, reference },
			       near / 2,
			       new ConstantFeatureDistanceProvider(near / 2),
			       new ConstantPairDistanceProvider(minLength),
			       new ConstantPairDistanceProvider(minLength), is3D)
		{
			_topoTables.Add(0, featureClass);
			_conflictTables.Add(1, reference);
			_isDirected = true;

			ConnectionMode = _defaultConnectionMode;
			UnconnectedLineCapStyle = _defaultUnconnectedLineCapStyle;
			IgnoreLoopsWithinNearDistance = _defaultIgnoreLoopsWithinNearDistance;
		}

		// ctor 4
		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaTopoNotNear(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, false, 1000.0) { }

		// ctor 5
		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaTopoNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: this(featureClass, near, minLength, false, tileSize) { }

		// ctor 6
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNear(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaNotNear_reference))]
				IReadOnlyFeatureClass reference,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, false, 1000.0) { }

		// ctor 7
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IReadOnlyFeatureClass reference,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: this(featureClass, reference, near, minLength, false) { }

		// ctor 8
		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaTopoNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[NotNull] string nearExpression,
			double connectedMinLengthFactor,
			double defaultUnconnectedMinLengthFactor,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D)
			: this(featureClass,
			       near,
			       new ExpressionBasedDistanceProvider(new[] { nearExpression },
			                                           new[] { featureClass }),
			       connectedMinLengthFactor,
			       defaultUnconnectedMinLengthFactor,
			       is3D) { }

		// ctor 9
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IReadOnlyFeatureClass reference,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double connectedMinLengthFactor,
			double defaultUnconnectedMinLengthFactor,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D)
			: this(new[] { featureClass, reference },
			       near / 2,
			       new ConstantFeatureDistanceProvider(near / 2),
			       new ConstantPairDistanceProvider(
				       connectedMinLengthFactor * (near / 2)),
			       new ConstantPairDistanceProvider(
				       defaultUnconnectedMinLengthFactor * (near / 2)),
			       is3D)
		{
			_topoTables.Add(0, featureClass);
			_conflictTables.Add(1, reference);
			_isDirected = true;

			ConnectionMode = _defaultConnectionMode;
			UnconnectedLineCapStyle = _defaultUnconnectedLineCapStyle;
			IgnoreLoopsWithinNearDistance = _defaultIgnoreLoopsWithinNearDistance;
		}

		// ctor 10
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNear(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IReadOnlyFeatureClass reference,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[NotNull] string featureClassNear,
			[NotNull] string referenceNear,
			double connectedMinLengthFactor,
			double defaultUnconnectedMinLengthFactor,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D)
			: this(featureClass,
			       reference,
			       near / 2,
			       new ExpressionBasedDistanceProvider(new[] { featureClassNear, referenceNear },
			                                           new[] { featureClass, reference }),
			       connectedMinLengthFactor,
			       defaultUnconnectedMinLengthFactor,
			       is3D)
		{
			_isDirected = true;

			ConnectionMode = _defaultConnectionMode;
			UnconnectedLineCapStyle = _defaultUnconnectedLineCapStyle;
			IgnoreLoopsWithinNearDistance = _defaultIgnoreLoopsWithinNearDistance;
		}

		[InternallyUsedTest]
		public QaTopoNotNear(QaTopoNotNearDefinition definition)
			: this(new QaTopoNotNearConstructionHelper(definition)) { }

		private QaTopoNotNear(QaTopoNotNearConstructionHelper constructionHelper)
			: this(constructionHelper.GetAllFeatureClasses(),
			       constructionHelper.Definition.Near,
			       constructionHelper.GetNearExpressionProvider(),
			       constructionHelper.GetConnectedMinLengthProvider(),
			       constructionHelper.GetDefaultUnconnectedMinLengthProvider(),
			       constructionHelper.Definition.Is3D)
		{
			if (constructionHelper.ExpressionBasedDistanceProvider != null)
			{
				constructionHelper.ExpressionBasedDistanceProvider
				                  .GetSqlCaseSensitivityForTableIndex = GetSqlCaseSensitivity;
			}

			QaTopoNotNearDefinition definition = constructionHelper.Definition;

			foreach (KeyValuePair<int, IFeatureClassSchemaDef> kvp in definition.TopoTables)
			{
				_topoTables.Add(kvp.Key, (IReadOnlyFeatureClass) kvp.Value);
			}

			foreach (var kvp in definition.ConflictTables)
			{
				_conflictTables.Add(kvp.Key, (IReadOnlyFeatureClass) kvp.Value);
			}

			_isDirected = definition.IsDirected;

			CrossingMinLengthFactor = definition.CrossingMinLengthFactor;
			NotReportedCondition = definition.NotReportedCondition;
			IgnoreNeighborCondition = definition.IgnoreNeighborCondition;
			JunctionCoincidenceTolerance = definition.JunctionCoincidenceTolerance;
			ConnectionMode = definition.ConnectionMode;
			UnconnectedLineCapStyle = definition.UnconnectedLineCapStyle;
			IgnoreLoopsWithinNearDistance = definition.IgnoreLoopsWithinNearDistance;
			IgnoreInconsistentLineSymbolEnds = definition.IgnoreInconsistentLineSymbolEnds;
			AllowCoincidentSections = definition.AllowCoincidentSections;
			RightSideNears = definition.RightSideNears;
			EndCapStyle = definition.EndCapStyle;
			JunctionIsEndExpression = definition.JunctionIsEndExpression;
		}

		protected QaTopoNotNear(
			IReadOnlyFeatureClass featureClass,
			double near,
			ExpressionBasedDistanceProvider nearExpressionsProvider,
			double connectedMinLengthFactor,
			double defaultUnconnectedMinLengthFactor,
			bool is3D)
			: this(new[] { featureClass },
			       near,
			       nearExpressionsProvider,
			       new FactorDistanceProvider(connectedMinLengthFactor,
			                                  nearExpressionsProvider),
			       new FactorDistanceProvider(defaultUnconnectedMinLengthFactor,
			                                  nearExpressionsProvider),
			       is3D)
		{
			nearExpressionsProvider.GetSqlCaseSensitivityForTableIndex =
				GetSqlCaseSensitivity;

			_topoTables.Add(0, featureClass);
			_conflictTables.Add(0, featureClass);
			_isDirected = false;
		}

		protected QaTopoNotNear(
			[NotNull] IReadOnlyFeatureClass featureClass,
			[NotNull] IReadOnlyFeatureClass reference,
			double near,
			[NotNull] ExpressionBasedDistanceProvider nearExpressionsProvider,
			double connectedMinLengthFactor,
			double defaultUnconnectedMinLengthFactor,
			bool is3D)
			: this(new[] { featureClass, reference },
			       near,
			       nearExpressionsProvider,
			       new FactorDistanceProvider(connectedMinLengthFactor,
			                                  nearExpressionsProvider),
			       new FactorDistanceProvider(defaultUnconnectedMinLengthFactor,
			                                  nearExpressionsProvider),
			       is3D)
		{
			nearExpressionsProvider.GetSqlCaseSensitivityForTableIndex =
				GetSqlCaseSensitivity;

			_topoTables.Add(0, featureClass);
			_conflictTables.Add(1, reference);
			_isDirected = true;
		}

		private QaTopoNotNear(
			[NotNull] IList<IReadOnlyFeatureClass> featureClasses,
			double near,
			[NotNull] IFeatureDistanceProvider featureDistanceProvider,
			[NotNull] IPairDistanceProvider connectedMinLengthProvider,
			[NotNull] IPairDistanceProvider defaultUnconnectedMinLengthProvider,
			bool is3D)
			: base(featureClasses,
			       near * 2,
			       featureDistanceProvider,
			       connectedMinLengthProvider,
			       defaultUnconnectedMinLengthProvider,
			       is3D, 0) { }

		protected override bool IsDirected => _isDirected.Value;

		[TestParameter(_defaultCrossingMinLengthFactor)]
		public double CrossingMinLengthFactor { get; set; }

		[TestParameter]
		public string NotReportedCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaNotNear_IgnoreNeighborCondition))]
		public string IgnoreNeighborCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaNotNear_JunctionCoincidenceTolerance))]
		public double JunctionCoincidenceTolerance { get; set; }

		[TestParameter(_defaultConnectionMode)]
		public ConnectionMode ConnectionMode { get; set; } = _defaultConnectionMode;

		[TestParameter(_defaultUnconnectedLineCapStyle)]
		public LineCapStyle UnconnectedLineCapStyle { get; set; } =
			_defaultUnconnectedLineCapStyle;

		[TestParameter(_defaultIgnoreLoopsWithinNearDistance)]
		public bool IgnoreLoopsWithinNearDistance { get; set; } =
			_defaultIgnoreLoopsWithinNearDistance;

		[TestParameter(_defaultIgnoreInconsistentLineSymbolEnds)]
		public bool IgnoreInconsistentLineSymbolEnds { get; set; } =
			_defaultIgnoreInconsistentLineSymbolEnds;

		[TestParameter(_defaultAllowCoincidentSections)]
		public bool AllowCoincidentSections { get; set; } =
			_defaultAllowCoincidentSections;

		[TestParameter]
		public IList<string> RightSideNears
		{
			get { return _rightSideNears; }
			set
			{
				_rightSideNears = value;
				if (value == null)
				{
					RightSideDistanceProvider = null;
				}
				else
				{
					IList<IReadOnlyFeatureClass> featureClasses = new List<IReadOnlyFeatureClass>();
					foreach (IReadOnlyTable table in InvolvedTables)
					{
						var featureClass = (IReadOnlyFeatureClass) table;
						featureClasses.Add(featureClass);
					}

					RightSideDistanceProvider =
						new ExpressionBasedDistanceProvider(value, featureClasses);
				}
			}
		}

		[TestParameter(_defaultEndCapStyle)]
		public LineCapStyle EndCapStyle { get; set; } = _defaultEndCapStyle;

		[TestParameter]
		public string JunctionIsEndExpression { get; set; }

		[PublicAPI]
		public int MaxConnectionErrors { get; set; } = _defaultMaxConnectionErrors;

		private IList<SegmentRelation> SegmentRelationsToCheck
			=> _relationsToCheck ??
			   (_relationsToCheck = GetSegmentRelationsToCheck());

		[NotNull]
		protected IList<SegmentRelation> GetSegmentRelationsToCheck()
		{
			var result = new List<SegmentRelation>();

			if (CrossingMinLengthFactor > 0)
			{
				result.Add(
					new CrossingRelation(
						new FactorDistanceProvider(CrossingMinLengthFactor,
						                           NearDistanceProvider)));
			}

			result.Add(new DisjointRelation(DefaultUnconnectedMinLengthProvider));

			return result;
		}

		private double UsedJunctionCoincidenceTolerance
		{
			get
			{
				if (_usedJunctionCoincidenceTolerance <= 0)
				{
					_usedJunctionCoincidenceTolerance = Math.Max(
						JunctionCoincidenceTolerance,
						GeometryUtils.GetXyTolerance(
							((IReadOnlyFeatureClass) InvolvedTables[0]).SpatialReference));
				}

				return _usedJunctionCoincidenceTolerance;
			}
		}

		private double JunctionCoincidenceToleranceSquare
		{
			get
			{
				if (_junctionCoincidenceToleranceSquare <= 0)
				{
					double tolerance = UsedJunctionCoincidenceTolerance;
					_junctionCoincidenceToleranceSquare =
						tolerance * tolerance;
				}

				return _junctionCoincidenceToleranceSquare;
			}
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (! _topoTables.ContainsKey(tableIndex))
			{
				return NoError;
			}

			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return NoError;
			}

			if (_conflictFilters == null)
			{
				InitFilter();
				Assert.NotNull(_conflictFilters, nameof(_conflictFilters));
			}

			SegmentNeighbors processed0;
			var featureKey = new RowKey(feature, tableIndex);
			if (! ProcessedList.TryGetValue(featureKey, out processed0))
			{
				processed0 = new SegmentNeighbors(new SegmentPartComparer());
				ProcessedList.Add(featureKey, processed0);
			}

			IGeometry geom0 = feature.Shape;
			IEnvelope box0 = geom0.Envelope;

			const bool asRatio = false;
			box0.Expand(SearchDistance, SearchDistance, asRatio);

			var errorCount = 0;

			IFeatureRowsDistance rowsDistance =
				NearDistanceProvider.GetRowsDistance(featureKey.Row,
				                                     featureKey.TableIndex);

			foreach (KeyValuePair<int, IReadOnlyFeatureClass> pair in _conflictTables)
			{
				int conflictTableIndex = pair.Key;
				IReadOnlyFeatureClass conflictTable = pair.Value;

				IFeatureClassFilter filter = _conflictFilters[conflictTableIndex];
				filter.FilterGeometry = box0;
				QueryFilterHelper helper = _conflictHelpers[conflictTableIndex];

				foreach (IReadOnlyRow neighborRow in
				         Search(conflictTable, filter, helper))
				{
					var neighborFeature = (IReadOnlyFeature) neighborRow;

					if (neighborFeature == feature)
					{
						continue;
					}

					SegmentNeighbors processed1;
					var neighborKey = new RowKey(neighborFeature, conflictTableIndex);
					if (! ProcessedList.TryGetValue(neighborKey, out processed1))
					{
						processed1 = new SegmentNeighbors(new SegmentPartComparer());

						ProcessedList.Add(neighborKey, processed1);
					}

					NeighborhoodFinder finder = GetNeighborhoodFinder(
						rowsDistance, feature, tableIndex,
						neighborFeature, conflictTableIndex);

					double near = rowsDistance.GetAddedDistance(
						neighborFeature, conflictTableIndex);

					errorCount += FindNeighborhood(finder, tableIndex, processed0,
					                               conflictTableIndex, processed1,
					                               near);
				}
			}

			return errorCount;
		}

		protected override NeighborhoodFinder GetNeighborhoodFinder(
			IFeatureRowsDistance distanceProvider, IReadOnlyFeature feature, int tableIndex,
			IReadOnlyFeature neighbor, int neighborTableIndex)
		{
			bool searchJunctions = _topoTables.ContainsKey(tableIndex) &&
			                       _topoTables.ContainsKey(neighborTableIndex);

			return new TopoNeighborhoodFinder(distanceProvider, feature, tableIndex,
			                                  neighbor, neighborTableIndex,
			                                  _junctions, JunctionCoincidenceToleranceSquare,
			                                  ConnectionMode, searchJunctions);
		}

		private void EnsureAllNotReportedInitialized()
		{
			if (_allNotReportedInitialized)
			{
				return;
			}

			if (StringUtils.IsNotEmpty(IgnoreNeighborCondition) ||
			    StringUtils.IsNotEmpty(NotReportedCondition))
			{
				_allNotReportedConditions =
					new AllNotReportedPairConditions(IgnoreNeighborCondition,
					                                 NotReportedCondition, GetSqlCaseSensitivity());
			}

			_allNotReportedInitialized = true;
		}

		protected override int Check(
			IReadOnlyFeature feat0, int tableIndex,
			SortedDictionary<SegmentPart, SegmentParts> processed0,
			IReadOnlyFeature feat1, int neighborTableIndex,
			SortedDictionary<SegmentPart, SegmentParts> processed1,
			double near)
		{
			foreach (SegmentParts polySegments in processed0.Values)
			{
				foreach (SegmentPart segmentPart in polySegments)
				{
					segmentPart.Complete = false;
				}
			}

			//return base.Check(feat0, tableIndex, processed0,
			//				  feat1, neighborTableIndex, processed1,
			//				  near);
			return 0;
		}

		private void InitFilter()
		{
			IList<IFeatureClassFilter> filters;
			IList<QueryFilterHelper> helpers;

			CopyFilters(out filters, out helpers);

			_conflictFilters = new Dictionary<int, IFeatureClassFilter>();
			_conflictHelpers = new Dictionary<int, QueryFilterHelper>();

			foreach (int conflictTableIndex in _conflictTables.Keys)
			{
				IFeatureClassFilter filter = filters[conflictTableIndex];
				filter.SpatialRelationship = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
				_conflictFilters.Add(conflictTableIndex, filter);

				_conflictHelpers.Add(conflictTableIndex, helpers[conflictTableIndex]);
			}

			CopyFilters(out filters, out helpers);
			_topoFilters = new Dictionary<int, IFeatureClassFilter>();
			_topoHelpers = new Dictionary<int, QueryFilterHelper>();
			foreach (int topoTableIndex in _topoTables.Keys)
			{
				IFeatureClassFilter topoFilter = filters[topoTableIndex];
				topoFilter.SpatialRelationship =
					esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
				_topoFilters.Add(topoTableIndex, topoFilter);
				_topoHelpers.Add(topoTableIndex, helpers[topoTableIndex]);
			}
		}

		[NotNull]
		private IEnumerable<NeighboredSegmentsSubpart> GetSplitAtJunctions(
			[NotNull] IReadOnlyFeature feature, int tableIndex,
			[NotNull] IIndexedSegments geometry,
			[NotNull] SegmentNeighbors curve,
			[NotNull] IDictionary<IReadOnlyFeature, List<FeaturePoint>> featureJunctionsDict)
		{
			List<FeaturePoint> featureJunctions;
			if (! featureJunctionsDict.TryGetValue(feature, out featureJunctions))
			{
				featureJunctions = new List<FeaturePoint>();
			}

			featureJunctions.Sort((x, y) =>
			{
				int i = x.Part.CompareTo(y.Part);
				if (i != 0)
				{
					return i;
				}

				i = x.FullFraction.CompareTo(y.FullFraction);
				if (i != 0)
				{
					return i;
				}

				return i;
			});

			using (IEnumerator<FeaturePoint> jctEnum = featureJunctions.GetEnumerator())
			{
				bool hasJunction = jctEnum.MoveNext();
				int prePartIndex = -1;
				int preStartSegmentIndex = -1;
				int preEndSegmentIndex = -1;

				var subparts = new List<SegmentsSubpart>();

				foreach (KeyValuePair<SegmentPart, SegmentParts> pair in curve)
				{
					SegmentPart key = pair.Key;
					if (key.PartIndex != prePartIndex ||
					    key.SegmentIndex > preEndSegmentIndex + 1)
					{
						if (prePartIndex >= 0 &&
						    preStartSegmentIndex <= preEndSegmentIndex)
						{
							var subpart = new SegmentsSubpart(feature, tableIndex,
							                                  geometry,
							                                  prePartIndex,
							                                  preStartSegmentIndex,
							                                  preEndSegmentIndex + 1);
							subparts.Add(subpart);
						}

						prePartIndex = key.PartIndex;
						preStartSegmentIndex = key.SegmentIndex;
						preEndSegmentIndex = key.SegmentIndex;
					}

					while (hasJunction &&
					       Assert.NotNull(jctEnum.Current).Part < prePartIndex)
					{
						hasJunction = jctEnum.MoveNext();
					}

					while (hasJunction &&
					       Assert.NotNull(jctEnum.Current).Part == prePartIndex &&
					       jctEnum.Current.FullFraction < preEndSegmentIndex + 1)
					{
						hasJunction = jctEnum.MoveNext();
					}

					if (hasJunction && jctEnum.Current.Part == prePartIndex &&
					    Math.Abs(jctEnum.Current.FullFraction - (preEndSegmentIndex + 1)) <
					    _epsi)
					{
						var subpart = new SegmentsSubpart(feature, tableIndex,
						                                  geometry, prePartIndex,
						                                  preStartSegmentIndex,
						                                  preEndSegmentIndex + 1);
						subparts.Add(subpart);

						preStartSegmentIndex = preEndSegmentIndex + 1;

						hasJunction = jctEnum.MoveNext();
					}

					preEndSegmentIndex = key.SegmentIndex;
				}

				if (prePartIndex >= 0 && preStartSegmentIndex <= preEndSegmentIndex)
				{
					var subpart = new SegmentsSubpart(feature, tableIndex,
					                                  geometry, prePartIndex,
					                                  preStartSegmentIndex,
					                                  preEndSegmentIndex + 1);
					subparts.Add(subpart);
				}

				foreach (SegmentsSubpart subpart in subparts)
				{
					var subpartKey = new RowKey(subpart.BaseFeature, subpart.TableIndex);
					SegmentNeighbors featureNeighbors = ProcessedList[subpartKey];

					var subpartNeighbors = new SegmentNeighbors(new SegmentPartComparer());

					foreach (SegmentProxy segment in subpart.GetSegments())
					{
						var key = new SegmentPart(segment, 0, 1, true);

						SegmentParts segmentNeighbors;
						if (featureNeighbors.TryGetValue(key, out segmentNeighbors))
						{
							subpartNeighbors.Add(key, segmentNeighbors);
						}
					}

					yield return new NeighboredSegmentsSubpart(subpart, subpartNeighbors);
				}
			}
		}

		[NotNull]
		private static Dictionary<IReadOnlyFeature, List<FeaturePoint>> GetFeatureJunctions(
			[NotNull] IEnumerable<FeaturePoint> junctionsEnum)
		{
			var result = new Dictionary<IReadOnlyFeature, List<FeaturePoint>>();

			foreach (FeaturePoint jct in junctionsEnum)
			{
				List<FeaturePoint> junctions;
				if (! result.TryGetValue(jct.Feature, out junctions))
				{
					junctions = new List<FeaturePoint>();
					result.Add(jct.Feature, junctions);
				}

				junctions.Add(jct);
			}

			return result;
		}

		[NotNull]
		private Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>>
			GetSplittedParts(
				[NotNull] IDictionary<IReadOnlyFeature, List<FeaturePoint>> featureJunctions)
		{
			var splitList = new Dictionary
				<FeaturePoint, List<NeighboredSegmentsSubpart>>(
					new FeaturePointComparer());

			foreach (KeyValuePair<RowKey, SegmentNeighbors> pair in ProcessedList)
			{
				RowKey featureKey = pair.Key;
				var feat = (IReadOnlyFeature) featureKey.Row;
				SegmentNeighbors nearList = pair.Value;

				foreach (NeighboredSegmentsSubpart subparts in
				         GetSplitAtJunctions(feat, featureKey.TableIndex,
				                             IndexedSegmentUtils.GetIndexedGeometry(feat,
					                             false),
				                             nearList, featureJunctions))
				{
					var partKey = new FeaturePoint(
						subparts.BaseFeature, subparts.TableIndex, subparts.PartIndex,
						0);
					List<NeighboredSegmentsSubpart> featureParts;

					if (! splitList.TryGetValue(partKey, out featureParts))
					{
						featureParts = new List<NeighboredSegmentsSubpart>();
						splitList.Add(partKey, featureParts);
					}

					featureParts.Add(subparts);
				}
			}

			return splitList;
		}

		[CanBeNull]
		private static SegmentPairRelation GetRelevantSegment(
			[NotNull] IList<SegmentRelation> relations,
			[NotNull] IList<SegmentPart> connectedParts)
		{
			int minRelationIndex = relations.Count;
			SegmentPairRelation minRelation = null;

			var cap = new RoundCap();
			foreach (SegmentPart connectedPart in connectedParts)
			{
				var related = (SegmentPartWithNeighbor) connectedPart;

				var pair = new SegmentPair2D(
					new SegmentHull(Assert.NotNull((SegmentProxy) related.SegmentProxy), 0, cap,
					                cap),
					new SegmentHull((SegmentProxy) related.NeighborProxy, 0, cap, cap));

				var relationIndex = 0;
				foreach (SegmentRelation relation in relations)
				{
					if (relation.IsRelated(pair))
					{
						if (minRelationIndex > relationIndex)
						{
							minRelation = new SegmentPairRelation(related, relation);
							minRelationIndex = relationIndex;
						}

						related.MinRelationIndex = relationIndex;
						break;
					}

					relationIndex++;
				}
			}

			return minRelation;
		}

		[NotNull]
		private IEnumerable<ConnectedLinesEx> GetDisjointErrorCandidates(
			[NotNull] NeighboredSegmentsSubpart part,
			[NotNull] ContinuationFinder continuationFinder)
		{
			var result = new List<ConnectedLinesEx>();

			IList<ConnectedSegmentParts> connectedPartsList;

			GetConnectedParts(part.BaseSegments, part.SegmentNeighbors.Values,
			                  0, 0, Is3D, out connectedPartsList, out _);

			foreach (ConnectedSegmentParts connectedParts in connectedPartsList)
			{
				AddDisjointErrorCandidates(part, continuationFinder, connectedParts, result);
			}

			return result;
		}

		private void AddDisjointErrorCandidates(
			[NotNull] NeighboredSegmentsSubpart part,
			[NotNull] ContinuationFinder continuationFinder,
			[NotNull] ConnectedSegmentParts connectedParts,
			[NotNull] List<ConnectedLinesEx> result)
		{
			SegmentPairRelation relevantSegment =
				Assert.NotNull(GetRelevantSegment(SegmentRelationsToCheck,
				                                  connectedParts));

			var curve = new SubClosedCurve(
				connectedParts.BaseGeometry, connectedParts.PartIndex,
				connectedParts.StartFullIndex, connectedParts.EndFullIndex);

			List<ConnectedLines> rawStarts = null;
			List<ConnectedLines> rawEnds = null;

			SubClosedCurve d = curve;
			SegmentNeighbors curveSelection =
				part.SegmentNeighbors.Select(p => p.PartIndex == d.PartIndex &&
				                                  p.FullMin >= d.StartFullIndex &&
				                                  p.FullMax <= d.EndFullIndex);

			double curveLength = curve.GetLength();
			if (curveLength <= 0)
			{
				return;
			}

			var subpart = new ConnectedSegmentsSubpart(part, curveSelection, curve);
			Dictionary<SegmentRelation, double> minLengths = GetMinLengths(
				new[] { subpart }, SegmentRelationsToCheck);

			double maxMinLength = 0;
			foreach (double minLength in minLengths.Values)
			{
				maxMinLength = Math.Max(minLength, maxMinLength);
			}

			if (curveLength > maxMinLength)
			{
				var lines =
					new ConnectedLines(new List<ConnectedSegmentsSubpart>
					                   {
						                   subpart
					                   });
				lines.RelevantSegment = relevantSegment;

				double minLength = minLengths[relevantSegment.Relation];
				var linesEx = new ConnectedLinesEx(lines, minLength);
				result.Add(linesEx);
				return;
			}

			if (curve.StartFullIndex <= part.FullStartFraction)
			{
				var inverted = new SegmentsSubpart(part.BaseFeature,
				                                   part.TableIndex,
				                                   part.BaseSegments,
				                                   part.PartIndex,
				                                   part.FullEndFraction,
				                                   part.FullStartFraction);

				var lines = new ConnectedLines(
					new[]
					{
						new ConnectedSegmentsSubpart(inverted, curveSelection,
						                             curve)
					});
				var join = new Join(lines, continuationFinder);

				rawStarts = new List<ConnectedLines>(
					join.Continue(curveLength, minLengths, relevantSegment));
			}

			if (curve.EndFullIndex >= part.FullEndFraction)
			{
				var lines = new ConnectedLines(
					new[]
					{
						new ConnectedSegmentsSubpart(part, curveSelection, curve)
					});
				var join = new Join(lines, continuationFinder);

				rawEnds = new List<ConnectedLines>(
					join.Continue(curveLength, minLengths, relevantSegment));
			}

			double l0 = curve.GetLength();

			List<ConnectedLines>[] allRaws = { rawStarts, rawEnds };
			relevantSegment =
				GetRelevantSegment(SegmentRelationsToCheck, allRaws) ?? relevantSegment;

			var fullCompletedStarts = new List<ConnectedLinesEx>();
			var fullStarts = new List<ConnectedLinesEx>();
			if (rawStarts != null)
			{
				foreach (ConnectedLinesEx start in FilterSubparts(rawStarts))
				{
					SubClosedCurve c = start.Line.BaseSegments[0].ConnectedCurve;
					if (c.GetLength() < l0)
					{
						fullCompletedStarts.Add(start);
					}
					else
					{
						fullStarts.Add(start);
					}
				}
			}

			var fullCompletedEnds = new List<ConnectedLinesEx>();
			var fullEnds = new List<ConnectedLinesEx>();
			if (rawEnds != null)
			{
				foreach (ConnectedLinesEx end in FilterSubparts(rawEnds))
				{
					SubClosedCurve c = end.Line.BaseSegments[0].ConnectedCurve;
					if (c.GetLength() < l0)
					{
						fullCompletedEnds.Add(end);
					}
					else
					{
						fullEnds.Add(end);
					}
				}
			}

			var allLines = new List<ConnectedLinesEx>();
			allLines.AddRange(fullCompletedStarts);
			allLines.AddRange(fullStarts);
			allLines.AddRange(fullCompletedEnds);
			allLines.AddRange(fullEnds);
			if (allLines.Count == 0)
			{
				ConnectedLinesEx combined = GetCombinedDisjoint(part, curve, null, null,
				                                                relevantSegment);
				allLines.Add(combined);
			}

			var neighborsContinuations = new NeighborsContinuations(allLines);
			neighborsContinuations.SetRelationIndexToConnecteds(
				continuationFinder, relevantSegment.Segment.MinRelationIndex);

			foreach (ConnectedLinesEx start in fullCompletedStarts)
			{
				CheckAdd(result,
				         GetCandidates(part, null, start, null,
				                       continuationFinder, relevantSegment,
				                       neighborsContinuations));
			}

			foreach (ConnectedLinesEx end in fullCompletedEnds)
			{
				CheckAdd(result,
				         GetCandidates(part, null, null, end,
				                       continuationFinder, relevantSegment,
				                       neighborsContinuations));
			}

			if (rawStarts == null && rawEnds == null)
			{
				CheckAdd(result,
				         GetCandidates(part, curve, null, null, continuationFinder,
				                       relevantSegment, neighborsContinuations));
			}

			if (fullStarts.Count == 0)
			{
				foreach (ConnectedLinesEx end in fullEnds)
				{
					CheckAdd(result,
					         GetCandidates(part, null, null, end,
					                       continuationFinder, relevantSegment,
					                       neighborsContinuations));
				}
			}

			if (fullEnds.Count == 0)
			{
				foreach (ConnectedLinesEx start in fullStarts)
				{
					CheckAdd(result,
					         GetCandidates(part, null, start, null,
					                       continuationFinder, relevantSegment,
					                       neighborsContinuations));
				}
			}

			foreach (ConnectedLinesEx start in fullStarts)
			{
				foreach (ConnectedLinesEx end in fullEnds)
				{
					CheckAdd(result,
					         GetCandidates(part, null, start, end,
					                       continuationFinder, relevantSegment,
					                       neighborsContinuations));
				}
			}
		}

		private SegmentPairRelation GetRelevantSegment(
			[NotNull] IList<SegmentRelation> relevantRelationCandidates,
			[NotNull] IList<List<ConnectedLines>> raws)
		{
			SegmentPairRelation relevant = null;
			foreach (List<ConnectedLines> raw in raws)
			{
				if (raw == null)
				{
					continue;
				}

				foreach (ConnectedLines connectedLines in raw)
				{
					SegmentPairRelation relation = connectedLines.RelevantSegment;

					if (relation == null)
					{
						continue;
					}

					relevant = GetRelevantRelation(relevantRelationCandidates, relation, relevant);
				}
			}

			return relevant;
		}

		private static void CheckAdd(
			[NotNull] ICollection<ConnectedLinesEx> errorCandidates,
			[NotNull] IEnumerable<ConnectedLinesEx> candidates)
		{
			foreach (ConnectedLinesEx candidate in candidates)
			{
				errorCandidates.Add(candidate);
			}
		}

		[NotNull]
		private IEnumerable<ConnectedLinesEx> FilterSubparts(
			[NotNull] IEnumerable<ConnectedLines> lines)
		{
			return FilterSubparts(
				GetConnectedLinesEx(lines, DefaultUnconnectedMinLengthProvider));
		}

		[NotNull]
		private static IEnumerable<ConnectedLinesEx> GetConnectedLinesEx(
			[NotNull] IEnumerable<ConnectedLines> lines,
			[NotNull] IPairDistanceProvider minLengthProvider)
		{
			return lines.Select(line => new ConnectedLinesEx(
				                    line,
				                    GetMinLength(line.BaseSegments, minLengthProvider)));
		}

		[NotNull]
		private static IEnumerable<ConnectedLinesEx> FilterSubparts(
			[NotNull] IEnumerable<ConnectedLinesEx> lines)
		{
			var connectedLines = new List<ConnectedLinesEx>();
			Box allBox = null;

			foreach (ConnectedLinesEx line in lines)
			{
				connectedLines.Add(line);
				if (allBox == null)
				{
					allBox = line.Box.Clone();
				}
				else
				{
					allBox.Include(line.Box);
				}
			}

			if (allBox == null)
			{
				return new List<ConnectedLinesEx>();
			}

			var fullTree = new BoxTree<ConnectedLinesEx>(2, 8, true);
			fullTree.Init(allBox, 8);

			connectedLines.Sort(
				(x, y) =>
				{
					int d = -x.Length.CompareTo(y.Length);
					if (d != 0)
					{
						return d;
					}

					d = -x.IsCircular.CompareTo(y.IsCircular);
					if (d != 0)
					{
						return d;
					}

					return 0;
				});

			foreach (ConnectedLinesEx line in connectedLines)
			{
				var isSubpart = false;
				foreach (
					BoxTree<ConnectedLinesEx>.TileEntry pair in
					fullTree.Search(line.Box))
				{
					ConnectedLinesEx fullLine = pair.Value;

					if (fullLine.IsCircular != line.IsCircular)
					{
						if (! fullLine.IsCircular || ! line.WithinNear)
						{
							continue;
						}
					}

					if (fullLine.WithinNear != line.WithinNear)
					{
						if (fullLine.IsCircular && line.IsCircular) { }
						else if (fullLine.IsCircular && line.WithinNear) { }
						else
						{
							continue;
						}
					}

					double diffLength = GetDifference(fullLine, line.Line);

					if (diffLength < line.MinLength)
					{
						if (! fullLine.IsCircular)
						{
							isSubpart = true;
						}

						if (diffLength < line.Length)
						{
							isSubpart = true;

							fullLine.Include(line);

							if (fullLine.IsCircular && line.WithinNear && diffLength < _epsi)
							{
								fullLine.WithinNear = true;
							}
						}
					}
				}

				if (! isSubpart)
				{
					fullTree.Add(line.Box, line);
				}
			}

			var fullLines = new List<ConnectedLinesEx>(fullTree.Count);

			foreach (BoxTree<ConnectedLinesEx>.TileEntry pair in fullTree.Search(null)
			        )
			{
				fullLines.Add(pair.Value);
			}

			return fullLines;
		}

		private static double GetDifference([NotNull] ConnectedLinesEx fullLine,
		                                    [NotNull] ConnectedLines line)
		{
			double diff = 0;
			foreach (ConnectedSegmentsSubpart linePart in line.BaseSegments)
			{
				var curves = new List<SubClosedCurve> { linePart.ConnectedCurve };

				foreach (
					ConnectedSegmentsSubpart fullPart in fullLine.GetParts(linePart))
				{
					var remaining = new List<SubClosedCurve>();
					foreach (SubClosedCurve curve in curves)
					{
						remaining.AddRange(
							curve.Difference(fullPart.ConnectedCurve.StartFullIndex,
							                 fullPart.ConnectedCurve.EndFullIndex));
					}

					curves = remaining;
				}

				foreach (SubClosedCurve differs in curves)
				{
					diff += differs.GetLength();
				}
			}

			return diff;
		}

		private static Dictionary<SegmentRelation, double> GetMinLengths<TSubpart>(
			[NotNull] IList<TSubpart> parts,
			IEnumerable<SegmentRelation> relations)
			where TSubpart : NeighboredSegmentsSubpart
		{
			var minLengths = new Dictionary<SegmentRelation, double>();

			foreach (SegmentRelation rel in relations)
			{
				minLengths.Add(rel, GetMinLength(parts, rel.DistanceProvider));
			}

			return minLengths;
		}

		private static double GetMinLength<TSubpart>(
			[NotNull] IEnumerable<TSubpart> parts,
			[NotNull] IPairDistanceProvider minLengthProvider)
			where TSubpart : NeighboredSegmentsSubpart
		{
			var constantDistanceProvider =
				minLengthProvider as IConstantDistanceProvider;

			double result;
			if (constantDistanceProvider != null &&
			    constantDistanceProvider.TryGetConstantDistance(out result))
			{
				return result;
			}

			result = 0;

			foreach (TSubpart part in parts)
			{
				IPairRowsDistance minLengthRowsDistance =
					minLengthProvider.GetRowsDistance(part.BaseFeature,
					                                  part.TableIndex);
				var handled = new Dictionary<RowKey, IReadOnlyFeature>(new RowKeyComparer());

				foreach (SegmentParts segments in part.SegmentNeighbors.Values)
				{
					foreach (SegmentPart segmentPart in segments)
					{
						var neighbor = segmentPart as SegmentPartWithNeighbor;
						if (neighbor == null)
						{
							continue;
						}

						var key = new RowKey(neighbor.NeighborFeature,
						                     neighbor.NeighborTableIndex);
						if (handled.ContainsKey(key))
						{
							continue;
						}

						double pairLength = minLengthRowsDistance.GetAddedDistance(
							neighbor.NeighborFeature, neighbor.NeighborTableIndex);
						result = Math.Max(result, pairLength);

						handled.Add(key, neighbor.NeighborFeature);
					}
				}
			}

			return result;
		}

		private void AssembleIncompleted(
			[NotNull] IEnumerable<ConnectedSegmentsSubpart> segmentSubparts,
			[NotNull] IDictionary<FeaturePoint, Dictionary<FeaturePoint, FeaturePoint>>
				groupedJunctions,
			[NotNull] IDictionary<RowKey, SegmentNeighbors> incompletedRows,
			[NotNull] IDictionary<FeaturePoint, FeaturePoint> incompletedJunctions)
		{
			foreach (ConnectedSegmentsSubpart segments in segmentSubparts)
			{
				SegmentNeighbors segmentNeighbors;
				var segmentsKey = new RowKey(segments.BaseFeature, segments.TableIndex);
				if (! incompletedRows.TryGetValue(segmentsKey, out segmentNeighbors))
				{
					segmentNeighbors = new SegmentNeighbors(new SegmentPartComparer());
					incompletedRows.Add(segmentsKey, segmentNeighbors);
				}

				SegmentNeighbors processed = ProcessedList[segmentsKey];
				foreach (
					KeyValuePair<SegmentPart, SegmentParts> pair in
					segments.SegmentNeighbors
				)
				{
					SegmentParts parts;
					if (! segmentNeighbors.TryGetValue(pair.Key, out parts))
					{
						parts = new SegmentParts();
						segmentNeighbors.Add(pair.Key, parts);

						parts.AddRange(processed[pair.Key]);
					}
				}

				var jcts = new List<FeaturePoint>();
				jcts.Add(new FeaturePoint(segments.BaseFeature, segments.TableIndex,
				                          segments.PartIndex,
				                          segments.FullStartFraction));
				jcts.Add(new FeaturePoint(segments.BaseFeature, segments.TableIndex,
				                          segments.PartIndex, segments.FullEndFraction));
				foreach (FeaturePoint jct in jcts)
				{
					if (! groupedJunctions.ContainsKey(jct))
					{
						continue;
					}

					if (! incompletedJunctions.ContainsKey(jct))
					{
						incompletedJunctions.Add(jct, jct);
					}
				}
			}
		}

		[NotNull]
		private IEnumerable<ConnectedLinesEx> GetCandidates(
			[NotNull] NeighboredSegmentsSubpart startPart,
			[CanBeNull] SubClosedCurve curve,
			[CanBeNull] ConnectedLinesEx start,
			[CanBeNull] ConnectedLinesEx end,
			ContinuationFinder continuationFinder,
			SegmentPairRelation relevantSegment,
			NeighborsContinuations treeProvider)
		{
			if (start != null && start.Line.Handled)
			{
				yield break;
			}

			if (end != null && end.Line.Handled)
			{
				yield break;
			}

			if (curve == null && start != null)
			{
				curve = start.Line.BaseSegments[0].ConnectedCurve;
			}

			if (curve == null && end != null)
			{
				curve = end.Line.BaseSegments[0].ConnectedCurve;
			}

			if (curve == null)
			{
				yield break;
			}

			ConnectedLinesEx combined = GetCombinedDisjoint(startPart, curve, start,
			                                                end, relevantSegment);

			if (IsCandidate(combined))
			{
				yield return combined;
				yield break;
			}

			foreach (
				ConnectedLinesEx reduced in
				GetSubconnecteds(combined, treeProvider, continuationFinder, relevantSegment))
			{
				yield return reduced;
			}
		}

		private bool IsCandidate(ConnectedLinesEx combined)
		{
			if (combined.Length > combined.MinLength)
			{
				return true;
			}

			if (combined.Line.IsWithinDistance(NearDistanceProvider))
			{
				combined.WithinNear = true;
				return true;
			}

			return false;
		}

		private ConnectedLinesEx GetReducedConflict(ConnectedLinesEx lineWithoutNotReported)
		{
			SegmentPairRelation relevantSegment =
				GetRelevantSegment(
					SegmentRelationsToCheck,
					new[] { new List<ConnectedLines> { lineWithoutNotReported.Line } });

			var reduced = new ConnectedLinesEx(
				lineWithoutNotReported.Line,
				GetMinLength(lineWithoutNotReported.Line.BaseSegments,
				             Assert.NotNull(relevantSegment).Relation.DistanceProvider));
			reduced.Line.RelevantSegment = relevantSegment;

			return reduced;
		}

		private IEnumerable<ConnectedLinesEx> GetSubconnecteds(
			ConnectedLinesEx line,
			NeighborsContinuations treeProvider,
			ContinuationFinder continuationFinder,
			SegmentPairRelation startRelevantSegment)
		{
			int relevantRelationIndex =
				SegmentRelationsToCheck.IndexOf(startRelevantSegment.Relation);
			IIndexedSegments baseSegments = null;

			foreach (ConnectedSegmentsSubpart seg in line.Line.BaseSegments)
			{
				baseSegments = seg.BaseSegments;
			}

			if (baseSegments == null)
			{
				yield break;
			}

			ConnectedLines subConnected = null;
			SegmentNeighbors subNeighbors = null;
			SegmentPairRelation subRelevantSegment = null;
			foreach (ConnectedSegmentsSubpart allParts in line.Line.BaseSegments)
			{
				double minIndex = allParts.FullMinFraction;
				double maxIndex = allParts.FullMaxFraction;

				ConnectedSegmentsSubpart subSubparts = null;
				foreach (
					KeyValuePair<SegmentPart, SegmentParts> segmentNeighbor in
					allParts.SegmentNeighbors)
				{
					foreach (SegmentPart neighbor in segmentNeighbor.Value)
					{
						var segmentPart = (SegmentPartWithNeighbor) neighbor;
						if (segmentPart.MinRelationIndex <= relevantRelationIndex)
						{
							continue;
						}

						if (segmentPart.FullMin > minIndex)
						{
							foreach (
								ConnectedLinesEx sub in
								GetConnecteds(subConnected, continuationFinder,
								              subRelevantSegment))
							{
								yield return sub;
							}

							subConnected = null;
						}

						if (subConnected == null)
						{
							subConnected =
								new ConnectedLines(
									new List<ConnectedSegmentsSubpart>());
							subRelevantSegment = null;
							subSubparts = null;
						}

						if (subRelevantSegment == null ||
						    segmentPart.MinRelationIndex <
						    subRelevantSegment.Segment.MinRelationIndex)
						{
							subRelevantSegment =
								new SegmentPairRelation(
									segmentPart,
									SegmentRelationsToCheck[segmentPart.MinRelationIndex]);
						}

						if (subSubparts == null)
						{
							subNeighbors =
								new SegmentNeighbors(new SegmentPartComparer());
							subSubparts =
								new ConnectedSegmentsSubpart(
									allParts, subNeighbors, allParts.ConnectedCurve);
							subConnected.BaseSegments.Add(subSubparts);
						}

						SegmentParts parts;
						var key =
							new SegmentPart(Assert.NotNull(segmentPart.SegmentProxy),
							                0, 1, true);
						if (! subNeighbors.TryGetValue(key, out parts))
						{
							parts = new SegmentParts();
							subNeighbors.Add(key, parts);
						}

						parts.Add(segmentPart);

						minIndex = Math.Max(minIndex, segmentPart.FullMax);
					}
				}

				if (minIndex < maxIndex)
				{
					foreach (
						ConnectedLinesEx sub in
						GetConnecteds(subConnected, continuationFinder,
						              subRelevantSegment))
					{
						yield return sub;
					}

					subConnected = null;
				}
			}

			foreach (
				ConnectedLinesEx sub in
				GetConnecteds(subConnected, continuationFinder, subRelevantSegment))
			{
				yield return sub;
			}
		}

		private IEnumerable<ConnectedLinesEx> GetConnecteds(
			[CanBeNull] ConnectedLines connected,
			ContinuationFinder continuationFinder,
			SegmentPairRelation relevantSegment)
		{
			if (connected == null)
			{
				yield break;
			}

			connected.RelevantSegment = relevantSegment;
			IPairDistanceProvider dist = relevantSegment.Relation.DistanceProvider;
			var connectedEx = new ConnectedLinesEx(
				connected, GetMinLength(connected.BaseSegments, dist));
			if (connectedEx.Length > connectedEx.MinLength)
			{
				yield return connectedEx;
				yield break;
			}

			var treeProvider = new NeighborsContinuations(new[] { connectedEx });
			foreach (
				ConnectedLinesEx sub in
				GetSubconnecteds(connectedEx, treeProvider, continuationFinder, relevantSegment))
			{
				yield return sub;
			}
		}

		private static SegmentPairRelation GetRelevantRelation(
			[NotNull] IList<SegmentRelation> sorted,
			[CanBeNull] SegmentPairRelation x,
			[CanBeNull] SegmentPairRelation y)
		{
			if (x == null)
			{
				return y;
			}

			if (y == null)
			{
				return x;
			}

			SegmentPairRelation relevant =
				sorted.IndexOf(x.Relation) <= sorted.IndexOf(y.Relation) ? x : y;
			return relevant;
		}

		[NotNull]
		private ConnectedLinesEx GetCombinedDisjoint(
			[NotNull] NeighboredSegmentsSubpart startPart,
			[NotNull] SubClosedCurve curve,
			[CanBeNull] ConnectedLinesEx start,
			[CanBeNull] ConnectedLinesEx end,
			SegmentPairRelation relevantSegment)
		{
			var combined = new List<ConnectedSegmentsSubpart>();

			if (start != null)
			{
				relevantSegment = GetRelevantRelation(SegmentRelationsToCheck,
				                                      relevantSegment,
				                                      start.Line.RelevantSegment);

				for (int i = start.Line.BaseSegments.Count - 1; i >= 1; i--)
				{
					ConnectedSegmentsSubpart baseSegment = start.Line.BaseSegments[i];
					combined.Add(baseSegment.Reverse());
				}
			}

			SegmentNeighbors startNeighbors =
				startPart.SegmentNeighbors.Select(
					p => p.FullMin >= curve.StartFullIndex &&
					     p.FullMax <= curve.EndFullIndex);

			combined.Add(new ConnectedSegmentsSubpart(startPart, startNeighbors,
			                                          curve));
			if (end != null)
			{
				relevantSegment = GetRelevantRelation(SegmentRelationsToCheck,
				                                      relevantSegment,
				                                      end.Line.RelevantSegment);
				for (var i = 1; i < end.Line.BaseSegments.Count; i++)
				{
					combined.Add(end.Line.BaseSegments[i]);
				}
			}

			var line = new ConnectedLinesEx(
				new ConnectedLines(combined),
				GetMinLength(combined,
				             Assert.NotNull(relevantSegment).Relation.DistanceProvider));
			line.Line.RelevantSegment = relevantSegment;

			if (end != null)
			{
				line.Include(end);
			}

			if (start != null)
			{
				line.Include(start);
			}

			return line;
		}

		[NotNull]
		private static InvolvedRows GetInvolvedRows(
			[NotNull] ConnectedLinesEx parts)
		{
			IEnumerable<IReadOnlyRow> rows = GetInvolvedFeatures(parts,
			                                                     includeIrrelevantNeighbors:
			                                                     false);

			InvolvedRows result = InvolvedRowUtils.GetInvolvedRows(rows);

			return result;
		}

		[NotNull]
		private static IEnumerable<IReadOnlyRow> GetInvolvedFeatures(
			[NotNull] ConnectedLinesEx connectedLines,
			bool includeIrrelevantNeighbors)
		{
			var involvedFeatures =
				new Dictionary<RowKey, IReadOnlyRow>(new RowKeyComparer());
			var neighboreRows =
				new Dictionary<RowKey, IReadOnlyRow>(new RowKeyComparer());

			foreach (
				ConnectedSegmentsSubpart segments in connectedLines.Line.BaseSegments)
			{
				var key = new RowKey(segments.BaseFeature, segments.TableIndex);
				if (! involvedFeatures.ContainsKey(key))
				{
					involvedFeatures.Add(key, segments.BaseFeature);
				}

				AddFeatures(neighboreRows, segments.SegmentNeighbors);
			}

			if (! includeIrrelevantNeighbors)
			{
				var irrelevantRows = new List<RowKey>();
				foreach (RowKey neighborRow in neighboreRows.Keys)
				{
					bool isRelevant = IsRelevant(connectedLines.Line, neighborRow);
					if (! isRelevant)
					{
						irrelevantRows.Add(neighborRow);
					}
				}

				foreach (RowKey irrelevantRow in irrelevantRows)
				{
					neighboreRows.Remove(irrelevantRow);
				}
			}

			foreach (KeyValuePair<RowKey, IReadOnlyRow> pair in neighboreRows)
			{
				if (! involvedFeatures.ContainsKey(pair.Key))
				{
					involvedFeatures.Add(pair.Key, pair.Key.Row);
				}
			}

			return involvedFeatures.Values;
		}

		private static bool IsRelevant([NotNull] ConnectedLines connectedLines,
		                               [NotNull] RowKey neighborRow)
		{
			var parts =
				new Dictionary<ConnectedSegmentsSubpart, NeighboredSegmentsSubpart>();

			foreach (ConnectedSegmentsSubpart part in connectedLines.BaseSegments)
			{
				SegmentNeighbors copyNeighbors = part.SegmentNeighbors.Select(
					p =>
					{
						var neighboredPart =
							p as
								SegmentPartWithNeighbor;

						if (neighboredPart ==
						    null)
						{
							return false;
						}

						if (neighborRow.Row ==
						    neighboredPart
							    .NeighborFeature)
						{
							return false;
						}

						return true;
					});

				var copy = new NeighboredSegmentsSubpart(part, copyNeighbors);

				parts.Add(part, copy);
			}

			foreach (
				KeyValuePair<ConnectedSegmentsSubpart, NeighboredSegmentsSubpart> pair
				in parts)
			{
				NeighboredSegmentsSubpart part = pair.Value;
				IList<SubClosedCurve> connectedParts;
				IList<SubClosedCurve> selfConnected;

				GetSubcurves(part.BaseSegments, part.SegmentNeighbors.Values, 0,
				             0, false, out connectedParts, out selfConnected);
				if (connectedParts.Count != 1)
				{
					return true;
				}

				if (Math.Abs(connectedParts[0].StartFullIndex -
				             pair.Key.ConnectedCurve.StartFullIndex) > _epsi ||
				    Math.Abs(connectedParts[0].EndFullIndex -
				             pair.Key.ConnectedCurve.EndFullIndex) >
				    _epsi)
				{
					return true;
				}
			}

			return false;
		}

		private static void AddFeatures([NotNull] IDictionary<RowKey, IReadOnlyRow> rows,
		                                [NotNull] SegmentNeighbors segmentNeighbors)
		{
			foreach (SegmentParts neighbors in segmentNeighbors.Values)
			{
				foreach (SegmentPart neighbor in neighbors)
				{
					var nb = neighbor as SegmentPartWithNeighbor;
					if (nb == null)
					{
						continue;
					}

					RowKey key = nb.CreateNeighborRowKey();
					if (! rows.ContainsKey(key))
					{
						rows.Add(key, key.Row);
					}
				}
			}
		}

		[NotNull]
		private static IPolyline GetErrorGeometry([NotNull] ConnectedLines line)
		{
			var lines = new List<IGeometry>();

			foreach (ConnectedSegmentsSubpart part in line.BaseSegments)
			{
				lines.Add(part.ConnectedCurve.GetGeometry());
			}

			if (lines.Count == 1)
			{
				return (IPolyline) lines[0];
			}

			return (IPolyline) GeometryFactory.CreateUnion(lines, 0);
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (args.State == TileState.Initial)
			{
				_junctions.Clear();
				Assert.NotNull(args.AllBox).QueryWKSCoords(out _allEnvelope);
				return base.CompleteTileCore(args);
			}

			var errorCount = 0;

			Assert.NotNull(args.CurrentEnvelope).QueryWKSCoords(out _currentEnvelope);

			CompleteJunctions();
			Dictionary<FeaturePoint, Dictionary<FeaturePoint, FeaturePoint>>
				groupedJunctions = GetJunctions(_junctions);

			Dictionary<IReadOnlyFeature, List<FeaturePoint>> featureJunctions =
				GetFeatureJunctions(groupedJunctions.Keys);

			Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>> splittedParts =
				GetSplittedParts(featureJunctions);

			int coincidentErrorCount;
			var coincidentAdapter = new CoincidentPartsAdapter(
				NearDistanceProvider,
				AllowCoincidentSections
					? (Func<CoincidenceError, int>) null
					: ReportCoincidentError);
			coincidentAdapter.DropCoincidentParts(splittedParts, out coincidentErrorCount);
			errorCount += coincidentErrorCount;

			var continuationFinder = new ContinuationFinder(groupedJunctions,
			                                                splittedParts);

			EnsureAllNotReportedInitialized();

			if (NearDistanceProvider is SideDistanceProvider)
			{
				var sideDistanceProvider = (SideDistanceProvider) NearDistanceProvider;
				var adapter = new AssymetricNearAdapter(sideDistanceProvider, Is3D);
				adapter.AdaptAssymetry(splittedParts);
			}

			if (UnconnectedLineCapStyle == LineCapStyle.Butt ||
			    EndCapStyle == LineCapStyle.Butt && ! string.IsNullOrEmpty(JunctionIsEndExpression))
			{
				var adapter = new LineEndsAdapter(
					              NearDistanceProvider, Is3D,
					              _allNotReportedConditions?.NotReportedCondition)
				              {
					              AdaptUnconnected = UnconnectedLineCapStyle == LineCapStyle.Butt,
					              JunctionIsEndExpression =
						              EndCapStyle == LineCapStyle.Butt
							              ? JunctionIsEndExpression
							              : null,
					              ReportShortSubpartError = ReportShortSubpartError,
					              ReportAngledEndError = IgnoreInconsistentLineSymbolEnds
						                                     ? (Func<AngleEndError, int>) null
						                                     : ReportAngledEndError
				              };

				int lineEndsErrorCount;
				adapter.AdaptLineEnds(splittedParts, continuationFinder,
				                      out lineEndsErrorCount);
				errorCount += lineEndsErrorCount;
			}

			var errorCandidates = new List<ConnectedLinesEx>();

			var errorCleanup = new ErrorCleanup(SegmentRelationsToCheck,
			                                    _allNotReportedConditions);

			foreach (
				ConnectedLinesEx errorCandidate in
				GetConnectedErrors(splittedParts, continuationFinder))
			{
				foreach (
					ConnectedLinesEx reportedErrorCandidate in
					errorCleanup.CleanupNotReportedPairs(errorCandidate))
				{
					reportedErrorCandidate.CircularNeighbor =
						errorCandidate.CircularNeighbor;
					reportedErrorCandidate.WithinNear = errorCandidate.WithinNear;
					errorCandidates.Add(reportedErrorCandidate);
				}
			}

			DropConnectedParts(splittedParts);

			foreach (List<NeighboredSegmentsSubpart> featureParts
			         in splittedParts.Values)
			{
				foreach (NeighboredSegmentsSubpart part in featureParts)
				{
					foreach (
						ConnectedLinesEx errorCandidate in
						GetDisjointErrorCandidates(part, continuationFinder))
					{
						foreach (ConnectedLinesEx lineWithoutNotReported in
						         errorCleanup.CleanupNotReportedPairs(errorCandidate))
						{
							if (lineWithoutNotReported != errorCandidate)
							{
								ConnectedLinesEx reduced =
									GetReducedConflict(lineWithoutNotReported);
								if (! IsCandidate(reduced))
								{
									continue;
								}
							}

							errorCandidates.Add(lineWithoutNotReported);
						}
					}
				}
			}

			var incompletedJunctions =
				new Dictionary<FeaturePoint, FeaturePoint>(new FeaturePointComparer());
			var incompletedRows =
				new Dictionary<RowKey, SegmentNeighbors>(new RowKeyComparer());

			IEnumerable<ConnectedLinesEx> filteredErrors =
				FilterSubparts(errorCandidates);
			foreach (ConnectedLinesEx error in filteredErrors)
			{
				bool processInLaterTile;
				errorCount += ReportError(error, out processInLaterTile);

				if (processInLaterTile)
				{
					AssembleIncompleted(error.JoinedParts.Values, groupedJunctions,
					                    incompletedRows,
					                    incompletedJunctions);
				}
			}

			//			errorCount += base.CompleteTileCore(args);

			_junctions.Clear();
			var jctComparer = new FeaturePointComparer();
			foreach (FeaturePoint jct in incompletedJunctions.Keys)
			{
				foreach (FeaturePoint otherJct in groupedJunctions[jct].Values)
				{
					if (jctComparer.Equals(jct, otherJct))
					{
						continue;
					}

					if (incompletedJunctions.ContainsKey(otherJct))
					{
						_junctions.Add(new Junction(
							               jct.Feature, jct.TableIndex, jct.Part,
							               jct.FullFraction,
							               otherJct.Feature, otherJct.TableIndex,
							               otherJct.Part,
							               otherJct.FullFraction));
					}
				}
			}

			ProcessedList.Clear();
			foreach (KeyValuePair<RowKey, SegmentNeighbors> pair in incompletedRows)
			{
				ProcessedList.Add(pair.Key, pair.Value);
			}

			return errorCount;
		}

		[NotNull]
		private IEnumerable<ConnectedLinesEx> GetConnectedErrors(
			[NotNull] Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>>
				splittedParts,
			[NotNull] ContinuationFinder continuationFinder)
		{
			foreach (
				KeyValuePair<FeaturePoint, List<NeighboredSegmentsSubpart>> pair in
				splittedParts)
			{
				List<NeighboredSegmentsSubpart> parts = pair.Value;

				var toCheck = new List<NeighboredSegmentsSubpart>(parts.Count * 2);
				foreach (NeighboredSegmentsSubpart part in parts)
				{
					toCheck.Add(part);

					var inverted = new SegmentsSubpart(part.BaseFeature,
					                                   part.TableIndex,
					                                   part.BaseSegments,
					                                   part.PartIndex,
					                                   part.FullEndFraction,
					                                   part.FullStartFraction);
					toCheck.Add(new NeighboredSegmentsSubpart(inverted,
					                                          part.SegmentNeighbors));
				}

				foreach (NeighboredSegmentsSubpart part in toCheck)
				{
					var search = new FeaturePoint(
						part.BaseFeature, part.TableIndex, part.PartIndex,
						part.FullStartFraction);

					List<NeighboredSegmentsSubpart> continuations =
						continuationFinder.GetContinuations(
							search, new[] { part }, breakOnExcludeFound: false);
					if (continuations == null)
					{
						continue;
					}

					foreach (NeighboredSegmentsSubpart continuation in continuations)
					{
						var errorCount = 0;
						var isCircular = false;

						var connectedPartsGrower = new ConnectedPartsGrower(part);
						var startConnected = new ConnectedPartsContinuer(part, 0,
							continuationFinder);
						var startNeighbored =
							new ConnectedPartsNeighbor(
								continuation, 0,
								new List<NeighboredSegmentsSubpart> { continuation },
								continuationFinder);
						foreach (ConnectedPartsGrower connected in
						         GetConnected(connectedPartsGrower, startConnected,
						                      startNeighbored,
						                      addToPreviousIn: true))
						{
							ConnectedLines connectedLines =
								connected.GetConnectedLines();

							if (connected.CircularNeighbor != null)
							{
								connected.CircularNeighbor.CompleteCircle(
									connectedLines.BaseSegments);
							}

							MarkConnectedParts(splittedParts, connectedLines);

							var ex =
								new ConnectedLinesEx(
									connectedLines,
									GetMinLength(connectedLines.BaseSegments,
									             ConnectedMinLengthProvider));
							ex.CircularNeighbor = connected.CircularNeighbor;

							bool report = ex.Length > ex.MinLength || ex.IsCircular;

							if (! report)
							{
								if (connected.IsFullyCovered())
								{
									ex.WithinNear =
										connectedLines.IsWithinDistance(NearDistanceProvider);
									report = ex.WithinNear;
								}
							}

							if (report)
							{
								isCircular |= ex.IsCircular;
								errorCount++;
								yield return ex;

								if (isCircular && MaxConnectionErrors > 0 &&
								    errorCount > MaxConnectionErrors)
								{
									break;
								}
							}
						}
					}
				}
			}
		}

		private static IEnumerable<SegmentParts> EnumSegmentParts(
			[NotNull] IEnumerable<List<NeighboredSegmentsSubpart>> splittedParts)
		{
			foreach (List<NeighboredSegmentsSubpart> splitted in splittedParts)
			{
				foreach (NeighboredSegmentsSubpart part in splitted)
				{
					foreach (SegmentParts segmentParts in part.SegmentNeighbors.Values)
					{
						yield return segmentParts;
					}
				}
			}
		}

		private static void MarkConnectedParts(
			[NotNull] Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>>
				splittedParts,
			[NotNull] ConnectedLines connectedLines)
		{
			var featurePartsDict =
				new Dictionary<FeaturePoint, List<ConnectedSegmentsSubpart>>(
					new FeaturePointComparer());

			foreach (
				ConnectedSegmentsSubpart connectedLine in connectedLines.BaseSegments)
			{
				var key = new FeaturePoint(connectedLine.BaseFeature,
				                           connectedLine.TableIndex,
				                           connectedLine.PartIndex, 0);
				List<ConnectedSegmentsSubpart> sameFeatureConnectedParts;
				if (! featurePartsDict.TryGetValue(key, out sameFeatureConnectedParts))
				{
					sameFeatureConnectedParts = new List<ConnectedSegmentsSubpart>();
					featurePartsDict.Add(key, sameFeatureConnectedParts);
				}

				sameFeatureConnectedParts.Add(connectedLine);
			}

			foreach (
				ConnectedSegmentsSubpart connectedPart in connectedLines.BaseSegments)
			{
				var featureKey = new FeaturePoint(
					connectedPart.BaseFeature, connectedPart.TableIndex,
					connectedPart.PartIndex, 0);

				List<NeighboredSegmentsSubpart> featureParts;
				if (! splittedParts.TryGetValue(featureKey, out featureParts))
				{
					continue;
				}

				// mark parts in connected lines
				foreach (NeighboredSegmentsSubpart featurePart in featureParts)
				{
					foreach (
						KeyValuePair<SegmentPart, SegmentParts> pair in
						featurePart.SegmentNeighbors)
					{
						SegmentParts neighbors = pair.Value;

						foreach (SegmentPart segmentPart in neighbors)
						{
							var neighbor = (SegmentPartWithNeighbor) segmentPart;
							if (neighbor.IsConnected)
							{
								continue;
							}

							var key = new FeaturePoint(neighbor.NeighborFeature,
							                           neighbor.NeighborTableIndex,
							                           neighbor.NeighborProxy
							                                   .PartIndex, 0);
							List<ConnectedSegmentsSubpart> sameFeatureConnectedParts;
							if (! featurePartsDict.TryGetValue(
								    key, out sameFeatureConnectedParts))
							{
								continue;
							}

							foreach (
								ConnectedSegmentsSubpart connected in
								sameFeatureConnectedParts)
							{
								if (neighbor.MinFraction < connected.FullMaxFraction &&
								    neighbor.MaxFraction > connected.FullMinFraction)
								{
									neighbor.IsConnected = true;
									break;
								}
							}
						}
					}
				}

				// mark neighbors
				foreach (
					KeyValuePair<SegmentPart, SegmentParts> pair in
					connectedPart.SegmentNeighbors)
				{
					foreach (SegmentPart segmentPart in pair.Value)
					{
						var neighbor = (SegmentPartWithNeighbor) segmentPart;
						neighbor.IsConnected = true;
					}
				}
			}
		}

		private static void DropConnectedParts(
			[NotNull] Dictionary<FeaturePoint, List<NeighboredSegmentsSubpart>>
				splittedParts)
		{
			foreach (
				List<NeighboredSegmentsSubpart> featureParts in splittedParts.Values)
			{
				foreach (NeighboredSegmentsSubpart featurePart in featureParts)
				{
					foreach (
						SegmentParts neighbors in featurePart.SegmentNeighbors.Values)
					{
						var hasConnecteds = false;
						var remaining = new List<SegmentPart>(neighbors.Count);
						foreach (SegmentPart part in neighbors)
						{
							var segmentPart = (SegmentPartWithNeighbor) part;
							if (segmentPart.IsConnected)
							{
								hasConnecteds = true;
							}
							else
							{
								remaining.Add(part);
							}
						}

						if (hasConnecteds)
						{
							neighbors.Clear();
							neighbors.AddRange(remaining);
						}
					}
				}
			}
		}

		private static int Compare([NotNull] NeighboredSegmentsSubpart featurePart,
		                           double featureAura,
		                           [NotNull] SegmentPartWithNeighbor neighbor,
		                           double neighborAura)
		{
			int d = neighborAura.CompareTo(featureAura);
			if (d != 0)
			{
				return d;
			}

			d = featurePart.TableIndex.CompareTo(neighbor.NeighborTableIndex);
			if (d != 0)
			{
				return d;
			}

			d = featurePart.BaseFeature.OID.CompareTo(neighbor.NeighborFeature.OID);
			if (d != 0)
			{
				return d;
			}

			d = featurePart.PartIndex.CompareTo(neighbor.NeighborProxy.PartIndex);
			if (d != 0)
			{
				return d;
			}

			d =
				featurePart.FullMinFraction.CompareTo(
					neighbor.NeighborProxy.SegmentIndex);
			return d;
		}

		private int ReportError([NotNull] ConnectedLinesEx errorEx,
		                        out bool processInLaterTile)
		{
			var notFullyProcessed = false;

			if (! errorEx.IsCircular)
			{
				if (errorEx.JoinedBox.Max.X > _currentEnvelope.XMax)
				{
					if (_currentEnvelope.XMax < _allEnvelope.XMax)
					{
						processInLaterTile = true;
						return NoError;
					}

					notFullyProcessed = true;
				}

				if (errorEx.JoinedBox.Max.Y > _currentEnvelope.YMax)
				{
					if (_currentEnvelope.YMax < _allEnvelope.YMax)
					{
						processInLaterTile = true;
						return NoError;
					}

					notFullyProcessed = true;
				}

				if (errorEx.JoinedBox.Max.Y < _currentEnvelope.YMin)
				{
					processInLaterTile = false;
					return NoError;
				}

				if (errorEx.JoinedBox.Max.X < _currentEnvelope.XMin)
				{
					processInLaterTile = false;
					return NoError;
				}

				if (errorEx.JoinedBox.Min.Y < _allEnvelope.YMin)
				{
					notFullyProcessed = true;
				}

				if (errorEx.JoinedBox.Min.X < _allEnvelope.XMin)
				{
					notFullyProcessed = true;
				}
			}

			ConnectedLines error = errorEx.Line;

			IPolyline errorGeometry = GetErrorGeometry(error);
			InvolvedRows involved = GetInvolvedRows(errorEx);
			ISpatialReference spatialReference = errorGeometry.SpatialReference;

			IssueCode code;
			string description;

			if (error.RelevantSegment != null)
			{
				if (! errorEx.WithinNear)
				{
					SegmentRelation relation = error.RelevantSegment.Relation;
					description = string.Format(
						relation.DescriptionTemplate,
						FormatLength(errorEx.Length, spatialReference).Trim(),
						FormatLength(errorEx.MinLength, spatialReference).Trim());

					code = notFullyProcessed
						       ? relation.ErrorCode_NotFullyProcessed
						       : relation.ErrorCode;
				}
				else
				{
					description = LocalizableStrings
						.QaTopoNotNear_NearlyCoincidentSection_WithinNear;

					code = Codes[Code.NearlyCoincidentSection_WithinNear];
				}
			}
			else if (errorEx.CircularNeighbor != null)
			{
				if (IgnoreLoopsWithinNearDistance)
				{
					processInLaterTile = false;
					return NoError;
				}

				var lines = new List<IGeometry> { errorGeometry };
				lines.AddRange(errorEx.CircularNeighbor.GetGeometry());
				errorGeometry = (IPolyline) GeometryFactory.CreateUnion(lines, 0);

				if (errorEx.WithinNear)
				{
					description = LocalizableStrings
						.QaTopoNotNear_NearlyCoincidentSection_Connected_Loop_WithinNear;
					code = Codes[Code.NearlyCoincidentSection_Connected_Loop_WithinNear];
				}
				else
				{
					description = LocalizableStrings
						.QaTopoNotNear_NearlyCoincidentSection_Connected_Loop;
					code = Codes[Code.NearlyCoincidentSection_Connected_Loop];
				}
			}
			else if (errorEx.WithinNear)
			{
				description =
					LocalizableStrings.QaTopoNotNear_NearlyCoincidentSection_WithinNear_Connected;
				code = Codes[Code.NearlyCoincidentSection_WithinNear_Connected];
			}
			else
			{
				description = string.Format(
					LocalizableStrings.QaTopoNotNear_NearlyCoincidentSection_Connected,
					FormatLength(errorEx.Length, spatialReference).Trim(),
					FormatLength(errorEx.MinLength, spatialReference).Trim());

				code = notFullyProcessed
					       ? Codes[Code.NearlyCoincidentSection_Connected_NotFullyProcessed]
					       : Codes[Code.NearlyCoincidentSection_Connected];
			}

			processInLaterTile = false;
			return ReportError(description, involved, errorGeometry, code, null);
		}

		private int ReportCoincidentError([NotNull] CoincidenceError coincidenceError)
		{
			IssueCode code = Codes[Code.CoincidentSection];

			InvolvedRows involved = InvolvedRowUtils.GetInvolvedRows(
				coincidenceError.DroppedPart.NeighborFeature,
				coincidenceError.UsedPart.BaseFeature);

			NeighboredSegmentsSubpart errorPart = coincidenceError.UsedPart;
			double fullMin = errorPart.FullMinFraction;
			double fullMax = errorPart.FullMaxFraction;
			var iMin = (int) fullMin;
			var iMax = (int) fullMax;
			if (iMax > 0 && iMax == fullMax) // TODO always true (FullMaxFraction is int also)
			{
				iMax--;
			}

			IGeometry errorGeometry =
				coincidenceError.UsedPart.BaseSegments.GetSubpart(errorPart.PartIndex,
					iMin, fullMin - iMin,
					iMax, fullMax - iMax);
			return ReportError(LocalizableStrings.QaTopoNotNear_CoincidentSectionFound,
			                   involved, errorGeometry, code, null);
		}

		private int ReportShortSubpartError([NotNull] ShortSubpartError subpartError)
		{
			string description =
				string.Format(LocalizableStrings.QaTopoNotNear_ShortSubpart,
				              FormatComparison(subpartError.Length, "<", subpartError.MinLength,
				                               "N1"));

			IssueCode code = Codes[Code.ShortSubpart];

			SegmentsSubpart subpart = subpartError.SegmentsSubpart;
			InvolvedRows involved = InvolvedRowUtils.GetInvolvedRows(subpart.BaseFeature);

			IGeometry errorGeometry = subpart.BaseSegments.GetSubpart(
				subpart.PartIndex, subpart.FullMinFraction, 0,
				subpart.FullMaxFraction, 0);
			return ReportError(description, involved, errorGeometry, code, null);
		}

		private int ReportAngledEndError([NotNull] AngleEndError sharpAngleError)
		{
			string description = LocalizableStrings.QaTopoNotNear_InconsistentLineSymbolEnd;

			IssueCode code = Codes[Code.InconsistentLineSymbolEnd];

			SegmentsSubpart subpart = sharpAngleError.SegmentsSubpart;
			InvolvedRows involved = InvolvedRowUtils.GetInvolvedRows(subpart.BaseFeature);

			//IGeometry errorGeometry = GeometryFactory.CreatePoint(
			//	sharpAngleError.At.X, sharpAngleError.At.Y,
			//	subpart.BaseFeature.Shape.SpatialReference);

			const double z = 0; // TODO revise
			IGeometry errorGeometry = GeometryFactory.CreatePolyline(
				sharpAngleError.At.X, sharpAngleError.At.Y, z,
				sharpAngleError.OtherEnd.X, sharpAngleError.OtherEnd.Y, z,
				dontSimplify: true);

			errorGeometry.SpatialReference = subpart.BaseFeature.Shape.SpatialReference;

			return ReportError(description, involved, errorGeometry, code, null);
		}

		[Obsolete("Verify code")] private static readonly bool _neededForCircularErrors =
			false;

		[NotNull]
		private IEnumerable<ConnectedPartsGrower> GetConnected(
			[NotNull] ConnectedPartsGrower connectedSegments,
			[NotNull] ConnectedPartsContinuer startConnected,
			[NotNull] ConnectedPartsNeighbor startNeighbored,
			bool addToPreviousIn)
		{
			ConnectedPartsContinuer connected = startConnected;
			ConnectedPartsNeighbor neighbored = startNeighbored;

			bool addToPrevious = addToPreviousIn;
			IList<ConnectedSegmentParts> neighboredLastConnectedParts = null;

			while (true)
			{
				if (! connectedSegments.Add(neighbored, addToPrevious,
				                            neighboredLastConnectedParts))
				{
					AddToCompleted(connectedSegments, neighbored);

					yield return connectedSegments;
					yield break;
				}

				addToPrevious = false;
				neighboredLastConnectedParts = null;

				if (! connectedSegments.IsFullyCovered())
				{
					addToPrevious = true;
					List<ConnectedPartsNeighbor> nextNeighbors =
						neighbored.GetNextNeighbors();

					if (nextNeighbors.Count == 0)
					{
						yield return connectedSegments;
						yield break;
					}

					if (nextNeighbors.Count <= 1)
					{
						neighbored = nextNeighbors[0];
						continue;
					}

					foreach (ConnectedPartsNeighbor nextNeighbor in nextNeighbors)
					{
						ConnectedPartsGrower copy = connectedSegments.Copy();
						foreach (ConnectedPartsGrower nextConnected in
						         GetConnected(copy, connected, nextNeighbor,
						                      addToPreviousIn: true))
						{
							yield return nextConnected;
						}
					}

					yield break;
				}
				else if (_neededForCircularErrors)
				{
					List<ConnectedPartsNeighbor> nextNeighbors =
						neighbored.GetNextNeighbors();
					if (nextNeighbors.Count == 1)
					{
						ConnectedPartsNeighbor neighboredCandidate = nextNeighbors[0];
						if (neighboredCandidate.EqualPreNeighbors(neighbored))
						{
							IList<ConnectedSegmentParts> lastConnectedParts =
								connectedSegments.GetLastConnectedParts(neighboredCandidate);
							if (lastConnectedParts.Count > 0)
							{
								neighbored = neighboredCandidate;
								addToPrevious = true;
								neighboredLastConnectedParts = lastConnectedParts;
								continue;
							}
						}
					}
				}

				List<ConnectedPartsContinuer> nextConnecteds =
					connected.GetNextConnecteds(connectedSegments.Subparts);

				if (nextConnecteds.Count == 0)
				{
					AddToCompleted(connectedSegments, neighbored);

					yield return connectedSegments;
					yield break;
				}

				if (nextConnecteds.Count == 1)
				{
					ConnectedPartsContinuer nextConnected = nextConnecteds[0];
					if (neighbored.IsEndEqual(nextConnected))
					{
						connectedSegments.CircularNeighbor = neighbored;
						yield return connectedSegments;
						yield break;
					}

					connectedSegments.Add(nextConnected);
					connected = nextConnected;
					continue;
				}

				foreach (ConnectedPartsContinuer nextConnected in nextConnecteds)
				{
					if (neighbored.IsEndEqual(nextConnected))
					{
						connectedSegments.CircularNeighbor = neighbored;
						yield return connectedSegments;
						continue;
					}

					ConnectedPartsGrower copy = connectedSegments.Copy();
					copy.Add(nextConnected);
					foreach (ConnectedPartsGrower next in
					         GetConnected(copy, nextConnected, neighbored,
					                      addToPreviousIn: false))
					{
						yield return next;
					}
				}

				yield break;
			}
		}

		private static void AddToCompleted(
			[NotNull] ConnectedPartsGrower connectedSegments,
			[NotNull] ConnectedPartsNeighbor neighbored)
		{
			List<ConnectedPartsNeighbor> nextNeighbors = neighbored.GetNextNeighbors();

			foreach (ConnectedPartsNeighbor nextNeighbor in nextNeighbors)
			{
				if (connectedSegments.AddToPreviousParts(nextNeighbor,
				                                         addToLastFraction: true))
				{
					AddToCompleted(connectedSegments, nextNeighbor);
				}
			}
		}

		#region Nested types

		private class CrossingRelation : SegmentRelation
		{
			public CrossingRelation([NotNull] IPairDistanceProvider distanceProvider)
				: base(distanceProvider) { }

			public override bool IsRelated(SegmentPair2D segmentPair)
			{
				return segmentPair.SegmentDistance <= 0;
			}

			public override string DescriptionTemplate
				=> LocalizableStrings.QaTopoNotNear_NearlyCoincidentSection_Crossing;

			public override IssueCode ErrorCode
				=> Codes[Code.NearlyCoincidentSection_Crossing];

			public override IssueCode ErrorCode_NotFullyProcessed
				=> Codes[Code.NearlyCoincidentSection_Crossing_NotFullyProcessed];
		}

		private class DisjointRelation : SegmentRelation
		{
			public DisjointRelation(IPairDistanceProvider distanceProvider)
				: base(distanceProvider) { }

			public override bool IsRelated(SegmentPair2D segmentPair)
			{
				return true;
			}

			public override string DescriptionTemplate
				=> LocalizableStrings.QaTopoNotNear_NearlyCoincidentSection_Disjoint;

			public override IssueCode ErrorCode => Codes[Code.NearlyCoincidentSection_Disjoint];

			public override IssueCode ErrorCode_NotFullyProcessed
				=> Codes[Code.NearlyCoincidentSection_Disjoint_NotFullyProcessed];
		}

		protected class NotReportedPairCondition
		{
			private readonly RowPairCondition _selfCondition;
			private readonly RowPairCondition _neighborCondition;

			public NotReportedPairCondition([CanBeNull] string notReportedCondition,
			                                bool caseSensitivity)
			{
				_selfCondition = new ValidRelationConstraint(
					notReportedCondition,
					constraintIsDirected: false,
					caseSensitive: caseSensitivity);
				_neighborCondition = new ValidRelationConstraint(
					notReportedCondition,
					constraintIsDirected: true,
					caseSensitive: caseSensitivity);
			}

			public bool IsFulfilled([NotNull] IReadOnlyFeature feature, int tableIndex,
			                        [NotNull] IReadOnlyFeature neighbor, int neighborTableIndex)
			{
				RowPairCondition condition = tableIndex == neighborTableIndex
					                             ? _selfCondition
					                             : _neighborCondition;
				return condition.IsFulfilled(feature, tableIndex, neighbor, neighborTableIndex);
			}
		}

		protected abstract class SegmentRelation
		{
			protected SegmentRelation([NotNull] IPairDistanceProvider distanceProvider)
			{
				DistanceProvider = distanceProvider;
			}

			[NotNull]
			public IPairDistanceProvider DistanceProvider { get; }

			[NotNull]
			public abstract string DescriptionTemplate { get; }

			public abstract bool IsRelated([NotNull] SegmentPair2D segmentPair);

			public abstract IssueCode ErrorCode { get; }

			public abstract IssueCode ErrorCode_NotFullyProcessed { get; }
		}

		protected class AllNotReportedPairConditions
		{
			[CanBeNull]
			public string NotReportedConditionText { get; }

			private NotReportedPairCondition _notReportedCondition;
			private bool _isNotReportedConditionInitialized;

			private readonly bool _sqlCaseSensitivity;

			[CanBeNull]
			public string IgnoreNeighborConditionText { get; }

			private IgnoreRowNeighborCondition _ignoreRowNeighborCondition;
			private IgnoreRowNeighborCondition _ignoreRowSelfCondition;
			private bool _isIgnoreNeighborConditionInitialized;

			public AllNotReportedPairConditions(string ignoreNeighborCondition,
			                                    string notReportedCondition,
			                                    bool sqlCaseSensitivity)
			{
				IgnoreNeighborConditionText = ignoreNeighborCondition;
				NotReportedConditionText = notReportedCondition;
				_sqlCaseSensitivity = sqlCaseSensitivity;
			}

			[CanBeNull]
			public NotReportedPairCondition NotReportedCondition
				=> EnsureNotReportedInitialized();

			public bool IsFulfilled(IReadOnlyFeature row1, int tableIndex1, IReadOnlyFeature row2,
			                        int tableIndex2)
			{
				if (NotReportedCondition?.IsFulfilled(row1, tableIndex1, row2, tableIndex2) ==
				    true)
				{
					return true;
				}

				return IgnoreNeighbor(row1, tableIndex1, row2, tableIndex2);
			}

			private void EnsureIgnoreNeighborInitialized()
			{
				if (_isIgnoreNeighborConditionInitialized)
				{
					return;
				}

				if (! string.IsNullOrEmpty(IgnoreNeighborConditionText))
				{
					_ignoreRowNeighborCondition = new IgnoreRowNeighborCondition(
						IgnoreNeighborConditionText, _sqlCaseSensitivity,
						isDirected: true);
					_ignoreRowSelfCondition = new IgnoreRowNeighborCondition(
						IgnoreNeighborConditionText, _sqlCaseSensitivity,
						isDirected: false);
				}

				_isIgnoreNeighborConditionInitialized = true;
			}

			[CanBeNull]
			private NotReportedPairCondition EnsureNotReportedInitialized()
			{
				if (_isNotReportedConditionInitialized)
				{
					return _notReportedCondition;
				}

				if (! string.IsNullOrEmpty(NotReportedConditionText))
				{
					_notReportedCondition = new NotReportedPairCondition(
						NotReportedConditionText, _sqlCaseSensitivity);
				}

				_isNotReportedConditionInitialized = true;
				return _notReportedCondition;
			}

			public bool IgnoreNeighbor([NotNull] IReadOnlyRow row, int tableIndex,
			                           [NotNull] IReadOnlyRow neighbor, int neighborTableIndex)
			{
				EnsureIgnoreNeighborInitialized();

				IgnoreRowNeighborCondition condition = tableIndex == neighborTableIndex
					                                       ? _ignoreRowSelfCondition
					                                       : _ignoreRowNeighborCondition;

				return condition?.IsFulfilled(row, tableIndex, neighbor, neighborTableIndex)
				       ?? false;
			}
		}

		protected class QaTopoNotNearConstructionHelper
		{
			public QaTopoNotNearDefinition Definition { get; }

			[CanBeNull] private ExpressionBasedDistanceProvider _expressionBasedDistanceProvider;

			public QaTopoNotNearConstructionHelper(QaTopoNotNearDefinition definition)
			{
				Definition = definition;
			}

			public IFeatureDistanceProvider GetNearExpressionProvider()
			{
				if (Definition.NearExpressionsProvider != null)
				{
					return ExpressionBasedDistanceProvider;
				}

				return new ConstantFeatureDistanceProvider(Definition.Near);
			}

			[CanBeNull]
			public ExpressionBasedDistanceProvider ExpressionBasedDistanceProvider
			{
				get
				{
					if (_expressionBasedDistanceProvider == null &&
					    Definition.NearExpressionsProvider != null)
					{
						_expressionBasedDistanceProvider =
							new ExpressionBasedDistanceProvider(Definition.NearExpressionsProvider);
					}

					return _expressionBasedDistanceProvider;
				}
			}

			public IPairDistanceProvider GetConnectedMinLengthProvider()
			{
				if (Definition.NearExpressionsProvider != null)
				{
					return new FactorDistanceProvider(
						Definition.ConnectedMinLengthFactor,
						Assert.NotNull(ExpressionBasedDistanceProvider));
				}

				return new ConstantPairDistanceProvider(
					Definition.ConnectedMinLengthConstantDistance);
			}

			public IPairDistanceProvider GetDefaultUnconnectedMinLengthProvider()
			{
				if (Definition.NearExpressionsProvider != null)
				{
					return new FactorDistanceProvider(
						Definition.DefaultUnconnectedMinLengthFactor,
						Assert.NotNull(ExpressionBasedDistanceProvider));
				}

				return new ConstantPairDistanceProvider(
					Definition.DefaultUnconnectedMinLengthConstantDistance);
			}

			public IList<IReadOnlyFeatureClass> GetAllFeatureClasses()
			{
				return Definition.AllFeatureClasses.Cast<IReadOnlyFeatureClass>().ToList();
			}
		}

		#endregion
	}
}
