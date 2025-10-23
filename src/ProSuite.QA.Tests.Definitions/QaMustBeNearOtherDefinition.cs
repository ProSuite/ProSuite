using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[ProximityTest]
	public class QaMustBeNearOtherDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public ICollection<IFeatureClassSchemaDef> NearClasses { get; }
		public double MaximumDistance { get; }
		public string RelevantRelationCondition { get; }


		[Doc(nameof(DocStrings.QaMustBeNearOther_0))]
		public QaMustBeNearOtherDefinition(
			[NotNull] [Doc(nameof(DocStrings.QaMustBeNearOther_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[NotNull] [Doc(nameof(DocStrings.QaMustBeNearOther_nearClasses))]
			ICollection<IFeatureClassSchemaDef> nearClasses,
			[Doc(nameof(DocStrings.QaMustBeNearOther_maximumDistance))]
			double maximumDistance,
			[Doc(nameof(DocStrings.QaMustBeNearOther_relevantRelationCondition))] [CanBeNull]
			string relevantRelationCondition)
			: base(new[] { featureClass }.Union(nearClasses).ToList())
		{
			FeatureClass = featureClass;
			NearClasses = nearClasses;
			MaximumDistance = maximumDistance;
			RelevantRelationCondition = relevantRelationCondition;
		}

		[CanBeNull]
		[TestParameter]
		[Doc(nameof(DocStrings.QaMustBeNearOther_ErrorDistanceFormat))]
		public string ErrorDistanceFormat { get; set; }
	}
}
