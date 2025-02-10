using System;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;
using CheckBox = ArcGIS.Desktop.Framework.Contracts.CheckBox;

namespace ProSuite.Commons.AGP.Carto;

public abstract class AvoidSLMWithoutSLDCheckBoxBase : CheckBox
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private bool? _lastState;

	protected virtual ISymbolDisplayManager Manager => SymbolDisplayManager.Instance;

	protected override void OnClick()
	{
		try
		{
			var map = MapView.Active?.Map;
			Manager.NoMaskingWithoutSLD[map] = IsChecked ?? false;
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	protected override void OnUpdate()
	{
		try
		{
			var map = MapView.Active?.Map;

			var state = Manager.NoMaskingWithoutSLD[map];

			if (state == _lastState)
			{
				return; // avoid overhead if state did not change
			}

			_lastState = state;

			IsChecked = state;
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}
}
