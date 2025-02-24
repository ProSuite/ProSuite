using System.Windows;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class DockPaneReshapeAlongViewModelBase : DockPaneViewModelBase
	{
		protected DockPaneReshapeAlongViewModelBase() : base(new DockPaneReshapeAlong())
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

		private string _heading = "Reshape Along Options";

		private ReshapeAlongToolOptions _options;
		private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public ReshapeAlongToolOptions Options
		{
			get => _options;
			set
			{
				if (SetProperty(ref _options, value) && value != null)
				{
					TargetFeatureSelectionVM =
						new TargetFeatureSelectionViewModel(
							_options.CentralizableTargetFeatureSelection)
						{
							SelectedFeaturesVisibility = Visibility.Collapsed,
							EditableSelectableFeaturesVisibility = Visibility.Collapsed
						};
				}
			}
		}
	}
}
