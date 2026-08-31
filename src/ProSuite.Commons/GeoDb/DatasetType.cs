namespace ProSuite.Commons.GeoDb
{
	/// <summary>
	/// Dataset type using values corresponding to the Esri dataset types, except for Unknown = 0.
	/// </summary>
	public enum DatasetType
	{
		Null = 0,
		Any = 1,
		Table = 10,
		FeatureClass = 5,
		Topology = 8,
		Raster = 12,
		RasterMosaic = 29,
		Terrain = 20,
		Multipatch = 30,

		/// <summary>
		/// A LAS point cloud (Esri: esriDTLasDataset).
		/// </summary>
		/// <remarks>
		/// Esri's id for a LAS dataset is 30, which this enum has been using for the non-Esri
		/// <see cref="Multipatch"/> member since before point clouds were modelled. Rather than
		/// renumber a member that other systems may already have received, point clouds get a
		/// value well outside the Esri range; map it explicitly when converting to or from
		/// esriDatasetType.
		/// </remarks>
		PointCloud = 130
	}
}
