using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.Test;
using NUnit.Framework;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaSpatialReferenceTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _workspace;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_workspace = TestWorkspaceUtils.CreateTestFgdbWorkspace(GetType().Name);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		// TODO
	}
}
