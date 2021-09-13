using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaIsCoveredByOther : ContainerTest
	{
		private readonly double _allowedUncoveredPercentage;
		private readonly int _coveringClassesCount; // # of covering feature classes
		private readonly int _coveredClassesCount; // # of covered feature classes
		private readonly int _areaOfInterestClassesCount; // # of aoi feature classes

		private readonly int _totalClassesCount; // total # of feature classes

		[NotNull] private readonly IDictionary<int, Dictionary<int, UnCoveredFeature>>
			_uncoveredFeatures = new Dictionary<int, Dictionary<int, UnCoveredFeature>>();

		[NotNull] private readonly IDictionary<int, SimpleSet<int>>
			_featuresKnownCovered = new Dictionary<int, SimpleSet<int>>();

		[NotNull] private readonly IDictionary<int, string>
			_affectedComponentsByClassIndex = new Dictionary<int, string>();

		[NotNull] private readonly IDictionary<int, bool>
			_hasIsCoveredConditionByCoveredClassIndex = new Dictionary<int, bool>();

		[NotNull] private readonly List<int> _coveringClassIndexes;
		[NotNull] private readonly List<int> _areaOfInterestClassIndexes;

		private bool _isCoveredConditionsInitialized;
		private QueryFilterHelper[] _helper;
		private ISpatialFilter[] _queryFilter;

		[CanBeNull] private readonly List<string> _isCoveringConditionsSql;
		[NotNull] private readonly List<GeometryComponent> _geometryComponents;
		[NotNull] private readonly List<esriGeometryType> _coveringShapeTypes;

		[NotNull] private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();
		private readonly bool _mostlyContainedInOneFeature;

		[NotNull] private readonly Dictionary<double, BufferFactory> _bufferFactories =
			new Dictionary<double, BufferFactory>();

		[NotNull] private readonly IDictionary<int, CoveringGeometryCache> _coveringGeometryCaches
			= new Dictionary<int, CoveringGeometryCache>();

		[CanBeNull] private GeometryConstraint _validUncoveredGeometryConstraint;

		private List<IsCoveringCondition> _isCoveringConditions;
		private double _tileEnvelopeXMin = double.NaN;
		private double _tileEnvelopeYMin = double.NaN;

		//TODO find reasonable maximum point count
		private const int _maximumBufferCachePointCount = 1000000;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private IList<double> _coveringClassTolerances =
			new ReadOnlyList<double>(new List<double>());

		private readonly IList<double> _coveringClassSpatialReferenceXyTolerances;

		[NotNull] private readonly Dictionary<int, double> _bufferDensifyDeviations =
			new Dictionary<int, double>();

		[CanBeNull] private IPoint _startPoint;
		[CanBeNull] private IPoint _endPoint;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string NotSufficientlyCovered = "NotSufficientlyCovered";
			public const string NotFullyCovered = "NotFullyCovered";
			public const string NotCoveredByAnyFeature = "NotCoveredByAnyFeature";

			public const string NotCoveredByAnyFeature_PartlyOutsideVerifiedExtent =
				"NotCoveredByAnyFeature.PartlyOutsideVerifiedExtent";

			public const string NotSufficientlyCovered_WithFulfilledConstraint =
				"NotSufficientlyCovered.WithFulfilledConstraint";

			public const string NotFullyCovered_WithFulfilledConstraint =
				"NotFullyCovered.WithFulfilledConstraint";

			public const string NotCoveredByAnyFeature_WithFulfilledConstraint =
				"NotCoveredByAnyFeature.WithFulfilledConstraint";

			public const string
				NotCoveredByAnyFeature_PartlyOutsideVerifiedExtent_WithFulfilledConstraint
					=
					"NotCoveredByAnyFeature.PartlyOutsideVerifiedExtent.WithFulfilledConstraint";

			public Code() : base("CoveredByOther") { }
		}

		#endregion

		#region Constructors

		[Doc(nameof(DocStrings.QaIsCoveredByOther_0))]
		public QaIsCoveredByOther(
				[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
				IList<IFeatureClass>
					covering,
				[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
				IList<IFeatureClass>
					covered)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(covering, covered, null) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_1))]
		public QaIsCoveredByOther(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_1))] [NotNull]
			IFeatureClass covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_1))] [NotNull]
			IFeatureClass covered)
			: this(new[] {covering}, new[] {covered}, null) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_2))]
		public QaIsCoveredByOther(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
			IList<IFeatureClass>
				covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
			IList<IFeatureClass>
				covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringCondition))]
			string isCoveringCondition)
			: this(covering, new[] {GeometryComponent.EntireGeometry},
			       covered, new[] {GeometryComponent.EntireGeometry},
			       isCoveringCondition) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_3))]
		public QaIsCoveredByOther(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_1))] [NotNull]
			IFeatureClass covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_1))] [NotNull]
			IFeatureClass covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringCondition))]
			string isCoveringCondition)
			: this(new[] {covering}, new[] {covered}, isCoveringCondition) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_4))]
		public QaIsCoveredByOther(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
			IList<IFeatureClass>
				covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveringGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveringGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
			IList<IFeatureClass>
				covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveredGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveredGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringCondition))] [CanBeNull]
			string
				isCoveringCondition)
			: this(covering, coveringGeometryComponents,
			       covered, coveredGeometryComponents,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       isCoveringCondition, 0d) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_5))]
		public QaIsCoveredByOther(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
			IList<IFeatureClass>
				covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveringGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveringGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
			IList<IFeatureClass>
				covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveredGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveredGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringCondition))] [CanBeNull]
			string
				isCoveringCondition,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_allowedUncoveredPercentage))]
			double
				allowedUncoveredPercentage)
			: this(covering, coveringGeometryComponents,
			       covered, coveredGeometryComponents,
			       string.IsNullOrEmpty(isCoveringCondition)
				       ? null
				       : new[] {isCoveringCondition},
			       allowedUncoveredPercentage) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_6))]
		public QaIsCoveredByOther(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
			IList<IFeatureClass>
				covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveringGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveringGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
			IList<IFeatureClass>
				covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveredGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveredGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringConditions))] [CanBeNull]
			IList<string>
				isCoveringConditions,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_allowedUncoveredPercentage))]
			double
				allowedUncoveredPercentage)
			: this(covering, coveringGeometryComponents,
			       covered, coveredGeometryComponents,
			       isCoveringConditions,
			       allowedUncoveredPercentage,
			       new List<IFeatureClass>()) { }

		[Doc(nameof(DocStrings.QaIsCoveredByOther_7))]
		public QaIsCoveredByOther(
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covering_0))] [NotNull]
			IList<IFeatureClass>
				covering,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveringGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveringGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_covered_0))] [NotNull]
			IList<IFeatureClass>
				covered,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_coveredGeometryComponents_0))] [NotNull]
			IList<GeometryComponent> coveredGeometryComponents,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_isCoveringConditions))] [CanBeNull]
			IList<string>
				isCoveringConditions,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_allowedUncoveredPercentage))]
			double
				allowedUncoveredPercentage,
			[Doc(nameof(DocStrings.QaIsCoveredByOther_areaOfInterestClasses))] [NotNull]
			IList<IFeatureClass>
				areaOfInterestClasses)
			: base(CastToTables(covering, covered, areaOfInterestClasses))
		{
			Assert.ArgumentNotNull(covering, nameof(covering));
			Assert.ArgumentNotNull(covered, nameof(covered));
			Assert.ArgumentNotNull(coveringGeometryComponents,
			                       nameof(coveringGeometryComponents));
			Assert.ArgumentNotNull(coveredGeometryComponents,
			                       nameof(coveredGeometryComponents));
			Assert.ArgumentCondition(allowedUncoveredPercentage >= 0,
			                         "allowed uncovered percentage must be >= 0");
			Assert.ArgumentCondition(allowedUncoveredPercentage < 100,
			                         "allowed uncovered percentage must be < 100");
			Assert.ArgumentCondition(isCoveringConditions == null ||
			                         isCoveringConditions.Count == 0 ||
			                         isCoveringConditions.Count == 1 ||
			                         isCoveringConditions.Count ==
			                         covering.Count * covered.Count,
			                         "unexpected number of isCovering conditions " +
			                         "(must be 0, 1, or # of covering classes * # of covered classes");

			_allowedUncoveredPercentage = allowedUncoveredPercentage;

			_isCoveringConditionsSql = isCoveringConditions == null ||
			                           isCoveringConditions.Count == 0
				                           ? null
				                           : isCoveringConditions.ToList();

			_coveringClassesCount = covering.Count;
			_coveredClassesCount = covered.Count;
			_areaOfInterestClassesCount = areaOfInterestClasses.Count;

			_totalClassesCount = _coveringClassesCount + _coveredClassesCount +
			                     _areaOfInterestClassesCount;

			_geometryComponents = new List<GeometryComponent>(
				GetGeometryComponents(covering, coveringGeometryComponents,
				                      covered, coveredGeometryComponents));

			_mostlyContainedInOneFeature = false;

			_areaOfInterestClassIndexes = GetAreaOfInterestClassIndexes().ToList();
			_coveringClassIndexes = GetCoveringClassIndexes().ToList();
			_coveringShapeTypes = covering.Select(featureClass => featureClass.ShapeType)
			                              .ToList();

			_coveringClassSpatialReferenceXyTolerances = new List<double>();
			foreach (IFeatureClass featureClass in covering)
			{
				double xyTolerance;
				if (! DatasetUtils.TryGetXyTolerance(featureClass, out xyTolerance))
				{
					xyTolerance = double.Epsilon;
				}

				_coveringClassSpatialReferenceXyTolerances.Add(xyTolerance);
			}
		}

		#endregion

		[Doc(nameof(DocStrings.QaIsCoveredByOther_CoveringClassTolerances))]
		[TestParameter]
		public IList<double> CoveringClassTolerances
		{
			get { return _coveringClassTolerances; }
			set
			{
				Assert.ArgumentCondition(value == null ||
				                         value.Count == 0 || value.Count == 1 ||
				                         value.Count == _coveringClassesCount,
				                         "unexpected number of covering class tolerance values " +
				                         "(must be 0, 1, or equal to the number of covering classes");

				double maximumTolerance = value == null
					                          ? 0
					                          : GetMaximumTolerance(value);

				SearchDistance = maximumTolerance;

				_coveringClassTolerances =
					new ReadOnlyList<double>(value?.ToList() ?? new List<double>());
			}
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaIsCoveredByOther_ValidUncoveredGeometryConstraint))]
		public string ValidUncoveredGeometryConstraint
		{
			get { return _validUncoveredGeometryConstraint?.Constraint; }
			set
			{
				_validUncoveredGeometryConstraint = StringUtils.IsNullOrEmptyOrBlank(value)
					                                    ? null
					                                    : new GeometryConstraint(value);
			}
		}

		//// TODO remove
		// [TestParameter]
		public bool UseSecondaryFilterForTolerance { get; [UsedImplicitly] set; }

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			if (_queryFilter == null)
			{
				InitFilter();
			}

			if (! IsCoveredClassIndex(tableIndex))
			{
				// only test features from "covered" classes
				return NoError;
			}

			EnsureIsCoveredConditionsInitialized();

			var feature = (IFeature) row;

			if (IsFeatureKnownCovered(tableIndex, feature))
			{
				return NoError;
			}

			IGeometry uncoveredGeometry = GetUncoveredGeometry(tableIndex, feature);

			if (uncoveredGeometry == null || uncoveredGeometry.IsEmpty)
			{
				return NoError;
			}

			// TODO at least for covering:component=point: tiling artefacts, not all errors
			// are found when features cross tile boundaries
			// --> use FilterHelper.ForNetwork = true if covering geometry component evaluates from >0d to 0d (e.g. line to point)

			var searcher = new CoveringFeatureSearcher(Assert.NotNull(_queryFilter), _helper,
			                                           Search, GetTolerance,
			                                           _tileEnvelopeXMin,
			                                           _tileEnvelopeYMin,
			                                           UseSecondaryFilterForTolerance);

			var intersectingCount = 0;
			foreach (int coveringClassIndex in _coveringClassIndexes)
			{
				var coveringClass = (IFeatureClass) InvolvedTables[coveringClassIndex];

				foreach (IFeature intersectingFeature in searcher.Search(uncoveredGeometry,
				                                                         coveringClass,
				                                                         coveringClassIndex))
				{
					if (TestUtils.IsSameRow(intersectingFeature, feature) &&
					    ! TreatSameFeatureAsCovering(feature, tableIndex, coveringClassIndex))
					{
						continue;
					}

					if (! IsCoveringConditionFulfilled(row, tableIndex,
					                                   intersectingFeature,
					                                   coveringClassIndex))
					{
						continue;
					}

					intersectingCount++;

					IGeometry coveringGeometry =
						GetCoveringGeometry(coveringClassIndex,
						                    intersectingFeature);

					if (coveringGeometry == null || coveringGeometry.IsEmpty)
					{
						continue;
					}

					uncoveredGeometry = GetRemainingUnCoveredGeometry(
						uncoveredGeometry,
						coveringGeometry);

					if (uncoveredGeometry == null || uncoveredGeometry.IsEmpty)
					{
						// no difference outside of covering features left --> covered
						FlagFeatureAsCovered(tableIndex, feature);
						return NoError;
					}
				}
			}

			// there is a remaining uncovered geometry, remember it
			StoreUncoveredGeometry(tableIndex, feature, uncoveredGeometry,
			                       intersectingCount);

			return NoError;
		}

		private bool IsCoveredClassIndex(int tableIndex)
		{
			if (tableIndex < _coveringClassesCount)
			{
				// covering class
				return false;
			}

			if (tableIndex >= _coveredClassesCount + _coveringClassesCount)
			{
				// area of interest class
				return false;
			}

			// covered class
			return true;
		}

		private bool TreatSameFeatureAsCovering([NotNull] IFeature feature,
		                                        int coveredClassIndex,
		                                        int coveringClassIndex)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			// treat same feature as "covering" if the polyline is closed and both 
			// covering and covered use a geometry component that corresponds to
			// one or both end points

			if (_coveringShapeTypes[coveringClassIndex] !=
			    esriGeometryType.esriGeometryPolyline)
			{
				// not a polyline
				return false;
			}

			GeometryComponent coveredComponent = _geometryComponents[coveredClassIndex];

			if (coveredComponent != GeometryComponent.LineEndPoints &&
			    coveredComponent != GeometryComponent.Boundary &&
			    coveredComponent != GeometryComponent.LineEndPoint &&
			    coveredComponent != GeometryComponent.LineStartPoint)
			{
				// not an end point geometry component
				return false;
			}

			GeometryComponent coveringComponent = _geometryComponents[coveringClassIndex];

			if (coveringComponent != GeometryComponent.LineEndPoints &&
			    coveringComponent != GeometryComponent.Boundary &&
			    coveringComponent != GeometryComponent.LineEndPoint &&
			    coveringComponent != GeometryComponent.LineStartPoint)
			{
				// not an end point geometry component
				return false;
			}

			var polyLine = feature.Shape as IPolyline;

			// treat as covering if the line forms a closed loop
			if (polyLine == null || polyLine.IsEmpty)
			{
				return false;
			}

			if (polyLine.IsClosed)
			{
				return true;
			}

			double tolerance = GetTolerance(coveringClassIndex);

			double endPointDistance = GetEndPointDistance(polyLine);

			return endPointDistance <= tolerance;
		}

		private double GetEndPointDistance([NotNull] IPolyline polyLine)
		{
			if (_startPoint == null)
			{
				_startPoint = new PointClass();
			}

			if (_endPoint == null)
			{
				_endPoint = new PointClass();
			}

			polyLine.QueryFromPoint(_startPoint);
			polyLine.QueryToPoint(_endPoint);

			return GeometryUtils.GetPointDistance(_startPoint, _endPoint);
		}

		private void EnsureIsCoveredConditionsInitialized()
		{
			if (_isCoveredConditionsInitialized)
			{
				return;
			}

			if (_isCoveringConditionsSql != null)
			{
				_isCoveringConditions =
					new List<IsCoveringCondition>(_isCoveringConditionsSql.Count);

				foreach (string isCoveringCondition in _isCoveringConditionsSql)
				{
					_isCoveringConditions.Add(
						new IsCoveringCondition(isCoveringCondition,
						                        GetSqlCaseSensitivity()));
				}
			}

			foreach (int coveredClassIndex in GetCoveredClassIndexes())
			{
				_hasIsCoveredConditionByCoveredClassIndex.Add(coveredClassIndex,
				                                              HasIsCoveredConditions(
					                                              coveredClassIndex));
			}

			_isCoveredConditionsInitialized = true;
		}

		private static double GetMaximumTolerance(
			[NotNull] IEnumerable<double> tolerances)
		{
			double maximumValue = 0;

			foreach (double tolerance in tolerances)
			{
				Assert.True(tolerance >= 0,
				            "Invalid tolerance value: {0} (must be >= 0)",
				            tolerance);

				if (tolerance > maximumValue)
				{
					maximumValue = tolerance;
				}
			}

			return maximumValue;
		}

		private static double GetBufferDensifyDeviation(
			[NotNull] IFeatureClass featureClass)
		{
			ISpatialReference spatialReference =
				DatasetUtils.GetSpatialReference(featureClass);

			Assert.NotNull(spatialReference,
			               "spatial reference not defined for {0}",
			               DatasetUtils.GetName(featureClass));

			double xyTolerance =
				((ISpatialReferenceTolerance) spatialReference).XYTolerance;

			double xyResolution =
				((ISpatialReferenceResolution) spatialReference).XYResolution[true];

			return Math.Max(xyTolerance / 2, xyResolution * 2);
		}

		[CanBeNull]
		private CoveringGeometryCache GetCache(int tableIndex)
		{
			if (! UseCoveringGeometryCache(tableIndex))
			{
				return null;
			}

			CoveringGeometryCache cache;
			if (! _coveringGeometryCaches.TryGetValue(tableIndex, out cache))
			{
				cache = new CoveringGeometryCache(_maximumBufferCachePointCount);
				_coveringGeometryCaches.Add(tableIndex, cache);
			}

			return cache;
		}

		private bool UseCoveringGeometryCache(int coveringClassIndex)
		{
			double tolerance = GetTolerance(coveringClassIndex);

			if (! RequireBuffer(coveringClassIndex, tolerance))
			{
				// TODO still create the cache if geometry component is expensive to create
				return false;
			}

			return true;
		}

		[CanBeNull]
		private IGeometry GetCoveringGeometry(int coveringClassIndex,
		                                      [NotNull] IFeature coveringFeature)
		{
			CoveringGeometryCache coveringGeometryCache = GetCache(coveringClassIndex);

			IGeometry coveringGeometry;

			if (coveringGeometryCache != null &&
			    coveringGeometryCache.TryGet(coveringFeature.OID, out coveringGeometry))
			{
				return coveringGeometry;
			}

			coveringGeometry = GetGeometryComponent(coveringClassIndex,
			                                        coveringFeature);

			double tolerance = GetTolerance(coveringClassIndex);

			if (coveringGeometry == null ||
			    ! RequireBuffer(coveringClassIndex, tolerance))
			{
				coveringGeometryCache?.Put(coveringFeature.OID, coveringGeometry);

				return coveringGeometry;
			}

			coveringGeometry = CreateBuffer(coveringGeometry, tolerance,
			                                GetDensifyDeviation(coveringClassIndex));

			// TODO if all(?) involved feature classes are Z-aware: make sure that buffer result is also Z aware and has reasonable Z values

			coveringGeometryCache?.Put(coveringFeature.OID, coveringGeometry);

			return coveringGeometry;
		}

		private bool RequireBuffer(int coveringClassIndex, double coveringClassTolerance)
		{
			double xyTolerance = _coveringClassSpatialReferenceXyTolerances[coveringClassIndex];

			return Math.Abs(coveringClassTolerance) > xyTolerance;
		}

		private double GetDensifyDeviation(int coveringClassIndex)
		{
			var featureClass = (IFeatureClass) InvolvedTables[coveringClassIndex];

			double result;

			if (! _bufferDensifyDeviations.TryGetValue(coveringClassIndex,
			                                           out result))
			{
				result = GetBufferDensifyDeviation(featureClass);
				_bufferDensifyDeviations.Add(coveringClassIndex, result);
			}

			return result;
		}

		[NotNull]
		private IGeometry CreateBuffer([NotNull] IGeometry bufferInput,
		                               double bufferDistance, double densifyDeviation)
		{
			BufferFactory bufferFactory;
			if (! _bufferFactories.TryGetValue(densifyDeviation, out bufferFactory))
			{
				const bool densify = true;
				const bool explodeBuffers = false;
				bufferFactory = new BufferFactory(explodeBuffers, densify)
				                {
					                DensifyDeviation = densifyDeviation
				                };
				_bufferFactories.Add(densifyDeviation, bufferFactory);
			}

			IList<IPolygon> bufferOutput = bufferFactory.Buffer(bufferInput, bufferDistance);
			Assert.AreEqual(1, bufferOutput.Count,
			                "Unexpected number of buffer results: {0}",
			                bufferOutput.Count);

			return bufferOutput[0];
		}

		[NotNull]
		private IEnumerable<int> GetCoveringClassIndexes()
		{
			for (var classIndex = 0;
			     classIndex < _coveringClassesCount;
			     classIndex++)
			{
				yield return classIndex;
			}
		}

		[NotNull]
		private IEnumerable<int> GetAreaOfInterestClassIndexes()
		{
			for (int classIndex = _coveredClassesCount + _coveringClassesCount;
			     classIndex <
			     _coveredClassesCount + _coveringClassesCount + _areaOfInterestClassesCount;
			     classIndex++)
			{
				yield return classIndex;
			}
		}

		[NotNull]
		private IEnumerable<int> GetCoveredClassIndexes()
		{
			for (int classIndex = _coveringClassesCount;
			     classIndex < _coveredClassesCount + _coveringClassesCount;
			     classIndex++)
			{
				yield return classIndex;
			}
		}

		private bool IsCoveringConditionFulfilled([NotNull] IRow coveredRow,
		                                          int coveredClassIndex,
		                                          [NotNull] IRow coveringRow,
		                                          int coveringClassIndex)
		{
			IsCoveringCondition isCoveringCondition = GetIsCoveringCondition(
				coveredClassIndex, coveringClassIndex);

			if (isCoveringCondition == null)
			{
				return true;
			}

			return isCoveringCondition.IsFulfilled(
				coveringRow, coveringClassIndex,
				coveredRow, coveredClassIndex);
		}

		[CanBeNull]
		private IsCoveringCondition GetIsCoveringCondition(int coveredClassIndex,
		                                                   int coveringClassIndex)
		{
			if (_isCoveringConditionsSql == null)
			{
				// no condition defined --> fulfilled
				return null;
			}

			int index;
			if (_isCoveringConditionsSql.Count == 1)
			{
				index = 0;
			}
			else
			{
				int localCoveredClassIndex = coveredClassIndex - _coveringClassesCount;
				index = localCoveredClassIndex * _coveringClassesCount + coveringClassIndex;
			}

			return _isCoveringConditions[index];
		}

		private bool HasIsCoveredConditions(int coveredClassIndex)
		{
			return GetIsCoveringConditions(coveredClassIndex)
				.Any(condition => condition != null &&
				                  ! string.IsNullOrEmpty(condition.Condition));
		}

		[NotNull]
		private IEnumerable<IsCoveringCondition> GetIsCoveringConditions(
			int coveredClassIndex)
		{
			if (_isCoveringConditions == null || _isCoveringConditions.Count == 0)
			{
				yield break;
			}

			if (_isCoveringConditions.Count == 1)
			{
				yield return _isCoveringConditions[0];
			}
			else
			{
				foreach (int coveringClassIndex in _coveringClassIndexes)
				{
					yield return
						GetIsCoveringCondition(coveredClassIndex, coveringClassIndex);
				}
			}
		}

		[CanBeNull]
		private IGeometry GetRemainingUnCoveredGeometry(
			[NotNull] IGeometry geometry,
			[NotNull] IGeometry coveringGeometry)
		{
			GeometryUtils.AllowIndexing(coveringGeometry);

			esriGeometryType coveredShapeType = geometry.GeometryType;

			if (coveredShapeType == esriGeometryType.esriGeometryPoint)
			{
				// is not guaranteed to be intersecting, since it might be a 
				// component of the geometry --> check for disjoint.
				// uses xy tolerance of COVERED geometry
				bool disjoint = ((IRelationalOperator) geometry).Disjoint(coveringGeometry);

				return disjoint
					       ? geometry
					       : null; // point intersects -> remainder is empty				
			}

			// the covered shape type is line, polygon, multipoint, or multipatch

			esriGeometryType coveringShapeType = coveringGeometry.GeometryType;

			if (coveringShapeType == esriGeometryType.esriGeometryPolygon ||
			    coveringShapeType == esriGeometryType.esriGeometryMultiPatch)
			{
				// the covering shape type is (at least) 2-dimensional
				// -> try cheaper methods to determine if the covered feature is contained
				geometry.QueryEnvelope(_envelopeTemplate);

				var coveringRelOp = (IRelationalOperator) coveringGeometry;

				// uses xy tolerance of COVERING
				if (coveringRelOp.Contains(_envelopeTemplate))
				{
					// the envelope of the geometry is fully contained in the covering geometry
					return null;
				}

				if (_mostlyContainedInOneFeature)
				{
					// uses xy tolerance of COVERING
					if (coveringRelOp.Contains(geometry))
					{
						return null;
					}
				}
			}

			IGeometry geometryToSubtract =
				coveringShapeType == esriGeometryType.esriGeometryMultiPatch
					? GeometryFactory.CreatePolygon((IMultiPatch) coveringGeometry)
					: coveringGeometry;

			GeometryUtils.AllowIndexing(geometry);
			// uses xy tolerance of COVERED
			return ((ITopologicalOperator) geometry).Difference(geometryToSubtract);
		}

		protected override void BeginTileCore(BeginTileParameters parameters)
		{
			base.BeginTileCore(parameters);

			if (parameters.TileEnvelope == null)
			{
				_tileEnvelopeXMin = double.NaN;
				_tileEnvelopeYMin = double.NaN;
			}
			else
			{
				parameters.TileEnvelope.QueryCoords(out _tileEnvelopeXMin,
				                                    out _tileEnvelopeYMin,
				                                    out double _, out double _);
			}
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			IEnvelope tileEnvelope = args.CurrentEnvelope;

			if (tileEnvelope == null)
			{
				return NoError;
			}

			EvictCoveringGeometryCacheEntries(tileEnvelope);

			IEnvelope testRunEnvelope = args.AllBox;
			bool isLastTile = args.State == TileState.Final;
			var errorCount = 0;

			foreach (
				KeyValuePair<int, Dictionary<int, UnCoveredFeature>> pair in
				_uncoveredFeatures)
			{
				int tableIndex = pair.Key;
				Dictionary<int, UnCoveredFeature> unCoveredFeaturesByOID = pair.Value;

				var oidsToRemove = new List<int>();
				foreach (UnCoveredFeature unCoveredFeature in unCoveredFeaturesByOID.Values)
				{
					if (unCoveredFeature.IsFullyChecked(tileEnvelope, testRunEnvelope))
					{
						// the feature was fully checked in tiles processed so far. 
						errorCount += ReportError(unCoveredFeature, completelyTested: true);

						oidsToRemove.Add(unCoveredFeature.OID);
					}
					else if (isLastTile && testRunEnvelope != null)
					{
						// uncovered bits of the feature that are in the fully checked area 
						// should still be reported! --> clip with testRunEnvelope

						unCoveredFeature.Clip(testRunEnvelope);

						if (! unCoveredFeature.Geometry.IsEmpty)
						{
							errorCount += ReportError(unCoveredFeature,
							                          completelyTested: false);

							oidsToRemove.Add(unCoveredFeature.OID);
						}
					}
				}

				foreach (int oid in oidsToRemove)
				{
					unCoveredFeaturesByOID.Remove(oid);
				}

				SimpleSet<int> featuresKnownCovered;
				if (_featuresKnownCovered.TryGetValue(tableIndex,
				                                      out featuresKnownCovered))
				{
					foreach (int oid in oidsToRemove)
					{
						featuresKnownCovered.Remove(oid);
					}
				}
			}

			return errorCount;
		}

		private void EvictCoveringGeometryCacheEntries([NotNull] IEnvelope tileEnvelope)
		{
			double xMax;
			double yMax;
			tileEnvelope.QueryCoords(out double _, out double _, out xMax, out yMax);

			foreach (KeyValuePair<int, CoveringGeometryCache> pair in _coveringGeometryCaches)
			{
				int coveringClassIndex = pair.Key;
				CoveringGeometryCache cache = pair.Value;

				if (cache == null)
				{
					continue;
				}

				double tolerance = GetTolerance(coveringClassIndex);

				double evictableXMax = xMax + tolerance;
				double evictableYMax = yMax + tolerance;

				_msg.VerboseDebugFormat(
					"Removing cache entries for (xMax <= {0} && yMax <= {1}) for covering class index {2}",
					evictableXMax, evictableYMax, coveringClassIndex);

				// remove all entries that are fully to the left/bottom of the tile upper/right boundary 
				// (plus the tolerance, since the tolerance is guaranteed to be applied for searching 
				// features to the upper/right beyond the tile boundary)
				cache.Evict(entry => entry.XMax <= evictableXMax && entry.YMax <= evictableYMax);
			}
		}

		private double GetTolerance(int coveringClassIndex)
		{
			if (_coveringClassTolerances == null ||
			    _coveringClassTolerances.Count == 0)
			{
				return 0;
			}

			if (_coveringClassTolerances.Count == 1)
			{
				return _coveringClassTolerances[0];
			}

			Assert.True(coveringClassIndex >= 0 &&
			            coveringClassIndex < _coveringClassTolerances.Count,
			            "invalid tolerance count");

			return _coveringClassTolerances[coveringClassIndex];
		}

		[NotNull]
		private static IEnumerable<GeometryComponent> GetGeometryComponents(
			[NotNull] ICollection<IFeatureClass> covering,
			[NotNull] IList<GeometryComponent> coveringGeometryComponents,
			[NotNull] ICollection<IFeatureClass> covered,
			[NotNull] IList<GeometryComponent> coveredGeometryComponents)
		{
			foreach (GeometryComponent component in GetGeometryComponents(
				coveringGeometryComponents, covering.Count,
				"coveringGeometryComponents"))
			{
				yield return component;
			}

			foreach (GeometryComponent component in GetGeometryComponents(
				coveredGeometryComponents, covered.Count,
				"coveredGeometryComponents"))
			{
				yield return component;
			}
		}

		[NotNull]
		private static IEnumerable<GeometryComponent> GetGeometryComponents(
			[NotNull] IList<GeometryComponent> geometryComponents,
			int featureClassCount,
			[NotNull] string paramName)
		{
			Assert.ArgumentCondition(geometryComponents.Count < 2 ||
			                         geometryComponents.Count == featureClassCount,
			                         "geometry component count must either be 0, 1 or " +
			                         "equal to the number of corresponding feature classes",
			                         paramName);

			for (var i = 0; i < featureClassCount; i++)
			{
				switch (geometryComponents.Count)
				{
					case 0:
						yield return GeometryComponent.EntireGeometry;
						break;

					case 1:
						yield return geometryComponents[0];
						break;

					default:
						yield return geometryComponents[i];
						break;
				}
			}
		}

		/// <summary>
		/// Flags a feature as fully covered. This information is needed to avoid testing the 
		/// same feature again in the next tile (for features that overlap tile boundaries).
		/// </summary>
		/// <param name="tableIndex">Index of the table.</param>
		/// <param name="feature">The feature.</param>
		private void FlagFeatureAsCovered(int tableIndex, [NotNull] IFeature feature)
		{
			Dictionary<int, UnCoveredFeature> unCoveredGeometriesByOID;
			if (_uncoveredFeatures.TryGetValue(tableIndex,
			                                   out unCoveredGeometriesByOID))
			{
				unCoveredGeometriesByOID.Remove(feature.OID);
			}

			SimpleSet<int> featuresKnownCovered;
			if (! _featuresKnownCovered.TryGetValue(tableIndex,
			                                        out featuresKnownCovered))
			{
				featuresKnownCovered = new SimpleSet<int>();
				_featuresKnownCovered.Add(tableIndex, featuresKnownCovered);
			}

			featuresKnownCovered.Add(feature.OID);
		}

		private bool IsFeatureKnownCovered(int tableIndex, [NotNull] IFeature feature)
		{
			SimpleSet<int> featuresKnownCovered;
			return
				_featuresKnownCovered.TryGetValue(tableIndex, out featuresKnownCovered) &&
				featuresKnownCovered.Contains(feature.OID);
		}

		private void StoreUncoveredGeometry(int tableIndex,
		                                    [NotNull] IFeature feature,
		                                    [NotNull] IGeometry geometry,
		                                    int intersectingCount)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentCondition(! geometry.IsEmpty, "geometry is emtpy");

			Dictionary<int, UnCoveredFeature> unCoveredFeaturesByOID;
			if (
				! _uncoveredFeatures.TryGetValue(tableIndex,
				                                 out unCoveredFeaturesByOID))
			{
				unCoveredFeaturesByOID = new Dictionary<int, UnCoveredFeature>();
				_uncoveredFeatures.Add(tableIndex, unCoveredFeaturesByOID);
			}

			UnCoveredFeature unCoveredFeature;
			if (
				! unCoveredFeaturesByOID.TryGetValue(feature.OID, out unCoveredFeature))
			{
				unCoveredFeature = new UnCoveredFeature(feature, tableIndex);
				unCoveredFeaturesByOID.Add(feature.OID, unCoveredFeature);
			}

			unCoveredFeature.Update(geometry, intersectingCount);
		}

		/// <summary>
		/// Gets the geometry that was not (yet) found to be covered by features in the covering feature classes.
		/// </summary>
		/// <param name="tableIndex">Index of the table.</param>
		/// <param name="feature">The feature.</param>
		/// <returns>The geometry not covered by covering features (as of the tiles processed so far). 
		/// If no covering features were found prior to this call, the feature's entire geometry is
		/// returned (not a copy of it). Therefore the result should not be changed.</returns>
		[CanBeNull]
		private IGeometry GetUncoveredGeometry(int tableIndex,
		                                       [NotNull] IFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			Dictionary<int, UnCoveredFeature> unCoveredGeometriesByOID;
			if (_uncoveredFeatures.TryGetValue(tableIndex,
			                                   out unCoveredGeometriesByOID))
			{
				UnCoveredFeature unCoveredFeature;
				if (unCoveredGeometriesByOID.TryGetValue(feature.OID,
				                                         out unCoveredFeature))
				{
					return unCoveredFeature.Geometry;
				}
			}

			return IsInAreaOfInterest(feature)
				       ? GetGeometryComponent(tableIndex, feature)
				       : null;
		}

		private bool IsInAreaOfInterest([NotNull] IFeature feature)
		{
			if (_areaOfInterestClassesCount == 0)
			{
				return true;
			}

			foreach (int aoiClassIndex in _areaOfInterestClassIndexes)
			{
				ISpatialFilter filter = _queryFilter[aoiClassIndex];
				ITable table = InvolvedTables[aoiClassIndex];
				QueryFilterHelper helper = _helper[aoiClassIndex];

				filter.Geometry = feature.Shape;

				if (Search(table, filter, helper).OfType<IFeature>()
				                                 .Any())
				{
					// intersecting feature found
					return true;
				}
			}

			// does not intersect any aoi feature
			return false;
		}

		[CanBeNull]
		private IGeometry GetGeometryComponent(int tableIndex,
		                                       [NotNull] IFeature feature)
		{
			return GeometryComponentUtils.GetGeometryComponent(feature,
			                                                   _geometryComponents[tableIndex]);
		}

		private int ReportError([NotNull] UnCoveredFeature unCoveredFeature,
		                        bool completelyTested)
		{
			IFeature feature = unCoveredFeature.Feature;

			bool hasIsCoveredConditions =
				_hasIsCoveredConditionByCoveredClassIndex[unCoveredFeature.TableIndex];

			string completenessSuffix =
				completelyTested
					? string.Empty
					: "; Note: the feature extends beyond the verified extent, " +
					  "reported errors may be incomplete";

			IGeometry errorGeometry;
			if (unCoveredFeature.IntersectionCount == 0)
			{
				// not covered at all

				string uncoveredDescription =
					hasIsCoveredConditions
						? $"The feature is not covered by any feature that fulfills the attribute constraint{completenessSuffix}"
						: $"The feature is not covered by any feature{completenessSuffix}";

				string codeId = GetIssueCodeIdNotCoveredByAnyFeature(completelyTested,
				                                                     hasIsCoveredConditions);

				errorGeometry = GetErrorGeometry(unCoveredFeature, feature);
				if (errorGeometry == null)
				{
					return NoError;
				}

				if (_validUncoveredGeometryConstraint != null &&
				    _validUncoveredGeometryConstraint.IsFulfilled(errorGeometry))
				{
					return NoError;
				}

				return ReportError(uncoveredDescription,
				                   errorGeometry,
				                   Codes[codeId],
				                   GetAffectedComponent(unCoveredFeature.TableIndex),
				                   feature);
			}

			// there are some intersecting features, but the feature is not covered completely
			errorGeometry = GetErrorGeometry(unCoveredFeature, feature);

			if (errorGeometry == null)
			{
				// Simplify emptied the geometry, or the polygon is too small
				return NoError;
			}

			string partlyCoveredDescription;

			IssueCode issueCode;

			if (_allowedUncoveredPercentage > 0)
			{
				IGeometry geometryComponent = GetGeometryComponent(
					unCoveredFeature.TableIndex, feature);

				double uncoveredPercentage = GetUncoveredPercentage(
					geometryComponent, errorGeometry);
				if (uncoveredPercentage <= _allowedUncoveredPercentage)
				{
					// sufficiently covered
					return NoError;
				}

				// not sufficiently covered
				issueCode =
					hasIsCoveredConditions
						? Codes[Code.NotSufficientlyCovered_WithFulfilledConstraint]
						: Codes[Code.NotSufficientlyCovered];

				double coveredPercentage = 100 - uncoveredPercentage;
				partlyCoveredDescription = string.Format(
					"{0} (covered: {1:N2}%){2}",
					hasIsCoveredConditions
						? "The feature is not sufficiently covered by other features for which the attribute constraint is fulfilled"
						: "The feature is not sufficiently covered by other features",
					coveredPercentage,
					completenessSuffix);
			}
			else
			{
				issueCode = hasIsCoveredConditions
					            ? Codes[Code.NotFullyCovered_WithFulfilledConstraint]
					            : Codes[Code.NotFullyCovered];
				partlyCoveredDescription =
					hasIsCoveredConditions
						? $"The feature is not fully covered by other features for which the attribute constraint is fulfilled{completenessSuffix}"
						: $"The feature is not fully covered by other features{completenessSuffix}";
			}

			var errorCount = 0;

			foreach (IGeometry part in GeometryUtils.Explode(errorGeometry))
			{
				if (part.IsEmpty)
				{
					continue;
				}

				if (_validUncoveredGeometryConstraint != null &&
				    _validUncoveredGeometryConstraint.IsFulfilled(part))
				{
					continue;
				}

				errorCount += ReportError(partlyCoveredDescription, part,
				                          issueCode,
				                          GetAffectedComponent(unCoveredFeature.TableIndex),
				                          feature);
			}

			return errorCount;
		}

		[CanBeNull]
		private IGeometry GetErrorGeometry(
			[NotNull] UnCoveredFeature unCoveredFeature,
			[NotNull] IFeature feature)
		{
			IGeometry geometry = unCoveredFeature.Geometry;

			if (feature.Shape == geometry)
			{
				return GetWithinAreaOfInterest(feature.ShapeCopy);
			}

			// simplify the geometry; there may be a number of odd cases
			// - polygons with 3-point rings
			// - polygons with very short segments, causing the polygon
			//   to be emptied by the simplify
			GeometryUtils.Simplify(geometry,
			                       allowReorder: true,
			                       allowPathSplitAtIntersections: true);

			if (geometry.IsEmpty)
			{
				// the geometry got emptied, the difference was apparently not significant
				return null;
			}

			if (geometry.GeometryType == esriGeometryType.esriGeometryPolygon &&
			    ! IsSignificantPolygon((IPolygon) geometry))
			{
				return null;
			}

			return GetWithinAreaOfInterest(geometry);
		}

		[CanBeNull]
		private IGeometry GetWithinAreaOfInterest([NotNull] IGeometry geometry)
		{
			if (_areaOfInterestClassesCount == 0)
			{
				return geometry;
			}

			esriGeometryDimension dimension = geometry.Dimension;

			var intersections = new List<IGeometry>();

			foreach (int aoiClassIndex in _areaOfInterestClassIndexes)
			{
				ISpatialFilter filter = _queryFilter[aoiClassIndex];
				ITable table = InvolvedTables[aoiClassIndex];
				QueryFilterHelper helper = _helper[aoiClassIndex];

				filter.Geometry = geometry;

				foreach (IFeature aoiFeature in Search(table, filter, helper).OfType<IFeature>())
				{
					// intersecting feature found
					if (dimension == esriGeometryDimension.esriGeometry0Dimension)
					{
						// point intersects aoi --> relevant
						return geometry;
					}

					// reduce result to the part within the area of interest
					IGeometry intersection = IntersectionUtils.GetIntersection(geometry,
					                                                           aoiFeature.Shape);

					if (! intersection.IsEmpty)
					{
						intersections.Add(intersection);
					}
				}
			}

			if (intersections.Count == 0)
			{
				return null;
			}

			IGeometry union = GeometryUtils.UnionGeometries(intersections);

			return union.IsEmpty ? null : union;
		}

		[NotNull]
		private static string GetIssueCodeIdNotCoveredByAnyFeature(
			bool completelyTested,
			bool hasIsCoveredConditions)
		{
			if (completelyTested)
			{
				return hasIsCoveredConditions
					       ? Code.NotCoveredByAnyFeature_WithFulfilledConstraint
					       : Code.NotCoveredByAnyFeature;
			}

			return
				hasIsCoveredConditions
					? Code
						.NotCoveredByAnyFeature_PartlyOutsideVerifiedExtent_WithFulfilledConstraint
					: Code.NotCoveredByAnyFeature_PartlyOutsideVerifiedExtent;
		}

		[CanBeNull]
		private string GetAffectedComponent(int coveredClassIndex)
		{
			string result;
			if (! _affectedComponentsByClassIndex.TryGetValue(coveredClassIndex,
			                                                  out result))
			{
				result = CollectAffectedComponent(coveredClassIndex);

				_affectedComponentsByClassIndex.Add(coveredClassIndex, result);
			}

			return result;
		}

		[NotNull]
		private string CollectAffectedComponent(int coveredClassIndex)
		{
			var featureClass = (IFeatureClass) InvolvedTables[coveredClassIndex];

			string shapeField = featureClass.ShapeFieldName;

			IsCoveringCondition isCoveringCondition =
				_isCoveringConditions != null && _isCoveringConditions.Count == 1
					? _isCoveringConditions[0]
					: null;

			if (isCoveringCondition == null || isCoveringCondition.Condition == null)
			{
				return shapeField;
			}

			string coveredAlias = isCoveringCondition.Row2Alias;

			List<string> fieldNames =
				ExpressionUtils.GetExpressionFieldNames(isCoveringCondition.Condition,
				                                        InvolvedTables[coveredClassIndex],
				                                        coveredAlias).ToList();
			fieldNames.Sort();

			fieldNames.Insert(0, shapeField);

			return StringUtils.Concatenate(fieldNames, ", ");
		}

		private static double GetUncoveredPercentage([CanBeNull] IGeometry shape,
		                                             [NotNull] IGeometry
			                                             uncoveredGeometry)
		{
			const double fullyUncovered = 100;

			if (shape == null)
			{
				return fullyUncovered;
			}

			var area = shape as IArea;
			if (area != null)
			{
				var uncoveredArea = uncoveredGeometry as IArea;
				if (uncoveredArea == null)
				{
					return fullyUncovered;
				}

				return 100 * (uncoveredArea.Area / area.Area);
			}

			var polyline = shape as IPolyline;
			if (polyline != null)
			{
				var uncoveredPolyline = uncoveredGeometry as IPolyline;
				if (uncoveredPolyline == null)
				{
					return fullyUncovered;
				}

				return 100 * (uncoveredPolyline.Length / polyline.Length);
			}

			return 100 *
			       (GeometryUtils.GetPointCount(uncoveredGeometry) /
			        GeometryUtils.GetPointCount(shape));
		}

		private static bool IsSignificantPolygon([NotNull] IPolygon polygon)
		{
			if (GeometryUtils.GetPointCount(polygon) > 3)
			{
				// there could still be multiple rings with insufficient point counts
				return true;
			}

			double minimumArea = GetMinimumArea(polygon);
			double area = ((IArea) polygon).Area;

			return area > minimumArea;
		}

		private static double GetMinimumArea([NotNull] IPolygon polygon)
		{
			var tolerance = polygon.SpatialReference as ISpatialReferenceTolerance;
			if (tolerance == null)
			{
				return 0;
			}

			double xyTolerance = tolerance.XYTolerance;
			return xyTolerance * (polygon.Length / 2);
		}

		/// <summary>
		/// create a filter that gets the lines crossing the current row,
		/// with the same attribute constraints as the table
		/// </summary>
		private void InitFilter()
		{
			IList<ISpatialFilter> filters;
			IList<QueryFilterHelper> filterHelpers;

			_queryFilter = new ISpatialFilter[_totalClassesCount];
			_helper = new QueryFilterHelper[_totalClassesCount];

			// Create copy of this filter and use it for quering features
			CopyFilters(out filters, out filterHelpers);
			for (var i = 0; i < _totalClassesCount; i++)
			{
				_queryFilter[i] = filters[i];
				_queryFilter[i].SpatialRel =
					esriSpatialRelEnum.esriSpatialRelIntersects;

				_helper[i] = filterHelpers[i];
			}
		}

		#region Nested types

		private class CoveringFeatureSearcher
		{
			private readonly ISpatialFilter[] _queryFilters;
			private readonly QueryFilterHelper[] _queryFilterHelpers;

			private readonly
				Func<ITable, IQueryFilter, QueryFilterHelper, IGeometry, IEnumerable<IRow>>
				_searchFunction;

			private readonly Func<int, double> _getTolerance;

			/// <summary>
			/// The static envelope template for getting the envelope of a shape
			/// </summary>
			/// <remarks>Always access via property</remarks>
			[ThreadStatic] private static IEnvelope _shapeEnvelopeTemplate;

			/// <summary>
			/// The static envelope template for getting the expanded envelope
			/// </summary>
			/// <remarks>Always access via property</remarks>
			[ThreadStatic] private static IEnvelope _expandedEnvelopeTemplate;

			private readonly double _tileEnvelopeXMin;
			private readonly double _tileEnvelopeYMin;
			private readonly bool _useSecondaryFilterForTolerance;

			public CoveringFeatureSearcher(
				[NotNull] ISpatialFilter[] queryFilters,
				[NotNull] QueryFilterHelper[] queryFilterHelpers,
				[NotNull]
				Func<ITable, IQueryFilter, QueryFilterHelper, IGeometry, IEnumerable<IRow>>
					searchFunction,
				[NotNull] Func<int, double> getTolerance,
				double tileEnvelopeXMin,
				double tileEnvelopeYMin,
				bool useSecondaryFilterForTolerance)
			{
				_queryFilters = queryFilters;
				_queryFilterHelpers = queryFilterHelpers;
				_searchFunction = searchFunction;
				_getTolerance = getTolerance;
				_tileEnvelopeXMin = tileEnvelopeXMin;
				_tileEnvelopeYMin = tileEnvelopeYMin;
				_useSecondaryFilterForTolerance = useSecondaryFilterForTolerance;
			}

			[NotNull]
			public IEnumerable<IFeature> Search([NotNull] IGeometry uncoveredGeometry,
			                                    [NotNull] IFeatureClass coveringFeatureClass,
			                                    int coveringClassIndex)
			{
				double tolerance = _getTolerance(coveringClassIndex);

				if (Math.Abs(tolerance) < double.Epsilon)
				{
					return SearchNoTolerance(uncoveredGeometry, coveringFeatureClass,
					                         coveringClassIndex);
				}

				return SearchToleranceWithEnvelope(uncoveredGeometry,
				                                   coveringFeatureClass,
				                                   coveringClassIndex,
				                                   tolerance);
			}

			[NotNull]
			private static IEnvelope ShapeEnvelopeTemplate
				=> _shapeEnvelopeTemplate ?? (_shapeEnvelopeTemplate = new EnvelopeClass());

			[NotNull]
			private static IEnvelope ExpandedEnvelopeTemplate => _expandedEnvelopeTemplate ??
			                                                     (_expandedEnvelopeTemplate =
				                                                      new EnvelopeClass());

			[NotNull]
			private IEnumerable<IFeature> SearchToleranceWithEnvelope(
				[NotNull] IGeometry uncoveredGeometry,
				[NotNull] IFeatureClass coveringFeatureClass,
				int coveringClassIndex,
				double tolerance)
			{
				IEnvelope expandedEnvelope = GetExpandedEnvelope(uncoveredGeometry, tolerance);

				_queryFilters[coveringClassIndex].Geometry = expandedEnvelope;

				QueryFilterHelper filterHelper = _queryFilterHelpers[coveringClassIndex];

				filterHelper.ForNetwork = RepeatCoveringFeaturesPerTile(uncoveredGeometry,
				                                                        expandedEnvelope);

				SecondaryFilter secondaryFilter = _useSecondaryFilterForTolerance
					                                  ? new SecondaryFilter(uncoveredGeometry,
					                                                        tolerance)
					                                  : null;

				foreach (IRow row in
					_searchFunction((ITable) coveringFeatureClass,
					                _queryFilters[coveringClassIndex],
					                filterHelper, null))
				{
					var feature = (IFeature) row;

					if (secondaryFilter != null && secondaryFilter.Exclude(feature))
					{
						continue;
					}

					yield return feature;
				}
			}

			[NotNull]
			private static IEnvelope GetExpandedEnvelope([NotNull] IGeometry geometry,
			                                             double distance)
			{
				IEnvelope result = ExpandedEnvelopeTemplate;

				geometry.QueryEnvelope(result);

				const bool asRatio = false;
				result.Expand(distance, distance, asRatio);

				return result;
			}

			[NotNull]
			private IEnumerable<IFeature> SearchNoTolerance(
				[NotNull] IGeometry uncoveredGeometry,
				[NotNull] IFeatureClass coveringFeatureClass,
				int coveringClassIndex)
			{
				_queryFilters[coveringClassIndex].Geometry = uncoveredGeometry;

				QueryFilterHelper filterHelper = _queryFilterHelpers[coveringClassIndex];
				filterHelper.ForNetwork = false;

				foreach (IRow row in
					_searchFunction((ITable) coveringFeatureClass,
					                _queryFilters[coveringClassIndex],
					                filterHelper, null))
				{
					yield return (IFeature) row;
				}
			}

			private bool RepeatCoveringFeaturesPerTile([NotNull] IGeometry shape,
			                                           [NotNull] IGeometry searchGeometry)
			{
				if (shape == searchGeometry)
				{
					// the shape is used directly as search geometry
					return false;
				}

				// return true if the shape does not exceed the XMin/YMin boundary of the tile, 
				// however the search geometry does exceed that boundary.
				return ! ExceedsTileMinCoords(shape) && ExceedsTileMinCoords(searchGeometry);
			}

			private bool ExceedsTileMinCoords([NotNull] IGeometry geometry)
			{
				if (double.IsNaN(_tileEnvelopeXMin) || double.IsNaN(_tileEnvelopeYMin))
				{
					return false;
				}

				double xMin;
				double yMin;
				GetEnvelopeMinCoords(geometry, out xMin, out yMin);

				return xMin < _tileEnvelopeXMin || yMin < _tileEnvelopeYMin;
			}

			private static void GetEnvelopeMinCoords([NotNull] IGeometry shape,
			                                         out double xMin,
			                                         out double yMin)
			{
				var envelope = shape as IEnvelope;

				if (envelope == null)
				{
					shape.QueryEnvelope(ShapeEnvelopeTemplate);
					envelope = ShapeEnvelopeTemplate;
				}

				envelope.QueryCoords(out xMin, out yMin, out double _, out double _);
			}

			private class SecondaryFilter
			{
				private readonly double _tolerance;

				/// <summary>
				/// The static envelope template for getting the envelope of a shape
				/// </summary>
				/// <remarks>Always access via property</remarks>
				[ThreadStatic] private static IEnvelope _envelopeTemplate;

				private readonly IProximityOperator _uncoveredProximityOperator;
				private readonly IRelationalOperator _uncoveredRelationalOperator;

				public SecondaryFilter([NotNull] IGeometry uncoveredGeometry, double tolerance)
				{
					_tolerance = tolerance;
					_uncoveredRelationalOperator = (IRelationalOperator) uncoveredGeometry;

					uncoveredGeometry.QueryEnvelope(EnvelopeTemplate);
					_uncoveredProximityOperator = EnvelopeTemplate as IProximityOperator;
				}

				public bool Exclude([NotNull] IFeature coveringFeature)
				{
					IGeometry coveringGeometry = coveringFeature.Shape;

					if (! _uncoveredRelationalOperator.Disjoint(coveringGeometry))
					{
						return false;
					}

					return _uncoveredProximityOperator.ReturnDistance(coveringGeometry) >
					       _tolerance;
				}

				[NotNull]
				private static IEnvelope EnvelopeTemplate
					=> _envelopeTemplate ?? (_envelopeTemplate = new EnvelopeClass());
			}
		}

		/// <summary>
		/// Represents a feature that has a remaining geometry that is not covered by the covering feature classes
		/// (in tiles processed so far)
		/// </summary>
		private class UnCoveredFeature
		{
			private IGeometry _geometry;

			/// <summary>
			/// Initializes a new instance of the <see cref="UnCoveredFeature"/> class.
			/// </summary>
			/// <param name="feature">The feature.</param>
			/// <param name="tableIndex">The table index for the feature</param>
			public UnCoveredFeature([NotNull] IFeature feature, int tableIndex)
			{
				Assert.ArgumentNotNull(feature, nameof(feature));

				Feature = feature;
				TableIndex = tableIndex;
				OID = feature.OID;
			}

			public int TableIndex { get; }

			public int OID { get; }

			// TODO don't keep the feature around, get it back when needed (only during error reporting)
			[NotNull]
			public IFeature Feature { get; }

			[NotNull]
			public IGeometry Geometry => Assert.NotNull(_geometry, "geometry not defined");

			public int IntersectionCount { get; private set; }

			public void Update([NotNull] IGeometry geometry, int newIntersectionCount)
			{
				Assert.ArgumentNotNull(geometry, nameof(geometry));

				_geometry = geometry;
				IntersectionCount += newIntersectionCount;
			}

			public bool IsFullyChecked([NotNull] IEnvelope tileEnvelope,
			                           [CanBeNull] IEnvelope testRunEnvelope)
			{
				return TestUtils.IsFeatureFullyChecked(Feature.Extent, tileEnvelope,
				                                       testRunEnvelope);
			}

			public void Clip([NotNull] IEnvelope clipperEnvelope)
			{
				if (_geometry == null || Feature.Shape == _geometry)
				{
					_geometry = Feature.ShapeCopy;
				}

				((ITopologicalOperator) _geometry).Clip(clipperEnvelope);
			}
		}

		private class IsCoveringCondition : RowPairCondition
		{
			private const bool _isDirected = true;
			private const bool _undefinedConstraintIsFulfilled = true;

			public IsCoveringCondition([CanBeNull] string constraint, bool caseSensitive)
				: base(constraint, _isDirected, _undefinedConstraintIsFulfilled, caseSensitive) { }
		}

		private class CoveringGeometryCache
		{
			private readonly Dictionary<int, Entry> _entries =
				new Dictionary<int, Entry>();

			private readonly int _maximumPointCount;
			private int _pointCount;
			private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

			public CoveringGeometryCache(int maximumPointCount)
			{
				_maximumPointCount = maximumPointCount;
			}

			public bool TryGet(int objectId, [CanBeNull] out IGeometry geometry)
			{
				Entry entry;
				bool found = _entries.TryGetValue(objectId, out entry);

				geometry = ! found
					           ? null
					           : entry.Geometry;
				return found;
			}

			public void Put(int objectId, IGeometry geometry)
			{
				if (_entries.ContainsKey(objectId))
				{
					return;
				}

				var entry = new Entry(geometry, _envelopeTemplate);

				_entries.Add(objectId, entry);

				_pointCount = _pointCount + entry.PointCount;

				if (_pointCount > _maximumPointCount)
				{
					_msg.VerboseDebugFormat("Maximum point count exceeded: {0} > {1}",
					                        _pointCount, _maximumPointCount);

					foreach (int oid in GetSmallestEntries(_pointCount - _maximumPointCount))
					{
						Entry smallestEntry;
						if (_entries.TryGetValue(oid, out smallestEntry))
						{
							_pointCount -= smallestEntry.PointCount;
							_entries.Remove(oid);
						}
					}
				}
			}

			[NotNull]
			private IEnumerable<int> GetSmallestEntries(int equalToOrExceedingPointCount)
			{
				var result = new List<int>();

				IEnumerable<KeyValuePair<int, Entry>> sorted = GetEntriesSortedOnPointCount();

				var pointCount = 0;
				foreach (KeyValuePair<int, Entry> pair in sorted)
				{
					int oid = pair.Key;
					Entry entry = pair.Value;

					pointCount = pointCount + entry.PointCount;

					result.Add(oid);

					if (pointCount > equalToOrExceedingPointCount)
					{
						break;
					}
				}

				return result;
			}

			private IEnumerable<KeyValuePair<int, Entry>> GetEntriesSortedOnPointCount()
			{
				var result = new List<KeyValuePair<int, Entry>>(_entries);

				result.Sort((p1, p2) => p1.Value.PointCount.CompareTo(p2.Value.PointCount));

				return result;
			}

			public void Evict([NotNull] Func<Entry, bool> isEntryEvictable)
			{
				List<KeyValuePair<int, Entry>> deletable =
					GetEntriesToDelete(isEntryEvictable).ToList();

				foreach (KeyValuePair<int, Entry> pair in deletable)
				{
					int oid = pair.Key;
					Entry entry = pair.Value;

					_entries.Remove(oid);

					_msg.VerboseDebugFormat("Removing covering feature {0} from cache", oid);

					_pointCount -= entry.PointCount;
				}

				Assert.True(_pointCount >= 0,
				            "Unexpected point count: {0}", _pointCount);
			}

			[NotNull]
			private IEnumerable<KeyValuePair<int, Entry>> GetEntriesToDelete(
				[NotNull] Func<Entry, bool> isEntryEvictable)
			{
				foreach (KeyValuePair<int, Entry> pair in _entries)
				{
					Entry entry = pair.Value;

					if (! entry.HasGeometry || isEntryEvictable(entry))
					{
						yield return new KeyValuePair<int, Entry>(pair.Key, entry);
					}
				}
			}

			public class Entry
			{
				private readonly double _xMax;
				private readonly double _yMax;

				public Entry([CanBeNull] IGeometry geometry,
				             [NotNull] IEnvelope envelopeTemplate)
				{
					Geometry = geometry;

					PointCount = GeometryUtils.GetPointCount(geometry);

					if (geometry == null || geometry.IsEmpty)
					{
						_xMax = double.NaN;
						_yMax = double.NaN;
					}
					else
					{
						geometry.QueryEnvelope(envelopeTemplate);

						envelopeTemplate.QueryCoords(out _, out _, out _xMax, out _yMax);
					}
				}

				public bool HasGeometry => ! (double.IsNaN(_xMax) || double.IsNaN(_yMax));

				public int PointCount { get; }

				[CanBeNull]
				public IGeometry Geometry { get; }

				public double XMax => _xMax;

				public double YMax => _yMax;
			}
		}

		#endregion
	}
}
