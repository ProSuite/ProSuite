using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public interface IUserEmailProvider
	{
		[CanBeNull]
		string Email { get; }
	}
}
