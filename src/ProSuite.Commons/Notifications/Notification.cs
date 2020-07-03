using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Notifications
{
	public class Notification : INotification
	{
		[NotNull] private readonly string _message;

		#region Constructors

		public Notification([NotNull] string message)
		{
			_message = message;
		}

		#endregion

		public string Message => _message;
	}
}
