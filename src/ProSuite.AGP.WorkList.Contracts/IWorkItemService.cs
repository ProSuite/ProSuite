using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AG.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItemService<T> where T : WorkItem
	{
		T GetItem(GdbRowReference reference);

		IEnumerable<object[]> GetRowValues(QueryFilter filter, bool recycle);

		Envelope GetExtent();

		void ProcessChanges(IList<GdbRowReference> creates,
		                    IList<GdbRowReference> modifies,
		                    IList<GdbRowReference> deletes);

		void Invalidate();
	}
}
