using ProSuite.Commons.AO.Geometry.Proxy;
using System.Collections.Generic;

namespace ProSuite.QA.Container.Geometry
{
	public interface ISegmentsCache
	{
		IEnumerable<SegmentProxy> GetSegments();
	}
}
