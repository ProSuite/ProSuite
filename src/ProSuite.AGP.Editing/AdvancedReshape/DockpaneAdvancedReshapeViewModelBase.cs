using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public abstract class DockpaneAdvancedReshapeViewModelBase : DockPaneViewModelBase
	{
		//private const string _dockPaneID = "Swisstopo_GoTop_AddIn_EditTools_AdvancedReshape";

		protected abstract string DockPaneDamlID { get; }

		protected DockpaneAdvancedReshapeViewModelBase() : base(new DockpaneAdvancedReshape())
		{
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}
		#region RestoreDefaultsButton
		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM { get; private set; }


		public ICommand RevertToDefaultsCommand { get; }

		public bool IsButtonEnabled => _options?.CentralOptions != null;

		private void RevertToDefaults() {
			Options?.RevertToDefaults();
		}
		#endregion


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

		/// <summary>
		/// Text shown near the top of the DockPane.
		/// </summary>
		private string _heading = "Reshape Options";

		private ReshapeToolOptions _options;

		public string Heading
		{
			get { return _heading; }
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public ReshapeToolOptions Options
		{
			get { return _options; }
			set { SetProperty(ref _options, value); }
		}


	}
}
