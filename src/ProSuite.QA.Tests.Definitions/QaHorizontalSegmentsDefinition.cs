using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports non-linear polycurve segments as errors
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	[ZValuesTest]
	public class QaHorizontalSegmentsDefinition : AlgorithmDefinition
	{
		[Doc(nameof(DocStrings.QaHorizontalSegments_0))]
		public QaHorizontalSegmentsDefinition(
			[Doc(nameof(DocStrings.QaHorizontalSegments_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaHorizontalSegments_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaHorizontalSegments_tolerance))]
			double tolerance)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
			Tolerance = tolerance;
		}

		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Limit { get; }
		public double Tolerance { get; }
	}
}
