using System;
using System.Threading.Tasks;
using ProSuite.AGP.Editing.Picker;

namespace ProSuite.AGP.Editing.PickerUI
{
	public partial class PickerWindow : IDisposable, ICloseable
	{
		private readonly IPickerViewModel _viewModel;

		public PickerWindow(IPickerViewModel viewModel)
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
