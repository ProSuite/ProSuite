using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.AGP.Editing;

/// <summary>
/// The line a sketch tool draws and where the height of each of its vertices comes from:
/// ArcGIS Pro's sketch while it is drawn, then the completed sketch until the next line is
/// started. ArcGIS Pro's sketch is only read, never written. Used on the UI thread only.
/// </summary>
public class DrawnLine
{
	[NotNull] private readonly Func<MapPoint, double> _getSnappedZ;

	[CanBeNull] private Polyline _line;
	private bool _completed;
	[CanBeNull] private VertexHeights _heights;
	[CanBeNull] private VertexHeights _rememberedHeights;

	/// <param name="getSnappedZ">The height of the feature a sketch vertex snapped to, NaN if
	/// it did not snap to one.</param>
	public DrawnLine([NotNull] Func<MapPoint, double> getSnappedZ)
	{
		_getSnappedZ = getSnappedZ;
	}

	/// <summary>
	/// The number of completed lines. A sketch read before a completion is outdated once
	/// this has changed: it must not replace the completed line.
	/// </summary>
	public int CompletionCount { get; private set; }

	/// <summary>
	/// Whether a line with at least one vertex is being drawn.
	/// </summary>
	public bool IsDrawing => _line is not null && ! _completed;

	/// <summary>
	/// Takes over ArcGIS Pro's sketch. Each vertex that is new gets its height source: the
	/// feature it snapped to, interpolation if <paramref name="interpolate"/> is set (never
	/// the first vertex of a part), otherwise its own height.
	/// </summary>
	public void Update([CanBeNull] Geometry sketch, bool interpolate)
	{
		if (sketch is null || sketch.IsEmpty)
		{
			// After the completion Pro clears its sketch: the completed line stays. Undoing the
			// first vertex keeps the height sources for a redo.
			if (! _completed)
			{
				_line = null;
			}

			return;
		}

		if (sketch is not Polyline line)
		{
			return;
		}

		if (_completed)
		{
			// The first vertex of the next line.
			Forget();
		}

		_line = line;

		if (! line.HasZ)
		{
			return;
		}

		VertexHeights heights =
			_heights ??= new VertexHeights(GeometryUtils.GetXyTolerance(line));

		// The first vertex of a part is recorded with the part's first segment. It is never
		// interpolated, so the mode at that time does not matter.
		foreach (ReadOnlySegmentCollection part in line.Parts)
		{
			for (int i = 0; i < part.Count; i++)
			{
				if (i == 0)
				{
					Record(heights, part[i].StartPoint, false);
				}

				Record(heights, part[i].EndPoint, interpolate);
			}
		}
	}

	/// <summary>
	/// Completes the line with the completed sketch, unless a line end is to be interpolated:
	/// it has nothing to be interpolated from.
	/// </summary>
	/// <param name="sketch">The completed sketch.</param>
	/// <param name="interpolate">Whether its last vertex, if new, is to be interpolated.</param>
	/// <param name="withZs">The line with its heights.</param>
	/// <returns>False if a line end has no known height.</returns>
	public bool TryComplete([NotNull] Polyline sketch, bool interpolate,
	                        [NotNull] out Polyline withZs)
	{
		CompletionCount++;

		// The modification of the last vertex can come after the completion and is then
		// skipped: the snap of the last click is recorded here.
		Update(sketch, interpolate);

		withZs = GetWithZs(sketch, out bool endsKnown);

		if (! endsKnown)
		{
			return false;
		}

		_completed = _line is not null;

		return true;
	}

	/// <summary>
	/// The line being drawn, or the completed one until the next line is started, with the
	/// heights of its vertices. Null if there is none.
	/// </summary>
	[CanBeNull]
	public Polyline GetWithZs()
	{
		return _line is null ? null : GetWithZs(_line, out _);
	}

	public void Forget()
	{
		_line = null;
		_completed = false;
		_heights = null;
	}

	/// <summary>
	/// Remembers the height sources for <see cref="RestoreRemembered"/>, together with the
	/// sketch the tool remembers.
	/// </summary>
	public void Remember()
	{
		_rememberedHeights = _heights;
	}

	/// <summary>
	/// Starts a new line with the remembered height sources, before the remembered sketch is
	/// set again.
	/// </summary>
	public void RestoreRemembered()
	{
		Forget();
		_heights = _rememberedHeights;
	}

	private void Record([NotNull] VertexHeights heights, [NotNull] MapPoint vertex,
	                    bool interpolate)
	{
		if (heights.Contains(vertex.X, vertex.Y))
		{
			return;
		}

		double snappedZ = _getSnappedZ(vertex);

		if (! double.IsNaN(snappedZ))
		{
			heights.AddSnapped(vertex.X, vertex.Y, snappedZ);
		}
		else if (interpolate)
		{
			heights.AddInterpolated(vertex.X, vertex.Y);
		}
		else
		{
			heights.AddOwn(vertex.X, vertex.Y);
		}
	}

	[NotNull]
	private Polyline GetWithZs([NotNull] Polyline line, out bool endsKnown)
	{
		endsKnown = true;

		if (! line.HasZ || _heights is null)
		{
			return line;
		}

		var builder = new PolylineBuilderEx(line);

		for (int partIndex = 0; partIndex < builder.PartCount; partIndex++)
		{
			if (builder.GetSegmentCount(partIndex) == 0)
			{
				// The first click: its height is its own.
				continue;
			}

			int pointCount = builder.GetPointCount(partIndex);

			List<MapPoint> vertices = Enumerable.Range(0, pointCount)
			                                    .Select(i => builder.GetPoint(partIndex, i))
			                                    .ToList();

			List<Pnt3D> withZs = _heights.Apply(
				vertices.Select(v => new Pnt3D(v.X, v.Y, v.Z)).ToList(),
				out bool partEndsKnown);

			endsKnown &= partEndsKnown;

			for (int i = 0; i < pointCount; i++)
			{
				var vertexBuilder = new MapPointBuilderEx(vertices[i]) { Z = withZs[i].Z };

				builder.SetPoint(partIndex, i, vertexBuilder.ToGeometry());
			}
		}

		return builder.ToGeometry();
	}
}
