using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.Cracker;

public abstract class DockPaneCrackerViewModelBase : DockPaneViewModelBase
{
	protected DockPaneCrackerViewModelBase() : base(new DockPaneCracker())
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

	/// Text shown near the top of the DockPane.
	private string _heading = "Cracker Options";

	private CrackerToolOptions _options;

	private CentralizableSettingViewModel<bool> _snapToTargetVertices;
	private CentralizableSettingViewModel<double> _snapTolerance;
	private CentralizableSettingViewModel<bool> _respectMinimumSegmentLength;
	private CentralizableSettingViewModel<double> _minimumSegmentLength;
	private CentralizableSettingViewModel<bool> _useSourceZs;

	private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;

	public string Heading
	{
		get => _heading;
		set { SetProperty(ref _heading, value, () => Heading); }
	}

	public CentralizableSettingViewModel<bool> SnapToTargetVertices
	{
		get => _snapToTargetVertices;
		set => SetProperty(ref _snapToTargetVertices, value);
	}

	public CentralizableSettingViewModel<double> SnapTolerance
	{
		get => _snapTolerance;
		set => SetProperty(ref _snapTolerance, value);
	}

	public CentralizableSettingViewModel<bool> RespectMinimumSegmentLength
	{
		get => _respectMinimumSegmentLength;
		set => SetProperty(ref _respectMinimumSegmentLength, value);
	}

	public CentralizableSettingViewModel<double> MinimumSegmentLength
	{
		get => _minimumSegmentLength;
		set => SetProperty(ref _minimumSegmentLength, value);
	}

	public CentralizableSettingViewModel<bool> UseSourceZs
	{
		get => _useSourceZs;
		set => SetProperty(ref _useSourceZs, value);
	}

	public TargetFeatureSelectionViewModel TargetFeatureSelectionVM
	{
		get => _targetFeatureSelectionVm;
		private set => SetProperty(ref _targetFeatureSelectionVm, value);
	}

	public CrackerToolOptions Options
	{
		get => _options;
		set
		{
			SetProperty(ref _options, value);

			SnapToTargetVertices =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableSnapToTargetVertices);

			SnapTolerance = new CentralizableSettingViewModel<double>(
				Options.CentralizableSnapTolerance,
				new[] { Options.CentralizableSnapToTargetVertices });

			RespectMinimumSegmentLength =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableRespectMinimumSegmentLength);

			MinimumSegmentLength = new CentralizableSettingViewModel<double>(
				Options.CentralizableMinimumSegmentLength,
				new[] { Options.CentralizableRespectMinimumSegmentLength });

			UseSourceZs =
				new CentralizableSettingViewModel<bool>(Options.CentralizableUseSourceZs);

			TargetFeatureSelectionVM =
				new TargetFeatureSelectionViewModel(
					_options.CentralizableTargetFeatureSelection);
		}
	}
}