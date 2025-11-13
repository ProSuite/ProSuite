using ProSuite.Commons.Essentials.Assertions;
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
	public class QaNoClosedPathsDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PolyLineClass { get; }

		[Doc(nameof(DocStrings.QaNoClosedPaths_0))]
		public QaNoClosedPathsDefinition(
			[Doc(nameof(DocStrings.QaNoClosedPaths_polylineClass))] [NotNull]
			IFeatureClassSchemaDef polyLineClass)
			: base(polyLineClass)
		{
			Assert.ArgumentNotNull(polyLineClass, nameof(polyLineClass));
			Assert.ArgumentCondition(
				polyLineClass.ShapeType == ProSuiteGeometryType.Polyline,
				"polyline feature class expected");

			PolyLineClass = polyLineClass;
		}
	}
}
