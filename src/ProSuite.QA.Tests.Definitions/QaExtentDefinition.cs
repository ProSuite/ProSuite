using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Check whether the x or y extent of features - or feature parts - exceeds a given limit.
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaExtentDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Limit { get; }
		public bool PerPart { get; }

		[Doc(nameof(DocStrings.QaExtent_0))]
		public QaExtentDefinition(
				[Doc(nameof(DocStrings.QaExtent_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaExtent_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, limit, false) { }

		[Doc(nameof(DocStrings.QaExtent_1))]
		public QaExtentDefinition(
			[Doc(nameof(DocStrings.QaExtent_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaExtent_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaExtent_perPart))]
			bool perPart)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
			PerPart = perPart;
		}
	}
}
