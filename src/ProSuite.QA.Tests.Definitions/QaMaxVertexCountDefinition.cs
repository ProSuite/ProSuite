using ProSuite.Commons.AO.Geodatabase;
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
	public class QaMaxVertexCountDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Limit { get; }
		public bool PerPart { get; }

		[Doc(nameof(DocStrings.QaMaxVertexCount_0))]
		public QaMaxVertexCountDefinition(
			[Doc(nameof(DocStrings.QaMaxVertexCount_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMaxVertexCount_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMaxVertexCount_perPart))]
			bool perPart)
			: base((ITableSchemaDef)featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			FeatureClass = (IFeatureClassSchemaDef) featureClass;
			Limit = limit;
			PerPart = perPart;
		}
	}
}
