using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaMustIntersectOtherDefinition : AlgorithmDefinition
	{
		public string RelevantRelationCondition { get; }
		public ICollection<IFeatureClassSchemaDef> FeatureClasses { get; }
		public ICollection<IFeatureClassSchemaDef> OtherFeatureClasses { get; }

		[Doc(nameof(DocStrings.QaMustIntersectOther_0))]
		public QaMustIntersectOtherDefinition(
			[Doc(nameof(DocStrings.QaMustIntersectOther_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMustIntersectOther_otherFeatureClass))] [NotNull]
			IFeatureClassSchemaDef otherFeatureClass,
			[Doc(nameof(DocStrings.QaMustIntersectOther_relevantRelationCondition))] [CanBeNull]
			string relevantRelationCondition)
			: this(new[] { featureClass }, new[] { otherFeatureClass },
			       relevantRelationCondition) { }

		[Doc(nameof(DocStrings.QaMustIntersectOther_1))]
		public QaMustIntersectOtherDefinition(
			[Doc(nameof(DocStrings.QaMustIntersectOther_featureClasses))] [NotNull]
			ICollection<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectOther_otherFeatureClasses))] [NotNull]
			ICollection<IFeatureClassSchemaDef> otherFeatureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectOther_relevantRelationCondition))] [CanBeNull]
			string relevantRelationCondition)
			: base(featureClasses.Union(otherFeatureClasses))
		{
			FeatureClasses = featureClasses;
			OtherFeatureClasses = otherFeatureClasses;
			RelevantRelationCondition = relevantRelationCondition;
		}
	}
}
