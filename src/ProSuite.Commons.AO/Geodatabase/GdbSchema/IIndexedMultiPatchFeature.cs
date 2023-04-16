
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public interface IIndexedMultiPatchFeature : IIndexedSegmentsFeature
	{
		[NotNull]
		IIndexedMultiPatch IndexedMultiPatch { get; }
	}
}
