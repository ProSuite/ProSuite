using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

// todo 3D, test multipatch sketch symbol!
public class SymbolizedSketchTypeBasedOnSelection : SelectionSketchTypeToggleBase
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
	/// <param name="defaultSelectionSketchType"></param>
	public SymbolizedSketchTypeBasedOnSelection([NotNull] ISymbolizedSketchTool tool,
	                                            SketchGeometryType defaultSelectionSketchType) :
		base(tool, defaultSelectionSketchType)
	{
		_tool = tool;

		_showFeatureSketchSymbology = ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology;

		SketchModifiedEvent.Subscribe(OnSketchModified);
		MapSelectionChangedEvent.Subscribe(OnMapSelectionChangedAsync);
	}

	protected override void DisposeCore()
	{
		SketchModifiedEvent.Unsubscribe(OnSketchModified);
		MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChangedAsync);

		ClearSketchSymbol();
		ResetSelectionSketchType();
	}

	private void ClearSketchSymbol()
	{
		_tool.SetSketchSymbol(null);
	}

	/// <summary>
	/// Must be called on the MCT.
	/// </summary>
	public void SetSketchSymbolBasedOnSelection()
	{
		Gateway.LogEntry(_msg);

		try
		{
			var selection = SelectionUtils.GetSelection<BasicFeatureLayer>(MapView.Active.Map);
			List<long> oids = GetApplicableSelection(selection, out FeatureLayer featureLayer);

			SetSketchSymbolBasedOnSelection(featureLayer, oids);

			SetSketchType(featureLayer);
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message, ex);
		}
	}

	/// <summary>
	/// Is always on MCT.
	/// </summary>
	private void OnSketchModified(SketchModifiedEventArgs args)
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
			SetSketchSymbolBasedOnSelection(featureLayer, oids);
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

			await QueuedTask.Run(() =>
			{
				List<long> oids = GetApplicableSelection(selection, out FeatureLayer featureLayer);

				SetSketchSymbolBasedOnSelection(featureLayer, oids);

				SetSketchType(featureLayer);
			});
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message, ex);
		}
	}

	private void SetSketchSymbolBasedOnSelection([CanBeNull] FeatureLayer featureLayer,
	                                             [CanBeNull] IList<long> oids)
	{
		if (featureLayer == null || oids == null)
		{
			ClearSketchSymbol();
			SetCurrentSelectionSketchType();

			return;
		}

		GeometryType geometryType = GeometryUtils.TranslateEsriGeometryType(featureLayer.ShapeType);

		if (ApplicationOptions.EditingOptions.ShowFeatureSketchSymbology)
		{
			if (_tool.CanSetConstructionSketchSymbol(geometryType))
			{
				_tool.SetSketchSymbol(
					GetSymbolReference(featureLayer, oids.FirstOrDefault()));
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

	private void SetSketchType([CanBeNull] BasicFeatureLayer featureLayer)
	{
		if (featureLayer == null)
		{
			return;
		}

		GeometryType geometryType = GeometryUtils.TranslateEsriGeometryType(featureLayer.ShapeType);
		SetSketchType(_tool, GetApplicableSketchType(geometryType));
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
}
