using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.MergeFeatures;

public abstract class DockPaneMergeFeaturesViewModelBase : DockPaneViewModelBase
{
	protected DockPaneMergeFeaturesViewModelBase() : base(new DockPaneMergeFeatures())
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

	private string _heading = "Merge Options";

	private MergeToolOptions _options;

	private CentralizableSettingViewModel<bool> _useMergeResultForNextMerge;
	private CentralizableSettingViewModel<bool> _allowMultipartResult;
	private CentralizableSettingViewModel<bool> _transferRelationships;

	private MergeOperationSurvivorViewModel _mergeOperationSurvivorVm;

	public string Heading
	{
		get => _heading;
		set { SetProperty(ref _heading, value, () => Heading); }
	}

	public CentralizableSettingViewModel<bool> UseMergeResultForNextMerge
	{
		get => _useMergeResultForNextMerge;
		set { SetProperty(ref _useMergeResultForNextMerge, value); }
	}

	public CentralizableSettingViewModel<bool> AllowMultipartResult
	{
		get => _allowMultipartResult;
		set { SetProperty(ref _allowMultipartResult, value); }
	}

	public CentralizableSettingViewModel<bool> TransferRelationships
	{
		get => _transferRelationships;
		set { SetProperty(ref _transferRelationships, value); }
	}

	public MergeOperationSurvivorViewModel MergeOperationSurvivorVM
	{
		get => _mergeOperationSurvivorVm;
		private set => SetProperty(ref _mergeOperationSurvivorVm, value);
	}

	public MergeToolOptions Options
	{
		get => _options;
		set
		{
			SetProperty(ref _options, value);

			UseMergeResultForNextMerge =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableUseMergeResultForNextMerge);
			AllowMultipartResult = new CentralizableSettingViewModel<bool>(
				Options.CentralizableAllowMultipartResult);

			TransferRelationships = new CentralizableSettingViewModel<bool>(
				Options.CentralizableTransferRelationships);

			MergeOperationSurvivorVM =
				new MergeOperationSurvivorViewModel(
					_options.CentralizableMergeOperationSurvivor) { };
		}
	}
}