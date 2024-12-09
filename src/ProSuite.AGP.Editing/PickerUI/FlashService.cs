using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using Envelope = ArcGIS.Core.Geometry.Envelope;
using Geometry = ArcGIS.Core.Geometry.Geometry;
using Polygon = ArcGIS.Core.Geometry.Polygon;

namespace ProSuite.AGP.Editing.PickerUI;

public class FlashService : IDisposable
{
	private readonly Dictionary<string, IFlashSymbol> _symbols;
	private readonly List<IDisposable> _overlays = new();
	private readonly CIMLineSymbol _lineSymbol;
	private readonly CIMPointSymbol _pointSymbol;
	private readonly CIMPolygonSymbol _polygonSymbol;

	public CIMLineSymbol LineSymbol => _lineSymbol;

	public CIMPointSymbol PointSymbol => _pointSymbol;

	public CIMPolygonSymbol PolygonSymbol => _polygonSymbol;

	public FlashService()
	{
		CIMColor magenta = ColorFactory.Instance.CreateRGBColor(255, 0, 255);

		_lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(magenta, 4);

		CIMStroke outline =
			SymbolFactory.Instance.ConstructStroke(
				magenta, 4, SimpleLineStyle.Solid);

		_polygonSymbol =
			SymbolFactory.Instance.ConstructPolygonSymbol(
				magenta, SimpleFillStyle.Null, outline);

		_pointSymbol = SymbolUtils.CreatePointSymbol(magenta, 6);
	}

	public FlashService(Dictionary<string, IFlashSymbol> symbols)
	{
		_symbols = symbols;
	}

	public FlashService(params IFlashSymbol[] symbols) : this(
		symbols.ToDictionary(s => s.Name, s => s)) { }

	public FlashService Flash(Geometry geometry)
	{
		DisposeOverlays();

		Geometry flashGeometry = null;
		CIMSymbol symbol = null;

		switch (geometry.GeometryType)
		{
			case GeometryType.Point:
			case GeometryType.Multipoint:
				flashGeometry = geometry;
				symbol = _pointSymbol;
				break;
			case GeometryType.Polyline:
				flashGeometry = geometry;
				symbol = _lineSymbol;
				break;
			case GeometryType.Polygon:
				flashGeometry = GetPolygonGeometry(geometry);
				symbol = _polygonSymbol;
				break;
			case GeometryType.Unknown:
			case GeometryType.Envelope:
			case GeometryType.Multipatch:
			case GeometryType.GeometryBag:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		QueuedTask.Run(() => { AddOverlay(flashGeometry, symbol); });

		return this;
	}

	public FlashService Flash(string symbolName, Geometry geometry)
	{
		try
		{
			Geometry flashGeometry = null;
			CIMSymbol symbol = _symbols[symbolName].GetSymbol(geometry.GeometryType);

			switch (geometry.GeometryType)
			{
				case GeometryType.Point:
				case GeometryType.Multipoint:
					flashGeometry = geometry;
					break;
				case GeometryType.Polyline:
					flashGeometry = geometry;
					break;
				case GeometryType.Polygon:
					flashGeometry = GetPolygonGeometry(geometry);
					break;
				case GeometryType.Unknown:
				case GeometryType.Envelope:
				case GeometryType.Multipatch:
				case GeometryType.GeometryBag:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			QueuedTask.Run(() => { AddOverlay(flashGeometry, symbol); });
		}
		catch (Exception) { }

		return this;
	}

	public void Dispose()
	{
		DisposeOverlays();
	}

	private static Geometry GetPolygonGeometry(Geometry geometry)
	{
		Envelope clipExtent = MapView.Active?.Extent;

		if (clipExtent == null)
		{
			return geometry;
		}

		double mapRotation = MapView.Active.NotNullCallback(mv => mv.Camera.Heading);

		return GeometryUtils.GetClippedPolygon((Polygon) geometry, clipExtent, mapRotation);
	}

	private void AddOverlay(Geometry geometry, CIMSymbol symbol)
	{
		MapView.Active.NotNullCallback(mv =>
		{
			IDisposable overlay = mv.AddOverlay(geometry, symbol.MakeSymbolReference());

			_overlays.Add(Assert.NotNull(overlay));
		});
	}

	public FlashService DisposeOverlays()
	{
		foreach (IDisposable overlay in _overlays)
		{
			overlay.Dispose();
		}

		_overlays.Clear();

		return this;
	}
}

public interface IFlashSymbol
{
	string Name { get; set; }

	CIMSymbol GetSymbol(GeometryType type);
}

public class ColoredSymbol : IFlashSymbol
{
	private readonly CIMColor _color;

	public ColoredSymbol(string name, double R, double G, double B)
		: this(R, G, B)
	{
		Name = name;
	}

	public ColoredSymbol(double R, double G, double B)
	{
		_color = ColorFactory.Instance.CreateRGBColor(R, G, B);
	}

	public string Name { get; set; }

	public CIMSymbol GetSymbol(GeometryType type)
	{
		switch (type)
		{
			case GeometryType.Point:
			case GeometryType.Multipoint:
			{
				return SymbolFactory.Instance.ConstructPointSymbol(_color, 6);
			}
			case GeometryType.Polyline:
			{
				return SymbolFactory.Instance.ConstructLineSymbol(_color, 4);
			}
			case GeometryType.Polygon:
			{
				CIMStroke outline =
					SymbolFactory.Instance.ConstructStroke(_color, 4, SimpleLineStyle.Solid);

				return SymbolFactory.Instance.ConstructPolygonSymbol(
					_color, SimpleFillStyle.Null, outline);
			}
			case GeometryType.Unknown:
			case GeometryType.Envelope:
			case GeometryType.Multipatch:
			case GeometryType.GeometryBag:
			default:
				return null;
		}
	}
}

public class BlueSymbol : ColoredSymbol
{
	public BlueSymbol(string name) : base(name, 0, 100, 255) { }
}

public class GreenSymbol : ColoredSymbol
{
	public GreenSymbol(string name) : base(name, 0, 255, 0) { }
}

public class MagentaSymbol : ColoredSymbol
{
	public MagentaSymbol(string name) : base(name, 255, 0, 255) { }
}

public class RedSymbol : ColoredSymbol
{
	public RedSymbol(string name) : base(name, 255, 0, 0) { }
}
