using System.Collections.Generic;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Validation
{
	public static class ValidationUtils
	{
		[NotNull]
		public static string FormatNotification([NotNull] Notification notification)
		{
			var sb = new StringBuilder();

			AppendNotification(sb, notification);

			return sb.ToString();
		}

		private static void AppendNotification([NotNull] StringBuilder sb,
		                                       [NotNull] Notification notification)
		{
			AppendNotification(sb, notification, string.Empty);
		}

		private static void AppendNotification([NotNull] StringBuilder sb,
		                                       [NotNull] Notification notification,
		                                       string padding)
		{
			Assert.ArgumentNotNull(sb, nameof(sb));
			Assert.ArgumentNotNull(notification, nameof(notification));

			AppendMessages(sb, notification.AllMessages, padding);

			foreach (KeyValuePair<string, Notification> pair in notification.Children)
			{
				Notification childNotification = pair.Value;

				if (childNotification.IsValid())
				{
					continue;
				}

				if (sb.Length > 0)
				{
					// blank line if there's something above
					sb.AppendLine();
				}

				string property = pair.Key;

				// TODO get property LabelText (alias if defined, or else label.Text if defined)

				sb.AppendFormat("{0}- {1}:", padding, property);
				sb.AppendLine();

				string indented = $"{padding}  ";

				AppendNotification(sb, childNotification, indented);
			}
		}

		private static void AppendMessages([NotNull] StringBuilder sb,
		                                   [NotNull] IEnumerable<NotificationMessage>
			                                   messages,
		                                   string padding)
		{
			Assert.ArgumentNotNull(sb, nameof(sb));
			Assert.ArgumentNotNull(messages, nameof(messages));

			foreach (NotificationMessage message in messages)
			{
				sb.AppendFormat("{0}- {1}", padding, message);
				sb.AppendLine();
			}
		}
	}
}