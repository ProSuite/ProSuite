namespace ProSuite.Commons.UI.WPF
{
	/// <summary>
	/// Closing a WPF window  (usually a dialog) from the ViewModel.
	/// The idea is to pass a reference to the window as the
	/// CommandParameter to the ViewModel's command, which then
	/// tests for this interface and can close the window/dialog.
	/// See "Close Window from ViewModel" on StackOverflow:
	/// https://stackoverflow.com/questions/16172462/close-window-from-viewmodel
	/// For another approach using a DependencyProperty, see
	/// http://blog.excastle.com/2010/07/25/mvvm-and-dialogresult-with-no-code-behind or
	/// https://web.archive.org/web/20180804160502/http://blog.excastle.com/2010/07/25/mvvm-and-dialogresult-with-no-code-behind
	/// </summary>
	public interface ICloseableWindow
	{
		void CloseWindow(bool? dialogResult = null);
	}
}
