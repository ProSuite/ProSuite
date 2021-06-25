using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.AGP.Editing.RemoveOverlaps;

namespace ProSuite.AGP.Solution.EditingOptionsUI
{
	internal class RemoveOverlapsOptionsViewModel : DockPane
	{
		private const string _dockPaneID = "ProSuite_AGP_Solution_EditingOptionsUI_RemoveOverlapsOptions";

		protected RemoveOverlapsOptionsViewModel()
		{
			
		}

		/// <summary>
		/// Show the DockPane.
		/// </summary>
		internal static void Show()
		{
			DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
			if (pane == null)
				return;

			pane.Activate();
		}

		internal static RemoveOverlapsOptionsViewModel GetInstance()
		{
			return FrameworkApplication.DockPaneManager.Find(_dockPaneID) as RemoveOverlapsOptionsViewModel;
		}

		internal static string GetId()
		{
			return _dockPaneID;
		}

		/// <summary>
		/// Text shown near the top of the DockPane.
		/// </summary>
		private string _heading = "Remove Overlaps Options";

		private RemoveOverlapsOptions _options;

		public string Heading
		{
			get { return _heading; }
			set
			{
				SetProperty(ref _heading, value, () => Heading);
			}
		}

		public RemoveOverlapsOptions Options
		{
			get => _options;
			set => _options = value;
		}
	}
	
	/// <summary>
	/// Button implementation to show the DockPane.
	/// </summary>
	internal class RemoveOverlapsOptions_ShowButton : Button
	{
		protected override void OnClick()
		{
			RemoveOverlapsOptionsViewModel.Show();
		}
	}
}
