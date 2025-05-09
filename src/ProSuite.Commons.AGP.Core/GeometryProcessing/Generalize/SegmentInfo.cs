using ArcGIS.Core.Geometry;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Generalize;

public class SegmentInfo
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SegmentInfo"/> class.
	/// </summary>
	/// <param name="segment">The segment</param>
	/// <param name="globalIndex">The global segment index</param>
	/// <param name="partIndex">The part index of the segment</param>
	/// <param name="localIndex">The segment index relative to its part.</param>
	public SegmentInfo(Segment segment,
	                   int globalIndex,
	                   int partIndex,
	                   int localIndex)
	{
		Segment = segment;

		GlobalIndex = globalIndex;

		SegmentIndex = new SegmentIndex(partIndex, localIndex);
	}

	public Segment Segment { get; }

	public SegmentIndex SegmentIndex { get; }

	public int GlobalIndex { get; }
}
