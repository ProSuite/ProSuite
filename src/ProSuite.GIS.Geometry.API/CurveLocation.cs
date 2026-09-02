using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.GIS.Geometry.API
{
	/// <summary>
	/// A position on a curve, expressed as the segment that contains it plus the position
	/// within that segment. The position is always on the curve, never beyond the end of a
	/// segment.
	/// </summary>
	public readonly struct CurveLocation : IEquatable<CurveLocation>
	{
		/// <summary>
		/// The location on no curve at all, used where no location could be determined,
		/// such as on an empty curve.
		/// </summary>
		public static readonly CurveLocation None =
			new CurveLocation(-1, double.NaN);

		[CanBeNull] private readonly IPoint _pointOnCurve;

		public CurveLocation(int segmentIndex, double alongSegmentRatio,
		                     [CanBeNull] IPoint pointOnCurve = null)
		{
			SegmentIndex = segmentIndex;
			AlongSegmentRatio = alongSegmentRatio;

			_pointOnCurve = pointOnCurve;
		}

		/// <summary>
		/// The index of the segment that contains the location. The index counts all segments
		/// of the curve, across parts.
		/// </summary>
		public int SegmentIndex { get; }

		/// <summary>
		/// The position within the segment, as a ratio of the segment's 2D length: 0 is the
		/// segment's start point, 1 is its end point.
		/// </summary>
		public double AlongSegmentRatio { get; }

		public bool IsDefined => SegmentIndex >= 0;

		/// <summary>
		/// The point on the curve at this location, if it is known. It is deliberately not a
		/// property and not part of this location's identity: two locations with the same
		/// segment index and ratio are equal, whether or not the point has been determined.
		/// </summary>
		[CanBeNull]
		public IPoint GetPointOnCurve()
		{
			return _pointOnCurve;
		}

		#region Equality members

		public bool Equals(CurveLocation other)
		{
			return SegmentIndex == other.SegmentIndex &&
			       AlongSegmentRatio.Equals(other.AlongSegmentRatio);
		}

		public override bool Equals(object obj)
		{
			return obj is CurveLocation other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (SegmentIndex * 397) ^ AlongSegmentRatio.GetHashCode();
			}
		}

		#endregion

		public override string ToString()
		{
			return $"Segment {SegmentIndex}, ratio along segment {AlongSegmentRatio}";
		}
	}
}
