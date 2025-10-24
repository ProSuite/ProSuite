using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check whether the x and y extent of features - or feature parts - are below a given limit.
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMinExtentDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Limit { get; }

		[Doc(nameof(DocStrings.QaMinExtent_0))]
		public QaMinExtentDefinition(
			[Doc(nameof(DocStrings.QaExtent_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinExtent_limit))]
			double limit)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaExtent_perPart))]
		public bool PerPart { get; set; }
	}
}
