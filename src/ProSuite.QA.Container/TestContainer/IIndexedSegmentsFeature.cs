using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.TestContainer
{
	public interface IIndexedSegmentsFeature
	{
		bool AreIndexedSegmentsLoaded { get; }

		[NotNull]
		IIndexedSegments IndexedSegments { get; }
	}
}
