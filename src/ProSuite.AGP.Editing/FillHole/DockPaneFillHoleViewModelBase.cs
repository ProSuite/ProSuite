using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.AGP.Editing.FillHole;

public abstract class DockPaneFillHoleViewModelBase : DockPaneViewModelBase
{
	protected DockPaneFillHoleViewModelBase()
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

	private string _heading = "Fill Hole Options";

	private HoleToolOptions _options;

	private CentralizableSettingViewModel<bool> _showPreview;
	private CentralizableSettingViewModel<bool> _limitPreviewToExtent;
	private CentralizableSettingViewModel<bool> _alwaysShowOptionsPane;

	public string Heading
	{
		get => _heading;
		set { SetProperty(ref _heading, value, () => Heading); }
	}

	public CentralizableSettingViewModel<bool> ShowPreview
	{
		get => _showPreview;
		set { SetProperty(ref _showPreview, value); }
	}

	public CentralizableSettingViewModel<bool> LimitPreviewToExtent
	{
		get => _limitPreviewToExtent;
		set { SetProperty(ref _limitPreviewToExtent, value); }
	}

	public CentralizableSettingViewModel<bool> AlwaysShowOptionsPane
	{
		get => _alwaysShowOptionsPane;
		set { SetProperty(ref _alwaysShowOptionsPane, value); }
	}

	public HoleToolOptions Options
	{
		get => _options;
		set
		{
			SetProperty(ref _options, value);

			ShowPreview =
				new CentralizableSettingViewModel<bool>(Options.CentralizableShowPreview);

			LimitPreviewToExtent = new CentralizableSettingViewModel<bool>(
				Options.CentralizableLimitPreviewToExtent,
				new[] { Options.CentralizableShowPreview });

			AlwaysShowOptionsPane =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableAlwaysShowOptionsPane);
		}
	}

	protected override Control CreateView()
	{
		return new DockPaneFillHole();
	}
}
