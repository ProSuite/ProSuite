using System.Windows;

namespace ProSuite.Commons.AGP.LoggerUI
{
	public interface ICloseable
	{
		void CloseWindow(bool? dialogResult);
	}

	public partial class LogMessageDetailsDialog : ICloseable
	{
		public LogMessageDetailsDialog()
		{
			Owner = Application.Current.MainWindow;
			InitializeComponent();
		}

		public void CloseWindow(bool? dialogResult)
		{
			DialogResult = dialogResult;
			Close();
		}
	}
}
