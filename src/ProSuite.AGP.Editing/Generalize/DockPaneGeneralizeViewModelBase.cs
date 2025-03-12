using System.ComponentModel;
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

		private string _heading = "Generalization Options";

		private AdvancedGeneralizeOptions _options;
		private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CentralizableSettingViewModel<bool> Weed =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableWeed);

		public CentralizableSettingViewModel<double> WeedTolerance =>
			new CentralizableSettingViewModel<double>(Options.CentralizableWeedTolerance,
			                                          Options.CentralizableWeed);

		public CentralizableSettingViewModel<bool> WeedNonLinearSegments =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableWeedNonLinearSegments,
			                                        Options.CentralizableWeed);

		public CentralizableSettingViewModel<bool> EnforceMinimumSegmentLength =>
			new CentralizableSettingViewModel<bool>(
				Options.CentralizableEnforceMinimumSegmentLength);

		public CentralizableSettingViewModel<double> MinimumSegmentLength =>
			new CentralizableSettingViewModel<double>(Options.CentralizableMinimumSegmentLength,
			                                          Options
				                                          .CentralizableEnforceMinimumSegmentLength);

		public CentralizableSettingViewModel<bool> Only2D =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableOnly2D);

		public CentralizableSettingViewModel<bool> ProtectTopologicalVertices =>
			new CentralizableSettingViewModel<bool>(
				Options.CentralizableProtectTopologicalVertices);

		public CentralizableSettingViewModel<bool> LimitToVisibleExtent =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableLimitToVisibleExtent);

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
						SelectedFeaturesVisibility = Visibility.Collapsed,
						IsTargetFeatureSelectionEnabled = IsTargetFeatureSelectionEnabled
					};

				_options.CentralizableProtectTopologicalVertices.PropertyChanged +=
					VertexProtectionPropertyChanged;
			}
		}

		private void VertexProtectionPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			TargetFeatureSelectionVM.IsTargetFeatureSelectionEnabled =
				IsTargetFeatureSelectionEnabled;
		}

		private bool IsTargetFeatureSelectionEnabled =>
			_options.ProtectTopologicalVertices && _options
			                                       .CentralizableVertexProtectingFeatureSelection
			                                       .CanOverrideLocally;
	}
}
