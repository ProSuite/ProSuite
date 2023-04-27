using System;
using System.Threading.Tasks;
using ProSuite.AGP.Editing.Picker;

namespace ProSuite.AGP.Editing.PickerUI
{
	public partial class PickerProWindow : IDisposable, ICloseable
	{
		private readonly PickerViewModel0 _viewModel;

		public PickerProWindow(PickerViewModel0 viewModel)
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
