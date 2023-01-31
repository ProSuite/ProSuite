using ArcGIS.Core.Geometry;
using System;

namespace ProSuite.Commons.AGP.Core.Carto;

public static class Extensions
{
	public static Pair ToPair(this MapPoint point)
	{
		if (point is null) throw new ArgumentNullException(nameof(point));
		return new Pair(point.X, point.Y);
	}
}
