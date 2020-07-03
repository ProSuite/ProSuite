using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Notifications
{
	public interface INotification
	{
		[NotNull]
		string Message { get; }
	}
}