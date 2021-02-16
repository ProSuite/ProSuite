using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.TestContainer
{
	public interface IIndexedMultiPatchFeature : IIndexedSegmentsFeature
	{
		[NotNull]
		IIndexedMultiPatch IndexedMultiPatch { get; }
	}
}
