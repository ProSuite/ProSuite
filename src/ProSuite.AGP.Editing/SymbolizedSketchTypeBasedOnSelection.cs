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
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

// todo 3D, test multipatch sketch symbol!
public class SymbolizedSketchTypeBasedOnSelection : IDisposable
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly ISymbolizedSketchTool _tool;
	private bool _showFeatureSketchSymbology;

	/// <summary>
	/// Sets sketch geometry type based on current selection.
	/// Also set sketch symbol bases on current selection if
	/// "Show feature symbology in sketch" (Options > Editing) is turned on.
	/// Annotation feature is not supported. Takes the first feature of the
	/// first FeatureLayer if many features are selected from many FeatureLayers.
	/// </summary>
	/// <param name="tool"></param>
	public SymbolizedSketchTypeBasedOnSelection([NotNull] ISymbolizedSketchTool tool)
	{
		_tool = tool;

		_showFeatureSketchSymbology = ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology;

		SketchModifiedEvent.Subscribe(OnSketchModified);
		MapSelectionChangedEvent.Subscribe(OnMapSelectionChangedAsync);
	}

	public void Dispose()
	{
		SketchModifiedEvent.Unsubscribe(OnSketchModified);
		MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChangedAsync);

		ClearSketchSymbol();
	}

	public void ClearSketchSymbol()
	{
		_tool.SetSketchSymbol(null);
	}

	/// <summary>
	/// Must be called on the MCT.
	/// </summary>
	public async Task SetSketchAppearanceBasedOnSelectionAsync()
	{
		Gateway.LogEntry(_msg);

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
		if (ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology ==
		    _showFeatureSketchSymbology)
		{
			return;
		}

		_showFeatureSketchSymbology =
			ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology;

		try
		{
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

	private async Task TrySetSketchAppearanceAsync([CanBeNull] FeatureLayer featureLayer, [CanBeNull] IList<long> oids)
	{
		if (featureLayer == null || oids == null)
		{
			ClearSketchSymbol();
			return;
		}

		GeometryType geometryType = GeometryUtils.TranslateEsriGeometryType(featureLayer.ShapeType);

		if (ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology)
		{
			if (await _tool.CanSetConstructionSketchSymbol(geometryType))
			{
				//SetSketchSymbol(GetSymbolReference(featureLayer, oids.FirstOrDefault()));
				SetSketchSymbol(GetSymbolReference(featureLayer, oids));
			}
			else
			{
				_msg.Debug($"Cannot set sketch symbol for geometry type {geometryType}");
				ClearSketchSymbol();
			}
		}
		else
		{
			_msg.Debug(
				"Cannot set sketch symbol. Show feature symbology in sketch is turned off.");
			ClearSketchSymbol();
		}
	}

	private void SetSketchSymbol(CIMSymbolReference symbolReference)
	{
		_tool.SetSketchSymbol(symbolReference);
	}

	public void SetSketchType([NotNull] BasicFeatureLayer featureLayer)
	{
		GeometryType geometryType = GeometryUtils.TranslateEsriGeometryType(featureLayer.ShapeType);
		_tool.SetSketchType(GetApplicableSketchType(geometryType));
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
	private static CIMSymbolReference GetSymbolReference([NotNull] FeatureLayer layer, long oid)
	{
		CIMSymbol symbol = layer.LookupSymbol(oid, MapView.Active);

		if (symbol == null)
		{
			_msg.Debug(
				$"Cannot set sketch symbol: no symbol found in layer {layer.Name} for oid {oid}.");
			return null;
		}

		return symbol.MakeSymbolReference();
	}

	[CanBeNull]
	private static CIMSymbolReference GetSymbolReference([NotNull] FeatureLayer layer, [CanBeNull] IList<long> oids)
	{
		if (oids == null || oids.Count < 1)
		{
			return null;
		}

		CIMSymbol symbol = null;
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
			CIMSymbolReference symref = SymbolUtils.GetSymbol(renderer, values, scaleDenom, out var overrides);

			if (! cimSymbolReferences.Any(s => s.ToJson() == symref.ToJson()))
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
				_msg.Debug($"Cannot set sketch symbol: found different symbols in selection of in layer {layer.Name}.");
			}
			return null;
		}

		//return symbol.MakeSymbolReference();
		return symbolReference;
	}

	private static ArcGIS.Core.Data.Feature GetFeature(FeatureLayer layer, long oid)
	{
		using var featureClass = layer.GetFeatureClass();
		if (featureClass is null) return null;
		return ProSuite.Commons.AGP.Core.Geodatabase.GdbQueryUtils.GetFeature(featureClass, oid);
	}

	private static SketchGeometryType GetApplicableSketchType(GeometryType geometryType)
	{
		switch (geometryType)
		{
			case GeometryType.Point:
			case GeometryType.Multipoint:
				return SketchGeometryType.Point;
			case GeometryType.Polyline:
				return SketchGeometryType.Line;
			case GeometryType.Polygon:
				return SketchGeometryType.Polygon;
			case GeometryType.Multipatch:
				return SketchGeometryType.Multipatch;
			case GeometryType.Unknown:
			case GeometryType.Envelope:
			case GeometryType.GeometryBag:
				throw new ArgumentOutOfRangeException(nameof(geometryType),
				                                      $@"Cannot apply sketch geometry type for {nameof(geometryType)}");
			default:
				throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, null);
		}
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
