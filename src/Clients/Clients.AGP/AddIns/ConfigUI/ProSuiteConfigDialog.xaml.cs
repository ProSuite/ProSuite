using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clients.AGP.ProSuiteSolution.ConfigUI
{
	public interface ICloseable
	{
		void CloseWindow(bool returnValue);
	}

	/// <summary>
	/// Interaction logic for ProSuiteConfigDialog.xaml
	/// </summary>
	public partial class ProSuiteConfigDialog : ArcGIS.Desktop.Framework.Controls.ProWindow, ICloseable
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
