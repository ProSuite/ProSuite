using System;
using System.Threading;
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
