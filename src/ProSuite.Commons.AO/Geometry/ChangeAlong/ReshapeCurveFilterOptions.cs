using System;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class ReshapeCurveFilterOptions
	{
		public ReshapeCurveFilterOptions(bool onlyInVisibleExtent,
		                                 double tolerance,
		                                 bool excludeOutsideSource,
		                                 bool excludeResultingInOverlaps)
		{
			OnlyInVisibleExtent = onlyInVisibleExtent;

			ExcludeTolerance = tolerance;

			ExcludeOutsideSource = excludeOutsideSource;
			ExcludeResultingInOverlaps = excludeResultingInOverlaps;
		}

		[CLSCompliant(false)]
		public ReshapeCurveFilterOptions(IReshapeAlongOptions reshapeAlongOptions)
			: this(
				reshapeAlongOptions.ClipLinesOnVisibleExtent,
				reshapeAlongOptions.ExcludeReshapeLinesOutsideTolerance
					? reshapeAlongOptions.ExcludeReshapeLinesTolerance
					: 0,
				reshapeAlongOptions.ExcludeReshapeLinesOutsideSource,
				reshapeAlongOptions.ExcludeReshapeLinesResultingInOverlaps) { }

		public bool OnlyInVisibleExtent { get; }

		public bool ExcludeOutsideTolerance => ExcludeTolerance > 0;

		public double ExcludeTolerance { get; }

		public bool ExcludeOutsideSource { get; }
		public bool ExcludeResultingInOverlaps { get; }
	}
}
