using ArcGIS.Core.Hosting;

namespace ProSuite.Commons.AGP.Hosting
{
	/// <summary>
	/// See the associated README file for rationale and usage.
	/// </summary>
	public static class CoreHostProxy
	{
		public static void Initialize(
			bool useServerLicense = false)
		{
			Host.LicenseProductCode productCode =
				useServerLicense
					? Host.LicenseProductCode.ArcGISServer
					: Host.LicenseProductCode.ArcGISPro;

			Host.Initialize(productCode);
		}
	}
}
