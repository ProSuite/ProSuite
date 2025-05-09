using System;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
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
			_msg.VerboseDebug(() => $"{action} state {stateId}");
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

	public static void Activate([NotNull] string stateId)
	{
		if (string.IsNullOrEmpty(stateId))
			throw new ArgumentNullException(nameof(stateId));

		FrameworkApplication.State.Activate(stateId);
	}

	public static void Deactivate([NotNull] string stateId)
	{
		if (string.IsNullOrEmpty(stateId))
			throw new ArgumentNullException(nameof(stateId));

		FrameworkApplication.State.Deactivate(stateId);
	}

	public static bool IsStateActive([NotNull] string stateId)
	{
		if (string.IsNullOrEmpty(stateId))
			throw new ArgumentNullException(nameof(stateId));

		return FrameworkApplication.State.Contains(stateId);
	}

	public static bool AnySketchOperations()
	{
		var mapView = MapView.Active;
		if (mapView is null) return false;

		OperationManager operationManager = mapView.Map.OperationManager;
		return operationManager
		       .FindUndoOperations(operation => operation.Category == "SketchOperations").Count > 0;
	}
}
