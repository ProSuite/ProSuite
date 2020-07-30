using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItemRepository
	{
		int GetCount(QueryFilter filter = null);

		IEnumerable<PluginField> GetFields(IEnumerable<string> fieldNames = null);

		IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(QueryFilter filter, bool recycle);

		IEnumerable<IWorkItem> GetAll();

		void Register(IObjectDataset dataset, DbStatusSchema statusSchema = null);
	}
}
