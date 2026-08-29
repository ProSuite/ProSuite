using System;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

/// <summary>
/// Sets the construction sketch symbol based on the current editing template's
/// target layer (as opposed to <see cref="SymbolizedSketchTypeBasedOnSelection"/>,
/// which derives the symbol from the current map selection). This is the correct
/// strategy for create-from-template tools such as the "Create Multiple Points" tool:
/// the sketch should look like the features that are about to be created, no matter
/// what happens to be selected in the map.
/// <para>
/// The symbol is resolved from the target layer's renderer using the template's
/// configured default attribute values, so unique-value renderers resolve to the
/// symbol that matches the template.
/// </para>
/// <para>
/// Only takes effect when "Show feature symbology in sketch" (Options &gt; Editing)
/// is turned on; otherwise the sketch symbol is cleared.
/// </para>
/// </summary>
public class SymbolizedSketchTypeBasedOnTemplate : ISymbolizedSketchType
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly ISymbolizedSketchTool _tool;

	public SymbolizedSketchTypeBasedOnTemplate([NotNull] ISymbolizedSketchTool tool)
	{
		_tool = tool;
	}

	public void Dispose()
	{
		_ = ClearSketchSymbol();
	}

	public async Task ClearSketchSymbol()
	{
		_tool.SetSketchSymbol(null);

		await ApplySketchSymbolWorkAround();
	}

	/// <summary>
	/// Must be called on the MCT.
	/// </summary>
	public async Task SetSketchAppearanceAsync()
	{
		_msg.VerboseDebug(() => nameof(SetSketchAppearanceAsync));

		try
		{
			if (! ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology)
			{
				_msg.Debug(
					"Cannot set sketch symbol. Show feature symbology in sketch is turned off.");
				await ClearSketchSymbol();
				return;
			}

			EditingTemplate template = EditingTemplate.Current;
			FeatureLayer targetLayer = ToolUtils.CurrentTargetLayer(template);

			if (targetLayer == null)
			{
				_msg.Debug("Cannot set sketch symbol. No target layer for the current template.");
				await ClearSketchSymbol();
				return;
			}

			GeometryType geometryType =
				GeometryUtils.TranslateEsriGeometryType(targetLayer.ShapeType);

			if (! await _tool.CanSetConstructionSketchSymbol(geometryType))
			{
				_msg.Debug($"Cannot set sketch symbol for geometry type {geometryType}");
				await ClearSketchSymbol();
				return;
			}

			CIMSymbolReference symbolReference = GetSymbolReference(template, targetLayer);

			if (symbolReference == null)
			{
				await ClearSketchSymbol();
				return;
			}

			_tool.SetSketchSymbol(symbolReference);

			await ApplySketchSymbolWorkAround();
		}
		catch (Exception ex)
		{
			_msg.Debug($"Error in {nameof(SetSketchAppearanceAsync)}: {ex.Message}", ex);
		}
	}

	public Task SelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		// The sketch symbol is driven by the current template, not by the selection.
		return Task.CompletedTask;
	}

	[CanBeNull]
	private static CIMSymbolReference GetSymbolReference([NotNull] EditingTemplate template,
	                                                     [NotNull] FeatureLayer targetLayer)
	{
		Map activeMap = MapView.Active?.Map;
		if (activeMap == null)
		{
			return null;
		}

		CIMRenderer renderer = targetLayer.GetRenderer();
		if (renderer == null)
		{
			_msg.Debug($"Cannot set sketch symbol: layer {targetLayer.Name} has no renderer.");
			return null;
		}

		double scaleDenom = activeMap.ReferenceScale;

		// Resolve the symbol for the template's configured default attribute values so that
		// unique-value renderers pick the class that matches the template.
		INamedValues templateValues = new TemplateNamedValues(template);

		try
		{
			return SymbolUtils.GetSymbol(renderer, templateValues, scaleDenom);
		}
		catch (Exception ex)
		{
			// e.g. renderer type not supported by SymbolUtils.GetSymbol
			_msg.Debug(
				$"Cannot set sketch symbol from renderer of layer {targetLayer.Name}: {ex.Message}",
				ex);
			return null;
		}
	}

	private async Task ApplySketchSymbolWorkAround()
	{
		MapView mapView = MapView.Active;
		if (mapView == null)
		{
			return;
		}

		Geometry sketch = await mapView.GetCurrentSketchAsync();

		// Clearing the sketch is needed to make the (new) sketch symbol take effect. Unlike
		// the selection-based variant we deliberately preserve and restore any in-progress
		// sketch: a template change must re-symbolize the existing points, not discard them.
		await mapView.ClearSketchAsync();

		if (sketch?.IsEmpty == false)
		{
			await mapView.SetCurrentSketchAsync(sketch);
		}
	}

	#region Nested type: TemplateNamedValues

	/// <summary>
	/// Exposes an editing template's configured default attribute values as
	/// <see cref="INamedValues"/> so a renderer can resolve the matching symbol.
	/// Must be accessed on the MCT.
	/// </summary>
	private class TemplateNamedValues : INamedValues
	{
		[CanBeNull] private readonly EditingTemplate _template;

		public TemplateNamedValues([CanBeNull] EditingTemplate template)
		{
			_template = template;
		}

		public bool Exists(string name)
		{
			return name != null && EditorUtils.TryGetDefaultValue(_template, name, out _);
		}

		public object GetValue(string name)
		{
			return EditorUtils.TryGetDefaultValue(_template, name, out object value)
				       ? value
				       : null;
		}
	}

	#endregion
}
