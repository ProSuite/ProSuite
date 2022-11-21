namespace ProSuite.Commons.AO.Licensing
{
	public class ServerLicense : IArcGISLicense
	{
		private static readonly ArcGISLicenses _lic = new ArcGISLicenses();

		#region Implementation of IArcGISLicense

		public bool Initialize(bool includeAnalyst3d = false)
		{
			if (includeAnalyst3d)
			{
				_lic.Checkout(EsriProduct.ArcGisServerEnterprise, EsriExtension.ThreeDAnalyst);
			}
			else
			{
				_lic.Checkout(EsriProduct.ArcGisServerEnterprise);
			}

			return _lic.InitializedProduct != EsriProduct.None;
		}

		public void Release()
		{
			_lic.Release();
		}

		#endregion
	}
}
