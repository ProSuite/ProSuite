using System;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing;

public static class ToolDockpaneUtils
{
	/// <summary>
	/// Returns a warning message when the given tolerance is larger than the current map
	/// view extent (in which case snapping would effectively apply across the whole visible
	/// area), or <c>null</c> when the tolerance is within the extent.
	/// </summary>
	/// <param name="tolerance">The snap tolerance, in map units.</param>
	/// <param name="mapExtent">The current map view extent, in map units.</param>
	/// <param name="unitLabel">The unit label to include in the message (e.g. "m", "dd").</param>
	[CanBeNull]
	public static string GetToleranceExceedsExtentWarning(
		double tolerance, [CanBeNull] Envelope mapExtent, [CanBeNull] string unitLabel)
	{
		if (mapExtent == null || mapExtent.IsEmpty)
		{
			return null;
		}

		if (tolerance <= 0)
		{
			return null;
		}

		double extent = Math.Min(mapExtent.Width, mapExtent.Height);

		if (tolerance <= extent)
		{
			return null;
		}

		string unit = string.IsNullOrEmpty(unitLabel) ? string.Empty : $" {unitLabel}";

		return
			$"The snap tolerance ({tolerance:0.######}{unit}) is larger than the current map " +
			"extent. Snapping will apply across the entire visible area – this can be slow and " +
			"result in unexpected results. Consider using a snap tolerance close to the data tolerance.";
	}
}
