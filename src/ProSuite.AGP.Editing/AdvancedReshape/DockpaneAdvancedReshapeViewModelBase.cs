using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public abstract class DockPaneAdvancedReshapeViewModelBase : DockPaneViewModelBase
	{
		protected DockPaneAdvancedReshapeViewModelBase() : base(new DockPaneAdvancedReshape())
		{
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}
		#region RestoreDefaultsButton

		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM { get; private set; }


		public ICommand RevertToDefaultsCommand { get; }

		public bool IsRevertToDefaultsEnabled => true;

		private void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}
		#endregion

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
