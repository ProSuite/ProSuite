using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.SpatialRelations;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[ZValuesTest]
	[IntersectionParameterTest]
	[TopologyTest]
	public class QaZDifferenceOther : QaSpatialRelationOtherBase
	{
		private readonly ZComparisonMethod _zComparisonMethod;
		[CanBeNull] private readonly string _zRelationConstraint;
		private readonly double _minimumZDifference;
		private readonly double _maximumZDifference;
		[CanBeNull] private string _minimumZDifferenceExpressionSql;
		[CanBeNull] private string _maximumZDifferenceExpressionSql;

		[CanBeNull] private ZDifferenceStrategy _zDifferenceStrategy;

		[CanBeNull] private string _relevantRelationConditionSql;
		[CanBeNull] private RelevantRowPairCondition _relevantRelationCondition;

		#region issue codes

		private static ITestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes
			=> _codes ?? (_codes = new AggregatedTestIssueCodes(
				              ZDifferenceStrategyBoundingBox.Codes));

		#endregion

		[Doc(nameof(DocStrings.QaZDifferenceOther_0))]
		public QaZDifferenceOther(
			[Doc(nameof(DocStrings.QaZDifferenceOther_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaZDifferenceOther_relatedClass))] [NotNull]
			IFeatureClass relatedClass,
			[Doc(nameof(DocStrings.QaZDifferenceOther_limit))] double limit,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(new[] {featureClass}, new[] {relatedClass},
			       limit, zComparisonMethod, zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceOther_1))]
		public QaZDifferenceOther(
			[Doc(nameof(DocStrings.QaZDifferenceOther_featureClasses))] [NotNull]
			IList<IFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaZDifferenceOther_relatedClasses))] [NotNull]
			IList<IFeatureClass> relatedClasses,
			[Doc(nameof(DocStrings.QaZDifferenceOther_limit))] double limit,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(featureClasses, relatedClasses, limit, 0, zComparisonMethod,
			       zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceOther_2))]
		public QaZDifferenceOther(
			[Doc(nameof(DocStrings.QaZDifferenceOther_featureClass))] [NotNull]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaZDifferenceOther_relatedClass))] [NotNull]
			IFeatureClass relatedClass,
			[Doc(nameof(DocStrings.QaZDifferenceOther_minimumZDifference))]
			double minimumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceOther_maximumZDifference))]
			double maximumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(new[] {featureClass}, new[] {relatedClass},
			       minimumZDifference, maximumZDifference,
			       zComparisonMethod, zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceOther_3))]
		public QaZDifferenceOther(
			[Doc(nameof(DocStrings.QaZDifferenceOther_featureClasses))] [NotNull]
			IList<IFeatureClass> featureClasses,
			[Doc(nameof(DocStrings.QaZDifferenceOther_relatedClasses))] [NotNull]
			IList<IFeatureClass> relatedClasses,
			[Doc(nameof(DocStrings.QaZDifferenceOther_minimumZDifference))]
			double minimumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceOther_maximumZDifference))]
			double maximumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: base(featureClasses, relatedClasses,
			       esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			Assert.ArgumentNotNaN(minimumZDifference, nameof(minimumZDifference));
			Assert.ArgumentNotNaN(maximumZDifference, nameof(maximumZDifference));

			_minimumZDifference = minimumZDifference;
			_maximumZDifference = maximumZDifference;
			_zComparisonMethod = zComparisonMethod;
			_zRelationConstraint = zRelationConstraint;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_RelevantRelationCondition))]
		public string RelevantRelationCondition
		{
			get { return _relevantRelationConditionSql; }
			set
			{
				_relevantRelationConditionSql = value;
				_relevantRelationCondition = null;
			}
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

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_UseDistanceFromReferenceRingPlane))]
		public bool UseDistanceFromReferenceRingPlane { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_ReferenceRingPlaneCoplanarityTolerance))]
		public double ReferenceRingPlaneCoplanarityTolerance { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_IgnoreNonCoplanarReferenceRings))]
		public bool IgnoreNonCoplanarReferenceRings { get; set; }

		protected override int FindErrors(IRow row1, int tableIndex1,
		                                  IRow row2, int tableIndex2)
		{
			if (_relevantRelationCondition == null)
			{
				_relevantRelationCondition = new RelevantRowPairCondition(
					_relevantRelationConditionSql,
					GetSqlCaseSensitivity());
			}

			if (! _relevantRelationCondition.IsFulfilled(row1, tableIndex1,
			                                             row2, tableIndex2))
			{
				return NoError;
			}

			if (_zDifferenceStrategy == null)
			{
				_zDifferenceStrategy = ZDifferenceStrategyFactory.CreateStrategy(
					_zComparisonMethod,
					_minimumZDifference, _minimumZDifferenceExpressionSql,
					_maximumZDifference, _maximumZDifferenceExpressionSql,
					_zRelationConstraint,
					GetSqlCaseSensitivity(),
					FormatComparison, this,
					index => UseDistanceFromReferenceRingPlane &&
					         IsInFromTableList(index),
					ReferenceRingPlaneCoplanarityTolerance,
					IgnoreNonCoplanarReferenceRings);
			}

			return _zDifferenceStrategy.ReportErrors(row1, tableIndex1,
			                                         row2, tableIndex2);
		}

		private class RelevantRowPairCondition : RowPairCondition
		{
			private const bool _isDirected = true;
			private const bool _undefinedConstraintIsFulfilled = true;
			private const string _row1Alias = "G1";
			private const string _row2Alias = "G2";

			public RelevantRowPairCondition([CanBeNull] string constraint,
			                                bool caseSensitive)
				: base(constraint, _isDirected, _undefinedConstraintIsFulfilled,
				       _row1Alias, _row2Alias, caseSensitive) { }
		}
	}
}
