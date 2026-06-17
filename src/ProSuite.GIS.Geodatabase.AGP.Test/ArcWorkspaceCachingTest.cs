using System;
using System.Threading;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.GIS.Geodatabase.API;
using Version = ArcGIS.Core.Data.Version;

namespace ProSuite.GIS.Geodatabase.AGP.Test
{
	/// <summary>
	/// Regression tests for the workspace caching in <see cref="ArcWorkspace"/>.
	///
	/// Background: <see cref="ArcTable.Workspace"/> relies on the per-handle cache
	/// (<c>ArcWorkspace.GetByHandle</c>) for thread-safe access: if the workspace for the
	/// table's datastore handle is cached, it is returned on any thread; otherwise
	/// <c>ArcTable.Workspace</c> must call <c>Table.GetDatastore()</c>, which may only be
	/// called on the thread the table was created on (CIM/MCT thread) and otherwise throws
	/// <c>CalledOnWrongThreadException</c>.
	///
	/// The defect (introduced together with the version-name cache
	/// <c>_workspacesByVersionName</c>): a single version (one version name) can be reached
	/// through several distinct PHYSICAL connections - each <see cref="Version.Connect"/>
	/// opens a new connection with its own <see cref="Datastore.Handle"/>. (Note: opening the
	/// same connection properties twice does NOT yield a new handle - the Pro SDK pools the
	/// physical connection, so the handle is keyed by the connection, not the managed object.)
	/// The version-name cache short-circuits <c>ArcWorkspace.Create</c> and returns the
	/// previously created workspace WITHOUT registering the requested geodatabase handle in
	/// the per-handle cache. As a result <c>GetByHandle(handle)</c> returns null forever for
	/// that handle and <c>ArcTable.Workspace</c> ends up calling <c>GetDatastore()</c> on the
	/// wrong thread.
	/// </summary>
	[TestFixture]
	public class ArcWorkspaceCachingTest
	{
		// Versioned Oracle test geodatabase, opened with operating system authentication.
		// Mirrors ProSuite.Commons.AO.Test.TestUtils.OpenOsaWorkspaceOracle() in spirit
		// (Oracle, operating system authentication).
		private const string _oracleInstance = "TOPGIST";

		// A versioned dataset that exists in the test geodatabase.
		private const string _datasetName = "TOPGIS_TLM.TLM_STRASSE";

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			// Helps core host apps (like unit tests) find dependencies like
			// CoreInterop.dll, freetype.dll, etc. in the proper place and version
			string installDir = ProRuntimeUtils.GetProInstallDir();
			ProRuntimeUtils.AddBinDirectoryToPath(installDir);

			CoreHostProxy.Initialize();
		}

		/// <summary>
		/// The core invariant: <c>ArcWorkspace.Create(geodatabase)</c> must return a workspace
		/// that wraps the requested geodatabase (i.e. has the requested handle), so that handle
		/// is retrievable from the per-handle cache. With the version-name cache this fails for
		/// the second physical connection to the same version: <c>Create</c> returns the
		/// workspace created for the first connection (whose handle differs), and the second
		/// handle is never cached.
		/// </summary>
		[Test]
		public void Create_returns_workspace_for_the_requested_geodatabase_handle()
		{
			using ArcGIS.Core.Data.Geodatabase gdb1 = OpenOsaVersionedWorkspaceOracle();

			Assert.True(gdb1.IsVersioningSupported(),
			            "Precondition: the test geodatabase must be versioned.");

			// A second, independent PHYSICAL connection to the same version (same version name,
			// distinct handle) - exactly what Version.Connect() produced in the original code
			// whenever a versioned workspace was created.
			using ArcGIS.Core.Data.Geodatabase gdb2 = ConnectToCurrentVersionAgain(gdb1);

			long handle1 = gdb1.Handle.ToInt64();
			long handle2 = gdb2.Handle.ToInt64();

			Assert.AreNotEqual(handle1, handle2,
			                   "Precondition: a second physical connection (Version.Connect) to " +
			                   "the same version must have a distinct datastore handle.");

			var workspace1 = ArcWorkspace.Create(gdb1);
			var workspace2 = ArcWorkspace.Create(gdb2);

			Assert.AreEqual(handle1, workspace1.Geodatabase.Handle.ToInt64(),
			                "Workspace for gdb1 must wrap gdb1.");

			// This is the actual regression: in the buggy version workspace2 is the
			// version-name-deduplicated workspace1 (wrapping gdb1, handle1), so handle2 of the
			// requesting geodatabase is never registered in the handle cache.
			Assert.AreEqual(handle2, workspace2.Geodatabase.Handle.ToInt64(),
			                "ArcWorkspace.Create must return a workspace for the requested " +
			                "geodatabase, not a different instance deduplicated by version name. " +
			                "Otherwise GetByHandle(tableDatastoreHandle) returns null and " +
			                "ArcTable.Workspace calls GetDatastore() on the wrong thread.");
		}

		/// <summary>
		/// Reproduces the production symptom: an <see cref="ArcTable"/> whose datastore belongs to
		/// one physical connection, while a workspace for the same version (different handle) was
		/// already cached, is accessed from a thread other than the one the Pro table was created
		/// on. This must not throw <c>CalledOnWrongThreadException</c> - it can only succeed if the
		/// table's datastore handle is in the per-handle cache.
		/// </summary>
		[Test]
		public void Workspace_of_table_is_accessible_from_another_thread()
		{
			// Simulate the workspace of (e.g.) a map layer that is already cached for this
			// version, obtained from one physical connection.
			using ArcGIS.Core.Data.Geodatabase layerGdb = OpenOsaVersionedWorkspaceOracle();

			Assert.True(layerGdb.IsVersioningSupported(),
			            "Precondition: the test geodatabase must be versioned.");

			ArcWorkspace.Create(layerGdb);

			// The table comes from a second, independent physical connection to the same version
			// (different handle), as Version.Connect() produces.
			using ArcGIS.Core.Data.Geodatabase tableGdb = ConnectToCurrentVersionAgain(layerGdb);

			Assert.AreNotEqual(layerGdb.Handle.ToInt64(), tableGdb.Handle.ToInt64(),
			                   "Precondition: the two physical connections must have distinct handles.");

			using Table proTable = tableGdb.OpenDataset<Table>(_datasetName);

			var arcTable = new ArcTable(proTable);

			// First access on the thread the Pro table was created on: this is what populates
			// the per-handle cache (via CreateArcWorkspace -> GetDatastore -> ArcWorkspace.Create).
			IWorkspace workspaceOnCreatingThread = arcTable.Workspace;
			Assert.NotNull(workspaceOnCreatingThread);

			// Now access from a different thread. With a correctly populated handle cache this
			// returns the cached workspace without touching the thread-affine Pro table.
			IWorkspace workspaceOnOtherThread = null;
			Exception caught = null;

			var thread = new Thread(() =>
			{
				try
				{
					workspaceOnOtherThread = arcTable.Workspace;
				}
				catch (Exception e)
				{
					caught = e;
				}
			});

			thread.Start();
			Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Worker thread timed out.");

			Assert.IsNull(caught,
			              "Accessing ArcTable.Workspace on a thread other than the one the Pro " +
			              "table was created on must not throw. It threw: " + caught);
			Assert.NotNull(workspaceOnOtherThread);
		}

		/// <summary>
		/// Opens the versioned Oracle test geodatabase using operating system authentication.
		/// </summary>
		private static ArcGIS.Core.Data.Geodatabase OpenOsaVersionedWorkspaceOracle()
		{
			var connectionProperties =
				new DatabaseConnectionProperties(EnterpriseDatabaseType.Oracle)
				{
					AuthenticationMode = AuthenticationMode.OSA,
					Instance = _oracleInstance
				};

			return (ArcGIS.Core.Data.Geodatabase) WorkspaceUtils.OpenDatastore(
				connectionProperties);
		}

		/// <summary>
		/// Opens a second, independent physical connection to the geodatabase's current version.
		/// The result has the same version name but a distinct <see cref="Datastore.Handle"/> -
		/// the situation the version-name cache mishandled.
		/// </summary>
		private static ArcGIS.Core.Data.Geodatabase ConnectToCurrentVersionAgain(
			ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			using VersionManager versionManager = geodatabase.GetVersionManager();
			using Version currentVersion = versionManager.GetCurrentVersion();

			// Version.Connect() opens a NEW physical connection to this version.
			return currentVersion.Connect();
		}
	}
}
