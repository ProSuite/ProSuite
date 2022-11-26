namespace ProSuite.Commons.AO.Licensing
{
	public class BasicLicense : IArcGISLicense
	{
		private static readonly ArcGISLicenses _lic = new ArcGISLicenses();

		#region Implementation of IArcGISLicense

		public bool Initialize(bool includeAnalyst3d = false)
		{
			if (includeAnalyst3d)
			{
				_lic.Checkout(EsriProduct.ArcView, EsriExtension.ThreeDAnalyst);
			}
			else
			{
				_lic.Checkout(EsriProduct.ArcView);
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
