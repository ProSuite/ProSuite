using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaCrossesSelfDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public string ValidRelationConstraint { get; }

		[Doc(nameof(DocStrings.QaCrossesSelf_0))]
		public QaCrossesSelfDefinition(
				[Doc(nameof(DocStrings.QaCrossesSelf_featureClasses))]
				IList<IFeatureClassSchemaDef> featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc(nameof(DocStrings.QaCrossesSelf_1))]
		public QaCrossesSelfDefinition(
				[Doc(nameof(DocStrings.QaCrossesSelf_featureClass))]
				IFeatureClassSchemaDef featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, null) { }

		[Doc(nameof(DocStrings.QaCrossesSelf_2))]
		public QaCrossesSelfDefinition(
			[Doc(nameof(DocStrings.QaCrossesSelf_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaCrossesSelf_validRelationConstraint))]
			string validRelationConstraint)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			ValidRelationConstraint = validRelationConstraint;
		}

		[Doc(nameof(DocStrings.QaCrossesSelf_3))]
		public QaCrossesSelfDefinition(
			[Doc(nameof(DocStrings.QaCrossesSelf_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaCrossesSelf_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { featureClass }, validRelationConstraint) { }
	}
}
