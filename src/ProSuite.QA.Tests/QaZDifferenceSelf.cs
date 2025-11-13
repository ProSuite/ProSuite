using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.SpatialRelations;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ZValuesTest]
	[IntersectionParameterTest]
	[TopologyTest]
	public class QaZDifferenceSelf : QaSpatialRelationSelfBase
	{
		private readonly ZComparisonMethod _zComparisonMethod;
		private readonly string _zRelationConstraint;
		private readonly double _minimumZDifference;
		private readonly double _maximumZDifference;
		[CanBeNull] private string _minimumZDifferenceExpressionSql;
		[CanBeNull] private string _maximumZDifferenceExpressionSql;
		private ZDifferenceStrategy _zDifferenceStrategy;

		#region issue codes

		private static ITestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes
			=> _codes ?? (_codes = new AggregatedTestIssueCodes(
				              ZDifferenceStrategyBoundingBox.Codes));

		#endregion

		[Doc(nameof(DocStrings.QaZDifferenceSelf_0))]
		public QaZDifferenceSelf(
			[Doc(nameof(DocStrings.QaZDifferenceSelf_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(new[] { featureClass }, limit, zComparisonMethod,
			       zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceSelf_1))]
		public QaZDifferenceSelf(
			[Doc(nameof(DocStrings.QaZDifferenceSelf_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(featureClasses, limit, 0, zComparisonMethod,
			       zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceSelf_2))]
		public QaZDifferenceSelf(
			[Doc(nameof(DocStrings.QaZDifferenceSelf_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_minimumZDifference))]
			double minimumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_maximumZDifference))]
			double maximumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(new[] { featureClass }, minimumZDifference, maximumZDifference,
			       zComparisonMethod, zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceSelf_3))]
		public QaZDifferenceSelf(
			[Doc(nameof(DocStrings.QaZDifferenceSelf_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_minimumZDifference))]
			double minimumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_maximumZDifference))]
			double maximumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: base(featureClasses, esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			_minimumZDifference = minimumZDifference;
			_maximumZDifference = maximumZDifference;
			_zComparisonMethod = zComparisonMethod;
			_zRelationConstraint = zRelationConstraint;
			AddCustomQueryFilterExpression(zRelationConstraint);
		}

		[InternallyUsedTest]
		public QaZDifferenceSelf([NotNull] QaZDifferenceSelfDefinition definition)
			: this(definition.FeatureClasses.Cast<IReadOnlyFeatureClass>().ToList(),
			       definition.MinimumZDifference, definition.MaximumZDifference,
			       definition.ZComparisonMethod, definition.ZRelationConstraint)
		{
			MinimumZDifferenceExpression = definition.MinimumZDifferenceExpression;
			MaximumZDifferenceExpression = definition.MaximumZDifferenceExpression;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_MinimumZDifferenceExpression))]
		public string MinimumZDifferenceExpression
		{
			get { return _minimumZDifferenceExpressionSql; }
			set { _minimumZDifferenceExpressionSql = value?.Trim(); }
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_MaximumZDifferenceExpression))]
		public string MaximumZDifferenceExpression
		{
			get { return _maximumZDifferenceExpressionSql; }
			set { _maximumZDifferenceExpressionSql = value?.Trim(); }
		}

		protected override int FindErrors(IReadOnlyRow row1, int tableIndex1,
		                                  IReadOnlyRow row2, int tableIndex2)
		{
			if (_zDifferenceStrategy == null)
			{
				_zDifferenceStrategy = ZDifferenceStrategyFactory.CreateStrategy(
					_zComparisonMethod,
					_minimumZDifference, _maximumZDifferenceExpressionSql,
					_maximumZDifference, _maximumZDifferenceExpressionSql,
					_zRelationConstraint, GetSqlCaseSensitivity(),
					FormatComparison, this);
			}

			return _zDifferenceStrategy.ReportErrors(row1, tableIndex1,
			                                         row2, tableIndex2);
		}
	}
}
