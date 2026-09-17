using System.Threading.Tasks;
using System.Windows.Controls;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList;

public interface IWorkItemDetailsPanel
{
	Task SetCurrentItemAsync(IWorkItem workItem);

	UserControl CreateDetailsPanelView();

	bool CanSelectCurrentFeature(IWorkItem currentItem);

	Task SelectCurrentFeatureAsync();

	public string ToolTipSelectCurrentFeature { get; }
}
