using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

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

		#region IPointList members

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

		public IEnumerable<int> FindPointIndexes(IPnt searchPoint,
		                                         double xyTolerance = double.Epsilon,
		                                         bool useSearchCircle = false,
		                                         bool allowIndexing = true)
		{
			for (var i = 0; i < PointCount; i++)
			{
				// TODO: Spatial index support
				bool withinBox =
					IsWithinBoxXY(searchPoint.X, searchPoint.Y,
					              _wksPoints[i].X, _wksPoints[i].Y, xyTolerance);

				if (! withinBox)
				{
					continue;
				}

				if (useSearchCircle)
				{
					if (GeomRelationUtils.IsWithinTolerance(
						    new Pnt2D(_wksPoints[i].X, _wksPoints[i].Y), searchPoint, xyTolerance,
						    true))
					{
						yield return i;
					}
				}
				else
				{
					yield return i;
				}
			}
		}

		#endregion

		#region IBoundedXY members

		public double XMin { get; set; } = double.NaN;
		public double YMin { get; set; } = double.NaN;
		public double XMax { get; set; } = double.NaN;
		public double YMax { get; set; } = double.NaN;

		#endregion

		private static bool IsWithinBoxXY(double testPoinX, double testPointY,
		                                  double boxCenterX, double boxCenterY, double tolerance)
		{
			return MathUtils.AreEqual(testPoinX, boxCenterX, tolerance) &&
			       MathUtils.AreEqual(testPointY, boxCenterY, tolerance);
		}
	}
}
