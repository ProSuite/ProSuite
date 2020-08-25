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
using Clients.AGP.ProSuiteSolution.WorkListUI;

namespace Clients.AGP.ProSuiteSolution
{
	/// <summary>
	/// Interaction logic for WorkList.xaml
	/// </summary>
	public partial class WorkList : ArcGIS.Desktop.Framework.Controls.ProWindow
	{
		WorkListViewModel ViewModel { get; set; }
		public WorkList()
		{
			InitializeComponent();
			DataContext = new WorkListViewModel();
		}

		public WorkList(WorkListViewModel viewModel)
		{
			ViewModel = viewModel;
		}
	}
}
