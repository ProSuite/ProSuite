using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.MergeFeatures;

public abstract class DockPaneMergeFeaturesViewModelBase : DockPaneViewModelBase
{
	protected DockPaneMergeFeaturesViewModelBase()
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
	private CentralizableSettingViewModel<bool> _transferRelationships;
	private CentralizableSettingViewModel<bool> _preventInconsistentMerge;
	private CentralizableSettingViewModel<bool> _preventMultipartResult;
	private CentralizableSettingViewModel<bool> _preventInconsistentClasses;
	private CentralizableSettingViewModel<bool> _preventInconsistentAttributes;
	private CentralizableSettingViewModel<bool> _preventInconsistentRelationships;
	private bool _showPreventInconsistentAttributesOption = true;
	private CentralizableSettingViewModel<bool> _preventLoops;
	private CentralizableSettingViewModel<bool> _preventLineFlip;

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

	public CentralizableSettingViewModel<bool> TransferRelationships
	{
		get => _transferRelationships;
		set { SetProperty(ref _transferRelationships, value); }
	}

	public CentralizableSettingViewModel<bool> PreventInconsistentMerge
	{
		get => _preventInconsistentMerge;
		set { SetProperty(ref _preventInconsistentMerge, value); }
	}

	public CentralizableSettingViewModel<bool> PreventMultipartResult
	{
		get => _preventMultipartResult;
		set { SetProperty(ref _preventMultipartResult, value); }
	}

	public CentralizableSettingViewModel<bool> PreventInconsistentClasses
	{
		get => _preventInconsistentClasses;
		set { SetProperty(ref _preventInconsistentClasses, value); }
	}

	public CentralizableSettingViewModel<bool> PreventInconsistentAttributes
	{
		get => _preventInconsistentAttributes;
		set { SetProperty(ref _preventInconsistentAttributes, value); }
	}

	public bool ShowPreventInconsistentAttributesOption
	{
		get => _showPreventInconsistentAttributesOption;
		set { SetProperty(ref _showPreventInconsistentAttributesOption, value); }
	}

	public CentralizableSettingViewModel<bool> PreventInconsistentRelationships
	{
		get => _preventInconsistentRelationships;
		set { SetProperty(ref _preventInconsistentRelationships, value); }
	}

	public CentralizableSettingViewModel<bool> PreventLoops
	{
		get => _preventLoops;
		set { SetProperty(ref _preventLoops, value); }
	}

	public CentralizableSettingViewModel<bool> PreventLineFlip
	{
		get => _preventLineFlip;
		set { SetProperty(ref _preventLineFlip, value); }
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

			TransferRelationships = new CentralizableSettingViewModel<bool>(
				Options.CentralizableTransferRelationships);

			PreventInconsistentMerge = new CentralizableSettingViewModel<bool>(
				Options.CentralizablePreventInconsistentMerge);

			PreventMultipartResult = new CentralizableSettingViewModel<bool>(
				Options.CentralizablePreventMultipartResult);

			PreventInconsistentClasses = new CentralizableSettingViewModel<bool>(
				Options.CentralizablePreventInconsistentClasses);

			PreventInconsistentAttributes = new CentralizableSettingViewModel<bool>(
				Options.CentralizablePreventInconsistentAttributes);

			PreventInconsistentRelationships = new CentralizableSettingViewModel<bool>(
				Options.CentralizablePreventInconsistentRelationships);

			PreventLoops = new CentralizableSettingViewModel<bool>(
				Options.CentralizablePreventLoops);

			PreventLineFlip = new CentralizableSettingViewModel<bool>(
				Options.CentralizablePreventLineFlip);

			MergeOperationSurvivorVM =
				new MergeOperationSurvivorViewModel(
					_options.CentralizableMergeOperationSurvivor) { };
		}
	}

	protected override Control CreateView()
	{
		return new DockPaneMergeFeatures();
	}
}
