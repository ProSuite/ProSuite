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

namespace ProSuite.AGP.Editing.PickerUI;

public class FlashService : IDisposable/*, IFlashService*/
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

	public FlashService(params IFlashSymbol[] symbols) : this(symbols.ToDictionary(s => s.Name, s => s))
	{
	}

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
			CIMSymbol symbol = _symbols[symbolName].GetSymbol();

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
		catch (Exception exc)
		{
			
		}

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
	CIMSymbol GetSymbol();
}

public class BluePolygonSymbol : IFlashSymbol
{
	public string Name { get; set; }

	public CIMSymbol GetSymbol()
	{
		CIMColor blue = ColorFactory.Instance.CreateRGBColor(0, 100, 255);

		CIMStroke outline =
			SymbolFactory.Instance.ConstructStroke(blue, 4, SimpleLineStyle.Solid);

		return SymbolFactory.Instance.ConstructPolygonSymbol(
			blue, SimpleFillStyle.Null, outline);
	}
}

public class GreenPolygonSymbol : IFlashSymbol
{
	public string Name { get; set; }

	public CIMSymbol GetSymbol()
	{
		CIMColor green = ColorFactory.Instance.CreateRGBColor(0, 255, 0);

		CIMStroke outline =
			SymbolFactory.Instance.ConstructStroke(green, 4, SimpleLineStyle.Solid);

		return SymbolFactory.Instance.ConstructPolygonSymbol(
			green, SimpleFillStyle.Null, outline);
	}
}

public class MagentaPolygonSymbol : IFlashSymbol
{
	public string Name { get; set; }

	public CIMSymbol GetSymbol()
	{
		CIMColor magenta = ColorFactory.Instance.CreateRGBColor(255, 0, 255);

		CIMStroke outline =
			SymbolFactory.Instance.ConstructStroke(magenta, 4, SimpleLineStyle.Solid);

		return SymbolFactory.Instance.ConstructPolygonSymbol(
			magenta, SimpleFillStyle.Null, outline);
	}
}

public class MagentaLineSymbol : IFlashSymbol
{
	public string Name { get; set; }

	public CIMSymbol GetSymbol()
	{
		CIMColor magenta = ColorFactory.Instance.CreateRGBColor(255, 0, 255);

		return SymbolFactory.Instance.ConstructLineSymbol(magenta, 4);
	}
}
