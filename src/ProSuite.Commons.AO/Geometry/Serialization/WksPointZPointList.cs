using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.Commons.AO.Geometry.Serialization
{
	public class WksPointZPointList : IPointList
	{
		private readonly WKSPointZ[] _wksPoints;
		private readonly int _startIndex;

		private readonly bool _reverseOrder;
		private readonly int _actualStartIndex;

		public WksPointZPointList([NotNull] WKSPointZ[] wksPoints) : this(
			wksPoints, 0, wksPoints.Length) { }

		public WksPointZPointList([NotNull] WKSPointZ[] wksPoints, int startIndex, int count,
		                          bool reverseOrder = false)
		{
			_wksPoints = wksPoints;

			_startIndex = startIndex;
			PointCount = count;

			_reverseOrder = reverseOrder;

			_actualStartIndex = reverseOrder ? _startIndex + PointCount - 1 : _startIndex;
		}

		public int PointCount { get; }

		public IPnt GetPoint(int pointIndex, bool clone = false)
		{
			int actualIndex = _reverseOrder
				                  ? _actualStartIndex - pointIndex
				                  : _actualStartIndex + pointIndex;

			WKSPointZ wksPoint = _wksPoints[actualIndex];

			return new Pnt3D(wksPoint.X, wksPoint.Y, wksPoint.Z);
		}

		public void GetCoordinates(int pointIndex, out double x, out double y, out double z)
		{
			int actualIndex = _reverseOrder
				                  ? _actualStartIndex - pointIndex
				                  : _actualStartIndex + pointIndex;

			WKSPointZ wksPoint = _wksPoints[actualIndex];

			x = wksPoint.X;
			y = wksPoint.Y;
			z = wksPoint.Z;
		}

		public IEnumerable<IPnt> AsEnumerablePoints(bool clone = false)
		{
			for (int i = _startIndex; i < _startIndex + PointCount; i++)
			{
				var wksPoint = _wksPoints[i];
				yield return new Pnt3D(wksPoint.X, wksPoint.Y, wksPoint.Z);
			}
		}
	}
}
