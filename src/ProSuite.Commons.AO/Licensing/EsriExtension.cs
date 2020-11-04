namespace ProSuite.Commons.AO.Licensing
{
	/// <summary>
	/// Esri extensions.
	/// Make sure the code corresponds with the respective <see cref="ESRI.ArcGIS.esriSystem.esriLicenseExtensionCode"/>
	/// NOTE: esriLicenseExtensionCode changed at 10.1, their code did not
	/// </summary>
	public enum EsriExtension
	{
		GeoStatisticalAnalyst = 6,
		NetworkAnalyst = 8,
		ThreeDAnalyst = 9,
		SpatialAnalyst = 10,
		Publisher = 15,
		TrackingAnalyst = 32,
		ArcScan = 34,
		Schematics = 36,
		JobTrackingForArcGIS = 40,
		DataInteroperability = 45,
		ServerStandard = 55,
		ServerAdvanced = 56,
		ServerEnterprise = 57
	}
}
