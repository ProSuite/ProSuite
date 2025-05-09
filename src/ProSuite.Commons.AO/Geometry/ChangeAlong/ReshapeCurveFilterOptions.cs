namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class ReshapeCurveFilterOptions
	{
		public ReshapeCurveFilterOptions(bool onlyInVisibleExtent = false,
		                                 double excludeTolerance = 0,
		                                 bool onlyResultingInRemovals = false,
		                                 bool excludeResultingInOverlaps = false)
		{
			OnlyInVisibleExtent = onlyInVisibleExtent;

			ExcludeTolerance = excludeTolerance;

			OnlyResultingInRemovals = onlyResultingInRemovals;
			ExcludeResultingInOverlaps = excludeResultingInOverlaps;
		}

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

		public bool OnlyResultingInRemovals { get; }
		public bool ExcludeResultingInOverlaps { get; }
	}
}
