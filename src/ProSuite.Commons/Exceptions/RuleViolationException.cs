using System;
using System.Runtime.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.Exceptions
{
	public class RuleViolationException : Exception
	{
		private readonly NotificationCollection _notifications;
		private const string _defaultSeparator = "\r\n";

		#region Constructors

		public RuleViolationException([NotNull] NotificationCollection notifications)
			: base(Concat(notifications))
		{
			_notifications = notifications;
		}

		public RuleViolationException([NotNull] NotificationCollection notifications,
		                              [NotNull] string message)
			: base(Concat(message, notifications))
		{
			_notifications = notifications;
		}

		public RuleViolationException([NotNull] NotificationCollection notifications,
		                              [NotNull] string format,
		                              params object[] args)
			: base(Concat(string.Format(format, args), notifications))
		{
			_notifications = notifications;
		}

		public RuleViolationException(NotificationCollection notifications, string message,
		                              Exception e)
			: base(Concat(message, notifications), e)
		{
			_notifications = notifications;
		}

		protected RuleViolationException(SerializationInfo info, StreamingContext context)
			: base(info, context) { }

		#endregion

		[NotNull]
		public NotificationCollection Notifications
		{
			get { return _notifications; }
		}

		#region Non-public members

		private static string Concat([NotNull] NotificationCollection notifications)
		{
			return notifications.Concatenate(_defaultSeparator, "- {0}");
		}

		private static string Concat([NotNull] string message,
		                             [NotNull] NotificationCollection notifications)
		{
			return string.Format("{0}\n\n{1}", message, Concat(notifications));
		}

		#endregion
	}
}
