using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Picker;

namespace ProSuite.AGP.Editing.PickerUI
{
	public partial class PickerWindow : IDisposable, ICloseable
	{
		private readonly PickerViewModel _viewModel;
		private readonly IToolMouseEventsAware _mouseEvents;

		public PickerWindow(PickerViewModel viewModel, IToolMouseEventsAware mouseEvents)
		{
			InitializeComponent();

			_viewModel = viewModel;
			_mouseEvents = mouseEvents;

			DataContext = viewModel;

			WireEvents();
		}

		private void WireEvents()
		{
			PreviewKeyDownEventHandler += OnPreviewKeyDown;

			_mouseEvents.MouseDown += MouseEvents_MouseDown;
		}

		private void UnwireEvents()
		{
			PreviewKeyDownEventHandler -= OnPreviewKeyDown;

			_mouseEvents.MouseDown -= MouseEvents_MouseDown;
		}

		private void MouseEvents_MouseDown(object sender, MouseButtonEventArgs e)
		{
			_viewModel.OnMouseDown(this, e);
		}

		public KeyEventHandler PreviewKeyDownEventHandler { get; private set; }

		private void OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			_viewModel.OnPreviewKeyDown(this, e);
		}

		public Task<IPickableItem> Task => _viewModel.Task;

		public void Dispose()
		{
			UnwireEvents();

			_viewModel.Dispose();
		}
	}
}
