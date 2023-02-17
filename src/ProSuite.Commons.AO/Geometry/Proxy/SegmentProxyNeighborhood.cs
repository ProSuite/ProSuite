
using ProSuite.Commons.Essentials.CodeAnnotations;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public class SegmentProxyNeighborhood
	{
		[NotNull]
		public SegmentProxy SegmentProxy { get; set; }

		[NotNull]
		public IEnumerable<SegmentProxy> Neighbours { get; set; }
	}
}
