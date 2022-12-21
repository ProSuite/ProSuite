using System;
using System.IO;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Test.TestSupport;

namespace ProSuite.Commons.AO.Test.Geodatabase.GdbSchema
{
	[TestFixture]
	public class GdbWorkspaceTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
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
		public void CanCompareWorkspacePaths()
		{
			string fgdbName = "GdbWorkspaceTest.CanCompareWorkspacePaths";

			IWorkspace realWorkspace = (IWorkspace) CreateTestWorkspace(fgdbName);
			Console.WriteLine(realWorkspace.PathName);

			string path = new Uri(realWorkspace.PathName).AbsoluteUri;
			IWorkspace mock = new WorkspaceMock(path);
			Console.WriteLine(mock.PathName);

			GdbWorkspace gdbWorkspace = GdbWorkspace.CreateFromFgdb(mock);

			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(realWorkspace, gdbWorkspace));
			Assert.IsTrue(WorkspaceUtils.IsSameDatabase(gdbWorkspace, realWorkspace));
		}

		[Test]
		[Category(TestCategory.Sde)]
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
		[Category(TestCategory.Sde)]
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
