using System;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Symbolization;

/// <summary>
/// The context menu for the <see cref="RotateSymbolToolBase"/>.
/// Override <see cref="AppendMenuItems"/> for more menu items.
/// Declare this class or a subclass as a dynamicMenu in the DAML.
/// </summary>
public class RotateSymbolDynamicMenu : DynamicMenu
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected virtual void AppendMenuItems() { }

	protected override void OnPopup()
	{
		try
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug($"{nameof(RotateSymbolToolBase)}: popup menu opening");
			}

			var activeTool = FrameworkApplication.ActiveTool;
			var rotateTool = activeTool as RotateSymbolToolBase;

			Add("Additive rotation", "",
			    rotateTool?.RotationKind == SymbolRotationKind.Additive,
			    rotateTool is not null,
			    false,
			    SetRotationKind, SymbolRotationKind.Additive);

			Add("Absolute rotation", "",
			    rotateTool?.RotationKind == SymbolRotationKind.Absolute,
			    rotateTool is not null,
			    false,
			    SetRotationKind, SymbolRotationKind.Absolute);

			AddSeparator();

			Add("Reset Rotation", "",
			    false,
			    rotateTool is not null,
			    false,
			    ResetOverrides);

			AppendMenuItems();
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
	}

	private static async void SetRotationKind(SymbolRotationKind kind)
	{
		try
		{
			var activeTool = FrameworkApplication.ActiveTool;

			if (activeTool is RotateSymbolToolBase rotateSymbolTool)
			{
				await rotateSymbolTool.SetRotationKind(kind);
			}
			else
			{
				_msg.Debug($"ActiveTool is not {nameof(RotateSymbolToolBase)}");
			}
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
	}

	private static async void ResetOverrides()
	{
		try
		{
			var activeTool = FrameworkApplication.ActiveTool;

			if (activeTool is RotateSymbolToolBase rotateSymbolTool)
			{
				await rotateSymbolTool.ResetRotationOverride();
			}
			else
			{
				_msg.Debug($"ActiveTool is not {nameof(RotateSymbolToolBase)}");
			}
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
	}
}
