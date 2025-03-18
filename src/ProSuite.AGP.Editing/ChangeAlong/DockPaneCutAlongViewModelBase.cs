using System.Windows;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class DockPaneCutAlongViewModelBase : DockPaneViewModelBase
	{
		public DockPaneCutAlongViewModelBase() : base(new DockPaneCutAlong())
		{
			RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
		}

		#region RestoreDefaultsButton

		public TargetFeatureSelectionViewModel TargetFeatureSelectionVM
		{
			get => _targetFeatureSelectionVm;
			private set => SetProperty(ref _targetFeatureSelectionVm, value);
		}

		public ZValueSourceSelectionViewModel ZValueSourceSelectionVM
		{
			get => _zValueSourceSelectionVm;
			private set => SetProperty(ref _zValueSourceSelectionVm, value);
		}

		public ICommand RevertToDefaultsCommand { get; }

		public bool IsRevertToDefaultsEnabled => true;

		private void RevertToDefaults()
		{
			Options?.RevertToDefaults();
		}

		#endregion

		private string _heading = "Cut Along Options";

		private CutAlongToolOptions _options;

		private CentralizableSettingViewModel<bool> _displayExcludeCutLines;
		private CentralizableSettingViewModel<bool> _displayRecalculateCutLines;
		private CentralizableSettingViewModel<bool> _displayHideCutLines;
		private CentralizableSettingViewModel<double> _displayHideCutLinesScale;
		private CentralizableSettingViewModel<bool> _bufferTarget;
		private CentralizableSettingViewModel<double> _bufferTolerance;
		private CentralizableSettingViewModel<bool> _enforceMinimumBufferSegmentLength;
		private CentralizableSettingViewModel<double> _minBufferSegmentLength;
		private CentralizableSettingViewModel<bool> _insertVertices;

		private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;
		private ZValueSourceSelectionViewModel _zValueSourceSelectionVm;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
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

		public CentralizableSettingViewModel<bool> DisplayHideCutLines
		{
			get => _displayHideCutLines;
			set => SetProperty(ref _displayHideCutLines, value);
		}

		public CentralizableSettingViewModel<double> DisplayHideCutLinesScale
		{
			get => _displayHideCutLinesScale;
			set => SetProperty(ref _displayHideCutLinesScale, value);
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

		public CutAlongToolOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value);

				DisplayExcludeCutLines =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableDisplayExcludeCutLines);
				DisplayRecalculateCutLines = new CentralizableSettingViewModel<bool>(
					Options.CentralizableDisplayRecalculateCutLines,
					Options.CentralizableDisplayExcludeCutLines);
				DisplayHideCutLines =
					new CentralizableSettingViewModel<bool>(
						Options.CentralizableDisplayHideCutLines);
				DisplayHideCutLinesScale = new CentralizableSettingViewModel<double>(
					Options.CentralizableDisplayHideCutLinesScale,
					Options.CentralizableDisplayHideCutLines);
				BufferTarget =
					new CentralizableSettingViewModel<bool>(Options.CentralizableBufferTarget);
				BufferTolerance = new CentralizableSettingViewModel<double>(
					Options.CentralizableBufferTolerance, Options.CentralizableBufferTarget);
				EnforceMinimumBufferSegmentLength = new CentralizableSettingViewModel<bool>(
					Options.CentralizableEnforceMinimumBufferSegmentLength,
					Options.CentralizableBufferTarget);
				MinBufferSegmentLength = new CentralizableSettingViewModel<double>(
					Options.CentralizableMinBufferSegmentLength,
					Options.CentralizableEnforceMinimumBufferSegmentLength);
				InsertVertices =
					new CentralizableSettingViewModel<bool>(Options.CentralizableInsertVertices);

				if (SetProperty(ref _options, value) && value != null)
				{
					TargetFeatureSelectionVM =
						new TargetFeatureSelectionViewModel(
							_options.CentralizableTargetFeatureSelection)
						{
							SelectedFeaturesVisibility = Visibility.Collapsed,
							EditableSelectableFeaturesVisibility = Visibility.Collapsed
						};

					ZValueSourceSelectionVM =
						new ZValueSourceSelectionViewModel(_options.CentralizableZValueSource);
				}
			}
		}
	}
}
