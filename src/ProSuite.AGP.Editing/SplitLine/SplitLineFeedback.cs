using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.SplitLine;

/// <summary>
/// Draws a short perpendicular pink cut line at the split point as a map overlay.
/// All methods must be called on the MCT (see <c>QueuedTask.Run</c>).
/// </summary>
public class SplitLineFeedback
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private const int _cutLineHalfLengthPixels = 12;

	[CanBeNull] private CIMSymbolReference _cutLineSymbol;
	[CanBeNull] private IDisposable _overlay;

	/// <summary>
	/// Shows a perpendicular cut line on <paramref name="polyline"/> at
	/// <paramref name="splitPoint"/> in <paramref name="mapView"/>, replacing any previous overlay.
	/// </summary>
	public void Update([NotNull] MapView mapView,
	                   [NotNull] Polyline polyline,
	                   [NotNull] MapPoint splitPoint)
	{
		Assert.ArgumentNotNull(mapView, nameof(mapView));
		Assert.ArgumentNotNull(polyline, nameof(polyline));
		Assert.ArgumentNotNull(splitPoint, nameof(splitPoint));

		Clear();

		SpatialReference mapSr = mapView.Map?.SpatialReference;
		MapPoint mapPoint = mapSr != null
			                    ? GeometryUtils.EnsureSpatialReference(splitPoint, mapSr)
			                    : splitPoint;

		double halfLength = MapUtils.ConvertScreenPixelToMapLength(
			mapView, _cutLineHalfLengthPixels, mapPoint);

		Polyline cutLine = SplitLineUtils.CreatePerpendicularCutLine(
			polyline, splitPoint, halfLength);

		if (cutLine is null || cutLine.IsEmpty)
		{
			return;
		}

		_cutLineSymbol ??= CreateCutLineSymbol();
		_overlay = mapView.AddOverlay(cutLine, _cutLineSymbol);
	}

	/// <summary>
	/// Removes the current cut-line overlay, if any.
	/// </summary>
	public void Clear()
	{
		if (_overlay is null)
		{
			return;
		}

		try
		{
			_overlay.Dispose();
		}
		catch (Exception ex)
		{
			_msg.Warn("Error disposing split line feedback overlay", ex);
		}
		finally
		{
			_overlay = null;
		}
	}

	[NotNull]
	private static CIMSymbolReference CreateCutLineSymbol()
	{
		CIMStroke pinkStroke = SymbolUtils.CreateSolidStroke(
			ColorUtils.CreateRGB(255, 0, 200), 2.5);
		pinkStroke.CapStyle = LineCapStyle.Round;

		CIMStroke casingStroke = SymbolUtils.CreateSolidStroke(
			ColorUtils.CreateRGB(255, 255, 255), 4.5);
		casingStroke.CapStyle = LineCapStyle.Round;

		return SymbolUtils.CreateLineSymbol(pinkStroke, casingStroke).MakeSymbolReference();
	}
}
