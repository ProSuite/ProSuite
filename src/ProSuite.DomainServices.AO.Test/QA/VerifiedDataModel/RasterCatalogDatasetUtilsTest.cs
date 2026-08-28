using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.Exceptions;
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
	public class RasterCatalogDatasetUtilsTest
	{
		private const string _catalogName = "RAS_DTM_TILES";
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

			RasterCatalogDatasetUtils.ApplyFieldRoles(model, GetFilePathRole(_catalogName));

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

			RasterCatalogDatasetUtils.ApplyFieldRoles(model, GetFilePathRole(_catalogName));

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

			RasterCatalogDatasetUtils.ApplyFieldRoles(
				model, new Dictionary<string, IList<DatasetFieldRole>>());

			Assert.AreSame(harvested, model.GetDatasetByModelName(_catalogName));
		}

		[Test]
		public void Unharvested_dataset_is_not_an_error_here()
		{
			// Whether a referenced but unharvested dataset is fatal is decided by the regular
			// unknown-dataset handling, not here.
			DdxModel model = CreateModel();

			Assert.DoesNotThrow(
				() => RasterCatalogDatasetUtils.ApplyFieldRoles(
					model, GetFilePathRole(_catalogName)));
		}

		[Test]
		public void Non_vector_dataset_as_a_catalog_is_an_error()
		{
			DdxModel model = CreateModel();

			model.AddDataset(new VerifiedTableDataset(_catalogName));

			var exception = Assert.Throws<InvalidConfigurationException>(
				() => RasterCatalogDatasetUtils.ApplyFieldRoles(
					model, GetFilePathRole(_catalogName)));

			Assert.IsTrue(exception.Message.Contains(_catalogName), exception.Message);
		}

		[Test]
		public void Catalog_without_a_file_path_role_is_rejected()
		{
			// A catalog whose file path cannot be found is useless; fail where the cause is.
			var roles = new List<DatasetFieldRole>
			            {
				            new DatasetFieldRole(AttributeRole.ObjectID, "OBJECTID")
			            };

			Assert.Throws<System.ArgumentException>(
				() => new VerifiedRasterCatalogDataset(_catalogName, roles));
		}

		#region Test setup

		private static readonly GeometryTypeShape PolygonGeometryType =
			new GeometryTypeShape("Polygon", Commons.Geom.EsriShape.ProSuiteGeometryType.Polygon);

		private static IDictionary<string, IList<DatasetFieldRole>> GetFilePathRole(
			string datasetName)
		{
			return new Dictionary<string, IList<DatasetFieldRole>>
			       {
				       {
					       datasetName,
					       new List<DatasetFieldRole>
					       {
						       new DatasetFieldRole(AttributeRole.FilePath, _pathField)
					       }
				       }
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
