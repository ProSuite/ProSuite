using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

public static class DisplayUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	/// <summary>
	/// Get if given <paramref name="map"/> has Layer Masking
	/// enabled (true) or not (false). For Pro before 3.3 this
	/// was not possible and this function returns null.
	/// </summary>
	/// <remarks>Must run on MCT</remarks>
	public static bool? UsesLayerMasking([CanBeNull] Map map)
	{
		if (map is null) return null;
		var cim = map.GetDefinition();
		const string propertyName = "UseMasking";
		// Use reflection: property only exists since Pro 3.3
		var property = cim.GetType().GetProperty(propertyName);
		object value = property?.GetValue(cim);
		return value is bool flag ? flag : null;
	}

	/// <summary>
	/// Turn Layer Masking (LM) on the given map on or off.
	/// </summary>
	/// <returns>true iff any layer or map was modified</returns>
	public static bool ToggleLayerMasking(Map map, bool turnOn)
	{
		if (map is null) return false;

		var cim = map.GetDefinition();
		const string propertyName = "UseMasking";
		// Use reflection: property only exists since Pro 3.3
		var property = cim.GetType().GetProperty(propertyName);

		if (property is null)
		{
			_msg.Debug($"No property {propertyName} on CIMMap (probably on old " +
			           $"version of ArcGIS Pro): must use a hack to toggle LM");
			return ToggleLayerMaskingHack(map, turnOn);
		}

		// Good: our ArcGIS Pro has the UseMasking CIM property:
		var currentValue = property.GetValue(cim);
		if (currentValue is bool flag && flag == turnOn)
		{
			// current setting is already good: avoid the expensive SetDefinition call
			return false;
		}

		property.SetValue(cim, turnOn);
		map.SetDefinition(cim);

		return true;
	}

	/// <summary>
	/// Turn Layer Masking (LM) on the given layer container
	/// (map or group layer) on or off. This is not officially
	/// possible in ArcGIS Pro, and we use a dirty hack, but
	/// the feature has been requested in Redlands in 2023.
	/// </summary>
	/// <returns>true iff any layer was modified</returns>
	private static bool ToggleLayerMaskingHack(ILayerContainer container, bool turnOn)
	{
		const string prefix = "_SKIP_";

		var layers = GetLayersInOrder(container);
		bool anyModified = false;

		foreach (var layer in layers)
		{
			if (layer is not FeatureLayer && layer is not GroupLayer)
			{
				continue; // layer type not relevant for LM TODO other layer types *can* have layer masks!
			}

			var cim = layer.GetDefinition();
			if (cim.LayerMasks is null || cim.LayerMasks.Length <= 0)
			{
				continue; // layer has no LM configured
			}

			bool modified = false;

			for (int i = 0; i < cim.LayerMasks.Length; i++)
			{
				var uri = cim.LayerMasks[i] ?? "";

				if (turnOn)
				{
					// remove prefix to revalidate mask layer references
					if (uri.StartsWith(prefix))
						uri = uri.Substring(prefix.Length);
				}
				else
				{
					// add prefix to invalidate mask layer references
					if (! uri.StartsWith(prefix))
						uri = string.Concat(prefix, uri);
				}

				if (! string.Equals(cim.LayerMasks[i], uri, StringComparison.Ordinal))
				{
					cim.LayerMasks[i] = uri;
					modified = true;
				}
			}

			if (modified)
			{
				layer.SetDefinition(cim);
				anyModified = true;
			}
		}

		return anyModified;
	}

	/// <summary>
	/// Return true iff the given map uses SLD on any layer
	/// (therefore this method traverses the layer stack).
	/// </summary>
	public static bool UsesSLD([CanBeNull] Map map)
	{
		if (map is null) return false;

		var layers = map.GetLayersAsFlattenedList();

		foreach (var layer in layers)
		{
			if (layer is GroupLayer groupLayer)
			{
				var cim = groupLayer.GetDefinition();
				if (cim is CIMGroupLayer cgl && cgl.SymbolLayerDrawing != null &&
				    cgl.SymbolLayerDrawing.UseSymbolLayerDrawing)
				{
					return true;
				}
			}
			else if (layer is FeatureLayer featureLayer)
			{
				var cim = featureLayer.GetDefinition();
				if (cim is CIMFeatureLayer cfl && cfl.SymbolLayerDrawing != null &&
				    cfl.SymbolLayerDrawing.UseSymbolLayerDrawing)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Return true iff the given layer CIM uses SLD.
	/// Only Feature Layers and Group Layers can use SLD.
	/// </summary>
	public static bool UsesSLD(this CIMBaseLayer cimLayer)
	{
		CIMSymbolLayerDrawing sld = null;

		if (cimLayer is CIMGeoFeatureLayerBase cimFeatureLayer)
		{
			sld = cimFeatureLayer.SymbolLayerDrawing;
		}
		else if (cimLayer is CIMGroupLayer cimGroupLayer)
		{
			sld = cimGroupLayer.SymbolLayerDrawing;
		}

		return sld?.UseSymbolLayerDrawing ?? false;
	}

	/// <summary>
	/// Turn Symbol Layer Drawing (SLD) on the given layer container
	/// (map or group layer) on or off. Do so by setting the layer CIM's
	/// UseSymbolLayerDrawing attribute to true or false.
	/// </summary>
	/// <returns>true iff any layer was modified</returns>
	public static bool ToggleSymbolLayerDrawing(ILayerContainer container, bool turnOn)
	{
		return turnOn
			       ? TurnSymbolLayerDrawingOn(container)
			       : TurnSymbolLayerDrawingOff(container);
	}

	/// <summary>
	/// Turn on a previously configured Symbol Layer Drawing (SLD)
	/// by setting UseSymbolLayerDrawing to true on group layers and
	/// feature layers (but not if nested within a controlling layer).
	/// </summary>
	/// <param name="container">a map or a group layer</param>
	private static bool TurnSymbolLayerDrawingOn(ILayerContainer container)
	{
		bool anyModified = false;
		var layers = GetLayersInOrder(container);

		GroupLayer controllingLayer = null;

		// This loop assumes layers are on pre-order:
		foreach (var layer in layers)
		{
			// Because SLD controlling layers are never nested (outermost
			// group layer wins), a scalar suffices (no need for a stack)

			if (! IsNestedLayer(layer, controllingLayer))
			{
				controllingLayer = null;
			}

			if (controllingLayer is not null)
			{
				continue;
			}

			if (layer is GroupLayer groupLayer)
			{
				var cim = layer.GetDefinition();
				if (cim is CIMGroupLayer { SymbolLayerDrawing.SymbolLayers.Length: > 0 } groupCim)
				{
					// Controlling group layer:
					controllingLayer = groupLayer;

					if (! groupCim.SymbolLayerDrawing.UseSymbolLayerDrawing)
					{
						groupCim.SymbolLayerDrawing.UseSymbolLayerDrawing = true;
						layer.SetDefinition(cim);
						anyModified = true;
					}
				}
			}
			else if (layer is FeatureLayer)
			{
				var cim = layer.GetDefinition();
				if (cim is CIMGeoFeatureLayerBase
				    {
					    SymbolLayerDrawing.SymbolLayers.Length: > 0
				    } layerCim)
				{
					// Feature layer with SLD outside a controlling group layer:
					if (! layerCim.SymbolLayerDrawing.UseSymbolLayerDrawing)
					{
						layerCim.SymbolLayerDrawing.UseSymbolLayerDrawing = true;
						layer.SetDefinition(cim);
						anyModified = true;
					}
				}
			}
		}

		return anyModified;
	}

	/// <summary>
	/// Just set UseSymbolLayerDrawing to false on all layers
	/// that have SymbolLayerDrawing in their CIM definition.
	/// </summary>
	private static bool TurnSymbolLayerDrawingOff(ILayerContainer container)
	{
		var layers = GetLayersInOrder(container);
		bool anyModified = false;

		foreach (var layer in layers)
		{
			if (layer is not FeatureLayer && layer is not GroupLayer)
			{
				continue; // layer type not relevant for SLD
			}

			var cim = layer.GetDefinition();
			if (cim is CIMGeoFeatureLayerBase
			    {
				    SymbolLayerDrawing.SymbolLayers.Length: > 0
			    } layerCim)
			{
				if (layerCim.SymbolLayerDrawing.UseSymbolLayerDrawing)
				{
					layerCim.SymbolLayerDrawing.UseSymbolLayerDrawing = false;
					layer.SetDefinition(cim);
					anyModified = true;
				}
			}
			else if (cim is CIMGroupLayer { SymbolLayerDrawing.SymbolLayers.Length: > 0 } groupCim)
			{
				if (groupCim.SymbolLayerDrawing.UseSymbolLayerDrawing)
				{
					groupCim.SymbolLayerDrawing.UseSymbolLayerDrawing = false;
					layer.SetDefinition(cim);
					anyModified = true;
				}
			}
			// else: wrong layer type or strange CIM (no SLD)
		}

		return anyModified;
	}

	/// <summary>
	/// Return true iff <paramref name="candidate"/>
	/// is nested within <paramref name="container"/> at any depth.
	/// </summary>
	// Note: to allow the Map as container, have overload with ILayerContainer
	public static bool IsNestedLayer(this Layer candidate, Layer container)
	{
		if (candidate is null)
			throw new ArgumentNullException(nameof(candidate));

		if (container is not ILayerContainer) return false;

		while (candidate.Parent as Layer is { } parent)
		{
			if (IsSameLayer(parent, container)) return true;
			candidate = parent;
		}

		return false;
	}

	public static bool IsSameLayer(MapMember a, MapMember b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		// Same Layer URI within same Map:
		return string.Equals(a.URI, b.URI, StringComparison.OrdinalIgnoreCase) &&
		       string.Equals(a.Map.URI, b.Map.URI, StringComparison.OrdinalIgnoreCase);
	}

	/// <returns>All layers in pre-order, including <paramref name="container"/>
	/// (if it's a group layer); may be empty, but never null</returns>
	private static IEnumerable<Layer> GetLayersInOrder(ILayerContainer container)
	{
		if (container is null)
		{
			yield break;
		}

		if (container is GroupLayer groupLayer)
		{
			yield return groupLayer;
		}

		// Assume Pro API yields layers in pre-order: this is reasonable
		// and likely, but not documented; Charlie from Esri Inc. writes:
		// “that's a safe assumption I reckon” (2024-02-21)
		foreach (var layer in container.GetLayersAsFlattenedList())
		{
			yield return layer;
		}
	}
}
