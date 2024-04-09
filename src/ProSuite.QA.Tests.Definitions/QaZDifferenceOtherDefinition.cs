using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ZValuesTest]
	[IntersectionParameterTest]
	[TopologyTest]
	public class QaZDifferenceOtherDefinition : AlgorithmDefinition
	{
		public double MinimumZDifference { get; }
		public double MaximumZDifference { get; }
		public ZComparisonMethod ZComparisonMethod { get; }
		public string ZRelationConstraint { get; }
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public IList<IFeatureClassSchemaDef> RelatedClasses { get; }

		[Doc(nameof(DocStrings.QaZDifferenceOther_0))]
		public QaZDifferenceOtherDefinition(
			[Doc(nameof(DocStrings.QaZDifferenceOther_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaZDifferenceOther_relatedClass))] [NotNull]
			IFeatureClassSchemaDef relatedClass,
			[Doc(nameof(DocStrings.QaZDifferenceOther_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(new[] { featureClass }, new[] { relatedClass },
			       limit, zComparisonMethod, zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceOther_1))]
		public QaZDifferenceOtherDefinition(
			[Doc(nameof(DocStrings.QaZDifferenceOther_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaZDifferenceOther_relatedClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> relatedClasses,
			[Doc(nameof(DocStrings.QaZDifferenceOther_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(featureClasses, relatedClasses, limit, 0, zComparisonMethod,
			       zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceOther_2))]
		public QaZDifferenceOtherDefinition(
			[Doc(nameof(DocStrings.QaZDifferenceOther_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaZDifferenceOther_relatedClass))] [NotNull]
			IFeatureClassSchemaDef relatedClass,
			[Doc(nameof(DocStrings.QaZDifferenceOther_minimumZDifference))]
			double minimumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceOther_maximumZDifference))]
			double maximumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(new[] { featureClass }, new[] { relatedClass },
			       minimumZDifference, maximumZDifference,
			       zComparisonMethod, zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceOther_3))]
		public QaZDifferenceOtherDefinition(
			[Doc(nameof(DocStrings.QaZDifferenceOther_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaZDifferenceOther_relatedClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> relatedClasses,
			[Doc(nameof(DocStrings.QaZDifferenceOther_minimumZDifference))]
			double minimumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceOther_maximumZDifference))]
			double maximumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceOther_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: base(featureClasses.Union(relatedClasses))
		{
			Assert.ArgumentNotNaN(minimumZDifference, nameof(minimumZDifference));
			Assert.ArgumentNotNaN(maximumZDifference, nameof(maximumZDifference));

			FeatureClasses = featureClasses;
			RelatedClasses = relatedClasses;
			MinimumZDifference = minimumZDifference;
			MaximumZDifference = maximumZDifference;
			ZComparisonMethod = zComparisonMethod;
			ZRelationConstraint = zRelationConstraint;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_RelevantRelationCondition))]
		public string RelevantRelationCondition { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_MinimumZDifferenceExpression))]
		public string MinimumZDifferenceExpression { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_MaximumZDifferenceExpression))]
		public string MaximumZDifferenceExpression { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_UseDistanceFromReferenceRingPlane))]
		public bool UseDistanceFromReferenceRingPlane { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_ReferenceRingPlaneCoplanarityTolerance))]
		public double ReferenceRingPlaneCoplanarityTolerance { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_IgnoreNonCoplanarReferenceRings))]
		public bool IgnoreNonCoplanarReferenceRings { get; set; }
	}
}
