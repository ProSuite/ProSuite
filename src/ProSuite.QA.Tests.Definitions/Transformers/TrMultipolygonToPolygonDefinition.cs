using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrMultipolygonToPolygonDefinition : TrGeometryTransformDefinition
	{
		private const PolygonPart _defaultPolygonPart = PolygonPart.SinglePolygons;

		[DocTr(nameof(DocTrStrings.TrMultipolygonToPolygon_0))]
		public TrMultipolygonToPolygonDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrMultipolygonToPolygon_featureClass))]
			IFeatureClassSchemaDef featureClass)
			: base(featureClass, ProSuiteGeometryType.Polygon) { }

		[TestParameter(_defaultPolygonPart)]
		[DocTr(nameof(DocTrStrings.TrMultipolygonToPolygon_TransformedParts))]
		public PolygonPart TransformedParts { get; set; }
	}
}
