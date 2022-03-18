namespace ProSuite.AGP.Editing.PickerUI
{
	/// <summary>
	/// Interaction logic for PickerWindow.xaml
	/// </summary>
	public partial class PickerWindow
	{
		public PickerWindow(PickerViewModel vm)
		{
			InitializeComponent();

			vm.CloseAction = Close;

			DataContext = vm;

			// The Deactivated event should be fired if the user clicks outside the window.
			// However, this sometimes does not work. The failing condition is:
			// - Debugger NOT attached
			// - The window is opened with ShowDialog() instead of Show()
			Deactivated += vm.OnWindowDeactivated;
			PreviewKeyDown += vm.OnPreviewKeyDown;
			Closing += vm.OnWindowClosing;
		}
	}
}
