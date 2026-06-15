using System;
using ArcGIS.Desktop.Core;
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

	/// <summary>
	/// Set the Fallback Tool of ArcGIS Pro to be the one
	/// specified by the given <paramref name="damlID"/>.
	/// </summary>
	/// <remarks>This is only supported in Pro 3.6 and newer; with
	/// older versions of Pro, this method does nothing. Once Pro
	/// 3.6 or newer is everywhere, this method SHALL be replaced
	/// by a direct assignment to the FallbackTool property.</remarks>
	public static void SetFallbackTool(string damlID)
	{
		var options = ApplicationOptions.EditingOptions;

		// Use reflection: property only exists since Pro 3.6
		const string propertyName = "FallbackTool";
		var property = options.GetType().GetProperty(propertyName);

		if (property is null)
		{
			_msg.Debug(
				$"No property {propertyName} on {nameof(ApplicationOptions.EditingOptions)}" +
				" (your version of ArcGIS Pro is probably before 3.6); ignoring request to" +
				$" set fallback tool to {damlID}");
		}
		else
		{
			property.SetValue(options, damlID);
		}
	}
}
