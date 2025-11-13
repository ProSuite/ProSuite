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
	public class QaMonotonicZDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef LineClass { get; }

		private const MonotonicityDirection _defaultExpectedMonotonicity =
			MonotonicityDirection.Any;

		private const bool _defaultAllowConstantValues = true;
		private const string _defaultFlipExpression = null;

		[Doc(nameof(DocStrings.QaMonotonicZ_0))]
		public QaMonotonicZDefinition(
			[Doc(nameof(DocStrings.QaMonotonicZ_lineClass))] [NotNull]
			IFeatureClassSchemaDef lineClass)
			: base(lineClass)
		{
			Assert.ArgumentNotNull(lineClass, nameof(lineClass));

			AllowConstantValues = _defaultAllowConstantValues;
			ExpectedMonotonicity = _defaultExpectedMonotonicity;
			FlipExpression = _defaultFlipExpression;

			LineClass = lineClass;
		}

		[Doc(nameof(DocStrings.QaMonotonicZ_AllowConstantValues))]
		[TestParameter(_defaultAllowConstantValues)]
		public bool AllowConstantValues { get; set; }

		[Doc(nameof(DocStrings.QaMonotonicZ_ExpectedMonotonicity))]
		[TestParameter(_defaultExpectedMonotonicity)]
		public MonotonicityDirection ExpectedMonotonicity { get; set; }

		[Doc(nameof(DocStrings.QaMonotonicZ_FlipExpression))]
		[TestParameter(_defaultFlipExpression)]
		public string FlipExpression { get; set; }
	}
}
