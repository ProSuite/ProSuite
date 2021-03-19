using System;
using System.Reflection;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Test.Surface
{
	public class SimpleRasterSurfaceTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnittestLogging();

			_msg.IsVerboseDebugEnabled = true;

			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanGetZFromSimpleRasterMosaic()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			IMosaicDataset mosaicDataset = DatasetUtils.OpenMosaicDataset(workspace,
			                                                              "TOPGIS_TLM.TLM_DTM_MOSAIC");

			var simpleRasterMosaic = new SimpleRasterMosaic(mosaicDataset);
			var simpleRasterSurface = new SimpleRasterSurface(simpleRasterMosaic);

			const double x = 2690021;
			const double y = 1254011;

			double z = simpleRasterSurface.GetZ(x, y);

			Assert.IsTrue(z > 400);

			Console.WriteLine("Z value at {0}, {1}: {2}", x, y, z);
		}
	}
}
