using System.Collections.Generic;

namespace ProSuite.Commons.Geom
{
	public interface IPointList : IBoundedXY
	{
		int PointCount { get; }

		IPnt GetPoint(int pointIndex, bool clone = false);

		void GetCoordinates(int pointIndex, out double x, out double y, out double z);

		IEnumerable<IPnt> AsEnumerablePoints(bool clone = false);

		/// <summary>
		/// Returns the index of a vertex that is within the XY-tolerance of the specified point.
		/// A potential Z value is ignored.
		/// </summary>
		/// <param name="searchPoint">The search point.</param>
		/// <param name="tolerance">The search tolerance.</param>
		/// <param name="useSearchCircle">Whether the search is performed within an actual circle,
		/// i.e. the tolerance is the XY distance threshold. If false, the search is performed
		/// within the box defined by 2*2 times the tolerance.
		/// </param>
		/// <param name="allowIndexing"></param>
		/// <returns></returns>
		IEnumerable<int> FindPointIndexes(IPnt searchPoint,
		                                  double tolerance = double.Epsilon,
		                                  bool useSearchCircle = false,
		                                  bool allowIndexing = true);
	}
}
