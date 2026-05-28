using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.RepairGeometry;

public abstract class DockPaneRepairGeometryViewModelBase : DockPaneViewModelBase
{
	protected DockPaneRepairGeometryViewModelBase()
	{
		RevertToDefaultsCommand = new RelayCommand(RevertToDefaults);
	}

	public ICommand RevertToDefaultsCommand { get; }

	public bool IsRevertToDefaultsEnabled => true;

	private void RevertToDefaults()
	{
		Options?.RevertToDefaults();
	}

	private string _heading = "Repair Geometry Options";

	private RepairGeometryToolOptions _options;
	private CentralizableSettingViewModel<bool> _enforceMinimumSegmentLength;
	private CentralizableSettingViewModel<double> _minimumSegmentLength;
	private CentralizableSettingViewModel<bool> _allowLoops;
	private CentralizableSettingViewModel<bool> _allowLinearSelfIntersections;
	private CentralizableSettingViewModel<bool> _addCrackPointsBetweenParts;
	private CentralizableSettingViewModel<double> _crackPointTolerance;
	private CentralizableSettingViewModel<bool> _use2D;

	public string Heading
	{
		get => _heading;
		set { SetProperty(ref _heading, value, () => Heading); }
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

	public CentralizableSettingViewModel<bool> AllowLoops
	{
		get => _allowLoops;
		set { SetProperty(ref _allowLoops, value); }
	}

	public CentralizableSettingViewModel<bool> AllowLinearSelfIntersections
	{
		get => _allowLinearSelfIntersections;
		set { SetProperty(ref _allowLinearSelfIntersections, value); }
	}

	public CentralizableSettingViewModel<bool> AddCrackPointsBetweenParts
	{
		get => _addCrackPointsBetweenParts;
		set { SetProperty(ref _addCrackPointsBetweenParts, value); }
	}

	public CentralizableSettingViewModel<double> CrackPointTolerance
	{
		get => _crackPointTolerance;
		set { SetProperty(ref _crackPointTolerance, value); }
	}

	public CentralizableSettingViewModel<bool> Use2D
	{
		get => _use2D;
		set { SetProperty(ref _use2D, value); }
	}

	public RepairGeometryToolOptions Options
	{
		get => _options;
		set
		{
			SetProperty(ref _options, value);

			EnforceMinimumSegmentLength =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableEnforceMinimumSegmentLength);

			MinimumSegmentLength =
				new CentralizableSettingViewModel<double>(
					Options.CentralizableMinimumSegmentLength,
					new[] { Options.CentralizableEnforceMinimumSegmentLength });

			AllowLoops =
				new CentralizableSettingViewModel<bool>(Options.CentralizableAllowLoops);

			AllowLinearSelfIntersections =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableAllowLinearSelfIntersections);

			AddCrackPointsBetweenParts =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableAddCrackPointsBetweenParts);

			CrackPointTolerance =
				new CentralizableSettingViewModel<double>(
					Options.CentralizableCrackPointTolerance,
					new[] { Options.CentralizableAddCrackPointsBetweenParts });

			Use2D =
				new CentralizableSettingViewModel<bool>(Options.CentralizableUse2D);
		}
	}

	protected override Control CreateView()
	{
		return new DockPaneRepairGeometry();
	}
}
