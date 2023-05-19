namespace ProSuite.Commons.Db
{
	/// <summary>
	/// Dataset type using values corresponding to the Esri dataset types.
	/// </summary>
	public enum DatasetType
	{
		Unknown = 1,
		Table = 10,
		FeatureClass = 5,
		Topology = 8,
		Raster = 12,
		RasterMosaic = 29,
		Terrain = 20
	}
}
