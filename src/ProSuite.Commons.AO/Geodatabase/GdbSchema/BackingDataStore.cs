using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Abstraction for the datasets contained in a <see cref="GdbWorkspace"/>
	/// in case the workspace is used for an actual dataset container.
	/// </summary>
	public abstract class BackingDataStore
	{
		public abstract void ExecuteSql(string sqlStatement);

		public abstract IEnumerable<IDataset> GetDatasets(esriDatasetType datasetType);

		public abstract ITable OpenTable(string name);

		public abstract ITable OpenQueryTable(string relationshipClassName);
	}
}
