using System;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;

namespace ProSuite.DomainServices.AO.Test.QA.Standalone.XmlBased
{
	[TestFixture]
	public class DataSourceTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
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

			if (EnvironmentUtils.Is64BitProcess)
			{
				// TODO: Move test data to different format
				return;
			}

			Assert.IsTrue(dataSource.HasWorkspaceInformation);
			Assert.IsTrue(dataSource.ReferencesValidWorkspace);
		}

		[Test]
		public void CanOpenFromCatalogPath()
		{
			string catalogPath = GetWorkspaceCatalogPath();

			IWorkspace workspace;
			try
			{
				workspace = WorkspaceUtils.OpenPgdbWorkspace(catalogPath);
			}
			catch (Exception)
			{
				// TODO: Move test data to different format
				if (EnvironmentUtils.Is64BitProcess)
				{
					Console.WriteLine("Expected exception: PGDB is not supported on x64");
					return;
				}

				throw;
			}

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

			IWorkspace workspace;
			try
			{
				workspace = WorkspaceUtils.OpenPgdbWorkspace(catalogPath);
			}
			catch (Exception)
			{
				// TODO: Move test data to different format
				if (EnvironmentUtils.Is64BitProcess)
				{
					Console.WriteLine("Expected exception: PGDB is not supported on x64");
					return;
				}

				throw;
			}

			string connectionString = WorkspaceUtils.GetConnectionString(workspace);

			var dataSource = new DataSource("test", "test")
			                 {WorkspaceAsText = connectionString};

			IWorkspace openedWorkspace = dataSource.OpenWorkspace();
			Assert.IsNotNull(openedWorkspace);
			Assert.AreEqual(workspace, openedWorkspace);
		}

		[Test]
		public void CanOpenFromConnectionStringSDE()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();
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
			var locator = TestDataLocator.Create("ProSuite", @"QA\TestData");

			return locator.GetPath("QATestData.mdb");
		}
	}
}
