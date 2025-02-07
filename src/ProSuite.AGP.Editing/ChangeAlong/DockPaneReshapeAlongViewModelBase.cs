using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.Commons.AGP.Framework;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class DockPaneReshapeAlongViewModelBase : DockPaneViewModelBase
	{
		protected DockPaneReshapeAlongViewModelBase() : base(new DockPaneReshapeAlong()) {
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

		private string _heading = "Reshape Along Options";
		
		private ReshapeAlongToolOptions _options;

		public string Heading {
			get { return _heading; }
			set { SetProperty(ref _heading, value, () => Heading); }
		}
		public ReshapeAlongToolOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value, () => Options);
				NotifyPropertyChanged();
				if (value != null)
				{
					TargetFeatureSelectionVM = new TargetFeatureSelectionViewModel(value.CentralizableTargetFeatureSelection);
				}
			}
		}
	}
}
