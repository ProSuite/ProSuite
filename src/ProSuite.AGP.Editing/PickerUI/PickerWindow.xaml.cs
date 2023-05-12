using System;
using System.Threading.Tasks;
using ProSuite.AGP.Editing.Picker;

namespace ProSuite.AGP.Editing.PickerUI
{
	public partial class PickerWindow : IDisposable, ICloseable
	{
		private readonly PickerViewModel _viewModel;

		public PickerWindow(PickerViewModel viewModel)
		{
			InitializeComponent();

			_viewModel = viewModel;
			DataContext = viewModel;
		}

		public Task<IPickableItem> Task => _viewModel.Task;

		public void Dispose()
		{
			_viewModel.Dispose();
		}
	}
}
