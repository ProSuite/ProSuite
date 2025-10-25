using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	[ZValuesTest]
	public class QaSmoothDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Limit { get; }
		public  double LimitCstr { get; }
		private const AngleUnit _defaultAngularUnit = DefaultAngleUnit;

		[Doc(nameof(DocStrings.QaSmooth_0))]
		public QaSmoothDefinition(
			[Doc(nameof(DocStrings.QaSmooth_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSmooth_limit))]
			double limit)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
			LimitCstr = limit;
		}

		[TestParameter(_defaultAngularUnit)]
		[Doc(nameof(DocStrings.QaSmooth_AngularUnit))]
		public AngleUnit AngularUnit { get; set; }
	}
}
