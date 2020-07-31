using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItemRepository
	{
		int GetCount(QueryFilter filter = null);

		IEnumerable<PluginField> GetFields(IEnumerable<string> fieldNames = null);

		IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true);
	}
}
