using System;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.Commons.AGP.Carto;

public abstract class SymbolDisplaySettingsButtonBase : ButtonCommandBase
{
	protected virtual ISymbolDisplayManager Manager => SymbolDisplayManager.Instance;

	protected override Task<bool> OnClickAsyncCore()
	{
		var map = MapView.Active?.Map; // may be null
		var scopeHint = map is null
			                ? "Default settings"
			                : $"Settings for current map “{map.Name}”";

		var manager = Manager ?? throw new InvalidOperationException();

		var viewModel = new SymbolDisplaySettingsViewModel
		                {
							ScopeMessage = scopeHint,
			                AvoidSLMWithoutSLD = manager.NoMaskingWithoutSLD[map],
			                UseScaleRange = manager.AutoSwitch[map],
			                MinScaleDenominator = manager.AutoMinScaleDenom[map],
			                MaxScaleDenominator = manager.AutoMaxScaleDenom[map]
		                };

	var result = ShowDialog<SymbolDisplaySettingsWindow>(viewModel);

		if (result == true)
		{
			manager.NoMaskingWithoutSLD[map] = viewModel.AvoidSLMWithoutSLD;
			manager.AutoSwitch[map] = viewModel.UseScaleRange;
			manager.AutoMaxScaleDenom[map] = viewModel.MaxScaleDenominator;
			manager.AutoMinScaleDenom[map] = viewModel.MinScaleDenominator;
		}

		return Task.FromResult(result ?? false);
	}

	// TODO Should be on Gateway, but there's already another function with the same signature (which passes VM to V's ctor, a quick hack that could/should be resolved)
	private static bool? ShowDialog<T>(object viewModel) where T : Window
	{
		var dispatcher = Application.Current.Dispatcher;

		return dispatcher.Invoke(() =>
		{
			var owner = Application.Current.MainWindow;
			var dialog = (Window)Activator.CreateInstance(typeof(T));
			if (dialog is null) return null;
			dialog.Owner = owner;
			dialog.DataContext = viewModel;
			var result = dialog.ShowDialog();
			return result;
		});
	}
}
