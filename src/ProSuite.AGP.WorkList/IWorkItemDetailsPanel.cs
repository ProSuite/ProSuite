using System.Threading.Tasks;
using System.Windows.Controls;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList;

public interface IWorkItemDetailsPanel
{
	Task SetCurrentItemAsync(IWorkItem workItem);

	UserControl CreateDetailsPanelView();

	void Unload();
}
