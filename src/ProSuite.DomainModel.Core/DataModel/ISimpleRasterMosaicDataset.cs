namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Higher-level marker interface for mosaic ddx datasets without dependency on Carto.
	/// TODO: Rename to IRasterMosaicDataset once the switch to non-layer based mosaic happened.
	/// </summary>
	public interface ISimpleRasterMosaicDataset : IDdxDataset { }
}
