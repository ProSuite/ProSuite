using System.Windows.Controls;
using System.Windows.Input;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Editing.Erase;

public abstract class DockPaneEraseViewModelBase : DockPaneViewModelBase
{
	protected DockPaneEraseViewModelBase()
	{
		RevertToDefaultsCommand = new RelayCommand(RevertToDefaults, () => true);
	}

	#region RestoreDefaultsButton

	public ICommand RevertToDefaultsCommand { get; }

	public bool IsRevertToDefaultsEnabled => true;

	private void RevertToDefaults()
	{
		Options?.RevertToDefaults();
	}

	#endregion

	private string _heading = "Erase Options";

	private EraseToolOptions _options;

	private CentralizableSettingViewModel<bool> _allowPolylineErasing;
	private CentralizableSettingViewModel<bool> _allowMultipointErasing;
	private CentralizableSettingViewModel<bool> _preventMultipartResults;

	public string Heading
	{
		get => _heading;
		set { SetProperty(ref _heading, value, () => Heading); }
	}

	public CentralizableSettingViewModel<bool> AllowPolylineErasing
	{
		get => _allowPolylineErasing;
		set => SetProperty(ref _allowPolylineErasing, value);
	}

	public CentralizableSettingViewModel<bool> AllowMultipointErasing
	{
		get => _allowMultipointErasing;
		set => SetProperty(ref _allowMultipointErasing, value);
	}

	public CentralizableSettingViewModel<bool> PreventMultipartResults
	{
		get => _preventMultipartResults;
		set => SetProperty(ref _preventMultipartResults, value);
	}

	public EraseToolOptions Options
	{
		get => _options;
		set
		{
			SetProperty(ref _options, value);

			AllowPolylineErasing =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableAllowPolylineErasing);

			AllowMultipointErasing =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizableAllowMultipointErasing);

			PreventMultipartResults =
				new CentralizableSettingViewModel<bool>(
					Options.CentralizablePreventMultipartResults);
		}
	}

	protected override Control CreateView()
	{
		return new DockPaneErase();
	}
}
