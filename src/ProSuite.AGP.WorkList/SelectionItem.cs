using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList;

public class SelectionItem : WorkItem
{
	public SelectionItem(long uniqueTableId, GdbRowIdentity identity) : base(
		uniqueTableId, identity) { }
}
