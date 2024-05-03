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
	public class QaOverlapsOtherDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> OverlappedClasses { get; }
		public IList<IFeatureClassSchemaDef> OverlappingClasses { get; }
		public string ValidRelationConstraint { get; }

		[Doc(nameof(DocStrings.QaOverlapsOther_0))]
		public QaOverlapsOtherDefinition(
				[Doc(nameof(DocStrings.QaOverlapsOther_overlappedClasses))]
				IList<IFeatureClassSchemaDef> overlapped,
				[Doc(nameof(DocStrings.QaOverlapsOther_overlappingClasses))]
				IList<IFeatureClassSchemaDef> overlapping)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(overlapped, overlapping, null) { }

		[Doc(nameof(DocStrings.QaOverlapsOther_1))]
		public QaOverlapsOtherDefinition(
				[Doc(nameof(DocStrings.QaOverlapsOther_overlappedClass))]
				IFeatureClassSchemaDef overlapped,
				[Doc(nameof(DocStrings.QaOverlapsOther_overlappingClass))]
				IFeatureClassSchemaDef overlapping)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(overlapped, overlapping, null) { }

		[Doc(nameof(DocStrings.QaOverlapsOther_2))]
		public QaOverlapsOtherDefinition(
			[Doc(nameof(DocStrings.QaOverlapsOther_overlappedClasses))]
			IList<IFeatureClassSchemaDef> overlappedClasses,
			[Doc(nameof(DocStrings.QaOverlapsOther_overlappingClasses))]
			IList<IFeatureClassSchemaDef> overlappingClasses,
			[Doc(nameof(DocStrings.QaOverlapsOther_validRelationConstraint))]
			string validRelationConstraint)
			: base(Union(overlappedClasses, overlappingClasses))
		{
			OverlappedClasses = overlappedClasses;
			OverlappingClasses = overlappingClasses;
			ValidRelationConstraint = validRelationConstraint;
		}

		[Doc(nameof(DocStrings.QaOverlapsOther_3))]
		public QaOverlapsOtherDefinition(
			[Doc(nameof(DocStrings.QaOverlapsOther_overlappedClass))]
			IFeatureClassSchemaDef overlappedClass,
			[Doc(nameof(DocStrings.QaOverlapsOther_overlappingClass))]
			IFeatureClassSchemaDef overlappingClass,
			[Doc(nameof(DocStrings.QaOverlapsOther_validRelationConstraint))]
			string validRelationConstraint)
			: this(new[] { overlappedClass }, new[] { overlappingClass },
			       validRelationConstraint) { }
	}
}
