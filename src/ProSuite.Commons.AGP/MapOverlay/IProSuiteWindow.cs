using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.Commons.AGP.MapOverlay
{
	public interface IProSuiteWindow
	{
		Window Owner { get; set; }

		WindowStartupLocation WindowStartupLocation { get; set; }
		SizeToContent SizeToContent { get; set; }

		double Left { get; set; }
		double Top { get; set; }
		double Width { get; set; }
		double Height { get; set; }
		Size RenderSize { get; set; }

		WindowPositioner Positioner { get; set; }

		bool ShowCloseButton { get; set; }

		T GetControlOfType<T>() where T : UserControl;

		void InitializeWindow<T>(T control, IMapOverlayViewModel vm, string title)
			where T : UserControl;

		void SetPositioner(WindowPositioner positioner, Point desiredPosition);

		void Show();

		void Close();

		event EventHandler Closed;
		event SizeChangedEventHandler SizeChanged;

		bool ShowInTaskbar { get; set; }

		InputBindingCollection GetInputBindings();

		object DataContext { get; set; }
	}
}
