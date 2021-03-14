using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain
{
	public class IssueWorkList : WorkList
	{
		public IssueWorkList(IWorkItemRepository repository, string name) :
			base(repository, name) { }

		public override string DisplayName => "Issue Work List";
	}
}
