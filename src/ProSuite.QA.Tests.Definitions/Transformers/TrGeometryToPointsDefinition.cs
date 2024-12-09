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
	public class TrGeometryToPointsDefinition : TrGeometryTransformDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }

		public GeometryComponent Component { get; }

		private readonly GeometryComponent _component;

		public const string AttrPartIndex = "PartIndex";
		public const string AttrVertexIndex = "VertexIndex";

		[DocTr(nameof(DocTrStrings.TrGeometryToPoints_0))]
		public TrGeometryToPointsDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrGeometryToPoints_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[DocTr(nameof(DocTrStrings.TrGeometryToPoints_component))]
			GeometryComponent component)
			: base(featureClass, ProSuiteGeometryType.Point)
		{
			FeatureClass = featureClass;
			Component = component;
			_component = component;
		}
	}
}
