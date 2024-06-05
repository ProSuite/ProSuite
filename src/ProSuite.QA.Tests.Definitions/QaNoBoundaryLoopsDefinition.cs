using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaNoBoundaryLoopsDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef PolygonClass { get; }
		public BoundaryLoopErrorGeometry ErrorGeometry { get; }
		public BoundaryLoopAreaRelation AreaRelation { get; }
		public double AreaLimit { get; }

		[Doc(nameof(DocStrings.QaNoBoundaryLoops_0))]
		public QaNoBoundaryLoopsDefinition(
				[Doc(nameof(DocStrings.QaNoBoundaryLoops_polygonClass))] [NotNull]
				IFeatureClassSchemaDef polygonClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(polygonClass, BoundaryLoopErrorGeometry.LoopPolygon) { }

		[Doc(nameof(DocStrings.QaNoBoundaryLoops_1))]
		public QaNoBoundaryLoopsDefinition(
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_errorGeometry))]
			BoundaryLoopErrorGeometry errorGeometry)
			: this(
				// ReSharper disable once IntroduceOptionalParameters.Global
				polygonClass, errorGeometry, BoundaryLoopAreaRelation.IgnoreSmallerOrEqual, 0) { }

		[Doc(nameof(DocStrings.QaNoBoundaryLoops_2))]
		public QaNoBoundaryLoopsDefinition(
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_polygonClass))] [NotNull]
			IFeatureClassSchemaDef polygonClass,
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_errorGeometry))]
			BoundaryLoopErrorGeometry errorGeometry,
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_areaRelation))]
			BoundaryLoopAreaRelation areaRelation,
			[Doc(nameof(DocStrings.QaNoBoundaryLoops_areaLimit))]
			double areaLimit)
			: base(polygonClass)
		{
			Assert.ArgumentNotNull(polygonClass, nameof(polygonClass));
			Assert.ArgumentCondition(
				polygonClass.ShapeType == ProSuiteGeometryType.Polygon ||
				polygonClass.ShapeType == ProSuiteGeometryType.MultiPatch,
				"polygon or multipatch feature class expected");

			PolygonClass = polygonClass;
			ErrorGeometry = errorGeometry;
			AreaRelation = areaRelation;
			AreaLimit = areaLimit;
		}
	}
}
