using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.DomainModel.AO.Workflow.WorkspaceFilters
{
	public class WorkspaceDbTypeMatchCriterion : IWorkspaceMatchCriterion
	{
		private readonly List<WorkspaceDbTypeInfo> _dbTypeInfos;

		public WorkspaceDbTypeMatchCriterion(
			[NotNull] IEnumerable<WorkspaceDbTypeInfo> dbTypes)
		{
			Assert.ArgumentNotNull(dbTypes, nameof(dbTypes));

			_dbTypeInfos = new List<WorkspaceDbTypeInfo>(dbTypes);
		}

		public bool IsSatisfied(IWorkspace workspace, out string reason)
		{
			foreach (WorkspaceDbType dbType in GetWorkspaceDbTypes(workspace))
			{
				WorkspaceDbTypeInfo dbTypeInfo = GetWorkspaceDbTypeInfo(dbType);
				if (dbTypeInfo == null)
				{
					continue;
				}

				reason = string.Format("Workspace has matching database type: {0}",
				                       dbTypeInfo.Name);
				return true;
			}

			reason = string.Format(
				"Workspace does not match the defined database type(s): {0}",
				StringUtils.Concatenate(
					_dbTypeInfos.Select(info => info.Name), ","));
			return false;
		}

		[CanBeNull]
		private WorkspaceDbTypeInfo GetWorkspaceDbTypeInfo(WorkspaceDbType dbType)
		{
			return _dbTypeInfos.FirstOrDefault(dbTypeInfo => dbTypeInfo.DBType == dbType);
		}

		[NotNull]
		private static IEnumerable<WorkspaceDbType> GetWorkspaceDbTypes(
			[NotNull] IWorkspace workspace)
		{
			switch (workspace.Type)
			{
				case esriWorkspaceType.esriFileSystemWorkspace:
					break;

				case esriWorkspaceType.esriLocalDatabaseWorkspace:
					if (WorkspaceUtils.IsFileGeodatabase(workspace))
					{
						yield return WorkspaceDbType.FileGeodatabase;
					}
					else if (WorkspaceUtils.IsPersonalGeodatabase(workspace))
					{
						yield return WorkspaceDbType.PersonalGeodatabase;
					}
					else if (WorkspaceUtils.IsMobileGeodatabase(workspace))
					{
						yield return WorkspaceDbType.MobileGeodatabase;
					}

					break;

				case esriWorkspaceType.esriRemoteDatabaseWorkspace:
					yield return WorkspaceDbType.ArcSDE;

					var connectionInfo = workspace as IDatabaseConnectionInfo2;
					if (connectionInfo != null)
					{
						switch (connectionInfo.ConnectionDBMS)
						{
							case esriConnectionDBMS.esriDBMS_Unknown:
								break;

							case esriConnectionDBMS.esriDBMS_Oracle:
								yield return WorkspaceDbType.ArcSDEOracle;
								break;

							case esriConnectionDBMS.esriDBMS_Informix:
								yield return WorkspaceDbType.ArcSDEInformix;
								break;

							case esriConnectionDBMS.esriDBMS_SQLServer:
								yield return WorkspaceDbType.ArcSDESqlServer;
								break;

							case esriConnectionDBMS.esriDBMS_DB2:
								yield return WorkspaceDbType.ArcSDEDB2;
								break;

							case esriConnectionDBMS.esriDBMS_PostgreSQL:
								yield return WorkspaceDbType.ArcSDEPostgreSQL;
								break;

							default:
								throw new ArgumentOutOfRangeException();
						}
					}

					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
