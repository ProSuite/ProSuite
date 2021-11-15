using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Geom
{
	public class Multipoint<T> : IPointList, IEquatable<Multipoint<T>> where T : IPnt
	{
		private readonly List<T> _points;

		public static Multipoint<TP> CreateEmpty<TP>(int capacity = 0) where TP : IPnt
		{
			var result = new Multipoint<TP>(capacity);

			result.SetEmpty();

			return result;
		}

		public static Multipoint<IPnt> CreateEmpty(int capacity = 0)
		{
			var result = new Multipoint<IPnt>(capacity);

			result.SetEmpty();

			return result;
		}

		public Multipoint(int capacity)
		{
			_points = new List<T>(capacity);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Multipoint&lt;T&gt;"/> class using the
		/// provided point instances directly.
		/// </summary>
		/// <param name="points">The points to add to the multipoint directly, WITHOUT CLONING!</param>
		/// <param name="knownCount">The known point count for efficient memory allocation.</param>
		public Multipoint([NotNull] IEnumerable<T> points,
		                  int? knownCount = null)
		{
			Assert.ArgumentNotNull(points, nameof(points));

			if (knownCount == null)
			{
				_points = new List<T>();
			}
			else
			{
				_points = new List<T>(knownCount.Value);
			}

			AddPoints(points);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Multipoint&lt;T&gt;"/> class using the
		/// provided point list instances directly.
		/// </summary>
		/// <param name="points">The list of points to be referenced directly, WITHOUT CLONING!</param>
		/// <param name="xMin">The known X-min value</param>
		/// <param name="yMin">The known Y-min value</param>
		/// <param name="xMax">The known X-max value</param>
		/// <param name="yMax">The known Y-max value</param>
		public Multipoint([NotNull] List<T> points,
		                  double xMin,
		                  double yMin,
		                  double xMax,
		                  double yMax)
		{
			// for clone

			Assert.ArgumentNotNull(points, nameof(points));

			_points = points;

			XMin = xMin;
			YMin = yMin;
			XMax = xMax;
			YMax = yMax;
		}

		public int AllowIndexingThreshold { get; set; } = 200;

		public ISpatialSearcher<int> SpatialIndex { get; set; }

		public EnvelopeXY Extent2D => new EnvelopeXY(this);

		#region IBoundedXY members

		public double XMin { get; private set; } = double.MaxValue;
		public double YMin { get; private set; } = double.MaxValue;

		public double XMax { get; private set; } = double.MinValue;
		public double YMax { get; private set; } = double.MinValue;

		#endregion

		#region IPointList members

		public int PointCount => _points.Count;

		public IPnt GetPoint(int pointIndex, bool clone = false)
		{
			T point = _points[pointIndex];

			return clone ? (T) point.Clone() : point;
		}

		public void GetCoordinates(int pointIndex, out double x, out double y, out double z)
		{
			IPnt point = GetPoint(pointIndex);

			x = point.X;
			y = point.Y;

			if (point is Pnt3D)
			{
				z = point[2];
			}
			else
			{
				z = double.NaN;
			}
		}

		public IEnumerable<IPnt> AsEnumerablePoints(bool clone = false)
		{
			return GetPoints(0, null, clone).Cast<IPnt>();
		}

		#endregion

		#region Equality members

		public bool Equals(Multipoint<T> other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (_points.Count != other._points.Count)
			{
				return false;
			}

			for (int i = 0; i < _points.Count; i++)
			{
				if (! _points[i].Equals(other._points[i]))
				{
					return false;
				}
			}

			return true;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((Multipoint<T>) obj);
		}

		public override int GetHashCode()
		{
			return _points.GetHashCode();
		}

		#endregion

		public override string ToString()
		{
			return $"Point count: {PointCount}";
		}

		public void SetEmpty()
		{
			_points.Clear();

			XMin = double.NaN;
			YMin = double.NaN;
			XMax = double.NaN;
			YMax = double.NaN;

			//_boundingBox = null;
			//SpatialIndex = null;
		}

		public void AddPoints(IEnumerable<T> points)
		{
			foreach (T point in points)
			{
				AddPoint(point);
			}
		}

		public void AddPoint(T point)
		{
			AddPoint(point, true);
		}

		public IEnumerable<T> GetPoints(int startPointIndex = 0,
		                                int? pointCount = null,
		                                bool clone = false)
		{
			int lastPointIndex = PointCount - 1;

			if (startPointIndex == lastPointIndex)
			{
				// last point is start:
				Assert.ArgumentCondition(pointCount == null || pointCount <= 1,
				                         "Requested point count out of range");

				yield return _points[lastPointIndex];

				yield break;
			}

			int endPointIndex;
			if (pointCount != null)
			{
				if (startPointIndex + pointCount > PointCount)
				{
					throw new ArgumentOutOfRangeException(nameof(pointCount),
					                                      "Requested point count out of range");
				}

				endPointIndex = startPointIndex + pointCount.Value - 1;
			}
			else
			{
				endPointIndex = PointCount - 1;
			}

			for (int i = startPointIndex; i <= endPointIndex; i++)
			{
				T current = _points[i];

				yield return clone ? (T) current.Clone() : current;
			}
		}

		/// <summary>
		/// Returns the index of a vertex that is within the XY-tolerance of the specified point.
		/// A potential Z value is ignored.
		/// </summary>
		/// <param name="searchPoint">The search point.</param>
		/// <param name="xyTolerance">The search tolerance.</param>
		/// <param name="useSearchCircle">Whether the search is performed within an actual circle,
		/// i.e. the tolerance is the XY distance threshold. If false, the search is performed
		/// within the box defined by 2*2 times the tolerance.
		/// </param>
		/// <param name="allowIndexing"></param>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<int> FindPointIndexes(IPnt searchPoint,
		                                         double xyTolerance = double.Epsilon,
		                                         bool useSearchCircle = false,
		                                         bool allowIndexing = true)
		{
			if (SpatialIndex == null && allowIndexing &&
			    PointCount > AllowIndexingThreshold)
			{
				SpatialIndex = SpatialHashSearcher<int>.CreateSpatialSearcher(this);
			}

			if (SpatialIndex != null)
			{
				// No need to add the tolerance to the search box, it is added by the index
				foreach (int foundPointIdx in SpatialIndex.Search(
					searchPoint.X, searchPoint.Y, searchPoint.X, searchPoint.Y, xyTolerance))
				{
					T foundPoint = _points[foundPointIdx];

					bool withinTolerance =
						GeomRelationUtils.IsWithinTolerance(foundPoint, searchPoint, xyTolerance,
						                                    useSearchCircle);

					if (withinTolerance)
					{
						yield return foundPointIdx;
					}
				}
			}
			else
			{
				for (var i = 0; i < PointCount; i++)
				{
					bool withinTolerance =
						GeomRelationUtils.IsWithinTolerance(_points[i], searchPoint, xyTolerance,
						                                    useSearchCircle);

					if (withinTolerance)
					{
						yield return i;
					}
				}
			}
		}

		private void AddPoint(T point, bool updateBounds)
		{
			if (updateBounds)
			{
				UpdateBounds(point);
			}

			_points.Add(point);

			SpatialIndex = null;
		}

		private void UpdateBounds([NotNull] IPnt point)
		{
			if (double.IsNaN(XMin))
			{
				InitializeBounds();
			}

			if (point.X < XMin)
			{
				XMin = point.X;
			}

			if (point.X > XMax)
			{
				XMax = point.X;
			}

			if (point.Y < YMin)
			{
				YMin = point.Y;
			}

			if (point.Y > YMax)
			{
				YMax = point.Y;
			}
		}

		private void InitializeBounds()
		{
			XMin = double.MaxValue;
			YMin = double.MaxValue;

			XMax = double.MinValue;
			YMax = double.MinValue;
		}
	}
}
