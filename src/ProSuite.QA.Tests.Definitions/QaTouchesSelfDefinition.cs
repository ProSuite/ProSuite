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
	public class QaTouchesSelfDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public string ValidRelationConstraint { get; }

		[Doc(nameof(DocStrings.QaTouchesSelf_0))]
		public QaTouchesSelfDefinition(
				[Doc(nameof(DocStrings.QaTouchesSelf_featureClasses))]
				IList<IFeatureClassSchemaDef> featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc(nameof(DocStrings.QaTouchesSelf_1))]
		public QaTouchesSelfDefinition(
				[Doc(nameof(DocStrings.QaTouchesSelf_featureClass))]
				IFeatureClassSchemaDef featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, null) { }

		[Doc(nameof(DocStrings.QaTouchesSelf_2))]
		public QaTouchesSelfDefinition(
			[Doc(nameof(DocStrings.QaTouchesSelf_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaTouchesSelf_validRelationConstraint))]
			string validRelationConstraint)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			ValidRelationConstraint = validRelationConstraint;
		}

		[Doc(nameof(DocStrings.QaTouchesSelf_3))]
		public QaTouchesSelfDefinition(
			[Doc(nameof(DocStrings.QaTouchesSelf_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaTouchesSelf_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { featureClass }, validRelationConstraint) { }

		[TestParameter]
		[Doc(nameof(DocStrings.QaTouchesSelf_ValidTouchGeometryConstraint))]
		public string ValidTouchGeometryConstraint { get; set; }
	}
}
