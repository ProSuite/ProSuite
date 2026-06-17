using System;
using System.IO;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.GIS.Geodatabase.AGP.Test
{
	/// <summary>
	/// <see cref="ArcWorkspace.Exists"/> previously checked only <c>Directory.Exists</c>, so it
	/// wrongly reported false for file-based geodatabases (a mobile geodatabase is a single file
	/// on disk, not a directory).
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ArcWorkspaceExistsTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void Exists_returns_true_for_file_based_mobile_geodatabase()
		{
			string dir = Path.Combine(Path.GetTempPath(),
			                          "ArcWorkspaceExistsTest_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);

			string mobileGdbPath = Path.Combine(dir, "test.geodatabase");

			ArcGIS.Core.Data.Geodatabase gdb = SchemaBuilder.CreateGeodatabase(
				new MobileGeodatabaseConnectionPath(new Uri(mobileGdbPath, UriKind.Absolute)));

			try
			{
				Assert.IsTrue(File.Exists(mobileGdbPath),
				              "Precondition: a mobile geodatabase is a file (not a directory).");

				var workspace = (ArcWorkspace) ArcWorkspace.Create(gdb);

				Assert.IsTrue(workspace.Exists(),
				              "Exists() must return true for a file-based (mobile) geodatabase, " +
				              "which is a file on disk - not only for directory-based ones.");

				// Dispose through the workspace (also disposes the geodatabase, see finding 5).
				workspace.Dispose();
			}
			finally
			{
				try
				{
					if (Directory.Exists(dir))
					{
						Directory.Delete(dir, recursive: true);
					}
				}
				catch
				{
					// Best-effort temp cleanup.
				}
			}
		}
	}
}
