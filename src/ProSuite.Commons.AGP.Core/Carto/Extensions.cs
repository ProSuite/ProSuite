using ArcGIS.Core.Geometry;
using System;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.Carto;

public static class Extensions
{
	public static Pair ToPair(this MapPoint point)
	{
		if (point is null) throw new ArgumentNullException(nameof(point));
		return new Pair(point.X, point.Y);
	}

	public static Pair ToPair(this Coordinate2D coord)
	{
		return new Pair(coord.X, coord.Y);
	}

	public static Coordinate2D ToCoordinate2D(this Pair pair)
	{
		return new Coordinate2D(pair.X, pair.Y);
	}
}
