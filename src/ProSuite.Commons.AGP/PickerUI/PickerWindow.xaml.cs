using System;
using System.Threading.Tasks;
using ProSuite.Commons.AGP.Picker;

namespace ProSuite.Commons.AGP.PickerUI
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
