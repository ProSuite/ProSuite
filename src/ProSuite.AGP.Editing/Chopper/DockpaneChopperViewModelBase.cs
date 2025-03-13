using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.Chopper
{
	public abstract class DockPaneChopperViewModelBase : DockPaneViewModelBase
	{
		protected DockPaneChopperViewModelBase() : base(new DockPaneChopper())
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
		private string _heading = "Chopper Options";

		private ChopperToolOptions _options;
		private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;

		public string Heading
		{
			get => _heading;
			set { SetProperty(ref _heading, value, () => Heading); }
		}

		public CentralizableSettingViewModel<bool> SnapToTargetVertices =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableSnapToTargetVertices);

		public CentralizableSettingViewModel<double> SnapTolerance =>
			new CentralizableSettingViewModel<double>(Options.CentralizableSnapTolerance,
			                                          Options.CentralizableSnapToTargetVertices);

		public CentralizableSettingViewModel<bool> RespectMinimumSegmentLength =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableRespectMinimumSegmentLength);

		public CentralizableSettingViewModel<double> MinimumSegmentLength =>
			new CentralizableSettingViewModel<double>(Options.CentralizableMinimumSegmentLength,
			                                          Options.CentralizableRespectMinimumSegmentLength);

		public CentralizableSettingViewModel<bool> UseSourceZs =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableUseSourceZs);

		public CentralizableSettingViewModel<bool> ExcludeInteriorInteriorIntersections =>
			new CentralizableSettingViewModel<bool>(Options.CentralizableExcludeInteriorInteriorIntersections);

		public ChopperToolOptions Options
		{
			get => _options;
			set
			{
				SetProperty(ref _options, value);

				TargetFeatureSelectionVM =
					new TargetFeatureSelectionViewModel(
						_options.CentralizableTargetFeatureSelection);
			}
		}
	}
}
