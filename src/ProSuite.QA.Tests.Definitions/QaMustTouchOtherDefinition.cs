using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using System.Linq;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaMustTouchOtherDefinition : AlgorithmDefinition
	{
		public string RelevantRelationCondition { get; }
		public ICollection<IFeatureClassSchemaDef> FeatureClasses { get; }
		public ICollection<IFeatureClassSchemaDef> OtherFeatureClasses { get; }

		[Doc(nameof(DocStrings.QaMustTouchOther_0))]
		public QaMustTouchOtherDefinition(
				[Doc(nameof(DocStrings.QaMustTouchOther_featureClass))] [NotNull]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaMustTouchOther_otherFeatureClass))] [NotNull]
				IFeatureClassSchemaDef otherFeatureClass,
				[Doc(nameof(DocStrings.QaMustTouchOther_relevantRelationCondition))] [CanBeNull]
				string relevantRelationCondition)
				: this(new[] { featureClass }, new[] { otherFeatureClass }, relevantRelationCondition) { }

		[Doc(nameof(DocStrings.QaMustTouchOther_1))]
		public QaMustTouchOtherDefinition(
			[Doc(nameof(DocStrings.QaMustTouchOther_featureClass))] [NotNull]
			ICollection<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMustTouchOther_otherFeatureClass))] [NotNull]
			ICollection<IFeatureClassSchemaDef> otherFeatureClasses,
			[Doc(nameof(DocStrings.QaMustTouchOther_relevantRelationCondition))] [CanBeNull]
			string relevantRelationCondition)
			: base(featureClasses.Union(otherFeatureClasses))
		{
			FeatureClasses = featureClasses;
			OtherFeatureClasses = otherFeatureClasses;
			RelevantRelationCondition = relevantRelationCondition;
		}
	}
}
