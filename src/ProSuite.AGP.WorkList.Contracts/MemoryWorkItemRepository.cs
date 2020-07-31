using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.WorkList.Contracts
{
	public class MemoryWorkItemRepository : IWorkItemRepository
	{
		public int GetCount(QueryFilter filter = null)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<PluginField> GetFields(IEnumerable<string> fieldNames = null)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter, bool recycle)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IWorkItem> GetAll()
		{
			throw new NotImplementedException();
		}
	}
}
