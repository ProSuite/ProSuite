using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms;
using ProSuite.Microservices.Client;

namespace ProSuite.UI.Core.MicroserverState
{
	public static class ServerStateUtils
	{
		public static WpfHostingWinForm CreateServerStateForm(
			[NotNull] IEnumerable<IMicroserviceClient> serviceClients,
			[CanBeNull] string title)
		{
			var serverStateViewModel = new ServerStateViewModel(serviceClients);
			serverStateViewModel.StartAutoEvaluation();

			return CreateServerStateForm(serverStateViewModel, title);
		}

		public static WpfHostingWinForm CreateServerStateForm(
			[NotNull] ServerStateViewModel serverStateViewModel,
			[CanBeNull] string title)
		{
			ServerStateControl wpfControl = new ServerStateControl();
			wpfControl.SetDataSource(serverStateViewModel);

			var winForm = new WpfHostingWinForm(wpfControl);

			winForm.KeyDown += WinForm_KeyDown;

			winForm.Text = title;

			winForm.SetMinimumSize(50, 30);

			return winForm;
		}

		private static void WinForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				Form form = (Form) sender;
				form.Close();
			}
		}
	}
}
