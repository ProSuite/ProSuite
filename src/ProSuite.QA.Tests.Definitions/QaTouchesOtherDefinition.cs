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
	public class QaTouchesOtherDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> Touching { get; }
		public IList<IFeatureClassSchemaDef> Touched { get; }
		public string ValidRelationConstraint { get; }

		[Doc(nameof(DocStrings.QaTouchesOther_0))]
		public QaTouchesOtherDefinition(
				[Doc(nameof(DocStrings.QaTouchesOther_touchingClasses))]
				IList<IFeatureClassSchemaDef> touching,
				[Doc(nameof(DocStrings.QaTouchesOther_touchedClasses))]
				IList<IFeatureClassSchemaDef> touched)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(touching, touched, null) { }

		[Doc(nameof(DocStrings.QaTouchesOther_1))]
		public QaTouchesOtherDefinition(
				[Doc(nameof(DocStrings.QaTouchesOther_touchingClass))]
				IFeatureClassSchemaDef touching,
				[Doc(nameof(DocStrings.QaTouchesOther_touchedClass))]
				IFeatureClassSchemaDef touched)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(touching, touched, null) { }

		[Doc(nameof(DocStrings.QaTouchesOther_2))]
		public QaTouchesOtherDefinition(
			[Doc(nameof(DocStrings.QaTouchesOther_touchingClasses))]
			IList<IFeatureClassSchemaDef> touching,
			[Doc(nameof(DocStrings.QaTouchesOther_touchedClasses))]
			IList<IFeatureClassSchemaDef> touched,
			[Doc(nameof(DocStrings.QaTouchesOther_validRelationConstraint))]
			string validRelationConstraint)
			: base(touching.Union(touched))

		{
			Touching = touching;
			Touched = touched;
			ValidRelationConstraint = validRelationConstraint;
		}

		[Doc(nameof(DocStrings.QaTouchesOther_3))]
		public QaTouchesOtherDefinition(
			[Doc(nameof(DocStrings.QaTouchesOther_touchingClass))]
			IFeatureClassSchemaDef touching,
			[Doc(nameof(DocStrings.QaTouchesOther_touchedClass))]
			IFeatureClassSchemaDef touched,
			[Doc(nameof(DocStrings.QaTouchesOther_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { touching }, new[] { touched }, validRelationConstraint) { }

		[TestParameter]
		[Doc(nameof(DocStrings.QaTouchesOther_ValidTouchGeometryConstraint))]
		public string ValidTouchGeometryConstraint { get; set; }
	}
}
