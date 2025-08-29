using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

// todo 3D, test multipatch sketch symbol!
public class SymbolizedSketchTypeBasedOnSelection : ISymbolizedSketchType
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly ISymbolizedSketchTool _tool;
	private bool _showFeatureSketchSymbology;
	private readonly Func<SketchGeometryType> _sketchGeometryTypeFunc;

	/// <summary>
	/// Sets sketch geometry type based on current selection.
	/// Also set sketch symbol bases on current selection if
	/// "Show feature symbology in sketch" (Options > Editing) is turned on.
	/// Annotation feature is not supported. Takes the first feature of the
	/// first FeatureLayer if many features are selected from many FeatureLayers.
	/// </summary>
	/// <param name="tool"></param>
	/// <param name="sketchType">Optional sketch type method that replaces the default sketch type</param>
	public SymbolizedSketchTypeBasedOnSelection([NotNull] ISymbolizedSketchTool tool,
	                                            Func<SketchGeometryType> sketchType = null)
	{
		_tool = tool;
		_sketchGeometryTypeFunc = sketchType;

		_showFeatureSketchSymbology = ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology;

		SketchModifiedEvent.Subscribe(OnSketchModified);
		MapSelectionChangedEvent.Subscribe(OnMapSelectionChangedAsync);
	}

	public void Dispose()
	{
		SketchModifiedEvent.Unsubscribe(OnSketchModified);
		MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChangedAsync);

		_ = ClearSketchSymbol();
	}

	public async Task ClearSketchSymbol()
	{
		_tool.SetSketchSymbol(null);

		// This is needed to make the sketch symbol take effect.
		// It likely triggers the relevant events internally...
		await MapView.Active.ClearSketchAsync();
	}

	/// <summary>
	/// Must be called on the MCT.
	/// </summary>
	public async Task SetSketchAppearanceAsync()
	{
		_msg.VerboseDebug(() => nameof(SetSketchAppearanceAsync));

		try
		{
			var selection = SelectionUtils.GetSelection<BasicFeatureLayer>(MapView.Active.Map);
			List<long> oids = GetApplicableSelection(selection, out FeatureLayer featureLayer);

			await TrySetSketchAppearanceAsync(featureLayer, oids);
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message, ex);
		}
	}

	/// <summary>
	/// Is always on MCT.
	/// </summary>
	private async void OnSketchModified(SketchModifiedEventArgs args)
	{
		try
		{
			if (ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology ==
			    _showFeatureSketchSymbology)
			{
				return;
			}

			_showFeatureSketchSymbology =
				ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology;

			var selection = SelectionUtils.GetSelection<BasicFeatureLayer>(MapView.Active.Map);
			List<long> oids = GetApplicableSelection(selection, out FeatureLayer featureLayer);

			// only set sketch symbol not sketch type!
			await TrySetSketchAppearanceAsync(featureLayer, oids);
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message, ex);
		}
	}

	/// <summary>
	/// Is always on worker thread.
	/// </summary>
	private async void OnMapSelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		Gateway.LogEntry(_msg);

		try
		{
			var selection = SelectionUtils.GetSelection<BasicFeatureLayer>(args.Selection);

			await QueuedTask.Run(async () =>
			{
				List<long> oids = GetApplicableSelection(selection, out FeatureLayer featureLayer);

				await TrySetSketchAppearanceAsync(featureLayer, oids);
			});
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message, ex);
		}
	}

	private async Task TrySetSketchAppearanceAsync([CanBeNull] FeatureLayer featureLayer,
	                                               [CanBeNull] IList<long> oids)
	{
		if (featureLayer == null || oids == null)
		{
			await ClearSketchSymbol();
			return;
		}

		GeometryType geometryType = GeometryUtils.TranslateEsriGeometryType(featureLayer.ShapeType);

		if (ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology)
		{
			if (await _tool.CanSetConstructionSketchSymbol(geometryType))
			{
				await SetSketchSymbol(GetSymbolReference(featureLayer, oids));
			}
			else
			{
				_msg.Debug($"Cannot set sketch symbol for geometry type {geometryType}");
				await ClearSketchSymbol();
			}
		}
		else
		{
			_msg.Debug(
				"Cannot set sketch symbol. Show feature symbology in sketch is turned off.");
			await ClearSketchSymbol();
		}
	}

	private async Task SetSketchSymbol(CIMSymbolReference symbolReference)
	{
		_tool.SetSketchSymbol(symbolReference);

		// This is needed to make the sketch symbol take effect.
		// It likely triggers the relevant events internally...
		await MapView.Active.ClearSketchAsync();
	}

	public void SetSketchType(BasicFeatureLayer featureLayer)
	{
		if (featureLayer == null)
		{
			return;
		}

		if (_sketchGeometryTypeFunc != null)
		{
			_tool.SetSketchType(_sketchGeometryTypeFunc());
		}
		else
		{
			GeometryType geometryType =
				GeometryUtils.TranslateEsriGeometryType(featureLayer.ShapeType);
			_tool.SetSketchType(ToolUtils.GetSketchGeometryType(geometryType));
		}
	}

	private List<long> GetApplicableSelection(
		[NotNull] IDictionary<BasicFeatureLayer, List<long>> selection,
		[CanBeNull] out FeatureLayer featureLayer)
	{
		featureLayer = null;

		if (selection.Count <= 0)
		{
			return null;
		}

		var oidsByLayer = SelectionUtils.GetApplicableSelection(selection, _tool.CanSelectFromLayer)
		                                .ToList();

		int layerCount = oidsByLayer.Count;

		if (layerCount > 1)
		{
			_msg.Debug(
				$"Features from {layerCount} different layers selected. Take the first layer.");
		}

		if (! _tool.CanUseSelection(oidsByLayer.ToDictionary(pair => pair.Key, pair => pair.Value)))
		{
			_msg.Debug("Cannot use selection");
			return null;
		}

		(BasicFeatureLayer layer, List<long> oids) = oidsByLayer.FirstOrDefault();

		if (layer is not FeatureLayer featLayer)
		{
			_msg.Debug(
				"Cannot set sketch symbol. No feature selected or no applicable selection from FeatureLayer");
			return null;
		}

		featureLayer = featLayer;
		return oids;
	}

	[CanBeNull]
	private static CIMSymbolReference GetSymbolReference([NotNull] FeatureLayer layer,
	                                                     [CanBeNull] IList<long> oids)
	{
		if (oids == null || oids.Count < 1)
		{
			return null;
		}

		CIMSymbolReference symbolReference = null;

		var activeMap = MapView.Active?.Map;
		if (activeMap is null)
		{
			return null;
		}

		var scaleDenom = activeMap.ReferenceScale;
		var renderer = layer.GetRenderer();

		IList<CIMSymbolReference> cimSymbolReferences = new List<CIMSymbolReference>();
		//IList<CIMSymbol> cimSymbols = new List<CIMSymbol>();

		foreach (long oid in oids)
		{
			//CIMSymbol oidLookupSymbol = layer.LookupSymbol(oid, MapView.Active);
			//cimSymbols.Add(oidLookupSymbol);

			var feature = GetFeature(layer, oid);
			//var shape = feature.GetShape();
			var values = new NamedValues(feature);
			CIMSymbolReference symref = SymbolUtils.GetSymbol(renderer, values, scaleDenom, out _);

			if (cimSymbolReferences.All(s => s.ToJson() != symref.ToJson()))
			{
				cimSymbolReferences.Add(symref);
			}
		}

		//all selected has same Symbol
		if (cimSymbolReferences.Count == 1)
		{
			symbolReference = cimSymbolReferences[0];
		}

		if (symbolReference == null)
		{
			if (oids.Count == 1)
			{
				_msg.Debug(
					$"Cannot set sketch symbol: no symbol found in layer {layer.Name} for oid {oids[0]}.");
			}
			else
			{
				_msg.Debug(
					$"Cannot set sketch symbol: found different symbols in selection of in layer {layer.Name}.");
			}

			return null;
		}

		//return symbol.MakeSymbolReference();
		return symbolReference;
	}

	private static Feature GetFeature(FeatureLayer layer, long oid)
	{
		using var featureClass = layer.GetFeatureClass();
		if (featureClass is null) return null;
		return GdbQueryUtils.GetFeature(featureClass, oid);
	}

	#region Nestsed type: NamedValues

	private class NamedValues : INamedValues
	{
		private readonly Row _row;

		public NamedValues(Row row)
		{
			_row = row ?? throw new ArgumentNullException(nameof(row));
		}

		public bool Exists(string name)
		{
			return name is not null && _row.FindField(name) >= 0;
		}

		public object GetValue(string name)
		{
			return _row[name];
		}
	}

	#endregion
}
