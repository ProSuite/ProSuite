using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.LoggerUI;
using System;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public abstract class DockpaneAdvancedReshapeViewModelBase :  DockPaneViewModelBase
	{
		//private const string _dockPaneID = "Swisstopo_GoTop_AddIn_EditTools_AdvancedReshape";

		protected abstract string DockPaneDamlID { get; }

		protected DockpaneAdvancedReshapeViewModelBase() : base(new DockpaneAdvancedReshape()) { }

		/// <summary>
		/// Show the DockPane.
		/// </summary>
		//internal static void Show()
		//{
		//	DockPane pane = FrameworkApplication.DockPaneManager.Find(DockPaneDamlID);
		//	if (pane == null)
		//		return;

		//	pane.Activate();
		//}

		//internal static DockpaneAdvancedReshapeViewModelBase GetInstance()
		//{
		//	return FrameworkApplication.DockPaneManager.Find(DockPaneDamlID) as DockpaneAdvancedReshapeViewModelBase;
		//}

		/// <summary>
		/// Text shown near the top of the DockPane.
		/// </summary>
		private string _heading = "Reshape Options";

		private ReshapeToolOptions _options;

		public string Heading
		{
			get { return _heading; }
			set
			{
				SetProperty(ref _heading, value, () => Heading);
			}
		}

		public ReshapeToolOptions Options
		{
			get { return _options; }
			set { SetProperty(ref _options, value); }
		}

	}

	/// <summary>
	/// Button implementation for the button on the menu of the burger button.
	/// </summary>
	internal class Dockpane1_MenuButton : Button
	{
		protected override void OnClick()
		{
		}
	}
}
