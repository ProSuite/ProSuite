using ArcGIS.Desktop.Framework.Contracts;

namespace ProSuite.AGP.Solution.WorkListUI
{
	internal class ShowIssueWorkListView : Button
	{
		//private IssueWorkListView _issueworklistview = null;

		protected override void OnClick()
		{
			//already open?
			//if (_issueworklistview != null)
			//	return;
			//_issueworklistview = new IssueWorkListView();
			//_issueworklistview.Owner = FrameworkApplication.Current.MainWindow;
			//_issueworklistview.Closed += (o, e) => { _issueworklistview = null; };
			//_issueworklistview.Show();
			//uncomment for modal
			//_issueworklistview.ShowDialog();
		}
	}
}
