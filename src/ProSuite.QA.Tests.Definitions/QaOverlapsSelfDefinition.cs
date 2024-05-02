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
	public class QaOverlapsSelfDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public string ValidRelationConstraint { get; }

		[Doc(nameof(DocStrings.QaOverlapsSelf_0))]
		public QaOverlapsSelfDefinition(
				[Doc(nameof(DocStrings.QaOverlapsSelf_featureClass))]
				IFeatureClassSchemaDef featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, null) { }

		[Doc(nameof(DocStrings.QaOverlapsSelf_1))]
		public QaOverlapsSelfDefinition(
				[Doc(nameof(DocStrings.QaOverlapsSelf_featureClasses))]
				IList<IFeatureClassSchemaDef> featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc(nameof(DocStrings.QaOverlapsSelf_2))]
		public QaOverlapsSelfDefinition(
			[Doc(nameof(DocStrings.QaOverlapsSelf_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaOverlapsSelf_validRelationConstraint))]
			string validRelationConstraint)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			ValidRelationConstraint = validRelationConstraint;
		}

		[Doc(nameof(DocStrings.QaOverlapsSelf_3))]
		public QaOverlapsSelfDefinition(
			[Doc(nameof(DocStrings.QaOverlapsSelf_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaOverlapsSelf_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { featureClass }, validRelationConstraint) { }
	}
}
