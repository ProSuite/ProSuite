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

		public ICommand RevertToDefaultsCommand { get; }

		public bool IsRevertToDefaultsEnabled => true;

		private void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}

		#endregion

		private string _heading = "Generalization Options";

		private AdvancedGeneralizeToolOptions _options;
		private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;

		private CentralizableSettingViewModel<bool> _weed;
		private CentralizableSettingViewModel<double> _weedTolerance;
		private CentralizableSettingViewModel<bool> _weedNonLinearSegments;
		private CentralizableSettingViewModel<bool> _enforceMinimumSegmentLength;
		private CentralizableSettingViewModel<double> _minimumSegmentLength;
		private CentralizableSettingViewModel<bool> _only2D;
		private CentralizableSettingViewModel<bool> _protectTopologicalVertices;
		private CentralizableSettingViewModel<bool> _limitToVisibleExtent;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CentralizableSettingViewModel<bool> Weed
		{
			get => _weed;
			set { SetProperty(ref _weed, value); }
		}

		public CentralizableSettingViewModel<double> WeedTolerance
		{
			get => _weedTolerance;
			set { SetProperty(ref _weedTolerance, value); }
		}

		public CentralizableSettingViewModel<bool> WeedNonLinearSegments
		{
			get => _weedNonLinearSegments;
			set { SetProperty(ref _weedNonLinearSegments, value); }
		}

		public CentralizableSettingViewModel<bool> EnforceMinimumSegmentLength
		{
			get => _enforceMinimumSegmentLength;
			set { SetProperty(ref _enforceMinimumSegmentLength, value); }
		}

		public CentralizableSettingViewModel<double> MinimumSegmentLength
		{
			get => _minimumSegmentLength;
			set { SetProperty(ref _minimumSegmentLength, value); }
		}

		public CentralizableSettingViewModel<bool> Only2D
		{
			get => _only2D;
			set { SetProperty(ref _only2D, value); }
		}

		public CentralizableSettingViewModel<bool> ProtectTopologicalVertices
		{
			get => _protectTopologicalVertices;
			set { SetProperty(ref _protectTopologicalVertices, value); }
		}

		public CentralizableSettingViewModel<bool> LimitToVisibleExtent
		{
			get => _limitToVisibleExtent;
			set { SetProperty(ref _limitToVisibleExtent, value); }
		}

		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM
		{
			get => _targetFeatureSelectionVm;
			private set => SetProperty(ref _targetFeatureSelectionVm, value);
		}

		public AdvancedGeneralizeToolOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value);

				Weed = new CentralizableSettingViewModel<bool>(Options.CentralizableWeed);
				WeedTolerance = new CentralizableSettingViewModel<double>(
					Options.CentralizableWeedTolerance, new[] { Options.CentralizableWeed });

				WeedNonLinearSegments = new CentralizableSettingViewModel<bool>(
					Options.CentralizableWeedNonLinearSegments,
					new[] { Options.CentralizableWeed });

				EnforceMinimumSegmentLength =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableEnforceMinimumSegmentLength);

				MinimumSegmentLength = new CentralizableSettingViewModel<double>(
					Options.CentralizableMinimumSegmentLength,
					new[] { Options.CentralizableEnforceMinimumSegmentLength });

				Only2D = new CentralizableSettingViewModel<bool>(Options.CentralizableOnly2D);

				ProtectTopologicalVertices =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableProtectTopologicalVertices);

				LimitToVisibleExtent =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableLimitToVisibleExtent);

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
