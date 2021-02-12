using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Geometry
{
	public class SegmentProxyNeighborhood
	{
		[NotNull]
		public SegmentProxy SegmentProxy { get; set; }

		[NotNull]
		public IEnumerable<SegmentProxy> Neighbours { get; set; }
	}
}
