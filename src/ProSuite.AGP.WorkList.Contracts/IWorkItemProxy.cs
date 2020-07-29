using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItemProxy<T> where T : IWorkItem
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
