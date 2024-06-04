using System.Collections.Generic;
using System.Linq;
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
	public class QaMustIntersectMatrixOtherDefinition : AlgorithmDefinition
	{
		public ICollection<IFeatureClassSchemaDef> FeatureClasses { get; }
		public ICollection<IFeatureClassSchemaDef> OtherFeatureClasses { get; }
		public string IntersectionMatrix { get; }
		public string RelevantRelationCondition { get; }
		public string RequiredIntersectionDimensions { get; }
		public string UnallowedIntersectionDimensions { get; }

		[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_0))]
		public QaMustIntersectMatrixOtherDefinition(
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_otherFeatureClass))] [NotNull]
			IFeatureClassSchemaDef otherFeatureClass,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_relevantRelationCondition))]
			[CanBeNull]
			string relevantRelationCondition)
			: this(featureClass, otherFeatureClass,
			       intersectionMatrix, relevantRelationCondition,
			       // ReSharper disable once IntroduceOptionalParameters.Global
			       null, null) { }

		[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_1))]
		public QaMustIntersectMatrixOtherDefinition(
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_otherFeatureClass))] [NotNull]
			IFeatureClassSchemaDef otherFeatureClass,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_relevantRelationCondition))]
			[CanBeNull]
			string relevantRelationCondition,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_requiredIntersectionDimensions))]
			string requiredIntersectionDimensions,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_unallowedIntersectionDimensions))]
			string unallowedIntersectionDimensions)
			: this(new[] { featureClass }, new[] { otherFeatureClass },
			       intersectionMatrix, relevantRelationCondition,
			       requiredIntersectionDimensions, unallowedIntersectionDimensions)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentNotNull(otherFeatureClass, nameof(otherFeatureClass));
		}

		[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_2))]
		public QaMustIntersectMatrixOtherDefinition(
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_featureClasses))] [NotNull]
			ICollection<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_otherFeatureClasses))] [NotNull]
			ICollection<IFeatureClassSchemaDef> otherFeatureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_intersectionMatrix))] [NotNull]
			string
				intersectionMatrix,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_relevantRelationCondition))]
			[CanBeNull]
			string
				relevantRelationCondition)
			: this(
				featureClasses, otherFeatureClasses,
				intersectionMatrix, relevantRelationCondition,
				// ReSharper disable once IntroduceOptionalParameters.Global
				null, null) { }

		[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_3))]
		public QaMustIntersectMatrixOtherDefinition(
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_featureClasses))] [NotNull]
			ICollection<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_otherFeatureClasses))] [NotNull]
			ICollection<IFeatureClassSchemaDef> otherFeatureClasses,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_intersectionMatrix))] [NotNull]
			string intersectionMatrix,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_relevantRelationCondition))]
			[CanBeNull]
			string relevantRelationCondition,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_requiredIntersectionDimensions))]
			[CanBeNull]
			string requiredIntersectionDimensions,
			[Doc(nameof(DocStrings.QaMustIntersectMatrixOther_unallowedIntersectionDimensions))]
			[CanBeNull]
			string unallowedIntersectionDimensions)
			: base(featureClasses.Union(otherFeatureClasses))
		{
			Assert.ArgumentNotNullOrEmpty(intersectionMatrix, nameof(intersectionMatrix));
			FeatureClasses = featureClasses;
			OtherFeatureClasses = otherFeatureClasses;
			IntersectionMatrix = intersectionMatrix;
			RelevantRelationCondition = relevantRelationCondition;
			RequiredIntersectionDimensions = requiredIntersectionDimensions;
			UnallowedIntersectionDimensions = unallowedIntersectionDimensions;
		}
	}
}
