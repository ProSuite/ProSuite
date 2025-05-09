#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Test.Surface
{
	[TestFixture]
	public class SimpleRasterSurfaceTest
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

			_msg.IsVerboseDebugEnabled = true;

			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.NoContainer)]
		public void CanGetZFromSimpleRasterMosaic()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			IMosaicDataset mosaicDataset = MosaicUtils.OpenMosaicDataset(workspace,
				"TOPGIS_TLM.TLM_DTM_MOSAIC");

			var simpleRasterMosaic = new SimpleRasterMosaic(mosaicDataset);
			var simpleRasterSurface = new SimpleRasterSurface(simpleRasterMosaic);

			double resolution = simpleRasterMosaic.GetCellSize();
			Assert.AreEqual(0.5, resolution);

			double halfResolution = resolution / 2;

			// Start at exact pixel center:
			double x = 2690020 + halfResolution;
			double y = 1254010 + halfResolution;

			double z00 = simpleRasterSurface.GetZ(x, y);

			Assert.IsTrue(z00 > 440 && z00 < 450);

			Console.WriteLine("Z value at {0}, {1}: {2}", x, y, z00);

			// Get the adjacent pixel centers:
			double z01 = simpleRasterSurface.GetZ(x, y + resolution);
			double z10 = simpleRasterSurface.GetZ(x + resolution, y);
			double z11 = simpleRasterSurface.GetZ(x + resolution, y + resolution);

			double centerZ = simpleRasterSurface.GetZ(x + resolution / 2, y + resolution / 2);

			// Simple case for bilinear interpolation:
			double expected = (z00 + z01 + z10 + z11) / 4d;
			Assert.AreEqual(expected, centerZ);
		}
	}
}
