using System.Windows;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.Commons.AGP.LoggerUI
{
	public partial class LogMessageDetailsDialog : ICloseableWindow
	{
		public LogMessageDetailsDialog()
		{
			// TODO get MainWindow from proper thread...
			Owner = Application.Current.MainWindow;
			InitializeComponent();
		}

		public void CloseWindow(bool? dialogResult = null)
		{
			if (dialogResult.HasValue)
			{
				DialogResult = dialogResult;
			}

			Close();
		}
	}
}
