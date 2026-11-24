using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.AGP.Editing;

/// <summary>
/// Where the heights of the vertices of a drawn line come from, decided per vertex when it
/// is drawn: its own height (the click), the height of the feature it snapped to, or
/// interpolated along the line between the vertices with a known height. Looked up by
/// position rather than vertex index, so a vertex finds its entry again after an undo, a
/// redo or the insertion of other vertices.
/// </summary>
public class VertexHeights
{
	private readonly double _xyTolerance;

	// The height of a snapped vertex, NaN for a vertex to be interpolated, null for a vertex
	// that keeps its own height.
	private readonly List<KeyValuePair<Pnt2D, double?>> _entries =
		new List<KeyValuePair<Pnt2D, double?>>();

	public VertexHeights(double xyTolerance)
	{
		_xyTolerance = xyTolerance;
	}

	public bool Contains(double x, double y)
	{
		return TryGetEntry(x, y, out _);
	}

	public void AddOwn(double x, double y)
	{
		_entries.Add(new KeyValuePair<Pnt2D, double?>(new Pnt2D(x, y), null));
	}

	public void AddSnapped(double x, double y, double z)
	{
		_entries.Add(new KeyValuePair<Pnt2D, double?>(new Pnt2D(x, y), z));
	}

	public void AddInterpolated(double x, double y)
	{
		_entries.Add(new KeyValuePair<Pnt2D, double?>(new Pnt2D(x, y), double.NaN));
	}

	/// <summary>
	/// Returns the vertices of a line with their heights: the own height of a vertex that is
	/// not recorded or keeps it, the snapped height, or the height interpolated along the
	/// line. An end to be interpolated takes the height of the nearest known vertex.
	/// </summary>
	/// <param name="vertices">The vertices of the line, with their own heights.</param>
	/// <param name="endsKnown">Whether both ends have a known height.</param>
	[NotNull]
	public List<Pnt3D> Apply([NotNull] IList<Pnt3D> vertices, out bool endsKnown)
	{
		List<Pnt3D> result =
			vertices.Select(v => new Pnt3D(v.X, v.Y, GetZ(v))).ToList();

		// An end with its own height still unknown (not yet delivered, or no surface there) is
		// not refused: it takes the height of the nearest known vertex.
		endsKnown = result.Count == 0 ||
		            ! IsInterpolated(vertices[0]) && ! IsInterpolated(vertices[result.Count - 1]);

		if (result.Count > 1 && result.Any(p => double.IsNaN(p.Z)))
		{
			var line = new Linestring(result);

			line.TryInterpolateUndefinedZs();

			result = line.GetPoints().ToList();
		}

		// Nothing to interpolate from: the vertices keep their own heights.
		for (int i = 0; i < result.Count; i++)
		{
			if (double.IsNaN(result[i].Z))
			{
				result[i].Z = vertices[i].Z;
			}
		}

		return result;
	}

	private bool IsInterpolated(Pnt3D vertex)
	{
		return TryGetEntry(vertex.X, vertex.Y, out double? z) && z.HasValue &&
		       double.IsNaN(z.Value);
	}

	private double GetZ(Pnt3D vertex)
	{
		return TryGetEntry(vertex.X, vertex.Y, out double? z) && z.HasValue
			       ? z.Value
			       : vertex.Z;
	}

	private bool TryGetEntry(double x, double y, out double? z)
	{
		foreach (KeyValuePair<Pnt2D, double?> entry in _entries)
		{
			if (MathUtils.AreEqual(entry.Key.X, x, _xyTolerance) &&
			    MathUtils.AreEqual(entry.Key.Y, y, _xyTolerance))
			{
				z = entry.Value;
				return true;
			}
		}

		z = null;
		return false;
	}
}
