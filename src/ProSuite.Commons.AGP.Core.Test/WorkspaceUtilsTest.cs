using System;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;

namespace ProSuite.Commons.AGP.Core.Test
{
	[TestFixture]
	public class WorkspaceUtilsTest
	{
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
	}
}
