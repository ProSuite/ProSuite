using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class SimpleRasterDataset : Dataset, ISimpleRasterDataset
	{
		public SimpleRasterDataset(string name) : base(name) { }

		public LayerFile DefaultLayerFile { get; set; }

		public override DatasetType DatasetType => DatasetType.Raster;
	}
}
