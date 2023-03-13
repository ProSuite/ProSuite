using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using System;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public class SegmentHull
	{
		[NotNull] private readonly SegmentProxy _segment;
		private readonly double _leftOffset;
		private readonly double _rightOffset;
		[NotNull] private readonly SegmentCap _startCap;
		[NotNull] private readonly SegmentCap _endCap;

		public SegmentHull([NotNull] SegmentProxy segment, double offset,
		                   [NotNull] SegmentCap startCap, [NotNull] SegmentCap endCap)
		{
			_segment = segment;
			_leftOffset = offset;
			_rightOffset = offset;
			_startCap = startCap;
			_endCap = endCap;
		}

		public SegmentHull([NotNull] SegmentProxy segment, double leftOffset, double rightOffset,
		                   [NotNull] SegmentCap startCap, [NotNull] SegmentCap endCap)
		{
			_segment = segment;
			_leftOffset = leftOffset;
			_rightOffset = rightOffset;
			_startCap = startCap;
			_endCap = endCap;
		}

		public SegmentProxy Segment
		{
			get { return _segment; }
		}

		public double MaxOffset
		{
			get { return Math.Max(Math.Abs(_leftOffset), Math.Abs(_rightOffset)); }
		}

		public double LeftOffset => _leftOffset;
		public double RightOffset => _rightOffset;

		public SegmentCap StartCap
		{
			get { return _startCap; }
		}

		public SegmentCap EndCap
		{
			get { return _endCap; }
		}

		public bool IsFullDeflatable()
		{
			if (_leftOffset != _rightOffset)
			{
				return false;
			}

			return MaxOffset <= 0 ||
			       StartCap.IsFullDeflatable && EndCap.IsFullDeflatable;
		}

		public override string ToString()
		{
			return
				$"l:{_leftOffset:N1}, r:{_rightOffset:N1} S: {StartCap.GetType().Name}; E: {EndCap.GetType().Name}";
		}
	}
}
