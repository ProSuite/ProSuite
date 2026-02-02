using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.GeoDb;
using IDatasetContainer = ProSuite.Commons.GeoDb.IDatasetContainer;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class GeoDbWorkspace : IDataStore
	{
		public IWorkspace BaseWorkspace { get; }

		public GeoDbWorkspace(IWorkspace workspace)
		{
			BaseWorkspace = workspace;
		}

		#region Implementation of IGdbDatasetContainer
		
		public T GetDataset<T>(string tableName) where T : class, IDatasetDef
		{
			// TODO: Other dataset types
			return ReadOnlyTableFactory.Create(DatasetUtils.OpenTable(BaseWorkspace, tableName)) as T;
		}

		public IEnumerable<IDatasetDef> GetDatasetDefs(DatasetType ofType = DatasetType.Any)
		{
			return DatasetUtils.GetObjectClasses(BaseWorkspace)
			                   .Select(oc => ReadOnlyTableFactory.Create((ITable) oc));
		}

		public bool Equals(IDatasetContainer otherWorkspace)
		{
			if (otherWorkspace is GeoDbWorkspace other)
			{
				return WorkspaceUtils.IsSameWorkspace(BaseWorkspace, other.BaseWorkspace,
				                                      WorkspaceComparison.Exact);
			}

			return false;
		}

		#endregion

		#region Implementation of IDataStore

		public IDatasetDef OpenDataset(string tableName)
		{
			IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)BaseWorkspace;

			return ReadOnlyTableFactory.Create(featureWorkspace.OpenTable(tableName));
		}

		public WorkspaceDbType DbType => WorkspaceUtils.GetWorkspaceDbType(BaseWorkspace);

		public string Path => BaseWorkspace.PathName;

		#endregion
	}
}
