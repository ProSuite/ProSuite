using System;
using System.Collections.Generic;

namespace ProSuite.QA.Container.Geometry
{
	[CLSCompliant(false)]
	public interface ISegmentsCache
	{
		IEnumerable<SegmentProxy> GetSegments();
	}
}
