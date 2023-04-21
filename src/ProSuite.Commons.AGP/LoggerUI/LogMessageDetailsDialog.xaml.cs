using System.Windows;

namespace ProSuite.Commons.AGP.LoggerUI
{
	public interface ICloseable
	{
		void CloseWindow(bool returnValue);
	}

	public partial class LogMessageDetailsDialog : ICloseable
	{
		public LogMessageDetailsDialog()
		{
			Owner = Application.Current.MainWindow;
			InitializeComponent();
		}

		public void CloseWindow(bool returnValue)
		{
			DialogResult = returnValue;
			Close();
		}
	}
}
