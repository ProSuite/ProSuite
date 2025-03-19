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

		private CentralizableSettingViewModel<bool> _excludeLinesOutsideSource;
		private CentralizableSettingViewModel<double> _excludeLinesTolerance;
		private CentralizableSettingViewModel<bool> _excludeLinesDisplay;
		private CentralizableSettingViewModel<bool> _excludeLinesShowOnlyRemove;
		private CentralizableSettingViewModel<bool> _excludeLinesOverlaps;
		private CentralizableSettingViewModel<bool> _displayExcludeCutLines;
		private CentralizableSettingViewModel<bool> _displayRecalculateCutLines;
		private CentralizableSettingViewModel<bool> _bufferTarget;
		private CentralizableSettingViewModel<double> _bufferTolerance;
		private CentralizableSettingViewModel<bool> _enforceMinimumBufferSegmentLength;
		private CentralizableSettingViewModel<double> _minBufferSegmentLength;
		private CentralizableSettingViewModel<bool> _insertVertices;
		private CentralizableSettingViewModel<bool> _minimalToleranceApply;
		private CentralizableSettingViewModel<double> _minimalTolerance;

		private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CentralizableSettingViewModel<bool> ExcludeLinesOutsideSource
		{
			get => _excludeLinesOutsideSource;
			set => SetProperty(ref _excludeLinesOutsideSource, value);
		}

		public CentralizableSettingViewModel<double> ExcludeLinesTolerance
		{
			get => _excludeLinesTolerance;
			set => SetProperty(ref _excludeLinesTolerance, value);
		}

		public CentralizableSettingViewModel<bool> ExcludeLinesDisplay
		{
			get => _excludeLinesDisplay;
			set => SetProperty(ref _excludeLinesDisplay, value);
		}

		public CentralizableSettingViewModel<bool> ExcludeLinesShowOnlyRemove
		{
			get => _excludeLinesShowOnlyRemove;
			set => SetProperty(ref _excludeLinesShowOnlyRemove, value);
		}

		public CentralizableSettingViewModel<bool> ExcludeLinesOverlaps
		{
			get => _excludeLinesOverlaps;
			set => SetProperty(ref _excludeLinesOverlaps, value);
		}

		public CentralizableSettingViewModel<bool> DisplayExcludeCutLines
		{
			get => _displayExcludeCutLines;
			set => SetProperty(ref _displayExcludeCutLines, value);
		}

		public CentralizableSettingViewModel<bool> DisplayRecalculateCutLines
		{
			get => _displayRecalculateCutLines;
			set => SetProperty(ref _displayRecalculateCutLines, value);
		}

		public CentralizableSettingViewModel<bool> BufferTarget
		{
			get => _bufferTarget;
			set => SetProperty(ref _bufferTarget, value);
		}

		public CentralizableSettingViewModel<double> BufferTolerance
		{
			get => _bufferTolerance;
			set => SetProperty(ref _bufferTolerance, value);
		}

		public CentralizableSettingViewModel<bool> EnforceMinimumBufferSegmentLength
		{
			get => _enforceMinimumBufferSegmentLength;
			set => SetProperty(ref _enforceMinimumBufferSegmentLength, value);
		}

		public CentralizableSettingViewModel<double> MinBufferSegmentLength
		{
			get => _minBufferSegmentLength;
			set => SetProperty(ref _minBufferSegmentLength, value);
		}

		public CentralizableSettingViewModel<bool> InsertVertices
		{
			get => _insertVertices;
			set => SetProperty(ref _insertVertices, value);
		}

		public CentralizableSettingViewModel<bool> MinimalToleranceApply
		{
			get => _minimalToleranceApply;
			set => SetProperty(ref _minimalToleranceApply, value);
		}

		public CentralizableSettingViewModel<double> MinimalTolerance
		{
			get => _minimalTolerance;
			set => SetProperty(ref _minimalTolerance, value);
		}

		public ReshapeAlongToolOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value);

				ExcludeLinesOutsideSource =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableExcludeLinesOutsideSource);

				ExcludeLinesTolerance = new CentralizableSettingViewModel<double>(
					Options.CentralizableExcludeLinesTolerance,
					new[] { Options.CentralizableExcludeLinesOutsideSource });

				ExcludeLinesDisplay = new CentralizableSettingViewModel<bool>(
					Options.CentralizableExcludeLinesDisplay,
					new[] { Options.CentralizableExcludeLinesOutsideSource });

				ExcludeLinesShowOnlyRemove =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableExcludeLinesShowOnlyRemove);

				ExcludeLinesOverlaps =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableExcludeLinesOverlaps);

				DisplayExcludeCutLines =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableDisplayExcludeCutLines);

				DisplayRecalculateCutLines = new CentralizableSettingViewModel<bool>(
					Options.CentralizableDisplayRecalculateCutLines,
					new[] { Options.CentralizableDisplayExcludeCutLines });

				BufferTarget =
					new CentralizableSettingViewModel<bool>(Options.CentralizableBufferTarget);
				BufferTolerance = new CentralizableSettingViewModel<double>(
					Options.CentralizableBufferTolerance,
					new[] { Options.CentralizableBufferTarget });

				EnforceMinimumBufferSegmentLength = new CentralizableSettingViewModel<bool>(
					Options.CentralizableEnforceMinimumBufferSegmentLength,
					new[] { Options.CentralizableBufferTarget });

				MinBufferSegmentLength = new CentralizableSettingViewModel<double>(
					Options.CentralizableMinBufferSegmentLength,
					new[]
					{
						Options.CentralizableEnforceMinimumBufferSegmentLength,
						Options.CentralizableBufferTarget
					});

				InsertVertices =
					new CentralizableSettingViewModel<bool>(Options.CentralizableInsertVertices);

				MinimalToleranceApply =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableMinimalToleranceApply);

				MinimalTolerance = new CentralizableSettingViewModel<double>(
					Options.CentralizableMinimalTolerance,
					new[] { Options.CentralizableMinimalToleranceApply });

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
