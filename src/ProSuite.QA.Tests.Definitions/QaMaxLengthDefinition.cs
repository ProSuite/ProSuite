using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports lines or polygon boundaries that are longer than a limit.
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMaxLengthDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Limit { get; }
		public bool Is3D { get; }
		public bool PerPart { get; }

		[Doc(nameof(DocStrings.QaMaxLength_0))]
		public QaMaxLengthDefinition(
			[Doc(nameof(DocStrings.QaMaxLength_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMaxLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMaxLength_is3D))]
			bool is3D)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
			Is3D = is3D;
		}

		[Doc(nameof(DocStrings.QaMaxLength_0))]
		public QaMaxLengthDefinition(
			[Doc(nameof(DocStrings.QaMaxLength_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMaxLength_limit))]
			double limit)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
		}

		[Doc(nameof(DocStrings.QaMaxLength_0))]
		public QaMaxLengthDefinition(
			[Doc(nameof(DocStrings.QaMaxLength_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMaxLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMaxLength_is3D))]
			bool is3D,
			[Doc(nameof(DocStrings.QaMaxLength_perPart))]
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
