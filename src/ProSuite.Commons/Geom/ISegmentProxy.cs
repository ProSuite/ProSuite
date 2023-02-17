
namespace ProSuite.Commons.Geom
{
	public interface ISegmentProxy
	{
		int PartIndex { get; }
		int SegmentIndex { get; }
		IPnt Max { get; }
	}
}
