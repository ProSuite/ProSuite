using System.IO;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.Commons.AO.Test.Geodatabase.GdbSchema
{
	[TestFixture]
	public class GdbWorkspaceTest
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
		public void CanCompareWithFgdb()
		{
			string fgdbName = "GdbWorkspaceTest";

			IWorkspace realWorkspace = (IWorkspace) CreateTestWorkspace(fgdbName);

			GdbWorkspace gdbWorkspace = GdbWorkspace.CreateFromFgdb(realWorkspace);

			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(gdbWorkspace, realWorkspace));
			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(realWorkspace, gdbWorkspace));

			Assert.IsTrue(WorkspaceUtils.IsSameWorkspace(gdbWorkspace, realWorkspace,
			                                             WorkspaceComparison.AnyUserSameVersion));
			Assert.IsTrue(WorkspaceUtils.IsSameWorkspace(realWorkspace, gdbWorkspace,
			                                             WorkspaceComparison.AnyUserSameVersion));
		}

		[Test]
		public void CanCompareWithSdeWorkspaceDefault()
		{
			var realWorkspace = TestUtils.OpenSDEWorkspaceOracle();
			var versionedWorkspace = realWorkspace as IVersionedWorkspace;
			Assert.NotNull(versionedWorkspace);

			GdbWorkspace gdbWorkspace = GdbWorkspace.CreateFromSdeWorkspace(versionedWorkspace);

			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(gdbWorkspace, realWorkspace));
			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(realWorkspace, gdbWorkspace));

			Assert.IsTrue(WorkspaceUtils.IsSameWorkspace(gdbWorkspace, realWorkspace,
			                                             WorkspaceComparison.AnyUserSameVersion));
			Assert.IsTrue(WorkspaceUtils.IsSameWorkspace(realWorkspace, gdbWorkspace,
			                                             WorkspaceComparison.AnyUserSameVersion));
		}

		[Test]
		public void CanCompareWithSdeWorkspaceNonDefault()
		{
			IWorkspace defaultVersion = TestUtils.OpenSDEWorkspaceOracle();

			IWorkspace childVersion = null;
			foreach (IVersionInfo childVersionInfo in WorkspaceUtils.GetChildVersionInfos(
				(IVersion) defaultVersion))
			{
				childVersion =
					WorkspaceUtils.OpenWorkspaceVersion(defaultVersion,
					                                    childVersionInfo.VersionName);
			}

			if (childVersion == null)
			{
				// No child exists
				return;
			}

			var versionedWorkspace = childVersion as IVersionedWorkspace;
			Assert.NotNull(versionedWorkspace);

			GdbWorkspace gdbWorkspace = GdbWorkspace.CreateFromSdeWorkspace(versionedWorkspace);

			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(gdbWorkspace, defaultVersion));
			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(defaultVersion, gdbWorkspace));

			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(gdbWorkspace, childVersion));
			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(childVersion, gdbWorkspace));

			Assert.IsFalse(WorkspaceUtils.IsSameWorkspace(gdbWorkspace, defaultVersion,
			                                              WorkspaceComparison.AnyUserSameVersion));
			Assert.IsFalse(WorkspaceUtils.IsSameWorkspace(defaultVersion, gdbWorkspace,
			                                              WorkspaceComparison.AnyUserSameVersion));

			Assert.IsTrue(WorkspaceUtils.IsSameWorkspace(gdbWorkspace, childVersion,
			                                             WorkspaceComparison.AnyUserSameVersion));
			Assert.IsTrue(WorkspaceUtils.IsSameWorkspace(childVersion, gdbWorkspace,
			                                             WorkspaceComparison.AnyUserSameVersion));
		}

		private static IFeatureWorkspace CreateTestWorkspace(string fgdbName)
		{
			string dir = Path.GetTempPath();

			string mdb = Path.Combine(dir, fgdbName) + ".gdb";

			if (Directory.Exists(mdb))
			{
				Directory.Delete(mdb, true);
			}

			IWorkspaceName wsName = WorkspaceUtils.CreateFileGdbWorkspace(dir, fgdbName);
			return (IFeatureWorkspace) ((IName) wsName).Open();
		}
	}
}
