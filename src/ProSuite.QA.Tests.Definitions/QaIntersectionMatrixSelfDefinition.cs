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
	public class QaIntersectionMatrixSelfDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public string IntersectionMatrix { get; set; }
		public string Constraint { get; }
		public string ValidIntersectionDimensions { get; set; }

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_0))]
		public QaIntersectionMatrixSelfDefinition(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string intersectionMatrix)
			: this(featureClasses, intersectionMatrix, string.Empty) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_1))]
		public QaIntersectionMatrixSelfDefinition(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_constraint))]
			string constraint)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			IntersectionMatrix = intersectionMatrix;
			Constraint = constraint;
		}

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_2))]
		public QaIntersectionMatrixSelfDefinition(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string intersectionMatrix)
			: this(featureClass, intersectionMatrix, string.Empty) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_3))]
		public QaIntersectionMatrixSelfDefinition(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_constraint))]
			string constraint)
			: this(new[] { featureClass }, intersectionMatrix, constraint) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_4))]
		public QaIntersectionMatrixSelfDefinition(
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_constraint))]
			string constraint,
			[Doc(nameof(DocStrings.QaIntersectionMatrixSelf_validIntersectionDimensions))]
			[CanBeNull]
			string validIntersectionDimensions)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			IntersectionMatrix = intersectionMatrix;
			Constraint = constraint;
			ValidIntersectionDimensions = validIntersectionDimensions;
		}
	}
}
