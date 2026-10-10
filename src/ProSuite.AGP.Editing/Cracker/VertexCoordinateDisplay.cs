using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Cracker;

/// <summary>
/// Labels the vertices of the displayed features with their coordinates and their
/// part/vertex index. This is the ArcGIS Pro implementation of the vertex display mode
/// ([V]) of the ArcObjects cracking tools.
/// </summary>
public class VertexCoordinateDisplay : OverlayDisplayBase
{
	// Offsets in points, i.e. independent of the map scale
	private const double _labelOffsetXPoints = 4;
	private const double _labelLineHeightPoints = 11;

	private readonly Dictionary<int, CIMSymbolReference> _labelSymbols =
		new Dictionary<int, CIMSymbolReference>();

	/// <summary>
	/// Labelling is skipped altogether for more than this many features: the labels would
	/// be unreadable anyway and creating them is expensive.
	/// </summary>
	public int MaxFeatureCount { get; set; } = 4;

	/// <summary>
	/// Labelling is skipped if more than this many vertices are within the current extent.
	/// Zooming in further reduces the count and brings the labels back.
	/// </summary>
	public int MaxVertexCount { get; set; } = 10000;

	/// <summary>
	/// How many labels are drawn on top of each other at coincident vertices before the
	/// remaining ones are collapsed into an ellipsis. Multipatch rings share their corner
	/// vertices, so this is the rule rather than the exception.
	/// </summary>
	public int MaxLabelsPerLocation { get; set; } = 4;

	protected override string ModeName => "vertex display mode";

	protected override string DisplayName => "Vertex labels";

	protected override void RenderCore(MapView mapView, Envelope viewExtent)
	{
		if (Shapes.Count > MaxFeatureCount)
		{
			Suppress(
				$"Vertex labels are not displayed for more than {MaxFeatureCount} selected features.");
			return;
		}

		var graphics = new List<CIMGraphic>();

		// Coincident vertices are stacked; the count is per refresh, across all features
		var labelsPerLocation = new Dictionary<(double, double), int>();

		var visibleVertexCount = 0;

		foreach (Geometry shape in Shapes)
		{
			Envelope clipBox = ProjectToShape(viewExtent, shape.SpatialReference);

			bool includeZ = shape.HasZ;
			bool includeM = shape.HasM;
			bool includeIds = shape.HasID;

			foreach (VertexLabelUtils.VertexInfo vertex in VertexLabelUtils.GetVertices(shape))
			{
				MapPoint point = vertex.Point;

				if (! IsWithin(point, clipBox))
				{
					continue;
				}

				visibleVertexCount++;

				if (visibleVertexCount > MaxVertexCount)
				{
					Suppress(
						$"More than {MaxVertexCount} vertices in the current extent: zoom in further to display the vertex labels.");
					return;
				}

				string label = VertexLabelUtils.GetVertexLabel(
					point, vertex.PartIndex, vertex.VertexIndex, includeZ, includeM, includeIds);

				AddLabel(graphics, point, label, labelsPerLocation);
			}
		}

		ClearSuppression();

		AddGraphics(mapView, graphics);
	}

	// Adds the label for a single vertex, stacked below any label already drawn at the
	// same location
	private void AddLabel([NotNull] ICollection<CIMGraphic> graphics, [NotNull] MapPoint point,
	                      [NotNull] string label,
	                      [NotNull] IDictionary<(double, double), int> labelsPerLocation)
	{
		// Round to 0.1 mm: vertices that close together cannot be told apart on screen
		var location = (Math.Round(point.X, 4), Math.Round(point.Y, 4));

		labelsPerLocation.TryGetValue(location, out int level);

		labelsPerLocation[location] = level + 1;

		if (level > MaxLabelsPerLocation)
		{
			// The ellipsis has already been drawn for this location
			return;
		}

		if (level == MaxLabelsPerLocation)
		{
			label = "...";
		}

		graphics.Add(new CIMTextGraphic
		             {
			             Text = label,
			             Shape = point,
			             Symbol = GetLabelSymbol(level)
		             });
	}

	[NotNull]
	private CIMSymbolReference GetLabelSymbol(int level)
	{
		if (! _labelSymbols.TryGetValue(level, out CIMSymbolReference symbol))
		{
			symbol = VertexLabelUtils.CreateLabelSymbol(
				VertexLabelUtils.SmallTextSize, _labelOffsetXPoints,
				-level * _labelLineHeightPoints);

			_labelSymbols.Add(level, symbol);
		}

		return symbol;
	}
}
