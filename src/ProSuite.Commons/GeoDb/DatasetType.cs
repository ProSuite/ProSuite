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
		Multipatch = 30
	}
}
