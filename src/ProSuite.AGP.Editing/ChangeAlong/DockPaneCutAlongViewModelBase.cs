using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class DockPaneCutAlongViewModelBase : DockPaneViewModelBase {
		public DockPaneCutAlongViewModelBase() : base(new DockPaneCutAlong()) {
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}
		#region RestoreDefaultsButton
		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM { get; private set; }

		public ZValueSourceSelectionViewModel ZValueSourceSelectionVM { get; private set; }

		public ICommand RevertToDefaultsCommand { get; }

		public bool IsButtonEnabled => _options?.CentralOptions != null;

		private void RevertToDefaults() {
			Options?.RevertToDefaults();
		}
		#endregion

		private string _heading = "Cut Along Options";

		private CutAlongToolOptions _options;

		public string Heading {
			get { return _heading; }
			set { SetProperty(ref _heading, value, () => Heading); }
		}
		public CutAlongToolOptions Options {
			get => _options;
			set {
				SetProperty(ref _options, value, () => Options);
				NotifyPropertyChanged();
				if (value != null)
				{
					ZValueSourceSelectionVM = new ZValueSourceSelectionViewModel(value.CentralizableZValueSource);
				}
			}
		}
	}
}
