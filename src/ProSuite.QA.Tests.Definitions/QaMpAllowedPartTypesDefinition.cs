using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpAllowedPartTypesDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef MultiPatchClass { get; }
		public bool AllowRings { get; }
		public bool AllowTriangleFans { get; }
		public bool AllowTriangleStrips { get; }
		public bool AllowTriangles { get; }

		[Doc(nameof(DocStrings.QaMpAllowedPartTypes_0))]
		public QaMpAllowedPartTypesDefinition(
			[Doc(nameof(DocStrings.QaMpAllowedPartTypes_multiPatchClass))]
			IFeatureClassSchemaDef multiPatchClass,
			[Doc(nameof(DocStrings.QaMpAllowedPartTypes_allowRings))]
			bool allowRings,
			[Doc(nameof(DocStrings.QaMpAllowedPartTypes_allowTriangleFans))]
			bool allowTriangleFans,
			[Doc(nameof(DocStrings.QaMpAllowedPartTypes_allowTriangleStrips))]
			bool allowTriangleStrips,
			[Doc(nameof(DocStrings.QaMpAllowedPartTypes_allowTriangles))]
			bool allowTriangles) :
			base(multiPatchClass)
		{
			MultiPatchClass = multiPatchClass;
			AllowRings = allowRings;
			AllowTriangleFans = allowTriangleFans;
			AllowTriangleStrips = allowTriangleStrips;
			AllowTriangles = allowTriangles;
		}
	}
}
