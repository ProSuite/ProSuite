using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Licensing
{
	public class ArcGISLicenses
	{
		private readonly LicenseInitializer _initializer = new LicenseInitializer();

		/// <summary>
		/// Attempts the check-out of a minimal license.
		/// </summary>
		public void Checkout(params EsriExtension[] extensions)
		{
			Checkout(EnvironmentUtils.Is64BitProcess
				         ? EsriProduct.ArcGisServerEnterprise
				         : EsriProduct.ArcView,
			         extensions);
		}

		public EsriProduct InitializedProduct { get; private set; }

		public void Checkout(EsriProduct product, params EsriExtension[] extensions)
		{
			Checkout(product, EsriProductFallback.TryHigherProduct, extensions);
		}

		public void Checkout(EsriProduct product, EsriProductFallback fallback,
		                     params EsriExtension[] extensions)
		{
			Checkout(product, fallback, GetExtensionCodes(extensions));
		}

		private void Checkout(EsriProduct product, EsriProductFallback fallback,
		                      params esriLicenseExtensionCode[] esriExtensionCodes)
		{
			esriLicenseProductCode[] productCodesArray =
				GetLicenseProductCodes(product, fallback);

			if (! _initializer.InitializeApplication(productCodesArray,
			                                         esriExtensionCodes))
			{
				string productString = esriExtensionCodes.Length > 0
					                       ? $"{product}, {StringUtils.Concatenate(esriExtensionCodes, ", ")}"
					                       : $"{product}";

				throw new InvalidOperationException($"Cannot check out license ({productString})");
			}

			// The _initializer is thread-affine and cannot be used from another thread.
			// Therefore, set the property here:
			InitializedProduct = (EsriProduct) _initializer.InitializedProduct;
		}

		public esriLicenseStatus CheckoutExtension(esriLicenseExtensionCode extensionCode)
		{
			return _initializer.CheckoutExtension(extensionCode);
		}

		public esriLicenseStatus CheckinExtension(esriLicenseExtensionCode extensionCode)
		{
			return _initializer.CheckinExtension(extensionCode);
		}

		public void Release()
		{
			_initializer.ShutdownApplication();
		}

		[NotNull]
		private static esriLicenseProductCode[] GetLicenseProductCodes(
			EsriProduct licenseProduct,
			EsriProductFallback fallback)
		{
			var productCodes = new List<esriLicenseProductCode>
			                   { GetLicenseProductCode(licenseProduct) };

			if (fallback == EsriProductFallback.TryHigherProduct)
			{
				switch (licenseProduct)
				{
					case EsriProduct.ArcView:
						productCodes.Add(GetLicenseProductCode(EsriProduct.ArcEditor));
						productCodes.Add(GetLicenseProductCode(EsriProduct.ArcInfo));
						break;

					case EsriProduct.ArcEditor:
						productCodes.Add(GetLicenseProductCode(EsriProduct.ArcInfo));
						break;
				}
			}

			return productCodes.ToArray();
		}

		private static esriLicenseProductCode GetLicenseProductCode(
			EsriProduct licenseProduct)
		{
			return (esriLicenseProductCode) licenseProduct;
		}

		[NotNull]
		private static esriLicenseExtensionCode[] GetExtensionCodes(
			[NotNull] IEnumerable<EsriExtension> extensions)
		{
			var extensionCodes = extensions.Select(
				                               extension => GetExtensionCode(extension))
			                               .ToList();

			var extensionCodesArray = new esriLicenseExtensionCode[extensionCodes.Count];
			extensionCodes.CopyTo(extensionCodesArray, 0);

			return extensionCodesArray;
		}

		private static esriLicenseExtensionCode GetExtensionCode(EsriExtension extension)
		{
			return (esriLicenseExtensionCode) extension;
		}
	}
}
