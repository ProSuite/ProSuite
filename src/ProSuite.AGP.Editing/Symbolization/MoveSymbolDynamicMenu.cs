using System;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Symbolization;

/// <summary>
/// The context menu for the <see cref="MoveSymbolToolBase"/>.
/// Override <see cref="AppendMenuItems"/> for more menu items.
/// Declare this class or a subclass as a dynamicMenu in the DAML.
/// </summary>
public class MoveSymbolDynamicMenu : DynamicMenu
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected virtual void AppendMenuItems() { }

	protected override void OnPopup()
	{
		try
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug($"{nameof(MoveSymbolToolBase)}: popup menu opening");
			}

			var activeTool = FrameworkApplication.ActiveTool;
			var moveTool = activeTool as MoveSymbolToolBase;

			Add("Reset Move", "",
			    false,
			    moveTool is not null,
			    false,
			    ResetOverrides);

			AppendMenuItems();
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

			if (activeTool is MoveSymbolToolBase moveSymbolTool)
			{
				await moveSymbolTool.ResetMoveOverride();
			}
			else
			{
				_msg.Debug($"ActiveTool is not {nameof(MoveSymbolToolBase)}");
			}
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
	}
}
