using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public abstract class DockPaneDestroyAndRebuildViewModelBase : DockPaneViewModelBase
{
	protected DockPaneDestroyAndRebuildViewModelBase()
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

	private string _heading = "Destroy And Rebuild Options";

	private DestroyAndRebuildToolOptions _options;
	private CentralizableSettingViewModel<bool> _highlightOriginalGeometry;
	private CentralizableSettingViewModel<bool> _hideEditedFeature;
	private CentralizableSettingViewModel<bool> _moveOpenJawEndJunction;

	public string Heading
	{
		get { return _heading; }
		set { SetProperty(ref _heading, value, () => Heading); }
	}

	public CentralizableSettingViewModel<bool> HighlightOriginalGeometry
	{
		get => _highlightOriginalGeometry;
		set => SetProperty(ref _highlightOriginalGeometry, value);
	}

	public CentralizableSettingViewModel<bool> HideEditedFeature
	{
		get => _hideEditedFeature;
		set => SetProperty(ref _hideEditedFeature, value);
	}

	public CentralizableSettingViewModel<bool> MoveOpenJawEndJunction
	{
		get => _moveOpenJawEndJunction;
		set => SetProperty(ref _moveOpenJawEndJunction, value);
	}

	public DestroyAndRebuildToolOptions Options
	{
		get { return _options; }
		set
		{
			SetProperty(ref _options, value);

			HighlightOriginalGeometry =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableHighlightOriginalGeometry);
			HideEditedFeature =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableHideEditedFeature);
			MoveOpenJawEndJunction =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableMoveOpenJawEndJunction);
		}
	}

	protected override Control CreateView()
	{
		return new DockPaneDestroyAndRebuild();
	}
}
