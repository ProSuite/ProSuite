using ProSuite.Commons.AGP.Framework;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;

namespace ProSuite.AGP.Editing.Cracker
{
	public abstract class DockpaneCrackerViewModelBase : DockPaneViewModelBase
	{
		protected abstract string DockPaneDamlID { get; }

		protected DockpaneCrackerViewModelBase() : base(new DockpaneCracker())
		{
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}

		/// Text shown near the top of the DockPane.
		private string _heading = "Cracker Options";

		private CrackerToolOptions _options;

		public string Heading
		{
			get { return _heading; }
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CrackerToolOptions Options
		{
			get { return _options; }
			set
			{
				SetProperty(ref _options, value);

				TargetFeatureSelectionVM =
					new TargetFeatureSelectionViewModel(
						_options.CentralizableTargetFeatureSelection);
			}
		}

		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM { get; private set; }

		public double SpinnerValue { get; set; }

		// Add a command to trigger RevertToDefaults
		public ICommand RevertToDefaultsCommand { get; }
	
		private void RevertToDefaults() {
			Options?.RevertToDefaults();
		}
	}
}
