using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList
{
	public interface IWorkListObserver
	{
		void WorkListAdded(IWorkList workList);

		void WorkListRemoved(IWorkList workList);

		void WorkListModified(IWorkList workList);

		void Show(IWorkList workList);
	}
}
