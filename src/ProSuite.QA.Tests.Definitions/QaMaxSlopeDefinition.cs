using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Core.ParameterTypes;

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
		private const AngleUnit _defaultAngularUnit = (AngleUnit)0;
		//private const AngleUnit _defaultAngularUnit = DefaultAngleUnit;
		public IReadOnlyFeatureClass FeatureClass { get; }
		public double Limit { get; }
		//private double _limitRad;

		[Doc(nameof(DocStrings.QaMaxSlope_0))]
		public QaMaxSlopeDefinition(
			[Doc(nameof(DocStrings.QaMaxSlope_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMaxSlope_limit))]
			double limit)
			: base((ITableSchemaDef)featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
		}

		[TestParameter(DefaultAngleUnit)]
		[Doc(nameof(DocStrings.QaLineIntersectAngle_AngularUnit))]
		public AngleUnit AngularUnit { get; set; } = DefaultAngleUnit;
	}
}
