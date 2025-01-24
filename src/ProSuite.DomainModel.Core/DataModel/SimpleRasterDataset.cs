using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.DataModel
{
	// TODO: Unify with the other SimpleRasterDataset class or delete.
	public class SimpleRasterDataset : Dataset, ISimpleRasterDataset
	{
		public SimpleRasterDataset(string name) : base(name) { }

		public LayerFile DefaultLayerFile { get; set; }

		public override DatasetType DatasetType => DatasetType.Raster;

		public override DatasetImplementationType ImplementationType =>
			DatasetImplementationType.Raster;
	}
}
