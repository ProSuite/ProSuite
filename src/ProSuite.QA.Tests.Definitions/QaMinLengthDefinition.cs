using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Finds all lines that are too short
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMinLengthDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; set; }
		public double Limit { get; set; }
		public bool Is3D { get; set; }
		public bool PerPart { get; set; }

		[Doc(nameof(DocStrings.QaMinLength_0))]
		public QaMinLengthDefinition(
			[Doc(nameof(DocStrings.QaMinLength_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinLength_is3D))]
			bool is3D)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
			Is3D = is3D;
		}

		[Doc(nameof(DocStrings.QaMinLength_0))]
		public QaMinLengthDefinition(
			[Doc(nameof(DocStrings.QaMinLength_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinLength_limit))]
			double limit)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
		}

		[Doc(nameof(DocStrings.QaMinLength_0))]
		public QaMinLengthDefinition(
			[Doc(nameof(DocStrings.QaMinLength_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinLength_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaMinLength_perPart))]
			bool perPart)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
			Is3D = is3D;
			PerPart = perPart;
		}
	}
}
