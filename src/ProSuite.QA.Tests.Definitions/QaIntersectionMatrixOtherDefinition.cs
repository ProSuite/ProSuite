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
	public class QaIntersectionMatrixOtherDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public IList<IFeatureClassSchemaDef> RelatedClasses { get; }
		public string IntersectionMatrix { get; }
		public string Constraint { get; }
		public string ValidIntersectionDimensions { get; }

		[Doc(nameof(DocStrings.QaIntersectionMatrixOther_0))]
		public QaIntersectionMatrixOtherDefinition(
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_relatedClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> relatedClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix)
			: this(featureClasses, relatedClasses, intersectionMatrix, string.Empty, null) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixOther_1))]
		public QaIntersectionMatrixOtherDefinition(
				[Doc(nameof(DocStrings.QaIntersectionMatrixOther_featureClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> featureClasses,
				[Doc(nameof(DocStrings.QaIntersectionMatrixOther_relatedClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> relatedClasses,
				[Doc(nameof(DocStrings.QaIntersectionMatrixOther_intersectionMatrix))] [NotNull]
				string intersectionMatrix,
				[Doc(nameof(DocStrings.QaIntersectionMatrixOther_constraint))] [CanBeNull]
				string constraint)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, relatedClasses, intersectionMatrix, constraint, null) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixOther_2))]
		public QaIntersectionMatrixOtherDefinition(
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_relatedClass))] [NotNull]
			IFeatureClassSchemaDef relatedClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix)
			: this(featureClass, relatedClass, intersectionMatrix, string.Empty) { }

		[Doc(nameof(DocStrings.QaIntersectionMatrixOther_3))]
		public QaIntersectionMatrixOtherDefinition(
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_relatedClass))] [NotNull]
			IFeatureClassSchemaDef relatedClass,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_constraint))] [CanBeNull]
			string constraint)
			: this(new[] { featureClass }, new[] { relatedClass }, intersectionMatrix,
				   constraint)
		{ }

		[Doc(nameof(DocStrings.QaIntersectionMatrixOther_4))]
		public QaIntersectionMatrixOtherDefinition(
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_relatedClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> relatedClasses,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_constraint))] [CanBeNull]
			string constraint,
			[Doc(nameof(DocStrings.QaIntersectionMatrixOther_validIntersectionDimensions))]
			[CanBeNull]
			string validIntersectionDimensions)
			: base(featureClasses.Union(relatedClasses))
		{
			FeatureClasses = featureClasses;
			RelatedClasses = relatedClasses;
			IntersectionMatrix = intersectionMatrix;
			Constraint = constraint;
			ValidIntersectionDimensions = validIntersectionDimensions;
		}
	}
}
