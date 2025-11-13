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
	public class TrLineToPolygonDefinition : TrGeometryTransformDefinition
	{
		public IFeatureClassSchemaDef ClosedLineClass { get; }

		private const PolylineConversion _defaultPolylineUsage =
			PolylineConversion.AsPolygonIfClosedElseIgnore;

		[DocTr(nameof(DocTrStrings.TrLineToPolygon_0))]
		public TrLineToPolygonDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrLineToPolygon_closedLineClass))]
			IFeatureClassSchemaDef closedLineClass)
			: base(closedLineClass, ProSuiteGeometryType.Polygon)
		{
			ClosedLineClass = closedLineClass;
		}

		[TestParameter(_defaultPolylineUsage)]
		[DocTr(nameof(DocTrStrings.TrLineToPolygon_PolylineUsage))]
		public PolylineConversion PolylineUsage { get; set; }
	}
}
