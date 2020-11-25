using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.TestContainer
{
	[CLSCompliant(false)]
	public interface IIndexedMultiPatchFeature : IIndexedSegmentsFeature
	{
		[NotNull]
		IIndexedMultiPatch IndexedMultiPatch { get; }
	}
}
