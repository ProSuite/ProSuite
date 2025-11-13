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
	public class QaInteriorIntersectsOtherDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public IList<IFeatureClassSchemaDef> RelatedClasses { get; }
		public string Constraint { get; }

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_0))]
		public QaInteriorIntersectsOtherDefinition(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClass))] [NotNull]
			IFeatureClassSchemaDef relatedClass)
			: this(featureClass, relatedClass, string.Empty) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_1))]
		public QaInteriorIntersectsOtherDefinition(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClass))] [NotNull]
			IFeatureClassSchemaDef relatedClass,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_constraint))] [CanBeNull]
			string constraint)
			: this(new[] { featureClass }, new[] { relatedClass }, constraint) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_2))]
		public QaInteriorIntersectsOtherDefinition(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> relatedClasses)
			: this(featureClasses, relatedClasses, string.Empty) { }

		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_3))]
		public QaInteriorIntersectsOtherDefinition(
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_relatedClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> relatedClasses,
			[Doc(nameof(DocStrings.QaInteriorIntersectsOther_constraint))] [CanBeNull]
			string constraint)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			RelatedClasses = relatedClasses;
			Constraint = constraint;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaInteriorIntersectsOther_ValidIntersectionGeometryConstraint))]
		public string ValidIntersectionGeometryConstraint { get; set; }
	}
}
