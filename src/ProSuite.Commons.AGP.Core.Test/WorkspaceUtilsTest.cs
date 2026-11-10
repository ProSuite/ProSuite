using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Testing;
using AssertionException = ProSuite.Commons.Essentials.Assertions.AssertionException;
using Version = ArcGIS.Core.Data.Version;

namespace ProSuite.Commons.AGP.Core.Test
{
	[TestFixture]
	public class WorkspaceUtilsTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		[Apartment(ApartmentState.STA)]
		public void GetDatastorePathIsNullForMemoryGeodatabase()
		{
			// An in-memory geodatabase has no catalog path: the SDK's Datastore.GetPath()
			// returns null for it, which is what the [CanBeNull] annotation on
			// GetDatastorePath makes explicit to callers.
			using ArcGIS.Core.Data.Geodatabase memoryGdb =
				SchemaBuilder.CreateGeodatabase(
					new MemoryConnectionProperties("WorkspaceUtilsPathTest"));

			Assert.Null(WorkspaceUtils.GetDatastorePath(memoryGdb));

			// GetCatalogPath promises a path, so it fails rather than returning null
			Assert.Throws<AssertionException>(
				() => WorkspaceUtils.GetCatalogPath(memoryGdb));
		}

		[Test]
		[Apartment(ApartmentState.STA)]
		public void GetDatastorePathIsPathForFileGeodatabase()
		{
			// Counterpart to GetDatastorePathIsNullForMemoryGeodatabase: make sure the
			// method does report a path where there is one.
			string gdbPath =
				TestDataPreparer.ExtractZip("can_get_rows_in_list.gdb.zip").GetPath();

			using ArcGIS.Core.Data.Geodatabase fileGdb =
				WorkspaceUtils.OpenGeodatabase(gdbPath);

			Uri path = WorkspaceUtils.GetDatastorePath(fileGdb);

			Assert.NotNull(path);
			Assert.AreEqual(Path.GetFullPath(gdbPath), Path.GetFullPath(path.LocalPath));

			// GetCatalogPath returns the same path, as a local file system path
			Assert.AreEqual(path.LocalPath, WorkspaceUtils.GetCatalogPath(fileGdb));
		}

		[Test]
		public void TimeDatasetFinderTablesOsa()
		{
			const string connectionString =
				"instance=sde:oracle11g:TOPGIST;dbclient=oracle;db_connection_properties=TOPGIST;" +
				"project_instance=SDE;version=SDE.DEFAULT;authentication_mode=OSA";

			DatabaseConnectionProperties properties = WorkspaceUtils.GetConnectionProperties(connectionString);

			Console.WriteLine(WorkspaceUtils.ConnectionPropertiesToString(properties));

			ArcGIS.Core.Data.Geodatabase geodatabase = (ArcGIS.Core.Data.Geodatabase)WorkspaceUtils.OpenDatastore(properties);

			
			Stopwatch stopwatch = Stopwatch.StartNew();
			IReadOnlyList<TableDefinition> defintionList = geodatabase.GetDefinitions<TableDefinition>();

			stopwatch.Stop();
			Console.WriteLine($"Time to get definitions: {stopwatch.ElapsedMilliseconds} ms");
			Console.WriteLine($"Amount of defintions: {defintionList.Count}");

			foreach (TableDefinition tableDefinition in defintionList)
			{
				Console.WriteLine($"Name: {tableDefinition.GetName()}");
			}



		}

		[Test]
		public void TimeDatasetFinderFeatureclasesOsa()
		{
			const string connectionString =
				"instance=sde:oracle11g:TOPGIST;dbclient=oracle;db_connection_properties=TOPGIST;" +
				"project_instance=SDE;version=SDE.DEFAULT;authentication_mode=OSA";

			DatabaseConnectionProperties properties = WorkspaceUtils.GetConnectionProperties(connectionString);

			Console.WriteLine(WorkspaceUtils.ConnectionPropertiesToString(properties));

			ArcGIS.Core.Data.Geodatabase geodatabase = (ArcGIS.Core.Data.Geodatabase)WorkspaceUtils.OpenDatastore(properties);


			Stopwatch stopwatch = Stopwatch.StartNew();
			IReadOnlyList<FeatureClassDefinition> defintionList = geodatabase.GetDefinitions<FeatureClassDefinition>();

			stopwatch.Stop();
			Console.WriteLine($"Time to get definitions: {stopwatch.ElapsedMilliseconds} ms");
			Console.WriteLine($"Amount of defintions: {defintionList.Count}");

			foreach (FeatureClassDefinition featureClassDefinition in defintionList)
			{
				Console.WriteLine($"Name: {featureClassDefinition.GetName()}");
			}



		}

		[Test]
		public void CanConvertConnectionStringToPropertiesOracleOsa()
		{
			const string tnsName = "ORADB_NAME";

			string connectionString =
				$"instance=sde:oracle11g:{tnsName};dbclient=oracle;db_connection_properties={tnsName};project_instance=SDE;version=SDE.DEFAULT;authentication_mode=OSA";
			var properties = WorkspaceUtils.GetConnectionProperties(connectionString);

			Console.WriteLine(WorkspaceUtils.ConnectionPropertiesToString(properties));

			Assert.AreEqual(EnterpriseDatabaseType.Oracle, properties.DBMS);
			Assert.AreEqual(AuthenticationMode.OSA, properties.AuthenticationMode);
			Assert.AreEqual(tnsName, properties.Instance);
			Assert.AreEqual(string.Empty, properties.Database);
			Assert.AreEqual(string.Empty, properties.User);
			Assert.AreEqual("SDE.DEFAULT", properties.Version);
			Assert.AreEqual("SDE", properties.ProjectInstance);
		}

		[Test]
		public void CanConvertConnectionStrintToPropertiesOracleOsaLegacy()
		{
			// From a lyr-file pulled into Pro:
			string connectionString =
				"SERVER=TOPGIST;INSTANCE=sde:oracle$TOPGIST;DBCLIENT=oracle;" +
				"DB_CONNECTION_PROPERTIES=TOPGIST;VERSION=SDE.DEFAULT;AUTHENTICATION_MODE=OSA";

			var properties = WorkspaceUtils.GetConnectionProperties(connectionString);

			Console.WriteLine(WorkspaceUtils.ConnectionPropertiesToString(properties));

			Assert.AreEqual(EnterpriseDatabaseType.Oracle, properties.DBMS);
			Assert.AreEqual(AuthenticationMode.OSA, properties.AuthenticationMode);
			Assert.AreEqual("TOPGIST", properties.Instance);
			Assert.AreEqual(string.Empty, properties.Database);
			Assert.AreEqual(string.Empty, properties.User);
			Assert.AreEqual("SDE.DEFAULT", properties.Version);
		}

		[Test]
		public void CanConvertConnectionStringToPropertiesPostgresUserPw()
		{
			string connectionString =
				"ENCRYPTED_PASSWORD_UTF8=00022e686f6c5957696a4569797a594f33354f443852754f664d7242763270473351655a77615448614e6f773271673d2a00;" +
				"ENCRYPTED_PASSWORD=00022e685646664451506a564c79382b4b79562b6a365369316149752f694351664a4b737578624431436c62434a773d2a00;" +
				"INSTANCE=sde:postgresql:localhost;DBCLIENT=postgresql;" +
				"DB_CONNECTION_PROPERTIES=localhost;DATABASE=data_osm;" +
				"USER=osm;VERSION=sde.DEFAULT;AUTHENTICATION_MODE=DBMS";

			var properties = WorkspaceUtils.GetConnectionProperties(connectionString);

			Console.WriteLine(WorkspaceUtils.ConnectionPropertiesToString(properties));

			Assert.AreEqual(EnterpriseDatabaseType.PostgreSQL, properties.DBMS);
			Assert.AreEqual(AuthenticationMode.DBMS, properties.AuthenticationMode);
			Assert.AreEqual("localhost", properties.Instance);
			Assert.AreEqual("data_osm", properties.Database);
			Assert.AreEqual("osm", properties.User);
			Assert.AreEqual("sde.DEFAULT", properties.Version);
			Assert.AreEqual(string.Empty, properties.ProjectInstance);
		}

		[Test]
		public void CanConvertConnectionStringToPropertiesPostgresUserPwWithServerKeyword()
		{
			string connectionString =
				"ENCRYPTED_PASSWORD_UTF8=00022e6876704339396a386449396c674d494537513850704e78544b33447335644f774b627153615a3166473938553d2a00;" +
				"ENCRYPTED_PASSWORD=00022e687856674c45514f2b6f4d6268443148435a522f4f6a7a636e44674c6358656848654d482f423234624d75733d2a00;" +
				"SERVER=localhost;INSTANCE=sde:postgresql:localhost;DBCLIENT=postgresql;DB_CONNECTION_PROPERTIES=localhost;" +
				"DATABASE=data_osm;USER=osm;VERSION=sde.DEFAULT;AUTHENTICATION_MODE=DBMS";

			var properties = WorkspaceUtils.GetConnectionProperties(connectionString);

			Console.WriteLine(WorkspaceUtils.ConnectionPropertiesToString(properties));

			Assert.AreEqual(EnterpriseDatabaseType.PostgreSQL, properties.DBMS);
			Assert.AreEqual(AuthenticationMode.DBMS, properties.AuthenticationMode);
			Assert.AreEqual("localhost", properties.Instance);
			Assert.AreEqual("data_osm", properties.Database);
			Assert.AreEqual("osm", properties.User);
			Assert.AreEqual("sde.DEFAULT", properties.Version);
			Assert.AreEqual(string.Empty, properties.ProjectInstance);
		}

		[Test]
		public void CanCreateFeatureServiceConnectorWithVersion()
		{
			const string connectionString =
				"URL=https://portal.dirageosystems.ch/server/rest/services/NSW/FeatureServer;" +
				"VERSION=paede@dira.ay_test;VERSIONGUID={D98F1C56-FDE7-4D2A-B067-1357442FFA12}";

			Connector connector =
				WorkspaceUtils.CreateConnector(WorkspaceFactory.FeatureService, connectionString);

			var serviceConnection = (ServiceConnectionProperties) connector;

			Assert.AreEqual(
				"https://portal.dirageosystems.ch/server/rest/services/NSW/FeatureServer",
				serviceConnection.URL.AbsoluteUri);
			Assert.AreEqual("paede@dira.ay_test", serviceConnection.Version);

			Connector otherConnector =
				WorkspaceUtils.CreateConnector(WorkspaceFactory.FeatureService, connectionString);

			var datastoreName1 = new DatastoreName(connector);
			var datastoreName2 = new DatastoreName(otherConnector);

			Assert.AreEqual(datastoreName1, datastoreName2);
			Assert.IsTrue(datastoreName1.Equals(datastoreName2));
			Assert.AreEqual(datastoreName1.GetHashCode(), datastoreName2.GetHashCode());

			Assert.IsTrue(
				datastoreName1.References(WorkspaceFactory.FeatureService, connectionString));
		}

		[Test]
		public void CanGetFeatureServiceDisplayTextWithBranch()
		{
			const string url = "https://portal.dirageosystems.ch/server/rest/services/NSW/FeatureServer";

			Connector withBranch =
				WorkspaceUtils.CreateConnector(WorkspaceFactory.FeatureService,
				                               $"URL={url};VERSION=paede@dira.ay_test");
			Connector withoutBranch =
				WorkspaceUtils.CreateConnector(WorkspaceFactory.FeatureService, $"URL={url}");

			Assert.AreEqual($"Service connection {url} - paede@dira.ay_test",
			                WorkspaceUtils.GetDatastoreDisplayText(withBranch));
			Assert.AreEqual($"Service connection {url}",
			                WorkspaceUtils.GetDatastoreDisplayText(withoutBranch));
		}

		[Test]
		public void CanCompareFeatureServiceConnectorsInDifferentBranches()
		{
			const string url = "https://portal.dirageosystems.ch/server/rest/services/NSW/FeatureServer";
			const string otherUrl = "https://portal.dirageosystems.ch/server/rest/services/Other/FeatureServer";

			var datastoreName = new DatastoreName(
				WorkspaceUtils.CreateConnector(WorkspaceFactory.FeatureService,
				                               $"URL={url};VERSION=paede@dira.ay_test"));

			string otherBranch = $"URL={url};VERSION=sde.DEFAULT";
			string otherService = $"URL={otherUrl};VERSION=paede@dira.ay_test";

			Assert.IsFalse(datastoreName.References(WorkspaceFactory.FeatureService, otherBranch,
			                                        DatastoreComparison.Exact));
			Assert.IsFalse(datastoreName.References(WorkspaceFactory.FeatureService, otherBranch,
			                                        DatastoreComparison.AnyUserSameVersion));
			Assert.IsTrue(datastoreName.References(WorkspaceFactory.FeatureService, otherBranch,
			                                       DatastoreComparison.AnyUserAnyVersion));

			Assert.IsFalse(datastoreName.References(WorkspaceFactory.FeatureService, otherService,
			                                        DatastoreComparison.AnyUserAnyVersion));
		}

		[Test]
		[Apartment(ApartmentState.STA)]
		public void CanReferenceMobileGeodatabaseByConnectionString()
		{
			string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			var uri = new Uri(Path.Combine(directory, "project.geodatabase"));

			using ArcGIS.Core.Data.Geodatabase mobileGdb =
				SchemaBuilder.CreateGeodatabase(new MobileGeodatabaseConnectionPath(uri));

			// What a project workspace stores to find the datastore again
			Assert.IsTrue(WorkspaceUtils.TryGetWorkspaceFactory(mobileGdb.GetConnector(),
			                                                    out WorkspaceFactory factory));
			string connectionString = mobileGdb.GetConnectionString();
			Console.WriteLine($"{factory}: {connectionString}");

			Connector connector = WorkspaceUtils.CreateConnector(factory, connectionString);

			Assert.IsInstanceOf<MobileGeodatabaseConnectionPath>(connector);
			Assert.IsTrue(new DatastoreName(mobileGdb).References(factory, connectionString));
			Assert.IsTrue(new DatastoreName(mobileGdb).References(factory, uri.LocalPath));
		}

		/// <summary>
		/// A path-based geodatabase is referenced by three forms of connection string: the one
		/// of Datastore.GetConnectionString() (stored by project workspaces), the connector path
		/// as file URI (stored by work list definitions) and the plain path. Characters that
		/// separate key/value pairs in connection strings may also occur in folder names.
		/// ArcGIS Pro cannot create a mobile geodatabase in a folder whose name contains ';'.
		/// </summary>
		[Test]
		[Apartment(ApartmentState.STA)]
		[TestCase("plain", "x.gdb")]
		[TestCase("a=b", "x.gdb")]
		[TestCase("a;b", "x.gdb")]
		[TestCase("a=b;c", "x.gdb")]
		[TestCase("plain", "x.geodatabase")]
		[TestCase("a=b", "x.geodatabase")]
		public void CanReferencePathBasedGeodatabaseByStoredConnectionStrings(
			string folderName, string geodatabaseName)
		{
			string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			string directory = Path.Combine(root, folderName);
			Directory.CreateDirectory(directory);

			try
			{
				var uri = new Uri(Path.Combine(directory, geodatabaseName));

				using ArcGIS.Core.Data.Geodatabase geodatabase =
					geodatabaseName.EndsWith(".gdb")
						? SchemaBuilder.CreateGeodatabase(new FileGeodatabaseConnectionPath(uri))
						: SchemaBuilder.CreateGeodatabase(new MobileGeodatabaseConnectionPath(uri));

				Connector originalConnector = geodatabase.GetConnector();
				Assert.IsTrue(WorkspaceUtils.TryGetWorkspaceFactory(originalConnector,
					              out WorkspaceFactory factory));

				string proConnectionString = geodatabase.GetConnectionString();
				string workListConnectionString =
					WorkspaceUtils.GetConnectionString(originalConnector, proConnectionString);

				foreach (string connectionString in new[]
				         {
					         proConnectionString, workListConnectionString, uri.LocalPath
				         })
				{
					Connector connector = WorkspaceUtils.CreateConnector(factory, connectionString);

					Assert.AreEqual(originalConnector.GetType(), connector.GetType(),
					                connectionString);
					Assert.IsTrue(new DatastoreName(geodatabase).References(
						              factory, connectionString), connectionString);
				}
			}
			finally
			{
				try
				{
					Directory.Delete(root, true);
				}
				catch (IOException)
				{
					// The geodatabase may still be locked by ArcGIS Pro
				}
			}
		}

		[Test]
		[Apartment(ApartmentState.STA)]
		[TestCase("plain")]
		[TestCase("a=b")]
		[TestCase("a;b")]
		public void CanReferenceShapefileFolderByStoredConnectionString(string folderName)
		{
			string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			string directory = Path.Combine(root, folderName);
			Directory.CreateDirectory(directory);

			try
			{
				var originalConnector = new FileSystemConnectionPath(
					new Uri(directory), FileSystemDatastoreType.Shapefile);

				using var datastore = new FileSystemDatastore(originalConnector);

				AssertCanReferenceByStoredConnectionString(datastore);
			}
			finally
			{
				Directory.Delete(root, true);
			}
		}

		[Test]
		[Apartment(ApartmentState.STA)]
		public void CanReferenceMemoryGeodatabaseByStoredConnectionString()
		{
			using ArcGIS.Core.Data.Geodatabase memoryGdb =
				SchemaBuilder.CreateGeodatabase(
					new MemoryConnectionProperties("WorkspaceUtilsStoredConnectionTest"));

			AssertCanReferenceByStoredConnectionString(memoryGdb);
		}

		/// <summary>
		/// Checks that the connection string stored in work list definitions
		/// (<see cref="WorkspaceUtils.GetConnectionString"/>) can be read back by
		/// <see cref="WorkspaceUtils.CreateConnector"/>.
		/// </summary>
		private static void AssertCanReferenceByStoredConnectionString(Datastore datastore)
		{
			Connector originalConnector = datastore.GetConnector();
			Assert.IsTrue(WorkspaceUtils.TryGetWorkspaceFactory(originalConnector,
			                                                    out WorkspaceFactory factory));

			string workListConnectionString =
				WorkspaceUtils.GetConnectionString(originalConnector,
				                                   datastore.GetConnectionString());
			Console.WriteLine($"{factory}: {workListConnectionString}");

			Connector connector = WorkspaceUtils.CreateConnector(factory, workListConnectionString);

			Assert.AreEqual(originalConnector.GetType(), connector.GetType());
			Assert.IsTrue(new DatastoreName(datastore).References(
				              factory, workListConnectionString));
		}

		[Test]
		[TestCase(@"D:\Daten\x.geodatabase")]
		[TestCase(@"D:\Daten\Stand=2026\x.geodatabase")]
		[TestCase("file:///D:/Daten/Stand=2026/x.geodatabase")]
		public void CanCreateMobileGeodatabaseConnectorFromPath(string path)
		{
			Connector connector = WorkspaceUtils.CreateConnector(WorkspaceFactory.SQLite, path);

			var mobileConnection = (MobileGeodatabaseConnectionPath) connector;
			Assert.AreEqual(new Uri(path).LocalPath, mobileConnection.Path.LocalPath);
		}

		/// <summary>
		/// <see cref="WorkspaceFactory.SQLite"/> is only reached by mobile geodatabases: ArcGIS
		/// Pro returns <see cref="DatabaseConnectionProperties"/> as connector of a plain SQLite
		/// database or GeoPackage, even when it was opened with a
		/// <see cref="SQLiteConnectionPath"/>. If this test fails, ArcGIS Pro has changed and
		/// these databases would reach CreateMobileGeodatabaseConnector.
		/// </summary>
		[Test]
		[Apartment(ApartmentState.STA)]
		[TestCase("plain.sqlite")]
		[TestCase("package.gpkg")]
		public void PlainSQLiteDatabaseHasNoSQLiteWorkspaceFactory(string fileName)
		{
			string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);

			try
			{
				// SQLite treats an empty file as an empty database
				string path = Path.Combine(directory, fileName);
				File.WriteAllBytes(path, Array.Empty<byte>());

				using var database = new Database(new SQLiteConnectionPath(new Uri(path)));

				Connector connector = database.GetConnector();
				Console.WriteLine(database.GetConnectionString());

				Assert.IsInstanceOf<DatabaseConnectionProperties>(connector);
				Assert.IsTrue(WorkspaceUtils.TryGetWorkspaceFactory(connector,
				                                                    out WorkspaceFactory factory));
				Assert.AreEqual(WorkspaceFactory.SDE, factory);
			}
			finally
			{
				Directory.Delete(directory, true);
			}
		}

		[Test]
		public void CanCreateMobileGeodatabaseConnectorFromConnectionString()
		{
			const string path = @"D:\Data\project.geodatabase";

			// As Datastore.GetConnectionString() returns it for a mobile geodatabase
			const string connectionString =
				$"DATABASE=main;SERVER=D:;INSTANCE=sde:sqlite:{path};" +
				$"DB_CONNECTION_PROPERTIES={path};IS_GEODATABASE=true;" +
				"AUTHENTICATION_MODE=OSA;IS_NOSERVER=0";

			Connector connector =
				WorkspaceUtils.CreateConnector(WorkspaceFactory.SQLite, connectionString);

			var mobileConnection = (MobileGeodatabaseConnectionPath) connector;
			Assert.AreEqual(path, mobileConnection.Path.LocalPath);

			Assert.IsTrue(new DatastoreName(connector).References(WorkspaceFactory.SQLite, path));
		}

		[Test, Ignore("Requires oracle connection")]
		public void Can_create_version_tree_as_osa()
        {
			// todo
            string catalogPath = null;
            catalogPath = "get path to osa connection";
            ArcGIS.Core.Data.Geodatabase geodatabase = WorkspaceUtils.OpenGeodatabase(catalogPath);

            Version currentVersion = WorkspaceUtils.GetCurrentVersion(geodatabase);
            DatabaseConnectionProperties props = WorkspaceUtils.GetConnectionProperties(currentVersion);
            Assert.NotNull(props);

			Version first = WorkspaceUtils.CreateVersion(geodatabase, "first");
            Version second = WorkspaceUtils.CreateVersion(first, "second");
            Version third = WorkspaceUtils.CreateVersion(second, "third");

            string firstName = first.GetName();
            string secondName = second.GetName();
            string thirdName = third.GetName();
            Assert.True(WorkspaceUtils.ExistsVersion(geodatabase, firstName));
            Assert.True(WorkspaceUtils.ExistsVersion(geodatabase, secondName));
            Assert.True(WorkspaceUtils.ExistsVersion(geodatabase, thirdName));

            WorkspaceUtils.DeleteVersion(geodatabase, first.GetName(), true);

            Assert.False(WorkspaceUtils.ExistsVersion(geodatabase, firstName));
            Assert.False(WorkspaceUtils.ExistsVersion(geodatabase, secondName));
            Assert.False(WorkspaceUtils.ExistsVersion(geodatabase, thirdName));
		}

		[Test, Ignore("Requires oracle connection")]
		public void Can_create_version_tree_as_user()
		{
			// todo
			string catalogPath = null;
            catalogPath = "get path to osa connection";
			ArcGIS.Core.Data.Geodatabase geodatabase = WorkspaceUtils.OpenGeodatabase(catalogPath);

            Version currentVersion = WorkspaceUtils.GetCurrentVersion(geodatabase);
            DatabaseConnectionProperties props = WorkspaceUtils.GetConnectionProperties(currentVersion);
			Assert.NotNull(props);

            Version first = WorkspaceUtils.CreateVersion(geodatabase, "first");
			Version second = WorkspaceUtils.CreateVersion(first, "second");
			Version third = WorkspaceUtils.CreateVersion(second, "third");

            string firstName = first.GetName();
            string secondName = second.GetName();
            string thirdName = third.GetName();
            Assert.True(WorkspaceUtils.ExistsVersion(geodatabase, firstName));
            Assert.True(WorkspaceUtils.ExistsVersion(geodatabase, secondName));
            Assert.True(WorkspaceUtils.ExistsVersion(geodatabase, thirdName));

			WorkspaceUtils.DeleteVersion(geodatabase, first.GetName(), true);

            Assert.False(WorkspaceUtils.ExistsVersion(geodatabase, firstName));
            Assert.False(WorkspaceUtils.ExistsVersion(geodatabase, secondName));
            Assert.False(WorkspaceUtils.ExistsVersion(geodatabase, thirdName));
		}
    }
}
