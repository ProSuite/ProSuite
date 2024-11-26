using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Symbolization;

/// <summary>
/// Draw feature previews using Pro SDK “overlays” on the active map view.
/// Be sure to clear when done (calling <see cref="Clear"/> or disposing).
/// </summary>
public class FeaturePreview : IDisposable
{
	// TODO synchronize access to this dict?
	private readonly Dictionary<Key, Info> _overlays = new();
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public float TransparencyPercent { get; set; } = 33.3f;

	/// <summary>
	/// Draw the given <paramref name="shape"/> on the active map
	/// using the symbol for feature <paramref name="oid"/> in the
	/// given <paramref name="layer"/>.
	/// Call <see cref="Clear"/> to remove all such previews.
	/// </summary>
	public void Draw(FeatureLayer layer, long oid, Geometry shape)
	{
		var mapView = MapView.Active;
		if (mapView is null) return;

		var refScale = mapView.Map.ReferenceScale;

		var key = new Key(layer.URI, oid);
		if (_overlays.TryGetValue(key, out Info info))
		{
			UpdateOverlay(mapView, info.Overlay, shape, info.Symbol, refScale);
		}
		else
		{
			var transparency = Clamp(TransparencyPercent, 0, 100);
			var symbol = layer.LookupSymbol(oid, mapView).SetAlpha(100f - transparency);
			var symref = symbol.MakeSymbolReference();
			var overlay = mapView.AddOverlay(shape, symref, refScale);
			_overlays.Add(key, new Info(overlay, symref));
		}
	}

	/// <summary>
	/// Draw a preview of the feature identified by the given layer and OID
	/// using the given <paramref name="shape"/> and <paramref name="symbol"/>
	/// (with <see cref="TransparencyPercent"/> applied).
	/// Make sure that shape and symbol are compatible.
	/// Call <see cref="Clear"/> to remove such previews.
	/// </summary>
	public void Draw(FeatureLayer layer, long oid, CIMSymbolReference symbol, Geometry shape)
	{
		var mapView = MapView.Active;
		if (mapView is null) return;

		var refScale = mapView.Map.ReferenceScale;

		var transparency = Clamp(TransparencyPercent, 0, 100);
		symbol.Symbol.SetAlpha(100f - transparency);

		var key = new Key(layer.URI, oid);
		if (_overlays.TryGetValue(key, out Info info))
		{
			shape ??= info.Shape ?? GetShape(layer, oid);
			UpdateOverlay(mapView, info.Overlay, shape, symbol, refScale);
		}
		else
		{
			shape ??= GetShape(layer, oid);
			var overlay = mapView.AddOverlay(shape, symbol, refScale);
			_overlays.Add(key, new Info(overlay, symbol, shape));
		}
	}

	public void Clear()
	{
		foreach (var overlay in _overlays)
		{
			overlay.Value.Overlay?.Dispose();
		}

		_overlays.Clear();
	}

	public void Dispose()
	{
		Clear();
	}

	#region Private stuff

	private static void UpdateOverlay(MapView mapView, IDisposable overlay, Geometry geometry, CIMSymbolReference symbol, double referenceScale = -1)
	{
		if (!mapView.UpdateOverlay(overlay, geometry, symbol, referenceScale))
		{
			_msg.Warn("UpdateOverlay() returned false; display feedback may be wrong; see K2#37");
			// ask Redlands when this can happen and what we should do (K2#37)
		}
	}

	private static Geometry GetShape(FeatureLayer layer, long oid)
	{
		var filter = new QueryFilter { ObjectIDs = new[] { oid } };
		using var cursor = layer.Search(filter);
		if (cursor is null) return null; // no valid data source
		if (!cursor.MoveNext()) return null; // no rows match filter
		using var row = cursor.Current;
		return row is Feature feature ? feature.GetShape() : null;
	}

	private static float Clamp(float value, float min, float max)
	{
		if (value < min) return min;
		if (value > max) return max;
		return value;
	}

	private readonly struct Info
	{
		public readonly IDisposable Overlay;
		public readonly CIMSymbolReference Symbol;
		public readonly Geometry Shape;

		public Info(IDisposable overlay, CIMSymbolReference symbol, Geometry shape = null)
		{
			Overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
			Symbol = symbol; // null ok?
			Shape = shape; // can be null
		}
	}

	private readonly struct Key : IEquatable<Key>
	{
		private readonly string _layerUri;
		private readonly long _oid;

		public Key(string layerUri, long oid)
		{
			_layerUri = layerUri ?? string.Empty;
			_oid = oid;
		}

		public bool Equals(Key other)
		{
			return _layerUri == other._layerUri && _oid == other._oid;
		}

		public override bool Equals(object obj)
		{
			return obj is Key other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_layerUri, _oid);
		}

		public override string ToString()
		{
			return $"OID {_oid} in layer {_layerUri}";
		}
	}

	#endregion
}
