using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.SpatialRelations;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
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

		[Doc("QaZDifferenceSelf_0")]
		public QaZDifferenceSelf(
			[Doc("QaZDifferenceSelf_featureClass")] [NotNull]
			IFeatureClass featureClass,
			[Doc("QaZDifferenceSelf_limit")] double limit,
			[Doc("QaZDifferenceSelf_zComparisonMethod")]
			ZComparisonMethod zComparisonMethod,
			[Doc("QaZDifferenceSelf_zRelationConstraint")] [CanBeNull]
			string zRelationConstraint)
			: this(new[] {featureClass}, limit, zComparisonMethod,
			       zRelationConstraint) { }

		[Doc("QaZDifferenceSelf_1")]
		public QaZDifferenceSelf(
			[Doc("QaZDifferenceSelf_featureClasses")] [NotNull]
			IList<IFeatureClass> featureClasses,
			[Doc("QaZDifferenceSelf_limit")] double limit,
			[Doc("QaZDifferenceSelf_zComparisonMethod")]
			ZComparisonMethod zComparisonMethod,
			[Doc("QaZDifferenceSelf_zRelationConstraint")] [CanBeNull]
			string zRelationConstraint)
			: this(featureClasses, limit, 0, zComparisonMethod, zRelationConstraint) { }

		[Doc("QaZDifferenceSelf_2")]
		public QaZDifferenceSelf(
			[Doc("QaZDifferenceSelf_featureClass")] [NotNull]
			IFeatureClass featureClass,
			[Doc("QaZDifferenceSelf_minimumZDifference")]
			double minimumZDifference,
			[Doc("QaZDifferenceSelf_maximumZDifference")]
			double maximumZDifference,
			[Doc("QaZDifferenceSelf_zComparisonMethod")]
			ZComparisonMethod zComparisonMethod,
			[Doc("QaZDifferenceSelf_zRelationConstraint")] [CanBeNull]
			string zRelationConstraint)
			: this(new[] {featureClass}, minimumZDifference, maximumZDifference,
			       zComparisonMethod, zRelationConstraint) { }

		[Doc("QaZDifferenceSelf_3")]
		public QaZDifferenceSelf(
			[Doc("QaZDifferenceSelf_featureClasses")] [NotNull]
			IList<IFeatureClass> featureClasses,
			[Doc("QaZDifferenceSelf_minimumZDifference")]
			double minimumZDifference,
			[Doc("QaZDifferenceSelf_maximumZDifference")]
			double maximumZDifference,
			[Doc("QaZDifferenceSelf_zComparisonMethod")]
			ZComparisonMethod zComparisonMethod,
			[Doc("QaZDifferenceSelf_zRelationConstraint")] [CanBeNull]
			string zRelationConstraint)
			: base(featureClasses, esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			_minimumZDifference = minimumZDifference;
			_maximumZDifference = maximumZDifference;
			_zComparisonMethod = zComparisonMethod;
			_zRelationConstraint = zRelationConstraint;
		}

		[TestParameter]
		[Doc("QaZDifferenceOther_MinimumZDifferenceExpression")]
		public string MinimumZDifferenceExpression
		{
			get { return _minimumZDifferenceExpressionSql; }
			set { _minimumZDifferenceExpressionSql = value?.Trim(); }
		}

		[TestParameter]
		[Doc("QaZDifferenceOther_MaximumZDifferenceExpression")]
		public string MaximumZDifferenceExpression
		{
			get { return _maximumZDifferenceExpressionSql; }
			set { _maximumZDifferenceExpressionSql = value?.Trim(); }
		}

		protected override int FindErrors(IRow row1, int tableIndex1,
		                                  IRow row2, int tableIndex2)
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
