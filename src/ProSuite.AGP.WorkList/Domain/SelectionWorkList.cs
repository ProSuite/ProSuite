using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain
{
	public class SelectionWorkList : WorkList
	{
		public SelectionWorkList(IWorkItemRepository repository, string uniqueName, string displayName) :
			base(repository, uniqueName, displayName) { }

		protected override string GetDisplayNameCore()
		{
			return "Selection Work List"; 
		}
	}
}
