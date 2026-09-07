#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaAreaPredictionMatchTest
	{

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void DefinitionAcceptsRasterDatasetReference()
		{
			IFeatureWorkspace workspace = TestWorkspaceUtils.CreateInMemoryWorkspace(
				nameof(DefinitionAcceptsRasterDatasetReference));
			IFeatureClass featureClass = TestWorkspaceUtils.CreateSimpleFeatureClass(
				workspace, "ExistingBuildings", esriGeometryType.esriGeometryPolygon);
			IPoint origin = new PointClass();
			IRasterDataset rasterDataset = ((IRasterWorkspace2) workspace).CreateRasterDataset(
				"Image", "MEM", origin, 10, 10, 1, 1, 3,
				rstPixelType.PT_UCHAR, null);

			var definition = new QaAreaPredictionMatchDefinition(
				ReadOnlyTableFactory.Create(featureClass),
				new RasterDatasetReference(rasterDataset),
				"http://localhost", "buildings");

			Assert.DoesNotThrow(() => new QaAreaPredictionMatch(definition));
		}

		[Test]
		public void DefinitionAcceptsMosaicDatasetReference()
		{
			IFeatureWorkspace workspace = TestWorkspaceUtils.CreateTestFgdbWorkspace(
				nameof(DefinitionAcceptsMosaicDatasetReference));
			IFeatureClass featureClass = TestWorkspaceUtils.CreateSimpleFeatureClass(
				workspace, "ExistingBuildings", esriGeometryType.esriGeometryPolygon);
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);
			IMosaicDataset mosaicDataset = CreateMosaicDataset(
				(IWorkspace) workspace, "ImageMosaic", spatialReference);

			var definition = new QaAreaPredictionMatchDefinition(
				ReadOnlyTableFactory.Create(featureClass),
				new MosaicRasterReference(new SimpleRasterMosaic(mosaicDataset)),
				"http://localhost", "buildings");

			Assert.DoesNotThrow(() => new QaAreaPredictionMatch(definition));
		}

		private static IMosaicDataset CreateMosaicDataset(
			IWorkspace workspace, string name, ISpatialReference spatialReference)
		{
			var parameters = new CreateMosaicDatasetParametersClass
			                 {
				                 BandCount = 3,
				                 PixelType = rstPixelType.PT_UCHAR
			                 };
			var helper = new MosaicWorkspaceExtensionHelperClass();
			IMosaicWorkspaceExtension extension = helper.FindExtension(workspace);
			return extension.CreateMosaicDataset(name, spatialReference, parameters, null);
		}
	}

	[TestFixture]
	public class QaAreaPredictionMatchConstructorTest
	{
		[TestCase("MatchMode")]
		public void OptionalParameterMetadataMatchesDefinition(string name)
		{
			PropertyInfo implementation = typeof(QaAreaPredictionMatch).GetProperty(name);
			PropertyInfo definition = typeof(QaAreaPredictionMatchDefinition).GetProperty(name);
			Assert.NotNull(implementation);
			Assert.NotNull(definition);
			Assert.AreEqual(implementation.PropertyType, definition.PropertyType);
			Assert.AreEqual(implementation.GetCustomAttribute<TestParameterAttribute>().DefaultValue,
			                definition.GetCustomAttribute<TestParameterAttribute>().DefaultValue);
			string description = implementation.GetCustomAttribute<DocAttribute>().Description;
			Assert.That(description, Is.Not.Null.And.Not.Empty);
			Assert.AreEqual(description, definition.GetCustomAttribute<DocAttribute>().Description);
		}

		[Test]
		public void PredictionShapefilePathIsNotExposedInDefinition()
		{
			Assert.NotNull(typeof(QaAreaPredictionMatch).GetProperty("PredictionShapefilePath"));
			Assert.Null(
				typeof(QaAreaPredictionMatchDefinition).GetProperty("PredictionShapefilePath"));
		}

		[Test]
		public void ExposesMosaicDatasetConstructors()
		{
			Assert.AreEqual(
				TestParameterType.RasterMosaicDataset,
				TestParameterTypes.GetParameterType(typeof(IMosaicRasterDatasetDef)));
			Assert.NotNull(typeof(QaAreaPredictionMatchDefinition).GetConstructor(
				new[]
				{
					typeof(IFeatureClassSchemaDef), typeof(IMosaicRasterDatasetDef),
					typeof(string), typeof(string)
				}));
			Assert.NotNull(typeof(QaAreaPredictionMatchDefinition).GetConstructor(
				new[]
				{
					typeof(IFeatureClassSchemaDef), typeof(IMosaicRasterDatasetDef),
					typeof(string), typeof(string), typeof(double), typeof(double)
				}));
			Assert.NotNull(typeof(QaAreaPredictionMatch).GetConstructor(
				new[]
				{
					typeof(IReadOnlyFeatureClass), typeof(MosaicRasterReference),
					typeof(string), typeof(string)
				}));
			Assert.NotNull(typeof(QaAreaPredictionMatch).GetConstructor(
				new[]
				{
					typeof(IReadOnlyFeatureClass), typeof(MosaicRasterReference),
					typeof(string), typeof(string), typeof(double), typeof(double)
				}));
		}
	}
}
