using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaSegmentLengthDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Limit { get; }
		public bool Is3D { get; }

		[Doc(nameof(DocStrings.QaSegmentLength_0))]
		public QaSegmentLengthDefinition(
			[Doc(nameof(DocStrings.QaSegmentLength_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSegmentLength_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSegmentLength_is3D))]
			bool is3D)
			: base(featureClass)
		{
			FeatureClass = featureClass;
			Limit = limit;
			Is3D = is3D;
		}

		[Doc(nameof(DocStrings.QaSegmentLength_0))]
		public QaSegmentLengthDefinition(
			[Doc(nameof(DocStrings.QaSegmentLength_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSegmentLength_limit))]
			double limit)
			: this(featureClass, limit,
			       featureClass.ShapeType == ProSuiteGeometryType.MultiPatch) { }
	}
}
