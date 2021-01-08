using System;
using System.Collections.Generic;

namespace ProSuite.QA.Container.Geometry
{
	public class SegmentPartComparer : IComparer<SegmentPart>
	{
		#region IComparer<PolySegment> Members

		public int Compare(SegmentPart x, SegmentPart y)
		{
			int i = x.PartIndex - y.PartIndex;
			if (i != 0)
			{
				return i;
			}

			i = x.SegmentIndex - y.SegmentIndex;

			if (i != 0)
			{
				return i;
			}

			i = Math.Sign(x.MinFraction - y.MinFraction);
			if (i != 0)
			{
				return i;
			}

			i = Math.Sign(y.MaxFraction - x.MaxFraction);
			if (i != 0)
			{
				return i;
			}

			if (x.Complete == y.Complete)
			{
				return 0;
			}

			if (x.Complete)
			{
				return -1;
			}

			return 1;
		}

		#endregion
	}
}
