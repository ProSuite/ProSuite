using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Notifications
{
	public static class NotificationUtils
	{
		/// <summary>
		/// Concatenates the specified notifications into one string.
		/// </summary>
		/// <param name="notifications">The notifications.</param>
		/// <param name="separator">The separator between the notifications.</param>
		/// <param name="format">Optional format string for the individual notifications. 
		/// If the format is specified, it is required to contain exactly one format 
		/// parameter (e.g. "- {0}").</param>
		/// <returns>The string containing all the notifications.</returns>
		[NotNull]
		public static string Concatenate([NotNull] NotificationCollection notifications,
		                                 [CanBeNull] string separator,
		                                 [CanBeNull] string format = null)
		{
			Assert.ArgumentNotNull(notifications, nameof(notifications));

			var sb = new StringBuilder();

			foreach (INotification notification in notifications)
			{
				string message = string.IsNullOrEmpty(format)
					                 ? notification.Message
					                 : string.Format(format, notification.Message);

				if (sb.Length == 0)
				{
					sb.Append(message);
				}
				else
				{
					sb.AppendFormat("{0}{1}", separator, message);
				}
			}

			return sb.ToString();
		}

		public static void Add([CanBeNull] NotificationCollection notifications,
		                       [NotNull] string message)
		{
			notifications?.Add(message);
		}

		[StringFormatMethod("format")]
		public static void Add([CanBeNull] NotificationCollection notifications,
		                       [NotNull] string format,
		                       params object[] args)
		{
			notifications?.Add(format, args);
		}
	}
}