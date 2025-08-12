using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.Commons.AGP.MapOverlay
{
	partial class ProSuiteWindow : IProSuiteWindow
	{
		private readonly MapOverlayAdapter _adapter;
		public WindowPositioner Positioner { get; set; }

		public ProSuiteWindow()
		{
			InitializeComponent();

			//WindowStyle = WindowStyle.None;
			//AllowsTransparency = true;
			//Background = Brushes.Transparent;
			ShowTitleBar = true;
			ShowIconOnTitleBar = false;

			// Resize icon in title bar disappears
			ResizeMode = ResizeMode.NoResize;

			ShowHelpButton = false;
			ShowCloseButton = true;
			ShowMinButton = false;
			ShowMaxRestoreButton = false;
			SaveWindowPosition = false;

			_adapter = new MapOverlayAdapter(this);
		}

		public void InitializeWindow<T>(T control, IMapOverlayViewModel vm, string title)
			where T : UserControl
		{
			grid.Children.Add(control);
			Grid.SetRow(control, 1);

			_adapter.SetViewModel(vm);

			Title = title;
		}

		public InputBindingCollection GetInputBindings()
		{
			return InputBindings;
		}

		public void SetPositioner(WindowPositioner positioner, Point desiredPosition)
		{
			Positioner = positioner;
			Positioner?.SetWindow(this, desiredPosition);
		}

		public T GetControlOfType<T>() where T : UserControl
		{
			foreach (var child in grid.Children)
			{
				if (child.GetType().IsAssignableFrom(typeof(T)))
				{
					return child as T;
				}
			}

			return null;
		}
	}
}
