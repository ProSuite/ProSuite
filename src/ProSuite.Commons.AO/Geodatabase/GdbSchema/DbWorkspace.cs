using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Db;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class DbWorkspace : IDataStore
	{
		public IWorkspace BaseWorkspace { get; }

		public DbWorkspace(IWorkspace workspace)
		{
			BaseWorkspace = workspace;
		}

		#region Implementation of IGdbDatasetContainer

		public IDbTableSchema OpenTable(string tableName)
		{
			IFeatureWorkspace featureWorkspace = (IFeatureWorkspace) BaseWorkspace;

			return ReadOnlyTableFactory.Create(featureWorkspace.OpenTable(tableName));
		}

		public IEnumerable<IDbDataset> GetGdbDatasets()
		{
			return DatasetUtils.GetObjectClasses(BaseWorkspace)
			                   .Select(oc => ReadOnlyTableFactory.Create((ITable) oc));
		}

		public bool Equals(IDbDatasetContainer otherWorkspace)
		{
			if (otherWorkspace is DbWorkspace other)
			{
				return WorkspaceUtils.IsSameWorkspace(BaseWorkspace, other.BaseWorkspace,
				                                      WorkspaceComparison.Exact);
			}

			return false;
		}

		#endregion

		#region Implementation of IDataStore

		public WorkspaceDbType DbType => WorkspaceUtils.GetWorkspaceDbType(BaseWorkspace);

		public string Path => BaseWorkspace.PathName;

		#endregion
	}
}
