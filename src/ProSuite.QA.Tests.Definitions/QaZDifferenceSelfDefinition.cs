using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;
using System.Collections.Generic;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[ZValuesTest]
	[IntersectionParameterTest]
	[TopologyTest]
	public class QaZDifferenceSelfDefinition : AlgorithmDefinition
	{
		public double MinimumZDifference { get; }
		public double MaximumZDifference { get; }
		public ZComparisonMethod ZComparisonMethod { get; }
		public string ZRelationConstraint { get; }
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }

		[Doc(nameof(DocStrings.QaZDifferenceSelf_0))]
		public QaZDifferenceSelfDefinition(
			[Doc(nameof(DocStrings.QaZDifferenceSelf_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(new[] { featureClass }, limit, zComparisonMethod,
				   zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceSelf_1))]
		public QaZDifferenceSelfDefinition(
			[Doc(nameof(DocStrings.QaZDifferenceSelf_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: this(featureClasses, limit, 0, zComparisonMethod,
			       zRelationConstraint) { }

		[Doc(nameof(DocStrings.QaZDifferenceSelf_2))]
		public QaZDifferenceSelfDefinition(
			[Doc(nameof(DocStrings.QaZDifferenceSelf_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
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
		public QaZDifferenceSelfDefinition(
			[Doc(nameof(DocStrings.QaZDifferenceSelf_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_minimumZDifference))]
			double minimumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_maximumZDifference))]
			double maximumZDifference,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zComparisonMethod))]
			ZComparisonMethod zComparisonMethod,
			[Doc(nameof(DocStrings.QaZDifferenceSelf_zRelationConstraint))] [CanBeNull]
			string zRelationConstraint)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			MinimumZDifference = minimumZDifference;
			MaximumZDifference = maximumZDifference;
			ZComparisonMethod = zComparisonMethod;
			ZRelationConstraint = zRelationConstraint;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_MinimumZDifferenceExpression))]
		public string MinimumZDifferenceExpression { get; set; }

		[TestParameter]
		[Doc(nameof(DocStrings.QaZDifferenceOther_MaximumZDifferenceExpression))]
		public string MaximumZDifferenceExpression{ get; set; }
	}
}
