using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public interface IIndexedSegmentsFeature
	{
		bool AreIndexedSegmentsLoaded { get; }

		[NotNull]
		IIndexedSegments IndexedSegments { get; }
	}
}
