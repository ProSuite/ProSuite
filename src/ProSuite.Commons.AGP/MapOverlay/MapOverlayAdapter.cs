using System;
using System.Windows;
using System.Windows.Input;

namespace ProSuite.Commons.AGP.MapOverlay
{
	public class MapOverlayAdapter
	{
		private IProSuiteWindow _window;

		public MapOverlayAdapter(IProSuiteWindow window)
		{
			_window = window;
			_window.WindowStartupLocation = WindowStartupLocation.Manual;
			// window size to fit its content
			_window.SizeToContent = SizeToContent.WidthAndHeight;
			_window.ShowInTaskbar = false;

			_window.Closed += OnWindowClosed;
		}

		public void SetViewModel(IMapOverlayViewModel vm)
		{
			_window.DataContext = vm;

			var escapeBinding =
				new KeyBinding(vm.EscapeCommand, Key.Escape, ModifierKeys.None)
				{
					CommandParameter = _window
				};

			var enterBinding =
				new KeyBinding(vm.OkCommand, Key.Enter, ModifierKeys.None)
				{
					CommandParameter = _window
				};

			InputBindingCollection bindings = _window.GetInputBindings();

			bindings.Add(escapeBinding);
			bindings.Add(enterBinding);
		}

		private void OnWindowClosed(object sender, EventArgs e)
		{
			_window.Closed -= OnWindowClosed;
			_window = null;
		}
	}
}
