using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Test
{
	public class MemoryQueryWorkList : Domain.WorkList
	{
		public MemoryQueryWorkList(IWorkItemRepository repository, string name) :
			base(repository, name) { }
	}
}
