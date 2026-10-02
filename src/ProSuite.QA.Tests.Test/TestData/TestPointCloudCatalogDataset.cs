using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test.TestData
{
	/// <summary>
	/// Minimal <see cref="IPointCloudCatalogDataset"/> test double: a polygon vector dataset that
	/// acts as a point cloud catalog over a separate catalog feature class, mimicking an archive
	/// point cloud dataset. Used to exercise
	/// <see cref="DomainModel.AO.DataModel.ModelElementUtils.CreatePointCloudCatalog"/>.
	/// The mirror image of <see cref="TestRasterCatalogDataset"/>.
	/// </summary>
	public class TestPointCloudCatalogDataset : ModelVectorDataset, IPointCloudCatalogDataset
	{
		public TestPointCloudCatalogDataset([NotNull] string name,
		                                    [NotNull] IVectorDataset catalogDataset,
		                                    [NotNull] string filePathFieldName)
			: base(name)
		{
			Assert.ArgumentNotNull(catalogDataset, nameof(catalogDataset));
			Assert.ArgumentNotNullOrEmpty(filePathFieldName, nameof(filePathFieldName));

			CatalogDataset = catalogDataset;
			FilePathFieldName = filePathFieldName;
		}

		public IVectorDataset CatalogDataset { get; }

		public string FilePathFieldName { get; }
	}
}
