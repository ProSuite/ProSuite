using ArcGIS.Desktop.Framework.Controls;
using ProSuite.AGP.Solution.ConfigUI;

namespace ProSuite.AGP.Solution.LoggerUI
{
	public partial class LogMessageDetailsDialog : ProWindow, ICloseable
	{
		public LogMessageDetailsDialog()
		{
			Owner = System.Windows.Application.Current.MainWindow;
			InitializeComponent();
		}

		public void CloseWindow(bool returnValue)
		{
			DialogResult = returnValue;
			Close();
		}
	}
}
