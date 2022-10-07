using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	public class LinearNetworkPresenter : SimpleEntityItemPresenter<LinearNetworkItem>,
	                                      ILinearNetworkObserver
	{
		#region Delegates

		public delegate IList<DatasetTableRow> GetSnapNetworkDatasetsToAdd(
			IWin32Window owner,
			params ColumnDescriptor[] columns);

		#endregion

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly GetSnapNetworkDatasetsToAdd _getNetworkDatasetsToAdd;

		private readonly SortableBindingList<LinearNetworkDatasetTableRow> _networkDatasetTableRows
			= new SortableBindingList<LinearNetworkDatasetTableRow>();

		private readonly ILinearNetworkView _view;

		public LinearNetworkPresenter(LinearNetworkItem item,
		                              ILinearNetworkView view,
		                              GetSnapNetworkDatasetsToAdd findDatasetsToAdd)
			: base(item)
		{
			Assert.ArgumentNotNull(findDatasetsToAdd, nameof(findDatasetsToAdd));

			_view = view;
			_getNetworkDatasetsToAdd = findDatasetsToAdd;
			_view.Observer = this;
		}

		private void RenderNetworkDatasets(LinearNetwork linearNetwork)
		{
			Assert.ArgumentNotNull(linearNetwork, nameof(linearNetwork));

			_networkDatasetTableRows.Clear();

			foreach (LinearNetworkDataset linearNetworkDataset in linearNetwork.NetworkDatasets)
			{
				_networkDatasetTableRows.Add(
					new LinearNetworkDatasetTableRow(linearNetworkDataset));
			}

			_view.BindToNetworkDatasets(_networkDatasetTableRows);
		}

		public void OnBoundTo(LinearNetwork linearNetwork)
		{
			RenderNetworkDatasets(linearNetwork);
		}

		public void TargetSelectionChanged() { }

		void ILinearNetworkObserver.AddTargetClicked()
		{
			LinearNetwork linearNetwork = Assert.NotNull(Item.GetEntity());

			IList<DatasetTableRow> selectedItems =
				_getNetworkDatasetsToAdd(_view,
				                         new ColumnDescriptor("Image", string.Empty),
				                         new ColumnDescriptor("AliasName", "Dataset"),
				                         new ColumnDescriptor("ModelName", "Model"),
				                         new ColumnDescriptor("DatasetCategory", "Category"));

			if (selectedItems == null)
			{
				return;
			}

			var anyAdded = false;
			foreach (DatasetTableRow selectedItem in selectedItems)
			{
				var selectedDataset = (VectorDataset) selectedItem.Entity;

				if (linearNetwork.NetworkDatasets.Any(d => d.Dataset.Id == selectedDataset.Id))
				{
					_msg.WarnFormat("The network already contains the dataset {0}",
					                selectedDataset.Name);
				}
				else
				{
					var networkDataset = new LinearNetworkDataset(selectedDataset);
					linearNetwork.AddNetworkDataset(networkDataset);

					_networkDatasetTableRows.Add(new LinearNetworkDatasetTableRow(networkDataset));

					anyAdded = true;
				}
			}

			if (anyAdded)
			{
				Item.NotifyChanged();
			}
		}

		void ILinearNetworkObserver.RemoveTargetClicked()
		{
			IList<LinearNetworkDatasetTableRow> selected =
				_view.GetSelectedLinearNetworkDatasetTableRows();

			LinearNetwork linearNetwork = Assert.NotNull(Item.GetEntity());

			foreach (LinearNetworkDatasetTableRow item in selected)
			{
				linearNetwork.RemoveNetworkDataset(item.TargetDataset);
				_networkDatasetTableRows.Remove(item);
			}

			Item.NotifyChanged();
		}
	}
}
