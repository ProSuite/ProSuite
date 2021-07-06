using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	public interface ISegmentProxy
	{
		int PartIndex { get; }
		int SegmentIndex { get; }
		IPnt Max { get; }
	}
}
