using System;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto;

public abstract class SymbolDisplaySettingsButtonBase : ButtonCommandBase
{
	[NotNull]
	protected abstract ISymbolDisplayManager SymbolDisplayManager { get; }

	[CanBeNull]
	protected abstract IPerimeterDisplayManager PerimeterDisplayManager { get; }

	protected override Task<bool> OnClickAsyncCore()
	{
		var map = MapView.Active?.Map; // may be null
		var scopeHint = map is null
			                ? "Default settings"
			                : $"Settings for current map “{map.Name}”";

		var manager = SymbolDisplayManager ?? throw new InvalidOperationException();

		var viewModel = new SymbolDisplaySettingsViewModel
		                {
			                ScopeMessage = scopeHint,
			                AvoidSLMWithoutSLD = manager.NoMaskingWithoutSLD[map],
			                UseScaleRange = manager.AutoSwitch[map],
			                MinScaleDenominator = manager.AutoMinScaleDenom[map],
			                MaxScaleDenominator = manager.AutoMaxScaleDenom[map],
			                WantSLD = manager.WantSLD[map],
			                WantLM = manager.WantLM[map]
		                };

		RefreshPerimeterSettings(viewModel);

		var result = Gateway.ShowDialog<SymbolDisplaySettingsWindow>(viewModel);

		if (result == true)
		{
			manager.NoMaskingWithoutSLD[map] = viewModel.AvoidSLMWithoutSLD;
			manager.AutoSwitch[map] = viewModel.UseScaleRange;
			manager.AutoMaxScaleDenom[map] = viewModel.MaxScaleDenominator;
			manager.AutoMinScaleDenom[map] = viewModel.MinScaleDenominator;

			Project.Current?.SetDirty();

			UpdatePerimeterSettings(viewModel);
		}

		return Task.FromResult(result ?? false);
	}

	private void RefreshPerimeterSettings(SymbolDisplaySettingsViewModel viewModel)
	{
		var manager = PerimeterDisplayManager;

		if (manager is not null)
		{
			viewModel.WantPerimeter = manager.WantPerimeter;
			viewModel.PerimeterSettingsVisibility = Visibility.Visible;
		}
		else
		{
			viewModel.WantPerimeter = false;
			viewModel.PerimeterSettingsVisibility = Visibility.Collapsed;
		}

	}

	private void UpdatePerimeterSettings(SymbolDisplaySettingsViewModel viewModel)
	{
		var manager = PerimeterDisplayManager;

		if (manager is not null)
		{
			manager.WantPerimeter = viewModel.WantPerimeter;

			manager.Refresh();
		}
	}
}
