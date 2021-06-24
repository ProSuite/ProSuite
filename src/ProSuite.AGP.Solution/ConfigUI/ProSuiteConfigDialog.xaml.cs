using ArcGIS.Desktop.Framework.Controls;

namespace ProSuite.AGP.Solution.ConfigUI
{
	public interface ICloseable
	{
		void CloseWindow(bool returnValue);
	}

	/// <summary>
	/// Interaction logic for ProSuiteConfigDialog.xaml
	/// </summary>
	public partial class ProSuiteConfigDialog : ProWindow, ICloseable
	{
		public ProSuiteConfigDialog()
		{
			InitializeComponent();
		}

		public void CloseWindow(bool returnValue)
		{
			DialogResult = returnValue;
			Close();
		}
	}
}
