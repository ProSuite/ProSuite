using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public interface IUserNameProvider
	{
		[NotNull]
		string DisplayName { get; }
	}
}