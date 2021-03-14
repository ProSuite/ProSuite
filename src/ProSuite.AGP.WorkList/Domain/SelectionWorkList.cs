using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain
{
	public class SelectionWorkList : WorkList
	{
		public SelectionWorkList(IWorkItemRepository repository, string name) :
			base(repository, name) { }

		public override string DisplayName => "Selection Work List";
	}
}
