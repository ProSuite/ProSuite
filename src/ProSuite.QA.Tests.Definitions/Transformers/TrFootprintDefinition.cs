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
	public class TrFootprintDefinition : TrGeometryTransformDefinition
	{
		public IFeatureClassSchemaDef MultipatchClass { get; }

		private const double _defaultToleranceValue = -1;

		[DocTr(nameof(DocTrStrings.TrFootprint_0))]
		public TrFootprintDefinition(
			[NotNull] [DocTr(nameof(DocTrStrings.TrFootprint_multipatchClass))]
			IFeatureClassSchemaDef multipatchClass)
			: base(multipatchClass, ProSuiteGeometryType.Polygon)
		{
			MultipatchClass = multipatchClass;
		}

		[TestParameter(_defaultToleranceValue)]
		[DocTr(nameof(DocTrStrings.TrFootprint_Tolerance))]
		public double Tolerance { get; set; }
	}
}
