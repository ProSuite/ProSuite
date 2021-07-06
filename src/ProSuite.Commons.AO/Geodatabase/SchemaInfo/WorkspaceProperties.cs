using System.ComponentModel;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	public class WorkspaceProperties
	{
		public WorkspaceProperties([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			WorkspaceDescription = workspace.WorkspaceFactory.WorkspaceDescription[false];
			const bool replacePassword = true;
			ConnectionString = WorkspaceUtils.GetConnectionString(workspace, replacePassword);

			var connectionInfo = workspace as IDatabaseConnectionInfo2;
			UserName = connectionInfo == null
				           ? "n/a"
				           : connectionInfo.ConnectedUser;
			Dbms = connectionInfo == null
				       ? "n/a"
				       : GetDbmsText(connectionInfo.ConnectionDBMS);
		}

		[DisplayName("Workspace Type")]
		[UsedImplicitly]
		public string WorkspaceDescription { get; private set; }

		[DisplayName("Connection String")]
		[UsedImplicitly]
		public string ConnectionString { get; private set; }

		[DisplayName("Database User Name")]
		[UsedImplicitly]
		public string UserName { get; private set; }

		[DisplayName("DBMS")]
		[UsedImplicitly]
		public string Dbms { get; private set; }

		public override string ToString()
		{
			return WorkspaceDescription;
		}

		[NotNull]
		private static string GetDbmsText(esriConnectionDBMS connectionDbms)
		{
			switch (connectionDbms)
			{
				case esriConnectionDBMS.esriDBMS_Unknown:
					return "Unknown";

				case esriConnectionDBMS.esriDBMS_Oracle:
					return "Oracle";

				case esriConnectionDBMS.esriDBMS_Informix:
					return "Informix";

				case esriConnectionDBMS.esriDBMS_SQLServer:
					return "SQLServer";

				case esriConnectionDBMS.esriDBMS_DB2:
					return "DB2";

				case esriConnectionDBMS.esriDBMS_PostgreSQL:
					return "PostgreSQL";

				default:
					return connectionDbms.ToString();
			}
		}
	}
}
