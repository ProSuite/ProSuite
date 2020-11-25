using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public interface IUniqueIdObject
	{
		[CanBeNull]
		UniqueId UniqueId { get; }
	}
}
