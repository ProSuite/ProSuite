using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrPolygonToLineDefinition : TrGeometryTransformDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }

		[DocTr(nameof(DocTrStrings.TrPolygonToLine_0))]
		public TrPolygonToLineDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrPolygonToLine_featureClass))]
			IFeatureClassSchemaDef featureClass)
			: base(featureClass, ProSuiteGeometryType.Polyline)
		{
			FeatureClass = featureClass;
		}
	}
}
