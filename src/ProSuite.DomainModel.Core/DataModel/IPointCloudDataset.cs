namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Higher-level marker interface for point cloud ddx datasets without dependency on Carto.
	/// Drives <see cref="QA.TestParameterType.PointCloudDataset"/> validation and the dataset
	/// opener's dispatch, exactly as <see cref="IRasterMosaicDataset"/> does for surfaces.
	/// </summary>
	/// <remarks>
	/// Deliberately not shared with <see cref="IRasterMosaicDataset"/>: a point cloud is not a
	/// valid argument for a raster mosaic parameter, and one marker per test parameter type is
	/// what keeps the editor from offering it as one.
	/// </remarks>
	public interface IPointCloudDataset : IDdxDataset { }
}
