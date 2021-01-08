using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Geometry
{
	public class SegmentProxyNeighborhood
	{
		[CLSCompliant(false)]
		[NotNull]
		public SegmentProxy SegmentProxy { get; set; }

		[CLSCompliant(false)]
		[NotNull]
		public IEnumerable<SegmentProxy> Neighbours { get; set; }
	}
}
