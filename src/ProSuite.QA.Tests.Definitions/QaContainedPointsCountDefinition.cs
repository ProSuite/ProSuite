using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaContainedPointsCountDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> PolygonClasses { get; }
		public IList<IFeatureClassSchemaDef> PointClasses { get; }
		public int MinimumPointCount { get; }
		public int MaximumPointCount { get; }
		public string RelevantPointCondition { get; }
		public bool CountPointOnPolygonBorder { get; }

		private readonly int _minimumPointCount;
		private readonly int _maximumPointCount;
		private readonly bool _countPointOnPolygonBorder;
		[CanBeNull] private readonly string _relevantPointConditionSql;
		private readonly int _polygonClassesCount;
		private readonly int _totalClassesCount;
		//private readonly object _polylineUsage;

		private const PolylineUsage _defaultPolylineUsage = PolylineUsage.AsIs;
		private PolylineUsage _polylineUsage;

		//private RelevantPointCondition _relevantPointCondition;
		//private QueryFilterHelper[] _helper;
		//private IFeatureClassFilter[] _queryFilter;

		// TODO store xmax/ymax also, to be able to discard polygons that are guaranteed to
		// not be reported anymore? Or filter out polygons from earlier tiles in CompleteTile()?
		[NotNull] private readonly Dictionary<int, HashSet<long>> _fullyCheckedPolygonsByTableIndex
			= new Dictionary<int, HashSet<long>>();
		//private object _defaultPolylineUsage;

		// TODO update doc strings (polygons AND polylines)

		[Doc(nameof(DocStrings.QaContainedPointsCount_0))]
		public QaContainedPointsCountDefinition(
			[Doc(nameof(DocStrings.QaContainedPointsCount_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_pointClass))] [NotNull]
			IFeatureClassSchemaDef pointClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_expectedPointCount))]
			int expectedPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_relevantPointCondition))] [CanBeNull]
			string
				relevantPointCondition)
			: this(new[] { polygonClass },
			       new[] { pointClass },
			       expectedPointCount,
			       expectedPointCount,
			       relevantPointCondition,
			       false) { }

		[Doc(nameof(DocStrings.QaContainedPointsCount_1))]
		public QaContainedPointsCountDefinition(
			[Doc(nameof(DocStrings.QaContainedPointsCount_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_pointClass))] [NotNull]
			IFeatureClassSchemaDef pointClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_minimumPointCount))]
			int minimumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_maximumPointCount))]
			int maximumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_relevantPointCondition))] [CanBeNull]
			string
				relevantPointCondition)
			: this(new[] { polygonClass },
			       new[] { pointClass },
			       minimumPointCount,
			       maximumPointCount,
			       relevantPointCondition,
			       false) { }

		[Doc(nameof(DocStrings.QaContainedPointsCount_2))]
		public QaContainedPointsCountDefinition(
			[Doc(nameof(DocStrings.QaContainedPointsCount_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_pointClass))] [NotNull]
			IFeatureClassSchemaDef pointClass,
			[Doc(nameof(DocStrings.QaContainedPointsCount_minimumPointCount))]
			int minimumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_maximumPointCount))]
			int maximumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_relevantPointCondition))] [CanBeNull]
			string
				relevantPointCondition,
			[Doc(nameof(DocStrings.QaContainedPointsCount_countPointOnPolygonBorder))]
			bool
				countPointOnPolygonBorder)
			: this(new[] { polygonClass },
			       new[] { pointClass },
			       minimumPointCount,
			       maximumPointCount,
			       relevantPointCondition,
			       countPointOnPolygonBorder) { }

		[Doc(nameof(DocStrings.QaContainedPointsCount_3))]
		public QaContainedPointsCountDefinition(
			[Doc(nameof(DocStrings.QaContainedPointsCount_polygonClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> polygonClasses,
			[Doc(nameof(DocStrings.QaContainedPointsCount_pointClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> pointClasses,
			[Doc(nameof(DocStrings.QaContainedPointsCount_expectedPointCount))]
			int expectedPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_relevantPointCondition))] [CanBeNull]
			string
				relevantPointCondition)
			: this(polygonClasses, pointClasses,
			       expectedPointCount, expectedPointCount,
			       relevantPointCondition, false) { }

		[Doc(nameof(DocStrings.QaContainedPointsCount_4))]
		public QaContainedPointsCountDefinition(
			[Doc(nameof(DocStrings.QaContainedPointsCount_polygonClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> polygonClasses,
			[Doc(nameof(DocStrings.QaContainedPointsCount_pointClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> pointClasses,
			[Doc(nameof(DocStrings.QaContainedPointsCount_minimumPointCount))]
			int minimumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_maximumPointCount))]
			int maximumPointCount,
			[Doc(nameof(DocStrings.QaContainedPointsCount_relevantPointCondition))] [CanBeNull]
			string relevantPointCondition,
			[Doc(nameof(DocStrings.QaContainedPointsCount_countPointOnPolygonBorder))]
			bool countPointOnPolygonBorder) :
			base(CastToTables(
				     // ReSharper disable once PossiblyMistakenUseOfParamsMethod
				     Union(polygonClasses, pointClasses)))
		{
			Assert.ArgumentNotNull(polygonClasses, nameof(polygonClasses));
			Assert.ArgumentNotNull(pointClasses, nameof(pointClasses));
			Assert.ArgumentCondition(polygonClasses.Count > 0, "No polygon class specified");
			Assert.ArgumentCondition(pointClasses.Count > 0, "No point class specified");

			PolygonClasses = polygonClasses;
			PointClasses = pointClasses;
			MinimumPointCount = minimumPointCount;
			MaximumPointCount = maximumPointCount;
			RelevantPointCondition = relevantPointCondition;
			CountPointOnPolygonBorder = countPointOnPolygonBorder;

			_minimumPointCount = minimumPointCount;
			_maximumPointCount = maximumPointCount;
			_countPointOnPolygonBorder = countPointOnPolygonBorder;
			_relevantPointConditionSql = relevantPointCondition;

			_polygonClassesCount = polygonClasses.Count;
			_totalClassesCount = polygonClasses.Count + pointClasses.Count;

			//_polylineUsage = _defaultPolylineUsage;
		}

		[TestParameter(_defaultPolylineUsage)]
		[Doc(nameof(DocStrings.QaContainedPointsCount_PolylineUsage))]
		public PolylineUsage PolylineUsage
		{
			get { return _polylineUsage; }
			set { _polylineUsage = value; }
		}
	}
}
