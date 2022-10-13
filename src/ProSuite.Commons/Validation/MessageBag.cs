using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Validation
{
	public class MessageBag
	{
		[NotNull] private readonly List<NotificationMessage> _list =
			new List<NotificationMessage>();

		public MessageBag([NotNull] string fieldName)
		{
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			FieldName = fieldName;
		}

		[PublicAPI]
		public string FieldName { get; }

		public void Add([NotNull] NotificationMessage message)
		{
			Assert.ArgumentNotNull(message, nameof(message));

			_list.Add(message);
		}

		[NotNull]
		public NotificationMessage[] Messages => _list.ToArray();

		public bool Contains([NotNull] NotificationMessage message)
		{
			Assert.ArgumentNotNull(message, nameof(message));

			return _list.Contains(message);
		}
	}
}
