using ProSuite.Commons.AO.Geometry.Proxy;

namespace ProSuite.QA.Tests
{
	internal interface ISegmentPair
	{
		SegmentProxy BaseSegment { get; }
		SegmentProxy RelatedSegment { get; }
	}
}
