using System.Threading.Tasks;
using System.Windows.Controls;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.Shared.AGP.WorkLists.WorkListUI;

public interface IWorkItemDetailsPanel
{
	Task SetCurrentItemAsync(IWorkItem workItem);

	UserControl CreateDetailsPanelView();
}
