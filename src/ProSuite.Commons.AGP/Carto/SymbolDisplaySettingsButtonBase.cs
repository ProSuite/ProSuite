using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core;
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

		var result = Gateway.ShowDialog<SymbolDisplaySettingsWindow>(viewModel);

		if (result == true)
		{
			manager.NoMaskingWithoutSLD[map] = viewModel.AvoidSLMWithoutSLD;
			manager.AutoSwitch[map] = viewModel.UseScaleRange;
			manager.AutoMaxScaleDenom[map] = viewModel.MaxScaleDenominator;
			manager.AutoMinScaleDenom[map] = viewModel.MinScaleDenominator;
			Project.Current?.SetDirty();
		}

		return Task.FromResult(result ?? false);
	}
}
