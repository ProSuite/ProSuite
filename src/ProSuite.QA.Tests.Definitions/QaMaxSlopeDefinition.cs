using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Determine that no slope exceeds a certain limit
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	[ZValuesTest]
	public class QaMaxSlopeDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Limit { get; }

		[Doc(nameof(DocStrings.QaMaxSlope_0))]
		public QaMaxSlopeDefinition(
			[Doc(nameof(DocStrings.QaMaxSlope_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMaxSlope_limit))]
			double limit)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
		}

		[TestParameter(DefaultAngleUnit)]
		[Doc(nameof(DocStrings.QaLineIntersectAngle_AngularUnit))]
		public AngleUnit AngularUnit { get; set; } = DefaultAngleUnit;
	}
}
