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
	public class QaSliverPolygonDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PolygonClass { get; }
		public double Limit { get; }
		public double MaxArea { get; }

		[Doc(nameof(DocStrings.QaSliverPolygon_0))]
		public QaSliverPolygonDefinition(
				[Doc(nameof(DocStrings.QaSliverPolygon_polygonClass))]
				IFeatureClassSchemaDef polygonClass,
				[Doc(nameof(DocStrings.QaSliverPolygon_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, limit, -1) { }

		[Doc(nameof(DocStrings.QaSliverPolygon_0))]
		public QaSliverPolygonDefinition(
			[Doc(nameof(DocStrings.QaSliverPolygon_polygonClass))]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaSliverPolygon_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaSliverPolygon_maxArea))]
			double maxArea)
			: base(polygonClass)
		{
			Assert.ArgumentNotNull(polygonClass, nameof(polygonClass));
			Assert.ArgumentCondition(
				polygonClass.ShapeType == ProSuiteGeometryType.Polygon ||
				polygonClass.ShapeType == ProSuiteGeometryType.MultiPatch,
				"Not a polygon or Multipatch feature class");

			PolygonClass = polygonClass;
			Limit = limit;
			MaxArea = maxArea;
		}
	}
}
