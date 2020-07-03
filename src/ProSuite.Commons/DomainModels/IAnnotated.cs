using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	public interface IAnnotated
	{
		[CanBeNull]
		string Description { get; }
	}
}