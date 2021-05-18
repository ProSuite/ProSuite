namespace ProSuite.Commons.AO.Licensing
{
	/// <summary>
	/// Esri products.
	/// Make sure the code corresponds with the respective <see cref="ESRI.ArcGIS.esriSystem.esriLicenseProductCode"/>
	/// NOTE: esriLicenseProductCodes changed at 10.1, their code did not.
	/// The default is 0
	/// </summary>
	public enum EsriProduct
	{
		None = 0,
		ArcView = 40,
		ArcEditor = 50,
		ArcInfo = 60,
		ArcGisServerEnterprise = 30
	}
}
