using ArcGIS.Core.CIM;
using NUnit.Framework;
using ProSuite.GIS.Geodatabase.AGP.DataConnections;

namespace ProSuite.GIS.Geodatabase.AGP.Test
{
	[TestFixture]
	public class DataConnectionWorkspaceNameTest
	{
		private const string _connectionString =
			"INSTANCE=sde:oracle;DBCLIENT=oracle;VERSION=SDE.DEFAULT;AUTHENTICATION_MODE=OSA";

		[Test]
		public void CanGetVersionName()
		{
			Assert.AreEqual("SDE.DEFAULT",
			                DataConnectionWorkspaceName.GetVersionName(_connectionString));
			Assert.AreEqual("X.DAILY",
			                DataConnectionWorkspaceName.GetVersionName("A=1;version=X.DAILY"));
			Assert.IsNull(DataConnectionWorkspaceName.GetVersionName("INSTANCE=sde:oracle"));
			Assert.IsNull(DataConnectionWorkspaceName.GetVersionName(null));
		}

		[Test]
		public void CanChangeVersion()
		{
			var name = new DataConnectionWorkspaceName(_connectionString, WorkspaceFactory.SDE);

			name.ChangeVersion("U1.DAILY");

			Assert.AreEqual(
				"INSTANCE=sde:oracle;DBCLIENT=oracle;VERSION=U1.DAILY;AUTHENTICATION_MODE=OSA",
				name.ConnectionString);
		}
	}
}
