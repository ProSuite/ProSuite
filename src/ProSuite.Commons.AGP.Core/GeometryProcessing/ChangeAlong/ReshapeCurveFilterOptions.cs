using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong
{
	public class ReshapeCurveFilterOptions
	{
		public ReshapeCurveFilterOptions(IBoundedXY clipExtent = null,
		                                 bool excludeLInesOutsideSourceBuffer = false,
		                                 double excludeOutSideSourceTolerance = 0)
		{
			ClipExtent = clipExtent;

			if (excludeLInesOutsideSourceBuffer)
			{
				ExcludeOutsideSourceBufferTolerance = excludeOutSideSourceTolerance;
			}
		}

		public IBoundedXY ClipExtent { get; }

		public double ExcludeOutsideSourceBufferTolerance { get; }

		public bool OnlyResultingInRemovals { get; set; }

		public bool ExcludeResultingInOverlaps { get; set; }
	}
}
