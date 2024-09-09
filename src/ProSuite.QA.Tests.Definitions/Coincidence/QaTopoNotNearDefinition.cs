using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Coincidence
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaTopoNotNearDefinition : AlgorithmDefinition
	{
		private const ConnectionMode _defaultConnectionMode =
			ConnectionMode.EndpointOnVertex;

		private const bool _defaultIgnoreLoopsWithinNearDistance = false;
		private const bool _defaultIgnoreInconsistentLineSymbolEnds = false;
		private const bool _defaultAllowCoincidentSections = false;
		private const LineCapStyle _defaultUnconnectedLineCapStyle = LineCapStyle.Round;
		private const LineCapStyle _defaultEndCapStyle = LineCapStyle.Round;
		private const double _defaultCrossingMinLengthFactor = -1.0;

		[NotNull]
		public IList<IFeatureClassSchemaDef> AllFeatureClasses { get; set; }

		public Dictionary<int, IFeatureClassSchemaDef> TopoTables { get; } =
			new Dictionary<int, IFeatureClassSchemaDef>();

		public Dictionary<int, IFeatureClassSchemaDef> ConflictTables { get; } =
			new Dictionary<int, IFeatureClassSchemaDef>();

		public double Near { get; set; }
		public bool Is3D { get; set; }

		[NotNull]
		public bool? IsDirected { get; }

		[CanBeNull] private IList<string> _rightSideNears;

		// Feature Distance
		public ExpressionBasedDistanceProviderDefinition NearExpressionsProvider { get; set; }
		public double ConstantFeatureDistance { get; set; }

		// Connected Min Length
		public double ConnectedMinLengthFactor { get; set; }
		public double ConnectedMinLengthConstantDistance { get; set; }

		// Default Unconnected Min Length
		public double DefaultUnconnectedMinLengthFactor { get; set; }
		public double DefaultUnconnectedMinLengthConstantDistance { get; set; }

		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaTopoNotNearDefinition(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IFeatureClassSchemaDef featureClass,
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
		public QaTopoNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
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
			       is3D)
		{
			ConstantFeatureDistance = near / 2;
			ConnectedMinLengthConstantDistance = minLength;
			DefaultUnconnectedMinLengthConstantDistance = minLength;

			TopoTables.Add(0, featureClass);
			ConflictTables.Add(0, featureClass);
			IsDirected = false;
		}

		// ctor 2
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNearDefinition(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaNotNear_reference))]
				IFeatureClassSchemaDef reference,
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
		public QaTopoNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IFeatureClassSchemaDef reference,
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
			       is3D)
		{
			ConstantFeatureDistance = near / 2;
			ConnectedMinLengthConstantDistance = minLength;
			DefaultUnconnectedMinLengthConstantDistance = minLength;

			TopoTables.Add(0, featureClass);
			ConflictTables.Add(1, reference);
			IsDirected = true;

			ConnectionMode = _defaultConnectionMode;
			UnconnectedLineCapStyle = _defaultUnconnectedLineCapStyle;
			IgnoreLoopsWithinNearDistance = _defaultIgnoreLoopsWithinNearDistance;
		}

		// ctor 4
		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaTopoNotNearDefinition(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, near, minLength, false, 1000.0) { }

		// ctor 5
		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaTopoNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: this(featureClass, near, minLength, false, tileSize) { }

		// ctor 6
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNearDefinition(
				[Doc(nameof(DocStrings.QaNotNear_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaNotNear_reference))]
				IFeatureClassSchemaDef reference,
				[Doc(nameof(DocStrings.QaNotNear_near))]
				double near,
				[Doc(nameof(DocStrings.QaNotNear_minLength))]
				double minLength)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, reference, near, minLength, false, 1000.0) { }

		// ctor 7
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IFeatureClassSchemaDef reference,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double minLength,
			[Doc(nameof(DocStrings.QaNotNear_tileSize))]
			double tileSize)
			: this(featureClass, reference, near, minLength, false) { }

		// ctor 8
		[Doc(nameof(DocStrings.QaNotNear_0))]
		public QaTopoNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[NotNull] string nearExpression,
			double connectedMinLengthFactor,
			double defaultUnconnectedMinLengthFactor,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D)
			: this(featureClass,
			       near,
			       new ExpressionBasedDistanceProviderDefinition(new[] { nearExpression },
			                                                     new[] { featureClass }),
			       connectedMinLengthFactor,
			       defaultUnconnectedMinLengthFactor,
			       is3D) { }

		// ctor 9
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IFeatureClassSchemaDef reference,
			[Doc(nameof(DocStrings.QaNotNear_near))]
			double near,
			[Doc(nameof(DocStrings.QaNotNear_minLength))]
			double connectedMinLengthFactor,
			double defaultUnconnectedMinLengthFactor,
			[Doc(nameof(DocStrings.QaNotNear_is3D))]
			bool is3D)
			: this(new[] { featureClass, reference },
			       near / 2,
			       is3D)
		{
			ConstantFeatureDistance = near / 2;
			ConnectedMinLengthConstantDistance = connectedMinLengthFactor * (near / 2);
			DefaultUnconnectedMinLengthConstantDistance =
				defaultUnconnectedMinLengthFactor * (near / 2);

			TopoTables.Add(0, featureClass);
			ConflictTables.Add(1, reference);
			IsDirected = true;

			ConnectionMode = _defaultConnectionMode;
			UnconnectedLineCapStyle = _defaultUnconnectedLineCapStyle;
			IgnoreLoopsWithinNearDistance = _defaultIgnoreLoopsWithinNearDistance;
		}

		// ctor 10
		[Doc(nameof(DocStrings.QaNotNear_2))]
		public QaTopoNotNearDefinition(
			[Doc(nameof(DocStrings.QaNotNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaNotNear_reference))]
			IFeatureClassSchemaDef reference,
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
			       new ExpressionBasedDistanceProviderDefinition(
				       new[] { featureClassNear, referenceNear },
				       new[] { featureClass, reference }),
			       connectedMinLengthFactor,
			       defaultUnconnectedMinLengthFactor,
			       is3D)
		{
			IsDirected = true;

			ConnectionMode = _defaultConnectionMode;
			UnconnectedLineCapStyle = _defaultUnconnectedLineCapStyle;
			IgnoreLoopsWithinNearDistance = _defaultIgnoreLoopsWithinNearDistance;
		}

		protected QaTopoNotNearDefinition(
			IFeatureClassSchemaDef featureClass,
			double near,
			ExpressionBasedDistanceProviderDefinition nearExpressionsProvider,
			double connectedMinLengthFactor,
			double defaultUnconnectedMinLengthFactor,
			bool is3D)
			: this(new[] { featureClass },
			       near,
			       is3D)
		{
			NearExpressionsProvider = nearExpressionsProvider;
			ConnectedMinLengthFactor = connectedMinLengthFactor;
			DefaultUnconnectedMinLengthFactor = defaultUnconnectedMinLengthFactor;

			TopoTables.Add(0, featureClass);
			ConflictTables.Add(0, featureClass);
			IsDirected = false;
		}

		protected QaTopoNotNearDefinition(
			[NotNull] IFeatureClassSchemaDef featureClass,
			[NotNull] IFeatureClassSchemaDef reference,
			double near,
			[NotNull] ExpressionBasedDistanceProviderDefinition nearExpressionsProvider,
			double connectedMinLengthFactor,
			double defaultUnconnectedMinLengthFactor,
			bool is3D)
			: this(new[] { featureClass, reference },
			       near,
			       is3D)
		{
			NearExpressionsProvider = nearExpressionsProvider;
			ConnectedMinLengthFactor = connectedMinLengthFactor;
			DefaultUnconnectedMinLengthFactor = defaultUnconnectedMinLengthFactor;

			TopoTables.Add(0, featureClass);
			ConflictTables.Add(1, reference);
			IsDirected = true;
		}

		private QaTopoNotNearDefinition(
			[NotNull] IList<IFeatureClassSchemaDef> featureClasses,
			double near,
			bool is3D)
			: base(featureClasses)
		{
			AllFeatureClasses = featureClasses;
			Near = near;
			Is3D = is3D;
		}

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
			set { _rightSideNears = value; }
		}

		[TestParameter(_defaultEndCapStyle)]
		public LineCapStyle EndCapStyle { get; set; } = _defaultEndCapStyle;

		[TestParameter]
		public string JunctionIsEndExpression { get; set; }
	}
}
