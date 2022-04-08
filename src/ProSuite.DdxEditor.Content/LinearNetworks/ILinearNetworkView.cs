using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	public interface ILinearNetworkView : IWrappedEntityControl<LinearNetwork>, IWin32Window
	{
		ILinearNetworkObserver Observer { get; set; }

		IEnumerable<LinearNetworkDataset> GetSelectedNetworkDatasets();

		IList<LinearNetworkDatasetTableRow> GetSelectedLinearNetworkDatasetTableRows();

		void BindToNetworkDatasets(
			SortableBindingList<LinearNetworkDatasetTableRow> networkDatasetTableRows);
	}
}