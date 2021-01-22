using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Tests
{
	internal interface ISegmentPair
	{
		SegmentProxy BaseSegment { get; }
		SegmentProxy RelatedSegment { get; }
	}
}
