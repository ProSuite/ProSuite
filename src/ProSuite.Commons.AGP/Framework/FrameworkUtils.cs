using System;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Framework;

public static class FrameworkUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public static void ToggleState(string stateId, bool activate)
	{
		if (string.IsNullOrEmpty(stateId))
			throw new ArgumentNullException(nameof(stateId));

		if (_msg.IsVerboseDebugEnabled)
		{
			var action = activate ? "Activate" : "Deactivate";
			_msg.VerboseDebug($"{action} state {stateId}");
		}

		if (activate)
		{
			FrameworkApplication.State.Activate(stateId);
		}
		else
		{
			FrameworkApplication.State.Deactivate(stateId);
		}
	}
}
