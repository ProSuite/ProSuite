using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public interface IWorkListObserver
	{
		void Show();

		void Set([NotNull] IWorkList worklist);

		bool CloseView();
	}
}
