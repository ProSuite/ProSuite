using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing.RepairGeometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.RepairGeometry;

public class RepairGeometryFeedback
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private static CIMLineSymbol _redSegmentSymbol;
	private static CIMSymbolReference _orangePointMarker;
	private static CIMSymbolReference _redCrossMarker;
	private static CIMSymbolReference _greySquareMarker;
	private static CIMSymbolReference _greenCrackMarker;

	private readonly List<IDisposable> _overlays = new();

	public RepairGeometryFeedback()
	{
		CIMColor white = ColorUtils.CreateRGB(255, 255, 255);
		CIMColor red = ColorUtils.CreateRGB(255, 0, 0);
		CIMColor orange = ColorUtils.CreateRGB(255, 165, 0);
		CIMColor green = ColorUtils.CreateRGB(0, 200, 0);
		CIMColor grey = ColorUtils.CreateRGB(100, 100, 100);

		_redSegmentSymbol = SymbolUtils.CreateLineSymbol(red, 5);
		_redCrossMarker = CreateOutlinedPointSymbol(white, red, 7, SymbolUtils.MarkerStyle.Cross);
		_orangePointMarker =
			CreateOutlinedPointSymbol(orange, white, 6, SymbolUtils.MarkerStyle.Circle);
		_greenCrackMarker =
			CreateOutlinedPointSymbol(green, white, 6, SymbolUtils.MarkerStyle.Circle);
		_greySquareMarker =
			CreateOutlinedPointSymbol(grey, white, 3, SymbolUtils.MarkerStyle.Square);
	}

	public void Update(
		[CanBeNull] RepairGeometryResult repairResult,
		[NotNull] IEnumerable<Feature> selectedFeatures,
		CancellationToken cancellationToken)
	{
		DisposeOverlays();

		foreach (Feature feature in selectedFeatures)
		{
			Geometry geometry = feature.GetShape();

			if (geometry is Multipart multipart)
			{
				Multipoint multipoint = MultipointBuilderEx.CreateMultipoint(multipart);
				IDisposable addedVertex = MapView.Active.AddOverlay(multipoint, _greySquareMarker);
				_overlays.Add(addedVertex);
			}
		}

		if (repairResult == null)
		{
			return;
		}

		Stopwatch stopwatch = _msg.DebugStartTiming();

		foreach (RepairableFeature repairableFeature in repairResult.ResultsByFeature)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				DisposeOverlays();
				return;
			}

			AddInvalidSegmentsOverlay(repairableFeature);

			// Draw points to delete as red X markers
			if (repairableFeature.PointsToDelete != null)
			{
				IDisposable addedDeletePoints =
					MapView.Active.AddOverlay(repairableFeature.PointsToDelete, _redCrossMarker);
				_overlays.Add(addedDeletePoints);
			}

			// Draw crack points to add as green markers
			if (repairableFeature.CrackPointsToAdd != null)
			{
				IDisposable addedCrackPoints =
					MapView.Active.AddOverlay(repairableFeature.CrackPointsToAdd,
					                          _greenCrackMarker);
				_overlays.Add(addedCrackPoints);
			}
		}

		_msg.DebugStopTiming(stopwatch, "Added {0} overlays for {1} features.",
		                     _overlays.Count, repairResult.ResultsByFeature.Count);
	}

	/// <summary>
	/// One overlay per segment blocks the main CIM thread for seconds on features with thousands
	/// of short segments, hence the single multipart overlay.
	/// </summary>
	private void AddInvalidSegmentsOverlay([NotNull] RepairableFeature repairableFeature)
	{
		IList<InvalidSegment> invalidSegments = repairableFeature.InvalidSegments;

		if (invalidSegments.Count == 0)
		{
			return;
		}

		IEnumerable<IEnumerable<Segment>> parts =
			invalidSegments.Select(invalidSegment => new[] { invalidSegment.Segment });

		Polyline polyline = PolylineBuilderEx.CreatePolyline(
			parts, invalidSegments[0].Segment.SpatialReference);

		_overlays.Add(MapView.Active.AddOverlay(
			              polyline, _redSegmentSymbol.MakeSymbolReference()));
	}

	public void DisposeOverlays()
	{
		foreach (IDisposable overlay in _overlays)
		{
			overlay.Dispose();
		}

		_overlays.Clear();
	}

	private static CIMSymbolReference CreateOutlinedPointSymbol(
		CIMColor fillColor, CIMColor strokeColor, double size,
		SymbolUtils.MarkerStyle style)
	{
		var stroke = SymbolUtils.CreateSolidStroke(strokeColor, size / 2);
		var polySym =
			SymbolUtils.CreatePolygonSymbol(fillColor, SymbolUtils.FillStyle.Solid, stroke);
		var marker = SymbolUtils.CreateMarker(style, polySym, size);
		var symbol = SymbolUtils.CreatePointSymbol(marker);
		return symbol.MakeSymbolReference();
	}
}
