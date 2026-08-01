using System;
using System.Threading;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Testing;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorklistUtilsTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

			// Without adding the install dir to the PATH variable a weird exception occurs on startup:
			//System.DllNotFoundException : Unable to load DLL 'CoreInterop.dll' or one of its dependencies:
			//The specified module could not be found. (0x8007007E)
			ProRuntimeUtils.AddBinDirectoryToPath(ProRuntimeUtils.GetProInstallDir());
			CoreHostProxy.Initialize();
		}

		[Test]
		public void Can_create_worklist_with_SDE_workspace_from_definition_file()
		{
			string path =
				TestDataPreparer.FromDirectory()
				                .GetPath("work_list_definition_pointing_to_sde.swl");

			XmlWorkListDefinition definition = XmlWorkItemStateRepository.Import(path);

			string displayName = WorkListUtils.ParseName(path);

			//IWorkList worklist = WorkListUtils.Create(definition, displayName);
			//Assert.NotNull(worklist);

			//Assert.AreEqual(0, worklist.Count());
		}

		[Test]
		public void IsWorkListConnection_recognizes_worklist_plugin_layer()
		{
			const string file = @"D:\APRX\1300-2\WorkLists\1300-2_2026_06_26_075119.swl";

			var connection = new CIMStandardDataConnection
			                 {
				                 WorkspaceFactory = WorkspaceFactory.Custom,
				                 WorkspaceConnectionString =
					                 $"DATABASE={file};IDENTIFIER=ProSuite_WorkListDatasource"
			                 };

			Assert.IsTrue(WorkListUtils.IsWorkListConnection(connection, out string workListFile));
			Assert.AreEqual(file, workListFile);
		}

		[Test]
		public void IsWorkListConnection_ignores_sde_layer()
		{
			// An SDE connection string is a valid ADO key=value string but has no
			// IDENTIFIER key; the WorkspaceFactory filter also rejects it up front.
			var connection = new CIMStandardDataConnection
			                 {
				                 WorkspaceFactory = WorkspaceFactory.SDE,
				                 WorkspaceConnectionString =
					                 "INSTANCE=sde:oracle11g:TOPGISP;DBCLIENT=oracle;" +
					                 "DB_CONNECTION_PROPERTIES=TOPGISP;" +
					                 "VERSION=U80811699.DV_TLM_2026-12-31_1300-2_Aktualisierung;" +
					                 "AUTHENTICATION_MODE=OSA"
			                 };

			Assert.IsFalse(WorkListUtils.IsWorkListConnection(connection, out string workListFile));
			Assert.IsNull(workListFile);
		}

		[Test]
		public void IsWorkListConnection_does_not_throw_on_non_keyvalue_connection_string()
		{
			// Regression (GOTOP-1214): a Custom plugin datasource layer whose
			// WorkspaceConnectionString is a bare path (no key=value) made
			// DbConnectionStringBuilder throw "Format of the initialization
			// string does not conform to specification starting at index 0",
			// aborting the whole "Open Work List" operation.
			var connection = new CIMStandardDataConnection
			                 {
				                 WorkspaceFactory = WorkspaceFactory.Custom,
				                 WorkspaceConnectionString =
					                 @"D:\APRX\1300-2\WorkLists\1300-2_2026_06_26_075119.swl"
			                 };

			bool result = false;

			Assert.DoesNotThrow(() => result = WorkListUtils
				                          .IsWorkListConnection(connection, out _));

			Assert.IsFalse(result);
		}

		[Test]
		public void IsWorkListConnection_handles_empty_and_null_connection_string()
		{
			foreach (string connectionString in new[] { null, "", "   " })
			{
				var connection = new CIMStandardDataConnection
				                 {
					                 WorkspaceFactory = WorkspaceFactory.Custom,
					                 WorkspaceConnectionString = connectionString
				                 };

				Assert.IsFalse(
					WorkListUtils.IsWorkListConnection(connection, out string workListFile));
				Assert.IsNull(workListFile);
			}
		}

		[Test, Ignore("Learning test")]
		public void Can_get_path_from_FileGDB()
		{
			string path = @"C:\temp\agp projects\Default.gdb";
			var gdb = new Geodatabase(
				new FileGeodatabaseConnectionPath(new Uri(path, UriKind.Absolute)));

			Console.WriteLine(gdb.GetPath());
			Console.WriteLine(gdb.GetConnectionString());
		}
	}
}
