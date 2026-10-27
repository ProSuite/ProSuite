using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.AdvancedReshape;

public abstract class DockPaneAdvancedReshapeViewModelBase : DockPaneViewModelBase
{
	protected DockPaneAdvancedReshapeViewModelBase()
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

	private string _heading = "Reshape Options";

	private ReshapeToolOptions _options;
	private CentralizableSettingViewModel<bool> _remainInSketchMode;
	private CentralizableSettingViewModel<bool> _showPreview;
	private CentralizableSettingViewModel<bool> _hideEditedFeatures;
	private CentralizableSettingViewModel<bool> _moveOpenJawEndJunction;
	private CentralizableSettingViewModel<bool> _alwaysShowOptionsPane;

	public string Heading
	{
		get { return _heading; }
		set { SetProperty(ref _heading, value, () => Heading); }
	}

	public CentralizableSettingViewModel<bool> RemainInSketchMode
	{
		get => _remainInSketchMode;
		set => SetProperty(ref _remainInSketchMode, value);
	}

	public CentralizableSettingViewModel<bool> ShowPreview
	{
		get => _showPreview;
		set => SetProperty(ref _showPreview, value);
	}

	public CentralizableSettingViewModel<bool> HideEditedFeatures
	{
		get => _hideEditedFeatures;
		set => SetProperty(ref _hideEditedFeatures, value);
	}

	public CentralizableSettingViewModel<bool> MoveOpenJawEndJunction
	{
		get => _moveOpenJawEndJunction;
		set => SetProperty(ref _moveOpenJawEndJunction, value);
	}

	public CentralizableSettingViewModel<bool> AlwaysShowOptionsPane
	{
		get => _alwaysShowOptionsPane;
		set => SetProperty(ref _alwaysShowOptionsPane, value);
	}

	public ReshapeToolOptions Options
	{
		get { return _options; }
		set
		{
			SetProperty(ref _options, value);

			RemainInSketchMode =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableRemainInSketchMode);
			ShowPreview =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableShowPreview);
			HideEditedFeatures =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableHideEditedFeatures);
			MoveOpenJawEndJunction =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableMoveOpenJawEndJunction);

			AlwaysShowOptionsPane =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableAlwaysShowOptionsPane);
		}
	}

	protected override Control CreateView()
	{
		return new DockPaneAdvancedReshape();
	}
}
