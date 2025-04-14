using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Testing;
using ProSuite.DomainModel.Core;

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

			string displayName = WorkListUtils.GetName(path);

			//IWorkList worklist = WorkListUtils.Create(definition, displayName);
			//Assert.NotNull(worklist);

			//Assert.AreEqual(0, worklist.Count());
		}

		[Test]
		public void Can_skip_work_item_workspace_because_of_invalid_connectionString()
		{
			string path =
				TestDataPreparer.FromDirectory()
				                .GetPath("work_list_definition_buggy_connectionString.swl");

			XmlWorkListDefinition definition = XmlWorkItemStateRepository.Import(path);

			List<Table> tables = WorkListUtils.GetDistinctTables(
				definition.Workspaces, definition.Name,
				definition.Path, out NotificationCollection notifications);

			var descriptor = new ClassDescriptor(definition.TypeName, definition.AssemblyName);
			Type type = descriptor.GetInstanceType();

			string name = definition.Name;
			string filePath = definition.Path;
			int currentIndex = definition.CurrentIndex;

			IWorkItemStateRepository stateRepository =
				WorkListUtils.CreateItemStateRepository(filePath, name, type, currentIndex);

			//IWorkItemRepository workItemRepository =
			//	WorkListUtils.CreateWorkItemRepository(tables, type, stateRepository, definition);
			//Assert.NotNull(workItemRepository);

			// This tries to load ArcGIS.Desktop.Framework. Why does WorkList needs this?
			// Try to push work list further up, away from AGP Desktop.
			//Assert.AreEqual(2, repository.GetCount());
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
