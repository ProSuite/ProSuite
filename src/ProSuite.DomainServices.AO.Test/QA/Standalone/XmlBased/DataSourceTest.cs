using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;

namespace EsriDE.ProSuite.Services.Test.QA.GP.XmlBased
{
	[TestFixture]
	public class DataSourceTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanCheckValidWorkspaceReference()
		{
			var dataSource = new DataSource("test", "test");

			Assert.IsFalse(dataSource.HasWorkspaceInformation);
			Assert.IsFalse(dataSource.ReferencesValidWorkspace);

			dataSource.WorkspaceAsText = @"DATABASE=C:\doesnotexist.gdb";

			Assert.IsTrue(dataSource.HasWorkspaceInformation);
			Assert.IsFalse(dataSource.ReferencesValidWorkspace);

			dataSource.WorkspaceAsText = GetWorkspaceCatalogPath();

			Assert.IsTrue(dataSource.HasWorkspaceInformation);
			Assert.IsTrue(dataSource.ReferencesValidWorkspace);
		}

		[Test]
		public void CanOpenFromCatalogPath()
		{
			string catalogPath = GetWorkspaceCatalogPath();

			IWorkspace workspace = WorkspaceUtils.OpenPgdbWorkspace(catalogPath);

			var dataSource = new DataSource("test", "test") {WorkspaceAsText = catalogPath};

			IWorkspace openedWorkspace = dataSource.OpenWorkspace();
			Assert.IsNotNull(openedWorkspace);

			Assert.AreEqual(WorkspaceUtils.GetConnectionString(workspace),
			                WorkspaceUtils.GetConnectionString(openedWorkspace));
			Assert.AreEqual(workspace, openedWorkspace);
		}

		[Test]
		public void CanOpenFromConnectionString()
		{
			string catalogPath = GetWorkspaceCatalogPath();

			IWorkspace workspace = WorkspaceUtils.OpenPgdbWorkspace(catalogPath);
			string connectionString = WorkspaceUtils.GetConnectionString(workspace);

			var dataSource = new DataSource("test", "test")
			                 {WorkspaceAsText = connectionString};

			IWorkspace openedWorkspace = dataSource.OpenWorkspace();
			Assert.IsNotNull(openedWorkspace);
			Assert.AreEqual(workspace, openedWorkspace);
		}

		[NotNull]
		private static string GetWorkspaceCatalogPath()
		{
			var locator = new TestDataLocator(@"..\..\EsriDE.ProSuite\src");

			return locator.GetPath("QATestData.mdb");
		}
	}
}
