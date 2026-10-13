using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.SplitLine;

public static class SplitLineUtils
{
	/// <summary>
	/// The point at which the line is split when clicked at the specified point: the closest
	/// vertex if it is within the search radius, otherwise the closest point on the line.
	/// </summary>
	/// <returns>The split point, or null if it is too close to the start or end of a part.</returns>
	[CanBeNull]
	public static MapPoint GetSplitPoint([NotNull] Polyline polyline,
	                                     [NotNull] MapPoint clickPoint,
	                                     double searchRadius,
	                                     double xyTolerance)
	{
		Assert.ArgumentNotNull(polyline, nameof(polyline));
		Assert.ArgumentNotNull(clickPoint, nameof(clickPoint));

		ProximityResult vertex = GeometryEngine.Instance.NearestVertex(polyline, clickPoint);

		MapPoint result = vertex.Distance <= searchRadius
			                  ? vertex.Point
			                  : GeometryEngine.Instance.QueryPointAndDistance(
				                  polyline, SegmentExtensionType.NoExtension, clickPoint,
				                  AsRatioOrLength.AsLength, out _, out _, out _);

		// Simplify removes segments shorter than about 3 times the tolerance, which would
		// leave the line unsplit or the parts unable to merge again.
		double minDistanceToEnd = 3 * xyTolerance;

		foreach (ReadOnlySegmentCollection part in polyline.Parts)
		{
			if (GeometryEngine.Instance.Distance(result, part[0].StartPoint) <= minDistanceToEnd ||
			    GeometryEngine.Instance.Distance(result, part[part.Count - 1].EndPoint) <=
			    minDistanceToEnd)
			{
				return null;
			}
		}

		return result;
	}

	/// <summary>
	/// The candidate closest to the split point that lies on the interior of the line, such
	/// as an existing junction the line should rather be split at.
	/// </summary>
	[CanBeNull]
	public static MapPoint GetClosestPointOnInterior([NotNull] Polyline polyline,
	                                                 [NotNull] IEnumerable<MapPoint> candidates,
	                                                 [NotNull] MapPoint splitPoint,
	                                                 double xyTolerance)
	{
		return candidates
		       .Where(p => GeometryEngine.Instance.Distance(p, polyline) <= xyTolerance &&
		                   GetSplitPoint(polyline, p, 0, xyTolerance) != null)
		       .OrderBy(p => GeometryEngine.Instance.Distance(p, splitPoint))
		       .FirstOrDefault();
	}
}
