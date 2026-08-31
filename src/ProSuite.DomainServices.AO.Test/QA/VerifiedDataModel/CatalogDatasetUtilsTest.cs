using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;

namespace ProSuite.DomainServices.AO.Test.QA.VerifiedDataModel
{
	/// <summary>
	/// Turning a harvested polygon feature class into a raster catalog, using the field roles that
	/// a standalone condition list transports alongside the dataset parameter value.
	/// </summary>
	[TestFixture]
	public class CatalogDatasetUtilsTest
	{
		private const string _catalogName = "RAS_DTM_TILES";
		private const string _pointCloudName = "LAS_TILES";
		private const string _pathField = "CURRENT_FILE_PATH";

		[Test]
		public void Harvested_vector_dataset_becomes_a_raster_catalog()
		{
			DdxModel model = CreateModel();

			var harvested = model.AddDataset(new VerifiedVectorDataset(_catalogName)
			                                 {
				                                 GeometryType = PolygonGeometryType,
				                                 AliasName = "DTM tiles"
			                                 });

			CatalogDatasetUtils.ApplyDeclarations(model, GetFilePathRole(_catalogName));

			Dataset applied = model.GetDatasetByModelName(_catalogName);

			Assert.AreNotSame(harvested, applied, "The harvested dataset was not replaced");

			var catalog = applied as IRasterCatalogDataset;

			Assert.NotNull(catalog, "Not a raster catalog dataset");
			Assert.AreEqual(_pathField, catalog.FilePathFieldName);
			Assert.AreSame(applied, catalog.CatalogDataset,
			               "The catalog of a raster catalog dataset is the dataset itself");

			// It stays a polygon vector dataset - it is dual-natured, exactly like the DDX's
			// elevation raster dataset.
			Assert.IsInstanceOf<VectorDataset>(applied);
			Assert.AreSame(PolygonGeometryType, applied.GeometryType);
			Assert.AreEqual("DTM tiles", applied.AliasName);
		}

		[Test]
		public void Harvested_vector_dataset_becomes_a_point_cloud_catalog()
		{
			DdxModel model = CreateModel();

			var harvested = model.AddDataset(new VerifiedVectorDataset(_pointCloudName)
			                                 {
				                                 GeometryType = PolygonGeometryType,
				                                 AliasName = "LAS tiles"
			                                 });

			CatalogDatasetUtils.ApplyDeclarations(
				model, GetFilePathRole(_pointCloudName,
				                       SupportedDatasetType.PointCloudCatalog));

			Dataset applied = model.GetDatasetByModelName(_pointCloudName);

			Assert.AreNotSame(harvested, applied, "The harvested dataset was not replaced");

			var catalog = applied as IPointCloudCatalogDataset;

			Assert.NotNull(catalog, "Not a point cloud catalog dataset");
			Assert.AreEqual(_pathField, catalog.FilePathFieldName);
			Assert.AreSame(applied, catalog.CatalogDataset,
			               "The catalog of a point cloud catalog dataset is the dataset itself");

			// It stays a polygon vector dataset - dual-natured, exactly like the raster catalog.
			Assert.IsInstanceOf<VectorDataset>(applied);
			Assert.AreSame(PolygonGeometryType, applied.GeometryType);
			Assert.AreEqual("LAS tiles", applied.AliasName);
		}

		[Test]
		public void A_point_cloud_catalog_is_not_a_raster_source()
		{
			// The two catalogs are indistinguishable in the geodatabase and carry the same file
			// path role. Only the declared type decides, and the resulting dataset must not be
			// bindable to a raster mosaic parameter.
			DdxModel model = CreateModel();

			model.AddDataset(new VerifiedVectorDataset(_pointCloudName)
			                 {
				                 GeometryType = PolygonGeometryType
			                 });

			CatalogDatasetUtils.ApplyDeclarations(
				model, GetFilePathRole(_pointCloudName,
				                       SupportedDatasetType.PointCloudCatalog));

			Dataset applied = model.GetDatasetByModelName(_pointCloudName);

			Assert.IsNotInstanceOf<IRasterMosaicDataset>(applied);
			Assert.IsNotInstanceOf<IRasterCatalogDataset>(applied);
		}

		[Test]
		public void A_raster_catalog_is_not_a_point_cloud_source()
		{
			DdxModel model = CreateModel();

			model.AddDataset(new VerifiedVectorDataset(_catalogName)
			                 {
				                 GeometryType = PolygonGeometryType
			                 });

			CatalogDatasetUtils.ApplyDeclarations(model, GetFilePathRole(_catalogName));

			Assert.IsNotInstanceOf<IPointCloudDataset>(
				model.GetDatasetByModelName(_catalogName));
		}

		[Test]
		public void Point_cloud_catalog_without_a_file_path_role_is_rejected()
		{
			var roles = new List<DatasetFieldRole>
			            {
				            new DatasetFieldRole(AttributeRole.ObjectID, "OBJECTID")
			            };

			Assert.Throws<ArgumentException>(() => new VerifiedPointCloudCatalogDataset(
				                                 _pointCloudName, roles));
		}

		[Test]
		public void Other_datasets_are_left_alone()
		{
			DdxModel model = CreateModel();

			model.AddDataset(new VerifiedVectorDataset(_catalogName)
			                 {
				                 GeometryType = PolygonGeometryType
			                 });

			var other = model.AddDataset(new VerifiedVectorDataset("TLM_STRASSE")
			                             {
				                             GeometryType = PolygonGeometryType
			                             });

			CatalogDatasetUtils.ApplyDeclarations(model, GetFilePathRole(_catalogName));

			Assert.AreSame(other, model.GetDatasetByModelName("TLM_STRASSE"));
		}

		[Test]
		public void Nothing_happens_without_transported_roles()
		{
			DdxModel model = CreateModel();

			var harvested = model.AddDataset(new VerifiedVectorDataset(_catalogName)
			                                 {
				                                 GeometryType = PolygonGeometryType
			                                 });

			CatalogDatasetUtils.ApplyDeclarations(
				model, new List<DatasetDeclaration>());

			Assert.AreSame(harvested, model.GetDatasetByModelName(_catalogName));
		}

		[Test]
		public void Unharvested_dataset_is_not_an_error_here()
		{
			// Whether a referenced but unharvested dataset is fatal is decided by the regular
			// unknown-dataset handling, not here.
			DdxModel model = CreateModel();

			Assert.DoesNotThrow(() => CatalogDatasetUtils.ApplyDeclarations(
				                    model, GetFilePathRole(_catalogName)));
		}

		[Test]
		public void Non_vector_dataset_as_a_catalog_is_an_error()
		{
			DdxModel model = CreateModel();

			model.AddDataset(new VerifiedTableDataset(_catalogName));

			var exception =
				Assert.Throws<InvalidConfigurationException>(() =>
					                                             CatalogDatasetUtils
						                                             .ApplyDeclarations(
							                                             model,
							                                             GetFilePathRole(
								                                             _catalogName)));

			Assert.IsTrue(exception.Message.Contains(_catalogName), exception.Message);
		}

		[Test]
		public void The_declared_type_decides_which_catalog_is_created()
		{
			// A file-path role by itself does not say what kind of catalog this is: a point
			// cloud catalog has one too. Only the declared type does.
			DdxModel model = CreateModel();

			model.AddDataset(new VerifiedVectorDataset(_catalogName)
			                 {
				                 GeometryType = PolygonGeometryType
			                 });

			var exception =
				Assert.Throws<InvalidConfigurationException>(() =>
					                                             CatalogDatasetUtils
						                                             .ApplyDeclarations(
							                                             model,
							                                             GetFilePathRole(
								                                             _catalogName,
								                                             SupportedDatasetType
									                                             .FeatureClass)));

			Assert.IsTrue(exception.Message.Contains(_catalogName), exception.Message);
			Assert.IsTrue(exception.Message.Contains("FeatureClass"), exception.Message);
		}

		[Test]
		public void Catalog_without_a_file_path_role_is_rejected()
		{
			// A catalog whose file path cannot be found is useless; fail where the cause is.
			var roles = new List<DatasetFieldRole>
			            {
				            new DatasetFieldRole(AttributeRole.ObjectID, "OBJECTID")
			            };

			Assert.Throws<ArgumentException>(() => new VerifiedRasterCatalogDataset(
				                                 _catalogName, roles));
		}

		#region Test setup

		private static readonly GeometryTypeShape PolygonGeometryType =
			new GeometryTypeShape("Polygon", ProSuiteGeometryType.Polygon);

		private static IList<DatasetDeclaration> GetFilePathRole(
			string datasetName,
			SupportedDatasetType datasetType = SupportedDatasetType.RasterCatalog)
		{
			return new List<DatasetDeclaration>
			       {
				       new DatasetDeclaration(
					       datasetName, datasetType,
					       new List<DatasetFieldRole>
					       {
						       new DatasetFieldRole(AttributeRole.FilePath, _pathField)
					       })
			       };
		}

		private static DdxModel CreateModel()
		{
			return new TestModel("model");
		}

		private class TestModel : DdxModel
		{
			public TestModel(string name) : base(name) { }

			public override string QualifyModelElementName(string modelElementName)
			{
				return modelElementName;
			}

			public override string TranslateToModelElementName(string masterDatabaseDatasetName)
			{
				return masterDatabaseDatasetName;
			}

			protected override void CheckAssignSpecialDatasetCore(Dataset dataset) { }
		}

		#endregion
	}
}
