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
	public class QaMinMeanSegmentLengthDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Limit { get; }
		public bool PerPart { get; }
		public bool Is3D { get; }

		[Doc(nameof(DocStrings.QaMinMeanSegmentLength_0))]
		public QaMinMeanSegmentLengthDefinition(
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_perPart))]
			bool perPart)
			: this(featureClass, limit, perPart, false) { }

		[Doc(nameof(DocStrings.QaMinMeanSegmentLength_0))]
		public QaMinMeanSegmentLengthDefinition(
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_perPart))]
			bool perPart,
			[Doc(nameof(DocStrings.QaMinMeanSegmentLength_is3D))]
			bool is3D)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			FeatureClass = featureClass;
			Limit = limit;
			PerPart = perPart;
			Is3D = is3D;
		}
	}
}
