using System.ComponentModel;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.Cracker
{
	public abstract class DockpaneCrackerViewModelBase : DockPaneViewModelBase
	{
		protected DockpaneCrackerViewModelBase() : base(new DockpaneCracker())
		{
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}

		#region RestoreDefaultsButton

		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM { get; private set; }

		public ICommand RevertToDefaultsCommand { get; }

		public bool IsButtonEnabled => _options?.CentralOptions != null;

		private void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}

		#endregion

		protected abstract string DockPaneDamlID { get; }

		/// Text shown near the top of the DockPane.
		private string _heading = "Cracker Options";

		private CrackerToolOptions _options;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CrackerToolOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value);

				TargetFeatureSelectionVM =
					new TargetFeatureSelectionViewModel(
						_options.CentralizableTargetFeatureSelection);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
