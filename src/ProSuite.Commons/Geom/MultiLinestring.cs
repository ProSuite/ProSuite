using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Geom
{
	public abstract class MultiLinestring : ISegmentList, IPointList, IEquatable<MultiLinestring>
	{
		protected List<Linestring> Linestrings { get; }

		public double XMin { get; private set; } = double.MaxValue;
		public double YMin { get; private set; } = double.MaxValue;

		public double XMax { get; private set; } = double.MinValue;
		public double YMax { get; private set; } = double.MinValue;

		private List<int> _emptyPartIndexes;
		private readonly List<int> _startSegmentIndexes = new List<int>();

		private bool? _isKnownClosed;

		protected MultiLinestring(IEnumerable<Linestring> linestrings)
		{
			Linestrings = new List<Linestring>(linestrings);

			foreach (Linestring linestring in Linestrings)
			{
				UpdateBounds(linestring);
			}

			CacheStartIndexes();
		}

		public int Count => Linestrings.Count;

		public int PartCount => Count;

		public bool IsEmpty => Count == 0 || Linestrings.TrueForAll(l => l.IsEmpty);

		public int SegmentCount => Linestrings.Sum(l => l.SegmentCount);

		public bool IsClosed
		{
			get
			{
				// This is a significant difference for topo-op performance!
				if (_isKnownClosed == null)
				{
					_isKnownClosed = Linestrings.All(l => l.IsClosed);
				}

				return _isKnownClosed.Value;
			}
		}

		public Linestring GetLinestring(int index)
		{
			return Linestrings[index];
		}

		public IEnumerable<Linestring> GetLinestrings()
		{
			return Linestrings;
		}

		public Line3D GetSegment(int linestringIndex, int localIndex)
		{
			return Linestrings[linestringIndex].GetSegment(localIndex);
		}

		public Line3D GetSegment(int globalIndex)
		{
			int partIndex;
			int localSegmentIndex = GetLocalSegmentIndex(globalIndex, out partIndex);

			return GetSegment(partIndex, localSegmentIndex);
		}

		public Linestring GetPart(int index)
		{
			return GetLinestring(index);
		}

		public int AllowIndexingThreshold { get; set; } = 200;

		[CanBeNull]
		public SpatialHashSearcher<SegmentIndex> SpatialIndex { get; set; }

		public bool Equals(MultiLinestring other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (Count != other.Count)
			{
				// Check number of linestrings
				return false;
			}

			for (int i = 0; i < Count; i++)
			{
				if (! GetLinestring(i).Equals(other.GetLinestring(i)))
				{
					return false;
				}
			}

			return true;
		}

		public IEnumerator<Line3D> GetEnumerator()
		{
			return Linestrings.SelectMany(linestring => linestring).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
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

			return Equals((MultiLinestring) obj);
		}

		public override int GetHashCode()
		{
			int hashCode = Linestrings != null ? Linestrings.GetHashCode() : 0;

			return hashCode;
		}

		public void SetEmpty()
		{
			Linestrings.Clear();

			XMin = double.NaN;
			YMin = double.NaN;
			XMax = double.NaN;
			YMax = double.NaN;

			SpatialIndex = null;
		}

		public bool IsFirstPointInPart(int vertexIndex)
		{
			return IsFirstPointInPart(vertexIndex, out int _);
		}

		public bool IsLastPointInPart(int vertexIndex)
		{
			return IsLastPointInPart(vertexIndex, out int _);
		}

		public bool IsFirstPointInPart(int vertexIndex, out int partIndex)
		{
			partIndex = 0;
			foreach (int startSegmentIndex in _startSegmentIndexes)
			{
				if (startSegmentIndex >= 0)
				{
					int startPointIdx = GetSegmentStartPointIndex(startSegmentIndex);

					if (vertexIndex == startPointIdx)
					{
						return true;
					}
				}

				partIndex++;
			}

			return false;
		}

		public bool IsLastPointInPart(int vertexIndex, out int partIndex)
		{
			partIndex = 0;
			int lastStartPointIdx = 0;

			for (var partIdx = 0; partIdx < _startSegmentIndexes.Count; partIdx++)
			{
				int startSegmentIndex = _startSegmentIndexes[partIdx];

				if (_emptyPartIndexes != null &&
				    _emptyPartIndexes.Contains(partIdx))
				{
					continue;
				}

				int startPointIdx = GetSegmentStartPointIndex(startSegmentIndex);

				if (vertexIndex < startPointIdx)
				{
					partIndex = partIdx - 1;

					return Linestrings[partIndex].IsLastPointInPart(
						vertexIndex - lastStartPointIdx);
				}

				lastStartPointIdx = startPointIdx;
			}

			partIndex = Count - 1;

			return Linestrings[partIndex].IsLastPointInPart(
				vertexIndex - lastStartPointIdx);
		}

		#region IPointList members and point index methods

		public int PointCount => SegmentCount + Linestrings.Count;

		public IPnt GetPoint(int pointIndex, bool clone = false)
		{
			int localPointIndex = GetLocalPointIndex(pointIndex, out int partIndex);

			return Linestrings[partIndex].GetPoint(localPointIndex, clone);
		}

		public void GetCoordinates(int pointIndex, out double x, out double y, out double z)
		{
			IPnt point = GetPoint(pointIndex);

			x = point.X;
			y = point.Y;
			z = point[2];
		}

		public IEnumerable<IPnt> AsEnumerablePoints(bool clone = false)
		{
			return GetPoints(clone);
		}

		public IEnumerable<int> FindPointIndexes(IPnt searchPoint,
		                                         double xyTolerance = double.Epsilon,
		                                         bool useSearchCircle = false,
		                                         bool allowIndexing = true)
		{
			if (SpatialIndex == null && allowIndexing &&
			    SegmentCount > AllowIndexingThreshold)
			{
				SpatialIndex = SpatialHashSearcher<int>.CreateSpatialSearcher(this);
			}

			if (SpatialIndex != null)
			{
				foreach (var foundSegmentIdx in SpatialIndex.Search(searchPoint.X, searchPoint.Y,
					         searchPoint.X, searchPoint.Y, xyTolerance))
				{
					Line3D segment =
						GetSegment(foundSegmentIdx.PartIndex, foundSegmentIdx.LocalIndex);

					bool withinTolerance =
						GeomRelationUtils.IsWithinTolerance(segment.StartPoint, searchPoint,
						                                    xyTolerance, useSearchCircle);

					if (withinTolerance)
					{
						yield return GetGlobalPointIndex(foundSegmentIdx.PartIndex,
						                                 foundSegmentIdx.LocalIndex);
					}

					if (Linestrings[foundSegmentIdx.PartIndex]
						    .IsLastSegmentInPart(foundSegmentIdx.LocalIndex) &&
					    Linestrings[foundSegmentIdx.PartIndex].EndPoint
					                                          .EqualsXY(searchPoint, xyTolerance))
					{
						yield return GetGlobalPointIndex(foundSegmentIdx.PartIndex,
						                                 foundSegmentIdx.LocalIndex + 1);
					}
				}
			}
			else
			{
				for (var i = 0; i < PartCount; i++)
				{
					Linestring linestring = Linestrings[i];

					foreach (int localIndex in linestring.FindPointIndexes(
						         searchPoint, xyTolerance, useSearchCircle, allowIndexing))
					{
						yield return GetGlobalPointIndex(i, localIndex);
					}
				}
			}
		}

		public int GetLocalPointIndex(int globalPointIndex, out int partIndex)
		{
			// TODO: Start with GetLowerBound and convert from segment indexes to point idx.

			int localPointIndex = globalPointIndex;
			partIndex = 0;
			foreach (Linestring linestring in Linestrings)
			{
				if (localPointIndex < linestring.PointCount)
				{
					return localPointIndex;
				}

				localPointIndex -= linestring.PointCount;

				partIndex++;
			}

			throw new ArgumentOutOfRangeException($"Invalid global index: {globalPointIndex}");
		}

		public int GetGlobalPointIndex(int partIndex, int localPointIndex)
		{
			int startSegmentIndex = _startSegmentIndexes[partIndex];

			if (_emptyPartIndexes != null &&
			    _emptyPartIndexes.Contains(partIndex))
			{
				throw new ArgumentOutOfRangeException(nameof(partIndex), partIndex,
				                                      "The part is empty.");
			}

			// All previous parts that are empty do not have the extra point:
			int emptyPartCount = _emptyPartIndexes == null
				                     ? 0
				                     : _emptyPartIndexes.Count(i => i < partIndex);

			int partStartIndex = startSegmentIndex + partIndex - emptyPartCount;

			return partStartIndex + localPointIndex;
		}

		public void SnapToResolution(double resolution,
		                             double xOrigin,
		                             double yOrigin,
		                             double zOrigin = double.NaN)
		{
			InitializeBounds();

			foreach (Linestring linestring in Linestrings)
			{
				linestring.SnapToResolution(resolution, xOrigin, yOrigin, zOrigin);
				UpdateBounds(linestring);
			}

			SpatialIndex = null;
		}

		#endregion

		public bool IsFirstSegmentInPart(int segmentIndex)
		{
			return _startSegmentIndexes.Contains(segmentIndex);
		}

		public bool IsLastSegmentInPart(int segmentIndex)
		{
			int partIndex;
			int localSegmentIndex = GetLocalSegmentIndex(segmentIndex, out partIndex);

			return Linestrings[partIndex].IsLastSegmentInPart(localSegmentIndex);
		}

		public int GetSegmentStartPointIndex(int segmentIndex)
		{
			int partIndex;
			GetLocalSegmentIndex(segmentIndex, out partIndex);

			return segmentIndex + partIndex;
		}

		public Line3D this[int index] => GetSegment(index);

		public int? NextSegmentIndex(int currentSegmentIndex)
		{
			int partIndex;
			int localSegmentIndex = GetLocalSegmentIndex(currentSegmentIndex, out partIndex);

			int? localResult = Linestrings[partIndex].NextSegmentIndex(localSegmentIndex);

			if (localResult != null)
			{
				return GetGlobalSegmentIndex(partIndex, localResult.Value);
			}

			return null;
		}

		public int? PreviousSegmentIndex(int currentSegmentIndex)
		{
			int partIndex;
			int localSegmentIndex = GetLocalSegmentIndex(currentSegmentIndex, out partIndex);

			int? localResult = Linestrings[partIndex].PreviousSegmentIndex(localSegmentIndex);

			if (localResult != null)
			{
				return GetGlobalSegmentIndex(partIndex, localResult.Value);
			}

			return null;
		}

		public Line3D NextSegment(int currentSegmentIdx,
		                          bool skipVerticalSegments = false,
		                          double tolerance = 0)
		{
			int partIndex;
			int localSegmentIndex = GetLocalSegmentIndex(currentSegmentIdx, out partIndex);

			return Linestrings[partIndex]
				.NextSegment(localSegmentIndex, skipVerticalSegments, tolerance);
		}

		public Line3D PreviousSegment(int currentSegmentIdx,
		                              bool skipVerticalSegments = false,
		                              double tolerance = 0)
		{
			int partIndex;
			int localSegmentIndex = GetLocalSegmentIndex(currentSegmentIdx, out partIndex);

			return Linestrings[partIndex].PreviousSegment(
				localSegmentIndex, skipVerticalSegments, tolerance);
		}

		public IEnumerable<Pnt3D> GetPoints(bool clone = false)
		{
			foreach (Linestring linestring in Linestrings)
			{
				foreach (var point in linestring.GetPoints(0, null, clone))
				{
					yield return point;
				}
			}
		}

		public void AddLinestring([NotNull] Linestring linestring)
		{
			Linestrings.Add(linestring);

			UpdateBounds(linestring);

			UpdateSpatialIndex(linestring, Linestrings.Count - 1);

			CacheStartIndexes();

			AddLinestringCore(linestring);

			_isKnownClosed = null;
		}

		protected virtual void AddLinestringCore(Linestring linestring) { }

		public void InsertLinestring(int index, [NotNull] Linestring linestring)
		{
			Linestrings.Insert(index, linestring);

			UpdateBounds(linestring);

			SpatialIndex = null;

			CacheStartIndexes();

			InsertLinestringCore(index, linestring);

			_isKnownClosed = null;
		}

		protected virtual void InsertLinestringCore(int index, Linestring linestring) { }

		public bool RemoveLinestring(Linestring linestring)
		{
			bool result = Linestrings.Remove(linestring);

			InitializeBounds();

			foreach (Linestring l in Linestrings)
			{
				UpdateBounds(l);
			}

			SpatialIndex = null;

			CacheStartIndexes();

			RemoveLinestringCore(linestring);

			return result;
		}

		protected virtual void RemoveLinestringCore(Linestring linestring) { }

		/// <summary>
		/// In case a linestring has been updated in a way that could effect cached properties
		/// of this instance, such as bounds, spatial index, whether it is closed or not, etc.
		/// this method should be called.
		/// </summary>
		public void InvalidateCachedProperties()
		{
			_isKnownClosed = null;

			InitializeBounds();

			foreach (Linestring l in Linestrings)
			{
				UpdateBounds(l);
			}

			SpatialIndex = null;

			CacheStartIndexes();
		}

		public double GetLength2D()
		{
			return Linestrings.Sum(l => l.GetLength2D());
		}

		public int GetLocalSegmentIndex(int globalSegmentIndex, out int linestringIndex)
		{
			linestringIndex = GetLowerBound(_startSegmentIndexes, globalSegmentIndex);

			if (linestringIndex < 0)
			{
				throw new IndexOutOfRangeException("Invalid result of binary search.");
			}

			if (_emptyPartIndexes != null &&
			    _emptyPartIndexes.Contains(linestringIndex))
			{
				if (linestringIndex > 0)
				{
					// It's the last segment of the previous part
					linestringIndex--;
				}
				else
				{
					// The first part is empty: Find the next non-empty part
					do
					{
						linestringIndex++;
					} while (_emptyPartIndexes.Contains(linestringIndex));

					if (linestringIndex >= _startSegmentIndexes.Count)
					{
						throw new IndexOutOfRangeException(
							"Invalid global segment index. One or more parts are empty.");
					}
				}
			}

			int startSegmentIndex = _startSegmentIndexes[linestringIndex];

			return globalSegmentIndex - startSegmentIndex;
		}

		public int GetGlobalSegmentIndex(int linestringIndex, int localSegmentIndex)
		{
			int startSegmentIndex = _startSegmentIndexes[linestringIndex];

			if (_emptyPartIndexes != null &&
			    _emptyPartIndexes.Contains(linestringIndex))
			{
				throw new ArgumentOutOfRangeException(nameof(linestringIndex), linestringIndex,
				                                      "The linestring is empty.");
			}

			return startSegmentIndex + localSegmentIndex;
		}

		public IEnumerable<KeyValuePair<int, Line3D>> FindSegments(
			IBoundedXY searchGeometry,
			double tolerance,
			bool allowIndexing = true,
			Predicate<int> predicate = null)
		{
			return FindSegments(searchGeometry.XMin, searchGeometry.YMin,
			                    searchGeometry.XMax, searchGeometry.YMax, tolerance, allowIndexing,
			                    predicate);
		}

		public IEnumerable<KeyValuePair<int, Line3D>> FindSegments(
			double xMin, double yMin, double xMax, double yMax,
			double tolerance, bool allowIndexing = true, Predicate<int> predicate = null)
		{
			if (SpatialIndex == null && allowIndexing &&
			    SegmentCount > AllowIndexingThreshold)
			{
				SpatialIndex = SpatialHashSearcher<SegmentIndex>.CreateSpatialSearcher(this);
			}

			Predicate<SegmentIndex> multipartIndexPredicate = null;
			if (predicate != null)
			{
				multipartIndexPredicate =
					found =>
					{
						int globalIdx = GetGlobalSegmentIndex(found.PartIndex, found.LocalIndex);

						return predicate(globalIdx);
					};
			}

			// NOTE: Using SpatialHashSearcher<int> and converting to local segment indexes is much slower!
			//       -> consider optimized MultiLinestring that is based off one long array of points (WKSPoint structs?)
			if (SpatialIndex != null)
			{
				foreach (SegmentIndex segmentIndex in SpatialIndex.Search(
					         xMin, yMin, xMax, yMax, this, tolerance, multipartIndexPredicate))
				{
					Line3D segment = GetSegment(segmentIndex.PartIndex, segmentIndex.LocalIndex);

					if (segment.ExtentIntersectsXY(
						    xMin, yMin, xMax, yMax, tolerance))
					{
						yield return new KeyValuePair<int, Line3D>(
							GetGlobalSegmentIndex(segmentIndex.PartIndex, segmentIndex.LocalIndex),
							segment);
					}
				}
			}
			else
			{
				// foreach is faster than index-access (due to global->local index conversions)
				int index = -1;
				foreach (Line3D segment in this)
				{
					index++;

					if (predicate != null && ! predicate(index))
					{
						continue;
					}

					if (segment.ExtentIntersectsXY(
						    xMin, yMin, xMax, yMax, tolerance))
					{
						yield return new KeyValuePair<int, Line3D>(index, segment);
					}
				}
			}
		}

		public IEnumerable<int> FindParts(
			IBoundedXY searchGeometry,
			double tolerance, bool allowIndexing = true)
		{
			if (GeomRelationUtils.AreBoundsDisjoint(this, searchGeometry, tolerance))
			{
				yield break;
			}

			if (GeomRelationUtils.AreBoundsContained(this, searchGeometry, tolerance))
			{
				foreach (int partIdx in Enumerable.Range(0, PartCount))
				{
					yield return partIdx;
				}
			}

			// Some geometries have a ridiculous amount of parts!
			if (SpatialIndex == null && allowIndexing &&
			    PartCount > AllowIndexingThreshold)
			{
				SpatialIndex = SpatialHashSearcher<SegmentIndex>.CreateSpatialSearcher(this);
			}

			HashSet<int> foundParts = new HashSet<int>();

			if (SpatialIndex != null)
			{
				foreach (SegmentIndex segmentIndex in SpatialIndex.Search(
					         searchGeometry.XMin, searchGeometry.YMin, searchGeometry.XMax,
					         searchGeometry.YMax,
					         this, tolerance))
				{
					if (foundParts.Contains(segmentIndex.PartIndex))
					{
						continue;
					}

					Line3D segment = GetSegment(segmentIndex.PartIndex, segmentIndex.LocalIndex);

					if (segment.ExtentIntersectsXY(searchGeometry, tolerance))
					{
						foundParts.Add(segmentIndex.PartIndex);
						yield return segmentIndex.PartIndex;
					}
				}
			}
			else
			{
				for (int i = 0; i < PartCount; i++)
				{
					IBoundedXY part = GetPart(i);

					if (! GeomRelationUtils.AreBoundsDisjoint(this, part, tolerance))
					{
						yield return i;
					}
				}
			}
		}

		public void ReverseOrientation()
		{
			foreach (Linestring linestring in Linestrings)
			{
				linestring.ReverseOrientation();
			}
		}

		public double GetArea2D()
		{
			return Linestrings.Sum(r => r.GetArea2D());
		}

		public void InterpolateUndefinedZs()
		{
			foreach (Linestring linestring in Linestrings)
			{
				linestring.TryInterpolateUndefinedZs();
			}

			_isKnownClosed = null;
		}

		public void AssignUndefinedZs([NotNull] Plane3D fromPlane)
		{
			foreach (Linestring linestring in Linestrings)
			{
				linestring.AssignUndefinedZs(fromPlane);
			}

			_isKnownClosed = null;
		}

		private void UpdateBounds([NotNull] Linestring additionalLinestring)
		{
			if (additionalLinestring.XMin < XMin)
			{
				XMin = additionalLinestring.XMin;
			}

			if (additionalLinestring.XMax > XMax)
			{
				XMax = additionalLinestring.XMax;
			}

			if (additionalLinestring.YMin < YMin)
			{
				YMin = additionalLinestring.YMin;
			}

			if (additionalLinestring.YMax > YMax)
			{
				YMax = additionalLinestring.YMax;
			}
		}

		private void InitializeBounds()
		{
			XMin = double.MaxValue;
			YMin = double.MaxValue;

			XMax = double.MinValue;
			YMax = double.MinValue;

			_isKnownClosed = null;
		}

		private void UpdateSpatialIndex([NotNull] Linestring additionalLinestring,
		                                int partIndex)
		{
			if (SpatialIndex == null)
			{
				return;
			}

			for (int i = 0; i < additionalLinestring.SegmentCount; i++)
			{
				Line3D line = additionalLinestring[i];

				SpatialIndex.Add(new SegmentIndex(partIndex, i),
				                 line.XMin, line.YMin, line.XMax, line.YMax);
			}
		}

		private void CacheStartIndexes(Linestring additionalLinestring = null)
		{
			int globalIndex = 0;

			if (additionalLinestring != null)
			{
				if (additionalLinestring.IsEmpty)
				{
					AddEmptyPartIndex(PartCount - 1);
					_startSegmentIndexes.Add(globalIndex);
				}

				globalIndex = SegmentCount;

				_startSegmentIndexes.Add(globalIndex);

				return;
			}

			_startSegmentIndexes.Clear();
			_emptyPartIndexes = null;

			for (var partIndex = 0; partIndex < Linestrings.Count; partIndex++)
			{
				Linestring linestring = Linestrings[partIndex];

				if (linestring.IsEmpty)
				{
					AddEmptyPartIndex(partIndex);
					_startSegmentIndexes.Add(globalIndex);
				}
				else
				{
					_startSegmentIndexes.Add(globalIndex);

					globalIndex += linestring.SegmentCount;
				}
			}
		}

		private void AddEmptyPartIndex(int partIndex)
		{
			if (_emptyPartIndexes == null)
			{
				_emptyPartIndexes = new List<int>();
			}

			_emptyPartIndexes.Add(partIndex);
		}

		private static int GetLowerBound<T>(List<T> values, T target) where T : IComparable<T>
		{
			// NOTE: This is critical for the performance, and would still be the bottleneck
			//       if the spatial index only contained the global index.
			int index = values.BinarySearch(target);

			if (index < 0)
			{
				// get the first value that is larger:
				return ~index - 1;
			}

			return index;
		}

		public abstract MultiLinestring Clone();

		public void DropZs()
		{
			foreach (Pnt3D point in GetPoints())
			{
				point.Z = double.NaN;
			}
		}
	}
}
