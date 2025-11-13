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
	[GeometryTest]
	[ZValuesTest]
	public class QaWithinZRangeDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double MinimumZValue { get; }
		public double MaximumZValue { get; }

		public IList<double> AllowedZValues { get; }

		[Doc(nameof(DocStrings.QaWithinZRange_0))]
		public QaWithinZRangeDefinition(
				[Doc(nameof(DocStrings.QaWithinZRange_featureClass))] [NotNull]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaWithinZRange_minimumZValue))]
				double minimumZValue,
				[Doc(nameof(DocStrings.QaWithinZRange_maximumZValue))]
				double maximumZValue)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, minimumZValue, maximumZValue, null) { }

		[Doc(nameof(DocStrings.QaWithinZRange_1))]
		public QaWithinZRangeDefinition(
			[Doc(nameof(DocStrings.QaWithinZRange_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaWithinZRange_minimumZValue))]
			double minimumZValue,
			[Doc(nameof(DocStrings.QaWithinZRange_maximumZValue))]
			double maximumZValue,
			[Doc(nameof(DocStrings.QaWithinZRange_allowedZValues))] [CanBeNull]
			IEnumerable<double> allowedZValues)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentCondition(maximumZValue >= minimumZValue,
			                         "Maximum z value must be equal or larger than minimum z value");

			FeatureClass = featureClass;
			MinimumZValue = minimumZValue;
			MaximumZValue = maximumZValue;
			if (allowedZValues != null)
			{
				AllowedZValues = allowedZValues.ToList();
			}
		}
	}
}
