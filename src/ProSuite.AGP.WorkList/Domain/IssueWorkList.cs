using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain
{
	public class IssueWorkList : WorkList
	{
		public IssueWorkList(IWorkItemRepository repository, string name, string displayName = null) :
			base(repository, name, displayName) { }
	}
}
