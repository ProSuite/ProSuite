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

		public CentralizableSettingViewModel<bool> ExcludeLinesOutsideSource =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableExcludeLinesOutsideSource);

		public CentralizableSettingViewModel<double> ExcludeLinesTolerance =>
			new CentralizableSettingViewModel<double>(Options.CentralizableExcludeLinesTolerance,
			                                          Options
				                                          .CentralizableExcludeLinesOutsideSource);

		public CentralizableSettingViewModel<bool> ExcludeLinesDisplay =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableExcludeLinesDisplay,
			                                        Options.CentralizableExcludeLinesOutsideSource);

		public CentralizableSettingViewModel<bool> ExcludeLinesShowOnlyRemove =>
			new CentralizableSettingViewModel<bool>(
				Options.CentralizableExcludeLinesShowOnlyRemove);

		public CentralizableSettingViewModel<bool> ExcludeLinesOverlaps =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableExcludeLinesOverlaps);

		public CentralizableSettingViewModel<bool> DisplayExcludeCutLines =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableDisplayExcludeCutLines);

		public CentralizableSettingViewModel<bool> DisplayRecalculateCutLines =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableDisplayRecalculateCutLines,
			                                        Options.CentralizableDisplayExcludeCutLines);

		public CentralizableSettingViewModel<bool> BufferTarget =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableBufferTarget);

		public CentralizableSettingViewModel<double> BufferTolerance =>
			new CentralizableSettingViewModel<double>(Options.CentralizableBufferTolerance,
			                                          Options.CentralizableBufferTarget);

		public CentralizableSettingViewModel<bool> EnforceMinimumBufferSegmentLength =>
			new CentralizableSettingViewModel<bool>(
				Options.CentralizableEnforceMinimumBufferSegmentLength,
				Options.CentralizableBufferTarget);

		public CentralizableSettingViewModel<double> MinBufferSegmentLength =>
			new CentralizableSettingViewModel<double>(Options.CentralizableMinBufferSegmentLength,
			                                          Options
				                                          .CentralizableEnforceMinimumBufferSegmentLength);

		public CentralizableSettingViewModel<bool> InsertVertices =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableInsertVertices);

		public CentralizableSettingViewModel<bool> MinimalToleranceApply =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableMinimalToleranceApply);

		public CentralizableSettingViewModel<double> MinimalTolerance =>
			new CentralizableSettingViewModel<double>(Options.CentralizableMinimalTolerance,
			                                          Options.CentralizableMinimalToleranceApply);

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
