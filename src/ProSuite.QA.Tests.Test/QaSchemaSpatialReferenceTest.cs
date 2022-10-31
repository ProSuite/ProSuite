using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Container.Test;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaSpatialReferenceTest
	{
		private IFeatureWorkspace _workspace;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_workspace = TestWorkspaceUtils.CreateTestFgdbWorkspace(GetType().Name);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		// TODO
	}
}
