using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test.TestData
{
	/// <summary>
	/// Minimal <see cref="IRasterCatalogDataset"/> test double: a polygon vector dataset that acts
	/// as a raster catalog over a separate catalog feature class, mimicking an elevation raster
	/// dataset. Used to exercise <see cref="DomainModel.AO.DataModel.ModelElementUtils.CreateRasterCatalogMosaic"/>.
	/// </summary>
	public class TestRasterCatalogDataset : ModelVectorDataset, IRasterCatalogDataset
	{
		public TestRasterCatalogDataset([NotNull] string name,
		                                [NotNull] IVectorDataset catalogDataset,
		                                [NotNull] string filePathFieldName,
		                                [CanBeNull] string zOrderFieldName = null,
		                                bool zOrderDescending = false,
		                                [CanBeNull] IVectorDataset boundaryDataset = null,
		                                [CanBeNull] string cellSizeFieldName = null)
			: base(name)
		{
			Assert.ArgumentNotNull(catalogDataset, nameof(catalogDataset));
			Assert.ArgumentNotNullOrEmpty(filePathFieldName, nameof(filePathFieldName));

			CatalogDataset = catalogDataset;
			FilePathFieldName = filePathFieldName;
			ZOrderFieldName = zOrderFieldName;
			ZOrderDescending = zOrderDescending;
			BoundaryDataset = boundaryDataset;
			CellSizeFieldName = cellSizeFieldName;
		}

		public IVectorDataset CatalogDataset { get; }

		public string FilePathFieldName { get; }

		public IVectorDataset BoundaryDataset { get; }

		public string ZOrderFieldName { get; }

		public bool ZOrderDescending { get; }

		public string CellSizeFieldName { get; }
	}
}
