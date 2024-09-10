using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[GeometryTransformer]
	public class TrMultilineToLineDefinition : TrGeometryTransformDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }

		[DocTr(nameof(DocTrStrings.TrMultilineToLine_0))]
		public TrMultilineToLineDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrMultilineToLine_featureClass))]
			IFeatureClassSchemaDef featureClass)
			: base(featureClass, ProSuiteGeometryType.Polyline)
		{
			FeatureClass = featureClass; 
		}
	}
}
