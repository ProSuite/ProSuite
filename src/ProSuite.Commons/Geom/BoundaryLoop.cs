using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	internal class BoundaryLoop
	{
		private readonly bool _isSourceRing;
		private Linestring _loop1;
		private Linestring _loop2;

		public BoundaryLoop([NotNull] IntersectionPoint3D start,
		                    [NotNull] IntersectionPoint3D end,
		                    [NotNull] Linestring fullRing,
		                    bool isSourceRing)
		{
			Start = start;
			End = end;
			FullRing = fullRing;

			_isSourceRing = isSourceRing;
		}

		[NotNull]
		public IntersectionPoint3D Start { get; }

		[NotNull]
		public IntersectionPoint3D End { get; }

		/// <summary>
		/// The entire linestring containing both loops.
		/// </summary>
		[NotNull]
		private Linestring FullRing { get; }

		/// <summary>
		/// The first loop, which is the segments between the start and the end intersection.
		/// </summary>
		public Linestring Loop1
		{
			get
			{
				if (_loop1 == null)
				{
					_loop1 = GetSubcurve(Start, End, FullRing);
				}

				return _loop1;
			}
		}

		/// <summary>
		/// The second loop, which is the segments between the end and the start intersection.
		/// </summary>
		public Linestring Loop2
		{
			get
			{
				if (_loop2 == null)
				{
					_loop2 = GetSubcurve(End, Start, FullRing);
				}

				return _loop2;
			}
		}

		public bool IsLoopingToOutside
		{
			get
			{
				if (Loop1.ClockwiseOriented == true && Loop2.ClockwiseOriented == true)
				{
					return true;
				}

				if (Loop1.ClockwiseOriented == false && Loop2.ClockwiseOriented == false)
				{
					return false;
				}

				// Check the actual rings
				bool? loop1Contains2 =
					GeomRelationUtils.AreaContainsXY(Loop1, Loop2, double.Epsilon);
				if (loop1Contains2 == true)
				{
					return false;
				}

				bool? loop2Contains1 =
					GeomRelationUtils.AreaContainsXY(Loop2, Loop1, double.Epsilon);
				if (loop2Contains1 == true)
				{
					return false;
				}

				return true;
			}
		}

		public bool Loop1ContainsLoop2 =>
			GeomRelationUtils.AreaContainsXY(Loop1, Loop2, double.Epsilon) == true;

		public bool Loop2ContainsLoop1 =>
			GeomRelationUtils.AreaContainsXY(Loop2, Loop1, double.Epsilon) == true;

		public IEnumerable<Linestring> GetBothRings()
		{
			yield return Loop1;

			yield return Loop2;
		}

		private Linestring GetSubcurve([NotNull] IntersectionPoint3D fromIntersection,
		                               [NotNull] IntersectionPoint3D toIntersection,
		                               [NotNull] Linestring fullRing)
		{
			double fromDistanceAlongAsRatio;
			double toDistanceAlongAsRatio;

			int fromIndex;
			int toIndex;
			if (_isSourceRing)
			{
				fromIndex = fromIntersection.GetLocalSourceIntersectionSegmentIdx(
					fullRing, out fromDistanceAlongAsRatio);
				toIndex = toIntersection.GetLocalSourceIntersectionSegmentIdx(
					fullRing, out toDistanceAlongAsRatio);
			}
			else
			{
				fromIndex = fromIntersection.GetLocalTargetIntersectionSegmentIdx(
					fullRing, out fromDistanceAlongAsRatio);
				toIndex = toIntersection.GetLocalTargetIntersectionSegmentIdx(
					fullRing, out toDistanceAlongAsRatio);
			}

			const bool forward = true;
			const bool preferFullRing = true;
			Linestring subcurve = fullRing.GetSubcurve(
				fromIndex, fromDistanceAlongAsRatio,
				toIndex, toDistanceAlongAsRatio,
				false, ! forward, preferFullRing);

			// Typically the Z-values could differ:
			subcurve.Close();

			return subcurve;
		}

		#region Equality members

		protected bool Equals(BoundaryLoop other)
		{
			return Start.Equals(other.Start) && End.Equals(other.End);
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

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((BoundaryLoop) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Start.GetHashCode() * 397) ^ End.GetHashCode();
			}
		}

		#endregion
	}

	internal class BoundaryLoopComparer : IEqualityComparer<BoundaryLoop>
	{
		#region Implementation of IEqualityComparer<in BoundaryLoop>

		public bool Equals(BoundaryLoop x, BoundaryLoop y)
		{
			if (x == null && y == null)
			{
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}

			if (x.Start.Equals(y.Start) ||
			    x.Start.Equals(y.End) &&
			    x.End.Equals(y.End) ||
			    x.End.Equals(y.Start))
			{
				return true;
			}

			return false;
		}

		public int GetHashCode(BoundaryLoop obj)
		{
			return (obj.Start.GetHashCode()) ^ obj.End.GetHashCode();
		}

		#endregion
	}
}
