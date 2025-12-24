using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts;

public interface IWorkItemProxy<T> where T : IWorkItem
{
	T GetItem(GdbRowIdentity identity);

	IEnumerable<object[]> GetRowValues(QueryFilter filter, bool recycle);

	Envelope GetExtent();

	void ProcessChanges(IList<GdbRowIdentity> creates,
	                    IList<GdbRowIdentity> modifies,
	                    IList<GdbRowIdentity> deletes);

	void Invalidate();
}
