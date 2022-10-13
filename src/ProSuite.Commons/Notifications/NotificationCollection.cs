using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Notifications
{
	/// <summary>
	/// Collection of rule notifications.
	/// </summary>
	public class NotificationCollection : Collection<INotification>
	{
		[NotNull]
		public INotification Add([NotNull] string message)
		{
			INotification notification = new Notification(message);

			Add(notification);

			return notification;
		}

		[NotNull]
		public INotification Add([NotNull] string format, params object[] args)
		{
			return Add(string.Format(format, args));
		}

		public void AddRange([NotNull] IEnumerable<INotification> notifications)
		{
			foreach (var notification in notifications)
			{
				Add(notification);
			}
		}

		[NotNull]
		public string Concatenate([CanBeNull] string separator)
		{
			return NotificationUtils.Concatenate(this, separator);
		}

		[NotNull]
		public string Concatenate([CanBeNull] string separator, [CanBeNull] string format)
		{
			return NotificationUtils.Concatenate(this, separator, format);
		}
	}
}
