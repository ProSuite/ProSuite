using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.Test.QA
{
	[TestFixture]
	public class TestParameterDatasetProviderBaseTest
	{
		[Test]
		public void CanGetRasterCatalogDatasetForRasterMosaicParameter()
		{
			// A raster catalog is physically a polygon feature class - it must be offered for
			// a RasterMosaicDataset parameter nevertheless.
			var catalog = new TestRasterCatalogDataset("catalog");
			var polygons = new TestVectorDataset("polygons");

			IList<Dataset> datasets =
				GetDatasets(TestParameterType.RasterMosaicDataset, catalog, polygons);

			Assert.AreEqual(1, datasets.Count);
			Assert.AreSame(catalog, datasets[0]);
		}

		[Test]
		public void CanGetMosaicDatasetForRasterMosaicParameter()
		{
			var mosaic = new TestRasterMosaicDataset("mosaic");
			var polygons = new TestVectorDataset("polygons");

			IList<Dataset> datasets =
				GetDatasets(TestParameterType.RasterMosaicDataset, mosaic, polygons);

			Assert.AreEqual(1, datasets.Count);
			Assert.AreSame(mosaic, datasets[0]);
		}

		[Test]
		public void CanGetRasterCatalogDatasetForPolygonParameter()
		{
			// The catalog is dual-natured: it remains a polygon vector dataset.
			var catalog = new TestRasterCatalogDataset("catalog");
			var mosaic = new TestRasterMosaicDataset("mosaic");

			IList<Dataset> datasets =
				GetDatasets(TestParameterType.PolygonDataset, catalog, mosaic);

			Assert.AreEqual(1, datasets.Count);
			Assert.AreSame(catalog, datasets[0]);
		}

		[Test]
		public void CannotGetPlainVectorDatasetForRasterMosaicParameter()
		{
			var polygons = new TestVectorDataset("polygons");

			IList<Dataset> datasets =
				GetDatasets(TestParameterType.RasterMosaicDataset, polygons);

			Assert.AreEqual(0, datasets.Count);
		}

		private static IList<Dataset> GetDatasets(TestParameterType validTypes,
		                                          params Dataset[] datasets)
		{
			var provider = new TestProvider(new DatasetRepositoryStub(datasets));

			return provider.GetDatasets(validTypes, null).ToList();
		}

		#region Test doubles

		private class TestProvider : TestParameterDatasetProviderBase
		{
			public TestProvider(IDatasetRepository datasetRepository)
				: base(action => action(), datasetRepository) { }

			protected override bool IsSelectable(Dataset dataset)
			{
				return true;
			}
		}

		private class TestVectorDataset : VectorDataset
		{
			public TestVectorDataset(string name) : base(name)
			{
				GeometryType = new GeometryTypeShape("Polygon", ProSuiteGeometryType.Polygon);
			}
		}

		/// <summary>
		/// Mimics an elevation raster dataset: a polygon vector dataset that is also a raster
		/// catalog, i.e. a valid raster mosaic source.
		/// </summary>
		private class TestRasterCatalogDataset : TestVectorDataset, IRasterCatalogDataset
		{
			public TestRasterCatalogDataset(string name) : base(name) { }

			IVectorDataset IRasterCatalogDataset.CatalogDataset => this;

			string IRasterCatalogDataset.FilePathFieldName => "FILE_PATH";

			IVectorDataset IRasterCatalogDataset.BoundaryDataset => null;

			string IRasterCatalogDataset.ZOrderFieldName => null;

			bool IRasterCatalogDataset.ZOrderDescending => false;

			string IRasterCatalogDataset.CellSizeFieldName => null;
		}

		private class TestRasterMosaicDataset : RasterMosaicDataset
		{
			public TestRasterMosaicDataset(string name) : base(name)
			{
				GeometryType = new GeometryTypeRasterMosaic("Raster Mosaic");
			}
		}

		private class DatasetRepositoryStub : IDatasetRepository
		{
			private readonly IList<Dataset> _datasets;

			public DatasetRepositoryStub(IEnumerable<Dataset> datasets)
			{
				_datasets = datasets.ToList();
			}

			public IList<T> GetAll<T>() where T : Dataset
			{
				return _datasets.OfType<T>().ToList();
			}

			public IList<Dataset> GetAll()
			{
				return _datasets;
			}

			public IList<Dataset> Get(string name) => throw new NotImplementedException();

			public IList<Dataset> Get(string name, bool includeDeleted) =>
				throw new NotImplementedException();

			public Dataset GetByAbbreviation(DdxModel model, string abbreviation) =>
				throw new NotImplementedException();

			public IList<T> Get<T>(DdxModel model) where T : Dataset =>
				throw new NotImplementedException();

			public IList<Dataset> Get(DatasetCategory datasetCategory) =>
				throw new NotImplementedException();

			public IList<Dataset> Get(IList<int> idList) => throw new NotImplementedException();

			public Dataset Get(int id) => throw new NotImplementedException();

			public void Delete(Dataset entity) => throw new NotImplementedException();

			public void Refresh(Dataset entity) => throw new NotImplementedException();

			public void Save(Dataset entity) => throw new NotImplementedException();
		}

		#endregion
	}
}
