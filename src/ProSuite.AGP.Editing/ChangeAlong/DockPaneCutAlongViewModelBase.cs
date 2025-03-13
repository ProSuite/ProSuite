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
		private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;
		private ZValueSourceSelectionViewModel _zValueSourceSelectionVm;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CentralizableSettingViewModel<bool> DisplayExcludeCutLines =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableDisplayExcludeCutLines);

		public CentralizableSettingViewModel<bool> DisplayRecalculateCutLines =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableDisplayRecalculateCutLines,
			                                        Options.CentralizableDisplayExcludeCutLines);

		public CentralizableSettingViewModel<bool> DisplayHideCutLines =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableDisplayHideCutLines);

		public CentralizableSettingViewModel<double> DisplayHideCutLinesScale =>
			new CentralizableSettingViewModel<double>(Options.CentralizableDisplayHideCutLinesScale,
			                                          Options.CentralizableDisplayHideCutLines);

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

		public CutAlongToolOptions Options
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

					ZValueSourceSelectionVM =
						new ZValueSourceSelectionViewModel(_options.CentralizableZValueSource);

				}
			}
		}
	}
}
