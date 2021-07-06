using System.Collections.Generic;

namespace ProSuite.Commons.Geom
{
	public interface IPointList
	{
		int PointCount { get; }

		IPnt GetPoint(int pointIndex, bool clone = false);

		void GetCoordinates(int pointIndex, out double x, out double y, out double z);

		IEnumerable<IPnt> AsEnumerablePoints(bool clone = false);
	}
}
