using System;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

public abstract class AvoidSLMWithoutSLDCheckBoxBase : CheckBox
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected virtual ISymbolLayerDisplay Manager => SymbolLayerDisplayManager.Instance;

	protected AvoidSLMWithoutSLDCheckBoxBase()
	{
		ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);

		Refresh();
	}

	private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
	{
		Refresh(); // TODO Bug! depends on event ordering, which is undefined
	}

	private void Refresh()
	{
		try
		{
			IsChecked = Manager.NoMaskingWithoutSLD;
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	protected override void OnClick()
	{
		try
		{
			Manager.NoMaskingWithoutSLD = IsChecked ?? false;
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}
}
