using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.Chopper;

public abstract class DockPaneChopperViewModelBase : DockPaneViewModelBase
{
	protected DockPaneChopperViewModelBase()
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
	private string _heading = "Chopper Options";

	private ChopperToolOptions _options;

	private CentralizableSettingViewModel<bool> _snapToTargetVertices;
	private CentralizableSettingViewModel<double> _snapTolerance;
	private CentralizableSettingViewModel<bool> _respectMinimumSegmentLength;
	private CentralizableSettingViewModel<double> _minimumSegmentLength;
	private CentralizableSettingViewModel<bool> _useSourceZs;
	private CentralizableSettingViewModel<bool> _excludeInteriorInteriorIntersections;

	private TargetFeatureSelectionViewModel _targetFeatureSelectionVm;

	private string _snapToleranceWarning = string.Empty;

	public string Heading
	{
		get => _heading;
		set { SetProperty(ref _heading, value, () => Heading); }
	}

	/// <summary>
	/// Warning shown when the snap tolerance is larger than the current map view extent.
	/// Empty when there is nothing to warn about.
	/// </summary>
	public string SnapToleranceWarning
	{
		get => _snapToleranceWarning;
		private set
		{
			if (SetProperty(ref _snapToleranceWarning, value))
			{
				NotifyPropertyChanged(nameof(HasSnapToleranceWarning));
			}
		}
	}

	public bool HasSnapToleranceWarning => ! string.IsNullOrEmpty(_snapToleranceWarning);

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

	public CentralizableSettingViewModel<bool> ExcludeInteriorInteriorIntersections
	{
		get => _excludeInteriorInteriorIntersections;
		set => SetProperty(ref _excludeInteriorInteriorIntersections, value);
	}

	public TargetFeatureSelectionViewModel TargetFeatureSelectionVM
	{
		get => _targetFeatureSelectionVm;
		private set => SetProperty(ref _targetFeatureSelectionVm, value);
	}

	public ChopperToolOptions Options
	{
		get => _options;
		set
		{
			SetProperty(ref _options, value);

			DisplayUnitInfo unit = DisplayUnitInfo.FromMap(MapView.Active?.Map);

			SnapToTargetVertices =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableSnapToTargetVertices);

			SnapTolerance = new CentralizableSettingViewModel<double>(
				                Options.CentralizableSnapTolerance,
				                new[] { Options.CentralizableSnapToTargetVertices })
			                {
				                Decimals = unit.Decimals, Step = unit.Step,
				                UnitLabel = unit.Label
			                };

			SnapTolerance.PropertyChanged += SnapToleranceRelatedPropertyChanged;
			SnapToTargetVertices.PropertyChanged += SnapToleranceRelatedPropertyChanged;
			UpdateSnapToleranceWarning();

			RespectMinimumSegmentLength =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableRespectMinimumSegmentLength);

			MinimumSegmentLength = new CentralizableSettingViewModel<double>(
				                       Options.CentralizableMinimumSegmentLength,
				                       new[] { Options.CentralizableRespectMinimumSegmentLength })
			                       {
				                       Decimals = unit.Decimals, Step = unit.Step,
				                       UnitLabel = unit.Label
			                       };

			UseSourceZs =
				new CentralizableSettingViewModel<bool>(Options.CentralizableUseSourceZs);

			ExcludeInteriorInteriorIntersections =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableExcludeInteriorInteriorIntersections);

			TargetFeatureSelectionVM =
				new TargetFeatureSelectionViewModel(
					_options.CentralizableTargetFeatureSelection);
		}
	}

	protected override void OnShowCore(bool isVisible)
	{
		if (isVisible)
		{
			// Recompute against the current extent whenever the pane is shown.
			UpdateSnapToleranceWarning();
		}
	}

	private void SnapToleranceRelatedPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(CentralizableSettingViewModel<double>.CurrentValue))
		{
			UpdateSnapToleranceWarning();
		}
	}

	private void UpdateSnapToleranceWarning()
	{
		if (SnapTolerance == null || SnapToTargetVertices == null ||
		    ! SnapToTargetVertices.CurrentValue)
		{
			SnapToleranceWarning = string.Empty;
			return;
		}

		string warning = ToolDockpaneUtils.GetToleranceExceedsExtentWarning(
			SnapTolerance.CurrentValue, MapView.Active?.Extent, SnapTolerance.UnitLabel);

		SnapToleranceWarning = warning ?? string.Empty;
	}

	protected override Control CreateView()
	{
		return new DockPaneChopper();
	}
}
