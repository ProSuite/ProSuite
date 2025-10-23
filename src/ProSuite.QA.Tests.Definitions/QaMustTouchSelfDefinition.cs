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
	public class QaMustTouchSelfDefinition : AlgorithmDefinition
	{
		public ICollection<IFeatureClassSchemaDef> FeatureClasses { get; }
		public string RelevantRelationCondition { get; }

		[Doc(nameof(DocStrings.QaMustTouchSelf_0))]
		public QaMustTouchSelfDefinition(
			[Doc(nameof(DocStrings.QaMustTouchSelf_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMustTouchSelf_relevantRelationCondition))] [CanBeNull]
			string relevantRelationCondition)
			: this(new[] { featureClass }, relevantRelationCondition)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
		}

		[Doc(nameof(DocStrings.QaMustTouchSelf_1))]
		public QaMustTouchSelfDefinition(
			[Doc(nameof(DocStrings.QaMustTouchSelf_featureClasses))] [NotNull]
			ICollection<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMustTouchSelf_relevantRelationCondition))] [CanBeNull]
			string relevantRelationCondition)
			: base(featureClasses)
		{
			Assert.ArgumentNotNull(featureClasses, nameof(featureClasses));

			FeatureClasses = featureClasses;
			if (relevantRelationCondition != null)
			{
				RelevantRelationCondition = relevantRelationCondition;
			}
		}
	}
}
