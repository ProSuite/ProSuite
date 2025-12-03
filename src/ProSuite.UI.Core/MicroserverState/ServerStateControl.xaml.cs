using System.Windows.Forms;
using ProSuite.Commons.UI.WPF;
using UserControl = System.Windows.Controls.UserControl;

namespace ProSuite.UI.Core.MicroserverState
{
	/// <summary>
	/// Interaction logic for ServerStateControl.xaml
	/// </summary>
	public partial class ServerStateControl : UserControl, IWinFormHostable
	{
		private Form _hostWinForm;

		public ServerStateControl()
		{
			InitializeComponent();
		}

		public void SetDataSource(ServerStateViewModel viewModel)
		{
			DataContext = viewModel;
		}

		#region Implementation of IWinFormHostable

		public Form HostFormsWindow
		{
			set
			{
				_hostWinForm = value;

				ServerStateViewModel vm = (ServerStateViewModel) DataContext;

				if (_hostWinForm != null && vm != null)
				{
					//_hostWinForm.Closing += vm.Closing;
				}
			}
		}

		#endregion
	}
}
