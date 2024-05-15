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
	[MValuesTest]
	public class QaMonotonicMeasuresDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef LineClass { get; }
		public bool AllowConstantValues { get; set; }
		public MonotonicityDirection ExpectedMonotonicity { get; }
		public string FlipExpression { get; }

		[Doc(nameof(DocStrings.QaMonotonicMeasures_0))]
		public QaMonotonicMeasuresDefinition(
				[Doc(nameof(DocStrings.QaMonotonicMeasures_lineClass))] [NotNull]
				IFeatureClassSchemaDef lineClass,
				[Doc(nameof(DocStrings.QaMonotonicMeasures_allowConstantValues))]
				bool allowConstantValues)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(lineClass, allowConstantValues, MonotonicityDirection.Any, null) { }

		[Doc(nameof(DocStrings.QaMonotonicMeasures_1))]
		public QaMonotonicMeasuresDefinition(
			[Doc(nameof(DocStrings.QaMonotonicMeasures_lineClass))] [NotNull]
			IFeatureClassSchemaDef lineClass,
			[Doc(nameof(DocStrings.QaMonotonicMeasures_allowConstantValues))]
			bool allowConstantValues,
			[Doc(nameof(DocStrings.QaMonotonicMeasures_expectedMonotonicity))]
			MonotonicityDirection
				expectedMonotonicity,
			[Doc(nameof(DocStrings.QaMonotonicMeasures_flipExpression))] [CanBeNull]
			string flipExpression)
			: base(lineClass)
		{
			Assert.ArgumentNotNull(lineClass, nameof(lineClass));

			LineClass = lineClass;
			AllowConstantValues = allowConstantValues;
			ExpectedMonotonicity = expectedMonotonicity;
			FlipExpression = flipExpression;
		}
	}
}
