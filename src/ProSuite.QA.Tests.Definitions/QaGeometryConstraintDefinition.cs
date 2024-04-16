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
	public class QaGeometryConstraintDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public string GeometryConstraint { get; }
		public bool PerPart { get; }

		[Doc(nameof(DocStrings.QaGeometryConstraint_0))]
		public QaGeometryConstraintDefinition(
			[Doc(nameof(DocStrings.QaGeometryConstraint_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaGeometryConstraint_geometryConstraint))] [NotNull]
			string
				geometryConstraint,
			[Doc(nameof(DocStrings.QaGeometryConstraint_perPart))]
			bool perPart)
			: base(featureClass)
		{
			Assert.ArgumentNotNullOrEmpty(geometryConstraint, nameof(geometryConstraint));

			FeatureClass = featureClass;
			GeometryConstraint = geometryConstraint;
			PerPart = perPart;
		}
	}
}
