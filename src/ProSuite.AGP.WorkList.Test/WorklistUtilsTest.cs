using System;
using System.Threading;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Testing;

namespace ProSuite.AGP.WorkList.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class WorklistUtilsTest
	{
		[SetUp]
		public void SetUp() { }

		[TearDown]
		public void TearDown() { }

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

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

			IWorkList worklist = WorkListUtils.Create(definition, displayName);
			Assert.NotNull(worklist);

			Assert.AreEqual(0, worklist.Count());
		}

		[Test]
		public void Can_skip_work_item_workspace_because_of_invalid_connectionString()
		{
			string path =
				TestDataPreparer.FromDirectory()
				                .GetPath("work_list_definition_buggy_connectionString.swl");

			XmlWorkListDefinition definition = XmlWorkItemStateRepository.Import(path);

			IWorkItemRepository repository = WorkListUtils.CreateWorkItemRepository(definition);
			Assert.NotNull(repository);

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
