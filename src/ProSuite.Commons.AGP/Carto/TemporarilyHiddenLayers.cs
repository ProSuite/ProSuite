using System;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// Recognises layers that are switched off only for a moment, so that the user can look at
/// the map beneath them, for example by a command that hides all foreground layers. An edit
/// tool keeps using a selection it has already accepted from such a layer.
/// </summary>
public static class TemporarilyHiddenLayers
{
	[CanBeNull] private static Func<Layer, bool> _isTemporarilyHiddenLayer;

	/// <summary>
	/// Sets the function that recognises a temporarily hidden layer. Null removes it, and
	/// then no layer counts as temporarily hidden.
	/// </summary>
	public static void SetIsTemporarilyHiddenLayerFunc(
		[CanBeNull] Func<Layer, bool> isTemporarilyHiddenLayer)
	{
		_isTemporarilyHiddenLayer = isTemporarilyHiddenLayer;
	}

	/// <remarks>Must be called on the main CIM thread.</remarks>
	public static bool Contains([NotNull] Layer layer)
	{
		return _isTemporarilyHiddenLayer?.Invoke(layer) ?? false;
	}
}
