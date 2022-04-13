using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	public partial class LinearNetworkControl : UserControl, ILinearNetworkView
	{
		public LinearNetworkControl()
		{
			InitializeComponent();

			_gridHandler =
				new BoundDataGridHandler<LinearNetworkDatasetTableRow>(
					_dataGridViewLinearNetworkDatasets);

			_binder =
				new ScreenBinder<LinearNetwork>(
					new ErrorProviderValidationMonitor(_errorProvider));
		}

		private readonly ScreenBinder<LinearNetwork> _binder;

		private readonly BoundDataGridHandler<LinearNetworkDatasetTableRow> _gridHandler;

		private readonly Latch _latch = new Latch();

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public ILinearNetworkObserver Observer { get; set; }

		public void BindTo(LinearNetwork target)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<LinearNetworkDataset> GetSelectedNetworkDatasets()
		{
			throw new NotImplementedException();
		}

		public IList<LinearNetworkDatasetTableRow> GetSelectedLinearNetworkDatasetTableRows()
		{
			return _gridHandler.GetSelectedRows();
		}

		public void BindToNetworkDatasets(
			SortableBindingList<LinearNetworkDatasetTableRow> networkDatasetTableRows)
		{
			_latch.RunInsideLatch(
				delegate
				{
					_dataGridViewLinearNetworkDatasets.DataSource = networkDatasetTableRows;
				});
		}

		public void OnBindingTo(LinearNetwork entity) { }

		public void SetBinder(ScreenBinder<LinearNetwork> binder)
		{
			binder.Bind(m => m.Name)
			      .To(_textBoxName);

			binder.Bind(m => m.CustomTolerance).To(_updownCustomTolerance);

			binder.Bind(m => m.EnforceFlowDirection).To(_cbEnforceFd);

			binder.Bind(m => m.Description).To(_textBoxDescription);

			binder.OnChange = BinderChanged;
		}

		private static void Try(Action proc)
		{
			try
			{
				proc();
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
		}

		private void BinderChanged()
		{
			Observer?.NotifyChanged(_binder.IsDirty());
		}

		public void OnBoundTo(LinearNetwork entity)
		{
			Observer.OnBoundTo(entity);
		}

		private void _buttonAddNetworkDatasets_Click(object sender, EventArgs e)
		{
			Try(delegate { Observer?.AddTargetClicked(); });
		}

		private void _buttonRemoveNetworkDatasets_Click(object sender, EventArgs e)
		{
			Try(delegate { Observer?.RemoveTargetClicked(); });
		}

		private void _customTolerance_Click(object sender, EventArgs e) { }

		private void labelEnforceFd_Click(object sender, EventArgs e) { }

		private void _dataGridViewLinearNetworkDatasets_CellValueChanged(
			object sender, DataGridViewCellEventArgs e)
		{
			Try(delegate
			{
				if (e.RowIndex < 0 || e.ColumnIndex < 0)
				{
					return;
				}

				Observer?.NotifyChanged(true);
			});
		}

		private void _dataGridViewLinearNetworkDatasets_SelectionChanged(object sender, EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			Try(delegate { Observer?.TargetSelectionChanged(); });
		}

		private void _dataGridViewLinearNetworkDatasets_CellValidating(
			object sender, DataGridViewCellValidatingEventArgs e) { }
	}
}