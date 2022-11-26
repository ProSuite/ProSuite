namespace ProSuite.Commons.AO.Licensing
{
	public interface IArcGISLicense
	{
		bool Initialize(bool includeAnalyst3d = false);

		void Release();
	}
}
