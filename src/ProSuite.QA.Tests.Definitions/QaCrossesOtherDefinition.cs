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
	public class QaCrossesOtherDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> CrossedClasses { get; }
		public IList<IFeatureClassSchemaDef> CrossingClasses { get; }
		public string ValidRelationConstraint { get; set; }

		[Doc(nameof(DocStrings.QaCrossesOther_0))]
		public QaCrossesOtherDefinition(
				[Doc(nameof(DocStrings.QaCrossesOther_crossedClasses))]
				IList<IFeatureClassSchemaDef> crossed,
				[Doc(nameof(DocStrings.QaCrossesOther_crossingClasses))]
				IList<IFeatureClassSchemaDef> crossing)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(crossed, crossing, null) { }

		[Doc(nameof(DocStrings.QaCrossesOther_1))]
		public QaCrossesOtherDefinition(
				[Doc(nameof(DocStrings.QaCrossesOther_crossedClass))]
				IFeatureClassSchemaDef crossed,
				[Doc(nameof(DocStrings.QaCrossesOther_crossingClass))]
				IFeatureClassSchemaDef crossing)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(crossed, crossing, null) { }

		[Doc(nameof(DocStrings.QaCrossesOther_2))]
		public QaCrossesOtherDefinition(
			[Doc(nameof(DocStrings.QaCrossesOther_crossedClasses))]
			IList<IFeatureClassSchemaDef> crossedClasses,
			[Doc(nameof(DocStrings.QaCrossesOther_crossingClasses))]
			IList<IFeatureClassSchemaDef> crossingClasses,
			[Doc(nameof(DocStrings.QaCrossesOther_validRelationConstraint))]
			string validRelationConstraint)
			: base(crossedClasses.Union(crossingClasses))
		{
			CrossedClasses = crossedClasses;
			CrossingClasses = crossingClasses;
			ValidRelationConstraint = validRelationConstraint;
		}

		[Doc(nameof(DocStrings.QaCrossesOther_3))]
		public QaCrossesOtherDefinition(
			[Doc(nameof(DocStrings.QaCrossesOther_crossedClass))]
			IFeatureClassSchemaDef crossedClass,
			[Doc(nameof(DocStrings.QaCrossesOther_crossingClass))]
			IFeatureClassSchemaDef crossingClass,
			[Doc(nameof(DocStrings.QaCrossesOther_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { crossedClass }, new[] { crossingClass }, validRelationConstraint) { }
	}
}
