using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.PointEnumerators;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	[TopologyTest]
	[UsedImplicitly]
	public class QaVertexCoincidenceSelf : QaSpatialRelationSelfBase
	{
		private const bool _defaultVerifyWithinFeature = false;
		private const double _defaultUseXyTolerance = -1;
		private const bool _defaultRequireVertexOnNearbyEdge = true;

		[CanBeNull] private IPointsEnumerator _pointsEnumerator;
		[NotNull] private readonly VertexCoincidenceChecker _vertexCoincidenceChecker;
		private const bool _defaultIs3D = false;
		[CanBeNull] private IEnvelope _pointSearchEnvelope;

		[CanBeNull] private readonly string _allowedNonCoincidenceConditionSql;
		[CanBeNull] private IValidRelationConstraint _allowedNonCoincidenceCondition;

		#region issue codes

		[CanBeNull] private static ITestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes =>
			_codes ?? (_codes = new AggregatedTestIssueCodes(
				           VertexCoincidenceChecker.Codes));

		#endregion

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_0))]
		public QaVertexCoincidenceSelf(
			[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass)
			: this(new[] {featureClass}) { }

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_1))]
		public QaVertexCoincidenceSelf(
				[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_featureClasses))] [NotNull]
				IList<IReadOnlyFeatureClass>
					featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_2))]
		public QaVertexCoincidenceSelf(
			[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass>
				featureClasses,
			[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_allowedNonCoincidenceCondition))]
			[CanBeNull]
			string
				allowedNonCoincidenceCondition)
			: base(featureClasses, esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			double maxXYTolerance = featureClasses
			                        .Select(fc => DatasetUtils.TryGetXyTolerance(
				                                      fc.SpatialReference, out double xyTolerance)
				                                      ? xyTolerance
				                                      : 0).Max();

			_vertexCoincidenceChecker =
				new VertexCoincidenceChecker(
					this, FormatComparison,
					maxXYTolerance)
				{
					Is3D = _defaultIs3D,
					VerifyWithinFeature = _defaultVerifyWithinFeature,
					PointTolerance = _defaultUseXyTolerance,
					EdgeTolerance = _defaultUseXyTolerance,
					RequireVertexOnNearbyEdge = _defaultRequireVertexOnNearbyEdge
				};

			_allowedNonCoincidenceConditionSql =
				StringUtils.IsNotEmpty(allowedNonCoincidenceCondition)
					? allowedNonCoincidenceCondition
					: null;
			AddCustomQueryFilterExpression(allowedNonCoincidenceCondition);

			UpdateSearchDistance();
		}

		[InternallyUsedTest]
		public QaVertexCoincidenceSelf([NotNull] QaVertexCoincidenceSelfDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.AllowedNonCoincidenceCondition)
		{
			PointTolerance = definition.PointTolerance;
			EdgeTolerance = definition.EdgeTolerance;
			RequireVertexOnNearbyEdge = definition.RequireVertexOnNearbyEdge;
			CoincidenceTolerance = definition.CoincidenceTolerance;
			Is3D = definition.Is3D;
			VerifyWithinFeature = definition.VerifyWithinFeature;
			ZTolerance = definition.ZTolerance;
			ZCoincidenceTolerance = definition.ZCoincidenceTolerance;
			ReportCoordinates = definition.ReportCoordinates;
		}

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_PointTolerance))]
		[TestParameter(_defaultUseXyTolerance)]
		public double PointTolerance
		{
			get { return _vertexCoincidenceChecker.PointTolerance; }
			set
			{
				_vertexCoincidenceChecker.PointTolerance = value;
				UpdateSearchDistance();
			}
		}

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_EdgeTolerance))]
		[TestParameter(_defaultUseXyTolerance)]
		public double EdgeTolerance
		{
			get { return _vertexCoincidenceChecker.EdgeTolerance; }
			set
			{
				_vertexCoincidenceChecker.EdgeTolerance = value;
				UpdateSearchDistance();
			}
		}

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_RequireVertexOnNearbyEdge))]
		[TestParameter(_defaultRequireVertexOnNearbyEdge)]
		public bool RequireVertexOnNearbyEdge
		{
			get { return _vertexCoincidenceChecker.RequireVertexOnNearbyEdge; }
			set { _vertexCoincidenceChecker.RequireVertexOnNearbyEdge = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_CoincidenceTolerance))]
		[TestParameter(0)]
		public double CoincidenceTolerance
		{
			get { return _vertexCoincidenceChecker.CoincidenceTolerance; }
			set { _vertexCoincidenceChecker.CoincidenceTolerance = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_Is3D))]
		[TestParameter(_defaultIs3D)]
		public bool Is3D
		{
			get { return _vertexCoincidenceChecker.Is3D; }
			set { _vertexCoincidenceChecker.Is3D = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_VerifyWithinFeature))]
		[TestParameter(_defaultVerifyWithinFeature)]
		public bool VerifyWithinFeature
		{
			get { return _vertexCoincidenceChecker.VerifyWithinFeature; }
			set { _vertexCoincidenceChecker.VerifyWithinFeature = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_ZTolerance))]
		[TestParameter(0)]
		public double ZTolerance
		{
			get { return _vertexCoincidenceChecker.ZTolerance; }
			set { _vertexCoincidenceChecker.ZTolerance = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_ZCoincidenceTolerance))]
		[TestParameter(0)]
		public double ZCoincidenceTolerance
		{
			get { return _vertexCoincidenceChecker.ZCoincidenceTolerance; }
			set { _vertexCoincidenceChecker.ZCoincidenceTolerance = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_ReportCoordinates))]
		[TestParameter]
		public bool ReportCoordinates
		{
			get { return _vertexCoincidenceChecker.ReportCoordinates; }
			set { _vertexCoincidenceChecker.ReportCoordinates = value; }
		}

		protected override void BeginTileCore(BeginTileParameters parameters)
		{
			base.BeginTileCore(parameters);

			for (var involvedTable = 0; involvedTable < InvolvedTables.Count; involvedTable++)
			{
				QueryFilterHelper filterHelper = GetQueryFilterHelper(involvedTable);
				filterHelper.ForNetwork = true;
			}

			_pointsEnumerator = null; // points enumerator is valid only within same tile

			_pointSearchEnvelope = null;
			if (parameters.TileEnvelope != null)
			{
				_pointSearchEnvelope = GeometryFactory.Clone(parameters.TileEnvelope);
			}

			_pointSearchEnvelope?.Expand(SearchDistance, SearchDistance, asRatio: false);
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			IgnoreUndirected = false;

			return base.ExecuteCore(row, tableIndex);
		}

		protected override IGeometry GetSearchGeometry(IReadOnlyFeature feature, int tableIndex)
		{
			IGeometry shape = feature.Shape;

			if (shape == null || shape.IsEmpty)
			{
				return shape;
			}

			IEnvelope result = shape.Envelope;

			// property returns a copy, no need to clone

			if (result.IsEmpty)
			{
				return result;
			}

			const bool asRatio = false;
			result.Expand(SearchDistance, SearchDistance, asRatio);

			return result;
		}

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			var feature1 = row1 as IReadOnlyFeature;
			var feature2 = row2 as IReadOnlyFeature;
			if (feature1 == null || feature2 == null)
			{
				return NoError;
			}

			if (! _vertexCoincidenceChecker.VerifyWithinFeature && feature1 == feature2)
			{
				return NoError;
			}

			if (_allowedNonCoincidenceConditionSql != null && feature1 != feature2)
			{
				if (_allowedNonCoincidenceCondition == null)
				{
					const bool constraintIsDirected = false;
					_allowedNonCoincidenceCondition = new ValidRelationConstraint(
						_allowedNonCoincidenceConditionSql,
						constraintIsDirected,
						GetSqlCaseSensitivity());
				}

				if (_allowedNonCoincidenceCondition.IsFulfilled(row1, tableIndex1,
				                                                row2, tableIndex2,
				                                                out string _))
				{
					// non-coincidence is allowed between these two features
					return NoError;
				}
			}

			if (_pointsEnumerator == null || _pointsEnumerator.Feature != feature1)
			{
				_pointsEnumerator = PointsEnumeratorFactory.Create(feature1,
					_pointSearchEnvelope);
			}

			return _vertexCoincidenceChecker.CheckCoincidence(_pointsEnumerator, feature2);
		}

		private void UpdateSearchDistance()
		{
			SearchDistance = _vertexCoincidenceChecker.SearchDistance;
		}
	}
}
