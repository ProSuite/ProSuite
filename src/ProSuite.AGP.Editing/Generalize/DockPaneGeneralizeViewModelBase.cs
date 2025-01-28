using System.Windows;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.Generalize
{
	public abstract class DockPaneGeneralizeViewModelBase : DockPaneViewModelBase
	{
		protected DockPaneGeneralizeViewModelBase() : base(new DockPaneGeneralize())
		{
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}

		#region RestoreDefaultsButton

		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM
		{
			get => _targetFeatureSelectionVm;
			private set => SetProperty(ref _targetFeatureSelectionVm, value);
		}

		public ICommand RevertToDefaultsCommand { get; }

		public bool IsRevertToDefaultsEnabled => true;

		private void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}

		#endregion

		/// Text shown near the top of the DockPane.
		private string _heading = "Generalization Options";

		private AdvancedGeneralizeOptions _options;
		private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public AdvancedGeneralizeOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value);

				TargetFeatureSelectionVM =
					new TargetFeatureSelectionViewModel(
						_options.CentralizableVertexProtectingFeatureSelection)
					{
						SelectedFeaturesVisibility = Visibility.Collapsed
					};
			}
		}
	}
}
